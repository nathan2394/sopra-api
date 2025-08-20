using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

using Sopra.Helpers;
using Sopra.Entities;
using System.Data;
using System.Collections.Generic;
using Sopra.Responses;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using System.Data.Common;
using Microsoft.VisualBasic;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Sopra.Services
{
    public interface InvoiceBottleInterface
    {
        Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<dynamic> GetByIdAsync(long id);
        Task<ListResponse<dynamic>> GetByOrderIdAsync(long id);
        Task<Invoice> CreateAsync(InvoiceBottle data, int userId);
        Task<Invoice> EditAsync(InvoiceBottle data, int userId);
        Task<bool> DeleteAsync(long id, int reason, int userId);
    }

    public class InvoiceBottleService : InvoiceBottleInterface
    {
        private readonly EFContext _context;

        public InvoiceBottleService(EFContext context)
        {
            _context = context;
        }

        private void ValidateSave(InvoiceBottle data)
        {
            // STATUS
            if (data.Status == "CANCEL")
            {
                throw new ArgumentException("Can't take any action, Invoice is already cancelled.");
            }

            // CUSTOMER
            if (data.CustomersID <= 0)
            {
                throw new ArgumentException("Customer must not be empty.");
            }

            // ORDER
            if (data.OrdersID <= 0)
            {
                throw new ArgumentException("Order ID must not be empty.");
            }

            // INVOICE NETTO
            if (data.Netto <= 0)
            {
                throw new ArgumentException("Invoice Netto must not be empty.");
            }

            if (!data.PaymentMethod.HasValue || data.PaymentMethod <= 0)
            {
                throw new ArgumentException("Payment Method must not be empty.");
            }

            // DEPOSIT
            if (data.PaymentMethod == 3)
            {
                if (data.Refund <= 0)
                {
                    throw new ArgumentException("Deposit Amount must not be empty while in Deposit method.");
                }

                // var getDepositAmount = _context.Deposit
                //     .Where(d => d.CustomersID)
                //     .Select(SUM(d.Amount));

                // if (getDepositAmount < data.Refund)
                // {
                //     throw new ArgumentException("Deposit balance is lower then the existing Deposit Amount.");
                // }
            }
            else
            {
                // DUE DATE
                if (data.DueDate < Utility.getCurrentTimestamps() || !data.DueDate.HasValue)
                {
                    if (data.DueDate <= Utility.getCurrentTimestamps())
                    {
                        throw new ArgumentException("Due Date must be greater than or equal to current time.");
                    }
                    else if (!data.DueDate.HasValue)
                    {
                        throw new ArgumentException("Due Date must not be empty.");
                    }
                }
            }
        }

        private async Task<string> GenerateVoucherNo(long? companyId)
        {
            var currentYear = DateTime.Now.Year;
            var currentYearString = DateTime.Now.ToString("yy");
            var company = companyId == 1 ? "SOPRA" : "TRASS";

            // Get the last ID from the appropriate table for the current year
            var lastId = await _context.Invoices
                .Where(x => x.TransDate.HasValue && x.TransDate.Value.Year == currentYear)
                .OrderByDescending(x => x.ID)
                .Select(x => x.ID)
                .FirstOrDefaultAsync();

            var nextNumber = lastId + 1;
            var docType = "INV";

            return $"{company}/{docType}/N/{currentYearString}/{nextNumber:D5}";
        }

        public async Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                //Get data from context Orders join to Users
                //Users join to Customers 
                var query = from a in _context.Invoices
                            join c in _context.Users on a.CustomersID equals c.RefID
                            join d in _context.Customers on c.CustomersID equals d.RefID into customerJoin
                            from d in customerJoin.DefaultIfEmpty()
                            join p in _context.Payments.Where(p => p.Status != "CANCEL") on a.ID equals p.InvoicesID into paymentJoin
                            from p in paymentJoin.DefaultIfEmpty()
                            where a.IsDeleted == false
                            select new { Invoice = a, Customer = d, Payment = p };

                var dateBetween = "";

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Invoice.RefID.ToString().Equals(search)
                        || x.Invoice.InvoiceNo.Contains(search)
                        || x.Invoice.TransDate.ToString().Contains(search)
                        || x.Invoice.CustomersID.ToString().Equals(search)
                        || x.Invoice.VANum.Contains(search)
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
                            if (fieldName == "transdate") dateBetween = Convert.ToString(value);
                            query = fieldName switch
                            {
                                "refid" => query.Where(x => x.Invoice.RefID.ToString().Equals(value)),
                                "invoiceno" => query.Where(x => x.Invoice.InvoiceNo.Contains(value)),
                                "invoicestatus" => query.Where(x => x.Invoice.Status.Contains(value)),
                                "customersid" => query.Where(x => x.Invoice.CustomersID.ToString().Equals(value)),
                                "companyid" => query.Where(x => x.Invoice.CompaniesID.ToString().Equals(value)),
                                "ispaid" => value == "0"
                                    ? query.Where(x => x.Payment == null)
                                    : value == "1"
                                        ? query.Where(x => x.Payment != null)
                                        : query,
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
                            "refid" => query.OrderByDescending(x => x.Invoice.RefID),
                            "invoiceno" => query.OrderByDescending(x => x.Invoice.InvoiceNo),
                            "invoicestatus" => query.OrderByDescending(x => x.Invoice.Status),
                            "transdate" => query.OrderByDescending(x => x.Invoice.TransDate),
                            "customersid" => query.OrderByDescending(x => x.Invoice.CustomersID),
                            "duedate" => query.OrderByDescending(x => x.Invoice.DueDate),
                            "dateup" => query.OrderByDescending(x => x.Invoice.DateUp),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.Invoice.RefID),
                            "invoiceno" => query.OrderBy(x => x.Invoice.InvoiceNo),
                            "invoicestatus" => query.OrderBy(x => x.Invoice.Status),
                            "transdate" => query.OrderBy(x => x.Invoice.TransDate),
                            "customersid" => query.OrderBy(x => x.Invoice.CustomersID),
                            "duedate" => query.OrderBy(x => x.Invoice.DueDate),
                            "dateup" => query.OrderBy(x => x.Invoice.DateUp),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.Invoice.TransDate);
                }

                if (dateBetween != "")
                {
                    var dateSplit = dateBetween.Split("&", StringSplitOptions.RemoveEmptyEntries);
                    var start = Convert.ToDateTime(dateSplit[0].Trim());
                    var end = Convert.ToDateTime(dateSplit[1].Trim());
                    query = query.Where(x => x.Invoice.TransDate >= start && x.Invoice.TransDate <= end);

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

                // Map to DTO
                var resData = data.Select(x =>
                {
                    return new
                    {
                        ID = x.Invoice.ID,
                        RefID = x.Invoice.RefID,
                        OrdersID = x.Invoice.OrdersID,
                        VoucherNo = x.Invoice.InvoiceNo,
                        TransDate = x.Invoice.TransDate,
                        CustomerName = x.Customer?.Name ?? "",
                        PaymentMethod = x.Invoice.PaymentMethod,

                        Refund = x.Invoice.Refund ?? 0,
                        Bill = x.Invoice.Bill ?? 0,
                        Netto = x.Invoice.Netto ?? 0,

                        FlagInv = x.Invoice.FlagInv ?? 0,
                        VANum = x.Invoice.VANum ?? "-",
                        DueDate = x.Invoice.DueDate,

                        HandleBy = x.Invoice.Username,
                        Status = x.Invoice.Status,

                        Progress = x.Invoice.Status == "ACTIVE"
                        ? (x.Payment == null
                        ? (x.Invoice.FlagInv == 1 ? "requested" : "invoiced")
                        : "paid")
                        : "cancel"
                    };
                })
                .Distinct()
                .ToList();

                return new ListResponse<dynamic>(resData, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<dynamic> GetByIdAsync(long id)
        {
            try
            {
                var data = await _context.Invoices
                .Where(x => x.ID == id && x.IsDeleted == false)
                .Select(x => new
                {
                    Invoice = x,
                    CustomerName = _context.Users
                        .Where(u => u.RefID == x.CustomersID)
                        .Join(_context.Customers, u => u.CustomersID, c => c.RefID, (u, c) => c.Name)
                        .FirstOrDefault() ?? ""
                })
                .FirstOrDefaultAsync();

                if (data == null) return null;

                var resData = new
                {
                    ID = data.Invoice.ID,
                    RefID = data.Invoice.RefID,
                    OrdersID = data.Invoice.OrdersID,
                    VoucherNo = data.Invoice.InvoiceNo,
                    TransDate = data.Invoice.TransDate,
                    CustomerID = data.Invoice.CustomersID,
                    CustomerName = data.CustomerName,
                    PaymentMethod = data.Invoice.PaymentMethod,
                    CompanyID = data.Invoice.CompaniesID,

                    Refund = data.Invoice.Refund ?? 0,
                    Bill = data.Invoice.Bill ?? 0,
                    Netto = data.Invoice.Netto ?? 0,

                    FlagInv = data.Invoice.FlagInv ?? 0,
                    VANum = data.Invoice.VANum ?? "-",
                    DueDate = data.Invoice.DueDate,

                    HandleBy = data.Invoice.Username,
                    Status = data.Invoice.Status
                };

                return resData;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ListResponse<dynamic>> GetByOrderIdAsync(long id)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

                var data = from i in _context.Invoices
                    join p in _context.Payments.Where(p => p.Status != "CANCEL") on i.ID equals p.InvoicesID into paymentJoin
                    from p in paymentJoin.DefaultIfEmpty()
                    where i.OrdersID == id && i.IsDeleted == false
                    select new { Invoice = i, Payment = p };

                var resData = data.Select(x => new
                {
                    ID = x.Invoice.ID,
                    RefID = x.Invoice.RefID,
                    Type = x.Invoice.Type,
                    InvoiceNo = x.Invoice.InvoiceNo,
                    TransDate = x.Invoice.TransDate,
                    PaymentMethod = x.Invoice.PaymentMethod,

                    Refund = x.Invoice.Refund,
                    Bill = x.Invoice.Bill,
                    Netto = x.Invoice.Netto,

                    FlagInv = x.Invoice.FlagInv,
                    DueDate = x.Invoice.DueDate,

                    Status = x.Invoice.Status,

                    Progress = x.Invoice.Status == "ACTIVE"
                    ? (x.Payment == null
                    ? (x.Invoice.FlagInv == 1 ? "requested" : "invoiced")
                    : "paid")
                    : "cancel"
                }).ToList();

                return new ListResponse<dynamic>(resData, resData?.Count ?? 0, 0);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Invoice> CreateInvoiceAsync(InvoiceBottle data, int userId)
        {
            try
            {
                Trace.WriteLine($"Creating invoice with request = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                ValidateSave(data);

                var newInvoiceNo = await GenerateVoucherNo(data.CompanyID);
                string vaNum = null;
                string custNum = null;

                var isDeposit = data.PaymentMethod == 3;
                vaNum = "Phone is Empty";

                var getCustomer = _context.Users
                    .Where(u => u.RefID == data.CustomersID)
                    .Join(_context.Customers,
                        us => us.CustomersID,
                        c => c.RefID,
                        (us, c) => new
                        {
                            Name = c.Name,
                            custNum = c.Mobile1
                        })
                    .FirstOrDefault();

                if (!isDeposit)
                {
                    if (getCustomer != null && getCustomer.custNum != null)
                    {
                        var vaCode = data.CompanyID == 1 ? "14767" : "13438";
                        vaNum = $"{vaCode}{getCustomer.custNum}";
                    }
                }
                else
                {
                    vaNum = null;
                }

                custNum = getCustomer?.custNum;

                var invoice = new Invoice
                {
                    RefID = data.RefID,
                    OrdersID = data.OrdersID,
                    TransDate = Utility.getCurrentTimestamps(),
                    CustomersID = data.CustomersID,
                    CompaniesID = data.CompanyID,
                    Username = data.CreatedBy,
                    PaymentMethod = data.PaymentMethod,
                    Refund = data.Refund,
                    Bill = data.Bill,
                    Netto = data.Netto,
                    Type = data.Type,
                    Status = data.Status,
                    InvoiceNo = newInvoiceNo,
                    DueDate = data.DueDate,
                    FlagInv = data.FlagInv,
                    VANum = vaNum,
                    CustNum = custNum,
                    UserIn = userId
                };

                await _context.Invoices.AddAsync(invoice);
                await _context.SaveChangesAsync();

                var invoiceLog = new UserLog
                {
                    ObjectID = invoice.ID,
                    ModuleID = 2, // Invoice module
                    UserID = userId,
                    Description = $"Invoice {invoice.InvoiceNo} was created.",
                    TransDate = Utility.getCurrentTimestamps(),
                    DateIn = Utility.getCurrentTimestamps(),
                    UserIn = userId,
                    UserUp = 0,
                    IsDeleted = false
                };

                await _context.UserLogs.AddAsync(invoiceLog);

                await Utility.AfterSave(_context, "InvoiceBottle", invoice.ID, "Add");
                await _context.SaveChangesAsync();

                Trace.WriteLine($"Invoice created successfully with ID = {invoice.ID}");

                return invoice;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"Error creating invoice, request = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                throw;
            }
        }

        public async Task<Invoice> CreateAsync(InvoiceBottle data, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await CreateInvoiceAsync(data, userId);
                await dbTrans.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"error save data invoice, payload = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                await dbTrans.RollbackAsync();
                Trace.WriteLine($"rollback db");
                throw;
            }
        }

        public async Task<Invoice> EditAsync(InvoiceBottle data, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            
            try
            {
                Trace.WriteLine($"Edit invoice with request = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                ValidateSave(data);

                var getInvoice = _context.Invoices
                    .Where(i => i.ID == data.ID)
                    .FirstOrDefault();

                if (getInvoice != null)
                {
                    if (getInvoice.FlagInv != data.FlagInv)
                    {
                        getInvoice.Bill = data.Bill;
                        getInvoice.Refund = data.Refund;
                        getInvoice.Netto = data.Netto;

                        getInvoice.DueDate = data.DueDate;

                        getInvoice.FlagInv = data.FlagInv;

                        if (data.FlagInv == 1)
                        {
                            // Notify Request Payment has been activated
                            // Invoice was request to be paid in {Utility.getCurrentTimestamps()} with due {getInvoice.DueDate}.
                        }
                        else
                        {
                            // Notify Request Payment has been deactivated
                            // Invoice to be hold from request payment.
                        }
                    }

                    if (getInvoice.PaymentMethod != data.PaymentMethod)
                    {
                        getInvoice.PaymentMethod = data.PaymentMethod;
                        // Notify Payment Method has been changed
                        // Payment method changed to {getInvoice.PaymentMethod}
                    }

                    getInvoice.DateUp = Utility.getCurrentTimestamps();
                    getInvoice.UserUp = userId;
                }

                _context.Invoices.Update(getInvoice);
                await _context.SaveChangesAsync();

                var invoiceLog = new UserLog
                {
                    ObjectID = getInvoice.ID,
                    ModuleID = 2, // Invoice module
                    UserID = userId,
                    Description = $"Invoice was updated.",
                    TransDate = Utility.getCurrentTimestamps(),
                    DateIn = Utility.getCurrentTimestamps(),
                    UserIn = userId,
                    UserUp = 0,
                    IsDeleted = false
                };

                await _context.UserLogs.AddAsync(invoiceLog);

                await Utility.AfterSave(_context, "InvoiceBottle", getInvoice.ID, "Add");
                await _context.SaveChangesAsync();

                Trace.WriteLine($"Invoice created successfully with ID = {getInvoice.ID}");

                await dbTrans.CommitAsync();

                return getInvoice;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"Error editing invoice, request = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                await dbTrans.RollbackAsync();

                throw;
            }
        }
        
        public async Task<bool> DeleteAsync(long id, int reason, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();

            try
            {
                var obj = await _context.Invoices.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false && x.Status == "ACTIVE");
                if (obj == null) return false;

                var getUser = await _context.Users.FirstOrDefaultAsync(x => x.ID == userId);
                if (getUser != null)
                {
                    obj.Status = "CANCEL";
                    obj.ReasonsID = reason;
                    obj.DateUp = Utility.getCurrentTimestamps();
                    obj.UserUp = userId;
                    obj.UsernameCancel = $"{getUser.FirstName} {getUser.LastName}";

                    await _context.SaveChangesAsync();

                    await Utility.AfterSave(_context, "InvoiceBottle", id, "Delete");

                    var logs = new UserLog
                    {
                        ObjectID = id,
                        ModuleID = 2, // Invoice module
                        UserID = userId,
                        Description = "Invoice was cancelled.",
                        TransDate = Utility.getCurrentTimestamps(),
                        DateIn = Utility.getCurrentTimestamps(),
                        UserIn = userId,
                        UserUp = 0,
                        IsDeleted = false
                    };

                    await _context.UserLogs.AddAsync(logs);
                    await _context.SaveChangesAsync();

                    await dbTrans.CommitAsync();

                    return true;
                }
                else
                {
                    return false;
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
    }
}