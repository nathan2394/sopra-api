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
using Newtonsoft.Json.Linq;
using Microsoft.EntityFrameworkCore.Storage;
using Google.Api;
using Google.Apis.Storage.v1.Data;
using Newtonsoft.Json;
using Google.Protobuf.WellKnownTypes;

namespace Sopra.Services
{
    public interface OrderInterface
    {
        Task<ListResponseProduct<OrderGroup>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date);
        //Task<ListResponse<Order>> GetAllAsync(int limit, int page, int total, string search, string sort,
        //string filter, string date);
        Task<Order> GetByIdAsync(long id);
        Task<Order> CreateAsync(List<Order> data,long userId);
        Task<Order> EditAsync(List<Order> data, long userId);
        Task<bool> DeleteAsync(long id, long userID);
        Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid);
    }
    public class OrderService : OrderInterface
    {
        private readonly EFContext _context;
        private readonly OrderDetailService _svc;

        private async Task<string> generateVoucherNo()
        {
            // Get the current year (YY)
            var currentYear = DateTime.Now.ToString("yy");

            // Fetch the last voucher number for the current year
            var lastVoucher = await _context.Orders
                //.Where(x => x.OrderNo.StartsWith($"SOPRA/SC/M/{currentYear}/") && x.ExternalOrderNo == null)
                .OrderByDescending(x => x.ID)
                .Select(x => x.ID)
                .FirstOrDefaultAsync();

            // Determine the next voucher number
            long nextNumber = 1; // Default to 1 if no vouchers exist for the year
            if (lastVoucher != 0)
            {
                nextNumber = lastVoucher + 1;
            }

            var newVoucherNo = $"SOPRA/SC/M/{currentYear}/{nextNumber:D5}";
            return Convert.ToString(newVoucherNo);
        }

        public OrderService(EFContext context)
        {
            _context = context;
            _svc = new OrderDetailService(_context);
        }

        public async Task<Order> CreateAsync(List<Order> data,long userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"payload order from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                var newVoucherNo = await generateVoucherNo();

                //try to increment cartsid
                var getCartId = _context.Orders.FirstOrDefault(x => x.CartsID == data[0].CartsID);
                var cartid = Convert.ToInt64(0);
                if (getCartId != null) cartid = Convert.ToInt64(getCartId.CartsID) + 1;
                else
                {
                    var lastCartId = _context.Orders.OrderByDescending(x => x.CartsID).Select(x => x.CartsID);
                    cartid = Convert.ToInt64(lastCartId) + 1;
                }
                

                foreach (var item in data)
                {
                    item.RefID = 0;
                    item.OrderNo = Convert.ToString(newVoucherNo);
                    if (item.isIntegration == "desktop")
                    {
                        var obj = await _context.Orders.FirstOrDefaultAsync(x => x.ExternalOrderNo == item.ExternalOrderNo && x.IsDeleted == false);
                        if (obj != null)
                        {
                            var objDetail = _context.OrderDetails.Where(x => x.OrdersID == obj.RefID && x.IsDeleted == false);
                            if (objDetail != null)
                            {
                                _context.OrderDetails.RemoveRange(objDetail);
                            }
                            _context.Orders.Remove(obj);
                        }
                        item.UserIn = userId;
                        item.ExternalOrderNo = item.OrderNo;
                        item.CartsID = cartid;

                        item.TransDate = item.DateIn;
                        await _context.Orders.AddAsync(item);
                        // Check Validate
                        await Utility.AfterSave(_context, "Order", item.ID, "Add");
                    } else if (item.isIntegration == "mobile") 
                    {
                        var obj = await _context.Orders.FirstOrDefaultAsync(x => x.OrderNo == item.ExternalOrderNo && x.IsDeleted == false);
                        if (obj != null)
                        {
                            var objDetail = _context.OrderDetails.Where(x => x.OrdersID == obj.RefID && x.IsDeleted == false);
                            if (objDetail != null)
                            {
                                _context.OrderDetails.RemoveRange(objDetail);
                            }
                            _context.Orders.Remove(obj);
                        }
                        item.UserIn = userId;
                        item.ExternalOrderNo = item.OrderNo;
                        item.CartsID = cartid;

                        item.TransDate = item.DateIn;
                        await _context.Orders.AddAsync(item);
                        // Check Validate
                        await Utility.AfterSave(_context, "Order", item.ID, "Add");
                    } else
                    {
                        var obj = await _context.Orders.FirstOrDefaultAsync(x => x.OrderNo == item.OrderNo && x.IsDeleted == false);
                        if (obj != null)
                        {
                            var objDetail = _context.OrderDetails.Where(x => (x.OrdersID == obj.RefID || x.OrdersID == obj.ID) && x.IsDeleted == false);
                            if (objDetail != null)
                            {
                                _context.OrderDetails.RemoveRange(objDetail);
                            }
                            _context.Orders.Remove(obj);
                        }

                        item.UserIn = userId;
                        item.ExternalOrderNo = null;
                        item.TransDate = item.DateIn;
                        item.CartsID = cartid;
                        await _context.Orders.AddAsync(item);
                        // Check Validate
                        await Utility.AfterSave(_context, "Order", item.ID, "Add");
                    }
                }
                await _context.SaveChangesAsync();
                Trace.WriteLine($"payload order after save data into database = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                //var user = await _context.Users.FirstOrDefaultAsync(x => x.ID == userId);
                //if (user != null && user.FirebaseToken != "" && user.FirebaseToken != null) await Utility.sendNotificationAsync(user?.FirebaseToken, "New Order!", "You got a new order!");

                await dbTrans.CommitAsync();

                return data[0];
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

        public async Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            try
            {
                return await _context.VTransactionOrderDetails.Where(x => x.OrderID == orderid && x.LinkedWms != 0 && x.ProductName != null).AsNoTracking().ToListAsync();
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<bool> DeleteAsync(long id, long UserID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Orders.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Order", id, "Delete");

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

        public async Task<Order> EditAsync(List<Order> data, long userId)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"payload order from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                var obj = null as Order;
                foreach (var item in data)
                {
                    if (item.isIntegration == "desktop")
                    {
                        obj = await _context.Orders.FirstOrDefaultAsync(x => x.ExternalOrderNo == item.OrderNo && x.IsDeleted == false);
                        if (obj != null)
                        {
                            var objDetail = _context.OrderDetails.Where(x => x.OrdersID == obj.RefID && x.IsDeleted == false);
                            if (objDetail != null)
                            {
                                _context.OrderDetails.RemoveRange(objDetail);
                            }
                            _context.Orders.Remove(obj);
                        }


                        item.UserIn = userId;
                        item.ExternalOrderNo = item.OrderNo;
                        
                        item.TransDate= item.DateIn;
                        await _context.Orders.AddAsync(item);
                        // Check Validate
                        await Utility.AfterSave(_context, "Order", item.ID, "Edit");
                    }
                    else if (item.isIntegration == "mobile")
                    {
                        obj = await _context.Orders.FirstOrDefaultAsync(x => x.OrderNo == item.ExternalOrderNo && x.IsDeleted == false);
                        if (obj != null)
                        {
                            var objDetail = _context.OrderDetails.Where(x => x.OrdersID == obj.RefID && x.IsDeleted == false);
                            if (objDetail != null)
                            {
                                _context.OrderDetails.RemoveRange(objDetail);
                            }
                            _context.Orders.Remove(obj);
                        }


                        item.UserIn = userId;
                        item.ExternalOrderNo = item.OrderNo;

                        item.TransDate = item.DateIn;
                        await _context.Orders.AddAsync(item);
                        // Check Validate
                        await Utility.AfterSave(_context, "Order", item.ID, "Edit");
                    }
                    else
                    {
                        obj = await _context.Orders.FirstOrDefaultAsync(x => x.ID == item.ID && x.IsDeleted == false);
                        if (obj == null) return null;

                        obj.RefID = item.RefID;
                        obj.OrderNo = item.OrderNo;
                        obj.TransDate = item.TransDate;
                        obj.CustomersID = item.CustomersID;
                        obj.ReferenceNo = item.ReferenceNo;
                        obj.Other = item.Other;
                        obj.Amount = item.Amount;
                        obj.Status = item.Status;
                        obj.VouchersID = item.VouchersID;
                        obj.Disc1 = item.Disc1;
                        obj.Disc1Value = item.Disc1Value;
                        obj.Disc2 = item.Disc2;
                        obj.Disc2Value = item.Disc2Value;
                        obj.Sfee = item.Sfee;
                        obj.DPP = item.DPP;
                        obj.TAX = item.TAX;
                        obj.TaxValue = item.TaxValue;
                        obj.Total = item.Total;
                        obj.Departure = item.Departure;
                        obj.Arrival = item.Arrival;
                        obj.WarehouseID = item.WarehouseID;
                        obj.CountriesID = item.CountriesID;
                        obj.ProvincesID = item.ProvincesID;
                        obj.RegenciesID = item.RegenciesID;
                        obj.DistrictsID = item.DistrictsID;
                        obj.Address = item.Address;
                        obj.PostalCode = item.PostalCode;
                        obj.TransportsID = item.TransportsID;
                        obj.TotalTransport = item.TotalTransport;
                        obj.TotalTransportCapacity = item.TotalTransportCapacity;
                        obj.TotalOrderCapacity = item.TotalOrderCapacity;
                        obj.TotalOrderWeight = item.TotalOrderWeight;
                        obj.TotalTransportCost = item.TotalTransportCost;
                        obj.RemainingCapacity = item.RemainingCapacity;
                        obj.ReasonsID = item.ReasonsID;
                        obj.OrderStatus = item.OrderStatus;
                        obj.ExpeditionsID = item.ExpeditionsID;
                        obj.BiayaPickup = item.BiayaPickup;
                        obj.CheckInvoice = item.CheckInvoice;
                        obj.InvoicedDate = item.InvoicedDate;
                        obj.TotalReguler = item.TotalReguler;
                        obj.TotalJumbo = item.TotalJumbo;
                        obj.TotalMix = item.TotalMix;
                        obj.TotalNewPromo = item.TotalNewPromo;
                        obj.TotalSupersale = item.TotalSupersale;
                        obj.ValidTime = item.ValidTime;
                        obj.DealerTier = item.DealerTier;
                        obj.DeliveryStatus = item.DeliveryStatus;
                        obj.PartialDeliveryStatus = item.PartialDeliveryStatus;
                        obj.PaymentTerm = item.PaymentTerm;
                        obj.Validity = item.Validity;
                        obj.IsVirtual = item.IsVirtual;
                        obj.VirtualAccount = item.VirtualAccount;
                        obj.BanksID = item.BanksID;
                        obj.ShipmentNum = item.ShipmentNum;
                        obj.PaidDate = item.PaidDate;
                        obj.RecreateOrderStatus = item.RecreateOrderStatus;
                        obj.CompaniesID = item.CompaniesID;
                        obj.AmountTotal = item.AmountTotal;
                        obj.FutureDateStatus = item.FutureDateStatus;
                        obj.ChangeExpeditionStatus = item.ChangeExpeditionStatus;
                        obj.ChangetruckStatus = item.ChangetruckStatus;
                        obj.SubscriptionCount = item.SubscriptionCount;
                        obj.SubscriptionStatus = item.SubscriptionStatus;
                        obj.SubscriptionDate = item.SubscriptionDate;
                        obj.Username = item.Username;
                        obj.UsernameCancel = item.UsernameCancel;
                        obj.SessionID = item.SessionID;
                        obj.SessionDate = item.SessionDate;
                        obj.Type = item.Type;
                        obj.PaymentStatus = item.PaymentStatus;
                        obj.Label= item.Label;
                        obj.Landmark = item.Landmark;
                        obj.IsExpress = item.IsExpress;
                        obj.UserIn = userId;

                        obj.UserUp = item.UserUp;

                        obj.DateUp = Utility.getCurrentTimestamps();

                        await Utility.AfterSave(_context, "Order", item.ID, "Edit");
                    }
                }
                

                await _context.SaveChangesAsync();
                Trace.WriteLine($"payload order after save data into database = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                // Check Validate


                await dbTrans.CommitAsync();

                return obj;
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


        public async Task<ListResponseProduct<OrderGroup>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Orders where a.IsDeleted == false select a;
                var promosId = 0;
                var dateBetween = "";
                //query = query.Include(order => order.OrderDetail)
                //             .Where(order => order.OrderDetail.Any(od => od.OrdersID == order.RefID));

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Equals(search)
                        || x.OrderNo.Contains(search)
                        || x.TransDate.ToString().Contains(search)
                        || x.CustomersID.ToString().Equals(search)
                        || x.ReferenceNo.Contains(search)
                        || x.OrderDetail.Any(od => od.PromosID.ToString().Equals(search))
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
                            if (fieldName == "promosid") promosId = Convert.ToInt32(value);
                            if (fieldName == "transdate") dateBetween = Convert.ToString(value);
                            query = fieldName switch
                            {
                                "refid" => query.Where(x => x.RefID.ToString().Equals(value)),
                                "orderno" => query.Where(x => x.OrderNo.Contains(value)),
                                "orderstatus" => query.Where(x => x.OrderStatus.Contains(value)),
                                //"transdate" => query.Where(x => x.TransDate.ToString().Contains(value)),
                                "customersid" => query.Where(x => x.CustomersID.ToString().Equals(value)),
                                "referenceno" => query.Where(x => x.ReferenceNo.Contains(value)),
                                //"promosid" => query.Where(x => x.OrderDetail.Any(od => od.PromosID.ToString().Equals(search))),
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
                            "orderno" => query.OrderByDescending(x => x.OrderNo),
                            "orderstatus" => query.OrderByDescending(x => x.OrderStatus),
                            "transdate" => query.OrderByDescending(x => x.TransDate),
                            "customersid" => query.OrderByDescending(x => x.CustomersID),
                            "referenceno" => query.OrderByDescending(x => x.ReferenceNo),
                            "dateup" => query.OrderByDescending(x => x.DateUp),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "orderno" => query.OrderBy(x => x.OrderNo),
                            "orderstatus" => query.OrderBy(x => x.OrderStatus),
                            "transdate" => query.OrderBy(x => x.TransDate),
                            "customersid" => query.OrderBy(x => x.CustomersID),
                            "referenceno" => query.OrderBy(x => x.ReferenceNo),
                            "dateup" => query.OrderBy(x => x.DateUp),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderByDescending(x => x.TransDate);
                }

                if(dateBetween != "")
                {
                    var dateSplit = dateBetween.Split("&", StringSplitOptions.RemoveEmptyEntries);
                    var start = Convert.ToDateTime(dateSplit[0].Trim());
                    var end = Convert.ToDateTime(dateSplit[1].Trim());
                    query = query.Where(x => x.TransDate >= start && x.TransDate <= end);

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

                foreach (var d in data)
                {
                    d.Reason = await _context.Reasons.FirstOrDefaultAsync(x => x.RefID == d.ReasonsID);
                    d.OrderDetail = await _context.OrderDetails
                            .Where(item => item.OrdersID == (d.RefID == 0 ? d.ID : d.RefID))
                            .Select(item => new OrderDetail
                            {
                                LeadTime = _context.ProductStatuses
                                    .Where(p => p.ProductID == item.ObjectID)
                                    .Select(x => x.LeadTime)
                                    .FirstOrDefault(),

                                ProductDetail = _context.ProductDetails2
                                    //.Where(x => x.RefID == item.ObjectID)
                                    .Select(x => new ProductDetail2
                                    {
                                        Type = x.Type,
                                        OriginID = x.OriginID,
                                        RefID = x.RefID,
                                        Name = x.Name,
                                        TokpedUrl = x.TokpedUrl,
                                        NewProd = x.NewProd,
                                        FavProd = x.FavProd,
                                        Image = x.Image,
                                        RealImage = x.RealImage,
                                        Weight = x.Weight,
                                        Price = x.Price,
                                        Stock = x.Stock,
                                        ClosuresID = x.ClosuresID,
                                        CategoriesID = x.CategoriesID,
                                        CategoryName = x.CategoryName,
                                        PlasticType = x.PlasticType,
                                        Functions = x.Functions,
                                        Tags = x.Tags,
                                        StockIndicator = x.StockIndicator,
                                        NecksID = x.NecksID,
                                        ColorsID = x.ColorsID,
                                        ShapesID = x.ShapesID,
                                        Volume = x.Volume,
                                        QtyPack = x.QtyPack,
                                        TotalShared = x.TotalShared,
                                        TotalViews = x.TotalViews,
                                        NewProdDate = x.NewProdDate,
                                        Height = x.Height,
                                        Length = x.Length,
                                        Width = x.Width,
                                        Diameter = x.Diameter,
                                        RimsID = x.RimsID,
                                        LidsID = x.LidsID,
                                        Status = x.Status,
                                        CountColor = x.CountColor,
                                        PackagingsID = x.PackagingsID,
                                        PackagingName = _context.Packagings
                                        .Where(pa => pa.RefID == x.PackagingsID)
                                        .Select(pa => pa.Name)
                                        .FirstOrDefault()
                                    }).FirstOrDefault(x => x.RefID == item.ObjectID && (item.ObjectType == "closures" ? x.Type == "closure" : item.ObjectType == x.Type)),

                                CompaniesID = item.CompaniesID,
                                Note = item.Note,
                                Outstanding = item.Outstanding,
                                FlagPromo = item.FlagPromo,
                                Amount = item.Amount,
                                ProductPrice = item.ProductPrice,
                                Qty = item.Qty,
                                QtyBox = item.QtyBox,
                                Type = item.Type,
                                PromosID = item.PromosID,
                                PromosType = item.PromosType,
                                ObjectType = item.ObjectType,
                                ParentID = item.ParentID,
                                ObjectID = item.ObjectID,
                                OrdersID = item.OrdersID,
                                RefID = item.RefID,
                            })
                            .ToListAsync();

                    foreach (var da in d.OrderDetail)
                    {
                        var accsExtOrders = await _svc.GetAccIdAsync<AccsExtOrder>(
                        da.ParentID == null ? 0 : (long)da.ParentID,
                        d == null ? 0 : (long)d.CustomersID,
                        (long)da.OrdersID,
                        da.ObjectType,
                        (long)da.ObjectID);

                        da.AccsExtOrders = accsExtOrders;
                    }
                }

                if (promosId != 0) data = data.Where(x => x.OrderDetail.Any(y => y.PromosID == promosId)).ToList();


                var cartids = data.Select(x => x.CartsID).Distinct().ToList();

                var orderGroup = cartids.Select(cart => new OrderGroup
                {
                    CartsID = cart,
                    Total = Convert.ToInt32(data.Where(p => p.CartsID == cart).Count()),
                    Orders = data.Where(p => p.CartsID == cart).ToList()
                });

                return new ListResponseProduct<OrderGroup>(orderGroup, total, page, null);
                //return new ListResponse<Order>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Order> GetByIdAsync(long id)
        {
            try
            {
                var data = await _context.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (data == null) return null;
                // Get the order details first
                var orderDetails = await _context.OrderDetails
                    .Where(item => item.OrdersID == data.RefID)
                    .ToListAsync();

                // Process each order detail to get LeadTime and AccsExtOrders
                foreach (var item in orderDetails)
                {
                    var leadTime = await _context.ProductStatuses
                        .Where(p => p.ProductID == item.ObjectID)
                        .Select(x => x.LeadTime)
                        .FirstOrDefaultAsync();

                    var accsExtOrders = await _svc.GetAccIdAsync<AccsExtOrder>(
                        item.ParentID == null ? 0 : (long)item.ParentID,
                        data == null ? 0 : (long)data.CustomersID,
                        (long)item.OrdersID,
                        item.ObjectType,
                        (long)item.ObjectID);

                    var productDetail = await _context.ProductDetails2
                        .Where(x => x.RefID == item.ObjectID)
                        .Select(x => new ProductDetail2
                        {
                            Type = x.Type,
                            OriginID = x.OriginID,
                            RefID = x.RefID,
                            Name = x.Name,
                            TokpedUrl = x.TokpedUrl,
                            NewProd = x.NewProd,
                            FavProd = x.FavProd,
                            Image = x.Image,
                            RealImage = x.RealImage,
                            Weight = x.Weight,
                            Price = x.Price,
                            Stock = x.Stock,
                            ClosuresID = x.ClosuresID,
                            CategoriesID = x.CategoriesID,
                            CategoryName = x.CategoryName,
                            PlasticType = x.PlasticType,
                            Functions = x.Functions,
                            Tags = x.Tags,
                            StockIndicator = x.StockIndicator,
                            NecksID = x.NecksID,
                            ColorsID = x.ColorsID,
                            ShapesID = x.ShapesID,
                            Volume = x.Volume,
                            QtyPack = x.QtyPack,
                            TotalShared = x.TotalShared,
                            TotalViews = x.TotalViews,
                            NewProdDate = x.NewProdDate,
                            Height = x.Height,
                            Length = x.Length,
                            Width = x.Width,
                            Diameter = x.Diameter,
                            RimsID = x.RimsID,
                            LidsID = x.LidsID,
                            Status = x.Status,
                            CountColor = x.CountColor
                        })
                        .FirstOrDefaultAsync();

                    // Now, map the OrderDetail and attach the fetched values
                    item.LeadTime = leadTime;
                    item.AccsExtOrders = accsExtOrders;
                    item.ProductDetail = productDetail;
                }

                // After processing, assign the orderDetails to your data
                data.OrderDetail = orderDetails;
                return await _context.Orders.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
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

