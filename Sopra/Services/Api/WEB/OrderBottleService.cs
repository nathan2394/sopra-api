using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.EntityFrameworkCore;

using Sopra.Helpers;
using Sopra.Responses;
using Sopra.Entities;
using System.Data;
using Newtonsoft.Json;
using System.Configuration;
using Google.Protobuf.WellKnownTypes;
using Swashbuckle.AspNetCore.SwaggerUI;
using Google.Protobuf.Reflection;
using Microsoft.VisualBasic;

namespace Sopra.Services
{
    public interface OrderBottleInterface
    {
        Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<OrderBottleDto> GetByIdAsync(long id);
        Task<Voucher> CheckVoucherAsync(string voucher, long amount);
        Task<object> CheckIndukAnakAsync(long customerID);
        Task<object> CheckDealerAsync(long customerID);
        Task<Order> CreateAsync(OrderBottleDto data, int userId);
        Task<OrderBottleDto> EditAsync(OrderBottleDto data, int userId);
        Task<bool> DeleteAsync(long id, int reason, int userId);
    }

    public class OrderBottleService : OrderBottleInterface
    {
        private readonly EFContext _context;
        private readonly IOrderBottleRepository _orderBottleRepository;
        private readonly InvoiceBottleService _invoiceService;
        private readonly PaymentBottleService _paymentService;

        public OrderBottleService(IOrderBottleRepository orderBottleRepository)
        {
            _orderBottleRepository = orderBottleRepository;
        }
        public OrderBottleService(
            EFContext context,
            InvoiceBottleService invoiceService,
            PaymentBottleService paymentService
        )
        {
            _context = context;
            _invoiceService = invoiceService;
            _paymentService = paymentService;
        }

        private void ValidateSave(OrderBottleDto data)
        {
            bool hasValidRegulerItem = data.RegulerItems?.Any(item => item.ProductsId > 0) ?? false;
            bool hasValidMixItem = data.MixItems?.Any(item => item.ProductsId > 0) ?? false;

            // STATUS
            if (data.OrderStatus == "CANCEL")
            {
                throw new ArgumentException("Can't take any action, Order is already cancelled.");
            }

            // CUSTOMER
            if (data.CustomerId <= 0)
            {
                throw new ArgumentException("Customer must not be empty.");
            }

            // EMPTY ORDER
            if (!hasValidRegulerItem && !hasValidMixItem)
            {
                throw new ArgumentException("Order can't be save if there's no product in it, try adding some products.");
            }

            // ORDER NETTO
            if (data.Netto <= 0)
            {
                throw new ArgumentException("Order netto must be greater than 0.");
            }

            // REGULER ITEMS
            if (data.RegulerItems != null && data.RegulerItems.Any())
            {
                for (int i = 0; i < data.RegulerItems.Count; i++)
                {
                    var item = data.RegulerItems[i];
                    if (item.ProductsId > 0 && (item.Amount == null || item.Amount <= 0))
                    {
                        throw new ArgumentException($"Reguler Item's amount, price, and quantity must not be empty: Item no. {i + 1}.");
                    }
                }
            }

            // MIX ITEMS
            if (data.MixItems != null && data.MixItems.Any())
            {
                for (int i = 0; i < data.MixItems.Count; i++)
                {
                    var item = data.MixItems[i];
                    if (item.ProductsId > 0 && (item.Amount == null || item.Amount <= 0))
                    {
                        throw new ArgumentException($"Mix Item's amount, price, and quantity must not be empty: Set no. {i + 1}.");
                    }
                }
            }

            // INVOICE ITEMS
            if (data.InvoiceItems != null && data.InvoiceItems.Any())
            {
                decimal totalInvoiceAmount = 0;
                decimal totalOrderAmount = data.Netto;

                foreach (var item in data.InvoiceItems)
                {
                    if (item.Netto <= 0)
                    {
                        throw new ArgumentException($"Invoice amount must be greater than 0.");
                    }

                    totalInvoiceAmount += item.Netto ?? 0;
                }
                
                if (totalInvoiceAmount != totalOrderAmount)
                {
                    throw new ArgumentException($"Invoice amount should be equal to order amount.");
                }
            }
        }

        private async Task<string> GenerateVoucherNo(long companyId)
        {
            var currentYear = DateTime.Now.Year;
            var currentYearString = DateTime.Now.ToString("yy");
            var company = companyId == 1 ? "SOPRA" : "TRASS";

            // Get the last ID from the appropriate table for the current year
            var lastId = await _context.Orders
                    .Where(x => x.TransDate.HasValue && x.TransDate.Value.Year == currentYear)
                    .OrderByDescending(x => x.ID)
                    .Select(x => x.ID)
                    .FirstOrDefaultAsync();

            var nextNumber = lastId + 1;
            var docType = "SC";

            return $"{company}/{docType}/N/{currentYearString}/{nextNumber:D5}";
        }

        public async Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
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
                            select new
                            {
                                Order = a,
                                Customer = d,
                                FullInvoiced = _context.Invoices
                                .Where(i => i.OrdersID == a.ID && i.Status != "CANCEL")
                                .Sum(i => i.Netto) >= a.Total,
                                Progress = a.OrderStatus == "CANCEL"
                                    ? "cancel"
                                    : !_context.Invoices.Any(i => i.OrdersID == a.ID)
                                        ? "order"
                                        : _context.Invoices.Where(i => i.OrdersID == a.ID && i.Status == "ACTIVE").All(i => _context.Payments.Any(p => p.InvoicesID == i.ID))
                                            ? "paid"
                                            : _context.Invoices.Where(i => i.OrdersID == a.ID && i.Status == "ACTIVE").Any(i => _context.Payments.Any(p => p.InvoicesID == i.ID))
                                                ? "partially paid"
                                                : _context.Invoices.Any(i => i.OrdersID == a.ID && i.Status == "ACTIVE" && i.FlagInv == 1)
                                                    ? "requested"
                                                    : "invoiced"
                            };

                var dateBetween = "";

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Order.RefID.ToString().Equals(search)
                        || x.Order.OrderNo.Contains(search)
                        || x.Order.TransDate.ToString().Contains(search)
                        || x.Order.CustomersID.ToString().Equals(search)
                        || x.Customer.Name.ToString().Equals(search)
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
                                "companyid" => query.Where(x => x.Order.CompaniesID.ToString().Equals(value)),
                                "isinvoiced" => value == "0"
                                    ? query.Where(x => x.FullInvoiced == false)
                                    : value == "1"
                                        ? query.Where(x => x.FullInvoiced != true)
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

                        Progress = x.Progress
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

        public async Task<OrderBottleDto> GetByIdAsync(long id)
        {
            try
            {
                var data = await _context.Orders
                    .FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);

                if (data == null) return null;

                var allRegulerItems = await _context.OrderDetails
                    .Where(x => x.OrdersID == data.ID && x.Type == "Reguler")
                    .ToListAsync();

                var allMixItems = await _context.OrderDetails
                    .Where(x => x.OrdersID == data.ID && x.Type == "Mix")
                    .ToListAsync();

                var productItems = await _context.ProductDetails2
                    .Where(p => p.Type == "bottle" || p.Type == "closure")
                    .ToListAsync();

                var regulerItems = allRegulerItems
                    .Where(x => x.ObjectType == "bottle")
                    .Select(y =>
                    {
                        var currentBottle = productItems.FirstOrDefault(p => p.RefID == y.ObjectID && p.Type == "bottle");

                        var closureItems = allRegulerItems
                            .Where(c => c.ObjectType == "closures" && c.ParentID == y.ObjectID)
                            .Join(productItems.Where(p => p.Type == "closure"),
                                c => c.ObjectID,
                                p => p.RefID,
                                (c, p) => new ClosureItem
                                {
                                    Id = c.ID,
                                    WmsCode = p.WmsCode,
                                    ProductsId = c.ObjectID,
                                    Name = p.Name,
                                    Qty = c.Qty,
                                    QtyBox = c.QtyBox,
                                    Price = c.ProductPrice,
                                    Amount = c.Amount
                                }).ToList();

                        return new RegulerItem
                        {
                            Id = y.ID,
                            ProductsId = y.ObjectID,
                            WmsCode = currentBottle?.WmsCode,
                            Name = currentBottle?.Name,
                            Qty = y.Qty,
                            QtyBox = y.QtyBox,
                            Price = y.ProductPrice,
                            Amount = y.Amount,
                            Notes = y.Note,
                            ClosureItems = closureItems
                        };
                    }).ToList();

                var mixItems = allMixItems
                    .Where(x => x.ObjectType == "bottle")
                    .Select(y =>
                    {
                        var currentBottle = productItems.FirstOrDefault(p => p.RefID == y.ObjectID && p.Type == "bottle");

                        var closureItems = allMixItems
                            .Where(c => c.ObjectType == "closures" && c.ParentID == y.ID)
                            .Join(productItems.Where(p => p.Type == "closure"),
                                c => c.ObjectID,
                                p => p.RefID,
                                (c, p) => new ClosureItem
                                {
                                    Id = c.ID,
                                    WmsCode = p.WmsCode,
                                    ProductsId = c.ObjectID,
                                    Name = p.Name,
                                    Qty = c.Qty,
                                    QtyBox = c.QtyBox,
                                    Price = c.ProductPrice,
                                    Amount = c.Amount
                                }).ToList();

                        return new MixItem
                        {
                            Id = y.ID,
                            ProductsId = y.ObjectID,
                            PromoId = y.PromosID,
                            WmsCode = currentBottle?.WmsCode,
                            Name = currentBottle?.Name,
                            Qty = y.Qty,
                            QtyBox = y.QtyBox,
                            Price = y.ProductPrice,
                            Amount = y.Amount,
                            Notes = y.Note,
                            ApprovalStatus = y.ApprovalStatus,
                            ClosureItems = closureItems
                        };
                    }).ToList();

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
                    ID = data.ID,
                    RefID = data.RefID,
                    VoucherNo = data.OrderNo,
                    TransDate = data.TransDate,
                    ReferenceNo = data.ReferenceNo,
                    CustomerId = data.CustomersID ?? 0,
                    CompanyId = data.CompaniesID ?? 1,
                    VouchersID = data.VouchersID,
                    Disc2 = data.Disc2Value,
                    Disc2Value = data.Disc2Value,
                    CreatedBy = data.Username,
                    OrderStatus = data.OrderStatus,
                    DiscStatus = data.Status,
                    Amount = data.Amount ?? 0,
                    TotalReguler = Math.Floor(data.TotalReguler ?? 0),
                    TotalMix = data.TotalMix ?? 0,
                    DiscPercentage = data.Disc1 ?? 0,
                    DiscAmount = Math.Floor(data.Disc1Value ?? 0),
                    Dpp = Math.Floor(data.DPP ?? 0),
                    Tax = data.TAX ?? 0,
                    TaxValue = Math.Floor(data.TaxValue ?? 0),
                    Netto = Math.Floor(data.Total ?? 0),
                    Sfee = Math.Floor(data.Sfee ?? 0),
                    Dealer = data.DealerTier,
                    RegulerItems = regulerItems,
                    MixItems = mixItems,
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

        public async Task<Voucher> CheckVoucherAsync(string voucher, long amount)
        {
            try
            {
                var Now = Utility.getCurrentTimestamps();
                var obj = await _context.Vouchers
                    .FirstOrDefaultAsync(x => x.VoucherNo == voucher && Now < x.ExpiredDate && x.IsDeleted == false);

                if (obj == null) throw new ArgumentException("Voucher is either not found or expired.");

                var orderUsage = await _context.Orders
                    .Where(x => x.VouchersID == voucher && x.IsDeleted == false && x.OrderStatus != "CANCEL")
                    .CountAsync();

                if (orderUsage >= obj.VoucherUsage) throw new ArgumentException("Voucher usage exceeds the specified limit.");
                if (amount < obj.MinOrder) throw new ArgumentException($"Order amount must be greater than {obj.MinOrder:N0}.");

                return obj;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<object> CheckIndukAnakAsync(long customerID)
        {
            try
            {
                var obj = await _context.Orders
                .Where(o => o.CustomersID == customerID &&
                            o.IsDeleted == false &&
                            o.OrderStatus == "ACTIVE" &&
                            o.Status == "INDUK" &&
                            o.TransDate >= DateTime.UtcNow.AddDays(-30))
                .Where(o => _context.Invoices.Any(i => i.OrdersID == o.ID) &&
                            _context.Invoices.Where(i => i.OrdersID == o.ID).Count() ==
                            _context.Invoices.Where(i => i.OrdersID == o.ID)
                                .Join(_context.Payments,
                                i => i.ID, p => p.InvoicesID,
                                (i, p) => i.ID)
                                .Distinct()
                                .Count())
                .OrderByDescending(x => x.ID)
                .FirstOrDefaultAsync();

                if (obj == null)
                {
                    return new
                    {
                        data = new
                        {
                            Status = false,
                            Disc1 = 0
                        }
                    };
                }

                return new
                {
                    data = new
                    {
                        Status = true,
                        Disc1 = obj.Disc1
                    }
                };
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<object> CheckDealerAsync(long customerID)
        {
            try
            {
                var currentTime = Utility.getCurrentTimestamps();

                var obj = await _context.UserDealers
                .Where(ud => ud.UserId == customerID &&
                            ud.IsDeleted == false &&
                            ud.StartDate <= currentTime &&
                            ud.EndDate >= currentTime)
                .Join(_context.Dealers,
                    ud => ud.DealerId,
                    d => d.RefID,
                    (ud, d) => new
                    {
                        UserDealerId = ud.ID,
                        DealerName = d.Tier,
                        DiscBottle = d.DiscBottle,
                        DiscThermo = d.DiscThermo
                    })
                .OrderByDescending(x => x.UserDealerId)
                .FirstOrDefaultAsync();

                if (obj == null)
                {
                    return new
                    {
                        data = new
                        {
                            Status = false,
                            Dealer = "Regular",
                            Disc = 0
                        }
                    };
                }

                return new
                {
                    data = new
                    {
                        Status = true,
                        Dealer = obj.DealerName,
                        Disc = obj.DiscBottle
                    }
                };
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Order> CreateAsync(OrderBottleDto data, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"payload order from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                ValidateSave(data);

                var newOrderNo = await GenerateVoucherNo(data.CompanyId);
                data.VoucherNo = Convert.ToString(newOrderNo);

                var logItems = new List<UserLog>();
                var order = new Order
                {
                    OrderNo = data.VoucherNo,
                    RefID = data.RefID,
                    TransDate = Utility.getCurrentTimestamps(),
                    ReferenceNo = data.ReferenceNo,
                    CustomersID = data.CustomerId,
                    CompaniesID = data.CompanyId,
                    VouchersID = data.VouchersID,
                    Disc1 = data.DiscPercentage,
                    Disc1Value = data.DiscAmount,
                    Disc2 = data.Disc2,
                    Disc2Value = data.Disc2Value,
                    TotalReguler = data.TotalReguler,
                    TotalMix = data.TotalMix,
                    OrderStatus = data.OrderStatus,
                    Status = data.DiscStatus,
                    Username = data.CreatedBy,
                    Amount = data.Amount,
                    DPP = data.Dpp,
                    TAX = data.Tax,
                    TaxValue = data.TaxValue,
                    Total = data.Netto,
                    Sfee = data.Sfee,
                    DealerTier = data.Dealer,
                    Type = data.Type
                };

                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                var orderLogs = new UserLog
                {
                    ObjectID = order.ID,
                    ModuleID = 1,
                    UserID = userId,
                    Description = "Order was created.",
                    TransDate = Utility.getCurrentTimestamps(),
                    DateIn = Utility.getCurrentTimestamps(),
                    UserIn = userId,
                    UserUp = 0,
                    IsDeleted = false
                };

                // INSERT REGULER
                var allOrderDetails = new List<OrderDetail>();
                foreach (var item in data.RegulerItems)
                {
                    if (item.ProductsId <= 0 || item.Qty <= 0) continue;

                    var regulerDetail = new OrderDetail
                    {
                        OrdersID = order.ID,
                        ObjectID = item.ProductsId,
                        ObjectType = "bottle",
                        Type = "Reguler",
                        QtyBox = item.QtyBox,
                        Qty = item.Qty,
                        ProductPrice = item.Price,
                        Amount = item.Amount,
                        Note = item.Notes
                    };

                    allOrderDetails.Add(regulerDetail);

                    foreach (var closure in item.ClosureItems)
                    {
                        if (closure.ProductsId <= 0 || closure.Qty <= 0) continue;

                        var closureDetail = new OrderDetail
                        {
                            OrdersID = order.ID,
                            ObjectID = closure.ProductsId,
                            ObjectType = "closures",
                            ParentID = item.ProductsId,
                            Type = "Reguler",
                            QtyBox = closure.QtyBox,
                            Qty = closure.Qty,
                            ProductPrice = closure.Price,
                            Amount = closure.Amount
                        };

                        allOrderDetails.Add(closureDetail);
                    }
                }

                await _context.OrderDetails.AddRangeAsync(allOrderDetails);

                // INSERT MIX
                foreach (var item in data.MixItems)
                {
                    if (item.ProductsId <= 0 || item.Qty <= 0) continue;

                    var mixDetail = new OrderDetail
                    {
                        OrdersID = order.ID,
                        ObjectID = item.ProductsId,
                        ObjectType = "bottle",
                        Type = "Mix",
                        QtyBox = item.QtyBox,
                        Qty = item.Qty,
                        ProductPrice = item.Price,
                        ParentID = 0,
                        PromosID = item.PromoId,
                        Amount = item.Amount,
                        Note = item.Notes,
                        ApprovalStatus = item.ApprovalStatus
                    };

                    _context.OrderDetails.Add(mixDetail);
                    await _context.SaveChangesAsync();

                    mixDetail.ParentID = mixDetail.ID;
                    await _context.SaveChangesAsync();

                    var closureDetails = new List<OrderDetail>();
                    foreach (var closure in item.ClosureItems)
                    {
                        var closureDetail = new OrderDetail
                        {
                            OrdersID = order.ID,
                            ObjectID = closure.ProductsId,
                            ObjectType = "closures",
                            ParentID = mixDetail.ID,
                            PromosID = item.PromoId,
                            Type = "Mix",
                            QtyBox = 1,
                            Qty = 0,
                            ProductPrice = 0,
                            Amount = 0
                        };

                        closureDetails.Add(closureDetail);
                    }

                    if (closureDetails.Any())
                    {
                        await _context.OrderDetails.AddRangeAsync(closureDetails);
                        await _context.SaveChangesAsync();
                    }
                }

                // INSERT INVOICES
                foreach (var item in data.InvoiceItems)
                {
                    var invoiceItem = new InvoiceBottle
                    {
                        RefID = item.RefID,
                        OrdersID = order.ID,
                        CustomersID = data.CustomerId,
                        CompanyID = data.CompanyId,
                        CreatedBy = data.CreatedBy,
                        PaymentMethod = item.PaymentMethod,
                        Refund = item.Refund,
                        Bill = item.Bill,
                        Netto = item.Netto,
                        Type = item.Type,
                        Status = item.Status,
                        DueDate = item.DueDate,
                        FlagInv = item.FlagInv
                    };

                    var invoice = await _invoiceService.CreateInvoiceAsync(invoiceItem, userId);

                    // INSERT PAYMENT (DEPOSIT)
                    if (item.PaymentMethod == 3)
                    {
                        var paymentItem = new PaymentBottle
                        {
                            RefID = invoice.RefID,
                            InvoicesID = invoice.ID,
                            TransDate = invoice.TransDate,
                            CustomersID = invoice.CustomersID,
                            CompanyID = invoice.CompaniesID,
                            CreatedBy = invoice.Username,
                            Netto = invoice.Netto,
                            BankTime = Utility.getCurrentTimestamps(),
                            BankRef = "DEPOSIT",
                            AmtReceive = invoice.Netto,
                            Type = invoice.Type,
                            Status = "ACTIVE"
                        };

                        var payment = await _paymentService.CreatePaymentAsync(paymentItem, userId);
                    }
                }

                await Utility.AfterSave(_context, "OrderBottle", data.ID, "Add");
                await _context.SaveChangesAsync();

                Trace.WriteLine($"payload order after save data into database = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                await _context.UserLogs.AddRangeAsync(logItems);
                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                return order;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"error save data order, payload = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                await dbTrans.RollbackAsync();
                Trace.WriteLine($"rollback db");
                throw;
            }
        }

        public async Task<OrderBottleDto> EditAsync(OrderBottleDto data, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"payload order from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                ValidateSave(data);

                var obj = null as Order;

                obj = await _context.Orders.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.CustomersID = data.CustomerId;
                obj.ReferenceNo = data.ReferenceNo;
                obj.Amount = data.Amount;
                obj.Status = data.DiscStatus;
                obj.VouchersID = data.VouchersID;
                obj.Disc1 = data.DiscPercentage;
                obj.Disc1Value = data.DiscAmount;
                obj.Disc2 = data.Disc2;
                obj.Disc2Value = data.Disc2Value;
                obj.Sfee = data.Sfee;
                obj.DPP = data.Dpp;
                obj.TAX = data.Tax;
                obj.TaxValue = data.TaxValue;
                obj.Total = data.Netto;
                obj.TotalReguler = data.TotalReguler;
                obj.TotalMix = data.TotalMix;

                obj.UserUp = userId;
                obj.DateUp = Utility.getCurrentTimestamps();

                // STORE EXISTING REGULER FOR COMPARISON
                var existingRegulerDetails = await _context.OrderDetails
                .Where(d => d.OrdersID == obj.ID && d.Type == "Reguler" && d.ObjectType == "bottle")
                .Join(_context.ProductDetails2,
                    od => od.ObjectID,
                    p => p.RefID,
                    (od, p) => new
                    {
                        ProductsId = od.ObjectID,
                        ProductName = p.Name,
                        Qty = od.Qty
                    })
                .ToListAsync();

                // REMOVE EXISTING DETAILS (REGULER, MIX)
                var existingReguler = _context.OrderDetails.Where(d => d.OrdersID == obj.ID);
                _context.OrderDetails.RemoveRange(existingReguler);

                // MAP REGULER PAYLOAD
                var allOrderDetails = new List<OrderDetail>();
                foreach (var item in data.RegulerItems)
                {
                    if (item.ProductsId <= 0 || item.Qty <= 0) continue;

                    var regulerDetail = new OrderDetail
                    {
                        OrdersID = obj.ID,
                        ObjectID = item.ProductsId,
                        ObjectType = "bottle",
                        Type = "Reguler",
                        QtyBox = item.QtyBox,
                        Qty = item.Qty,
                        ProductPrice = item.Price,
                        Amount = item.Amount,
                        Note = item.Notes
                    };

                    allOrderDetails.Add(regulerDetail);

                    foreach (var closure in item.ClosureItems)
                    {
                        if (closure.ProductsId <= 0 || closure.Qty <= 0) continue;

                        var closureDetail = new OrderDetail
                        {
                            OrdersID = obj.ID,
                            ObjectID = closure.ProductsId,
                            ObjectType = "closures",
                            ParentID = item.ProductsId,
                            Type = "Reguler",
                            QtyBox = closure.QtyBox,
                            Qty = closure.Qty,
                            ProductPrice = closure.Price,
                            Amount = closure.Amount
                        };

                        allOrderDetails.Add(closureDetail);
                    }
                }

                // INSERT REGULER
                await _context.OrderDetails.AddRangeAsync(allOrderDetails);

                // MAP MIX PRODUCT PAYLOAD
                foreach (var item in data.MixItems)
                {
                    if (item.ProductsId <= 0 || item.Qty <= 0) continue;
                    
                    var mixDetail = new OrderDetail
                    {
                        OrdersID = obj.ID,
                        ObjectID = item.ProductsId,
                        ObjectType = "bottle",
                        Type = "Mix",
                        QtyBox = item.QtyBox,
                        Qty = item.Qty,
                        ProductPrice = item.Price,
                        ParentID = 0,
                        PromosID = item.PromoId,
                        Amount = item.Amount,
                        Note = item.Notes,
                        ApprovalStatus = item.ApprovalStatus
                    };

                    // INSERT MIX PRODUCT
                    _context.OrderDetails.Add(mixDetail);
                    await _context.SaveChangesAsync();

                    mixDetail.ParentID = mixDetail.ID;
                    await _context.SaveChangesAsync();

                    // MAP MIX CLOSURE PAYLOAD
                    var closureDetails = new List<OrderDetail>();
                    foreach (var closure in item.ClosureItems)
                    {
                        var closureDetail = new OrderDetail
                        {
                            OrdersID = obj.ID,
                            ObjectID = closure.ProductsId,
                            ObjectType = "closures",
                            ParentID = mixDetail.ID,
                            PromosID = item.PromoId,
                            Type = "Mix",
                            QtyBox = 1,
                            Qty = 0,
                            ProductPrice = 0,
                            Amount = 0
                        };

                        closureDetails.Add(closureDetail);
                    }

                    if (closureDetails.Any())
                    {
                        // INSERT MIX CLOSURE
                        await _context.OrderDetails.AddRangeAsync(closureDetails);
                        await _context.SaveChangesAsync();
                    }
                }

                // STORE EXISTING INVOICES FOR COMPARISON
                var existingInvoices = await _context.Invoices
                .Where(i => i.OrdersID == obj.ID)
                .ToListAsync();

                // INSERT/UPDATE INVOICES
                foreach (var item in data.InvoiceItems)
                {
                    if (item.ID != 0)
                    {
                        // UPDATE EXISTING INVOICE
                        var existingInvoice = existingInvoices.FirstOrDefault(x => x.ID == item.ID);

                        // Track changes for logging
                        var invoiceChanges = new List<string>();

                        if (existingInvoice.Netto != item.Netto)
                        {
                            invoiceChanges.Add($"Netto changed from {existingInvoice.Netto:N2} to {item.Netto:N2}");
                            existingInvoice.Netto = item.Netto;
                        }

                        if (existingInvoice.DueDate != item.DueDate)
                        {
                            invoiceChanges.Add($"Due date changed from {existingInvoice.DueDate:dd/MM/yyyy} to {item.DueDate:dd/MM/yyyy}");
                            existingInvoice.DueDate = item.DueDate;
                        }

                        if (existingInvoice.PaymentMethod != item.PaymentMethod)
                        {
                            invoiceChanges.Add($"Payment method changed from {existingInvoice.PaymentMethod} to {item.PaymentMethod}");
                            existingInvoice.PaymentMethod = item.PaymentMethod;
                        }

                        // Update other fields that might have changed
                        if (existingInvoice.Refund != item.Refund)
                        {
                            invoiceChanges.Add($"Refund changed from {existingInvoice.Refund} to {item.Refund}");
                            existingInvoice.Refund = item.Refund;
                        }

                        if (existingInvoice.Bill != item.Bill)
                        {
                            invoiceChanges.Add($"Bill changed from {existingInvoice.Bill} to {item.Bill}");
                            existingInvoice.Bill = item.Bill;
                        }

                        if (existingInvoice.FlagInv != item.FlagInv)
                        {
                            invoiceChanges.Add($"Flag changed from {existingInvoice.FlagInv} to {item.FlagInv}");
                            existingInvoice.FlagInv = item.FlagInv;
                        }

                        existingInvoice.UserUp = userId;
                        existingInvoice.DateUp = Utility.getCurrentTimestamps();

                        _context.Invoices.Update(existingInvoice);
                        await _context.SaveChangesAsync();

                        if (invoiceChanges.Any())
                        {
                            var invoiceDescription = "Invoice was updated.";
                            invoiceDescription += $"<br/><br/>{existingInvoice.InvoiceNo}:";

                            invoiceDescription += "<br/><ul>";
                            foreach (var change in invoiceChanges)
                            {
                                invoiceDescription += $"<li>{change}</li>";
                            }

                            invoiceDescription += "</ul>";

                            var invoiceLog = new UserLog
                            {
                                ObjectID = existingInvoice.ID,
                                ModuleID = 2, // Invoice module
                                UserID = userId,
                                Description = invoiceDescription,
                                TransDate = Utility.getCurrentTimestamps(),
                                DateIn = Utility.getCurrentTimestamps(),
                                UserIn = userId,
                                UserUp = 0,
                                IsDeleted = false
                            };

                            await _context.UserLogs.AddAsync(invoiceLog);
                            await _context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        var refinedItem = new InvoiceBottle
                        {
                            RefID = item.RefID,
                            OrdersID = obj.ID,
                            CustomersID = data.CustomerId,
                            CompanyID = data.CompanyId,
                            CreatedBy = data.CreatedBy,
                            PaymentMethod = item.PaymentMethod,
                            Refund = item.Refund,
                            Bill = item.Bill,
                            Netto = item.Netto,
                            Type = item.Type,
                            Status = item.Status,
                            DueDate = item.DueDate,
                            FlagInv = item.FlagInv
                        };

                        var invoice = await _invoiceService.CreateInvoiceAsync(refinedItem, userId);
                    }
                }

                await Utility.AfterSave(_context, "OrderBottle", data.ID, "Edit");
                await _context.SaveChangesAsync();

                Trace.WriteLine($"payload order after save data into database = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                // COMPARE CHANGES (QTY)
                var RegulerQtyChanges = new List<string>();
                
                foreach (var newItem in data.RegulerItems)
                {
                    var existingItem = existingRegulerDetails.FirstOrDefault(x => x.ProductsId == newItem.ProductsId);

                    if (existingItem != null)
                    {
                        // ITEM EXIST, CHECK IF QTY CHANGED
                        if (existingItem.Qty != newItem.Qty)
                        {
                            RegulerQtyChanges.Add($"{existingItem.ProductName} qty's changed from {existingItem.Qty:N0} to {newItem.Qty:N0}.");
                        }
                    }
                    else
                    {
                        // NEW ITEM ADDED
                        var productName = await _context.ProductDetails2
                            .Where(p => p.RefID == newItem.ProductsId)
                            .Select(p => p.Name)
                            .FirstOrDefaultAsync();
                        
                        if (!string.IsNullOrEmpty(productName))
                        {
                            RegulerQtyChanges.Add($"{productName} was added with qty {newItem.Qty:N0}.");
                        }
                    }
                }

                // CHECK FOR REMOVED ITEMS
                foreach (var existingItem in existingRegulerDetails)
                {
                    var newItem = data.RegulerItems.FirstOrDefault(x => x.ProductsId == existingItem.ProductsId);
                    if (newItem == null)
                    {
                        // ITEM WAS REMOVED
                        RegulerQtyChanges.Add($"{existingItem.ProductName} was removed.");
                    }
                }

                // MAP THE DESCRIPTION
                var description = "Order was updated.";
                if (RegulerQtyChanges.Any())
                {
                    description += "<br/><br/>Reguler Items:";

                    description += "<br/><ul>";
                    foreach (var change in RegulerQtyChanges)
                    {
                        description += $"<li>{change}</li>";
                    }

                    description += "</ul>";
                }

                var logs = new UserLog
                {
                    ObjectID = obj.ID,
                    ModuleID = 1,
                    UserID = userId,
                    Description = description,
                    TransDate = Utility.getCurrentTimestamps(),
                    DateIn = Utility.getCurrentTimestamps(),
                    UserIn = userId,
                    UserUp = 0,
                    IsDeleted = false
                };

                await _context.UserLogs.AddAsync(logs);
                await _context.SaveChangesAsync();

                await dbTrans.CommitAsync();

                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"error save data order,payload = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                await dbTrans.RollbackAsync();
                Trace.WriteLine($"rollback db");

                throw;
            }
        }

        public async Task<bool> DeleteAsync(long id, int reason, int userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();

            try
            {
                var obj = await _context.Orders.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false && x.OrderStatus == "ACTIVE");
                if (obj == null) return false;

                var getUser = await _context.Users.FirstOrDefaultAsync(x => x.ID == userId);

                if (getUser != null)
                {
                    obj.OrderStatus = "CANCEL";
                    obj.ReasonsID = reason;
                    obj.DateUp = Utility.getCurrentTimestamps();
                    obj.UserUp = userId;
                    obj.UsernameCancel = $"{getUser.FirstName} {getUser.LastName}";

                    await _context.SaveChangesAsync();

                    await Utility.AfterSave(_context, "OrderBottle", id, "Delete");

                    var logs = new UserLog
                    {
                        ObjectID = id,
                        ModuleID = 1,
                        UserID = userId,
                        Description = "Order was cancelled.",
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
                    await dbTrans.CommitAsync();
                    
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