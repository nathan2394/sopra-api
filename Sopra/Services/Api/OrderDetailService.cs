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
using Newtonsoft.Json;

namespace Sopra.Services
{
    public class OrderDetailService : IServiceAsync<OrderDetail>
    {
        private readonly EFContext _context;

        public OrderDetailService(EFContext context)
        {
            _context = context;
        }

        public async Task<OrderDetail> CreateAsync(OrderDetail data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"payload order detail from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                data.RefID = 0;
                await _context.OrderDetails.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "OrderDetail", data.ID, "Add");

                await dbTrans.CommitAsync();
                Trace.WriteLine($"payload order after save data into database = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"error save data order detail,payload = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                await dbTrans.RollbackAsync();
                Trace.WriteLine($"rollback db");

                throw;
            }
        }

        public async Task<bool> DeleteAsync(long id, long UserID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.OrderDetails.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "OrderDetail", id, "Delete");

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

        public async Task<OrderDetail> EditAsync(OrderDetail data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"payload order detail from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                var obj = await _context.OrderDetails.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.ParentID = data.ParentID;
                obj.OrdersID = data.OrdersID;
                obj.ObjectID = data.ObjectID;
                obj.ObjectType = data.ObjectType;
                obj.PromosID = data.PromosID;
                obj.Type = data.Type;
                obj.QtyBox = data.QtyBox;
                obj.Qty = data.Qty;
                obj.ProductPrice = data.ProductPrice;
                obj.Amount = data.Amount;
                obj.FlagPromo = data.FlagPromo;
                obj.Outstanding = data.Outstanding;
                obj.Note = data.Note;
                obj.CompaniesID = data.CompaniesID;
                obj.AccFlag = data.AccFlag;

                obj.UserUp = data.UserUp;
                obj.DateUp = Utility.getCurrentTimestamps();

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "OrderDetail", data.ID, "Edit");

                await dbTrans.CommitAsync();
                Trace.WriteLine($"payload order detail after save data into database = " + JsonConvert.SerializeObject(obj, Formatting.Indented));

                return obj;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                Trace.WriteLine($"error save data order detail,payload = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                await dbTrans.RollbackAsync();
                Trace.WriteLine($"rollback db");

                throw;
            }
        }


        public async Task<ListResponse<OrderDetail>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.OrderDetails where a.IsDeleted == false select a;
                //query = query.Include(x => x.Orders);

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Contains(search)
                        || x.OrdersID.ToString().Equals(search)
                        || x.ParentID.ToString().Equals(search)
                        || x.ObjectID.ToString().Equals(search)
                        || x.ObjectType.Contains(search)
                        || x.PromosID.ToString().Equals(search)
                        || x.Type.Contains(search)
                        || x.QtyBox.ToString().Equals(search)
                        || x.Qty.ToString().Equals(search)
                        || x.ProductPrice.ToString().Equals(search)
                        || x.Amount.ToString().Equals(search)
                        || x.FlagPromo.ToString().Contains(search)
                        || x.Outstanding.ToString().Contains(search)
                        || x.Note.Contains(search)
                        || x.CompaniesID.ToString().Equals(search)
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
                                "refid" => query.Where(x => x.RefID.ToString().Equals(value)),
                                "parentid" => query.Where(x => x.ParentID.ToString().Equals(value)),
                                "ordersid" => query.Where(x => x.OrdersID.ToString().Equals(value)),
                                "objectid" => query.Where(x => x.ObjectID.ToString().Equals(value)),
                                "objecttype" => query.Where(x => x.ObjectType.Contains(value)),
                                "promosid" => query.Where(x => x.PromosID.ToString().Equals(value)),
                                "type" => query.Where(x => x.Type.Contains(value)),
                                "qtybox" => query.Where(x => x.QtyBox.ToString().Equals(value)),
                                "qty" => query.Where(x => x.Qty.ToString().Equals(value)),
                                "productprice" => query.Where(x => x.ProductPrice.ToString().Equals(value)),
                                "amount" => query.Where(x => x.Amount.ToString().Equals(value)),
                                "flagpromo" => query.Where(x => x.FlagPromo.ToString().Contains(value)),
                                "outstanding" => query.Where(x => x.Outstanding.ToString().Contains(value)),
                                "note" => query.Where(x => x.Note.Contains(value)),
                                "companiesid" => query.Where(x => x.CompaniesID.ToString().Equals(value)),
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
                            "parentid" => query.OrderByDescending(x => x.ParentID),
                            "ordersid" => query.OrderByDescending(x => x.OrdersID),
                            "objectid" => query.OrderByDescending(x => x.ObjectID),
                            "objecttype" => query.OrderByDescending(x => x.ObjectType),
                            "promosid" => query.OrderByDescending(x => x.PromosID),
                            "type" => query.OrderByDescending(x => x.Type),
                            "qtybox" => query.OrderByDescending(x => x.QtyBox),
                            "qty" => query.OrderByDescending(x => x.Qty),
                            "productprice" => query.OrderByDescending(x => x.ProductPrice),
                            "amount" => query.OrderByDescending(x => x.Amount),
                            "flagpromo" => query.OrderByDescending(x => x.FlagPromo),
                            "outstanding" => query.OrderByDescending(x => x.Outstanding),
                            "note" => query.OrderByDescending(x => x.Note),
                            "companiesid" => query.OrderByDescending(x => x.CompaniesID),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "parentid" => query.OrderBy(x => x.ParentID),
                            "ordersid" => query.OrderBy(x => x.OrdersID),
                            "objectid" => query.OrderBy(x => x.ObjectID),
                            "objecttype" => query.OrderBy(x => x.ObjectType),
                            "promosid" => query.OrderBy(x => x.PromosID),
                            "type" => query.OrderBy(x => x.Type),
                            "qtybox" => query.OrderBy(x => x.QtyBox),
                            "qty" => query.OrderBy(x => x.Qty),
                            "productprice" => query.OrderBy(x => x.ProductPrice),
                            "amount" => query.OrderBy(x => x.Amount),
                            "flagpromo" => query.OrderBy(x => x.FlagPromo),
                            "outstanding" => query.OrderBy(x => x.Outstanding),
                            "note" => query.OrderBy(x => x.Note),
                            "companiesid" => query.OrderBy(x => x.CompaniesID),
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

                foreach (var item in data)
                {
                    var order = await _context.Orders.FirstOrDefaultAsync(x => x.RefID == item.OrdersID || x.ID == item.OrdersID);
                    item.AccsExtOrders = await GetAccIdAsync<AccsExtOrder>(item.ParentID == null ? 0 : (long)item.ParentID, order == null ? 0 : (long)order.CustomersID,(long)item.OrdersID,item.ObjectType, (long)item.ObjectID);
                    item.ProductDetail = await _context.ProductDetails2.Select(x => new ProductDetail2
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
                        PackagingsID = x.PackagingsID,
                        //Whistlist = x.Whistlist,
                        Diameter = x.Diameter,
                        RimsID = x.RimsID,
                        LidsID = x.LidsID,
                        Status = x.Status,
                        CountColor = x.CountColor
                    }).FirstOrDefaultAsync(x => x.RefID == item.ObjectID);
                    item.LeadTime = Convert.ToDecimal(await _context.ProductStatuses.Where(p => p.ProductID == item.ObjectID).Select(x => x.LeadTime).FirstOrDefaultAsync());
                }

                return new ListResponse<OrderDetail>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<OrderDetail> GetByIdAsync(long id)
        {
            try
            {
                var data = await _context.OrderDetails.AsNoTracking().Include(x => x.Orders).FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if(data==null) return null;
                var order = await _context.Orders.FirstOrDefaultAsync(x => x.RefID == data.OrdersID || x.ID == data.OrdersID);
                data.AccsExtOrders = await GetAccIdAsync<AccsExtOrder>(data.ParentID == null ? 0 : (long)data.ParentID, order == null ? 0 : (long)order.CustomersID,(long)data.OrdersID,data.ObjectType,(long)data.ObjectID);
                data.LeadTime = Convert.ToDecimal(await _context.ProductStatuses.Where(p => p.ProductID == data.ObjectID).Select(x => x.LeadTime).FirstOrDefaultAsync());
                return data;
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<OrderDetail> ChangePassword(ChangePassword obj, long id) { return null; }

        public Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid)
        {
            throw new NotImplementedException();
        }

        public async Task<List<T>> GetAccIdAsync<T>(long id, long customerId,long masterId,string objectType,long objectId) where T : class
        {
            var customerid = customerId;
            var Id = id;
            try
            {
                if (typeof(T) == typeof(AccsExtOrder))
                {
                    var cardDetail = await _context.OrderDetails.AsNoTracking()
                    .Include(x => x.Orders)
                    .FirstOrDefaultAsync(x => x.ParentID == Id && x.OrdersID == masterId && x.ObjectType == objectType && x.IsDeleted == false);

                    if (cardDetail == null) return null;

                    var parent = await _context.Orders.FirstOrDefaultAsync(x => x.CustomersID == customerId && (x.RefID == cardDetail.OrdersID || x.ID == cardDetail.OrdersID));


                    if (parent == null) return null;


                    if (cardDetail.ObjectType == "bottle")
                    {
                        var cartDetailData = _context.OrderDetails
                            .Join(_context.Closures, cd => cd.ObjectID, c => c.RefID, (cd, c) => new { cd, c })
                            .Where(x => x.cd.ParentID == cardDetail.ParentID && x.cd.ObjectType == "closures" && x.cd.OrdersID == cardDetail.OrdersID && (x.cd.OrdersID == parent.RefID || x.cd.OrdersID == parent.ID) && x.cd.IsDeleted == false)
                            .Select(x => new { x.cd.ID, x.c.Price, x.cd.Qty, x.cd.QtyBox }).FirstOrDefault();

                        if (cartDetailData != null)
                        {
                            var data = await (from cd in _context.OrderDetails
                                              join c in _context.Closures on cd.ObjectID equals c.RefID
                                              where cd.ParentID == cardDetail.ParentID 
                                                && (cd.OrdersID == parent.RefID || cd.OrdersID == parent.ID) 
                                                && cd.IsDeleted == false
                                                && cd.ObjectType == "closures"
                                              let realImage = _context.Images.Where(x => x.ObjectID == c.RefID && x.Type == "Closures")
                                                  .OrderBy(x => x.RefID)
                                                  .Select(x => x.ProductImage)
                                                  .FirstOrDefault()
                                              select new AccsExtOrder
                                              {
                                                  RefID = cardDetail.RefID,
                                                  OrdersID= cardDetail.OrdersID,
                                                  OrderDetailsID = cd.ID,
                                                  ObjectID = c.RefID,
                                                  ParentID = cardDetail.ParentID,
                                                  Type = "closures",
                                                  QtyBox = cd.QtyBox,
                                                  Qty = cd.Qty,
                                                  Price = c.Price,
                                                  Amount = cardDetail.Amount,
                                                  Note = cd.Note,
                                                  ProductDetail = (from closures in _context.Closures
                                                                   where closures.RefID == c.RefID
                                                                          && closures.IsDeleted == false
                                                                   select new Product
                                                                   {
                                                                       ID = closures.ID,
                                                                       RefID = closures.RefID,
                                                                       Name = closures.Name,
                                                                       Image = closures.Image,
                                                                       Price = closures.Price,
                                                                       QtyPack = closures.QtyPack,
                                                                       RealImage = realImage,
                                                                       Type = "closures",

                                                                   }).FirstOrDefault(),
                                                  LeadTime = Convert.ToDecimal(_context.ProductStatuses.Where(p => p.ProductName.Contains(c.Name)).Select(x => x.LeadTime).FirstOrDefault()),
                        }).Distinct().ToListAsync();
                            return data as List<T>;
                        }


                    }
                    else if (cardDetail.ObjectType == "cup" || cardDetail.ObjectType == "tray")
                    {
                        
                        var cartDetailData = _context.OrderDetails
                            .Join(_context.Lids, cd => cd.ObjectID, c => c.RefID, (cd, c) => new { cd, c })
                            .Where(x => x.cd.ParentID == cardDetail.ParentID && x.cd.ObjectType == "lid"&& x.cd.OrdersID == cardDetail.OrdersID && (x.cd.OrdersID == parent.RefID || x.cd.OrdersID == parent.ID) && x.cd.IsDeleted == false)
                            .Select(x => new { x.cd.ID, x.c.Price, x.cd.Qty, x.cd.QtyBox }).FirstOrDefault();

                        if (cartDetailData != null)
                        {
                            var data = await (from cd in _context.OrderDetails
                                              join c in _context.Lids on cd.ObjectID equals c.RefID
                                              where cd.ParentID == cardDetail.ParentID 
                                                        && (cd.OrdersID == parent.RefID || cd.OrdersID == parent.ID) 
                                                        && cd.IsDeleted == false
                                                        && cd.ObjectType == "lid"
                                              let realImage = _context.Images.Where(x => x.ObjectID == c.RefID && x.Type == "Lids")
                                                  .OrderBy(x => x.RefID)
                                                  .Select(x => x.ProductImage)
                                                  .FirstOrDefault()
                                              select new AccsExtOrder
                                              {
                                                  RefID = cardDetail.RefID,
                                                  OrdersID= cardDetail.OrdersID,
                                                  OrderDetailsID = cd.ID,
                                                  ObjectID = c.RefID,
                                                  ParentID = cardDetail.ParentID,
                                                  Type = "lid",
                                                  QtyBox = cd.QtyBox,
                                                  Qty = cd.Qty,
                                                  Price = c.Price,
                                                  Amount = cardDetail.Amount,
                                                  Note = cd.Note,
                                                  ProductDetail = (from thermo in _context.Lids
                                                                   where thermo.RefID == cd.ObjectID
                                                                          && thermo.IsDeleted == false
                                                                   select new Product
                                                                   {
                                                                       ID = thermo.ID,
                                                                       RefID = thermo.RefID,
                                                                       Name = thermo.Name,
                                                                       Image = thermo.Image,
                                                                       Price = thermo.Price,
                                                                       QtyPack = thermo.Qty,
                                                                       RealImage = realImage,
                                                                       Type = "lid",

                                                                   }).FirstOrDefault(),
                                                  LeadTime = Convert.ToDecimal(_context.ProductStatuses.Where(p => p.ProductName.Contains(c.Name)).Select(x => x.LeadTime).FirstOrDefault()),
                                              }).Distinct().ToListAsync();
                            return data as List<T>;
                        }
                    }
                    else
                    {
                        return null;
                    }
                    return null;
                }
                    return null;
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

