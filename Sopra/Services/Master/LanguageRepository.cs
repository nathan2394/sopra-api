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

namespace SOPRA.Services
{
    public class LanguageRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Language....");
            

            var tableLanguage = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_languages WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableLanguage != null)
            {
                Trace.WriteLine($"Start Sync Language {tableLanguage.Rows.Count} Data(s)....");
                try
                {
                    ///LOOPING DATA
                    foreach (DataRow row in tableLanguage.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync LanguageID : {Convert.ToInt64(row["ID"])}");

                            ///CHECK DATA EXISTS / TIDAK DI SQL
                            Utility.ExecuteNonQuery(string.Format("DELETE Languages WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var Language = new Language();

                            Language.RefID = Convert.ToInt64(row["id"]);
                            Language.Content = row["label"].ToString();
                            Language.NameID = row["name"].ToString();
                            Language.NameEN = row["name_en"].ToString();
                            LanguageRepository.Insert(Language);

                            Trace.WriteLine($"Success syncing LanguageID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing LanguageID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Language completed successfully.");
                    //Utility.DeleteDiffData("mit_languages", "Languages");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                }
            }

            Trace.WriteLine("Finished Sync Language....");
        }

        public static Int64 Insert(Language Language)
        {
            try
            {
                var sql = @"INSERT INTO [Languages] (RefID, Content, NameID, NameEN, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@RefID, @Content, @NameID, @NameEN, @DateIn, @UserIn, @IsDeleted) ";
                Language.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, Language);

                ///888 from integration
                //SystemLogRepository.Insert("Language", Language.ID, 888, Language);
                return Language.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(Language Language)
        {
            try
            {
                var sql = @"UPDATE [Languages] SET
                            Content = @Content,
                            NameID = @NameID,
                            NameEN = @NameEN,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, Language, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("Language", Language.ID, 888, Language);
                return Language.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
