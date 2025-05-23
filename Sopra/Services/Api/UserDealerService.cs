using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using Sopra.Entities;
using Sopra.Helpers;
using Sopra.Requests;
using Sopra.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopra.Services.Api
{
    public class UserDealerService : IServiceAsync<UserDealer>
    {

        private readonly EFContext _context;

        public UserDealerService(EFContext context)
        {
            _context = context;
        }
        
        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }


        public async Task<UserDealer> CreateAsync(UserDealer data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.UserDealers.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "UserDealer", data.ID, "Add");

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
                var obj = await _context.UserDealers.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = userID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "UserDealer", id, "Delete");

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

            public async Task<UserDealer> EditAsync(UserDealer data)
                {
                await using var dbTrans = await _context.Database.BeginTransactionAsync();
                try
                {
                    var obj = await _context.UserDealers.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                    if (obj == null) return null;

                    obj.RefID = data.RefID;
                    obj.StartDate= data.StartDate;
                    obj.EndDate = data.EndDate;
                    obj.UserId= data.UserId;
                    obj.DealerId = data.DealerId;

                    obj.UserUp = data.UserUp;
                    obj.DateUp = DateTime.Now;

                    await _context.SaveChangesAsync();

                    // Check Validate
                    await Utility.AfterSave(_context, "UserDealer", data.ID, "Edit");

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

            public async Task<ListResponse<UserDealer>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
            {
                try
                {
                    _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.UserDealers where a.IsDeleted == false && (DateTime.Now >= a.StartDate && DateTime.Now <= a.EndDate) select a;

                    // Searching
                    if (!string.IsNullOrEmpty(search))
                        query = query.Where(x => x.RefID.ToString().Contains(search)
                            || x.DealerId.ToString().Equals(search)
                            || x.UserId.ToString().Equals(search)
                            || x.StartDate.ToString().Contains(search)
                            || x.EndDate.ToString().Contains(search)
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
                                    "userid" => query.Where(x => x.UserId.ToString().Equals(value)),
                                    "dealerid" => query.Where(x => x.DealerId.ToString().Equals(value)),
                                    "enddate" => query.Where(x => x.EndDate.ToString().Contains(value)),
                                    "startdate" => query.Where(x => x.StartDate.ToString().Contains(value)),
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
                                "userid" => query.OrderByDescending(x => x.UserId),
                                "dealerid" => query.OrderByDescending(x => x.DealerId),
                                "enddate" => query.OrderByDescending(x => x.EndDate),
                                "startdate" => query.OrderByDescending(x => x.StartDate),
                                _ => query
                            };
                        }
                        else
                        {
                            query = orderBy.ToLower() switch
                            {
                                "refid" => query.OrderBy(x => x.RefID),
                                "userid" => query.OrderBy(x => x.UserId),
                                "dealerid" => query.OrderBy(x => x.DealerId),
                                "enddate" => query.OrderBy(x => x.EndDate),
                                "startdate" => query.OrderBy(x => x.StartDate),
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
                        item.Dealer = await _context.Dealers.FirstOrDefaultAsync(x => x.RefID == item.DealerId);
                    }

                    return new ListResponse<UserDealer>(data, total, page);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    if (ex.StackTrace != null)
                        Trace.WriteLine(ex.StackTrace);

                    throw;
                }
            }

            public async Task<UserDealer> GetByIdAsync(long id)
            {
                try
                {
                    return await _context.UserDealers.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    if (ex.StackTrace != null)
                        Trace.WriteLine(ex.StackTrace);

                    throw;
                }
            }

        Task<UserDealer> IServiceAsync<UserDealer>.ChangePassword(ChangePassword obj, long id)
        {
            throw new NotImplementedException();
        }

        Task<List<T>> IServiceAsync<UserDealer>.GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId)
        {
            throw new NotImplementedException();
        }
    }
}
