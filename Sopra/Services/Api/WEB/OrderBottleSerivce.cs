using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Sopra.Helpers;
using Sopra.Responses;
using Sopra.Entities;

namespace Sopra.Services
{
    public interface OrderBottleInterface
    {
        Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<OrderBottleDto> GetByIdAsync(long id);
        // Task<T> CreateAsync(T data);
        // Task<T> EditAsync(T data);
        Task<bool> DeleteAsync(long id, long userID);
    }

    public class OrderBottleService : OrderBottleInterface
    {
        private readonly EFContext _context;
        public OrderBottleService(EFContext context)
        {
            _context = context;
        }

        public async Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                //Get data from context Orders join to Users
                //Users join to Customers 
                var query = from a in _context.Orders
                            join c in _context.Users on a.CustomersID equals c.RefID
                            join d in _context.Customers on c.CustomersID equals d.RefID into customerJoin
                            from d in customerJoin.DefaultIfEmpty()
                            where a.IsDeleted == false
                            select new { Order = a, Customer = d };

                var dateBetween = "";

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Order.RefID.ToString().Equals(search)
                        || x.Order.OrderNo.Contains(search)
                        || x.Order.TransDate.ToString().Contains(search)
                        || x.Order.CustomersID.ToString().Equals(search)
                        || x.Order.ReferenceNo.Contains(search)
                        || x.Order.OrderDetail.Any(od => od.PromosID.ToString().Equals(search))
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
                                "refid" => query.Where(x => x.Order.RefID.ToString().Equals(value)),
                                "orderno" => query.Where(x => x.Order.OrderNo.Contains(value)),
                                "orderstatus" => query.Where(x => x.Order.OrderStatus.Contains(value)),
                                "customersid" => query.Where(x => x.Order.CustomersID.ToString().Equals(value)),
                                "referenceno" => query.Where(x => x.Order.ReferenceNo.Contains(value)),
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
                            "refid" => query.OrderByDescending(x => x.Order.RefID),
                            "orderno" => query.OrderByDescending(x => x.Order.OrderNo),
                            "orderstatus" => query.OrderByDescending(x => x.Order.OrderStatus),
                            "transdate" => query.OrderByDescending(x => x.Order.TransDate),
                            "customersid" => query.OrderByDescending(x => x.Order.CustomersID),
                            "referenceno" => query.OrderByDescending(x => x.Order.ReferenceNo),
                            "dateup" => query.OrderByDescending(x => x.Order.DateUp),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.Order.RefID),
                            "orderno" => query.OrderBy(x => x.Order.OrderNo),
                            "orderstatus" => query.OrderBy(x => x.Order.OrderStatus),
                            "transdate" => query.OrderBy(x => x.Order.TransDate),
                            "customersid" => query.OrderBy(x => x.Order.CustomersID),
                            "referenceno" => query.OrderBy(x => x.Order.ReferenceNo),
                            "dateup" => query.OrderBy(x => x.Order.DateUp),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.Order.TransDate);
                }

                if (dateBetween != "")
                {
                    var dateSplit = dateBetween.Split("&", StringSplitOptions.RemoveEmptyEntries);
                    var start = Convert.ToDateTime(dateSplit[0].Trim());
                    var end = Convert.ToDateTime(dateSplit[1].Trim());
                    query = query.Where(x => x.Order.TransDate >= start && x.Order.TransDate <= end);

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
                        ID = x.Order.ID,
                        RefID = x.Order.RefID,
                        VoucherNo = x.Order.OrderNo,
                        TransDate = x.Order.TransDate,
                        CustomerName = x.Customer?.Name ?? "",

                        TotalReguler = x.Order.TotalReguler ?? 0,
                        TotalMix = x.Order.TotalMix ?? 0,
                        Amount = x.Order.Amount ?? 0,

                        Disc1 = x.Order.Disc1 ?? 0,
                        Disc1Value = x.Order.Disc1Value ?? 0,
                        Disc2 = x.Order.Disc2 ?? 0,
                        Disc2Value = x.Order.Disc2Value ?? 0,

                        Dpp = x.Order.DPP ?? 0,
                        Tax = x.Order.TAX ?? 0,
                        TaxValue = x.Order.TaxValue ?? 0,
                        Netto = x.Order.Total ?? 0,

                        HandleBy = x.Order.Username,
                        Status = x.Order.OrderStatus,
                    };
                }).ToList();

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

        public async Task<OrderBottleDto> GetByIdAsync(long id)
        {
            try
            {
                var data = await _context.Orders
                    .FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);

                if (data == null) return null;

                if (data.Total == null)
                {
                    var taxValue = MathF.Floor((float)(data.TAX ?? 0));
                    var dpp = MathF.Floor((float)(data.DPP ?? 0));
                    var netto = dpp + taxValue;

                    data.TaxValue = (decimal)taxValue;
                    data.Total = (decimal)netto;
                }

                var resData = new OrderBottleDto
                {
                    VoucherNo = data.OrderNo,
                    TransDate = data.TransDate,
                    ReferenceNo = data.ReferenceNo,
                    CustomerId = data.CustomersID ?? 0,
                    CompanyId = data.CompaniesID ?? 0,
                    Voucher = data.VouchersID,
                    CreatedBy = data.Username,
                    OrderStatus = data.OrderStatus,
                    DiscStatus = data.Disc1 > 0 ? "1" : "0",
                    TotalReguler = data.TotalReguler ?? 0,
                    TotalMix = data.TotalMix ?? 0,
                    DiscPercentage = data.Disc1 ?? 0,
                    DiscAmount = data.Disc1Value ?? 0,
                    Dpp = data.DPP ?? 0,
                    Tax = data.TAX ?? 0,
                    TaxValue = data.TaxValue ?? 0,
                    Netto = data.Total ?? 0,
                    Dealer = data.DealerTier,
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

        public async Task<bool> DeleteAsync(long id, long userID)
        {
            throw new NotImplementedException();
        }
    }
}