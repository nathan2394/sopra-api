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
    public class VoucherService : IServiceAsync<Voucher>
    {
        private readonly EFContext _context;

        public VoucherService(EFContext context)
        {
            _context = context;
        }

        public async Task<Voucher> CreateAsync(Voucher data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Vouchers.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Voucher", data.ID, "Add");

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
                var obj = await _context.Vouchers.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Voucher", id, "Delete");

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

        public async Task<Voucher> EditAsync(Voucher data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Vouchers.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.VoucherNo = data.VoucherNo;
                obj.Amount = data.Amount;
                obj.ExpiredDate = data.ExpiredDate;
                obj.OrdersID = data.OrdersID;
                obj.Disc = data.Disc;
                obj.VoucherUsage = data.VoucherUsage;
                obj.MinOrder = data.MinOrder;
                obj.Status = data.Status;
                obj.FlagOrder = data.FlagOrder;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Voucher", data.ID, "Edit");

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


        public async Task<ListResponse<Voucher>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Vouchers where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Contains(search)
                        || x.VoucherNo.Contains(search)
                        || x.Amount.ToString().Contains(search)
                        || x.ExpiredDate.ToString().Contains(search)
                        || x.OrdersID.ToString().Contains(search)
                        || x.Disc.ToString().Contains(search)
                        || x.VoucherUsage.ToString().Contains(search)
                        || x.MinOrder.ToString().Contains(search)
                        || x.Status.Contains(search)
                        || x.FlagOrder.Contains(search)
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
								"voucherno" => query.Where(x => x.VoucherNo.Contains(value)),
								"amount" => query.Where(x => x.Amount.ToString().Contains(value)),
								"expireddate" => query.Where(x => x.ExpiredDate.ToString().Contains(value)),
								"ordersid" => query.Where(x => x.OrdersID.ToString().Contains(value)),
								"disc" => query.Where(x => x.Disc.ToString().Contains(value)),
								"voucherusage" => query.Where(x => x.VoucherUsage.ToString().Contains(value)),
								"minorder" => query.Where(x => x.MinOrder.ToString().Contains(value)),
								"status" => query.Where(x => x.Status.Contains(value)),
								"flagorder" => query.Where(x => x.FlagOrder.Contains(value)),
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
							"voucherno" => query.OrderByDescending(x => x.VoucherNo),
							"amount" => query.OrderByDescending(x => x.Amount),
							"expireddate" => query.OrderByDescending(x => x.ExpiredDate),
							"ordersid" => query.OrderByDescending(x => x.OrdersID),
							"disc" => query.OrderByDescending(x => x.Disc),
							"voucherusage" => query.OrderByDescending(x => x.VoucherUsage),
							"minorder" => query.OrderByDescending(x => x.MinOrder),
							"status" => query.OrderByDescending(x => x.Status),
							"flagorder" => query.OrderByDescending(x => x.FlagOrder),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
							"voucherno" => query.OrderBy(x => x.VoucherNo),
							"amount" => query.OrderBy(x => x.Amount),
							"expireddate" => query.OrderBy(x => x.ExpiredDate),
							"ordersid" => query.OrderBy(x => x.OrdersID),
							"disc" => query.OrderBy(x => x.Disc),
							"voucherusage" => query.OrderBy(x => x.VoucherUsage),
							"minorder" => query.OrderBy(x => x.MinOrder),
							"status" => query.OrderBy(x => x.Status),
							"flagorder" => query.OrderBy(x => x.FlagOrder),
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

                return new ListResponse<Voucher>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Voucher> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Vouchers.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<Voucher> ChangePassword(ChangePassword obj, long id) { return null; }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        public Task<Voucher> GetAccIdAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<Voucher> GetAccIdAsync(long id, long customerid)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId) where T : class
        {
            throw new NotImplementedException();
        }

        Task<List<T>> IServiceAsync<Voucher>.GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId)
        {
            throw new NotImplementedException();
        }
    }
}

