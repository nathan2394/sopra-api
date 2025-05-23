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
    public class ImageRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Image....");

            //GET DATA FROM MYSQL
            var tableImageCalculator = Utility.MySqlGetObjects(string.Format(
                                        @"select *,'Calculators' as type from mit_images WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'   
                                            union all 
                                            select *,'Closures' as type from mit_closures_images WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'
                                            union all 
                                            select *,'Lids' as type from mit_lids_images WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'
                                            union all 
                                            select *,'Thermos' as type from mit_thermos_images WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableImageCalculator != null)
            {
                Trace.WriteLine($"Start Sync Image {tableImageCalculator.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableImageCalculator.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync ImageID : {Convert.ToInt64(row["ID"])}");
                            
                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Images WHERE RefID = {0} AND Type = '{1}' AND IsDeleted = 0", row["ID"], row["type"]));
                        
                            var Image = new Image();

                            Image.RefID = Convert.ToInt64(row["id"]);
                            Image.ProductImage = row["image"].ToString();
                            Image.Type = row["type"].ToString();
                            Image.ObjectID = row["mit_calculators_id"] == DBNull.Value ? 0 :  Convert.ToInt64(row["mit_calculators_id"]);
                            Image.UserIn = 888;
                            ImageRepository.Insert(Image);

                            Trace.WriteLine($"Success syncing ImageID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing ImageID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            //Utility.DeleteDiffData("mit_images","images","AND Type = 'Calculators'");
            //Utility.DeleteDiffData("mit_closures_images","images","AND Type = 'Closures'");
            //Utility.DeleteDiffData("mit_lids_images","images","AND Type = 'Lids'");
            //Utility.DeleteDiffData("mit_thermos_images","images","AND Type = 'Thermos'");

            Trace.WriteLine("Finished Sync Image....");
        }

        public static Int64 Insert(Image Image)
        {
            try
            {
                var sql = @"INSERT INTO [Images]  (RefID, ProductImage, Type, ObjectID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES  (@RefID, @ProductImage, @Type, @ObjectID, @DateIn, @UserIn, @IsDeleted) ";
                Image.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Image);

                ///888 from integration
                //SystemLogRepository.Insert("Image", Image.ID, 888, Image);
                return Image.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
