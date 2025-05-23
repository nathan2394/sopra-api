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
    public class PaymentService : IServiceAsync<Payment>
    {
        private readonly EFContext _context;

        public PaymentService(EFContext context)
        {
            _context = context;
        }

        public async Task<Payment> CreateAsync(Payment data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Payments.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Payment", data.ID, "Add");

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
                var obj = await _context.Payments.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Payment", id, "Delete");

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

        public async Task<Payment> EditAsync(Payment data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Payments.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.PaymentNo = data.PaymentNo;
                obj.Type = data.Type;
                obj.TransDate = data.TransDate;
                obj.CustomersID = data.CustomersID;
                obj.InvoicesID = data.InvoicesID;
                obj.Netto = data.Netto;
                obj.BankRef = data.BankRef;
                obj.BankTime = data.BankTime;
                obj.AmtReceive = data.AmtReceive;
                obj.Status = data.Status;
                obj.ReasonsID = data.ReasonsID;
                obj.Note = data.Note;
                obj.Username = data.Username;
                obj.UsernameCancel = data.UsernameCancel;
                obj.CompaniesID = data.CompaniesID;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Payment", data.ID, "Edit");

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


        public async Task<ListResponse<Payment>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Payments where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Contains(search)
                        || x.PaymentNo.Contains(search)
                        || x.Type.Contains(search)
                        || x.TransDate.ToString().Contains(search)
                        || x.CustomersID.ToString().Contains(search)
                        || x.InvoicesID.ToString().Contains(search)
                        || x.Netto.ToString().Contains(search)
                        || x.BankRef.ToString().Contains(search)
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
                                "paymentno" => query.Where(x => x.PaymentNo.Contains(value)),
                                "type" => query.Where(x => x.Type.Contains(value)),
                                "transdate" => query.Where(x => x.Type.ToString().Contains(value)),
                                "customersid" => query.Where(x => x.CustomersID.ToString().Contains(value)),
                                "invoicesid" => query.Where(x => x.InvoicesID.ToString().Contains(value)),
                                "netto" => query.Where(x => x.Netto.ToString().Contains(value)),
                                "bankref" => query.Where(x => x.BankRef.Contains(value)),
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
                            "paymentno" => query.OrderByDescending(x => x.PaymentNo),
                            "type" => query.OrderByDescending(x => x.Type),
                            "transdate" => query.OrderByDescending(x => x.TransDate),
                            "customersid" => query.OrderByDescending(x => x.CustomersID),
                            "invoicesid" => query.OrderByDescending(x => x.InvoicesID),
                            "netto" => query.OrderByDescending(x => x.Netto),
                            "bankref" => query.OrderByDescending(x => x.BankRef),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "paymentno" => query.OrderBy(x => x.PaymentNo),
                            "type" => query.OrderBy(x => x.Type),
                            "transdate" => query.OrderBy(x => x.TransDate),
                            "customersid" => query.OrderBy(x => x.CustomersID),
                            "invoicesid" => query.OrderBy(x => x.InvoicesID),
                            "netto" => query.OrderBy(x => x.Netto),
                            "bankref" => query.OrderBy(x => x.BankRef),
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

                return new ListResponse<Payment>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Payment> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Payments.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<Payment> ChangePassword(ChangePassword obj, long id) { return null; }

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

