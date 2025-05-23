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
    public class FavoriteService : IServiceAsync<Favorite>
    {
        private readonly EFContext _context;

        public FavoriteService(EFContext context)
        {
            _context = context;
        }

        public async Task<Favorite> CreateAsync(Favorite data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Favorites.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Favorite", data.ID, "Add");

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
                var obj = await _context.Favorites.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Favorite", id, "Delete");

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

        public async Task<Favorite> EditAsync(Favorite data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Favorites.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.ObjectID = data.ObjectID;
                obj.Type = data.Type;
                obj.Accs1ID = data.Accs1ID;
                obj.Accs2ID = data.Accs2ID;
                obj.SalesPrice = data.SalesPrice;
                obj.PriceAcc1 = data.PriceAcc1;
                obj.PriceAcc2 = data.PriceAcc2;
                obj.UsersID = data.UsersID;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Favorite", data.ID, "Edit");

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


        public async Task<ListResponse<Favorite>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Favorites where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Contains(search)
                        || x.ObjectID.ToString().Contains(search)
                        || x.Type.Contains(search)
                        || x.Accs1ID.ToString().Contains(search)
                        || x.Accs2ID.ToString().Contains(search)
                        || x.SalesPrice.ToString().Contains(search)
                        || x.PriceAcc1.ToString().Contains(search)
                        || x.PriceAcc2.ToString().Contains(search)
                        || x.UsersID.ToString().Contains(search)
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
                                "objectid" => query.Where(x => x.ObjectID.ToString().Contains(value)),
                                "type" => query.Where(x => x.Type.Contains(value)),
                                "accs1id" => query.Where(x => x.Accs1ID.ToString().Contains(value)),
                                "accs2id" => query.Where(x => x.Accs2ID.ToString().Contains(value)),
                                "salesprice" => query.Where(x => x.SalesPrice.ToString().Contains(value)),
                                "priceacc1" => query.Where(x => x.PriceAcc1.ToString().Contains(value)),
                                "priceacc2" => query.Where(x => x.PriceAcc2.ToString().Contains(value)),
                                "usersid" => query.Where(x => x.UsersID.ToString().Contains(value)),
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
                            "objectid" => query.OrderByDescending(x => x.ObjectID),
                            "type" => query.OrderByDescending(x => x.Type),
                            "accs1id" => query.OrderByDescending(x => x.Accs1ID),
                            "accs2id" => query.OrderByDescending(x => x.Accs2ID),
                            "salesprice" => query.OrderByDescending(x => x.SalesPrice),
                            "priceacc1" => query.OrderByDescending(x => x.PriceAcc1),
                            "priceacc2" => query.OrderByDescending(x => x.PriceAcc2),
                            "usersid" => query.OrderByDescending(x => x.UsersID),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "objectid" => query.OrderBy(x => x.ObjectID),
                            "type" => query.OrderBy(x => x.Type),
                            "accs1id" => query.OrderBy(x => x.Accs1ID),
                            "accs2id" => query.OrderBy(x => x.Accs2ID),
                            "salesprice" => query.OrderBy(x => x.SalesPrice),
                            "priceacc1" => query.OrderBy(x => x.PriceAcc1),
                            "priceacc2" => query.OrderBy(x => x.PriceAcc2),
                            "usersid" => query.OrderBy(x => x.UsersID),
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

                return new ListResponse<Favorite>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Favorite> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Favorites.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<Favorite> ChangePassword(ChangePassword obj, long id) { return null; }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId) where T : class
        {
            throw new NotImplementedException();
        }
    }
}

