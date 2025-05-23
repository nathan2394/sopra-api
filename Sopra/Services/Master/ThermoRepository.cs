using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Linq;
using System.Text;

using System.Data;

//using SOPRA.Utility;
//using SOPRA.Models;
using Dapper;
using System.Threading;
using Sopra.Helpers;
using Sopra.Entities;

namespace SOPRA.Services
{
    public class ThermoRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Thermo....");
            
            ///GET DATA FROM MYSQL
            var tableThermo = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_thermos ", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableThermo != null)
            {
                Trace.WriteLine($"Start Sync Thermo {tableThermo.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableThermo.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync ThermoID : {Convert.ToInt64(row["ID"])}");
                            
                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Thermos WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Thermo = new Thermo();

                            Thermo.RefID = Convert.ToInt64(row["id"]);
                            Thermo.Name = row["name"].ToString();
                            Thermo.NewProd = row["new_prod"] == DBNull.Value ? 0 : Convert.ToInt64(row["new_prod"]);
                            Thermo.NewProdDate = row["new_prod_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["new_prod_date"]);
                            Thermo.FavProd = row["fav_prod"] == DBNull.Value ? 0 : Convert.ToInt64(row["fav_prod"]);
                            Thermo.Image = row["image"].ToString();
                            Thermo.Weight = Convert.ToDecimal(row["weight"]);
                            Thermo.Price = row["total_price"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_price"]);
                            Thermo.PrintedHet = row["price"] == DBNull.Value ? 0 : Convert.ToInt32(row["price"]);
                            Thermo.CategoriesID = row["mit_thermo_categories_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_thermo_categories_id"]);
                            Thermo.Stock = Convert.ToDecimal(row["data_stock"]);
                            Thermo.Status = Convert.ToInt64(row["status"]);
                            Thermo.MaterialsID = row["mit_materials_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_materials_id"]);
                            Thermo.LidsID = row["mit_lids_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_lids_id"]);
                            Thermo.Volume = row["volume"] == DBNull.Value ? 0 : Convert.ToDecimal(row["volume"]);
                            Thermo.Qty = row["qty"] == DBNull.Value ? 0 : Convert.ToDecimal(row["qty"]);
                            Thermo.RimsID = row["mit_rims_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_rims_id"]);
                            Thermo.ColorsID = row["mit_colors_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_colors_id"]);
                            Thermo.ShapesID = row["mit_shapes_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_shapes_id"]);
                            Thermo.Length = row["length"] == DBNull.Value ? 0 : Convert.ToDecimal(row["length"]);
                            Thermo.Width = row["width"] == DBNull.Value ? 0 : Convert.ToDecimal(row["width"]);
                            Thermo.Height = row["height"] == DBNull.Value ? 0 : Convert.ToDecimal(row["height"]);
                            Thermo.PackagingsID = row["packaging"] == DBNull.Value ? 0 : Convert.ToInt64(row["packaging"]);
                            Thermo.Code = row["wms_code"] == DBNull.Value ? null : Convert.ToString(row["wms_code"]);
                            Thermo.TotalViews = row["total_views"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_views"]);
                            Thermo.TotalShared = row["total_shared"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_shared"]);
                            Thermo.Note = row["notes"] == DBNull.Value ? null : Convert.ToString(row["notes"]);
							Thermo.Microwable = row["microwable"] == DBNull.Value ? 0 : Convert.ToInt64(row["microwable"]);
							Thermo.LessThan60 = row["less_than_60"] == DBNull.Value ? 0 : Convert.ToInt64(row["less_than_60"]);
							Thermo.LeakProof = row["leak_proof"] == DBNull.Value ? 0 : Convert.ToInt64(row["leak_proof"]);
							Thermo.TamperEvident = row["tamper_evident"] == DBNull.Value ? 0 : Convert.ToInt64(row["tamper_evident"]);
							Thermo.AirTight = row["air_tight"] == DBNull.Value ? 0 : Convert.ToInt64(row["air_tight"]);
							Thermo.BreakResistant = row["break_resistant"] == DBNull.Value ? 0 : Convert.ToInt64(row["break_resistant"]);
							Thermo.SpillProof = row["spill_proof"] == DBNull.Value ? 0 : Convert.ToInt64(row["spill_proof"]);
                            Thermo.WmsCode = row["wms_code"] == DBNull.Value ? null : Convert.ToString(row["wms_code"]);
                            Thermo.TokpedUrl = row["tokped_url"] == DBNull.Value ? null : Convert.ToString(row["tokped_url"]);
                            Thermo.UserIn = 888;
                            ThermoRepository.Insert(Thermo);

                            Trace.WriteLine($"Success syncing ThermoID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing ThermoID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Thermo completed successfully.");
                    
                    //Utility.DeleteDiffData("mit_thermos","Thermos");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");

                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            Trace.WriteLine("Finished Sync Thermo....");
        }

        public static Int64 Insert(Thermo Thermo)
        {
            try
            {
                var sql = @"INSERT INTO [Thermos]  (WmsCode,RefID, Name, NewProd, NewProdDate, FavProd, Image, Weight, Price, CategoriesID, Stock, Status, MaterialsID, LidsID, Volume, Qty, RimsID, ColorsID, ShapesID, Length, Width, Height, PackagingsID, Code, TotalViews, TotalShared, Note, Microwable, LessThan60, LeakProof, TamperEvident, AirTight, BreakResistant, SpillProof, DateIn, UserIn, IsDeleted,PrintedHet) 
                            OUTPUT INSERTED.ID
                            VALUES  (@WmsCode,@RefID, @Name, @NewProd, @NewProdDate, @FavProd, @Image, @Weight, @Price, @CategoriesID, @Stock, @Status, @MaterialsID, @LidsID, @Volume, @Qty, @RimsID, @ColorsID, @ShapesID, @Length, @Width, @Height, @PackagingsID, @Code, @TotalViews, @TotalShared, @Note, @Microwable, @LessThan60, @LeakProof, @TamperEvident, @AirTight, @BreakResistant, @SpillProof, @DateIn, @UserIn, @IsDeleted,@PrintedHet) ";
                Thermo.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Thermo);

                ///888 from integration
                //SystemLogRepository.Insert("Thermo", Thermo.ID, 888, Thermo);
                return Thermo.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
