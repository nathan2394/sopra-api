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

    public interface CartInterface
    {
        Task<ListResponse<Cart>> GetAllAsync(int limit, int page, int total, string search, string sort,
		string filter, string date);
		Task<Cart> GetByIdAsync(long id);
		Task<Cart> CreateAsync(Cart data);
		Task<Cart> EditAsync(Cart data);
		Task<bool> DeleteAsync(long id, long userID);
        Task<bool> ChangeCart(long id, long userID);
    }
    
    public class CartService : CartInterface
    {
        private readonly EFContext _context;
        private readonly CartDetailService _cart_details;

        public CartService(EFContext context)
        {
            _context = context;
            _cart_details = new CartDetailService(_context);
        }

        public async Task<Cart> CreateAsync(Cart data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Carts.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Cart", data.ID, "Add");

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
                var obj = await _context.Carts.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Cart", id, "Delete");

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

        public async Task<Cart> EditAsync(Cart data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Carts.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.CustomersID = data.CustomersID;
                obj.PromosID = data.PromosID;
                obj.TypePromo = data.TypePromo;
                obj.Status = data.Status;

                obj.UserUp = data.UserUp;
                obj.DateUp = Utility.getCurrentTimestamps();

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Cart", data.ID, "Edit");

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
        
        public async Task<ListResponse<Cart>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Carts where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Equals(search)
                        || x.CustomersID.ToString().Equals(search)
                        || x.PromosID.ToString().Equals(search)
                        || x.TypePromo.Contains(search)
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
                                "refid" => query.Where(x => x.RefID.ToString().Equals(value)),
                                "customersid" => query.Where(x => x.CustomersID.ToString().Equals(value)),
                                "promosid" => query.Where(x => x.PromosID.ToString().Equals(value)),
                                "typepromo" => query.Where(x => x.TypePromo.Contains(value)),
                                "status" => query.Where(x => x.Status.ToString().Contains(value)),
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
                            "customersid" => query.OrderByDescending(x => x.CustomersID),
                            "promosid" => query.OrderByDescending(x => x.PromosID),
                            "typepromo" => query.OrderByDescending(x => x.TypePromo),
                            "status" => query.OrderByDescending(x => x.Status),
                            "dateup" => query.OrderByDescending(x => x.DateUp),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                             "customersid" => query.OrderBy(x => x.CustomersID),
                            "promosid" => query.OrderBy(x => x.PromosID),
                            "typepromo" => query.OrderBy(x => x.TypePromo),
                            "status" => query.OrderBy(x => x.Status),
                            "dateup" => query.OrderBy(x => x.DateUp),
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

                return new ListResponse<Cart>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Cart> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Carts.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<bool> ChangeCart(long id, long userid)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                /// 1. Check Cart exists on userid
                /// 2. If Cart exists? remove cart
                var obj = await _context.Carts.FirstOrDefaultAsync(x => x.CustomersID == userid && x.IsDeleted == false);
                if(obj != null)
                {
                    obj.IsDeleted = true;
                    obj.UserUp = userid;
                    obj.DateUp = Utility.getCurrentTimestamps();

                    await _context.SaveChangesAsync();

                    // Check Validate
                    await Utility.AfterSave(_context, "Cart", id, "Delete");
                }
                
                /// 3. Insert new Cart Detail based on new CustomerID
                var carts = await _context.Carts.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                carts.CustomersID = userid;
                carts.DateUp = Utility.getCurrentTimestamps();

                var result = await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                return true;
            }
            catch(Exception e)
            {
                await dbTrans.RollbackAsync();
                var abc = e.ToString();
                throw new Exception(e.ToString());
            }
        }
    }
}

