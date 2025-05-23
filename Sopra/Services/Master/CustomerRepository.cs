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
    public class CustomerRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Customer....");
            

            var tableCustomer = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_customers where last_sync > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableCustomer != null)
            {
                Trace.WriteLine($"Start Sync Customer {tableCustomer.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableCustomer.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync CustomerID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE Customers WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));
                            //var obj = Utility.SQLDBConnection.QueryFirstOrDefault<Customer>(string.Format("SELECT * FROM Countries WHERE RefID = {0} AND IsDeleted = 0 ", row["ID"]), transaction: null);

                            var Customer = new Customer();

                            Customer.RefID = Convert.ToInt64(row["id"]);
                            Customer.Name = row["name"] == DBNull.Value ? null : row["name"].ToString();
                            Customer.CustomerNumber = row["customer_number"] == DBNull.Value ? null : row["customer_number"].ToString();
                            Customer.DeliveryAddress = row["delivery_address"] == DBNull.Value ? null : row["delivery_address"].ToString();
                            Customer.BillingAddress = row["billing_address"] == DBNull.Value ? null : row["billing_address"].ToString();
                            Customer.DeliveryRegencyID = row["delivery_regency_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["delivery_regency_id"]);
                            Customer.DeliveryProvinceID = row["delivery_province_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["delivery_province_id"]);
                            Customer.CountriesID = row["country"] == DBNull.Value ? 0 : Convert.ToInt64(row["country"]);
                            Customer.DeliveryPostalCode = row["delivery_postal_code"] == DBNull.Value ? 0 : Convert.ToInt64(row["delivery_postal_code"]);
                            Customer.Mobile1 = row["mobile1"] == DBNull.Value ? null : row["mobile1"].ToString();
                            Customer.PIC = row["pic"] == DBNull.Value ? null : row["pic"].ToString();
                            Customer.Email = row["email"] == DBNull.Value ? null : row["email"].ToString();
                            Customer.Termin = row["termin"] == DBNull.Value ? null : row["termin"].ToString();
                            Customer.Currency = row["currency"] == DBNull.Value ? null : row["currency"].ToString();
                            Customer.Tax1 = row["tax_1"] == DBNull.Value ? null : row["tax_1"].ToString();
                            Customer.NPWP = row["npwp"] == DBNull.Value ? null : row["npwp"].ToString();
                            Customer.NIK = row["nik"] == DBNull.Value ? null : row["nik"].ToString();
                            Customer.TaxType = row["tax_type"] == DBNull.Value ? null : row["tax_type"].ToString();
                            Customer.VirtualAccount = row["virtual_account"] == DBNull.Value ? null : row["virtual_account"].ToString();
                            Customer.Seller = row["seller"] == DBNull.Value ? null : row["seller"].ToString();
                            Customer.CustomerType = row["customer_type"] == DBNull.Value ? 0 : Convert.ToInt64(row["customer_type"]);
                            Customer.Mobile2 = row["mobile2"] == DBNull.Value ? null : row["mobile2"].ToString();
                            Customer.DeliveryDistrictID = row["delivery_district_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["delivery_district_id"]);
                            Customer.DeliveryPhone = row["delivery_phone"] == DBNull.Value ? null : row["delivery_phone"].ToString();
                            Customer.Status = row["status"] == DBNull.Value ? 0 : Convert.ToInt64(row["status"]);
                            Customer.SalesID = row["sales_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["sales_id"]);
                            CustomerRepository.Insert(Customer);

                            Trace.WriteLine($"Success syncing CustomerID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing CustomerID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Customer completed successfully.");
                    //Utility.DeleteDiffData("mit_customers", "Customers");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Customer....");
        }

        public static Int64 Insert(Customer Customer)
        {
            try
            {
                var sql = @"INSERT INTO [Customers] (Name, RefID, CustomerNumber, DeliveryAddress, BillingAddress, DeliveryRegencyID, DeliveryProvinceID, CountriesID, DeliveryPostalCode, Mobile1, PIC, Email, Termin, Currency, Tax1, NPWP, NIK, TaxType, VirtualAccount, Seller, CustomerType, Mobile2, DeliveryDistrictID, DeliveryPhone,  Status, SalesID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Name, @RefID, @CustomerNumber, @DeliveryAddress, @BillingAddress, @DeliveryRegencyID, @DeliveryProvinceID, @CountriesID, @DeliveryPostalCode, @Mobile1, @PIC, @Email, @Termin, @Currency, @Tax1, @NPWP, @NIK, @TaxType, @VirtualAccount, @Seller, @CustomerType, @Mobile2, @DeliveryDistrictID, @DeliveryPhone,  @Status, @SalesID, @DateIn, @UserIn, @IsDeleted) ";
                Customer.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Customer);

                ///888 from integration
                //SystemLogRepository.Insert("Customer", Customer.ID, 888, Customer);
                return Customer.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Customer Customer)
        {
            try
            {
                var sql = @"UPDATE [Customers] SET
                            Name = @Name,
                            CustomerNumber = @CustomerNumber,
                            DeliveryAddress = @DeliveryAddress,
                            BillingAddress = @BillingAddress,
                            DeliveryRegencyID = @DeliveryRegencyID,
                            DeliveryProvinceID = @DeliveryProvinceID,
                            CountriesID = @CountriesID,
                            DeliveryPostalCode = @DeliveryPostalCode,
                            Mobile1 = @Mobile1,
                            PIC = @PIC,
                            Email = @Email,
                            Termin = @Termin,
                            Currency = @Currency,
                            Tax1 = @Tax1,
                            NPWP = @NPWP,
                            NIK = @NIK,
                            TaxType= @TaxType,
                            VirtualAccount = @VirtualAccount,
                            Seller = @Seller,
                            CustomerType= @CustomerType,
                            Mobile2 = @Mobile2,
                            DeliveryDistrictID = @DeliveryDistrictID,
                            DeliveryPhone= @DeliveryPhone,
                            Status = @Status,
                            SalesID = @SalesID,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Customer, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Customer", Customer.ID, 888, Customer);
                return Customer.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
