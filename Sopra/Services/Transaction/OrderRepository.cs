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
    public class OrderRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Order....");
            var deleteData = Utility.SQLDBConnection.QueryFirstOrDefault<Order>(string.Format("TRUNCATE TABLE Orders"), transaction: null);
            //GET DATA FROM MYSQL 
            var tableOrderBottle = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_orders_bottle where month(created_at) between month(curdate()) - 3 and month(curdate()) and year(created_at) = year(curdate())", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableOrderBottle != null)
            {
                Trace.WriteLine($"Start Sync Order Bottle {tableOrderBottle.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableOrderBottle.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync OrderID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var obj = Utility.SQLDBConnection.QueryFirstOrDefault<Order>(string.Format("SELECT * FROM Orders WHERE RefID = {0} AND IsDeleted = 0 ", row["ID"]), transaction: null);

                            var Order = new Order();
                            if (obj != null) Order = obj;

                            Order.RefID = Convert.ToInt64(row["id"]);
                            Order.OrderNo = row["voucher_no"] == DBNull.Value ? null : row["voucher_no"].ToString();
                            Order.TransDate = row["transdate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["transdate"]);
                            Order.CustomersID = row["customer_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["customer_id"]);
                            Order.ReferenceNo = row["reference_no"] == DBNull.Value ? null : row["reference_no"].ToString();
                            Order.Other = row["others"] == DBNull.Value ? null : row["others"].ToString();
                            Order.Amount = row["amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["amount"]);
                            Order.Status = row["status"] == DBNull.Value ? null : row["status"].ToString();
                            Order.VouchersID = row["voucher"] == DBNull.Value ? null : Convert.ToString(row["voucher"]);
                            Order.Disc1 = row["disc1"] == DBNull.Value ? 0 : Convert.ToDecimal(row["disc1"]);
                            Order.Disc1Value = row["disc1_value"] == DBNull.Value ? null : Convert.ToDecimal(row["disc1_value"]);
                            Order.Disc2 = row["disc2"] == DBNull.Value ? 0 : Convert.ToDecimal(row["disc2"]);
                            Order.Disc2Value = row["disc2_value"] == DBNull.Value ? 0 : Convert.ToDecimal(row["disc2_value"]);
                            Order.Sfee = row["sfee"] == DBNull.Value ? 0 : Convert.ToDecimal(row["sfee"]);
                            Order.DPP = row["dpp"] == DBNull.Value ? 0 : Convert.ToDecimal(row["dpp"]);
                            Order.TAX = row["tax"] == DBNull.Value ? 0 : Convert.ToDecimal(row["tax"]);
                            Order.TaxValue = row["tax_value"] == DBNull.Value ? 0 : Convert.ToDecimal(row["tax_value"]);
                            Order.Total = row["total"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total"]);
                            Order.Departure = row["departure"] == DBNull.Value ? null : row["departure"].ToString();
                            Order.Arrival = row["arrival"] == DBNull.Value ? null : row["arrival"].ToString();
                            Order.WarehouseID = row["warehouse_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["warehouse_id"]);
                            Order.CountriesID = row["country_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["country_id"]);
                            Order.ProvincesID = row["province_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["province_id"]);
                            Order.RegenciesID = row["regency_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["regency_id"]);
                            Order.DistrictsID = row["district_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["district_id"]);
                            Order.Address = row["address"] == DBNull.Value ? null : row["address"].ToString();
                            Order.PostalCode = row["postal_code"] == DBNull.Value ? null : row["postal_code"].ToString();
                            Order.TransportsID = row["transport_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["transport_id"]);
                            Order.TotalTransport = row["total_transport"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_transport"]);
                            Order.TotalTransportCapacity = row["total_transport_capacity"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_transport_capacity"]);
                            Order.TotalOrderCapacity = row["total_order_capacity"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_order_capacity"]);
                            Order.TotalOrderWeight = row["total_order_weight"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_order_weight"]);
                            Order.TotalTransportCost = row["total_transport_cost"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_transport_cost"]);
                            Order.RemainingCapacity = row["remaining_capacity"] == DBNull.Value ? 0 : Convert.ToDecimal(row["remaining_capacity"]);
                            Order.ReasonsID = row["reason_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["reason_id"]);
                            Order.OrderStatus = row["order_status"] == DBNull.Value ? null : row["order_status"].ToString();
                            Order.ExpeditionsID = row["jenis_expedisi"] == DBNull.Value ? 0 : Convert.ToInt64(row["jenis_expedisi"]);
                            Order.BiayaPickup = row["biaya_pickup"] == DBNull.Value ? 0 : Convert.ToDecimal(row["biaya_pickup"]);
                            Order.CheckInvoice = row["check_invoice"] == DBNull.Value ? 0 : Convert.ToInt64(row["check_invoice"]);
                            Order.InvoicedDate = row["invoiced_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["invoiced_date"]);
                            Order.TotalReguler = row["total_reguler"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_reguler"]);
                            Order.TotalJumbo = row["total_jumbo"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_jumbo"]);
                            Order.TotalMix = row["total_mix"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_mix"]);
                            Order.TotalNewPromo = row["total_new_promo"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_new_promo"]);
                            Order.TotalSupersale = row["biaya_pickup"] == DBNull.Value ? 0 : Convert.ToDecimal(row["biaya_pickup"]);
                            Order.ValidTime = row["valid_time"] == DBNull.Value ? null : row["valid_time"].ToString();
                            Order.DealerTier = row["dealer_tier"] == DBNull.Value ? null : row["dealer_tier"].ToString();
                            Order.DeliveryStatus = row["delivery_status"] == DBNull.Value ? 0 : Convert.ToInt64(row["delivery_status"]);
                            Order.PartialDeliveryStatus = row["partial_delivery_status"] == DBNull.Value ? 0 : Convert.ToInt64(row["partial_delivery_status"]);
                            Order.PaymentTerm = row["payment_term"] == DBNull.Value ? null : row["payment_term"].ToString();
                            Order.Validity = row["validity"] == DBNull.Value ? null : row["validity"].ToString();
                            Order.IsVirtual = row["is_virtual"] == DBNull.Value ? 0 : Convert.ToInt64(row["is_virtual"]);
                            Order.VirtualAccount = row["virtual_account"] == DBNull.Value ? null : row["virtual_account"].ToString();
                            Order.BanksID = row["mit_banks_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_banks_id"]);
                            Order.ShipmentNum = row["shipment_num"] == DBNull.Value ? null : row["shipment_num"].ToString();
                            Order.PaidDate = row["paid_date"] == DBNull.Value ? null : Convert.ToDateTime(row["paid_date"]);
                            Order.RecreateOrderStatus = row["recreate_order_status"] == DBNull.Value ? 0 : Convert.ToInt64(row["recreate_order_status"]);
                            Order.CompaniesID = row["company_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["company_id"]);
                            Order.AmountTotal = row["amount_total"] == DBNull.Value ? 0 : Convert.ToDecimal(row["amount_total"]);
                            //Order.FutureDateStatus = row["future_date_status"] == DBNull.Value ? 0 : Convert.ToInt64(row["future_date_status"]);
                            Order.FutureDateStatus = 0;
                            Order.ChangeExpeditionStatus = row["change_expedition_status"] == DBNull.Value ? 0 : Convert.ToInt64(row["change_expedition_status"]);
                            Order.ChangetruckStatus = row["change_truck_status"] == DBNull.Value ? 0 : Convert.ToInt64(row["change_truck_status"]);
                            Order.SubscriptionCount = row["subscription_count"] == DBNull.Value ? 0 : Convert.ToInt64(row["subscription_count"]);
                            Order.SubscriptionStatus = row["subscription_status"] == DBNull.Value ? 0 : Convert.ToInt64(row["subscription_status"]);
                            Order.SubscriptionDate = row["subscription_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["subscription_date"]);
                            Order.Username = row["username"] == DBNull.Value ? null : row["username"].ToString();
                            Order.UsernameCancel = row["username_cancel"] == DBNull.Value ? null : row["username_cancel"].ToString();
                            Order.SessionID = row["session_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["session_id"]);
                            Order.SessionDate = row["session_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["session_date"]);

                            if (obj == null)
                            {
                                // INSERT
                                Order.UserIn = 888;
                                OrderRepository.Insert(Order);
                            }
                            else
                            {
                                // UPDATE
                                // MAPPING KE CLASS Order
                                Order.UserUp = 888;
                                OrderRepository.Update(Order);
                            }

                            Trace.WriteLine($"Success syncing OrderID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing OrderID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Order Bottle completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            var tableOrderThermo = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_orders_thermo where month(created_at) between month(curdate()) - 3 and month(curdate()) and year(created_at) = year(curdate())", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableOrderThermo != null)
            {
                Trace.WriteLine($"Start Sync Order Bottle {tableOrderThermo.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableOrderThermo.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync OrderID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            var obj = Utility.SQLDBConnection.QueryFirstOrDefault<Order>(string.Format("SELECT * FROM Orders WHERE RefID = {0} AND IsDeleted = 0 ", row["ID"]), transaction: null);

                            var Order = new Order();
                            if (obj != null) Order = obj;

                            Order.RefID = Convert.ToInt64(row["id"]);
                            Order.OrderNo = row["voucher_no"] == DBNull.Value ? null : row["voucher_no"].ToString();
                            Order.TransDate = row["transdate"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["transdate"]);
                            Order.CustomersID = row["customer_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["customer_id"]);
                            Order.ReferenceNo = row["reference_no"] == DBNull.Value ? null : row["reference_no"].ToString();
                            Order.Other = row["others"] == DBNull.Value ? null : row["others"].ToString();
                            Order.Amount = row["amount"] == DBNull.Value ? 0 : Convert.ToDecimal(row["amount"]);
                            Order.Status = row["status"] == DBNull.Value ? null : row["status"].ToString();
                            Order.VouchersID = row["voucher"] == DBNull.Value ? null : Convert.ToString(row["voucher"]);
                            Order.Disc1 = row["disc1"] == DBNull.Value ? 0 : Convert.ToDecimal(row["disc1"]);
                            Order.Disc1Value = row["disc1_value"] == DBNull.Value ? 0 : Convert.ToDecimal(row["disc1_value"]);
                            Order.Disc2 = row["disc2"] == DBNull.Value ? 0 : Convert.ToDecimal(row["disc2"]);
                            Order.Disc2Value = row["disc2_value"] == DBNull.Value ? 0 : Convert.ToDecimal(row["disc2_value"]);
                            Order.Sfee = row["sfee"] == DBNull.Value ? 0 : Convert.ToDecimal(row["sfee"]);
                            Order.DPP = row["dpp"] == DBNull.Value ? 0 : Convert.ToDecimal(row["dpp"]);
                            Order.TAX = row["tax"] == DBNull.Value ? 0 : Convert.ToDecimal(row["tax"]);
                            Order.TaxValue = row["tax_value"] == DBNull.Value ? 0 : Convert.ToDecimal(row["tax_value"]);
                            Order.Total = row["total"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total"]);
                            Order.Departure = row["departure"] == DBNull.Value ? null : row["departure"].ToString();
                            Order.Arrival = row["arrival"] == DBNull.Value ? null : row["arrival"].ToString();
                            Order.WarehouseID = row["warehouse_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["warehouse_id"]);
                            Order.CountriesID = row["country_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["country_id"]);
                            Order.ProvincesID = row["province_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["province_id"]);
                            Order.RegenciesID = row["regency_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["regency_id"]);
                            Order.DistrictsID = row["district_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["district_id"]);
                            Order.Address = row["address"] == DBNull.Value ? null : row["address"].ToString();
                            Order.PostalCode = row["postal_code"] == DBNull.Value ? null : row["postal_code"].ToString();
                            Order.TransportsID = row["transport_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["transport_id"]);
                            Order.TotalTransport = row["total_transport"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_transport"]);
                            Order.TotalTransportCapacity = row["total_transport_capacity"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_transport_capacity"]);
                            Order.TotalOrderCapacity = row["total_order_capacity"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_order_capacity"]);
                            Order.TotalOrderWeight = row["total_order_weight"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_order_weight"]);
                            Order.TotalTransportCost = row["total_transport_cost"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_transport_cost"]);
                            Order.RemainingCapacity = row["remaining_capacity"] == DBNull.Value ? 0 : Convert.ToDecimal(row["remaining_capacity"]);
                            Order.ReasonsID = row["reason_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["reason_id"]);
                            Order.OrderStatus = row["order_status"] == DBNull.Value ? null : row["order_status"].ToString();
                            Order.ExpeditionsID = row["jenis_expedisi"] == DBNull.Value ? 0 : Convert.ToInt64(row["jenis_expedisi"]);
                            Order.BiayaPickup = row["biaya_pickup"] == DBNull.Value ? 0 : Convert.ToDecimal(row["biaya_pickup"]);
                            Order.CheckInvoice = row["check_invoice"] == DBNull.Value ? 0 : Convert.ToInt64(row["check_invoice"]);
                            Order.InvoicedDate = row["invoiced_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["invoiced_date"]);
                            Order.TotalReguler = 0;
                            Order.TotalJumbo = 0;
                            Order.TotalMix = 0;
                            Order.TotalNewPromo = 0;
                            Order.TotalSupersale = 0;
                            Order.ValidTime = null;
                            Order.DealerTier = row["dealer_tier"] == DBNull.Value ? null : row["dealer_tier"].ToString();
                            Order.DeliveryStatus = 0;
                            Order.PartialDeliveryStatus = 0;
                            Order.PaymentTerm = row["payment_term"] == DBNull.Value ? null : row["payment_term"].ToString();
                            Order.Validity = row["validity"] == DBNull.Value ? null : row["validity"].ToString();
                            Order.IsVirtual = row["is_virtual"] == DBNull.Value ? 0 : Convert.ToInt64(row["is_virtual"]);
                            Order.VirtualAccount = row["virtual_account"] == DBNull.Value ? null : row["virtual_account"].ToString();
                            Order.BanksID = row["mit_banks_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_banks_id"]);
                            Order.ShipmentNum = null;
                            Order.PaidDate = null;
                            Order.RecreateOrderStatus = row["recreate_order_status"] == DBNull.Value ? 0 : Convert.ToInt64(row["recreate_order_status"]);
                            Order.CompaniesID = row["company_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["company_id"]);
                            Order.AmountTotal = 0;
                            //Order.FutureDateStatus = row["future_date_status"] == DBNull.Value ? 0 : Convert.ToInt64(row["future_date_status"]);
                            Order.FutureDateStatus = 0;
                            Order.ChangeExpeditionStatus = 0;
                            Order.ChangetruckStatus = 0;
                            Order.SubscriptionCount = 0;
                            Order.SubscriptionStatus = 0;
                            Order.SubscriptionDate = null;
                            Order.Username = row["username"] == DBNull.Value ? null : row["username"].ToString();
                            Order.UsernameCancel = row["username_cancel"] == DBNull.Value ? null : row["username_cancel"].ToString();
                            Order.SessionID = 0;
                            Order.SessionDate = null;

                            if (obj == null)
                            {
                                // INSERT
                                Order.UserIn = 888;
                                OrderRepository.Insert(Order);
                            }
                            else
                            {
                                // UPDATE
                                // MAPPING KE CLASS Order
                                Order.UserUp = 888;
                                OrderRepository.Update(Order);
                            }

                            Trace.WriteLine($"Success syncing OrderID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing OrderID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Order Bottle completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Order....");
        }

        public static Int64 Insert(Order Order)
        {
            try
            {
                var sql = @"INSERT INTO [Orders] (RefID, OrderNo, TransDate, CustomersID, ReferenceNo, Other, Amount, Status, VouchersID,  Disc1, Disc1Value, Disc2, Disc2Value, Sfee, DPP, TAX, TaxValue, Total, Departure, Arrival, WarehouseID, CountriesID, ProvincesID, RegenciesID, DistrictsID, Address, PostalCode, TransportsID, TotalTransport, TotalTransportCapacity, TotalOrderCapacity, TotalOrderWeight, TotalTransportCost, RemainingCapacity, ReasonsID, OrderStatus, ExpeditionsID, BiayaPickup, CheckInvoice,  InvoicedDate, TotalReguler, TotalJumbo, TotalMix, TotalNewPromo, TotalSupersale, ValidTime, DealerTier, DeliveryStatus, PartialDeliveryStatus, PaymentTerm, Validity, IsVirtual, VirtualAccount, BanksID, ShipmentNum, PaidDate, RecreateOrderStatus, CompaniesID, AmountTotal, FutureDateStatus, ChangeExpeditionStatus, ChangetruckStatus, SubscriptionCount, SubscriptionStatus, SubscriptionDate, Username, UsernameCancel, SessionID, SessionDate, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @OrderNo, @TransDate, @CustomersID, @ReferenceNo, @Other, @Amount, @Status, @VouchersID,  @Disc1, @Disc1Value, @Disc2, @Disc2Value, @Sfee, @DPP, @TAX, @TaxValue, @Total, @Departure, @Arrival, @WarehouseID, @CountriesID, @ProvincesID, @RegenciesID, @DistrictsID, @Address, @PostalCode, @TransportsID, @TotalTransport, @TotalTransportCapacity, @TotalOrderCapacity, @TotalOrderWeight, @TotalTransportCost, @RemainingCapacity, @ReasonsID, @OrderStatus, @ExpeditionsID, @BiayaPickup, @CheckInvoice, @InvoicedDate, @TotalReguler,@TotalJumbo, @TotalMix, @TotalNewPromo, @TotalSupersale, @ValidTime, @DealerTier, @DeliveryStatus, @PartialDeliveryStatus, @PaymentTerm, @Validity, @IsVirtual, @VirtualAccount, @BanksID, @ShipmentNum, @PaidDate, @RecreateOrderStatus, @CompaniesID, @AmountTotal, @FutureDateStatus, @ChangeExpeditionStatus, @ChangetruckStatus, @SubscriptionCount, @SubscriptionStatus, @SubscriptionDate, @Username, @UsernameCancel, @SessionID, @SessionDate, @DateIn, @UserIn, @IsDeleted) ";
                Order.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Order);

                ///888 from integration
                //SystemLogRepository.Insert("Order", Order.ID, 888, Order);
                return Order.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Order Order)
        {
            try
            {
                var sql = @"UPDATE [Orders] SET
                            OrderNo = @OrderNo,
                            TransDate = @TransDate,
                            CustomersID = @CustomersID,
                            ReferenceNo = @ReferenceNo,
                            Other = @Other,
                            Amount = @Amount,
                            Status = @Status,
                            VouchersID = @VouchersID,
                            Disc1 = @Disc1,
                            Disc1Value = @Disc1Value,
                            Disc2 = @Disc2,
                            Disc2Value = @Disc2Value,
                            Sfee = @Sfee,
                            DPP = @DPP,
                            TAX = @TAX,
                            TaxValue = @TaxValue,
                            Total = @Total,
                            Departure = @Departure,
                            Arrival = @Arrival,
                            WarehouseID = @WarehouseID,
                            CountriesID = @CountriesID,
                            ProvincesID = @ProvincesID,
                            RegenciesID = @RegenciesID,
                            DistrictsID = @DistrictsID,
                            Address = @Address,
                            PostalCode = @PostalCode,
                            TransportsID = @TransportsID,
                            TotalTransport = @TotalTransport,
                            TotalTransportCapacity = @TotalTransportCapacity,
                            TotalOrderCapacity = @TotalOrderCapacity,
                            TotalOrderWeight = @TotalOrderWeight,
                            TotalTransportCost = @TotalTransportCost,
                            RemainingCapacity = @RemainingCapacity,
                            ReasonsID = @ReasonsID,
                            ExpeditionsID = @ExpeditionsID,
                            BiayaPickup = @BiayaPickup,
                            CheckInvoice = @CheckInvoice,
                            InvoicedDate = @InvoicedDate,
                            TotalReguler = @TotalReguler,
                            TotalJumbo = @TotalJumbo,
                            TotalMix = @TotalMix,
                            TotalNewPromo = @TotalNewPromo,
                            TotalSupersale = @TotalSupersale,
                            ValidTime = @ValidTime,
                            DealerTier = @DealerTier,
                            DeliveryStatus = @DeliveryStatus,
                            PartialDeliveryStatus = @PartialDeliveryStatus,
                            PaymentTerm = @PaymentTerm,
                            Validity = @Validity,
                            IsVirtual = @IsVirtual,
                            VirtualAccount = @VirtualAccount,
                            BanksID = @BanksID,
                            ShipmentNum = @ShipmentNum,
                            PaidDate = @PaidDate,
                            RecreateOrderStatus = @RecreateOrderStatus,
                            CompaniesID = @CompaniesID,
                            AmountTotal = @AmountTotal,
                            FutureDateStatus = @FutureDateStatus,
                            ChangeExpeditionStatus = @ChangeExpeditionStatus,
                            ChangetruckStatus = @ChangetruckStatus,
                            SubscriptionCount = @SubscriptionCount,
                            SubscriptionStatus = @SubscriptionStatus,
                            SubscriptionDate = @SubscriptionDate,
                            Username = @Username,
                            UsernameCancel = @UsernameCancel,
                            SessionID = @SessionID,
                            SessionDate = @SessionDate,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Order, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Order", Order.ID, 888, Order);
                return Order.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
