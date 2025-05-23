using Microsoft.EntityFrameworkCore;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Sopra.Entities;
using Sopra.Helpers;
using Sopra.Responses;
using Microsoft.AspNetCore.Mvc;
using Sopra.Requests;
using Azure.Core;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace Sopra.Services
{
    public class PromoQuantityService : IServiceAsync<PromoQuantity>
    {
        private readonly EFContext _context;

        public PromoQuantityService(EFContext context)
        {
            _context = context;
        }

        public async Task<PromoQuantity> CreateAsync(PromoQuantity data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                data.PromoMixId = data.PromoMixId == null ? 0 : data.PromoMixId;
                data.PromoJumboId = data.PromoJumboId == null ? 0 : data.PromoJumboId;
                if (data.PromoMixId != 0 ) data.Price = 0;
                await _context.PromoQuantities.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "PromoQuantity", data.ID, "Add");

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

        public async Task<bool> DeleteAsync(long id, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.PromoQuantities.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = userID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "PromoQuantity", id, "Delete");

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

        public async Task<PromoQuantity> EditAsync(PromoQuantity data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.PromoQuantities.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.PromoJumboId = data.PromoJumboId;
                obj.PromoMixId = data.PromoMixId;
                obj.MinQuantity = data.MinQuantity;
                obj.Price = data.Price;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "PromoQuantity", data.ID, "Edit");

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

        public Task<PromoQuantity> GetAccIdAsync(long id, long customerid)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId) where T : class
        {
            throw new NotImplementedException();
        }

        public async Task<ListResponse<PromoQuantity>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.PromoQuantities where a.IsDeleted == false select a;
                //query = query.Include(x => x.PromoMix);
                //query = query.Include(x => x.PromoJumbo);

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.PromoJumboId.ToString().Contains(search)
                        || x.PromoMixId.ToString().Contains(search)
                        || x.MinQuantity.ToString().Contains(search)
                        || x.Price.ToString().Contains(search));

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
                                "promojumboid" => query.Where(x => x.PromoJumboId.ToString().Contains(value)),
                                "promomixid" => query.Where(x => x.PromoMixId.ToString().Contains(value)),
                                "minquantity" => query.Where(x => x.MinQuantity.ToString().Contains(value)),
                                "price" => query.Where(x => x.Price.ToString().Contains(value)),
                                //"role" => query.Where(x => x.RoleID.Contains(value)),
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
                            "promojumboid" => query.OrderByDescending(x => x.PromoJumboId),
                            "promomixid" => query.OrderByDescending(x => x.PromoMixId),
                            "minquantity" => query.OrderByDescending(x => x.MinQuantity),
                            "price" => query.OrderByDescending(x => x.Price),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "promojumboid" => query.OrderBy(x => x.PromoJumboId),
                            "promomixid" => query.OrderBy(x => x.PromoMixId),
                            "minquantity" => query.OrderBy(x => x.MinQuantity),
                            "price" => query.OrderBy(x => x.Price),
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

                foreach (var item in data)
                {
                    item.PromoJumbo = _context.Promos.FirstOrDefault(x => x.RefID == item.PromoJumboId && x.Type == "Jumbo");
                    item.PromoMix = _context.Promos.FirstOrDefault(x => x.RefID == item.PromoMixId && x.Type == "Mix ");
                }

                return new ListResponse<PromoQuantity>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<PromoQuantity> GetByIdAsync(long id)
        {
            try
            {
                var data = await _context.PromoQuantities.AsNoTracking()
                        //.Include(x => x.PromoJumbo)
                        //.Include(x => x.PromoMix)
                        .FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                data.PromoJumbo = _context.Promos.FirstOrDefault(x => x.RefID == data.PromoJumboId && x.Type == "Jumbo");
                data.PromoMix = _context.Promos.FirstOrDefault(x => x.RefID == data.PromoMixId && x.Type == "Mix ");
                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        Task<PromoQuantity> IServiceAsync<PromoQuantity>.ChangePassword(ChangePassword obj, long id)
        {
            throw new NotImplementedException();
        }
    }
}
