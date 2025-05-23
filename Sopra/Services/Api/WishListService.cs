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
    public class WishListService : IServiceAsync<WishList>
    {
        private readonly EFContext _context;
        private readonly SearchOptimizationService _svc;

        public WishListService(EFContext context, SearchOptimizationService svc)
        {
            _context = context;
            _svc = svc;
        }

        public Task<WishList> ChangePassword(ChangePassword obj, long id)
        {
            throw new NotImplementedException();
        }

        public async Task<WishList> CreateAsync(WishList data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.WishLists.FirstOrDefaultAsync(x => x.ProductId == data.ProductId && x.UserId == data.UserId);
                if (obj == null)
                {
                    await _context.WishLists.AddAsync(data);
                    await _context.SaveChangesAsync();

                    // Check Validate
                    await Utility.AfterSave(_context, "WishList", data.ID, "Add");
                    await dbTrans.CommitAsync();
                    return data;
                } else
                {
                    if(obj.IsDeleted == false)
                    {
                        obj.IsDeleted = true;
                        _context.WishLists.Update(obj);
                        await _context.SaveChangesAsync();
                    } else
                    {
                        obj.IsDeleted = false;
                        _context.WishLists.Update(obj);
                        await _context.SaveChangesAsync();
                    }
                    await dbTrans.CommitAsync();
                    return obj;
                }
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

        public Task<bool> DeleteAsync(long id, long userID)
        {
            throw new NotImplementedException();
        }

        public Task<WishList> EditAsync(WishList data)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId) where T : class
        {
            throw new NotImplementedException();
        }

        public async Task<ListResponse<WishList>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.WishLists where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Equals(search)
                        || x.UserId.ToString().Equals(search)
                        || x.Type.Contains(search)
                        || x.ProductId.ToString().Contains(search)
                        //|| x.ProductDetail.Name.Contains(search)
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
                                "userid" => query.Where(x => x.UserId.ToString().Equals(value)),
                                "type" => query.Where(x => x.Type.Contains(value)),
                                "productid" => query.Where(x => x.ProductId.ToString().Contains(value)),
                                //"productname" => query.Where(x => x.ProductDetail.Name.Contains(value)),
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
                            "productid" => query.OrderByDescending(x => x.ProductId),
                            //"productname" => query.OrderByDescending(x => x.ProductDetail.Name),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "userid" => query.OrderBy(x => x.UserId),
                            "type" => query.OrderBy(x => x.Type),
                            "productid" => query.OrderBy(x => x.ProductId),
                            //"productname" => query.OrderBy(x => x.ProductDetail.Name),
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
                    
                    item.ProductDetail = await _context.ProductDetails2.Select(x => new Product
                    {
                        ID = (long)x.OriginID,
                        RefID = x.RefID,
                        Name =  x.Name,
                        Image = x.Image,
                        Price = x.Price,
                        QtyPack = x.QtyPack,
                        RealImage = x.RealImage,
                        Type = x.Type,
                        ClosuresID = x.ClosuresID,
                        LidsID= x.LidsID,
                        StockIndicator = x.StockIndicator
                    }).FirstOrDefaultAsync(x => x.RefID == item.ProductId && x.Type == (item.Type == "closures" ? "closure" : item.Type));
                    if(item.ProductDetail != null)
                    {
                        var countcolor = await _svc.GetCountColor(item.ProductDetail.Type, item.ProductDetail.Name);
                        item.ProductDetail.CountColor = Convert.ToInt32(countcolor);
                    }
                    
                }

                return new ListResponse<WishList>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<WishList> GetByIdAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        Task<List<T>> IServiceAsync<WishList>.GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId)
        {
            throw new NotImplementedException();
        }
    }
}
