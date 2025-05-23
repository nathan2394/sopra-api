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

namespace Sopra.Services
{
    public class BillingAddressRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Billing Address....");
            

            var tableLanguage = Utility.MySqlGetObjects(string.Format("select id,billing_address ,billing_province_id ,billing_regency_id ,billing_district_id ,billing_postal_code ,billing_phone ,billing_fax ,billing_street ,country from mit_customers where last_sync > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableLanguage != null)
            {
                Trace.WriteLine($"Start Sync BillingAddress {tableLanguage.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableLanguage.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync BillingAddressID : {Convert.ToInt64(row["ID"])}");

                            ///CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE BillingAddresses WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var BillingAddress = new BillingAddress();

                            BillingAddress.RefID = row["id"] == DBNull.Value ? 0 : Convert.ToInt64(row["id"]);
                            BillingAddress.DistrictId = row["billing_district_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["billing_district_id"]);
                            BillingAddress.CityId = row["billing_regency_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["billing_regency_id"]);
                            BillingAddress.ProvinceId = row["billing_province_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["billing_province_id"]);
                            BillingAddress.CountryId = row["country"] == DBNull.Value ? 0 : Convert.ToInt64(row["country"]);
                            BillingAddress.Address = row["billing_address"] == DBNull.Value ? null : row["billing_address"].ToString();
                            BillingAddress.ZipCode = row["billing_postal_code"] == DBNull.Value ? null : row["billing_postal_code"].ToString();
                            BillingAddress.UserId = row["id"] == DBNull.Value ? 0 : Convert.ToInt64(row["id"]);
                            BillingAddressRepository.Insert(BillingAddress);

                            Trace.WriteLine($"Success syncing BillingAddressID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing BillingAddressID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization BillingAddress completed successfully.");
                    //Utility.DeleteDiffData("mit_customers", "BillingAddresses");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            Trace.WriteLine("Finished Sync BillingAddress....");
        }

        public static Int64 Insert(BillingAddress BillingAddress)
        {
            try
            {
                var sql = @"INSERT INTO BillingAddresses (RefID, Address,DistrictId,CityId,ProvinceId,CountryId,ZipCode,UserId, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @Address,@DistrictId,@CityId,@ProvinceId,@CountryId,@ZipCode,@UserId,@UserIn, @IsDeleted) ";
                BillingAddress.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, BillingAddress);

                ///888 from integration
                //SystemLogRepository.Insert("Language", Language.ID, 888, Language);
                return BillingAddress.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(BillingAddress BillingAddress)
        {
            try
            {
                var sql = @"UPDATE BillingAddresses SET
                            Address = @Address,
                            DistrictId = @DistrictId,
                            CityId = @CityId,
                            ProvinceId = @ProvinceId,
                            CountryId = @CountryId,
                            ZipCode = @ZipCode,
                            UserId = @UserId,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, BillingAddress, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("BillingAddress", BillingAddress.ID, 888, BillingAddress);
                return BillingAddress.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
