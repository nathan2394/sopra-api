using System;
using System.Collections.Generic;
using System.Diagnostics;
//using System.Linq;
using System.Text;
using System.Threading;

using System.Data;

//using SOPRA.Utility;
//using SOPRA.Models;
using Dapper;
using Sopra.Helpers;
using Sopra.Entities;

namespace SOPRA.Services
{
    public class CalculatorRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Calculator....");
            
            ///GET DATA FROM MYSQL
            var tableCalculator = Utility.MySqlGetObjects(string.Format("SELECT mit_calculators.*, mit_products.wms_code AS wmscode, mit_products.stock FROM mit_calculators JOIN mit_products ON mit_products.mit_calculators_id = mit_calculators.id WHERE (mit_calculators.updated_at is null AND mit_calculators.created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR mit_calculators.updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableCalculator != null)
            {
                Trace.WriteLine($"Start Sync Calculator {tableCalculator.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableCalculator.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync CalculatorID : {Convert.ToInt64(row["ID"])}");

                            // DELETE DATA IF EXISTS
                            Utility.ExecuteNonQuery(string.Format("DELETE Calculators WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Calculator = new Calculator();

                            Calculator.RefID = Convert.ToInt64(row["id"]);
                            Calculator.Name = row["name"].ToString();
                            Calculator.NewProd = row["new_prod"] == DBNull.Value ? 0 : Convert.ToInt64(row["new_prod"]);
                            Calculator.FavProd = row["fav_prod"] == DBNull.Value ? 0 : Convert.ToInt64(row["fav_prod"]);
                            Calculator.NewProdDate = row["new_prod_date"] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row["new_prod_date"]);
                            Calculator.Stock = Convert.ToDecimal(row["data_stock"]);
                            Calculator.Price = row["het_total"] == DBNull.Value ? 0 : Convert.ToDecimal(row["het_total"]);
                            Calculator.PrintedHet = row["printed_het"] == DBNull.Value ? 0 : Convert.ToInt32(row["printed_het"]);
                            Calculator.Weight = Convert.ToDecimal(row["weight"]);
                            Calculator.Image = row["image"].ToString();
                            Calculator.CategoriesID = row["mit_categories_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_categories_id"]);
                            Calculator.ColorsID = row["color"] == DBNull.Value ? 0 : Convert.ToInt64(row["color"]);
                            Calculator.Status = row["status"] == DBNull.Value || string.IsNullOrEmpty(row["status"].ToString()) ? 0 : Convert.ToInt64(row["status"]);
                            Calculator.MaterialsID = row["mit_materials_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_materials_id"]);
                            Calculator.Height = row["height"] == DBNull.Value ? 0 : Convert.ToDecimal(row["height"]);
                            Calculator.Length = row["length"] == DBNull.Value ? 0 : Convert.ToDecimal(row["length"]);
                            Calculator.Volume = row["volume"] == DBNull.Value ? 0 : Convert.ToDecimal(row["volume"]);
                            Calculator.Code = row["code"] == DBNull.Value ? null : row["code"].ToString();
                            Calculator.QtyPack = row["qty_pack"] == DBNull.Value ? 0 : Convert.ToDecimal(row["qty_pack"]);
                            Calculator.TotalViews = row["total_views"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_views"]);
                            Calculator.TotalShared = row["total_shared"] == DBNull.Value ? 0 : Convert.ToDecimal(row["total_shared"]);
                            Calculator.ClosuresID = row["closure_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["closure_id"]);
                            Calculator.ShapesID = row["mit_shapes_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_shapes_id"]);
                            Calculator.NecksID = row["mit_necks_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_necks_id"]);
                            Calculator.Width = row["width"] == DBNull.Value ? 0 : Convert.ToDecimal(row["width"]);
                            Calculator.PackagingsID = row["mit_packagings_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_packagings_id"]);
                            Calculator.Note = row["notes"] == DBNull.Value ? null : Convert.ToString(row["notes"]);
                            Calculator.Microwable = row["microwable"] == DBNull.Value ? 0 : Convert.ToInt64(row["microwable"]);
                            Calculator.LessThan60 = row["less_than_60"] == DBNull.Value ? 0 : Convert.ToInt64(row["less_than_60"]);
                            Calculator.LeakProof = row["leak_proof"] == DBNull.Value ? 0 : Convert.ToInt64(row["leak_proof"]);
                            Calculator.TamperEvident = row["tamper_evident"] == DBNull.Value ? 0 : Convert.ToInt64(row["tamper_evident"]);
                            Calculator.AirTight = row["air_tight"] == DBNull.Value ? 0 : Convert.ToInt64(row["air_tight"]);
                            Calculator.BreakResistant = row["break_resistant"] == DBNull.Value ? 0 : Convert.ToInt64(row["break_resistant"]);
                            Calculator.SpillProof = row["spill_proof"] == DBNull.Value ? 0 : Convert.ToInt64(row["spill_proof"]);
                            Calculator.WmsCode = row["wmscode"] == DBNull.Value ? null : Convert.ToString(row["wmscode"]);
                            Calculator.DailyOutput = row["daily_output"] == DBNull.Value ? 0 : Convert.ToDecimal(row["daily_output"]);
                            Calculator.TokpedUrl= row["tokped_url"] == DBNull.Value ? null : Convert.ToString(row["tokped_url"]);
                            Calculator.Plug = row["plug_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["plug_id"]);
                            Calculator.UserIn = 888;
                            CalculatorRepository.Insert(Calculator);

                            Trace.WriteLine($"Success syncing CalculatorID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing CalculatorID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Calculator completed successfully.");
                    
                    //Utility.DeleteDiffData("mit_calculators","Calculators");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");

                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Calculator....");
        }

        public static Int64 Insert(Calculator Calculator)
        {
            try
            {
                var sql = @"INSERT INTO [Calculators] (Plug,PrintedHet,DailyOutput,WmsCode,RefID, Name, NewProd, FavProd, NewProdDate, Stock, Price, Weight, Image, CategoriesID, ColorsID, Status, MaterialsID, Height, Length, Volume, Code, QtyPack, TotalViews, TotalShared, ClosuresID, ShapesID, NecksID, Width, PackagingsID, Note, Microwable, LessThan60, LeakProof, TamperEvident, AirTight, BreakResistant, SpillProof, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@Plug,@PrintedHet,@DailyOutput,@WmsCode,@RefID, @Name, @NewProd, @FavProd, @NewProdDate, @Stock, @Price, @Weight, @Image, @CategoriesID, @ColorsID, @Status, @MaterialsID, @Height, @Length, @Volume, @Code, @QtyPack, @TotalViews, @TotalShared, @ClosuresID, @ShapesID, @NecksID, @Width, @PackagingsID, @Note, @Microwable, @LessThan60, @LeakProof, @TamperEvident, @AirTight, @BreakResistant, @SpillProof, @DateIn, @UserIn, @IsDeleted) ";
                Calculator.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Calculator);

                ///888 from integration
                //SystemLogRepository.Insert("Calculator", Calculator.ID, 888, Calculator);
                return Calculator.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
