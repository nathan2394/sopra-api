
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
    public class LidService : IServiceAsync<Lid>
    {
        private readonly EFContext _context;

        public LidService(EFContext context)
        {
            _context = context;
        }

        public async Task<Lid> CreateAsync(Lid data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Lids.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Lid", data.ID, "Add");

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
                var obj = await _context.Lids.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Lid", id, "Delete");

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

        public async Task<Lid> EditAsync(Lid data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Lids.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.Name = data.Name;
                obj.NewProd = data.NewProd;
                obj.FavProd = data.FavProd;
                obj.RimsID = data.RimsID;
                obj.ColorsID = data.ColorsID;
                obj.MaterialsID = data.MaterialsID;
                obj.Price = data.Price;
                obj.Length = data.Length;
                obj.Width = data.Width;
                obj.Height = data.Height;
                obj.Weight = data.Weight;
                obj.Qty = data.Qty;
                obj.Image = data.Image;
                obj.Status = data.Status;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Lid", data.ID, "Edit");

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


        public async Task<ListResponse<Lid>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Lids where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Contains(search)
                        || x.Name.Contains(search)
                        || x.NewProd.ToString().Contains(search)
                        || x.FavProd.ToString().Contains(search)
                        || x.RimsID.ToString().Contains(search)
                        || x.MaterialsID.ToString().Contains(search)
                        || x.ColorsID.ToString().Contains(search)
                        || x.Price.ToString().Contains(search)
                        || x.Length.ToString().Contains(search)
                        || x.Width.ToString().Contains(search)
                        || x.Height.ToString().Contains(search)
                        || x.Weight.ToString().Contains(search)
                        || x.Qty.ToString().Contains(search)
                        || x.Image.Contains(search)
                        || x.Status.ToString().Contains(search)

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
                                "newprod" => query = query.Where(x => x.NewProd.ToString().Contains(value)),
                                "favprod" => query = query.Where(x => x.FavProd.ToString().Contains(value)),
                                "rimsid" => query = query.Where(x => x.RimsID.ToString().Contains(value)),
                                "materialsid" => query = query.Where(x => x.MaterialsID.ToString().Contains(value)),
                                "colorsid" => query = query.Where(x => x.ColorsID.ToString().Contains(value)),
                                "price" => query = query.Where(x => x.Price.ToString().Contains(value)),
                                "length" => query = query.Where(x => x.Length.ToString().Contains(value)),
                                "width" => query = query.Where(x => x.Width.ToString().Contains(value)),
                                "height" => query = query.Where(x => x.Height.ToString().Contains(value)),
                                "weight" => query = query.Where(x => x.Weight.ToString().Contains(value)),
                                "qty" => query = query.Where(x => x.Qty.ToString().Contains(value)),
                                "image" => query = query.Where(x => x.Image.Contains(value)),
                                "status" => query = query.Where(x => x.Status.ToString().Contains(value)),
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
                            "newprod" => query = query.OrderByDescending(x => x.NewProd),
                            "favprod" => query = query.OrderByDescending(x => x.FavProd),
                            "rimsid" => query = query.OrderByDescending(x => x.RimsID),
                            "materialsid" => query = query.OrderByDescending(x => x.MaterialsID),
                            "colorsid" => query = query.OrderByDescending(x => x.ColorsID),
                            "price" => query = query.OrderByDescending(x => x.Price),
                            "length" => query = query.OrderByDescending(x => x.Length),
                            "width" => query = query.OrderByDescending(x => x.Width),
                            "height" => query = query.OrderByDescending(x => x.Height),
                            "weight" => query = query.OrderByDescending(x => x.Weight),
                            "qty" => query = query.OrderByDescending(x => x.Qty),
                            "image" => query = query.OrderByDescending(x => x.Image),
                            "status" => query = query.OrderByDescending(x => x.Status),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "name" => query.OrderBy(x => x.Name),
                            "newprod" => query = query.OrderBy(x => x.NewProd),
                            "favprod" => query = query.OrderBy(x => x.FavProd),
                            "rimsid" => query = query.OrderBy(x => x.RimsID),
                            "materialsid" => query = query.OrderBy(x => x.MaterialsID),
                            "colorsid" => query = query.OrderBy(x => x.ColorsID),
                            "price" => query = query.OrderBy(x => x.Price),
                            "length" => query = query.OrderBy(x => x.Length),
                            "width" => query = query.OrderBy(x => x.Width),
                            "height" => query = query.OrderBy(x => x.Height),
                            "weight" => query = query.OrderBy(x => x.Weight),
                            "qty" => query = query.OrderBy(x => x.Qty),
                            "image" => query = query.OrderBy(x => x.Image),
                            "status" => query = query.OrderBy(x => x.Status),
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

                return new ListResponse<Lid>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Lid> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Lids.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<Lid> ChangePassword(ChangePassword obj, long id) { return null; }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId) where T : class
        {
            throw new NotImplementedException();
        }
    }
}

