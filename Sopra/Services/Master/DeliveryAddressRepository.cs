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
using SOPRA.Services;

namespace Sopra.Services
{
    public class DeliveryAddressRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Delivery Address....");

            var tableLanguage = Utility.MySqlGetObjects(string.Format("select id,delivery_address ,delivery_regency_id ,delivery_province_id ,delivery_postal_code ,delivery_phone ,delivery_district_id ,delivery_fax ,delivery_street,country from mit_customers where last_sync > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableLanguage != null)
            {
                Trace.WriteLine($"Start Sync DeliveryAddress {tableLanguage.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableLanguage.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync DeliveryAddressID : {Convert.ToInt64(row["ID"])}");

                            ///CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE DeliveryAddresses WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));
                            var DeliveryAddress = new DeliveryAddress();

                            DeliveryAddress.RefID = row["id"] == DBNull.Value ? 0 : Convert.ToInt64(row["id"]);
                            DeliveryAddress.DistrictId = row["delivery_district_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["delivery_district_id"]);
                            DeliveryAddress.CityId = row["delivery_regency_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["delivery_regency_id"]);
                            DeliveryAddress.ProvinceId = row["delivery_province_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["delivery_province_id"]);
                            DeliveryAddress.CountryId = row["country"] == DBNull.Value ? 0 : Convert.ToInt64(row["country"]);
                            DeliveryAddress.Address = row["delivery_address"] == DBNull.Value ? null : row["delivery_address"].ToString();
                            DeliveryAddress.ZipCode = row["delivery_postal_code"] == DBNull.Value ? null : row["delivery_postal_code"].ToString();
                            DeliveryAddress.IsUse = false;
                            DeliveryAddress.UserId = row["id"] == DBNull.Value ? 0 : Convert.ToInt64(row["id"]);
                            DeliveryAddressRepository.Insert(DeliveryAddress);
                            DeliveryAddressRepository.UpdateIsUse(Convert.ToInt64(DeliveryAddress.UserId));
                            Trace.WriteLine($"Success syncing DeliveryAddressID : {Convert.ToInt64(row["ID"])}");
                            //Utility.DeleteDiffData("mit_customers", "DeliveryAddresses");

                            //Trace.WriteLine($"Delete Diff Data completed successfully.");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing DeliveryAddressID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization DeliveryAddress completed successfully.");
                    //Utility.DeleteDiffData("mit_carts_detail_thermo", "CartDetails");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            Trace.WriteLine("Finished Sync DeliveryAddress....");
        }

        public static Int64 Insert(DeliveryAddress DeliveryAddress)
        {
            try
            {
                var sql = @"INSERT INTO DeliveryAddresses (RefID, Address,DistrictId,CityId,ProvinceId,CountryId,ZipCode,UserId,IsUse, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @Address,@DistrictId,@CityId,@ProvinceId,@CountryId,@ZipCode,@UserId,@IsUse,@UserIn, @IsDeleted) ";
                DeliveryAddress.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, DeliveryAddress);

                ///888 from integration
                //SystemLogRepository.Insert("Language", Language.ID, 888, Language);
                return DeliveryAddress.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static void UpdateIsUse(long userId)
        {
            var tableLanguage = Utility.SQLGetObjects(string.Format("select * from DeliveryAddresses where UserId = {0}", userId), Utility.SQLDBConnection);
            if (tableLanguage != null)
            {
                try
                {
                    if(tableLanguage.Rows.Count == 1)
                    {
                        Utility.ExecuteNonQuery(string.Format("update DeliveryAddresses set IsUse = 1 where UserId = {0}",userId));
                    } 
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during update is use : {ex.Message}");
                }
            }
        }

        public static Int64 Update(DeliveryAddress DeliveryAddress)
        {
            try
            {
                var sql = @"UPDATE DeliveryAddresses SET
                            Address = @Address,
                            DistrictId = @DistrictId,
                            CityId = @CityId,
                            ProvinceId = @ProvinceId,
                            CountryId = @CountryId,
                            ZipCode = @ZipCode,
                            UserId = @UserId,
                            IsUse = @IsUse,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, DeliveryAddress, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("DeliveryAddress", DeliveryAddress.ID, 888, DeliveryAddress);
                return DeliveryAddress.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
