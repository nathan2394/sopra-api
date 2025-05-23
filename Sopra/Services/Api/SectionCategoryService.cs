using Microsoft.EntityFrameworkCore;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Sopra.Entities;
using Sopra.Helpers;
using Sopra.Responses;
using System.Net.NetworkInformation;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using Sopra.Requests;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR;

namespace Sopra.Services
{
    public interface SectionCategoryInterface
    {
        //Task<ListResponseProduct<SectionCategoryGroup>> GetAllAsync(int limit, int page, int total, string search, string sort,
        //string filter, string date);
        Task<ListResponse<SectionCategory>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date, int? userid);
        Task<SectionCategory> GetByIdAsync(long id);
        Task<SectionCategory> CreateAsync(SectionCategory data);
        Task<SectionCategory> EditAsync(SectionCategory data);
        Task<bool> DeleteAsync(long id, long userID);
    }

    public class SectionCategoryService : SectionCategoryInterface
    {
        private readonly EFContext _context;

        public SectionCategoryService(EFContext context)
        {
            _context = context;
        }

        public async Task<SectionCategory> CreateAsync(SectionCategory data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                await _context.SectionCategorys.AddAsync(data);
                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "SectionCategory", data.ID, "Add");

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

        public async Task<bool> DeleteAsync(long id, long UserID)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.SectionCategorys.FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                if (obj == null) return false;

                obj.IsDeleted = true;
                obj.UserUp = UserID;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "SectionCategory", id, "Delete");

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

        public async Task<SectionCategory> EditAsync(SectionCategory data)
        {
            await using var dbTrans = await _context.Database.BeginTransactionAsync();
            try
            {
                var obj = await _context.SectionCategorys.FirstOrDefaultAsync(x => x.ID == data.ID && x.IsDeleted == false);
                if (obj == null) return null;

                obj.RefID = data.RefID;
                obj.SectionTitle = data.SectionTitle;
                obj.SectionTitleEN = data.SectionTitleEN;
                obj.Seq = data.Seq;
                obj.Status = data.Status;
                obj.ImgBannerDesktop = data.ImgBannerDesktop;
                obj.ImgBannerMobile = data.ImgBannerMobile;
                obj.Description = data.Description;

                obj.UserUp = data.UserUp;
                obj.DateUp = DateTime.Now;

                await _context.SaveChangesAsync();

                // Check Validate
                await Utility.AfterSave(_context, "SectionCategory", data.ID, "Edit");

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


        public async Task<ListResponse<SectionCategory>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date, int? userid)
        {
            try
            {
                _context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
                var query = from a in _context.SectionCategorys
                            where a.IsDeleted == false
                            && a.Status == "Active"
                            orderby a.Seq ascending
                            let sectionCategoryList = (from b in _context.SectionCategoryKeys
                                                       join c in _context.ProductCategorys on b.PoductCategoriesID equals c.RefID
                                                       join d in _context.SectionCategorys on b.SectionCategoriesID equals d.RefID
                                                       where d.Status == "Active"
                                                       && d.ID == a.ID
                                                       select new ProductCategory 
													   {
														   ID = c.ID,
														   RefID = c.RefID,
														   Name = c.Name,
														   RealImage = b.ImgTheme,
														   Image =  c.Image,
														   Keyword = c.Keyword,
														   Type = c.Type,
														   Seq = c.Seq
													   })
                                                      .ToList()
                            //let product = (from pd in _context.ProductDetails2
                            //               where sectionCategoryList.Any(sc => pd.Name.Contains(sc.Name))
                            //               let qtyCart = (from c in _context.Carts
                            //                              join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
                            //                              from carts_detail in carts.DefaultIfEmpty()
                            //                              where c.CustomersID == userid
                            //                       && carts_detail.ObjectID == pd.RefID
                            //                       && carts_detail.Type == (carts_detail.Type == "closures" ? "closure" : pd.Type)
                            //                       && carts_detail.IsDeleted == false
                            //                              select carts_detail.Qty
                            //                    ).Sum()
                            //               select new Product
                            //               {
                            //                   Type = pd.Type,
                            //                   ID = Convert.ToInt64(pd.OriginID),
                            //                   RefID = pd.RefID,
                            //                   Name = pd.Name,
                            //                   TokpedUrl = pd.TokpedUrl,
                            //                   NewProd = pd.NewProd,
                            //                   FavProd = pd.FavProd,
                            //                   Image = pd.Image,
                            //                   RealImage = pd.RealImage,
                            //                   Weight = pd.Weight,
                            //                   Price = pd.Price,
                            //                   Stock = pd.Stock,
                            //                   ClosuresID = pd.ClosuresID,
                            //                   CategoriesID = pd.CategoriesID,
                            //                   CategoryName = pd.CategoryName,
                            //                   PlasticType = pd.PlasticType,
                            //                   //Functions = pd.Functions,
                            //                   //Tags = pd.Tags,
                            //                   StockIndicator = pd.StockIndicator,
                            //                   NecksID = pd.NecksID,
                            //                   ColorsID = pd.ColorsID,
                            //                   ShapesID = pd.ShapesID,
                            //                   Volume = pd.Volume,
                            //                   QtyPack = pd.QtyPack,
                            //                   TotalShared = pd.TotalShared,
                            //                   TotalViews = pd.TotalViews,
                            //                   NewProdDate = pd.NewProdDate,
                            //                   Height = pd.Height,
                            //                   Length = pd.Length,
                            //                   Width = pd.Width,
                            //                   //Whistlist = pd.Whistlist,
                            //                   Diameter = pd.Diameter,
                            //                   RimsID = pd.RimsID,
                            //                   LidsID = pd.LidsID,
                            //                   //Status = pd.Status,
                            //                   CountColor = pd.CountColor,
                            //                   QtyCart = Convert.ToInt64(qtyCart)
                            //               }).ToList()
                            select new SectionCategory
                            {
                                ID = a.ID,
								RefID = a.RefID,
								SectionTitle = a.SectionTitle,
                                SectionTitleEN = a.SectionTitleEN,
                                Seq = a.Seq,
								Status = a.Status,
								ImgBannerDesktop = a.ImgBannerDesktop,
								ImgBannerMobile = a.ImgBannerMobile,
								Description = a.Description,
								CategoryList = sectionCategoryList,
                                //Product = a.SectionTitle == "New Collection" ? product : null,
                            };

                // Searching
                if (!string.IsNullOrEmpty(search))
                    query = query.Where(x => x.RefID.ToString().Contains(search)
                        || x.SectionTitle.Contains(search)
                        || x.Seq.ToString().Contains(search)
                        || x.Status.Contains(search)
                        || x.ImgBannerDesktop.Contains(search)
                        || x.ImgBannerMobile.Contains(search)
                        || x.Description.Contains(search)
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
                                "refid" => query.Where(x => x.RefID.ToString().Contains(value)),
                                "sectiontitle" => query.Where(x => x.SectionTitle.Contains(value)),
                                "sectiontitleen" => query.Where(x => x.SectionTitleEN.Contains(value)),
                                "seq" => query.Where(x => x.Seq.ToString().Contains(value)),
                                "status" => query.Where(x => x.Status.Contains(value)),
                                "imgbannerdesktop" => query.Where(x => x.ImgBannerDesktop.Contains(value)),
                                "imgbannermobile" => query.Where(x => x.ImgBannerMobile.Contains(value)),
                                "description" => query.Where(x => x.Description.Contains(value)),
                                _ => query
                            };
                        }
                    }
                }

                // Sorting
                if (!string.IsNullOrEmpty(sort))
                {
                    var temp = sort.Split(',', StringSplitOptions.RemoveEmptyEntries);
                    var SectionCategoryBy = sort;
                    if (temp.Length > 1)
                        SectionCategoryBy = temp[0];

                    if (temp.Length > 1)
                    {
                        query = SectionCategoryBy.ToLower() switch
                        {
                            "refid" => query.OrderByDescending(x => x.RefID),
                            "sectiontitle" => query.OrderByDescending(x => x.SectionTitle),
                            "sectiontitleen" => query.OrderByDescending(x => x.SectionTitleEN),
                            "seq" => query.OrderByDescending(x => x.Seq),
                            "status" => query.OrderByDescending(x => x.Status),
                            "imgbannerdesktop" => query.OrderByDescending(x => x.ImgBannerDesktop),
                            "imgbannermobile" => query.OrderByDescending(x => x.ImgBannerMobile),
                            "description" => query.OrderByDescending(x => x.Description),
                            _ => query
                        };
                    }
                    else
                    {
                        query = SectionCategoryBy.ToLower() switch
                        {
                            "refid" => query.OrderBy(x => x.RefID),
                            "sectiontitle" => query.OrderBy(x => x.SectionTitle),
                            "sectiontitleen" => query.OrderBy(x => x.SectionTitleEN),
                            "seq" => query.OrderBy(x => x.Seq),
                            "status" => query.OrderBy(x => x.Status),
                            "imgbannerdesktop" => query.OrderBy(x => x.ImgBannerDesktop),
                            "imgbannermobile" => query.OrderBy(x => x.ImgBannerMobile),
                            "description" => query.OrderBy(x => x.Description),
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
                    return await GetAllAsync(limit, page, total, search, sort, filter, date,userid);
                }

                foreach (var item in data)
                {
                    if(item.SectionTitleEN == "New Collection")
                    {
                        var productDetails = (from product_detail in _context.ProductDetails2
                                              where product_detail.IsDeleted == false
                                              let qtyCart = (from c in _context.Carts
                                                             join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
                                                             from carts_detail in carts.DefaultIfEmpty()
                                                             where c.CustomersID == userid
                                                                 && carts_detail.ObjectID == product_detail.RefID
                                                             && carts_detail.Type == (carts_detail.Type == "closures" ? "closure" : product_detail.Type)
                                                             && carts_detail.IsDeleted == false
                                                             select carts_detail.Qty
                                                              ).Sum()
                                              select new Product
                                              {
                                                  ID = (long)product_detail.OriginID,
                                                  RefID = product_detail.RefID,
                                                  Name = product_detail.Name,
                                                  RealImage = product_detail.RealImage,
                                                  Image = product_detail.Image,
                                                  Weight = product_detail.Weight,
                                                  Price = product_detail.Price,
                                                  CategoriesID = product_detail.CategoriesID,
                                                  CategoryName = product_detail.CategoryName,
                                                  Stock = product_detail.Stock,
                                                  NewProd = product_detail.NewProd,
                                                  FavProd = product_detail.FavProd,
                                                  ClosuresID = product_detail.ClosuresID,
                                                  Volume = product_detail.Volume,
                                                  Height = product_detail.Height,
                                                  Length = product_detail.Length,
                                                  ColorsID = product_detail.ColorsID,
                                                  ShapesID = product_detail.ShapesID,
                                                  NecksID = product_detail.NecksID,
                                                  Width = product_detail.Width,
                                                  StockIndicator = product_detail.StockIndicator,
                                                  PlasticType = product_detail.PlasticType,
                                                  RimsID = product_detail.RimsID,
                                                  QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
                                                  Type = product_detail.Type,
                                                  Neck = _context.Necks.Where(x => x.RefID == product_detail.NecksID).Select(x => x.Code).FirstOrDefault(),
                                                  Rim = _context.Rims.Where(x => x.RefID == product_detail.RimsID).Select(x => x.Name).FirstOrDefault(),
                                                  Color = _context.Necks.Where(x => x.RefID == product_detail.NecksID).Select(x => x.Code).FirstOrDefault(),
                                              }).ToList();
                        item.Product = productDetails
                                        .Where(pd => item.CategoryList.Any(sc => pd.Name.Contains(sc.Name)))
                                        .ToList();
                    }
                }

                    return new ListResponse<SectionCategory>(data, total, page);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public async Task<SectionCategory> GetByIdAsync(long id)
        {
            try
            {
                return await _context.SectionCategorys.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }

        public Task<SectionCategory> ChangePassword(ChangePassword obj, long id) { return null; }
    }
}

