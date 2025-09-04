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

namespace Sopra.Services
{
    public interface PaymentBottleInterface
    {
        Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<dynamic> GetByIdAsync(long id);
        Task<Payment> CreateAsync(PaymentBottle data, int userId);
        Task<Payment> CreateByInvoiceIDAsync(long invoiceId, string bankRef);
        Task<Payment> EditAsync(PaymentBottle data, int userId);
        Task<bool> DeleteAsync(long id, int reason, int userId);
    }

    public class PaymentBottleService : PaymentBottleInterface
    {
        private readonly EFContext _context;

        public PaymentBottleService(EFContext context)
        {
            _context = context;
        }

        private void ValidateSave(PaymentBottle data)
        {
            // STATUS
            if (data.Status == "CANCEL")
            {
                throw new ArgumentException("Can't take any action, Payment is already cancelled.");
            }

            // CUSTOMER
            if (data.CustomersID <= 0)
            {
                throw new ArgumentException("Customer must not be empty.");
            }

            // INVOICE
            if (data.InvoicesID <= 0)
            {
                throw new ArgumentException("Invoice ID must not be empty.");
            }

            // PAYMENT NETTO
            if (data.AmtReceive <= 0 || data.Netto <= 0)
            {
                throw new ArgumentException("Payment Netto must not be empty.");
            }
        }

        private async Task<string> GenerateVoucherNo(long? companyId)
        {
            var currentYear = DateTime.Now.Year;
            var currentYearString = DateTime.Now.ToString("yy");
            var company = companyId == 1 ? "SOPRA" : "TRASS";

            // Get the last ID from the appropriate table for the current year
            var lastId = await _context.Payments
                .Where(x => x.TransDate.HasValue && x.TransDate.Value.Year == currentYear)
                .OrderByDescending(x => x.ID)
                .Select(x => x.ID)
                .FirstOrDefaultAsync();

            var nextNumber = lastId + 1;
            var docType = "PAY";

            return $"{company}/{docType}/N/{currentYearString}/{nextNumber:D5}";
        }

        public async Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                //Get data from context Orders join to Users
                //Users join to Customers 
                var query = from a in _context.Payments
                            join c in _context.Users on a.CustomersID equals c.RefID
                            join d in _context.Customers on c.CustomersID equals d.RefID into customerJoin
                            from d in customerJoin.DefaultIfEmpty()
                            where a.IsDeleted == false
                            select new { Payment = a, Customer = d };

                var dateBetween = "";

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Payment.RefID.ToString().Equals(search)
                        || x.Payment.PaymentNo.Contains(search)
                        || x.Payment.TransDate.ToString().Contains(search)
                        || x.Payment.CustomersID.ToString().Equals(search)
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
                                "refid" => query.Where(x => x.Payment.RefID.ToString().Equals(value)),
                                "paymentno" => query.Where(x => x.Payment.PaymentNo.Contains(value)),
                                "paymentstatus" => query.Where(x => x.Payment.Status.Contains(value)),
                                "customersid" => query.Where(x => x.Payment.CustomersID.ToString().Equals(value)),
                                "companyid" => query.Where(x => x.Payment.CompaniesID.ToString().Equals(value)),
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
                            "refid" => query.OrderByDescending(x => x.Payment.RefID),
                            "paymentno" => query.OrderByDescending(x => x.Payment.PaymentNo),
                            "paymentstatus" => query.OrderByDescending(x => x.Payment.Status),
                            "transdate" => query.OrderByDescending(x => x.Payment.TransDate),
                            "customersid" => query.OrderByDescending(x => x.Payment.CustomersID),
                            "dateup" => query.OrderByDescending(x => x.Payment.DateUp),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.Payment.RefID),
                            "paymentno" => query.OrderBy(x => x.Payment.PaymentNo),
                            "paymentstatus" => query.OrderBy(x => x.Payment.Status),
                            "transdate" => query.OrderBy(x => x.Payment.TransDate),
                            "customersid" => query.OrderBy(x => x.Payment.CustomersID),
                            "dateup" => query.OrderBy(x => x.Payment.DateUp),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.Payment.DateIn);
                }

                if (dateBetween != "")
                {
                    var dateSplit = dateBetween.Split("&", StringSplitOptions.RemoveEmptyEntries);
                    var start = Convert.ToDateTime(dateSplit[0].Trim());
                    var end = Convert.ToDateTime(dateSplit[1].Trim());
                    query = query.Where(x => x.Payment.TransDate >= start && x.Payment.TransDate <= end);
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
                        ID = x.Payment.ID,
                        RefID = x.Payment.RefID,
                        InvoicesID = x.Payment.InvoicesID,
                        VoucherNo = x.Payment.PaymentNo,
                        TransDate = x.Payment.TransDate,
                        CustomerName = x.Customer?.Name ?? "",
                        CustomersID = x.Payment.CustomersID,

                        Netto = x.Payment.Netto ?? 0,
                        AmtReceive = x.Payment.AmtReceive ?? 0,
                        CompanyID = x.Payment.CompaniesID,

                        BankRef = x.Payment.BankRef,
                        BankTime = x.Payment.BankTime,

                        HandleBy = x.Payment.Username,
                        Type = x.Payment.Type,
                        Status = x.Payment.Status
                    };
                })
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
                var data = await _context.Payments
                .Where(x => x.ID == id && x.IsDeleted == false)
                .Select(x => new
                {
                    Payment = x,
                    CustomerName = _context.Users
                        .Where(u => u.RefID == x.CustomersID)
                        .Join(_context.Customers, u => u.CustomersID, c => c.RefID, (u, c) => c.Name)
                        .FirstOrDefault() ?? ""
                })
                .FirstOrDefaultAsync();

                if (data == null) return null;

                var resData = new
                {
                    ID = data.Payment.ID,
                    RefID = data.Payment.RefID,
                    InvoicesID = data.Payment.InvoicesID,
                    PaymentNo = data.Payment.PaymentNo,
                    TransDate = data.Payment.TransDate,
                    CustomersID = data.Payment.CustomersID,

                    CompanyID = data.Payment.CompaniesID,
                    CreatedBy = data.Payment.Username,

                    Netto = data.Payment.Netto ?? 0,
                    AmtReceive = data.Payment.AmtReceive ?? 0,

                    BankTime = data.Payment.BankTime,
                    BankRef = data.Payment.BankRef,


                    Type = data.Payment.Type,
                    Status = data.Payment.Status
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

        public async Task<Payment> CreatePaymentAsync(PaymentBottle data, int userId)
        {
            try
            {
                Trace.WriteLine($"Creating payment with request = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                ValidateSave(data);

                var newPaymentNo = await GenerateVoucherNo(data.CompanyID);

                var payment = new Payment
                {
                    RefID = data.RefID,
                    InvoicesID = data.InvoicesID,
                    CustomersID = data.CustomersID,
                    CompaniesID = data.CompanyID,
                    Username = data.CreatedBy,
                    Netto = data.Netto,

                    TransDate = Utility.currentTimezone(data.TransDate ?? DateTime.UtcNow),
                    BankTime = Utility.currentTimezone(data.BankTime ?? DateTime.UtcNow),

                    BankRef = data.BankRef,
                    AmtReceive = data.AmtReceive,
                    Type = data.Type,
                    Status = data.Status,
                    PaymentNo = newPaymentNo,
                    UserIn = userId
                };

                await _context.Payments.AddAsync(payment);
                await _context.SaveChangesAsync();

                if (data.BankRef == "DEPOSIT")
                {
                    var PaymentDeposit = new Deposit
                    {
                        ObjectID = payment.ID,
                        CustomersID = payment.CustomersID,
                        TotalAmount = payment.Netto * -1,
                        TransDate = Utility.getCurrentTimestamps(),
                        DateIn = Utility.getCurrentTimestamps(),
                    };

                    await _context.Deposit.AddAsync(PaymentDeposit);
                    await _context.SaveChangesAsync();
                }

                var paymentLog = new UserLog
                {
                    ObjectID = payment.ID,
                    ModuleID = 3, // Payment module
                    UserID = userId,
                    Description = $"Payment {payment.PaymentNo} was created.",
                    TransDate = Utility.getCurrentTimestamps(),
                    DateIn = Utility.getCurrentTimestamps(),
                    UserIn = userId,
                    UserUp = 0,
                    IsDeleted = false
                };

                await _context.UserLogs.AddAsync(paymentLog);

                await Utility.AfterSave(_context, "PaymentBottle", payment.ID, "Add");
                await _context.SaveChangesAsync();

                Trace.WriteLine($"Payment created successfully with ID = {payment.ID}");

                return payment;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"Error creating payment, request = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                throw;
            }
        }

        public async Task<Payment> CreateByInvoiceIDAsync(long invoiceId, string bankRef)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                // Check if Invoice are available
                var findInvoice = await _context.Invoices
                .Where(x => x.ID == invoiceId)
                .FirstOrDefaultAsync();

                if (findInvoice == null) return null;

                // Double check if its already Paid
                var findPayment = await _context.Payments
                .Where(x => x.InvoicesID == invoiceId)
                .FirstOrDefaultAsync();

                if (findPayment != null) return null;

                var userId = findInvoice.UserIn;
                var data = new PaymentBottle
                {
                    ID = 0,
                    RefID = 0,
                    InvoicesID = invoiceId,
                    PaymentMethod = findInvoice.PaymentMethod, // VA BCA
                    PaymentNo = "",
                    Type = findInvoice.Type,
                    BankRef = bankRef ?? "",
                    Status = "ACTIVE",
                    Netto = findInvoice.Netto,
                    AmtReceive = findInvoice.Bill,
                    CustomersID = findInvoice.CustomersID,
                    CreatedBy = findInvoice.Username,
                    TransDate = DateTime.UtcNow,
                    BankTime = DateTime.UtcNow,
                    CompanyID = findInvoice.CompaniesID,
                    userId = userId
                };
                
                var result = await CreatePaymentAsync(data, Convert.ToInt32(userId));

                await dbTrans.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"error save data payment by invoice ID (Automated from VA BCA), invoiceId = {invoiceId}");

                await dbTrans.RollbackAsync();
                Trace.WriteLine($"rollback db");
                throw;
            }
        }

        public async Task<Payment> CreateAsync(PaymentBottle data, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var result = await CreatePaymentAsync(data, userId);
                await dbTrans.CommitAsync();
                return result;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"error save data payment, payload = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                await dbTrans.RollbackAsync();
                Trace.WriteLine($"rollback db");
                throw;
            }
        }

        public async Task<Payment> EditAsync(PaymentBottle data, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();

            try
            {
                Trace.WriteLine($"Edit payment with request = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                ValidateSave(data);

                var getPayment = _context.Payments
                    .Where(i => i.ID == data.ID)
                    .FirstOrDefault();

                if (getPayment != null)
                {
                    getPayment.TransDate = Utility.currentTimezone(data.TransDate ?? DateTime.UtcNow);

                    getPayment.DateUp = Utility.getCurrentTimestamps();
                    getPayment.UserUp = userId;
                }

                _context.Payments.Update(getPayment);
                await _context.SaveChangesAsync();

                var paymentLog = new UserLog
                {
                    ObjectID = getPayment.ID,
                    ModuleID = 3, // Payment module
                    UserID = userId,
                    Description = $"Payment was updated.",
                    TransDate = Utility.getCurrentTimestamps(),
                    DateIn = Utility.getCurrentTimestamps(),
                    UserIn = userId,
                    UserUp = 0,
                    IsDeleted = false
                };

                await _context.UserLogs.AddAsync(paymentLog);

                await Utility.AfterSave(_context, "PaymentBottle", getPayment.ID, "Add");
                await _context.SaveChangesAsync();

                Trace.WriteLine($"Payment created successfully with ID = {getPayment.ID}");

                await dbTrans.CommitAsync();

                return getPayment;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"Error editing payment, request = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                await dbTrans.RollbackAsync();

                throw;
            }
        }
        
        public async Task<bool> DeleteAsync(long id, int reason, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var payment = await _context.Payments.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false && x.Status == "ACTIVE");
                if (payment == null) return false;

                var invoice = await _context.Invoices.FirstOrDefaultAsync(x => x.ID == payment.InvoicesID && x.IsDeleted == false && x.Status == "ACTIVE");
                if (invoice == null) return false;

                var user = await _context.Users.FirstOrDefaultAsync(x => x.ID == userId);
                if (user == null) return false;

                var now = Utility.getCurrentTimestamps();
                var userName = $"{user.FirstName} {user.LastName}";

                 var invoiceIds = await _context.Invoices
                    .Where(i => i.OrdersID == invoice.OrdersID && i.IsDeleted == false && i.Status == "ACTIVE")
                    .Select(i => i.ID)
                    .ToListAsync();

                var orderPayments = await _context.Payments
                    .Where(p => p.InvoicesID.HasValue && invoiceIds.Contains(p.InvoicesID.Value) && p.IsDeleted == false && p.Status == "ACTIVE")
                    .ToListAsync();

                var deposits = orderPayments.Select(p => {
                    p.Status = "CANCEL";
                    p.ReasonsID = reason;
                    p.DateUp = now;
                    p.UserUp = userId;
                    p.UsernameCancel = userName;
                    return new Deposit { ObjectID = p.ID, CustomersID = p.CustomersID, TotalAmount = p.Netto, TransDate = now, DateIn = now };
                }).ToList();

                var logs = orderPayments.Select(p => new UserLog {
                    ObjectID = p.ID, ModuleID = 3, UserID = userId, Description = "Payment was cancelled.",
                    TransDate = now, DateIn = now, UserIn = userId, UserUp = 0, IsDeleted = false
                }).ToList();

                await _context.Invoices.Where(i => i.OrdersID == invoice.OrdersID && i.IsDeleted == false && i.Status == "ACTIVE")
                    .ForEachAsync(i => { i.Status = "CANCEL"; i.DateUp = now; i.UserUp = userId; });

                await _context.Orders.Where(o => o.ID == invoice.OrdersID && o.IsDeleted == false && o.OrderStatus == "ACTIVE")
                    .ForEachAsync(o => { o.OrderStatus = "CANCEL"; o.DateUp = now; o.UserUp = userId; });

                await _context.Deposit.AddRangeAsync(deposits);
                await _context.UserLogs.AddRangeAsync(logs);

                await _context.SaveChangesAsync();
                await Utility.AfterSave(_context, "PaymentBottle", id, "Delete");
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
    }
}