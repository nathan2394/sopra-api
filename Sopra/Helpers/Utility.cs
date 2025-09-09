using Microsoft.EntityFrameworkCore;

using System;
using System.Configuration;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using FirebaseAdmin.Messaging;
using MySqlConnector;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using Sopra.Entities;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System.IO;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;

using System.Security.Cryptography;
using System.Text;

namespace Sopra.Helpers
{
    public static class Utility
    {
        public static SqlConnection WMSDBConnection;
        public static SqlConnection SQLDBConnection;
        public static MySqlConnection MySQLDBConnection;
        public static DateTime SyncDate;
        public static string APIURL;
        public static DateTime TransactionSyncDate;
        private static IConfiguration config;
        private static FirebaseMessaging _messaging;
        private static bool _isFirebaseMessaginginit = false;

        public static IConfiguration Configuration
        {
            get
            {
                if (config == null)
                {
                    var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json");
                    config = builder.Build();
                }
                return config;
            }
        }

        public static string Secret { get { return Configuration.GetSection("AppSettings:Secret").Value; } }
        public static string SQL_Server { get { return Configuration.GetSection("SQL:Server").Value; } }
        public static string SQL_Database { get { return Configuration.GetSection("SQL:Database").Value; } }
        public static string SQL_UserID { get { return Configuration.GetSection("SQL:UserID").Value; } }
        public static string SQL_Password { get { return Configuration.GetSection("SQL:Password").Value; } }
        public static string GoogleApplicationCredential { get { return Configuration.GetSection("GoogleApplicationCredential").Value; } }
        public static DateTime getCurrentTimestamps()
        {
            DateTime utcNow = DateTime.UtcNow; // Get the current UTC time
            return currentTimezone(utcNow);
        }
        public static DateTime currentTimezone(DateTime value)
        {
            TimeZoneInfo gmtPlus7 = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time"); // Time zone for GMT+7
            DateTime gmtPlus7Time = TimeZoneInfo.ConvertTimeFromUtc(value, gmtPlus7); // Convert UTC to GMT+7
            return gmtPlus7Time;
        }
        public static IConfiguration GetConfig()
        {
            // Example: Load configuration from appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            return builder.Build();
        }

        public static string GenerateAttachmentKey(string voucherNo, long id, DateTime transDate)
        {
            string combined = $"{voucherNo}|{id}|{transDate:yyyy-MM-dd HH:mm:ss}";

            using (var sha256 = SHA256.Create())
            {
                byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                // Remove padding and make URL-safe
                return Convert.ToBase64String(hashBytes)
                    .Replace('+', '-')
                    .Replace('/', '_')
                    .Replace('=', '\0').Replace("\0", "");
            }
        }

        public static User UserFromToken(string token)
        {
            try
            {
                //var secret = config.GetSection("AppSettings")["Secret"];
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.ASCII.GetBytes(Secret);
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var id = long.Parse(jwtToken.Claims.First(x => x.Type == "id").Value);

                //var yourEFContextInstance = new EFContext();
                //var service = new UserService(context);
                //var user = service.GetByIdAsync(id).Result;
                //return user;
                return null;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                return null;
            }
        }

        public static string HashPassword(string password)
        {
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            return hashedPassword;
        }

        // private static byte[] GenerateRandomSalt()
        // {
        //     using (var rng = new RNGCryptoServiceProvider())
        //     {
        //         byte[] salt = new byte[16];
        //         rng.GetBytes(salt);
        //         return salt;
        //     }
        // }

        public static bool VerifyHashedPassword(string hashedPassword, string password)
        {
            try
            {
                return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            }
            catch (BCrypt.Net.SaltParseException)
            {
                // Handle the case where the salt format is not valid
                return false;
            }
        }

        public static async Task AfterSave(EFContext context, string tableName, long id, string type)
        {
            // Check Validate
            //var results = await context.AfterSave.FromSqlRaw("EXEC [spAfterSave] @TableName, @Oid, @Type"
            //    , new SqlParameter("@TableName", tableName)
            //    , new SqlParameter("@Oid", id)
            //    , new SqlParameter("@Type", type)).ToListAsync();
            //if (results.Count > 0)
            //{
            //    var result = results[0];
            //    if (result.Err == 1)
            //        throw new Exception(result.ErrMessage);
            //}
        }

        public static void DeleteDiffData(string mysqlTableName, string sqlTableName, string addCond = "", string primaryKey = "id", string foreignKey = "RefID")
        {
            var ids = Utility.MySqlGetObjects($"SELECT GROUP_CONCAT({primaryKey}) FROM {mysqlTableName}", Utility.MySQLDBConnection);
            Utility.ExecuteNonQuery(@$"
                delete {sqlTableName}
                where {foreignKey} not in (
                    {ids.Rows[0][0]}
                ) {addCond}");
        }

        #region SQLFunction
        public static void ConnectSQL(string server, string database, string userid, string password)
        {
            if (SQLDBConnection == null)
                SQLDBConnection = new SqlConnection(string.Format("Server={0};Database={1};User Id={2};Password={3};", server, database, userid, password));
        }

        public static void ConnectWMSSQL(string server, string database, string userid, string password)
        {
            if (WMSDBConnection == null)
                WMSDBConnection = new SqlConnection(string.Format("Server={0};Database={1};User Id={2};Password={3};", server, database, userid, password));
        }

        public static object FindObject(string field, string tableName, string condition, string groupBy, string having, string orderBy, SqlConnection conn = null, SqlTransaction trans = null)
        {
            if (conn == null)
                conn = SQLDBConnection;

            var flag = false;
            var cmd = (SqlCommand)null;
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                    flag = true;
                }
                cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.Transaction = trans;
                cmd.CommandText = string.Format("SELECT {0}", (object)field);
                if (!string.IsNullOrEmpty(tableName))
                    cmd.CommandText = string.Format("{0} FROM {1}", (object)cmd.CommandText, (object)tableName);
                if (!string.IsNullOrEmpty(condition))
                    cmd.CommandText = string.Format("{0} WHERE {1}", (object)cmd.CommandText, (object)condition);
                if (!string.IsNullOrEmpty(groupBy))
                    cmd.CommandText = string.Format("{0} GROUP BY {1}", (object)cmd.CommandText, (object)groupBy);
                if (!string.IsNullOrEmpty(having))
                    cmd.CommandText = string.Format("{0} HAVING {1}", (object)cmd.CommandText, (object)condition);
                if (!string.IsNullOrEmpty(orderBy))
                    cmd.CommandText = string.Format("{0} ORDER BY {1}", (object)cmd.CommandText, (object)orderBy);
                return cmd.ExecuteScalar();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (flag)
                    conn.Close();
                if (cmd != null)
                    cmd.Dispose();
            }
        }

        public static object ExecuteNonQuery(string cmdText, SqlConnection conn = null, SqlTransaction trans = null)
        {
            if (conn == null)
                conn = SQLDBConnection;

            var flag = false;
            var cmd = (SqlCommand)null;
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                    flag = true;
                }
                cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.Transaction = trans;
                cmd.CommandText = cmdText;
                return cmd.ExecuteScalar();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (flag)
                    conn.Close();
                if (cmd != null)
                    cmd.Dispose();
            }
        }

        public static DataTable SQLGetObjects(string cmdText, SqlConnection conn)
        {
            if (SQLDBConnection == null)
            {
                var config = Utility.GetConfig();
                Utility.ConnectSQL(config["SQL:Server"], config["SQL:Database"], config["SQL:UserID"], config["SQL:Password"]);
            }

            if (conn == null)
                conn = SQLDBConnection;

            var flag = false;
            var cmd = (SqlCommand)null;
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                    flag = true;
                }

                cmd = new SqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = cmdText;

                var readersearch = cmd.ExecuteReader();
                var result = new DataTable();

                result.Load(readersearch);
                readersearch.Close();

                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (flag)
                    conn.Close();
                if (cmd != null)
                    cmd.Dispose();
            }
        }
        #endregion

        #region MySQLFunction
        public static void ConnectMySQL(string server, string database, string userid, string password)
        {
            if (MySQLDBConnection == null)
                MySQLDBConnection = new MySqlConnection(string.Format("Server={0};Database={1};User Id={2};Password={3};", server, database, userid, password));
        }

        public static object ExecuteNonQueryMySQL(string cmdText, MySqlConnection conn = null, MySqlTransaction trans = null)
        {
            if (conn == null)
                conn = MySQLDBConnection;

            var flag = false;
            var cmd = (MySqlCommand)null;
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                    flag = true;
                }
                cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.Transaction = trans;
                cmd.CommandText = cmdText;
                return cmd.ExecuteScalar();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (flag)
                    conn.Close();
                if (cmd != null)
                    cmd.Dispose();
            }
        }

        public static DataTable MySqlGetObjects(string cmdText, MySqlConnection conn)
        {
            if (conn == null)
                conn = MySQLDBConnection;

            var flag = false;
            var cmd = (MySqlCommand)null;
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                    flag = true;
                }

                cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.CommandText = cmdText;

                var readersearch = cmd.ExecuteReader();
                var result = new DataTable();

                result.Load(readersearch);
                readersearch.Close();

                return result;
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (flag)
                    conn.Close();
                if (cmd != null)
                    cmd.Dispose();
            }
        }

        public static object MySqlFindObject(string field, string tableName, string condition, string groupBy, string having, string orderBy, MySqlConnection conn = null, MySqlTransaction trans = null)
        {
            if (conn == null)
                conn = MySQLDBConnection;

            var flag = false;
            var cmd = (MySqlCommand)null;
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                    flag = true;
                }
                cmd = new MySqlCommand();
                cmd.Connection = conn;
                cmd.Transaction = trans;
                cmd.CommandText = string.Format("SELECT {0}", (object)field);
                if (!string.IsNullOrEmpty(tableName))
                    cmd.CommandText = string.Format("{0} FROM {1}", (object)cmd.CommandText, (object)tableName);
                if (!string.IsNullOrEmpty(condition))
                    cmd.CommandText = string.Format("{0} WHERE {1}", (object)cmd.CommandText, (object)condition);
                if (!string.IsNullOrEmpty(groupBy))
                    cmd.CommandText = string.Format("{0} GROUP BY {1}", (object)cmd.CommandText, (object)groupBy);
                if (!string.IsNullOrEmpty(having))
                    cmd.CommandText = string.Format("{0} HAVING {1}", (object)cmd.CommandText, (object)condition);
                if (!string.IsNullOrEmpty(orderBy))
                    cmd.CommandText = string.Format("{0} ORDER BY {1}", (object)cmd.CommandText, (object)orderBy);
                return cmd.ExecuteScalar();
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (flag)
                    conn.Close();
                if (cmd != null)
                    cmd.Dispose();
            }
        }

        #endregion
    }
}
