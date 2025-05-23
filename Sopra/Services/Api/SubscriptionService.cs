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
    public class SubscriptionService : IServiceAsync<Subscription>
    {
        private readonly EFContext _context;

        public SubscriptionService(EFContext context)
        {
            _context = context;
        }

        public async Task<Subscription> CreateAsync(Subscription data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Subscriptions.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Subscription", data.ID, "Add");

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

        public async Task<bool>  DeleteAsync(long id, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Subscriptions.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = userID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Subscription", id, "Delete");

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

        public async Task<Subscription> EditAsync(Subscription data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Subscriptions.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.OrdersID = data.OrdersID;

                obj.Status = data.Status;
                obj.SubscriptionDate = data.SubscriptionDate;

                obj.SubscriptionType = data.SubscriptionType;
                obj.Type = data.Type;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Subscription", data.ID, "Edit");

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

        public async Task<ListResponse<Subscription>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Subscriptions where a.IsDeleted == false select a;
                query = query.Include(x => x.Orders);

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.OrdersID.ToString().Contains(search)
                        || x.Status.ToString().Contains(search)
                        || x.Type.Contains(search)
                        || x.SubscriptionType.Contains(search));

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
                                "ordersid" => query.Where(x => x.OrdersID.ToString().Contains(value)),
                                "status" => query.Where(x => x.Status.ToString().Contains(value)),
                                "type" => query.Where(x => x.Type.Contains(value)),
                                "subscriptiontype" => query.Where(x => x.SubscriptionType.Contains(value)),
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
                            "ordersid" => query.OrderByDescending(x => x.OrdersID),
                            "status" => query.OrderByDescending(x => x.Status),
                            "type" => query.OrderByDescending(x => x.Type),
                            "subscriptiontype" => query.OrderByDescending(x => x.SubscriptionType),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "ordersid" => query.OrderBy(x => x.OrdersID),
                            "status" => query.OrderBy(x => x.Status),
                            "type" => query.OrderBy(x => x.Type),
                            "subscriptiontype" => query.OrderBy(x => x.SubscriptionType),
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

                return new ListResponse<Subscription>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Subscription> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Subscriptions.AsNoTracking().Include(x => x.Orders).FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        Task<Subscription> IServiceAsync<Subscription>.ChangePassword(ChangePassword obj, long id)
        {
            throw new NotImplementedException();
        }
        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        public Task<Subscription> GetAccIdAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<Subscription> GetAccIdAsync(long id, long customerid)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId) where T : class
        {
            throw new NotImplementedException();
        }

        Task<List<T>> IServiceAsync<Subscription>.GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId)
        {
            throw new NotImplementedException();
        }
    }
}
