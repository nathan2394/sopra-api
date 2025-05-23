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
    public class TagVideoRepository
    {
        public static void Sync()
        {
            Trace.WriteLine("Running Sync Tag Video....");
            

            var tableTagVideoBottle = Utility.MySqlGetObjects(string.Format("SELECT * FROM mit_tags_details WHERE (updated_at is null AND created_at > '{0:yyyy-MM-dd HH:mm:ss}') OR updated_at > '{0:yyyy-MM-dd HH:mm:ss}'", Utility.SyncDate), Utility.MySQLDBConnection);
            if (tableTagVideoBottle != null)
            {
                Trace.WriteLine($"Start Sync Tag Video {tableTagVideoBottle.Rows.Count} Data(s)....");
                try
                {
                    // LOOPING DATA
                    foreach (DataRow row in tableTagVideoBottle.Rows)
                    {
                        try
                        {
                            Trace.WriteLine($"Sync TagVideoID : {Convert.ToInt64(row["ID"])}");
                            // CHECK DATA EXISTS / TIDAK DI SQL

                            Utility.ExecuteNonQuery(string.Format("DELETE TagVideos WHERE RefID = {0} AND IsDeleted = 0", row["ID"]));

                            var TagVideo = new TagVideo();

                            TagVideo.RefID = Convert.ToInt64(row["id"]);
                            TagVideo.TagsID = row["mit_tags_id"] == DBNull.Value ? 0 : Convert.ToInt64(row["mit_tags_id"]);
                            TagVideo.VideoLink = row["video_link"] == DBNull.Value ? null : Convert.ToString(row["video_link"]);
                            TagVideo.Description = row["description"] == DBNull.Value ? null : Convert.ToString(row["description"]);
                            TagVideoRepository.Insert(TagVideo);

                            Trace.WriteLine($"Success syncing TagVideoID : {Convert.ToInt64(row["ID"])}");
                            Thread.Sleep(100);
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine($"Error syncing TagVideoID: {Convert.ToInt64(row["ID"])} - {ex.Message}");
                            Thread.Sleep(100);
                        }
                    }

                    Trace.WriteLine($"Synchronization Tag Video completed successfully.");
                    //Utility.DeleteDiffData("mit_tags_details", "TagVideos");

                    //Trace.WriteLine($"Delete Diff Data completed successfully.");
                    Thread.Sleep(100);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"Error during synchronization: {ex.Message}");
                    Thread.Sleep(100);
                }
            }

            Trace.WriteLine("Finished Sync Tag....");
        }

        public static Int64 Insert(TagVideo TagVideo)
        {
            try
            {
                var sql = @"INSERT INTO [TagVideos] (TagsID, VideoLink, Description, RefID, DateIn, UserIn, IsDeleted) 
                            OUTPUT INSERTED.ID
                            VALUES (@TagsID, @VideoLink,@Description, @RefID, @DateIn, @UserIn, @IsDeleted) ";
                TagVideo.ID = Utility.SQLDBConnection.QuerySingle<Int64>(sql, TagVideo);

                ///888 from integration
                //SystemLogRepository.Insert("TagVideo", TagVideo.ID, 888, TagVideo);
                return TagVideo.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public static Int64 Update(TagVideo TagVideo)
        {
            try
            {
                var sql = @"UPDATE [TagVideos] SET
                            TagsID = @TagsID,
                            VideoLink = @VideoLink,
                            Description = @Description,
                            IsDeleted = @IsDeleted,
                            DateUp = GETDATE(),
                            UserUp = @UserUp
                           WHERE ID = @ID";
                Utility.SQLDBConnection.Execute(sql, TagVideo, transaction: null);

                ///888 from integration
                //SystemLogRepository.Insert("TagVideo", TagVideo.ID, 888, TagVideo);
                return TagVideo.ID;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
