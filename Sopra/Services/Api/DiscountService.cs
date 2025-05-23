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
    public class DiscountService : IServiceAsync<Discount>
    {
        private readonly EFContext _context;

        public DiscountService(EFContext context)
        {
            _context = context;
        }
        public Task<Discount> ChangePassword(ChangePassword obj, long id)
        {
            throw new NotImplementedException();
        }

        public Task<Discount> CreateAsync(Discount data)
        {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteAsync(long id, long userID)
        {
            throw new NotImplementedException();
        }

        public Task<Discount> EditAsync(Discount data)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId) where T : class
        {
            throw new NotImplementedException();
        }

        public async Task<ListResponse<Discount>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Discounts where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Equals(search)
                        || x.Disc.ToString().Equals(search)
                        || x.Amount.ToString().Contains(search)
                        || x.AmountMax.ToString().Contains(search)
                        || x.Type.Contains(search)
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
                                "refid" => query.Where(x => x.RefID.ToString().Equals(value)),
                                "disc" => query.Where(x => x.Disc.ToString().Equals(value)),
                                "type" => query.Where(x => x.Type.Contains(value)),
                                "amount" => query.Where(x => x.Amount.ToString().Contains(value)),
                                "amountmax" => query.Where(x => x.AmountMax.ToString().Contains(value)),
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
                            "disc" => query.OrderByDescending(x => x.Disc),
                            "type" => query.OrderByDescending(x => x.Type),
                            "amount" => query.OrderByDescending(x => x.Amount),
                            "amountmax" => query.OrderByDescending(x => x.AmountMax),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "disc" => query.OrderBy(x => x.Disc),
                            "type" => query.OrderBy(x => x.Type),
                            "amount" => query.OrderBy(x => x.Amount),
                            "amountmax" => query.OrderBy(x => x.AmountMax),
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

                return new ListResponse<Discount>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<Discount> GetByIdAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }
    }
}
