using Microsoft.EntityFrameworkCore;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Sopra.Entities;
using Sopra.Helpers;
using Sopra.Responses;
using Sopra.Requests;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Data;
using System.Net.Http;
using System.Text;

namespace Sopra.Services
{
    public interface CartDetailInterface
    {
        Task<CartDetail> CreateAsync(List<CartDetail> data,long userid,bool isIncrease);
        Task<bool> DeleteAsync(long id, long UserID);
        Task<CartDetail> EditAsync(List<CartDetail> data,long userid);
        Task<ListResponse<CartDetail>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date);
        Task<CartDetail> GetByIdAsync(long id);
        Task<List<T>> GetAccIdAsync<T>(long id, long customerId, long masterId, string objectType, long objectId) where T : class;

    }
    public class CartDetailService : CartDetailInterface
    {
        private readonly EFContext _context;

        public CartDetailService(EFContext context)
        {
            _context = context;
        }

        public async Task<CartDetail> CreateAsync(List<CartDetail> data, long userid, bool isIncrease)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"payload cart detail from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                foreach (var item in data)
                {
                    var obj = await _context.CartDetails.FirstOrDefaultAsync(x => x.ObjectID == item.ObjectID && x.ParentID == item.ParentID && x.CartsID == item.CartsID && x.Type == item.Type && x.IsDeleted == false && x.PromosID == item.PromosID);
                    if (obj != null)
                    {
                        obj.Qty = (isIncrease == true ? obj.Qty + item.Qty : item.Qty);
                        obj.DateUp = Utility.getCurrentTimestamps();
                    }
                    else
                    {
                        item.UserIn = userid;
                        await _context.CartDetails.AddAsync(item);
                    }
                    // Check Validate
                    await Utility.AfterSave(_context, "CartDetail", item.ID, "Add");
                }
                    
                await _context.SaveChangesAsync();
                Trace.WriteLine($"payload cart detail from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                await dbTrans.CommitAsync();

                return data[0];
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
                var objParent = await _context.CartDetails.Where(x => x.ParentID == id && x.Carts.CustomersID == UserID && x.IsDeleted == false).ToListAsync();
                if(objParent.Count() != 0)
                {
                    foreach (var item in objParent)
                    {
                        item.IsDeleted = true;
                        item.UserUp = UserID;
                        item.DateUp = DateTime.Now;
                        _context.CartDetails.Update(item);
                    }
                }

                var obj = await _context.CartDetails.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;




                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "CartDetail", id, "Delete");

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

        public async Task<CartDetail> EditAsync(List<CartDetail> data, long userid)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                Trace.WriteLine($"payload cart detail from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));
                var distinctCartIds = new HashSet<long>();
                var obj = null as CartDetail;
                foreach (var item in data)
                {
                    obj = await _context.CartDetails.FirstOrDefaultAsync(x => x.ID == item.ID && x.IsDeleted == false);
                    if (obj == null) return null;

                    obj.RefID = item.RefID;
                    obj.ParentID = item.ParentID;
                    obj.CartsID = item.CartsID;
                    obj.ObjectID = item.ObjectID;
                    obj.Type = item.Type;
                    obj.Notes = item.Notes;
                    obj.QtyBox = item.QtyBox;
                    obj.Qty = item.Qty;
                    obj.Price = item.Price;
                    obj.Amount = item.Amount;
                    obj.IsCheckout = item.IsCheckout;
                    obj.PromosID = item.PromosID;
                    obj.PromoType = item.PromoType;
                    obj.FlagPromo = item.FlagPromo;
                    obj.AccFlag = item.AccFlag;

                    obj.UserUp = item.UserUp;
                    obj.DateUp = Utility.getCurrentTimestamps();
                    await Utility.AfterSave(_context, "CartDetail", item.ID, "Edit");

                    // Add CartsID to the HashSet (duplicates will be ignored)
                    distinctCartIds.Add(Convert.ToInt64(obj.CartsID));
                }

                await _context.SaveChangesAsync();
                Trace.WriteLine($"payload cart detail from frontend = " + JsonConvert.SerializeObject(data, Formatting.Indented));

                await dbTrans.CommitAsync();

                // Convert the HashSet to an array if needed
                long[] uniqueCartIdsArray = distinctCartIds.ToArray();

                var dataCart = await _context.Carts
                    .Where(cart => uniqueCartIdsArray.Contains(cart.ID) || uniqueCartIdsArray.Contains(cart.RefID))
                    .ToListAsync();

                if(dataCart.Count > 0)
                {
                    foreach (var item in dataCart)
                    {
                        var cartDetail = await _context.CartDetails.Where(x => x.CartsID == item.RefID || x.CartsID == item.ID).ToListAsync();
                        var cartDetailFlagPromo = cartDetail.Where(x => x.FlagPromo == true && x.DateUp >= Utility.getCurrentTimestamps().AddHours(-1) && x.DateUp <= Utility.getCurrentTimestamps().AddMinutes(10)).ToList();
                        item.CartDetails = cartDetailFlagPromo;
                    }

                    var settings = new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                        Formatting = Formatting.Indented,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    };

                    var url = Utility.APIURL;
                    var postUrl = url + "/CartReguler";

                    string jsonString = JsonConvert.SerializeObject(dataCart, settings);
                    JObject jsonData = JObject.Parse("{ \"data\": " + jsonString + " }");
                    Trace.WriteLine($"payload = {jsonData}");
                    string json = Newtonsoft.Json.JsonConvert.SerializeObject(jsonData);

                    try
                    {
                        using (var httpClient = new HttpClient())
                        {
                            httpClient.BaseAddress = new Uri(url);

                            // Set content type to JSON
                            var content = new StringContent(json, Encoding.UTF8, "application/json");

                            // Send the POST request
                            var postResponse = await httpClient.PostAsync(postUrl, content);
                            var responseData = await postResponse.Content.ReadAsStringAsync();

                            if (postResponse.IsSuccessStatusCode)
                            {
                                Trace.WriteLine("Data sent successfully!");
                                Trace.WriteLine($"content : {responseData}");
                            }
                            else
                            {
                                Trace.WriteLine($"Failed to send data. Status Code: {postResponse.StatusCode}");
                                Trace.WriteLine($"content : {responseData}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error: {ex.Message}");
                    }
                }

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


        public async Task<ListResponse<CartDetail>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            var index=0;
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.CartDetails where a.IsDeleted == false select a;
                query = query.Include(x => x.Carts);

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Equals(search)
                        || x.CartsID.ToString().Equals(search)
                        || x.ObjectID.ToString().Equals(search)
                        || x.ParentID.ToString().Equals(search)
                        || x.PromosID.ToString().Equals(search)
                        || x.Type.Contains(search)
                        || x.QtyBox.ToString().Equals(search)
                        || x.Qty.ToString().Equals(search)
                        || x.Price.ToString().Equals(search)
                        || x.Amount.ToString().Equals(search)
                        || x.IsCheckout.ToString().Contains(search)
                        || x.Notes.Contains(search)
                        || x.Carts.CustomersID.ToString().Equals(search)
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
                                "cartsid" => query.Where(x => x.CartsID.ToString().Equals(value)),
                                "objectid" => query.Where(x => x.ObjectID.ToString().Equals(value)),
                                "promosid" => query.Where(x => x.PromosID.ToString().Equals(value)),
                                "type" => query.Where(x => x.Type.Contains(value)),
                                "notes" => query.Where(x => x.Notes.Contains(value)),
                                "qtybox" => query.Where(x => x.QtyBox.ToString().Contains(value)),
                                "qty" => query.Where(x => x.Qty.ToString().Contains(value)),
                                "price" => query.Where(x => x.Price.ToString().Equals(value)),
                                "amount" => query.Where(x => x.Amount.ToString().Equals(value)),
                                "ischeckout" => query.Where(x => x.IsCheckout.ToString().Contains(value)),
                                "customersid" => query.Where(x => x.Carts.CustomersID.ToString().Equals(value)),
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
                            "cartsid" => query.OrderByDescending(x => x.CartsID),
                            "objectid" => query.OrderByDescending(x => x.ObjectID),
                            "type" => query.OrderByDescending(x => x.Type),
                            "notes" => query.OrderByDescending(x => x.Notes),
                            "qtybox" => query.OrderByDescending(x => x.QtyBox),
                            "qty" => query.OrderByDescending(x => x.Qty),
                            "price" => query.OrderByDescending(x => x.Price),
                            "amount" => query.OrderByDescending(x => x.Amount),
                            "ischeckout" => query.OrderByDescending(x => x.IsCheckout),
                            "customersid" => query.OrderByDescending(x => x.Carts.CustomersID),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "parentid" => query.OrderBy(x => x.ParentID),
                            "cartsid" => query.OrderBy(x => x.CartsID),
                            "objectid" => query.OrderBy(x => x.ObjectID),
                            "type" => query.OrderBy(x => x.Type),
                            "notes" => query.OrderBy(x => x.Notes),
                            "qtybox" => query.OrderBy(x => x.QtyBox),
                            "qty" => query.OrderBy(x => x.Qty),
                            "price" => query.OrderBy(x => x.Price),
                            "amount" => query.OrderBy(x => x.Amount),
                            "ischeckout" => query.OrderBy(x => x.IsCheckout),
                            "customersid" => query.OrderBy(x => x.Carts.CustomersID),
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
                    var cart = await _context.Carts.FirstOrDefaultAsync(x => x.RefID == item.CartsID || x.ID == item.CartsID);
                    item.AccsExts = await GetAccIdAsync<AccsExt>(item.ID , cart == null ? 0 : (long)cart.CustomersID,(long)item.CartsID,item.Type,(long)item.ObjectID);
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
                        Diameter = x.Diameter,
                        RimsID = x.RimsID,
                        LidsID = x.LidsID,
                        Status = x.Status,
                        CountColor = x.CountColor
                    }).FirstOrDefaultAsync(x => x.RefID == item.ObjectID && x.Type == (item.Type == "closures" ? "closure" : item.Type));
                    item.LeadTime = Convert.ToDecimal(await _context.ProductStatuses.Where(p => p.ProductID == item.ObjectID).Select(x => x.LeadTime).FirstOrDefaultAsync());
                    item.Packagings = await _context.Packagings.FirstOrDefaultAsync(x => x.RefID == item.ProductDetail.PackagingsID);
                    index++;
                }

                return new ListResponse<CartDetail>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                Trace.WriteLine(index);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<CartDetail> GetByIdAsync(long id)
        {
            try
            {
                var data = await _context.CartDetails.AsNoTracking()
                    .Include(x => x.Carts)  
                    .FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (data == null) return null;
                var cart = await _context.Carts.FirstOrDefaultAsync(x => x.RefID == data.CartsID || x.ID == data.CartsID);
                data.AccsExts = await GetAccIdAsync<AccsExt>(data.ParentID == null ? 0 : (long)data.ParentID, cart == null ? 0 : (long)cart.CustomersID,(long)data.CartsID,data.Type, (long)data.ObjectID);
                data.ProductDetail = await _context.ProductDetails2.Select(x => new ProductDetail2
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
                    //Whistlist = x.Whistlist,
                    Diameter = x.Diameter,
                    RimsID = x.RimsID,
                    LidsID = x.LidsID,
                    Status = x.Status,
                    CountColor = x.CountColor
                }).FirstOrDefaultAsync(x => x.RefID == data.ObjectID);
                data.LeadTime = Convert.ToDecimal(await _context.ProductStatuses.Where(p => p.ProductID == data.ObjectID).Select(x => x.LeadTime).FirstOrDefaultAsync());
                data.Packagings = await _context.Packagings.FirstOrDefaultAsync(x => x.RefID == data.ProductDetail.PackagingsID);
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

        public async Task<List<T>> GetAccIdAsync<T>(long id, long customerId,long masterId,string objectType,long objectId) where T : class
        {
            var customerid = customerId;
            var Id = id;
            try
            {

                if (typeof(T) == typeof(AccsExt))
                {
                    var cardDetail = await _context.CartDetails.AsNoTracking()
                    .Include(x => x.Carts)
                    .FirstOrDefaultAsync(x => x.ID == Id && x.CartsID == masterId && x.Type == objectType && x.IsDeleted == false);

                    if (cardDetail == null) return null;

                    var parent = await _context.Carts.FirstOrDefaultAsync(x => x.CustomersID == customerId && (x.ID == cardDetail.CartsID || x.RefID == cardDetail.CartsID));


                    if (parent == null) return null;


                    if (cardDetail.Type == "bottle")
                    {
                        var cartDetailData = _context.CartDetails
                            .Join(_context.Closures, cd => cd.ObjectID, c => c.RefID, (cd, c) => new { cd, c })
                            .Where(x => x.cd.ParentID == cardDetail.ParentID && x.cd.Type == "closures" && (x.cd.CartsID == parent.RefID || x.cd.CartsID == parent.ID) && x.cd.IsDeleted == false && x.cd.PromosID == cardDetail.PromosID)
                            .Select(x => new { x.cd.ID, x.cd.Price ,x.cd.Qty,x.cd.QtyBox}).FirstOrDefault();

                        if (cartDetailData != null)
                        {
                            var data = await (from cd in _context.CartDetails
                                          join c in _context.Closures on cd.ObjectID equals c.RefID
                                              where cd.ParentID == cardDetail.ParentID 
                                                && (cd.CartsID == parent.RefID || cd.CartsID == parent.ID) 
                                                && cd.IsDeleted == false
                                                && cd.Type == "closures"
                                                && cd.PromosID == cardDetail.PromosID
                                              let realImage = _context.Images.Where(x => x.ObjectID == c.RefID && x.Type == "Closures")
                                                  .OrderBy(x => x.RefID)
                                                  .Select(x => x.ProductImage)
                                                  .FirstOrDefault()
                                              select new AccsExt
                                          {
                                              RefID = cardDetail.RefID,
                                              CartsID = cardDetail.CartsID,
                                              CartDetailsID = cd.ID,
                                              ObjectID = c.RefID,
                                              ParentID = cardDetail.ParentID,
                                              Type = "closures",
                                              QtyBox = cd.QtyBox,
                                              Qty = cd.Qty,
                                              Price = cd.Price,
                                              Amount = cardDetail.Amount,
                                              IsCheckout = cardDetail.IsCheckout,
                                                  Notes = cd.Notes,
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
                                                                   Height = closures.Height,
                                                                   Weight = closures.Weight,
                                                                   Diameter= closures.Diameter,
                                                                   Length=0,

                                                               }).FirstOrDefault(),
                                                  LeadTime = Convert.ToDecimal(_context.ProductStatuses.Where(p => p.ProductName.Contains(c.Name)).Select(x => x.LeadTime).FirstOrDefault()),
                                              }).Distinct().ToListAsync();
                            return data as List<T>;
                        }


                    }
                    else if (cardDetail.Type == "cup" || cardDetail.Type == "tray")
                    {
                        
                        var cartDetailData = _context.CartDetails
                            .Join(_context.Lids, cd => cd.ObjectID, c => c.RefID, (cd, c) => new { cd, c })
                            .Where(x => x.cd.ParentID == cardDetail.ParentID && x.cd.Type == "lid" && (x.cd.CartsID == parent.RefID || x.cd.CartsID == parent.ID) && x.cd.IsDeleted == false && x.cd.PromosID == cardDetail.PromosID)
                            .Select(x => new { x.cd.ID, x.cd.Price, x.cd.Qty, x.cd.QtyBox }).FirstOrDefault();

                        if (cartDetailData != null)
                        {
                            var data = await (from cd in _context.CartDetails
                                          join c in _context.Lids on cd.ObjectID equals c.RefID
                                              where cd.ParentID == cardDetail.ParentID 
                                                      && (cd.CartsID == parent.RefID || cd.CartsID == parent.ID) 
                                                      && cd.IsDeleted == false
                                                      && cd.Type == "lid"
                                                      && cd.PromosID == cardDetail.PromosID
                                              let realImage = _context.Images.Where(x => x.ObjectID == c.RefID && x.Type == "Lids")
                                                  .OrderBy(x => x.RefID)
                                                  .Select(x => x.ProductImage)
                                                  .FirstOrDefault()
                                              select new AccsExt
                                          {
                                              RefID = cardDetail.RefID,
                                              CartsID = cardDetail.CartsID,
                                              CartDetailsID = cd.ID,
                                              ObjectID = c.RefID,
                                              ParentID = cardDetail.ParentID,
                                              Type = "lid",
                                              QtyBox = cd.QtyBox,
                                              Qty = cd.Qty,
                                              Price = cd.Price,
                                              Amount = cardDetail.Amount,
                                              IsCheckout = cardDetail.IsCheckout,
                                                  Notes = cd.Notes,
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
                                                                   Height = thermo.Height,
                                                                   Weight = thermo.Weight,
                                                                   Diameter = 0,
                                                                   Length= thermo.Length,
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

                return null;
            }
        }
    }
}

