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
    public class PromoOrderBottleDetailRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync PromoOrderBottleDetail....");


            var tablePromoOrderBottleDetail = Utility.MySqlGetObjects(string.Format(
                @"
                    select id ,created_at ,updated_at ,mit_orders_bottle_id ,promo_jumbo_id,
                        product_jumbo_id ,0 as promo_mix_id ,0 as product_mix_id ,qty_box ,qty,
                        product_price ,amount ,flag_promo ,0 as outstanding ,0 as qty_acc ,notes 
                    from 
                        mit_orders_bottle_detail_jumbo 
                    WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') 
                        OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'
                         
                    union all 
                    
                    select id ,created_at ,updated_at ,mit_orders_bottle_id ,0 as promo_jumbo_id,
                        0 as product_jumbo_id ,promo_mix_id ,product_mix_id ,qty_box ,qty ,product_price,
                        amount ,flag_promo ,0 as outstanding ,qty_acc ,notes 
                    from mit_orders_bottle_detail_mix 
                    WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') 
                        OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);

            if (tablePromoOrderBottleDetail != null)
            {
                Trace.WriteLine($"Start Sync Promo Quantity{tablePromoOrderBottleDetail.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tablePromoOrderBottleDetail.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync PromoOrderBottleDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            var condition = Convert.ToInt64(row["promo_jumbo_id"]) == 0 ? "AND PromoJumboId = 0" : "AND PromoMixId = 0";
                            Utility.ExecuteNonQuery(string.Format("DELETE PromoOrderBottleDetails WHERE RefID = {0} AND IsDeleted = 0 {1}", row["ID"], condition));

                            var PromoOrderBottleDetail = new PromoOrderBottleDetail();

                            PromoOrderBottleDetail.RefID = Convert.ToInt64(row["id"]);
                            PromoOrderBottleDetail.OrdersId = Convert.ToInt64(row["mit_orders_bottle_id"]);
                            PromoOrderBottleDetail.PromoJumboId = row["promo_jumbo_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["promo_jumbo_id"]);
                            PromoOrderBottleDetail.PromoMixId = row["promo_mix_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["promo_mix_id"]);
                            PromoOrderBottleDetail.ProductJumboId = row["product_jumbo_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["product_jumbo_id"]);
                            PromoOrderBottleDetail.ProductMixId = row["product_mix_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["product_mix_id"]);
                            PromoOrderBottleDetail.QtyBox = Convert.ToInt32(row["qty_box"]);
                            PromoOrderBottleDetail.Qty = Convert.ToInt32(row["qty"]);
                            PromoOrderBottleDetail.ProductPrice = Convert.ToDecimal(row["product_price"]);
                            PromoOrderBottleDetail.Amount = Convert.ToDecimal(row["amount"]);
                            PromoOrderBottleDetail.FlagPromo = Convert.ToInt32(row["flag_promo"]);
                            PromoOrderBottleDetail.Outstanding = Convert.ToInt32(row["outstanding"]);
                            PromoOrderBottleDetail.QtyAcc = Convert.ToInt32(row["qty_acc"]);
                            PromoOrderBottleDetail.Notes = row["notes"] == DBNull.Value ? null : row["notes"].ToString();
                            PromoOrderBottleDetailRepository.Insert(PromoOrderBottleDetail);

                            Trace.WriteLine($"Success syncing PromoOrderBottleDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing PromoOrderBottleDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Promo Order Bottle Detail completed successfully.");
                    //Utility.DeleteDiffData("mit_orders_bottle_detail_jumbo", "PromoOrderBottleDetails", "AND PromoMixId = 0");
                    //Utility.DeleteDiffData("mit_orders_bottle_detail_mix", "PromoOrderBottleDetails", "AND PromoJumboId = 0");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync PromoOrderBottleDetail....");
        }

        public static Int64 Insert(PromoOrderBottleDetail PromoOrderBottleDetail)
        {
            try
            {
                var sql = @"INSERT INTO [PromoOrderBottleDetails] (RefID ,OrdersId ,PromoJumboId ,PromoMixId ,ProductJumboId ,ProductMixId ,QtyBox ,Qty ,ProductPrice ,Amount ,FlagPromo ,Outstanding ,QtyAcc ,Notes , DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID ,@OrdersId ,@PromoJumboId ,@PromoMixId ,@ProductJumboId ,@ProductMixId ,@QtyBox ,@Qty ,@ProductPrice ,@Amount ,@FlagPromo ,@Outstanding ,@QtyAcc ,@Notes ,@DateIn, @UserIn, @IsDeleted) ";
                PromoOrderBottleDetail.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, PromoOrderBottleDetail);

                ///888 from integration
                //SystemLogRepository.Insert("PromoOrderBottleDetail", PromoOrderBottleDetail.ID, 888, PromoOrderBottleDetail);
                return PromoOrderBottleDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(PromoOrderBottleDetail PromoOrderBottleDetail)
        {
            try
            {
                var sql = @"UPDATE [PromoOrderBottleDetails] SET
                            RefID = @RefID ,
                            OrdersId = @OrdersId ,
                            PromoJumboId = @PromoJumboId ,
                            PromoMixId = @PromoMixId ,
                            ProductJumboId = @ProductJumboId ,
                            ProductMixId = @ProductMixId ,
                            QtyBox = @QtyBox ,
                            Qty = @Qty ,
                            ProductPrice = @ProductPrice ,
                            Amount = @Amount ,
                            FlagPromo = @FlagPromo ,
                            Outstanding = @Outstanding ,
                            QtyAcc = @QtyAcc ,
                            Notes = @Notes ,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, PromoOrderBottleDetail, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("PromoOrderBottleDetail", PromoOrderBottleDetail.ID, 888, PromoOrderBottleDetail);
                return PromoOrderBottleDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
