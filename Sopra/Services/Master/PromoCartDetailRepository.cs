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
    public class PromoCartDetailRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync PromoCartDetail....");
            

            var tablePromoCartDetail = Utility.MySqlGetObjects(string.Format("select id ,created_at ,updated_at ,carts_id ,promo_jumbo_id ,product_jumbo_id ,0 as promo_mix_id ,0 as product_mix_id ,qty_box ,qty ,product_price ,amount ,is_checkout ,flag_promo from mit_carts_detail_jumbo WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}' union all select id ,created_at ,updated_at ,carts_id ,0 as promo_jumbo_id ,0 as product_jumbo_id ,promo_mix_id ,product_mix_id ,qty_box ,qty ,product_price ,amount,is_checkout,flag_promo from mit_carts_detail_mix WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tablePromoCartDetail != null)
            {
                Trace.WriteLine($"Start Sync Promo Quantity{tablePromoCartDetail.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tablePromoCartDetail.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync PromoCartDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            var condition = Convert.ToInt64(row["promo_jumbo_id"]) == 0 ? "AND PromoJumboId = 0" : "AND PromoMixId = 0";
                            Utility.ExecuteNonQuery(string.Format("DELETE PromoCartDetails WHERE RefID = {0} AND IsDeleted = 0 {1}", row["ID"], condition));

                            var PromoCartDetail = new PromoCartDetail();

                            PromoCartDetail.RefID = Convert.ToInt64(row["id"]);
                            PromoCartDetail.CartsId = row["carts_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["carts_id"]);
                            PromoCartDetail.PromoJumboId = row["promo_jumbo_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["promo_jumbo_id"]);
                            PromoCartDetail.PromoMixId = row["promo_mix_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["promo_mix_id"]);
                            PromoCartDetail.ProductJumboId = row["product_jumbo_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["product_jumbo_id"]);
                            PromoCartDetail.ProductMixId = row["product_mix_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["product_mix_id"]);
                            PromoCartDetail.QtyBox = Convert.ToInt32(row["qty_box"]);
                            PromoCartDetail.Qty = Convert.ToInt32(row["qty"]);
                            PromoCartDetail.ProductPrice = Convert.ToDecimal(row["product_price"]);
                            PromoCartDetail.Amount = Convert.ToDecimal(row["amount"]);
                            PromoCartDetail.FlagPromo = row["flag_promo"] == DBNull.Value ? 0 : Convert.ToInt32(row["flag_promo"]);
                            PromoCartDetail.IsCheckout = Convert.ToInt32(row["is_checkout"]);
                            PromoCartDetailRepository.Insert(PromoCartDetail);

                            Trace.WriteLine($"Success syncing PromoCartDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing PromoCartDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Promo Cart Detail completed successfully.");
                    //Utility.DeleteDiffData("mit_carts_detail_jumbo", "PromoCartDetails", "AND PromoMixId = 0");
                    //Utility.DeleteDiffData("mit_carts_detail_mix", "PromoCartDetails", "AND PromoJumboId = 0");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync PromoCartDetail....");
        }

        public static Int64 Insert(PromoCartDetail PromoCartDetail)
        {
            try
            {
                var sql = @"INSERT INTO [PromoCartDetails] (RefID ,CartsId ,PromoJumboId ,PromoMixId ,ProductJumboId ,ProductMixId ,QtyBox ,Qty ,ProductPrice ,Amount ,FlagPromo ,IsCheckout,DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID ,@CartsId ,@PromoJumboId ,@PromoMixId ,@ProductJumboId ,@ProductMixId ,@QtyBox ,@Qty ,@ProductPrice ,@Amount ,@FlagPromo ,@IsCheckout,@DateIn, @UserIn, @IsDeleted) ";
                PromoCartDetail.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, PromoCartDetail);

                ///888 from integration
                //SystemLogRepository.Insert("PromoCartDetail", PromoCartDetail.ID, 888, PromoCartDetail);
                return PromoCartDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(PromoCartDetail PromoCartDetail)
        {
            try
            {
                var sql = @"UPDATE [PromoCartDetails] SET
                            RefID = @RefID ,
                            CartsId = @CartsId ,
                            PromoJumboId = @PromoJumboId ,
                            PromoMixId = @PromoMixId ,
                            ProductJumboId = @ProductJumboId ,
                            ProductMixId = @ProductMixId ,
                            QtyBox = @QtyBox ,
                            Qty = @Qty ,
                            ProductPrice = @ProductPrice ,
                            Amount = @Amount ,
                            FlagPromo = @FlagPromo ,
                            IsCheckout = @IsCheckout,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, PromoCartDetail, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("PromoCartDetail", PromoCartDetail.ID, 888, PromoCartDetail);
                return PromoCartDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
