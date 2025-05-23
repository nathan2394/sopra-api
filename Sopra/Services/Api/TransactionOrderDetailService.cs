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
    public class TransactionOrderDetailService : IServiceAsync<TransactionOrderDetail>
    {
        private readonly EFContext _context;

        public TransactionOrderDetailService(EFContext context)
        {
            _context = context;
        }

        public async Task<TransactionOrderDetail> CreateAsync(TransactionOrderDetail data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.TransactionOrderDetails.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "TransactionOrderDetail", data.ID, "Add");

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
                var obj = await _context.TransactionOrderDetails.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "TransactionOrderDetail", id, "Delete");

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

        public async Task<TransactionOrderDetail> EditAsync(TransactionOrderDetail data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.TransactionOrderDetails.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.CompanyID = data.CompanyID;
                obj.OrdersID = data.OrdersID;
                obj.OrderNo = data.OrderNo;
                obj.OrderDate = data.OrderDate;
                obj.CustomerName = data.CustomerName;
                obj.CustomersID = data.CustomersID;
                obj.ProvinceName = data.ProvinceName;
                obj.RegencyName = data.RegencyName;
                obj.DistrictName = data.DistrictName;
                obj.ObjectID = data.ObjectID;
                obj.ProductName = data.ProductName;
                obj.ProductType = data.ProductType;
                obj.Qty = data.Qty;
                obj.Amount = data.Amount;
                obj.Type = data.Type;
                obj.InvoicesID = data.InvoicesID;
                obj.InvoiceNo = data.InvoiceNo;
                obj.PaymentsID = data.PaymentsID;
                obj.PaymentsNo = data.PaymentsNo;
                obj.Status = data.Status;
                obj.LinkedWMS = data.LinkedWMS;
                obj.DealerTier = data.DealerTier;
                obj.OrderStatus = data.OrderStatus;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "TransactionOrderDetail", data.ID, "Edit");

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


        public async Task<ListResponse<TransactionOrderDetail>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.TransactionOrderDetails where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Contains(search)
                        || x.CompanyID.ToString().Contains(search)
                        || x.OrdersID.ToString().Contains(search)
                        || x.OrderNo.ToString().Contains(search)
                        || x.OrderDate.ToString().Contains(search)
                        || x.CustomerName.Contains(search)
                        || x.CustomersID.ToString().Contains(search)
                        || x.ProvinceName.Contains(search)
                        || x.RegencyName.Contains(search)
                        || x.DistrictName.Contains(search)
                        || x.ProductName.Contains(search)
                        || x.ProductType.Contains(search)
                        || x.Qty.ToString().Contains(search)
                        || x.Amount.ToString().Contains(search)
                        || x.Type.ToString().Contains(search)
                        || x.InvoicesID.ToString().Contains(search)
                        || x.InvoiceNo.Contains(search)
                        || x.PaymentsID.ToString().Contains(search)
                        || x.PaymentsNo.Contains(search)
                        || x.Status.Contains(search)
                        || x.LinkedWMS.ToString().Contains(search)
                        || x.DealerTier.Contains(search)
                        || x.OrderStatus.Contains(search)
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
								"companyid" => query.Where(x => x.CompanyID.ToString().Contains(value)),
								"ordersid" => query.Where(x => x.OrdersID.ToString().Contains(value)),
								"orderno" => query.Where(x => x.OrderNo.Contains(value)),
								"orderdate" => query.Where(x => x.OrderDate.ToString().Contains(value)),
								"customername" => query.Where(x => x.CustomerName.Contains(value)),
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
							"companyid" => query.OrderByDescending(x => x.CompanyID),
							"ordersid" => query.OrderByDescending(x => x.OrdersID),
							"orderno" => query.OrderByDescending(x => x.OrderNo),
							"orderdate" => query.OrderByDescending(x => x.OrderDate),
							"customername" => query.OrderByDescending(x => x.CustomerName),
							"customersid" => query.OrderByDescending(x => x.CustomersID),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
							"companyid" => query.OrderBy(x => x.CompanyID),
							"ordersid" => query.OrderBy(x => x.OrdersID),
							"orderno" => query.OrderBy(x => x.OrderNo),
							"orderdate" => query.OrderBy(x => x.OrderDate),
							"customername" => query.OrderBy(x => x.CustomerName),
							"customersid" => query.OrderBy(x => x.CustomersID),
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

                return new ListResponse<TransactionOrderDetail>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<TransactionOrderDetail> GetByIdAsync(long id)
        {
            try
            {
                return await _context.TransactionOrderDetails.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<TransactionOrderDetail> ChangePassword(ChangePassword obj, long id) { return null; }
        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        public Task<TransactionOrderDetail> GetAccIdAsync(long id)
        {
            throw new NotImplementedException();
        }

        public Task<TransactionOrderDetail> GetAccIdAsync(long id, long customerid)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId) where T : class
        {
            throw new NotImplementedException();
        }

        Task<List<T>> IServiceAsync<TransactionOrderDetail>.GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId)
        {
            throw new NotImplementedException();
        }
    }
}

