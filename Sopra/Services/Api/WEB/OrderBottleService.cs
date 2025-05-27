using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Sopra.Helpers;
using Sopra.Responses;
using Sopra.Entities;
using System.Data;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Sopra.Services
{
    public interface OrderBottleInterface
    {
        Task<ListResponse<dynamic>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        Task<OrderBottleDto> GetByIdAsync(long id);
        Task<OrderBottleDto> CreateAsync(OrderBottleDto data);
        Task<OrderBottleDto> EditAsync(OrderBottleDto data);
        Task<bool> DeleteAsync(long id);
    }

    public class OrderBottleService : OrderBottleInterface
    {
        private readonly EFContext _context;
        private readonly IOrderBottleRepository _orderBottleRepository;

        public OrderBottleService(IOrderBottleRepository orderBottleRepository)
        {
            _orderBottleRepository = orderBottleRepository;
        }
        
        public OrderBottleService(EFContext context)
        {
            _context = context;
        }

        private async Task<string> generateVoucherNo()
        {
            // Get the current year (YY)
            var currentYear = DateTime.Now.ToString("yy");

            // Fetch the last voucher number for the current year
            var lastVoucher = await _orderBottleRepository.GetLastOrderIdAsync();

            // Determine the next voucher number
            long nextNumber = 1; // Default to 1 if no vouchers exist for the year
            if (lastVoucher != 0)
            {
                nextNumber = lastVoucher + 1;
            }

            var newVoucherNo = $"SOPRA/SC/N/{currentYear}/{nextNumber:D5}";
            return Convert.ToString(newVoucherNo);
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

                var allRegulerItems = await _context.OrderDetails
                    .Where(x => x.OrdersID == data.ID && x.Type == "Reguler")
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
                            .Join(productItems,
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
                    RegulerItems = regulerItems
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

        public async Task<OrderBottleDto> CreateAsync(OrderBottleDto data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"payload order from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                var newVoucherNo = await generateVoucherNo();
                data.VoucherNo = Convert.ToString(newVoucherNo);

                var order = new Order
                {
                    OrderNo = data.VoucherNo,
                    RefID = data.RefID,
                    TransDate = data.TransDate,
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
                    Username = data.CreatedBy,
                    Amount = data.Amount,
                    DPP = data.Dpp,
                    TAX = data.Tax,
                    TaxValue = data.TaxValue,
                    Total = data.Netto,
                    Sfee = data.Sfee,
                    DealerTier = data.Dealer
                };

                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                var allOrderDetails = new List<OrderDetail>();
                foreach (var item in data.RegulerItems)
                {
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

                await Utility.AfterSave(_context, "OrderBottle", data.ID, "Add");
                await _context.SaveChangesAsync();

                Trace.WriteLine($"payload order after save data into database = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                await dbTrans.CommitAsync();

                return data;
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

        public async Task<OrderBottleDto> EditAsync(OrderBottleDto data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"payload order from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                var obj = null as Order;
                
                obj = await _context.Orders.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.CustomersID = data.CustomerId;
                obj.ReferenceNo = data.ReferenceNo;
                // obj.Other = data.Other;
                obj.Amount = data.Dpp;
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
                // obj.Departure = data.Departure;
                // obj.Arrival = data.Arrival;
                // obj.WarehouseID = data.WarehouseID;
                // obj.CountriesID = data.CountriesID;
                // obj.ProvincesID = data.ProvincesID;
                // obj.RegenciesID = data.RegenciesID;
                // obj.DistrictsID = data.DistrictsID;
                // obj.Address = data.Address;
                // obj.PostalCode = data.PostalCode;
                // obj.TransportsID = data.TransportsID;
                // obj.TotalTransport = data.TotalTransport;
                // obj.TotalTransportCapacity = data.TotalTransportCapacity;
                // obj.TotalOrderCapacity = data.TotalOrderCapacity;
                // obj.TotalOrderWeight = data.TotalOrderWeight;
                // obj.TotalTransportCost = data.TotalTransportCost;
                // obj.RemainingCapacity = data.RemainingCapacity;
                // obj.ReasonsID = data.ReasonsID;
                // obj.ExpeditionsID = data.ExpeditionsID;
                // obj.BiayaPickup = data.BiayaPickup;
                // obj.CheckInvoice = data.CheckInvoice;
                // obj.InvoicedDate = data.InvoicedDate;
                obj.TotalReguler = data.TotalReguler;
                // obj.TotalJumbo = data.TotalJumbo;
                obj.TotalMix = data.TotalMix;
                // obj.TotalNewPromo = data.TotalNewPromo;
                // obj.TotalSupersale = data.TotalSupersale;
                // obj.ValidTime = data.ValidTime;
                // obj.DealerTier = data.DealerTier;
                // obj.DeliveryStatus = data.DeliveryStatus;
                // obj.PartialDeliveryStatus = data.PartialDeliveryStatus;
                // obj.PaymentTerm = data.PaymentTerm;
                // obj.Validity = data.Validity;
                // obj.IsVirtual = data.IsVirtual;
                // obj.VirtualAccount = data.VirtualAccount;
                // obj.BanksID = data.BanksID;
                // obj.ShipmentNum = data.ShipmentNum;
                // obj.PaidDate = data.PaidDate;
                // obj.RecreateOrderStatus = data.RecreateOrderStatus;
                // obj.CompaniesID = data.CompaniesID;
                // obj.AmountTotal = data.AmountTotal;
                // obj.FutureDateStatus = data.FutureDateStatus;
                // obj.ChangeExpeditionStatus = data.ChangeExpeditionStatus;
                // obj.ChangetruckStatus = data.ChangetruckStatus;
                // obj.SubscriptionCount = data.SubscriptionCount;
                // obj.SubscriptionStatus = data.SubscriptionStatus;
                // obj.SubscriptionDate = data.SubscriptionDate;
                // obj.Username = data.Username;
                // obj.UsernameCancel = data.UsernameCancel;
                // obj.SessionID = data.SessionID;
                // obj.SessionDate = data.SessionDate;
                // obj.Type = data.Type;
                // obj.PaymentStatus = data.PaymentStatus;
                // obj.Label = data.Label;
                // obj.Landmark = data.Landmark;
                // obj.IsExpress = data.IsExpress;
                // obj.UserIn = userId;

                // obj.UserUp = data.UserUp;

                obj.DateUp = Utility.getCurrentTimestamps();

                var existingReguler = _context.OrderDetails.Where(d => d.OrdersID == obj.ID);
                _context.OrderDetails.RemoveRange(existingReguler);

                var allOrderDetails = new List<OrderDetail>();
                foreach (var item in data.RegulerItems)
                {
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

                await _orderBottleRepository.AddOrderDetailsAsync(allOrderDetails);

                await Utility.AfterSave(_context, "OrderBottle", data.ID, "Edit");
                await _context.SaveChangesAsync();

                Trace.WriteLine($"payload order after save data into database = " + JsonConvert.SerializeObject(data, Formatting.Indented));
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

        public async Task<bool> DeleteAsync(long id)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();

            try
            {
                var obj = await _context.Orders.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false && x.OrderStatus == "ACTIVE");
                if (obj == null) return false;

                obj.OrderStatus = "CANCEL";
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                await Utility.AfterSave(_context, "OrderBottle", id, "Delete");
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