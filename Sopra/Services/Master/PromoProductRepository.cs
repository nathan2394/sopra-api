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

namespace Sopra.Services.Master
{
    public class PromoProductRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync PromoProduct....");

            var tablePromoProduct = Utility.MySqlGetObjects(string.Format(
                @"
                    select 
                        mmp.id ,mmp.created_at ,mmp.updated_at ,0 as promo_jumbo_id ,promo_mix_id ,
                        mp.mit_calculators_id as products_id ,mp2.mit_closures_id as accs1_id ,mp3.mit_closures_id as accs2_id ,
                        mmp.price,mmp.price2,mmp.price3 
                    from mit_mix_product mmp 
                        join mit_products mp on mp.id = mmp.products_id 
                        join mit_promo_mix mpm on mmp.promo_mix_id = mpm.id
                        left join mit_products mp2 on mp2.id = mmp.accs1_id  
                        left join mit_products mp3 on mp3.id = mmp.accs2_id 
                    where (mpm.updated_at is null AND mpm.created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR mpm.updated_at > '{0:yyyy-MM-dd HH:mm:ss}'
                    order by id desc", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tablePromoProduct != null)
            {
                Trace.WriteLine($"Start Sync Promo Quantity {tablePromoProduct.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tablePromoProduct.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync PromoProductID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var condition = Convert.ToInt64(row["promo_jumbo_id"]) == 0 ? "AND PromoJumboId = 0" : "AND PromoMixId = 0";
                            Utility.ExecuteNonQuery(string.Format("DELETE PromoProducts WHERE RefID = {0} AND IsDeleted = 0 {1}", row["ID"], condition));

                            var PromoProduct = new PromoProduct();

                            PromoProduct.RefID = row["id"] == DBNull.Value ? 0 : Convert.ToInt64(row["id"]);
                            PromoProduct.PromoJumboId = row["promo_jumbo_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["promo_jumbo_id"]);
                            PromoProduct.PromoMixId = row["promo_mix_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["promo_mix_id"]);
                            PromoProduct.ProductsId = row["products_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["products_id"]);
                            PromoProduct.Accs1Id = row["accs1_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["accs1_id"]);
                            PromoProduct.Accs2Id = row["accs2_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["accs2_id"]);
                            PromoProduct.Price = row["price"] == DBNull.Value ? 0 : Convert.ToDecimal(row["price"]);
                            PromoProduct.Price2 = row["price2"] == DBNull.Value ? 0 : Convert.ToDecimal(row["price2"]);
                            PromoProduct.Price3 = row["price3"] == DBNull.Value ? 0 : Convert.ToDecimal(row["price3"]);
                            PromoProductRepository.Insert(PromoProduct);

                            Trace.WriteLine($"Success syncing PromoProductID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing PromoProductID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Promo Product completed successfully.");
                    //Utility.DeleteDiffData("mit_jumbo_product", "PromoProducts", "AND PromoMixId = 0");
                    //Utility.DeleteDiffData("mit_mix_product", "PromoProducts", "AND PromoJumboId = 0");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync PromoProduct....");
        }

        public static Int64 Insert(PromoProduct PromoProduct)
        {
            try
            {
                var sql = @"INSERT INTO [PromoProducts] (RefID ,PromoJumboId ,PromoMixId ,ProductsId ,Accs1Id ,Accs2Id ,Price,Price2,Price3, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID ,@PromoJumboId ,@PromoMixId ,@ProductsId ,@Accs1Id ,@Accs2Id,@Price,@Price2,@Price3,@DateIn, @UserIn, @IsDeleted) ";
                PromoProduct.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, PromoProduct);

                ///888 from integration
                //SystemLogRepository.Insert("PromoProduct", PromoProduct.ID, 888, PromoProduct);
                return PromoProduct.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(PromoProduct PromoProduct)
        {
            try
            {
                var sql = @"UPDATE [PromoProducts] SET
                            PromoJumboId = @PromoJumboId ,
                            PromoMixId = @PromoMixId ,
                            ProductsId = @ProductsId,
                            Accs1Id = @Accs1Id,
                            Accs2Id = @Accs2Id,
                            Price = @Price,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, PromoProduct, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("PromoProduct", PromoProduct.ID, 888, PromoProduct);
                return PromoProduct.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
