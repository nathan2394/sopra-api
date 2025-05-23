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
    public class OrderDetailRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync OrderDetail....");
            var deleteData = Utility.SQLDBConnection.QueryFirstOrDefault<OrderDetail>(string.Format("TRUNCATE TABLE OrderDetails"), transaction: null);
            //GET DATA FROM MYSQL Reguler 
            //var tableOrderDetail = Utility.MySqlGetObjects(string.Format("SELECT mobd.* FROM mit_alt_neck WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            var tableOrderDetailReguler = Utility.MySqlGetObjects(string.Format("SELECT mobd.*,mp.mit_calculators_id as product_id  FROM mit_orders_bottle_detail mobd join mit_products mp on mp.id = mobd.products_id where month(mobd.created_at) between month(curdate()) - 3 and month(curdate()) and year(mobd.created_at) = year(curdate())", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableOrderDetailReguler != null)
            {
                Trace.WriteLine($"Start Sync Order Detail Reguler {tableOrderDetailReguler.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableOrderDetailReguler.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync OrderDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var obj = Utility.SQLDBConnection.QueryFirstOrDefault<OrderDetail>(string.Format("SELECT * FROM OrderDetails WHERE RefID = {0} AND Type = 'Reguler' AND IsDeleted = 0 ", row["ID"]), transaction: null);

                            var OrderDetail = new OrderDetail();
                            if (obj != null) OrderDetail = obj;

                            OrderDetail.RefID = Convert.ToInt64(row["id"]);
                            OrderDetail.OrdersID = row["mit_orders_bottle_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_orders_bottle_id"]);
                            OrderDetail.ObjectID = row["product_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["product_id"]);
                            OrderDetail.ObjectType = "bottle";
                            OrderDetail.PromosID = 0;
                            OrderDetail.Type = "Reguler";
                            OrderDetail.QtyBox = row["qty_box"] == DBNull.Value ? 0 : Convert.ToDecimal(row["qty_box"]);
                            OrderDetail.Qty = row["quantity"] == DBNull.Value ? 0 : Convert.ToDecimal(row["quantity"]);
                            OrderDetail.ProductPrice = row["products_price"] == DBNull.Value ? 0 : Convert.ToDecimal(row["products_price"]);
                            OrderDetail.Amount = row["amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["amount"]);
                            OrderDetail.FlagPromo = false;
                            //OrderDetail.Outstanding = row["outstanding"] == DBNull.Value ? 0 : Convert.ToDecimal(row["outstanding"]);
                            OrderDetail.Outstanding = 0;
                            OrderDetail.Note = row["notes"] == DBNull.Value ? null : row["notes"].ToString();
                            OrderDetail.CompaniesID = 1;

                            if (obj == null)
                            {
                                // INSERT
                                OrderDetail.UserIn = 888;
                                OrderDetailRepository.Insert(OrderDetail);
                            }
                            else
                            {
                                // UPDATE
                                // MAPPING KE CLASS OrderDetail
                                OrderDetail.UserUp = 888;
                                OrderDetailRepository.Update(OrderDetail);
                            }

                            Trace.WriteLine($"Success syncing OrderDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing OrderDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Order Detail Reguler completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            //GET ORDER DETAIL ACC
            var tableOrderDetailAcc = Utility.MySqlGetObjects(string.Format("SELECT mobd.*,mp.mit_closures_id as product_id  FROM mit_orders_bottle_detail_accesories mobd join mit_products mp on mp.id = mobd.products_id where month(mobd.created_at) between month(curdate()) - 3 and month(curdate()) and year(mobd.created_at) = year(curdate())", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableOrderDetailAcc != null)
            {
                Trace.WriteLine($"Start Sync Order Detail Acc {tableOrderDetailAcc.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableOrderDetailAcc.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync OrderDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var obj = Utility.SQLDBConnection.QueryFirstOrDefault<OrderDetail>(string.Format("SELECT * FROM OrderDetails WHERE RefID = {0} AND Type = 'Reguler' AND IsDeleted = 0 ", row["ID"]), transaction: null);

                            var OrderDetail = new OrderDetail();
                            if (obj != null) OrderDetail = obj;

                            OrderDetail.RefID = Convert.ToInt64(row["id"]);
                            OrderDetail.OrdersID = row["mit_orders_bottle_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_orders_bottle_id"]);
                            OrderDetail.ObjectID = row["product_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["product_id"]);
                            OrderDetail.ObjectType = "closure";
                            OrderDetail.PromosID = 0;
                            OrderDetail.Type = "Reguler";
                            OrderDetail.QtyBox = row["qty_box"] == DBNull.Value ? 0 : Convert.ToDecimal(row["qty_box"]);
                            OrderDetail.Qty = row["quantity"] == DBNull.Value ? 0 : Convert.ToDecimal(row["quantity"]);
                            OrderDetail.ProductPrice = row["products_price"] == DBNull.Value ? 0 : Convert.ToDecimal(row["products_price"]);
                            OrderDetail.Amount = row["amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["amount"]);
                            OrderDetail.FlagPromo = false;
                            //OrderDetail.Outstanding = row["outstanding"] == DBNull.Value ? 0 : Convert.ToDecimal(row["outstanding"]);
                            OrderDetail.Outstanding = 0;
                            OrderDetail.Note = null;
                            OrderDetail.CompaniesID = 1;
                            OrderDetail.ParentID = row["bottle_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["bottle_id"]);

                            if (obj == null)
                            {
                                // INSERT
                                OrderDetail.UserIn = 888;
                                OrderDetailRepository.Insert(OrderDetail);
                            }
                            else
                            {
                                // UPDATE
                                // MAPPING KE CLASS OrderDetail
                                OrderDetail.UserUp = 888;
                                OrderDetailRepository.Update(OrderDetail);
                            }

                            Trace.WriteLine($"Success syncing OrderDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing OrderDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Order Detail Reguler completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            //GET DATA FROM MYSQL Jumbo
            var tableOrderDetailJumbo = Utility.MySqlGetObjects(string.Format("select modj.*, mp.mit_calculators_id as product_id   from mit_orders_detail_jumbo modj  join mit_jumbo_product mjp on mjp.id = modj.product_jumbo_id  join mit_products mp on mp.id = mjp.products_id  where month(modj.created_at) between month(curdate()) - 3 and month(curdate()) and year(modj.created_at) = year(curdate())", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableOrderDetailJumbo != null)
            {
                Trace.WriteLine($"Start Sync Order Detail Jumbo {tableOrderDetailJumbo.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableOrderDetailJumbo.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync OrderDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var obj = Utility.SQLDBConnection.QueryFirstOrDefault<OrderDetail>(string.Format("SELECT * FROM OrderDetails WHERE RefID = {0} AND Type = 'Jumbo' AND IsDeleted = 0 ", row["ID"]), transaction: null);

                            var OrderDetail = new OrderDetail();
                            if (obj != null) OrderDetail = obj;

                            OrderDetail.RefID = Convert.ToInt64(row["id"]);
                            OrderDetail.OrdersID = row["mit_orders_bottle_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_orders_bottle_id"]);
                            OrderDetail.ObjectID = row["product_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["product_id"]);
                            OrderDetail.ObjectType = "bottle";
                            OrderDetail.PromosID = row["promo_jumbo_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["promo_jumbo_id"]);
                            OrderDetail.Type = "Jumbo";
                            OrderDetail.QtyBox = row["qty_box"] == DBNull.Value ? 0 : Convert.ToDecimal(row["qty_box"]);
                            OrderDetail.Qty = row["qty"] == DBNull.Value ? 0 : Convert.ToDecimal(row["qty"]);
                            OrderDetail.ProductPrice = row["product_price"] == DBNull.Value ? 0 : Convert.ToDecimal(row["product_price"]);
                            OrderDetail.Amount = row["amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["amount"]);
                            OrderDetail.FlagPromo = row["flag_promo"] == DBNull.Value ? false : Convert.ToBoolean(row["flag_promo"]);
                            //OrderDetail.Outstanding = row["outstanding"] == DBNull.Value ? 0 : Convert.ToDecimal(row["outstanding"]);
                            OrderDetail.Outstanding =  0;
                            OrderDetail.Note = row["notes"] == DBNull.Value ? null : row["notes"].ToString();
                            OrderDetail.CompaniesID = 1;

                            if (obj == null)
                            {
                                // INSERT
                                OrderDetail.UserIn = 888;
                                OrderDetailRepository.Insert(OrderDetail);
                            }
                            else
                            {
                                // UPDATE
                                // MAPPING KE CLASS OrderDetail
                                OrderDetail.UserUp = 888;
                                OrderDetailRepository.Update(OrderDetail);
                            }

                            Trace.WriteLine($"Success syncing OrderDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing OrderDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Order Detail Reguler completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            //GET DATA FROM MYSQL MIX
            var tableOrderDetailMix = Utility.MySqlGetObjects(string.Format("select mobdm.*, mp.mit_calculators_id as product_id from mit_orders_bottle_detail_mix mobdm join mit_products mp on mp.id = mobdm.product_mix_id  where month(mobdm.created_at) between month(curdate()) - 3 and month(curdate()) and year(mobdm.created_at) = year(curdate())", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableOrderDetailMix != null)
            {
                Trace.WriteLine($"Start Sync Order Detail Jumbo {tableOrderDetailMix.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableOrderDetailMix.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync OrderDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var obj = Utility.SQLDBConnection.QueryFirstOrDefault<OrderDetail>(string.Format("SELECT * FROM OrderDetails WHERE RefID = {0} AND Type = 'Jumbo' AND IsDeleted = 0 ", row["ID"]), transaction: null);

                            var OrderDetail = new OrderDetail();
                            if (obj != null) OrderDetail = obj;

                            OrderDetail.RefID = Convert.ToInt64(row["id"]);
                            OrderDetail.OrdersID = row["mit_orders_bottle_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_orders_bottle_id"]);
                            OrderDetail.ObjectID = row["product_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["product_id"]);
                            OrderDetail.ObjectType = "bottle";
                            OrderDetail.PromosID = row["promo_mix_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["promo_jumbo_id"]);
                            OrderDetail.Type = "Mix";
                            OrderDetail.QtyBox = row["qty_box"] == DBNull.Value ? 0 : Convert.ToDecimal(row["qty_box"]);
                            OrderDetail.Qty = row["qty"] == DBNull.Value ? 0 : Convert.ToDecimal(row["qty"]);
                            OrderDetail.ProductPrice = row["product_price"] == DBNull.Value ? 0 : Convert.ToDecimal(row["product_price"]);
                            OrderDetail.Amount = row["amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["amount"]);
                            OrderDetail.FlagPromo = row["flag_promo"] == DBNull.Value ? false : Convert.ToBoolean(row["flag_promo"]);
                            //OrderDetail.Outstanding = row["outstanding"] == DBNull.Value ? 0 : Convert.ToDecimal(row["outstanding"]);
                            OrderDetail.Outstanding = 0;
                            OrderDetail.Note = row["notes"] == DBNull.Value ? null : row["notes"].ToString();
                            OrderDetail.CompaniesID = 1;

                            if (obj == null)
                            {
                                // INSERT
                                OrderDetail.UserIn = 888;
                                OrderDetailRepository.Insert(OrderDetail);
                            }
                            else
                            {
                                // UPDATE
                                // MAPPING KE CLASS OrderDetail
                                OrderDetail.UserUp = 888;
                                OrderDetailRepository.Update(OrderDetail);
                            }

                            Trace.WriteLine($"Success syncing OrderDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing OrderDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Order Detail Reguler completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            //GET THERMO DETAIL
            var tableThermoDetailReguler = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_orders_thermo_detail where month(created_at) between month(curdate()) - 3 and month(curdate()) and year(created_at) = year(curdate())", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableThermoDetailReguler != null)
            {
                Trace.WriteLine($"Start Sync Order Detail Reguler {tableThermoDetailReguler.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableThermoDetailReguler.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync OrderDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var obj = Utility.SQLDBConnection.QueryFirstOrDefault<OrderDetail>(string.Format("SELECT * FROM OrderDetails WHERE RefID = {0} AND Type = 'Reguler' AND IsDeleted = 0 ", row["ID"]), transaction: null);

                            var OrderDetail = new OrderDetail();
                            if (obj != null) OrderDetail = obj;

                            OrderDetail.RefID = Convert.ToInt64(row["id"]);
                            OrderDetail.OrdersID = row["mit_orders_thermo_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_orders_bottle_id"]);
                            OrderDetail.ObjectID = row["products_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["products_id"]);
                            OrderDetail.ObjectType = "thermo";
                            OrderDetail.PromosID = 0;
                            OrderDetail.Type = "Reguler";
                            OrderDetail.QtyBox = row["qty_box"] == DBNull.Value ? 0 : Convert.ToDecimal(row["qty_box"]);
                            OrderDetail.Qty = row["quantity"] == DBNull.Value ? 0 : Convert.ToDecimal(row["quantity"]);
                            OrderDetail.ProductPrice = row["products_price"] == DBNull.Value ? 0 : Convert.ToDecimal(row["products_price"]);
                            OrderDetail.Amount = row["amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["amount"]);
                            OrderDetail.FlagPromo = false;
                            //OrderDetail.Outstanding = row["outstanding"] == DBNull.Value ? 0 : Convert.ToDecimal(row["outstanding"]);
                            OrderDetail.Outstanding = 0;
                            OrderDetail.Note = row["notes"] == DBNull.Value ? null : row["notes"].ToString();
                            OrderDetail.CompaniesID = 1;

                            if (obj == null)
                            {
                                // INSERT
                                OrderDetail.UserIn = 888;
                                OrderDetailRepository.Insert(OrderDetail);
                            }
                            else
                            {
                                // UPDATE
                                // MAPPING KE CLASS OrderDetail
                                OrderDetail.UserUp = 888;
                                OrderDetailRepository.Update(OrderDetail);
                            }

                            Trace.WriteLine($"Success syncing OrderDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing OrderDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Order Detail Reguler completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            //GET THERMO DETAIL ACC
            var tableThermoDetailAcc = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_orders_thermo_detail_accesories where month(created_at) between month(curdate()) - 3 and month(curdate()) and year(created_at) = year(curdate())", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableThermoDetailAcc != null)
            {
                Trace.WriteLine($"Start Sync Order Detail Acc {tableThermoDetailAcc.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableThermoDetailAcc.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync OrderDetailID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var obj = Utility.SQLDBConnection.QueryFirstOrDefault<OrderDetail>(string.Format("SELECT * FROM OrderDetails WHERE RefID = {0} AND Type = 'Reguler' AND IsDeleted = 0 ", row["ID"]), transaction: null);

                            var OrderDetail = new OrderDetail();
                            if (obj != null) OrderDetail = obj;

                            OrderDetail.RefID = Convert.ToInt64(row["id"]);
                            OrderDetail.OrdersID = row["mit_orders_thermo_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_orders_bottle_id"]);
                            OrderDetail.ObjectID = row["products_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["products_id"]);
                            OrderDetail.ObjectType = "lid";
                            OrderDetail.PromosID = 0;
                            OrderDetail.Type = "Reguler";
                            OrderDetail.QtyBox = row["qty_box"] == DBNull.Value ? 0 : Convert.ToDecimal(row["qty_box"]);
                            OrderDetail.Qty = row["quantity"] == DBNull.Value ? 0 : Convert.ToDecimal(row["quantity"]);
                            OrderDetail.ProductPrice = row["products_price"] == DBNull.Value ? 0 : Convert.ToDecimal(row["products_price"]);
                            OrderDetail.Amount = row["amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["amount"]);
                            OrderDetail.FlagPromo = false;
                            //OrderDetail.Outstanding = row["outstanding"] == DBNull.Value ? 0 : Convert.ToDecimal(row["outstanding"]);
                            OrderDetail.Outstanding = 0;
                            OrderDetail.Note = null;
                            OrderDetail.CompaniesID = 1;
                            OrderDetail.ParentID = row["thermo_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["thermo_id"]);

                            if (obj == null)
                            {
                                // INSERT
                                OrderDetail.UserIn = 888;
                                OrderDetailRepository.Insert(OrderDetail);
                            }
                            else
                            {
                                // UPDATE
                                // MAPPING KE CLASS OrderDetail
                                OrderDetail.UserUp = 888;
                                OrderDetailRepository.Update(OrderDetail);
                            }

                            Trace.WriteLine($"Success syncing OrderDetailID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing OrderDetailID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Order Detail Reguler completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync OrderDetail....");
        }

        public static Int64 Insert(OrderDetail OrderDetail)
        {
            try
            {
                var sql = @"INSERT INTO [OrderDetails] (RefID, OrdersID, ObjectID, ObjectType, PromosID, Type, QtyBox, Qty, ProductPrice, Amount, FlagPromo, Outstanding, Note, CompaniesID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @OrdersID, @ObjectID, @ObjectType, @PromosID, @Type, @QtyBox, @Qty, @ProductPrice, @Amount, @FlagPromo, @Outstanding, @Note, @CompaniesID, @DateIn, @UserIn, @IsDeleted) ";
                OrderDetail.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, OrderDetail);

                ///888 from integration
                //SystemLogRepository.Insert("OrderDetail", OrderDetail.ID, 888, OrderDetail);
                return OrderDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(OrderDetail OrderDetail)
        {
            try
            {
                var sql = @"UPDATE [OrderDetails] SET
                            OrdersID = @OrdersID,
                            ObjectID = @ObjectID,
                            ObjectType = @ObjectType,
                            PromosID = @PromosID,
                            Type = @Type,
                            QtyBox = @QtyBox,
                            Qty = @Qty,
                            ProductPrice = @ProductPrice,
                            Amount = @Amount,
                            FlagPromo = @FlagPromo,
                            Outstanding = @Outstanding,
                            Note = @Note,
                            CompaniesID = @CompaniesID,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, OrderDetail, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("OrderDetail", OrderDetail.ID, 888, OrderDetail);
                return OrderDetail.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
