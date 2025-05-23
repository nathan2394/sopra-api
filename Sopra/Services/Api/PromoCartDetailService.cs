using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
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

namespace Sopra.Services
{
    public class PromoCartDetailService : IServiceAsync<PromoCartDetail>
    {
        private readonly EFContext _context;

        public PromoCartDetailService(EFContext context)
        {
            _context = context;
        }

        public Task<PromoCartDetail> ChangePassword(ChangePassword obj, long id)
        {
            throw new NotImplementedException();
        }

        public async Task<PromoCartDetail> CreateAsync(PromoCartDetail data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.PromoCartDetails.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "PromoCartDetail", data.ID, "Add");

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
                var obj = await _context.PromoCartDetails.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = userID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "PromoCartDetail", id, "Delete");

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

        public async Task<PromoCartDetail> EditAsync(PromoCartDetail data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.PromoCartDetails.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.CartsId = data.CartsId;
                obj.ProductMixId = data.ProductMixId;
                obj.PromoJumboId = data.PromoJumboId;
                obj.ProductJumboId = data.ProductJumboId;
                obj.PromoMixId = data.PromoMixId;
                obj.QtyBox = data.QtyBox;
                obj.Qty = data.Qty;
                obj.ProductPrice = data.ProductPrice;
                obj.Amount = data.Amount;
                obj.FlagPromo = data.FlagPromo;
                obj.IsCheckout = data.IsCheckout;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "PromoCartDetail", data.ID, "Edit");

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

        public Task<PromoCartDetail> GetAccIdAsync(long id, long customerid)
        {
            throw new NotImplementedException();
        }

        public async Task<ListResponse<PromoCartDetail>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.PromoCartDetails where a.IsDeleted == false select a;
                query = query.Include(x => x.PromoMix);
                query = query.Include(x => x.PromoJumbo);
                query = query.Include(x => x.ProductJumbo);
                query = query.Include(x => x.ProductMix);
                query = query.Include(x => x.Carts);

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.CartsId.ToString().Contains(search)
                        || x.ProductMixId.ToString().Contains(search)
                        || x.PromoJumboId.ToString().Contains(search)
                        || x.ProductJumboId.ToString().Contains(search)
                        || x.PromoMixId.ToString().Contains(search)
                        || x.QtyBox.ToString().Contains(search)
                        || x.Qty.ToString().Contains(search)
                        || x.ProductPrice.ToString().Contains(search)
                        || x.Amount.ToString().Contains(search)
                        || x.FlagPromo.ToString().Contains(search)
                        || x.IsCheckout.ToString().Contains(search));

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
                                "cartsid" => query.Where(x => x.CartsId.ToString().Contains(value)),
                                "productmixid" => query.Where(x => x.ProductMixId.ToString().Contains(value)),
                                "promojumboid" => query.Where(x => x.PromoJumboId.ToString().Contains(value)),
                                "productjumboid" => query.Where(x => x.ProductJumboId.ToString().Contains(value)),
                                "promomixid" => query.Where(x => x.PromoMixId.ToString().Contains(value)),
                                "qtybox" => query.Where(x => x.QtyBox.ToString().Contains(value)),
                                "qty" => query.Where(x => x.Qty.ToString().Contains(value)),
                                "productprice" => query.Where(x => x.ProductPrice.ToString().Contains(value)),
                                "amount" => query.Where(x => x.Amount.ToString().Contains(value)),
                                "flagpromo" => query.Where(x => x.FlagPromo.ToString().Contains(value)),
                                "ischeckout" => query.Where(x => x.IsCheckout.ToString().Contains(value)),
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
                            "cartsid" => query.OrderByDescending(x => x.CartsId),
                            "productmixid" => query.OrderByDescending(x => x.ProductMixId),
                            "promojumboid" => query.OrderByDescending(x => x.PromoJumboId),
                            "productjumboid" => query.OrderByDescending(x => x.ProductJumboId),
                            "promomixid" => query.OrderByDescending(x => x.PromoMixId),
                            "qtybox" => query.OrderByDescending(x => x.QtyBox),
                            "qty" => query.OrderByDescending(x => x.Qty),
                            "productprice" => query.OrderByDescending(x => x.ProductPrice),
                            "amount" => query.OrderByDescending(x => x.Amount),
                            "flagpromo" => query.OrderByDescending(x => x.FlagPromo),
                            "ischeckout" => query.OrderByDescending(x => x.IsCheckout),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "cartsid" => query.OrderBy(x => x.CartsId),
                            "productmixid" => query.OrderBy(x => x.ProductMixId),
                            "promojumboid" => query.OrderBy(x => x.PromoJumboId),
                            "productjumboid" => query.OrderBy(x => x.ProductJumboId),
                            "promomixid" => query.OrderBy(x => x.PromoMixId),
                            "qtybox" => query.OrderBy(x => x.QtyBox),
                            "qty" => query.OrderBy  (x => x.Qty),
                            "productprice" => query.OrderBy(x => x.ProductPrice),
                            "amount" => query.OrderBy(x => x.Amount),
                            "flagpromo" => query.OrderBy(x => x.FlagPromo),
                            "ischeckout" => query.OrderBy(x => x.IsCheckout),
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

                return new ListResponse<PromoCartDetail>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<PromoCartDetail> GetByIdAsync(long id)
        {
            try
            {
                return await _context.PromoCartDetails.AsNoTracking()
                        .Include(x => x.PromoMix)
                        .Include(x => x.PromoJumbo)
                        .Include(x => x.ProductJumbo)
                        .Include(x => x.ProductMix)
                        .Include(x => x.Carts)
                        .FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
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

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId) where T : class
        {
            throw new NotImplementedException();
        }
    }
}
