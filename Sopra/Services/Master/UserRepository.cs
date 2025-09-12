using System;
using System.Diagnostics;
//using System.Linq;
using System.Threading;
using System.Data;

//using SOPRA.Utility;
//using SOPRA.Models;
using Dapper;

using Sopra.Helpers;
using Sopra.Entities;
namespace Sopra.Services
{
    public class UserRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync User....");
            

            var tableUser = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_users WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.TransactionSyncDate), Utility.MySQLDBConnection);
            if (tableUser != null)
            {
                Trace.WriteLine($"Start Sync User {tableUser.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableUser.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync UserID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE Users WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var User = new User();

                            User.RefID = Convert.ToInt64(row["id"]);
                            User.Email = row["username"] == DBNull.Value ? null : row["username"].ToString();
                            User.Password = row["password"] == DBNull.Value ? null : row["password"].ToString();
                            User.Name = row["name"] == DBNull.Value ? null : row["name"].ToString();
                            User.RoleID = row["mit_privileges_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_privileges_id"]);
                            User.FirstName = row["first_name"] == DBNull.Value ? null : row["first_name"].ToString();
                            User.LastName = row["last_name"] == DBNull.Value ? null : row["last_name"].ToString();
                            User.CustomersID = row["customer_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["customer_id"]);
                            User.PublicationName = row["publication_name"] == DBNull.Value ? null : row["publication_name"].ToString();
                            User.PublicationPIC = row["publication_pic"] == DBNull.Value ? null : row["publication_pic"].ToString();
                            User.PublicationProvincesID = row["publication_province_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["publication_province_id"]);
                            User.PublicationDistrictsID = row["publication_district_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["publication_district_id"]);
                            User.PublicationRegenciesID = row["publication_regency_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["publication_regency_id"]);
                            User.PublicationPhone1 = row["publication_phone_1"] == DBNull.Value ? null : row["publication_phone_1"].ToString();
                            User.PublicationPhone2 = row["publication_phone_2"] == DBNull.Value ? null : row["publication_phone_2"].ToString();
                            User.CustNum = row["cust_num"] == DBNull.Value ? null : row["cust_num"].ToString();
                            User.CustomerGroup = row["customer_group"] == DBNull.Value ? 0 : Convert.ToInt64(row["customer_group"]);
                            User.LastLoginDates = row["last_login_dates"] == DBNull.Value ? null : Convert.ToDateTime(row["last_login_dates"]);
                            User.CompanyID = row["company_id"] == DBNull.Value ? "0" : Convert.ToString(row["company_id"]);
                            User.Subdomain = row["subdomain"] == DBNull.Value ? null : row["subdomain"].ToString();
                            UserRepository.Insert(User);
                            

                            Trace.WriteLine($"Success syncing UserID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing UserID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization User completed successfully.");
                    //Utility.DeleteDiffData("mit_users", "Users");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync User....");
        }

        public static Int64 Insert(User User)
        {
            try
            {
                var sql = @"INSERT INTO [Users] (RefID, Email, Password, Name, RoleID, FirstName, LastName, CustomersID, PublicationName, PublicationPIC, PublicationProvincesID, PublicationDistrictsID, PublicationRegenciesID, PublicationPhone1, PublicationPhone2, CustNum, CustomerGroup, LastLoginDates, CompanyID, Subdomain, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @Email, @Password, @Name, @RoleID, @FirstName, @LastName, @CustomersID, @PublicationName, @PublicationPIC, @PublicationProvincesID, @PublicationDistrictsID, @PublicationRegenciesID, @PublicationPhone1, @PublicationPhone2, @CustNum, @CustomerGroup, @LastLoginDates, @CompanyID, @Subdomain, @DateIn, @UserIn, @IsDeleted) ";
                User.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, User);

                ///888 from integration
                //SystemLogRepository.Insert("User", User.ID, 888, User);
                return User.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(User User)
        {
            try
            {
                var sql = @"UPDATE [Users] SET
                            Email = @Email,
                            Password = @Password,
                            Name = @Name,
                            RoleID = @RoleID,
                            FirstName = @FirstName,
                            LastName = @LastName,
                            CustomersID = @CustomersID,
                            PublicationName = @PublicationName,
                            PublicationPIC = @PublicationPIC,
                            PublicationProvincesID = @PublicationProvincesID,
                            PublicationDistrictsID = @PublicationDistrictsID,
                            PublicationRegenciesID = @PublicationRegenciesID,
                            PublicationPhone1 = @PublicationPhone1,
                            PublicationPhone2 = @PublicationPhone2,
                            CustNum = @CustNum,
                            CustomerGroup = @CustomerGroup,
                            LastLoginDates = @LastLoginDates,
                            CompanyID = @CompanyID,
                            Subdomain = @Subdomain,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, User, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("User", User.ID, 888, User);
                return User.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
