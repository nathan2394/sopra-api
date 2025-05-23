using Microsoft.EntityFrameworkCore;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Sopra.Entities;
using Sopra.Helpers;
using Sopra.Responses;
using Sopra.Requests;
using System.Collections.Generic;

namespace Sopra.Services
{
    public class ClosureService : IServiceAsync<Closure>
    {
        private readonly EFContext _context;

        public ClosureService(EFContext context)
        {
            _context = context;
        }

        public async Task<Closure> CreateAsync(Closure data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Closures.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Closure", data.ID, "Add");

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
                var obj = await _context.Closures.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Closure", id, "Delete");

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

        public async Task<Closure> EditAsync(Closure data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Closures.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.Name = data.Name;
                obj.Price = data.Price;
                obj.Diameter = data.Diameter;
                obj.Height = data.Height;
                obj.Weight = data.Weight;
                obj.NecksID = data.NecksID;
                obj.Status = data.Status;
                obj.ColorsID = data.ColorsID;
                obj.Image = data.Image;
                obj.Ranking = data.Ranking;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Closure", data.ID, "Edit");

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


        public async Task<ListResponse<Closure>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Closures where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Contains(search)
                        || x.Name.Contains(search)
                        || x.Price.ToString().Contains(search)
                        || x.Diameter.ToString().Contains(search)
                        || x.Height.ToString().Contains(search)
                        || x.Weight.ToString().Contains(search)
                        || x.NecksID.ToString().Contains(search)
                        || x.Status.ToString().Contains(search)
                        || x.ColorsID.ToString().Contains(search)
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
                                "price" => query = query.Where(x => x.Price.ToString().Contains(value)),
                                "diameter" => query = query.Where(x => x.Diameter.ToString().Contains(value)),
                                "height" => query = query.Where(x => x.Height.ToString().Contains(value)),
                                "weight" => query = query.Where(x => x.Weight.ToString().Contains(value)),
                                "necksid" => query = query.Where(x => x.NecksID.ToString().Contains(value)),
                                "status" => query = query.Where(x => x.Status.ToString().Contains(value)),
                                "colorsid" => query = query.Where(x => x.ColorsID.ToString().Contains(value)),
                                "image" => query = query.Where(x => x.Image.Contains(value)),
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
                            "price" => query = query.OrderByDescending(x => x.Price),
                            "diameter" => query = query.OrderByDescending(x => x.Diameter),
                            "height" => query = query.OrderByDescending(x => x.Height),
                            "weight" => query = query.OrderByDescending(x => x.Weight),
                            "necksid" => query = query.OrderByDescending(x => x.NecksID),
                            "status" => query = query.OrderByDescending(x => x.Status),
                            "colorsid" => query = query.OrderByDescending(x => x.ColorsID),
                            "image" => query = query.OrderByDescending(x => x.Image),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "name" => query.OrderBy(x => x.Name),
                            "price" => query = query.OrderBy(x => x.Price),
                            "diameter" => query = query.OrderBy(x => x.Diameter),
                            "height" => query = query.OrderBy(x => x.Height),
                            "weight" => query = query.OrderBy(x => x.Weight),
                            "necksid" => query = query.OrderBy(x => x.NecksID),
                            "status" => query = query.OrderBy(x => x.Status),
                            "colorsid" => query = query.OrderBy(x => x.ColorsID),
                            "image" => query = query.OrderBy(x => x.Image),
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
                    return await GetAllAsync(limit, page, total, search, sort, filter, date);
                }

                return new ListResponse<Closure>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Closure> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Closures.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<Closure> ChangePassword(ChangePassword obj, long id) { return null; }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId, long masterId) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId) where T : class
        {
            throw new NotImplementedException();
        }
    }
}

