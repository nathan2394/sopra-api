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
    public class LidRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Lid....");

            ///GET DATA FROM MYSQL
            var tableLids = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_lids WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableLids != null)
            {
                Trace.WriteLine($"Start Sync Lid {tableLids.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableLids.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync LidID : {Convert.ToInt64(row["ID"])}");
                            
                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Lids WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Lid = new Lid();

                            Lid.RefID = Convert.ToInt64(row["id"]);
                            Lid.Name = row["name"].ToString();
                            Lid.NewProd = Convert.ToInt64(row["new_prod"]);
                            Lid.FavProd = Convert.ToInt64(row["fav_prod"]);
                            Lid.RimsID = Convert.ToInt64(row["mit_rims_id"]);
                            Lid.MaterialsID = Convert.ToInt64(row["mit_materials_id"]);
                            Lid.ColorsID = Convert.ToInt64(row["mit_colors_id"]);
                            Lid.Price = Convert.ToDecimal(row["price"]);
                            Lid.Length = Convert.ToDecimal(row["length"]);
                            Lid.Width = Convert.ToDecimal(row["width"]);
                            Lid.Height = Convert.ToDecimal(row["height"]);
                            Lid.Weight = Convert.ToDecimal(row["weight"]);
                            Lid.Qty = Convert.ToDecimal(row["qty"]);
                            Lid.Code = row["wms_code"] == DBNull.Value ? null : row["wms_code"].ToString();
                            Lid.Image = row["image"].ToString();
                            Lid.Status = Convert.ToInt64(row["status"]);
                            Lid.Note = row["notes"] == DBNull.Value ? null : Convert.ToString(row["notes"]);
                            Lid.WmsCode = row["wms_code"] == DBNull.Value ? null : Convert.ToString(row["wms_code"]);
                            Lid.UserIn = 888;

                            LidRepository.Insert(Lid);

                            Trace.WriteLine($"Success syncing LidID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing LidID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Lid completed successfully.");

                    //Utility.DeleteDiffData("mit_lids","Lids");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            Trace.WriteLine("Finished Sync Lid....");
        }

        public static Int64 Insert(Lid Lid)
        {
            try
            {
                var sql = @"INSERT INTO [Lids] (
                               [RefID]
                               ,[Name]
                               ,[WmsCode]
                               ,[NewProd]
                               ,[FavProd]
                               ,[RimsID]
                               ,[MaterialsID]
                               ,[ColorsID]
                               ,[Price]
                               ,[Length]
                               ,[Width]
                               ,[Height]
                               ,[Weight]
                               ,[Qty]
                               ,[Code]
                               ,[Note]
                               ,[Image]
                               ,[Status]
                               ,[DateIn]
                               ,[UserIn]
                               ,[IsDeleted]) 
                            OUTPUT INSERTED.ID
                            VALUES (
                               @RefID
                               ,@Name
                               ,@WmsCode
                               ,@NewProd
                               ,@FavProd
                               ,@RimsID
                               ,@MaterialsID
                               ,@ColorsID
                               ,@Price
                               ,@Length
                               ,@Width
                               ,@Height
                               ,@Weight
                               ,@Qty
                               ,@Code
                               ,@Note
                               ,@Image
                               ,@Status
                               ,@DateIn
                               ,@UserIn
                               ,@IsDeleted) ";
                Lid.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Lid);

                ///888 from integration
                //SystemLogRepository.Insert("Lid", Lid.ID, 888, Lid);
                return Lid.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Lid Lid)
        {
            try
            {
                var sql = @"UPDATE [Lids] SET
                            [RefID] = @RefID,
                            [Name] = @Name,
                            [WmsCode] = @WmsCode,
                            [NewProd] = @NewProd,
                            [FavProd] = @FavProd,
                            [RimsID] = @RimsID,
                            [MaterialsID] = @MaterialsID,
                            [ColorsID] = @ColorsID,
                            [Price] = @Price,
                            [Length] = @Length,
                            [Width] = @Width,
                            [Height] = @Height,
                            [Weight] = @Weight,
                            [Qty] = @Qty,
                            [Code] = @Code,
                            [Note] = @Note,
                            [Image] = @Image,
                            [Status] = @Status,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Lid, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Lid", Lid.ID, 888, Lid);
                return Lid.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
