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
    public class InvoiceService : IServiceAsync<Invoice>
    {
        private readonly EFContext _context;

        public InvoiceService(EFContext context)
        {
            _context = context;
        }

        public async Task<Invoice> CreateAsync(Invoice data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Invoices.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Invoice", data.ID, "Add");

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
                var obj = await _context.Invoices.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Invoice", id, "Delete");

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

        public async Task<Invoice> EditAsync(Invoice data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Invoices.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.OrdersID = data.OrdersID;
                obj.PaymentMethod = data.PaymentMethod;
                obj.InvoiceNo = data.InvoiceNo;
                obj.Type = data.Type;
                obj.Netto = data.Netto;
                obj.CustomersID = data.CustomersID;
                obj.TransDate = data.TransDate;
                obj.Status = data.Status;
                obj.VANum = data.VANum;
                obj.CustNum = data.CustNum;
                obj.FlagInv = data.FlagInv;
                obj.ReasonsID = data.ReasonsID;
                obj.Note = data.Note;
                obj.Refund = data.Refund;
                obj.Bill = data.Bill;
                obj.PICInv = data.PICInv;
                obj.BankName = data.BankName;
                obj.AccountNumber = data.AccountNumber;
                obj.AccountName = data.AccountName;
                obj.TransferDate = data.TransferDate;
                obj.BankRef = data.BankRef;
                obj.TransferAmount = data.TransferAmount;
                obj.Username = data.Username;
                obj.UsernameCancel = data.UsernameCancel;
                obj.XenditID = data.XenditID;
                obj.XenditBank = data.XenditBank;
                obj.PDFFile = data.PDFFile;
                obj.DueDate = data.DueDate;
                obj.CompaniesID = data.CompaniesID;
                obj.SentWaCounter = data.SentWaCounter;
                obj.WASentTime = data.WASentTime;
                obj.FutureDateStatus = data.FutureDateStatus;
                obj.CreditStatus = data.CreditStatus;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Invoice", data.ID, "Edit");

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


        public async Task<ListResponse<Invoice>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Invoices where a.IsDeleted == false select a;

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Contains(search)
                        || x.OrdersID.ToString().Contains(search)
                        || x.PaymentMethod.ToString().Contains(search)
                        || x.InvoiceNo.Contains(search)
                        || x.Type.Contains(search)
                        || x.Netto.ToString().Contains(search)
                        || x.CustomersID.ToString().Contains(search)
                        || x.TransDate.ToString().Contains(search)
                        || x.Status.Contains(search)
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
                                "ordersid" => query.Where(x => x.OrdersID.ToString().Contains(value)),
                                "paymentmethod" => query.Where(x => x.PaymentMethod.ToString().Contains(value)),
                                "invoiceno" => query.Where(x => x.InvoiceNo.Contains(value)),
                                "type" => query.Where(x => x.Type.Contains(value)),
                                "netto" => query.Where(x => x.Netto.ToString().Contains(value)),
                                "customersid" => query.Where(x => x.CustomersID.ToString().Contains(value)),
                                "transdate" => query.Where(x => x.TransDate.ToString().Contains(value)),
                                "status" => query.Where(x => x.Status.Contains(value)),
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
                            "ordersid" => query.OrderByDescending(x => x.OrdersID),
                            "paymentmethod" => query.OrderByDescending(x => x.PaymentMethod),
                            "invoiceno" => query.OrderByDescending(x => x.InvoiceNo),
                            "type" => query.OrderByDescending(x => x.Type),
                            "netto" => query.OrderByDescending(x => x.Netto),
                            "customersid" => query.OrderByDescending(x => x.CustomersID),
                            "transdate" => query.OrderByDescending(x => x.TransDate),
                            "status" => query.OrderByDescending(x => x.Status),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "ordersid" => query.OrderBy(x => x.OrdersID),
                            "paymentmethod" => query.OrderBy(x => x.PaymentMethod),
                            "invoiceno" => query.OrderBy(x => x.InvoiceNo),
                            "type" => query.OrderBy(x => x.Type),
                            "netto" => query.OrderBy(x => x.Netto),
                            "customersid" => query.OrderBy(x => x.CustomersID),
                            "transdate" => query.OrderBy(x => x.TransDate),
                            "status" => query.OrderBy(x => x.Status),
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

                return new ListResponse<Invoice>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Invoice> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Invoices.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<Invoice> ChangePassword(ChangePassword obj, long id) { return null; }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId) where T : class
        {
            throw new NotImplementedException();
        }

        public Task<List<T>> GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId) where T : class
        {
            throw new NotImplementedException();
        }
    }
}

