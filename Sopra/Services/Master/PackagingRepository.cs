using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Data;

//using SOPRA.Utility;
//using SOPRA.Models;
using Dapper;
using System.Threading;
using Sopra.Helpers;
using Sopra.Entities;

namespace SOPRA.Services
{
    public class PackagingRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Packaging....");

            //var deleteData = Utility.SQLDBConnection.QueryFirstOrDefault<Packaging>(string.Format("TRUNCATE TABLE Packagings"), transaction: null);

            ///GET DATA FROM MYSQL
            var tablePackaging = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_packagings WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tablePackaging != null)
            {
                Trace.WriteLine($"Start Sync Packaging {tablePackaging.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tablePackaging.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync PackagingID : {Convert.ToInt64(row["ID"])}");
                            Utility.ExecuteNonQuery(string.Format("DELETE Packagings WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Packaging = new Packaging();

                            Packaging.RefID = Convert.ToInt64(row["id"]);
                            Packaging.Name = row["name"].ToString();
                            Packaging.Length = Convert.ToDecimal(row["length"]);
                            Packaging.Width = Convert.ToDecimal(row["width"]);
                            Packaging.Thickness = Convert.ToDecimal(row["thickness"]);
                            Packaging.Height = Convert.ToDecimal(row["height"]);
                            Packaging.Tipe = Convert.ToInt32(row["tipe"]);
                            PackagingRepository.Insert(Packaging);

                            Trace.WriteLine($"Success syncing PackagingID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing PackagingID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }
                    Trace.WriteLine("Finished Sync Packaging....");

                    //Utility.DeleteDiffData("mit_packagings", "Packagings");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");

                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            
        }

        public static Int64 Insert(Packaging Packaging)
        {
            try
            {
                var sql = @"INSERT INTO [Packagings]  (Tipe,RefID, Name, Length, Width, Thickness, Height, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES  (@Tipe,@RefID, @Name, @Length, @Width, @Thickness, @Height, @DateIn, @UserIn, @IsDeleted) ";
                Packaging.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Packaging);

                ///888 from integration
                //SystemLogRepository.Insert("Packaging", Packaging.ID, 888, Packaging);
                return Packaging.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Packaging Packaging)
        {
            try
            {
                var sql = @"UPDATE [Packagings] SET
                            Name = @Name,
                            Tipe = @Tipe,
                            Length = @Length,
                            Width = @Width,
                            Thickness = @Thickness,
                            Height = @Height,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Packaging, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Packaging", Packaging.ID, 888, Packaging);
                return Packaging.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
