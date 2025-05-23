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
    public class BannerRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Banner....");

            ///GET DATA FROM MYSQL
            var tableBanner = Utility.MySqlGetObjects(string.Format(
                                @"SELECT distinct ms.*, TRIM(BOTH ', ' FROM GROUP_CONCAT(mpck.name SEPARATOR ', ')) AS concatenated_names, 
                                    TRIM(BOTH ', ' FROM GROUP_CONCAT(mpck.name_en SEPARATOR ', ')) AS concatenated_names_en 
                                FROM mit_settings ms  
                                    left join mit_product_categories mpc on ms.link = mpc.id  
                                    left join mit_products_categories mpc2 on mpc2.mit_product_categories_id = mpc.id  
                                    left join mit_products_categories_key mpck on mpck.id = mpc2.mit_products_categories_key_id  
                                WHERE 
                                    ms.name LIKE '%slider%' 
                                group by ms.id order by ms.name", Utility.SyncDate), Utility.MySQLDBConnection);
            
            if (tableBanner != null)
            {
                Trace.WriteLine($"Start Sync Banner {tableBanner.Rows.Count} Data(s)....");
                try
                {
                    // DELETE DATA IF EXISTS
                    Utility.ExecuteNonQuery("TRUNCATE TABLE Banners");

                    ///LOOPING DATA
                    foreach (DataRow row in tableBanner.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync BannerID : {Convert.ToInt64(row["ID"])}");

                            var Banner = new Banner();
                            Banner.RefID = Convert.ToInt64(row["id"]);
                            Banner.Name = row["name"].ToString();
                            Banner.Image = row["content"].ToString();
                            Banner.nameInd= row["concatenated_names"].ToString();
                            Banner.nameEng = row["concatenated_names_en"].ToString();
                            Banner.UserIn = 888;
                            BannerRepository.Insert(Banner);

                            Trace.WriteLine($"Success syncing BannerID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing BannerID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            Trace.WriteLine("Finished Sync Banner....");
        }

        public static Int64 Insert(Banner Banner)
        {
            try
            {
                var sql = @"INSERT INTO [Banners]  (nameInd,nameEng,RefID, Name, Image, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES  (@nameInd,@nameEng,@RefID, @Name, @Image, @DateIn, @UserIn, @IsDeleted) ";
                Banner.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Banner);

                ///888 from integration
                //SystemLogRepository.Insert("Banner", Banner.ID, 888, Banner);
                return Banner.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
