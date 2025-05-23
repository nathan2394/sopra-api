using Google.Cloud.Vision.V1;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
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

namespace Sopra.Services
{
    public interface PromoProductInterface
    {
        Task<PromoProduct> CreateAsync(PromoProduct data);
        Task<bool> DeleteAsync(long id, long userID);
        Task<PromoProduct> EditAsync(PromoProduct data);
        Task<ListResponse<PromoProduct>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date);
        Task<PromoProduct> GetByIdAsync(long id);
        Task<List<PromoDetail>> GetPromoDetail(long promoid, string type, long productid);
    }

    public class PromoProductService : PromoProductInterface
    {
        private readonly EFContext _context;

        public PromoProductService(EFContext context)
        {
            _context = context;
        }

        public async Task<PromoProduct> CreateAsync(PromoProduct data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                data.PromoMixId = data.PromoMixId == null ? 0 : data.PromoMixId;
                data.PromoJumboId = data.PromoJumboId == null ? 0 : data.PromoJumboId;
                if (data.PromoJumboId != 0) data.Price = 0;
                await _context.PromoProducts.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "PromoProduct", data.ID, "Add");

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
                var obj = await _context.PromoProducts.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = userID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "PromoProduct", id, "Delete");

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

        public async Task<PromoProduct> EditAsync(PromoProduct data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.PromoProducts.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.PromoJumboId = data.PromoJumboId;
                obj.PromoMixId = data.PromoMixId;
                obj.ProductsId = data.ProductsId;
                obj.Accs1Id = data.Accs1Id;
                obj.Accs2Id = data.Accs2Id;
                obj.Price = data.Price;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "PromoProduct", data.ID, "Edit");

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

        public async Task<ListResponse<PromoProduct>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.PromoProducts where a.IsDeleted == false select a;
                //query = query.Include(x => x.Products);
                //query = query.Include(x => x.PromoMix);
                //query = query.Include(x => x.PromoJumbo);
                //query = query.Include(x => x.Accs1);
                //query = query.Include(x => x.Accs2);

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.PromoJumboId.ToString().Contains(search)
                        || x.PromoMixId.ToString().Contains(search)
                        || x.ProductsId.ToString().Contains(search)
                        || x.Accs1Id.ToString().Contains(search)
                        || x.Accs2Id.ToString().Contains(search)
                        || x.Price.ToString().Contains(search));

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
                                "promojumboid" => query.Where(x => x.PromoJumboId.ToString().Contains(value)),
                                "promomixid" => query.Where(x => x.PromoMixId.ToString().Contains(value)),
                                "productsid" => query.Where(x => x.ProductsId.ToString().Contains(value)),
                                "accs1id" => query.Where(x => x.Accs1Id.ToString().Contains(value)),
                                "accs2id" => query.Where(x => x.Accs2Id.ToString().Contains(value)),
                                "price" => query.Where(x => x.Price.ToString().Contains(value)),
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
                            "promojumboid" => query.OrderByDescending(x => x.PromoJumboId),
                            "promomixid" => query.OrderByDescending(x => x.PromoMixId),
                            "productsid" => query.OrderByDescending(x => x.ProductsId),
                            "accs1id" => query.OrderByDescending(x => x.Accs1Id),
                            "accs2id" => query.OrderByDescending(x => x.Accs2Id),
                            "price" => query.OrderByDescending(x => x.Price),
                            "refid" => query.OrderByDescending(x => x.RefID),
                            _ => query
                        };
                    }
                    else
                    {
                        query = orderBy.ToLower() switch
                        {
                            "promojumboid" => query.OrderBy(x => x.PromoJumboId),
                            "promomixid" => query.OrderBy(x => x.PromoMixId),
                            "productsid" => query.OrderBy(x => x.ProductsId),
                            "accs1id" => query.OrderBy(x => x.Accs1Id),
                            "accs2id" => query.OrderBy(x => x.Accs2Id),
                            "price" => query.OrderBy(x => x.Price),
                            "refid" => query.OrderBy(x => x.RefID),
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
                var data = await query.Distinct().ToListAsync();
                if (data.Count <= 0 && page > 0)
                {
                    page = 0;
                    return await GetAllAsync(limit, page, total, search, sort, filter, date);
                }

                foreach (var item in data)
                {
                    var currentTimestamp = Utility.getCurrentTimestamps();

                    var promoJumboList = _context.Promos
                        .Where(x => x.RefID == item.PromoJumboId && x.Type == "Jumbo")
                        .AsEnumerable() // This forces the query to be executed and brings the data into memory
                        .Where(x => DateTime.Compare(Convert.ToDateTime(x.EndDate), currentTimestamp) >= 0)
                        .FirstOrDefault();

                    var promoMixList = _context.Promos
                        .Where(x => x.RefID == item.PromoMixId && x.Type == "Mix")
                        .AsEnumerable()
                        .Where(x => DateTime.Compare(Convert.ToDateTime(x.EndDate), currentTimestamp) >= 0)
                        .FirstOrDefault();

                    item.Product = _context.ProductDetails2.FirstOrDefault(x => x.RefID == Convert.ToInt64(item.ProductsId) && x.Type == "bottle");
                    item.Accs1 = _context.ProductDetails2.FirstOrDefault(x => x.RefID == Convert.ToInt64(item.Accs1Id) && x.Type == "closure");
                    item.Accs2 = _context.ProductDetails2.FirstOrDefault(x => x.RefID == Convert.ToInt64(item.Accs2Id) && x.Type == "closure");
                    item.PromoJumbo = promoJumboList;
                    item.PromoMix = promoMixList;
                }

                    return new ListResponse<PromoProduct>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<PromoProduct> GetByIdAsync(long id)
        {
            try
            {
                var data =  await _context.PromoProducts.AsNoTracking()
                        //.Include(x => x.Products)
                        //.Include(x => x.PromoMix)
                        //.Include(x => x.PromoJumbo)
                        //.Include(x => x.Accs1)
                        //.Include(x => x.Accs2)
                        .FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                data.PromoJumbo = _context.Promos.FirstOrDefault(x => x.RefID == data.PromoJumboId && x.Type == "Jumbo");
                data.PromoMix = _context.Promos.FirstOrDefault(x => x.RefID == data.PromoMixId && x.Type == "Mix ");
                data.Product = _context.ProductDetails2.FirstOrDefault(x => x.RefID == Convert.ToInt64(data.ProductsId) && x.Type == "bottle");
                data.Accs1 = _context.ProductDetails2.FirstOrDefault(x => x.RefID == Convert.ToInt64(data.Accs1Id) && x.Type == "closure");
                data.Accs2 = _context.ProductDetails2.FirstOrDefault(x => x.RefID == Convert.ToInt64(data.Accs2Id) && x.Type == "closure");
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

        public async Task<List<PromoDetail>> GetPromoDetail(long promoid, string type,long productid)
        {
            try
            {
                var getDate = Utility.getCurrentTimestamps();
                var promoDetail = await (
                    from p in _context.Promos
                    join pp in _context.PromoProducts on p.RefID equals pp.PromoMixId into ppMixGroup
                    from pp in ppMixGroup.DefaultIfEmpty()

                    join pp2 in _context.PromoProducts on p.RefID equals pp2.PromoJumboId into ppJumboGroup
                    from pp2 in ppJumboGroup.DefaultIfEmpty()

                    join pq in _context.PromoQuantities on p.RefID equals pq.PromoMixId into pqMixGroup
                    from pq in pqMixGroup.DefaultIfEmpty()

                    join pq2 in _context.PromoQuantities on p.RefID equals pq2.PromoJumboId into pqJumboGroup
                    from pq2 in pqJumboGroup.DefaultIfEmpty()

                    where p.RefID == promoid 
                    && p.Type == type
                    && p.EndDate >= getDate
                    && (pp.ProductsId == productid || pp2.ProductsId == productid)
                    select new PromoDetail
                    {
                        PromoId = p.RefID,
                        Type = p.Type,
                        ProductsId = p.Type == "Mix" ? pp.ProductsId : pp2.ProductsId,
                        Accs1Id = p.Type == "Mix" ? pp.Accs1Id : pp2.Accs1Id,
                        Accs2Id = p.Type == "Mix" ? pp.Accs2Id : pp2.Accs2Id,
                        Qty = p.Type == "Mix" ? pq.MinQuantity : pq2.MinQuantity,
                        Price = p.Type == "Mix" ? pp.Price : pq2.Price
                    }
                ).ToListAsync();

                if (promoDetail.Count <= 0) return null;

                foreach(var data in promoDetail)
                {
                    data.Product = _context.ProductDetails2.FirstOrDefault(x => x.RefID == Convert.ToInt64(data.ProductsId) && x.Type == "bottle");
                    data.Accs1 = _context.ProductDetails2.FirstOrDefault(x => x.RefID == Convert.ToInt64(data.Accs1Id) && x.Type == "closure");
                    data.Accs2 = _context.ProductDetails2.FirstOrDefault(x => x.RefID == Convert.ToInt64(data.Accs2Id) && x.Type == "closure");
                }

                return promoDetail;
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
