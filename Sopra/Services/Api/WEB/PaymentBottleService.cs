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
        // Task<dynamic> GetByIdAsync(long id);
        // Task<Invoice> CreateAsync(PaymentBottle data, int userId);
        // Task<Invoice> EditAsync(PaymentBottle data, int userId);
        // Task<bool> DeleteAsync(long id, int reason, int userId);
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

            // INVOICE NETTO
            if (data.Netto <= 0)
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
            var docType = "PMT";

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
                    query = query.OrderByDescending(x => x.Payment.TransDate);
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

                        Netto = x.Payment.Netto ?? 0,
                        AmtReceive = x.Payment.AmtReceive ?? 0,

                        HandleBy = x.Payment.Username,
                        Status = x.Payment.Status
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
    }
}