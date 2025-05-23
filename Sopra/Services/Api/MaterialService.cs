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
    public class MaterialService : IServiceAsync<Material>
    {
        private readonly EFContext _context;

        public MaterialService(EFContext context)
        {
            _context = context;
        }

        public async Task<Material> CreateAsync(Material data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Materials.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Material", data.ID, "Add");

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
                var obj = await _context.Materials.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Material", id, "Delete");

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

        public async Task<Material> EditAsync(Material data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Materials.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.Name = data.Name;
                obj.PlasticType = data.PlasticType;
                obj.Halal = data.Halal;
                obj.FoodGrade = data.FoodGrade;
                obj.BpaFree = data.BpaFree;
                obj.EcoFriendly = data.EcoFriendly;
                obj.Recyclable = data.Recyclable;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Material", data.ID, "Edit");

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


        public async Task<ListResponse<Material>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Materials where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Contains(search)
                        || x.Name.Contains(search)
                        || x.PlasticType.Contains(search)
                        || x.Halal.ToString().Contains(search)
                        || x.FoodGrade.ToString().Contains(search)
                        || x.BpaFree.ToString().Contains(search)
                        || x.EcoFriendly.ToString().Contains(search)
                        || x.Recyclable.ToString().Contains(search)
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
                                "plastictype" => query.Where(x => x.PlasticType.Contains(value)),
								"halal" => query.Where(x => x.Halal.ToString().Contains(value)),
								"foodgrade" => query.Where(x => x.FoodGrade.ToString().Contains(value)),
								"bpafree" => query.Where(x => x.BpaFree.ToString().Contains(value)),
								"ecofriendly" => query.Where(x => x.EcoFriendly.ToString().Contains(value)),
								"recyclable" => query.Where(x => x.Recyclable.ToString().Contains(value)),
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
                            "plastictype" => query.OrderByDescending(x => x.PlasticType),
							"halal" => query.OrderByDescending(x => x.Halal),
							"foodgrade" => query.OrderByDescending(x => x.FoodGrade),
							"bpafree" => query.OrderByDescending(x => x.BpaFree),
							"ecofriendly" => query.OrderByDescending(x => x.EcoFriendly),
							"recyclable" => query.OrderByDescending(x => x.Recyclable),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "name" => query.OrderBy(x => x.Name),
                            "plastictype" => query.OrderBy(x => x.PlasticType),
							"halal" => query.OrderBy(x => x.Halal),
							"foodgrade" => query.OrderBy(x => x.FoodGrade),
							"bpafree" => query.OrderBy(x => x.BpaFree),
							"ecofriendly" => query.OrderBy(x => x.EcoFriendly),
							"recyclable" => query.OrderBy(x => x.Recyclable),
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

                return new ListResponse<Material>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Material> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Materials.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<Material> ChangePassword(ChangePassword obj, long id) { return null; }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        public Task<Material> GetAccIdAsync(long id, long customerid)
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

