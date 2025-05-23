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
    public class ClosureRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Closure....");
            
            ///GET DATA FROM MYSQL
            var tableClosure = Utility.MySqlGetObjects(string.Format("SELECT mit_closures.*, mit_products.wms_code AS wmscode, mit_products.stock FROM mit_closures JOIN mit_products ON mit_products.mit_closures_id = mit_closures.id WHERE (mit_closures.updated_at is null AND mit_closures.created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR mit_closures.updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableClosure != null)
            {
                Trace.WriteLine($"Start Sync Closure {tableClosure.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableClosure.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync ClosureID : {Convert.ToInt64(row["ID"])}");

                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Closures WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Closure = new Closure();

                            Closure.RefID = Convert.ToInt64(row["id"]);
                            Closure.Name = row["name"].ToString();
                            Closure.Price = Convert.ToDecimal(row["price"]);
                            Closure.Diameter = Convert.ToDecimal(row["diameter"]);
                            Closure.Height = Convert.ToDecimal(row["height"]);
                            Closure.Weight = Convert.ToDecimal(row["weight"]);
                            Closure.NecksID = row["mit_necks_id"] == DBNull.Value || string.IsNullOrEmpty(row["mit_necks_id"].ToString()) ? 0 : Convert.ToInt64(row["mit_necks_id"]);
                            Closure.Status = row["status"] == DBNull.Value || string.IsNullOrEmpty(row["status"].ToString()) ? 0 : Convert.ToInt64(row["status"]);
                            Closure.ColorsID = row["color"] == DBNull.Value || string.IsNullOrEmpty(row["color"].ToString()) ? 0 : Convert.ToInt64(row["color"]);
                            Closure.QtyPack = row["qty_pack"]  == DBNull.Value ? 0 : Convert.ToDecimal(row["qty_pack"]);
                            Closure.Code = row["wmscode"]  == DBNull.Value ? null : row["wmscode"].ToString();
                            Closure.Stock = row["stock"]  == DBNull.Value ? 0 :  Convert.ToDecimal(row["stock"]);
                            Closure.Image = row["image"] == DBNull.Value ? null :  row["image"].ToString();
                            Closure.WmsCode = row["wmscode"] == DBNull.Value ? null : Convert.ToString(row["wmscode"]);
                            Closure.ClosureType = row["closure_type"] == DBNull.Value ? null : Convert.ToInt32(row["closure_type"]);
                            Closure.Ranking = row["ranking"] == DBNull.Value ? null : Convert.ToInt32(row["ranking"]);
                            Closure.UserIn = 888;
                            ClosureRepository.Insert(Closure);

                            Trace.WriteLine($"Success syncing ClosureID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing ClosureID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Closure completed successfully.");
                    
                    //Utility.DeleteDiffData("mit_closures","Closures");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");

                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            Trace.WriteLine("Finished Sync Closure....");
        }

        public static Int64 Insert(Closure Closure)
        {
            try
            {
                var sql = @"INSERT INTO [Closures] (ClosureType,WmsCode,RefID, Name, Price, Diameter, Height, Weight, NecksID, Status, ColorsID, QtyPack, Code, Stock, Image, DateIn, UserIn, IsDeleted,Ranking) 
                            OUTPUT INSERTED.ID
                            VALUES (@ClosureType,@WmsCode,@RefID, @Name, @Price, @Diameter, @Height, @Weight, @NecksID, @Status, @ColorsID, @QtyPack, @Code, @Stock, @Image, @DateIn, @UserIn, @IsDeleted,@Ranking) ";
                Closure.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Closure);

                ///888 from integration
                //SystemLogRepository.Insert("Closure", Closure.ID, 888, Closure);
                return Closure.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Closure Closure)
        {
            try
            {
                var sql = @"UPDATE [Closures] SET
                            Name = @Name,
                            ClosureType = @ClosureType,
                            WmsCode = @WmsCode,
                            Price = @Price,
                            Diameter = @Diameter,
                            Height = @Height,
                            Weight = @Weight,
                            NecksID = @NecksID,
                            Status = @Status,
                            ColorsID = @ColorsID,
                            QtyPack = @QtyPack,
                            Code = @Code,
                            Stock = @Stock,
                            Image = @Image,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp,
                            Ranking = @Ranking
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Closure, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Closure", Closure.ID, 888, Closure);
                return Closure.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
