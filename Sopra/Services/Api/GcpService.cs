using Microsoft.EntityFrameworkCore;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Sopra.Entities;
using Sopra.Helpers;
using Sopra.Responses;
using Google.Cloud.Storage.V1;
using System.IO;
using System.Net;

namespace Sopra.Services
{
    public class GcpService : IServiceGcpAsync<Gcp>
    {
        private readonly EFContext _context;
        private readonly StorageClient _storageClient;

        public GcpService(EFContext context, StorageClient storageClient)
        {
            _context = context;
            _storageClient = storageClient;
        }

        public async Task<Gcp> CreateAsync(Gcp data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Gcps.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Gcp", data.ID, "Add");

                await dbTrans.CommitAsync();

                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                await dbTrans.RollbackAsync();

                throw;
            }
        }

        public async Task<bool> DeleteAsync(long id, long UserID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Gcps.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Gcp", id, "Delete");

                await dbTrans.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                await dbTrans.RollbackAsync();

                throw;
            }
        }

        public async Task<Gcp> EditAsync(Gcp data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Gcps.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.Name = data.Name;
                obj.Image = data.Image;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Gcp", data.ID, "Edit");

                await dbTrans.CommitAsync();

                return obj;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                await dbTrans.RollbackAsync();

                throw;
            }
        }


        public async Task<ListResponse<Gcp>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date, string fileName)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Gcps where a.IsDeleted == false select a;

                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Contains(search)
                        || x.Name.Contains(search)
                        || x.Image.Contains(search)
                        );

                // Filtering
                if (!string.IsNullOrEmpty(filter))
                {
                    var filterList = filter.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    foreach (var f in filterList)
                    {
                        var searchList = f.Split(":", StringSplitOptions.RemoveEmptyEntries);
                        if (searchList.Length == 2)
                        {
                            var fieldName = searchList[0].Trim().ToLower();
                            var value = searchList[1].Trim();
                            query = fieldName switch
                            {
                                "refid" => query.Where(x => x.RefID.ToString().Contains(value)),
                                "name" => query.Where(x => x.Name.Contains(value)),
                                "image" => query.Where(x => x.Image.Contains(value)),
                                _ => query
                            };
                        }
                    }
                }

                // Sorting
                if (!string.IsNullOrEmpty(sort))
                {
                    var temp = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var orderBy = sort;
                    if (temp.Length > 1)
                        orderBy = temp[0];

                    if (temp.Length > 1)
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderByDescending(x => x.RefID),
                            "name" => query.OrderByDescending(x => x.Name),
                            "image" => query.OrderByDescending(x => x.Image),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "name" => query.OrderBy(x => x.Name),
                            "image" => query.OrderBy(x => x.Image),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.ID);
                }

                // Get Total Before Limit and Page
                total = await query.CountAsync();

                // Set Limit and Page
                if (limit != 0)
                    query = query.Skip(page * limit).Take(limit);

                // Get Data
                var data = await query.ToListAsync();
                if (data.Count <= 0 && page > 0)
                {
                    page = 0;
                    return await GetAllAsync(limit, page, total, search, sort, filter, date, fileName);
                }

                // Fetching URLs asynchronously
                var tasks = data.Select(async gcp =>
                {
                    var stream = new MemoryStream();
                    try
                    {
                        var obj = await _storageClient.DownloadObjectAsync("sopra-web", gcp.Image, stream);

                        if (obj != null)
                        {
                            stream.Position = 0;
                            gcp.PublicUrl = "https://storage.googleapis.com/sopra-web/" + obj.Name;
                            gcp.AuthenticatedUrl = "https://storage.cloud.google.com/sopra-web/" + obj.Name;
                        }
                        else
                        {
                            gcp.PublicUrl = null;
                            gcp.AuthenticatedUrl = null;
                        }
                    }
                    catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.NotFound)
                    {
                        Trace.WriteLine($"Object not found in storage: {gcp.Image}");
                        gcp.PublicUrl = null;
                        gcp.AuthenticatedUrl = null;
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error fetching object for {gcp.Image}: {ex.Message}");
                        gcp.PublicUrl = null;
                        gcp.AuthenticatedUrl = null;
                    }
                });

                // Wait for all URL tasks to complete
                await Task.WhenAll(tasks);

                return new ListResponse<Gcp>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Gcp> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Gcps.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }
    }
}

