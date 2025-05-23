using Google.Api;
using Google.Protobuf.WellKnownTypes;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using Sopra.Entities;
using Sopra.Helpers;
using Sopra.Requests;
using Sopra.Responses;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Google.Cloud.Vision.V1.ProductSearchResults.Types;

namespace Sopra.Services
{
    public interface PromoInterface
    {
        Task<Promo> CreateAsync(Promo data);
        Task<bool> DeleteAsync(long id, long userID);
        Task<Promo> EditAsync(Promo data);
        Task<ListResponse<Promo>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date);
        Task<ListResponse<Promo>> GetAllAsyncNew(int limit, int page, int total, string search, string sort, string filter, string date);
        Task<Promo> GetByIdAsync(long id);
    }
    public class PromoService : PromoInterface
    {
        private readonly EFContext _context;

        public PromoService(EFContext context)
        {
            _context = context;
        }

        public async Task<Promo> CreateAsync(Promo data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.Promos.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Promo", data.ID, "Add");

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

        public async Task<bool> DeleteAsync(long id, long userID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Promos.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = userID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Promo", id, "Delete");

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

        public async Task<Promo> EditAsync(Promo data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.Promos.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.Name = data.Name;
                obj.PromoDesc = data.PromoDesc;
                obj.Type = data.Type;
                obj.StartDate = data.StartDate;
                obj.EndDate = data.EndDate;
                obj.Image= data.Image;
                obj.ImgThumb = data.ImgThumb;
                obj.Category = data.Category;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "Promo", data.ID, "Edit");

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

        public async Task<ListResponse<Promo>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                var productId = Convert.ToInt64(0);

                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.Promos where a.IsDeleted == false select a;
                //query = query.Where(x => DateTime.TryParse(x.EndDate, out DateTime endDate) && endDate >= DateTime.Today);

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Name.Contains(search)
                        || x.ID.ToString().Equals(search)
                        || x.RefID.ToString().Equals(search)
                        || x.PromoDesc.Contains(search)
                        || x.Type.Contains(search)
                        || x.Category.ToString().Contains(search)
                        || x.StartDate.ToString().Contains(search)
                        || x.EndDate.ToString().Contains(search));

                // Filtering
                if (!string.IsNullOrEmpty(filter))
                {
                    var filterList = filter.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    foreach (var f in filterList)
                    {
                        var searchList = f.Split(":", StringSplitOptions.RemoveEmptyEntries);
                        if (searchList.Length == 2)
                        {
                            //productsid
                            var fieldName = searchList[0].Trim().ToLower();
                            var value = searchList[1].Trim();
                            if (fieldName == "productsid") productId = Convert.ToInt64(value);

                            query = fieldName switch
                            {
                                "name" => query.Where(x => x.Name.Contains(value)),
                                "refid" => query.Where(x => x.RefID.ToString().Equals(value)),
                                "id" => query.Where(x => x.ID.ToString().Equals(value)),
                                "promodesc" => query.Where(x => x.PromoDesc.Contains(value)),
                                "type" => query.Where(x => x.Type.Contains(value)),
                                "category" => query.Where(x => x.Category.ToString().Contains(value)),
                                "startdate" => query.Where(x => x.StartDate.ToString().Contains(value)),
                                "enddate" => query.Where(x => x.EndDate.ToString().Contains(value)),
                                //"role" => query.Where(x => x.RoleID.Contains(value)),
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
                            "name" => query.OrderByDescending(x => x.Name),
                            "refid" => query.OrderByDescending(x => x.RefID),
                            "id" => query.OrderByDescending(x => x.ID),
                            "promodesc" => query.OrderByDescending(x => x.PromoDesc),
                            "type" => query.OrderByDescending(x => x.Type),
                            "category" => query.OrderByDescending(x => x.Category),
                            "startdate" => query.OrderByDescending(x => x.StartDate),
                            "enddate" => query.OrderByDescending(x => x.EndDate),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.Name),
                            "refid" => query.OrderBy(x => x.RefID),
                            "id" => query.OrderBy(x => x.ID),
                            "promodesc" => query.OrderBy(x => x.PromoDesc),
                            "type" => query.OrderBy(x => x.Type),
                            "category" => query.OrderBy(x => x.Category),
                            "startdate" => query.OrderBy(x => x.StartDate),
                            "enddate" => query.OrderBy(x => x.EndDate),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderBy(x => x.Name);
                }

                // Get Total Before Limit and Page
                //total = await query.CountAsync();

                

                // Get Data
                var data = await query.ToListAsync();
                if (data.Count <= 0 && page > 0)
                {
                    page = 0;
                    return await GetAllAsync(limit, page, total, search, sort, filter, date);
                }

                var getCurrentTime = Utility.getCurrentTimestamps();

                DateTime setGetCurrentTime = new DateTime(getCurrentTime.Year, getCurrentTime.Month, getCurrentTime.Day, 0, 0, 0);

                //validate endDate
                data = data.Where(x => x.EndDate >= setGetCurrentTime).ToList();

                // Get Total Before Limit and Page
                total = data.Count();

                // Set Limit and Page
                if (limit != 0)
                    data = data.Skip(page * limit).Take(limit).ToList();

                foreach (var item in data)
                {
                    // Union before Select for promo product
                    var resultMix = from p in _context.Promos
                                    join pp in _context.PromoProducts
                                    on new { RefId = p.RefID, PromoType = p.Type }
                                    equals new { RefId = pp.PromoMixId, PromoType = "Mix" }
                                    where p.Type == "Mix"
                                    && p.RefID==item.RefID
                                    && p.EndDate >= setGetCurrentTime
                                    && (pp.ProductsId == 0 || pp.ProductsId == productId)
                                    select pp;

                    var resultJumbo = from p in _context.Promos
                                      join pp in _context.PromoProducts
                                      on new { RefId = p.RefID, PromoType = p.Type }
                                      equals new { RefId = pp.PromoJumboId, PromoType = "Jumbo" }
                                      where p.Type == "Jumbo"
                                      && p.RefID == item.RefID
                                      && p.EndDate >= setGetCurrentTime
                                      && (pp.ProductsId == 0 || pp.ProductsId == productId)
                                      select pp;

                    PromoProduct finalResult = null;
                    //if (productId != 0) finalResult = resultMix.Union(resultJumbo).FirstOrDefault(x => x.ProductsId == productId);

                    if (resultMix != null && resultJumbo != null) finalResult = resultMix.Union(resultJumbo).FirstOrDefault();
                    else if (resultMix != null) finalResult = resultMix.FirstOrDefault();
                    else finalResult = resultJumbo.FirstOrDefault();
                    //if (finalResult == null) finalResult = resultMix.Union(resultJumbo).FirstOrDefault();

                    // Apply client-side logic after Union
                    if (finalResult != null)
                    {
                        item.PromoProduct = new PromoProduct
                        {
                            RefID = Convert.ToInt64(finalResult.RefID),
                            PromoJumboId = Convert.ToInt64(finalResult.PromoJumboId),
                            PromoMixId = Convert.ToInt64(finalResult.PromoMixId),
                            ProductsId = Convert.ToInt64(finalResult.ProductsId),
                            Accs1Id = Convert.ToInt64(finalResult.Accs1Id),
                            Accs2Id = Convert.ToInt64(finalResult.Accs2Id),
                            Price = Convert.ToDecimal(finalResult.Price),
                            Product = _context.ProductDetails2.FirstOrDefault(x => x.RefID == Convert.ToInt64(finalResult.ProductsId)  && x.Type == "bottle"),
                            Accs1 = _context.ProductDetails2.FirstOrDefault(x => x.RefID == Convert.ToInt64(finalResult.Accs1Id) && x.Type == "closure"),
                            Accs2 = _context.ProductDetails2.FirstOrDefault(x => x.RefID == Convert.ToInt64(finalResult.Accs2Id) && x.Type == "closure"),
                        };
                    }

                    // Union before Select for promo quantities
                    var resultQuantitiesJumbo = from p in _context.Promos
                                                join pp in _context.PromoQuantities
                                                on new { RefId = p.RefID, PromoType = p.Type }
                                                equals new { RefId = pp.PromoJumboId, PromoType = "Jumbo" }
                                                where p.Type == "Jumbo"
                                                && p.RefID == item.RefID
                                                select pp;

                    var resultQuantitiesMix = from p in _context.Promos
                                              join pp in _context.PromoQuantities
                                              on new { RefId = p.RefID, PromoType = p.Type }
                                              equals new { RefId = pp.PromoMixId, PromoType = "Mix" }
                                              where p.Type == "Mix" 
                                              && p.RefID == item.RefID
                                              select pp;

                    var finalQuantitiesResult = resultQuantitiesMix.Union(resultQuantitiesJumbo).FirstOrDefault();

                    // Apply client-side logic after Union
                    if (finalQuantitiesResult != null)
                    {
                        item.PromoQuantity = new PromoQuantity
                        {
                            RefID = Convert.ToInt64(finalQuantitiesResult.RefID),
                            PromoJumboId = Convert.ToInt64(finalQuantitiesResult.PromoJumboId),
                            PromoMixId = Convert.ToInt64(finalQuantitiesResult.PromoMixId),
                            MinQuantity = Convert.ToInt32(finalQuantitiesResult.MinQuantity),
                            Price = Convert.ToDecimal(finalQuantitiesResult.Price),
                        };
                    }
                }

                if (productId != 0)
                {
                    var temp = data.Where(x => x.PromoProduct != null).ToList();
                    data = temp.Where(x => x.PromoProduct.ProductsId == productId).ToList();
                }

                return new ListResponse<Promo>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<ListResponse<Promo>> GetAllAsyncNew(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                var productName = "";

                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from pp in _context.PromoProducts
                            
                            join pd in _context.ProductDetails2
                                on pp.ProductsId equals pd.RefID
                            where pd.Type != "closure" && pd.Type != "lid"

                            join pd2 in _context.ProductDetails2
                                on pp.Accs1Id equals pd2.RefID into pd2Group
                            from pd2 in pd2Group.DefaultIfEmpty()
                            where (pd2.Type != "bottle" && pd2.Type != "cup" && pd2.Type != "tray")

                            join p in _context.Promos
                                on pp.PromoMixId equals p.RefID
                            where p.Type == "Mix" && p.Name.Contains(".")

                            join ipq in (
                                from pq in _context.PromoQuantities
                                join p2 in _context.Promos
                                    on pq.PromoMixId equals p2.RefID
                                where p2.Type == "Mix"
                                group pq by pq.PromoMixId into g
                                select new
                                {
                                    PromoMixId = g.Key,
                                    Qty = g.Max(x => x.Level == 1 ? (int?)x.MinQuantity : 0),
                                    Qty2 = g.Max(x => x.Level == 2 ? (int?)x.MinQuantity : 0),
                                    Qty3 = g.Max(x => x.Level == 3 ? (int?)x.MinQuantity : 0)
                                }
                            )
                            on pp.PromoMixId equals ipq.PromoMixId
                            orderby p.Name
                            select new Promo
                            {
                                    RefID = p.RefID,
                                    Name = p.Name,
                                    PromoDesc = p.PromoDesc,
                                    Type = p.Type,
                                    StartDate = p.StartDate,
                                    EndDate = p.EndDate,
                                    Image = p.Image,
                                    ImgThumb = p.ImgThumb,
                                    Category = p.Category,
                                    PromoMix = new PromoMixDetail
                                    {
                                        ProductName = pd.Name,
                                        Accs1Name = pd2 != null ? pd2.Name : null,
                                        Accs2Name = null,
                                        PromoMixId = pp.PromoMixId,
                                        Price = pp.Price,
                                        Price2 = pp.Price2,
                                        Price3 = pp.Price3,
                                        Qty = ipq.Qty,
                                        Qty2 = ipq.Qty2,
                                        Qty3 = ipq.Qty3,
                                    }
                            };
                //query = query.Where(x => DateTime.TryParse(x.EndDate, out DateTime endDate) && endDate >= DateTime.Today);

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.Name.Contains(search)
                        || x.ID.ToString().Equals(search)
                        || x.RefID.ToString().Equals(search)
                        || x.PromoDesc.Contains(search)
                        || x.Type.Contains(search)
                        || x.Category.ToString().Contains(search)
                        || x.StartDate.ToString().Contains(search)
                        || x.EndDate.ToString().Contains(search));

                // Filtering
                if (!string.IsNullOrEmpty(filter))
                {
                    var filterList = filter.Split("|", StringSplitOptions.RemoveEmptyEntries);
                    foreach (var f in filterList)
                    {
                        var searchList = f.Split(":", StringSplitOptions.RemoveEmptyEntries);
                        if (searchList.Length == 2)
                        {
                            //productsid
                            var fieldName = searchList[0].Trim().ToLower();
                            var value = searchList[1].Trim();
                            if (fieldName == "productname") productName = Convert.ToString(value);

                            query = fieldName switch
                            {
                                "name" => query.Where(x => x.Name.Contains(value)),
                                "refid" => query.Where(x => x.RefID.ToString().Equals(value)),
                                "id" => query.Where(x => x.ID.ToString().Equals(value)),
                                "promodesc" => query.Where(x => x.PromoDesc.Contains(value)),
                                "type" => query.Where(x => x.Type.Contains(value)),
                                "category" => query.Where(x => x.Category.ToString().Contains(value)),
                                "startdate" => query.Where(x => x.StartDate.ToString().Contains(value)),
                                "enddate" => query.Where(x => x.EndDate.ToString().Contains(value)),
                                //"productname" => query.Where(x => x.PromoMix.ProductName.ToString().Contains(value)),
                                //"role" => query.Where(x => x.RoleID.Contains(value)),
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
                            "name" => query.OrderByDescending(x => x.Name),
                            "refid" => query.OrderByDescending(x => x.RefID),
                            "id" => query.OrderByDescending(x => x.ID),
                            "promodesc" => query.OrderByDescending(x => x.PromoDesc),
                            "type" => query.OrderByDescending(x => x.Type),
                            "category" => query.OrderByDescending(x => x.Category),
                            "startdate" => query.OrderByDescending(x => x.StartDate),
                            "enddate" => query.OrderByDescending(x => x.EndDate),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "name" => query.OrderBy(x => x.Name),
                            "refid" => query.OrderBy(x => x.RefID),
                            "id" => query.OrderBy(x => x.ID),
                            "promodesc" => query.OrderBy(x => x.PromoDesc),
                            "type" => query.OrderBy(x => x.Type),
                            "category" => query.OrderBy(x => x.Category),
                            "startdate" => query.OrderBy(x => x.StartDate),
                            "enddate" => query.OrderBy(x => x.EndDate),
                            _ => query
                        };
                    }
                }
                else
                {
                    query = query.OrderBy(x => x.Name);
                }

                // Get Total Before Limit and Page
                //total = await query.CountAsync();



                // Get Data
                var data = await query.ToListAsync();
                if (data.Count <= 0 && page > 0)
                {
                    page = 0;
                    return await GetAllAsyncNew(limit, page, total, search, sort, filter, date);
                }

                var getCurrentTime = Utility.getCurrentTimestamps();

                DateTime setGetCurrentTime = new DateTime(getCurrentTime.Year, getCurrentTime.Month, getCurrentTime.Day, 0, 0, 0);

                //validate endDate
                data = data.Where(x => x.EndDate >= setGetCurrentTime).ToList();

                // Get Total Before Limit and Page
                total = data.Count();

                // Set Limit and Page
                if (limit != 0)
                    data = data.Skip(page * limit).Take(limit).ToList();

                if (productName != "") data = data.Where(x => x.PromoMix.ProductName.Contains(productName)).ToList();

                return new ListResponse<Promo>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<Promo> GetByIdAsync(long id)
        {
            try
            {
                return await _context.Promos.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
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
