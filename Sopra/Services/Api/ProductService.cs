using Microsoft.EntityFrameworkCore;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

using Sopra.Entities;
using Sopra.Helpers;
using Sopra.Responses;
using System.Collections.Generic;
using System.Data;
using static System.Net.Mime.MediaTypeNames;
using Google.Apis.Storage.v1.Data;
using RestSharp;
using Newtonsoft.Json.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Net.Http;
using System.Net.Sockets;

namespace Sopra.Services
{
	public class ProductService : IServiceProdAsync<Product>
	{
		private readonly EFContext _context;
		private readonly SearchOptimizationService _svc;

		public ProductService(EFContext context, SearchOptimizationService svc)
		{
			_context = context;
			_svc = svc;
		}

		public async Task<ListResponse<Product>> GetAllAsync(int limit, int page, int total, string search, string sort, string order, string filter, string date, string type, int? userid, string CategoriesID, int categoriesId, int productId)
		{
			IQueryable<Product> query = null;

			try
			{
				_context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

				// product home page
				if (type.ToLower() == "launched")
				{
					var imageQueryCalculator = from calculator in _context.Calculators
											   join image in _context.Images on calculator.RefID equals image.ObjectID into imagesGroup
											   where calculator.IsDeleted == false
													 && calculator.Status == 3
													 && calculator.ColorsID == 14
													 && calculator.NewProd == 1
													 && imagesGroup.Any(img => img.Type == "Calculators")
											   select new
											   {
												   CalculatorId = calculator.ID,
												   RealImages = imagesGroup
																 .Where(img => img.Type == "Calculators")
																 .Select(img => img.ProductImage)
																 .ToList()
											   };

					var imagesCalculator = imageQueryCalculator.ToDictionary(x => x.CalculatorId, x => x.RealImages.FirstOrDefault());

					var getCalculator = from calculator in _context.Calculators
										join color in _context.Colors on calculator.ColorsID equals color.RefID
										join neck in _context.Necks on calculator.NecksID equals neck.RefID
										join material in _context.Materials on calculator.MaterialsID equals material.RefID
										let stockindicator = (from a in _context.ProductDetails2 where calculator.RefID == a.RefID select a).FirstOrDefault()
										let categorys_detail = _context.Categorys.Where(x => x.RefID == calculator.CategoriesID && x.Type == "Bottles").FirstOrDefault()
										let countcolor = (from a in _context.ProductDetails2
															  //join b in _context.Colors on a.ColorsID equals b.RefID
														  where a.Name.Contains(calculator.Name + ",")
														  select a.Name).Distinct().ToList()
										let wishlist = _context.WishLists
											.Where(si => calculator.ID == si.ProductId && si.UserId == userid && si.Type == "bottle")
											.FirstOrDefault()
										//let qtyCart = (from c in _context.Carts
										//                                             join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
										//                                             from carts_detail in carts.DefaultIfEmpty()
										//			   where c.CustomersID == userid
										//                                             && carts_detail.ObjectID == calculator.RefID
										//			   && carts_detail.Type == "bottle"
										//                                             && carts_detail.IsDeleted == false
										//                                             select carts_detail.Qty
										//                                             ).Sum()
										where calculator.IsDeleted == false
											  && calculator.Status == 3
											  && calculator.ColorsID == 14
											  && calculator.NewProd == 1
										select new Product
										{
											ID = calculator.ID,
											RefID = calculator.RefID,
											Name = calculator.Name,
											NewProdDate = calculator.NewProdDate,
											Image = calculator.Image,
											Weight = calculator.Weight,
											Price = calculator.Price,
											QtyPack = calculator.QtyPack,
											Volume = calculator.Volume,
											Length = calculator.Length,
											Width = calculator.Width,
											Height = calculator.Height,
											Code = calculator.WmsCode,
											CategoriesID = calculator.CategoriesID,
											CategoryName = categorys_detail.Name,
											Stock = calculator.Stock,
											NewProd = calculator.NewProd,
											FavProd = calculator.FavProd,
											ClosuresID = calculator.ClosuresID,
											LidsID = 0,
											StockIndicator = stockindicator.StockIndicator,
											Neck = neck.Code,
											Rim = null,

											Color = color.Name,

											PlasticType = material.PlasticType,
											MaterialsID = calculator.MaterialsID,

											CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),

											Wishlist = wishlist != null ? 1 : 0,
											//QtyCart = (qtyCart == null || qtyCart==0) ? null : Convert.ToInt64(qtyCart),
											Type = "bottle"
										};

					var imageQueryThermo = from thermo in _context.Thermos
										   join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
										   where thermo.IsDeleted == false
													 && thermo.Status == 3
													 && thermo.NewProd == 1
													 && imagesGroup.Any(img => img.Type == "Thermos")
										   select new
										   {
											   ThermoId = thermo.ID,
											   RealImages = imagesGroup
															 .Where(img => img.Type == "Thermos")
															 .Select(img => img.ProductImage)
															 .ToList()
										   };

					var imagesThermo = imageQueryThermo.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

					var getThermo = from thermo in _context.Thermos
									join color in _context.Colors on thermo.ColorsID equals color.RefID
									join rim in _context.Rims on thermo.RimsID equals rim.RefID
									join material in _context.Materials on thermo.MaterialsID equals material.RefID
									let categorys_detail = _context.Categorys.Where(x => x.RefID == thermo.CategoriesID && x.Type == "Thermos").FirstOrDefault()
									let stockindicator = _context.StockIndicators
									   .Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
									   .FirstOrDefault()
									let wishlist = _context.WishLists
											.Where(si => thermo.ID == si.ProductId && si.UserId == userid && si.Type == "thermo")
											.FirstOrDefault()
									let countcolor = (from a in _context.ProductDetails2
														  //join b in _context.Colors on a.ColorsID equals b.RefID
													  where a.Name.Contains(thermo.Name + ",")
													  select a.Name).Distinct().ToList()
									//                     let qtyCart = (from c in _context.Carts
									//                                    join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
									//                                    from carts_detail in carts.DefaultIfEmpty()
									//                                    where c.CustomersID == userid
									//&& carts_detail.ObjectID == thermo.RefID
									//&& carts_detail.Type == (thermo.CategoriesID == 5 ? "cup" : "tray")
									//                                    && carts_detail.IsDeleted == false
									//                                    select carts_detail.Qty
									//).Sum()
									where thermo.IsDeleted == false
									  && thermo.Status == 3
									  && thermo.NewProd == 1
									select new Product
									{
										ID = thermo.ID,
										RefID = thermo.RefID,
										Name = thermo.Name,
										NewProdDate = thermo.NewProdDate,
										Image = thermo.Image,
										Weight = thermo.Weight,
										Price = thermo.Price,
										QtyPack = thermo.Qty,
										Volume = thermo.Volume,
										Length = thermo.Length,
										Width = thermo.Width,
										Height = thermo.Height,
										Code = thermo.WmsCode,
										CategoriesID = thermo.CategoriesID,
										CategoryName = categorys_detail.Name,
										Stock = thermo.Stock,
										NewProd = thermo.NewProd,
										FavProd = thermo.FavProd,
										LidsID = thermo.LidsID,
										ClosuresID = 0,
										StockIndicator = thermo.Stock <= 0 ? "Pre Order" : stockindicator.Name,

										Neck = null,
										Rim = rim.Name,

										Color = color.Name,

										PlasticType = material.PlasticType,
										MaterialsID = thermo.MaterialsID,

										CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),

										Wishlist = wishlist != null ? 1 : 0,
										//QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
										Type = thermo.CategoriesID == 5 ? "cup" : "tray"
									};

					var combinedQuery = getThermo
										.Union(getCalculator);

					var finalQuery = from product in combinedQuery
									 select new Product
									 {
										 ID = product.ID,
										 RefID = product.RefID,
										 Name = product.Name,
										 NewProdDate = product.NewProdDate,
										 Image = product.Image,
										 Weight = product.Weight,
										 Price = product.Price,
										 QtyPack = product.QtyPack,
										 Volume = product.Volume,
										 Length = product.Length,
										 Width = product.Width,
										 Height = product.Height,
										 Neck = product.Neck,
										 Color = product.Color,
										 Rim = product.Rim,
										 Code = product.Code,
										 CategoriesID = product.CategoriesID,
										 CategoryName = product.CategoryName,
										 Stock = product.Stock,
										 NewProd = product.NewProd,
										 FavProd = product.FavProd,
										 ClosuresID = product.ClosuresID,
										 LidsID = product.LidsID,
										 RealImage = product.Type == "bottle" ?
											 imagesCalculator.ContainsKey(product.ID) ? imagesCalculator[product.ID] : null :
											 imagesThermo.ContainsKey(product.ID) ? imagesThermo[product.ID] : null,
										 StockIndicator = product.StockIndicator,
										 PlasticType = product.PlasticType,
										 MaterialsID = product.MaterialsID,
										 CountColor = product.CountColor,
										 Wishlist = product.Wishlist,
										 //QtyCart = product.QtyCart,
										 Type = product.Type
									 };

					query = finalQuery;

				}
				else if (type.ToLower() == "recommended")
				{
					/// Retrieve Data from Products ID
					var prodDetail = (from ProductDetail2 in _context.ProductDetails2
									  where ProductDetail2.RefID == productId
									  select new
									  {
										  ProductDetail2.Type,
										  ProductDetail2.CategoriesID,
										  ProductDetail2.Volume
									  }).FirstOrDefault();

					long categoryID = 0;
					decimal volume = 0;
					string types = "";

					if (prodDetail != null)
					{
						categoryID = Convert.ToInt64(prodDetail.CategoriesID);
						types = (prodDetail.Type.ToString().Equals("tray") || prodDetail.Type.ToString().Equals("cup") ? "thermo" : prodDetail.Type.ToString());
						volume = Convert.ToDecimal(prodDetail.Volume);
					}

					if (types == "bottle")
					{

						var imageQueryCalculator = from calculator in _context.Calculators
												   join image in _context.Images on calculator.RefID equals image.ObjectID into imagesGroup
												   where calculator.IsDeleted == false
														 && calculator.Status == 3
														 && calculator.ColorsID == 14
														 //                                          && 
														 //(
														 //(categoryID != 0 ? calculator.CategoriesID == categoryID : false)
														 //	&& 
														 //(calculator.Volume == volume)
														 //)
														 && imagesGroup.Any(img => img.Type == "Calculators")
												   select new
												   {
													   CalculatorId = calculator.ID,
													   RealImages = imagesGroup
																	 .Where(img => img.Type == "Calculators")
																	 .Select(img => img.ProductImage)
																	 .ToList()
												   };

						var imagesCalculator = imageQueryCalculator.ToDictionary(x => x.CalculatorId, x => x.RealImages.FirstOrDefault());

						var getCalculator = (from calculator in _context.Calculators
											 join color in _context.Colors on calculator.ColorsID equals color.RefID
											 join neck in _context.Necks on calculator.NecksID equals neck.RefID
											 join material in _context.Materials on calculator.MaterialsID equals material.RefID
											 let categorys_detail = _context.Categorys.Where(x => x.RefID == calculator.CategoriesID && x.Type == "Bottles").FirstOrDefault()
											 let stockindicator = _context.StockIndicators
												.Where(si => calculator.Stock >= si.MinQty && calculator.Stock <= si.MaxQty)
												.FirstOrDefault()
											 let countcolor = (from a in _context.ProductDetails2
																   //join b in _context.Colors on a.ColorsID equals b.RefID
															   where a.Name.Contains(calculator.Name + ",")
															   select a.Name).Distinct().ToList()
											 let wishlist = _context.WishLists
												 .Where(si => calculator.ID == si.ProductId && si.UserId == userid && si.Type == "bottle")
												 .FirstOrDefault()
											 where calculator.IsDeleted == false
												  && calculator.Status == 3
												  && calculator.ColorsID == 14
												  && (productId == 0 || calculator.RefID != productId)
												  &&
													(
														(categoryID != 0 ? calculator.CategoriesID == categoryID : true)
														&&
														(volume != 0 ? calculator.Volume == volume : true)
													)
											 select new Product
											 {
												 ID = calculator.ID,
												 RefID = calculator.RefID,
												 Name = calculator.Name,
												 Image = calculator.Image,
												 Weight = calculator.Weight,
												 Price = calculator.Price,
												 QtyPack = calculator.QtyPack,
												 Volume = calculator.Volume,
												 Length = calculator.Length,
												 Width = calculator.Width,
												 Height = calculator.Height,
												 Code = calculator.WmsCode,
												 CategoriesID = calculator.CategoriesID,
												 CategoryName = categorys_detail.Name,
												 Stock = calculator.Stock,
												 NewProd = calculator.NewProd,
												 FavProd = calculator.FavProd,
												 TotalViews = calculator.TotalViews,
												 ClosuresID = calculator.ClosuresID,
												 LidsID = 0,
												 StockIndicator = calculator.Stock <= 0 ? "Pre Order" : stockindicator.Name,

												 Neck = neck.Code,
												 Rim = null,

												 Color = color.Name,

												 PlasticType = material.PlasticType,
												 MaterialsID = calculator.MaterialsID,
												 RealImage = imagesCalculator.ContainsKey(calculator.ID) ? imagesCalculator[calculator.ID] : null,

												 CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),

												 Wishlist = wishlist != null ? 1 : 0,
												 Type = "bottle"
											 }).Take(20);

						if (getCalculator.ToList().Count < 20)
						{
							var getCalculator2 = (from calculator in _context.Calculators
												  join color in _context.Colors on calculator.ColorsID equals color.RefID
												  join neck in _context.Necks on calculator.NecksID equals neck.RefID
												  join material in _context.Materials on calculator.MaterialsID equals material.RefID
												  let categorys_detail = _context.Categorys.Where(x => x.RefID == calculator.CategoriesID && x.Type == "Bottles").FirstOrDefault()
												  let stockindicator = _context.StockIndicators
													 .Where(si => calculator.Stock >= si.MinQty && calculator.Stock <= si.MaxQty)
													 .FirstOrDefault()
												  let countcolor = (from a in _context.ProductDetails2
																		//join b in _context.Colors on a.ColorsID equals b.RefID
																	where a.Name.Contains(calculator.Name + ",")
																	select a.Name).Distinct().ToList()
												  let wishlist = _context.WishLists
													  .Where(si => calculator.ID == si.ProductId && si.UserId == userid && si.Type == "bottle")
													  .FirstOrDefault()
												  where calculator.IsDeleted == false
													   && calculator.Status == 3
													   && calculator.ColorsID == 14
													   && (productId == 0 || calculator.RefID != productId)
													   &&
														 (
															 ((categoryID != 0 ? calculator.CategoriesID == categoryID : true)
															 &&
															 (volume != 0 ? calculator.Volume == volume : true))
															 ||
															 (categoryID != 0 ? calculator.CategoriesID == categoryID : true)
														 )
												  select new Product
												  {
													  ID = calculator.ID,
													  RefID = calculator.RefID,
													  Name = calculator.Name,
													  Image = calculator.Image,
													  Weight = calculator.Weight,
													  Price = calculator.Price,
													  QtyPack = calculator.QtyPack,
													  Volume = calculator.Volume,
													  Length = calculator.Length,
													  Width = calculator.Width,
													  Height = calculator.Height,
													  Code = calculator.WmsCode,
													  CategoriesID = calculator.CategoriesID,
													  CategoryName = categorys_detail.Name,
													  Stock = calculator.Stock,
													  NewProd = calculator.NewProd,
													  FavProd = calculator.FavProd,
													  TotalViews = calculator.TotalViews,
													  ClosuresID = calculator.ClosuresID,
													  LidsID = 0,
													  StockIndicator = calculator.Stock <= 0 ? "Pre Order" : stockindicator.Name,

													  Neck = neck.Code,
													  Rim = null,

													  Color = color.Name,

													  PlasticType = material.PlasticType,
													  MaterialsID = calculator.MaterialsID,
													  RealImage = imagesCalculator.ContainsKey(calculator.ID) ? imagesCalculator[calculator.ID] : null,

													  CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),

													  Wishlist = wishlist != null ? 1 : 0,
													  Type = "bottle"
												  }).Take(20);
							query = getCalculator2;
						}
						else query = getCalculator;
					}

					else if (CategoriesID.ToLower() == "thermo")
					{
						var imageQueryThermo = from thermo in _context.Thermos
											   join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
											   where thermo.IsDeleted == false
														 && thermo.Status == 3
														 && thermo.FavProd == 1
														 && imagesGroup.Any(img => img.Type == "Thermos")
											   select new
											   {
												   ThermoId = thermo.ID,
												   RealImages = imagesGroup
																 .Where(img => img.Type == "Thermos")
																 .Select(img => img.ProductImage)
																 .ToList()
											   };

						var imagesThermo = imageQueryThermo.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

						var getThermo = (from thermo in _context.Thermos
										 join color in _context.Colors on thermo.ColorsID equals color.RefID
										 join rim in _context.Rims on thermo.RimsID equals rim.RefID
										 join material in _context.Materials on thermo.MaterialsID equals material.RefID
										 let categorys_detail = _context.Categorys.Where(x => x.RefID == thermo.CategoriesID && x.Type == "Thermos").FirstOrDefault()
										 let stockindicator = _context.StockIndicators
										   .Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
										   .FirstOrDefault()
										 let wishlist = _context.WishLists
												 .Where(si => thermo.ID == si.ProductId && si.UserId == userid && si.Type == "thermo")
												 .FirstOrDefault()
										 let countcolor = (from a in _context.ProductDetails2
															   //join b in _context.Colors on a.ColorsID equals b.RefID
														   where a.Name.Contains(thermo.Name + ",")
														   select a.Name).Distinct().ToList()
										 //                           let qtyCart = (from c in _context.Carts
										 //                                          join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
										 //                                          from carts_detail in carts.DefaultIfEmpty()
										 //                                          where c.CustomersID == userid
										 //                                   && carts_detail.ObjectID == thermo.RefID
										 //                                   && carts_detail.Type == (thermo.CategoriesID == 5 ? "cup" : "tray")
										 //                                   && carts_detail.IsDeleted == false
										 //                                          select carts_detail.Qty
										 //).Sum()
										 where thermo.IsDeleted == false
										  && thermo.Status == 3
										  && thermo.FavProd == 1
										  && (productId == 0 || thermo.RefID != productId)
													  &&
														(
															(categoryID != 0 ? thermo.CategoriesID == categoryID : true)
															&&
															(volume != 0 ? thermo.Volume == volume : true)
														)
										 orderby Guid.NewGuid()
										 select new Product
										 {
											 ID = thermo.ID,
											 RefID = thermo.RefID,
											 Name = thermo.Name,
											 Image = thermo.Image,
											 Weight = thermo.Weight,
											 Price = thermo.Price,
											 QtyPack = thermo.Qty,
											 Volume = thermo.Volume,
											 Length = thermo.Length,
											 Width = thermo.Width,
											 Height = thermo.Height,
											 Code = thermo.WmsCode,
											 CategoriesID = thermo.CategoriesID,
											 CategoryName = categorys_detail.Name,
											 Stock = thermo.Stock,
											 NewProd = thermo.NewProd,
											 FavProd = thermo.FavProd,
											 LidsID = thermo.LidsID,
											 TotalViews = thermo.TotalViews,
											 ClosuresID = 0,
											 StockIndicator = thermo.Stock <= 0 ? "Pre Order" : stockindicator.Name,

											 Neck = null,
											 Rim = rim.Name,

											 Color = color.Name,

											 PlasticType = material.PlasticType,
											 MaterialsID = thermo.MaterialsID,

											 CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
											 RealImage = imagesThermo.ContainsKey(thermo.ID) ? imagesThermo[thermo.ID] : null,

											 Wishlist = wishlist != null ? 1 : 0,
											 //QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											 Type = thermo.CategoriesID == 5 ? "cup" : "tray"
										 }).Take(20);

						if (getThermo.ToList().Count < 20)
						{
							var getThermo2 = (from thermo in _context.Thermos
											  join color in _context.Colors on thermo.ColorsID equals color.RefID
											  join rim in _context.Rims on thermo.RimsID equals rim.RefID
											  join material in _context.Materials on thermo.MaterialsID equals material.RefID
											  let categorys_detail = _context.Categorys.Where(x => x.RefID == thermo.CategoriesID && x.Type == "Thermos").FirstOrDefault()
											  let stockindicator = _context.StockIndicators
												.Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
												.FirstOrDefault()
											  let wishlist = _context.WishLists
													  .Where(si => thermo.ID == si.ProductId && si.UserId == userid && si.Type == "thermo")
													  .FirstOrDefault()
											  let countcolor = (from a in _context.ProductDetails2
																	//join b in _context.Colors on a.ColorsID equals b.RefID
																where a.Name.Contains(thermo.Name + ",")
																select a.Name).Distinct().ToList()
											  //                           let qtyCart = (from c in _context.Carts
											  //                                          join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
											  //                                          from carts_detail in carts.DefaultIfEmpty()
											  //                                          where c.CustomersID == userid
											  //                                   && carts_detail.ObjectID == thermo.RefID
											  //                                   && carts_detail.Type == (thermo.CategoriesID == 5 ? "cup" : "tray")
											  //                                   && carts_detail.IsDeleted == false
											  //                                          select carts_detail.Qty
											  //).Sum()
											  where thermo.IsDeleted == false
											   && thermo.Status == 3
											   && thermo.FavProd == 1
											   && (productId == 0 || thermo.RefID != productId)
													   &&
														 (
															 ((categoryID != 0 ? thermo.CategoriesID == categoryID : true)
															 &&
															 (volume != 0 ? thermo.Volume == volume : true))
															 ||
															 (categoryID != 0 ? thermo.CategoriesID == categoryID : true)
														 )
											  orderby Guid.NewGuid()
											  select new Product
											  {
												  ID = thermo.ID,
												  RefID = thermo.RefID,
												  Name = thermo.Name,
												  Image = thermo.Image,
												  Weight = thermo.Weight,
												  Price = thermo.Price,
												  QtyPack = thermo.Qty,
												  Volume = thermo.Volume,
												  Length = thermo.Length,
												  Width = thermo.Width,
												  Height = thermo.Height,
												  Code = thermo.WmsCode,
												  CategoriesID = thermo.CategoriesID,
												  CategoryName = categorys_detail.Name,
												  Stock = thermo.Stock,
												  NewProd = thermo.NewProd,
												  FavProd = thermo.FavProd,
												  LidsID = thermo.LidsID,
												  TotalViews = thermo.TotalViews,
												  ClosuresID = 0,
												  StockIndicator = thermo.Stock <= 0 ? "Pre Order" : stockindicator.Name,

												  Neck = null,
												  Rim = rim.Name,

												  Color = color.Name,

												  PlasticType = material.PlasticType,
												  MaterialsID = thermo.MaterialsID,

												  CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
												  RealImage = imagesThermo.ContainsKey(thermo.ID) ? imagesThermo[thermo.ID] : null,

												  Wishlist = wishlist != null ? 1 : 0,
												  //QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
												  Type = thermo.CategoriesID == 5 ? "cup" : "tray"
											  }).Take(20);

							query = getThermo2;
						}
						else query = getThermo;
					}

					else if (CategoriesID.ToLower() == "lid")
					{
						var imageQueryLid = from lid in _context.Lids
											join image in _context.Images on lid.RefID equals image.ObjectID into imagesGroup
											where lid.IsDeleted == false
													  && lid.Status == 3
													  && lid.FavProd == 1
													  && imagesGroup.Any(img => img.Type == "Lids")
											select new
											{
												LidId = lid.ID,
												RealImages = imagesGroup
															  .Where(img => img.Type == "Lids")
															  .Select(img => img.ProductImage)
															  .ToList()
											};

						var imagesLid = imageQueryLid.ToDictionary(x => x.LidId, x => x.RealImages.FirstOrDefault());

						var getLid = (from lid in _context.Lids
									  join color in _context.Colors on lid.ColorsID equals color.RefID
									  join rim in _context.Rims on lid.RimsID equals rim.RefID
									  join material in _context.Materials on lid.MaterialsID equals material.RefID
									  let wishlist = _context.WishLists
											  .Where(si => lid.ID == si.ProductId && si.UserId == userid && si.Type == "lid")
											  .FirstOrDefault()
									  let countcolor = (from a in _context.ProductDetails2
														where a.Name.Contains(lid.Name + ",")
														select a.Name).Distinct().ToList()
									  where lid.IsDeleted == false
										  && lid.Status == 3
										  && lid.FavProd == 1
									  orderby Guid.NewGuid()
									  select new Product
									  {
										  ID = lid.ID,
										  RefID = lid.RefID,
										  Name = lid.Name,
										  Image = lid.Image,
										  Weight = lid.Weight,
										  Price = lid.Price,
										  QtyPack = lid.Qty,
										  Volume = 0,
										  Length = lid.Length,
										  Width = lid.Width,
										  Height = lid.Height,
										  Code = lid.WmsCode,
										  CategoriesID = 0,
										  Stock = 0,
										  NewProd = lid.NewProd,
										  FavProd = lid.FavProd,
										  ClosuresID = 0,
										  TotalViews = 0,

										  Neck = null,
										  Rim = rim.Name,

										  Color = color.Name,

										  PlasticType = material.PlasticType,
										  MaterialsID = lid.MaterialsID,

										  CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),

										  Wishlist = wishlist != null ? 1 : 0,
										  RealImage = imagesLid.ContainsKey(lid.ID) ? imagesLid[lid.ID] : null,

										  Type = "lid"
									  }).Take(20);
						query = getLid;
					}

					else
					{
						var imageQueryCalculator = from calculator in _context.Calculators
												   join image in _context.Images on calculator.RefID equals image.ObjectID into imagesGroup
												   where calculator.IsDeleted == false
														 && calculator.Status == 3
														 && calculator.ColorsID == 14
														 && calculator.FavProd == 1
														 && imagesGroup.Any(img => img.Type == "Calculators")
												   select new
												   {
													   CalculatorId = calculator.ID,
													   RealImages = imagesGroup
																	 .Where(img => img.Type == "Calculators")
																	 .Select(img => img.ProductImage)
																	 .ToList()
												   };

						var imagesCalculator = imageQueryCalculator.ToDictionary(x => x.CalculatorId, x => x.RealImages.FirstOrDefault());

						var getCalculator = (from calculator in _context.Calculators
											 join color in _context.Colors on calculator.ColorsID equals color.RefID
											 join material in _context.Materials on calculator.MaterialsID equals material.RefID
											 let categorys_detail = _context.Categorys.Where(x => x.RefID == calculator.CategoriesID && x.Type == "Bottles").FirstOrDefault()
											 let stockindicator = _context.StockIndicators
												.Where(si => calculator.Stock >= si.MinQty && calculator.Stock <= si.MaxQty)
												.FirstOrDefault()
											 let countcolor = (from a in _context.ProductDetails2
																   //join b in _context.Colors on a.ColorsID equals b.RefID
															   where a.Name.Contains(calculator.Name + ",")
															   select a.Name).Distinct().ToList()
											 //                              let qtyCart = (from c in _context.Carts
											 //                                             join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
											 //                                             from carts_detail in carts.DefaultIfEmpty()
											 //                                             where c.CustomersID == userid
											 //                                                 && carts_detail.ObjectID == calculator.RefID
											 //                                             && carts_detail.Type == "bottle"
											 //                                             && carts_detail.IsDeleted == false
											 //                                             select carts_detail.Qty
											 //).Sum()
											 where calculator.IsDeleted == false
												  && calculator.Status == 3
												  && calculator.ColorsID == 14
												  && calculator.FavProd == 1
												  && (categoriesId != 0 ? calculator.CategoriesID == categoriesId : true)
											 orderby Guid.NewGuid()
											 select new Product
											 {
												 ID = calculator.ID,
												 RefID = calculator.RefID,
												 Name = calculator.Name,
												 Image = calculator.Image,
												 Weight = calculator.Weight,
												 Price = calculator.Price,
												 CategoriesID = calculator.CategoriesID,
												 CategoryName = categorys_detail.Name,
												 Stock = calculator.Stock,
												 NewProd = calculator.NewProd,
												 FavProd = calculator.FavProd,
												 TotalViews = calculator.TotalViews,
												 ClosuresID = calculator.ClosuresID,
												 LidsID = 0,
												 Code = calculator.WmsCode,
												 StockIndicator = stockindicator.Name,
												 CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
												 PlasticType = material.PlasticType,
												 MaterialsID = calculator.MaterialsID,
												 Color = color.Name,
												 //QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
												 Type = "bottle"
											 }).Take(10);

						var imageQueryThermo = from thermo in _context.Thermos
											   join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
											   where thermo.IsDeleted == false
														 && thermo.Status == 3
														 && thermo.FavProd == 1
														 && imagesGroup.Any(img => img.Type == "Thermos")
											   select new
											   {
												   ThermoId = thermo.ID,
												   RealImages = imagesGroup
																 .Where(img => img.Type == "Thermos")
																 .Select(img => img.ProductImage)
																 .ToList()
											   };

						var imagesThermo = imageQueryThermo.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

						var getThermo = (from thermo in _context.Thermos
										 join color in _context.Colors on thermo.ColorsID equals color.RefID
										 join material in _context.Materials on thermo.MaterialsID equals material.RefID
										 let categorys_detail = _context.Categorys.Where(x => x.RefID == thermo.CategoriesID && x.Type == "Thermos").FirstOrDefault()
										 let stockindicator = _context.StockIndicators
										   .Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
										   .FirstOrDefault()
										 let countcolor = (from a in _context.ProductDetails2
															   //join b in _context.Colors on a.ColorsID equals b.RefID
														   where a.Name.Contains(thermo.Name + ",")
														   select a.Name).Distinct().ToList()
										 //                            let qtyCart = (from c in _context.Carts
										 //                                           join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
										 //                                           from carts_detail in carts.DefaultIfEmpty()
										 //                                           where c.CustomersID == userid
										 //                                    && carts_detail.ObjectID == thermo.RefID
										 //                                    && carts_detail.Type == (thermo.CategoriesID == 5 ? "cup" : "tray")
										 //                                    && carts_detail.IsDeleted == false
										 //                                           select carts_detail.Qty
										 //).Sum()
										 where thermo.IsDeleted == false
										  && thermo.Status == 3
										  && thermo.FavProd == 1
										  && (categoriesId != 0 ? thermo.CategoriesID == categoriesId : true)
										 orderby Guid.NewGuid()
										 select new Product
										 {
											 ID = thermo.ID,
											 RefID = thermo.RefID,
											 Name = thermo.Name,
											 Image = thermo.Image,
											 Weight = thermo.Weight,
											 Price = thermo.Price,
											 CategoriesID = thermo.CategoriesID,
											 CategoryName = categorys_detail.Name,
											 Stock = thermo.Stock,
											 NewProd = thermo.NewProd,
											 FavProd = thermo.FavProd,
											 LidsID = thermo.LidsID,
											 ClosuresID = 0,
											 TotalViews = thermo.TotalViews,
											 Code = thermo.WmsCode,
											 StockIndicator = stockindicator.Name,
											 CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
											 PlasticType = material.PlasticType,
											 MaterialsID = thermo.MaterialsID,
											 Color = color.Name,
											 //QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											 Type = thermo.CategoriesID == 5 ? "cup" : "tray"
										 }).Take(10);
						var combinedQuery = getCalculator
											.Union(getThermo);

						var finalQuery = from product in combinedQuery
										 orderby Guid.NewGuid()
										 select new Product
										 {
											 ID = product.ID,
											 RefID = product.RefID,
											 Name = product.Name,
											 Image = product.Image,
											 Weight = product.Weight,
											 Price = product.Price,
											 CategoriesID = product.CategoriesID,
											 CategoryName = product.CategoryName,
											 Stock = product.Stock,
											 NewProd = product.NewProd,
											 FavProd = product.FavProd,
											 ClosuresID = product.ClosuresID,
											 LidsID = product.LidsID,
											 Code = product.Code,
											 TotalViews = product.TotalViews,
											 RealImage = product.Type == "bottle" ?
												 imagesCalculator.ContainsKey(product.ID) ? imagesCalculator[product.ID] : null :
												 imagesThermo.ContainsKey(product.ID) ? imagesThermo[product.ID] : null,
											 StockIndicator = product.StockIndicator,
											 PlasticType = product.PlasticType,
											 MaterialsID = product.MaterialsID,
											 CountColor = product.CountColor,
											 Color = product.Color,
											 //QtyCart = product.QtyCart,
											 Type = product.Type
										 };
						query = finalQuery;
					}
				}
				else if (type.ToLower() == "favourite")
				{
					var imageQueryCalculator = from calculator in _context.Calculators
											   join image in _context.Images on calculator.RefID equals image.ObjectID into imagesGroup
											   where calculator.IsDeleted == false
													 && calculator.Status == 3
													 && calculator.ColorsID == 14
													 && calculator.FavProd == 1
													 && imagesGroup.Any(img => img.Type == "Calculators")
											   select new
											   {
												   CalculatorId = calculator.ID,
												   RealImages = imagesGroup
																 .Where(img => img.Type == "Calculators")
																 .Select(img => img.ProductImage)
																 .ToList()
											   };

					var imagesCalculator = imageQueryCalculator.ToDictionary(x => x.CalculatorId, x => x.RealImages.FirstOrDefault());

					var getCalculator = from calculator in _context.Calculators
										join color in _context.Colors on calculator.ColorsID equals color.RefID
										join neck in _context.Necks on calculator.NecksID equals neck.RefID
										join material in _context.Materials on calculator.MaterialsID equals material.RefID
										let categorys_detail = _context.Categorys.Where(x => x.RefID == calculator.CategoriesID && x.Type == "Bottles").FirstOrDefault()
										let stockindicator = _context.StockIndicators
											.Where(si => calculator.Stock >= si.MinQty && calculator.Stock <= si.MaxQty)
											.FirstOrDefault()
										let countcolor = (from a in _context.ProductDetails2
															  //join b in _context.Colors on a.ColorsID equals b.RefID
														  where a.Name.Contains(calculator.Name + ",")
														  select a.Name).Distinct().ToList()
										let wishlist = _context.WishLists
											.Where(si => calculator.ID == si.ProductId && si.UserId == userid && si.Type == "bottle")
											.FirstOrDefault()
										//                          let qtyCart = (from c in _context.Carts
										//                                         join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
										//                                         from carts_detail in carts.DefaultIfEmpty()
										//                                         where c.CustomersID == userid
										//                                             && carts_detail.ObjectID == calculator.RefID
										//                                         && carts_detail.Type == "bottle"
										//                                         && carts_detail.IsDeleted == false
										//                                         select carts_detail.Qty
										//).Sum()
										where calculator.IsDeleted == false
											  && calculator.Status == 3
											  && calculator.ColorsID == 14
											  && calculator.FavProd == 1
										orderby Guid.NewGuid()
										select new Product
										{
											ID = calculator.ID,
											RefID = calculator.RefID,
											Name = calculator.Name,
											Image = calculator.Image,
											Weight = calculator.Weight,
											Price = calculator.Price,
											QtyPack = calculator.QtyPack,
											Volume = calculator.Volume,
											Length = calculator.Length,
											Width = calculator.Width,
											Height = calculator.Height,
											Code = calculator.WmsCode,
											CategoriesID = calculator.CategoriesID,
											CategoryName = categorys_detail.Name,
											Stock = calculator.Stock,
											NewProd = calculator.NewProd,
											FavProd = calculator.FavProd,
											TotalViews = calculator.TotalViews,
											ClosuresID = calculator.ClosuresID,
											LidsID = 0,
											StockIndicator = calculator.Stock <= 0 ? "Pre Order" : stockindicator.Name,

											Neck = neck.Code,
											Rim = null,

											Color = color.Name,

											PlasticType = material.PlasticType,
											MaterialsID = calculator.MaterialsID,

											CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),

											Wishlist = wishlist != null ? 1 : 0,
											//QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											Type = "bottle"
										};

					var imageQueryThermo = from thermo in _context.Thermos
										   join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
										   where thermo.IsDeleted == false
													 && thermo.Status == 3
													 && thermo.FavProd == 1
													 && imagesGroup.Any(img => img.Type == "Thermos")
										   select new
										   {
											   ThermoId = thermo.ID,
											   RealImages = imagesGroup
															 .Where(img => img.Type == "Thermos")
															 .Select(img => img.ProductImage)
															 .ToList()
										   };

					var imagesThermo = imageQueryThermo.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

					var getThermo = from thermo in _context.Thermos
									join color in _context.Colors on thermo.ColorsID equals color.RefID
									join rim in _context.Rims on thermo.RimsID equals rim.RefID
									join material in _context.Materials on thermo.MaterialsID equals material.RefID
									let categorys_detail = _context.Categorys.Where(x => x.RefID == thermo.CategoriesID && x.Type == "Thermos").FirstOrDefault()
									let stockindicator = _context.StockIndicators
									   .Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
									   .FirstOrDefault()
									let wishlist = _context.WishLists
											.Where(si => thermo.ID == si.ProductId && si.UserId == userid && si.Type == "thermo")
											.FirstOrDefault()
									let countcolor = (from a in _context.ProductDetails2
														  //join b in _context.Colors on a.ColorsID equals b.RefID
													  where a.Name.Contains(thermo.Name + ",")
													  select a.Name).Distinct().ToList()
									//                       let qtyCart = (from c in _context.Carts
									//                                      join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
									//                                      from carts_detail in carts.DefaultIfEmpty()
									//                                      where c.CustomersID == userid
									//                               && carts_detail.ObjectID == thermo.RefID
									//                               && carts_detail.Type == (thermo.CategoriesID == 5 ? "cup" : "tray")
									//                               && carts_detail.IsDeleted == false
									//                                      select carts_detail.Qty
									//).Sum()
									where thermo.IsDeleted == false
									  && thermo.Status == 3
									  && thermo.FavProd == 1
									orderby Guid.NewGuid()
									select new Product
									{
										ID = thermo.ID,
										RefID = thermo.RefID,
										Name = thermo.Name,
										Image = thermo.Image,
										Weight = thermo.Weight,
										Price = thermo.Price,
										QtyPack = thermo.Qty,
										Volume = thermo.Volume,
										Length = thermo.Length,
										Width = thermo.Width,
										Height = thermo.Height,
										Code = thermo.WmsCode,
										CategoriesID = thermo.CategoriesID,
										CategoryName = categorys_detail.Name,
										Stock = thermo.Stock,
										NewProd = thermo.NewProd,
										FavProd = thermo.FavProd,
										LidsID = thermo.LidsID,
										TotalViews = thermo.TotalViews,
										ClosuresID = 0,
										StockIndicator = thermo.Stock <= 0 ? "Pre Order" : stockindicator.Name,

										Neck = null,
										Rim = rim.Name,

										Color = color.Name,

										PlasticType = material.PlasticType,
										MaterialsID = thermo.MaterialsID,

										CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),

										Wishlist = wishlist != null ? 1 : 0,
										//QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
										Type = thermo.CategoriesID == 5 ? "cup" : "tray"
									};
					var combinedQuery = getCalculator
										.Union(getThermo);

					var finalQuery = from product in combinedQuery
									 orderby Guid.NewGuid()
									 select new Product
									 {
										 ID = product.ID,
										 RefID = product.RefID,
										 Name = product.Name,
										 Image = product.Image,
										 Weight = product.Weight,
										 Price = product.Price,
										 QtyPack = product.QtyPack,
										 Volume = product.Volume,
										 Length = product.Length,
										 Width = product.Width,
										 Height = product.Height,
										 Neck = product.Neck,
										 Color = product.Color,
										 Rim = product.Rim,
										 Code = product.Code,
										 CategoriesID = product.CategoriesID,
										 CategoryName = product.CategoryName,
										 Stock = product.Stock,
										 NewProd = product.NewProd,
										 FavProd = product.FavProd,
										 ClosuresID = product.ClosuresID,
										 LidsID = product.LidsID,
										 TotalViews = product.TotalViews,
										 RealImage = product.Type == "bottle" ?
											 imagesCalculator.ContainsKey(product.ID) ? imagesCalculator[product.ID] : null :
											 imagesThermo.ContainsKey(product.ID) ? imagesThermo[product.ID] : null,
										 StockIndicator = product.StockIndicator,
										 PlasticType = product.PlasticType,
										 MaterialsID = product.MaterialsID,
										 CountColor = product.CountColor,
										 Wishlist = product.Wishlist,
										 //QtyCart = product.QtyCart,
										 Type = product.Type
									 };

					query = finalQuery;

				}
				else if (type.ToLower() == "bestseller")
				{

					var imageQueryCalculator = from calculator in _context.Calculators
											   join image in _context.Images on calculator.RefID equals image.ObjectID into imagesGroup
											   where calculator.IsDeleted == false
													 && calculator.Status == 3
													 && calculator.ColorsID == 14
													 && imagesGroup.Any(img => img.Type == "Calculators")
											   select new
											   {
												   CalculatorId = calculator.ID,
												   RealImages = imagesGroup
																 .Where(img => img.Type == "Calculators")
																 .Select(img => img.ProductImage)
																 .ToList()
											   };

					var imagesCalculator = imageQueryCalculator.ToDictionary(x => x.CalculatorId, x => x.RealImages.FirstOrDefault());

					DateTime threeMonthsAgo = Utility.getCurrentTimestamps().AddMonths(-3);

					var getBestSeller = from calculator in _context.Calculators
										join color in _context.Colors on calculator.ColorsID equals color.RefID
										join neck in _context.Necks on calculator.NecksID equals neck.RefID
										join bestseller in _context.BestSellers on calculator.RefID equals bestseller.ObjectID
										join material in _context.Materials on calculator.MaterialsID equals material.RefID
										let categorys_detail = _context.Categorys.Where(x => x.RefID == calculator.CategoriesID && x.Type == "Bottles").FirstOrDefault()
										let stockindicator = (from a in _context.ProductDetails2 where calculator.RefID == a.RefID select a).FirstOrDefault()
										//                          let qtyCart = (from c in _context.Carts
										//                                         join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
										//                                         from carts_detail in carts.DefaultIfEmpty()
										//                                         where c.CustomersID == userid
										//                                             && carts_detail.ObjectID == calculator.RefID
										//                                         && carts_detail.Type == "bottle"
										//                                         && carts_detail.IsDeleted == false
										//                                         select carts_detail.Qty
										//).Sum()
										//where transactionorderdetail.OrderStatus == "active"
										where calculator.ColorsID == 14
										&& calculator.IsDeleted == false
										&& calculator.Status == 3
										//&& transactionorderdetail.OrderDate >= threeMonthsAgo
										//&& productName.Contains(calculator.Name)
										//group new { calculator, transactionorderdetail, material, stockindicator, color, neck }
										//by new { calculator.ID, calculator.RefID, calculator.Name, calculator.Image, calculator.Weight, calculator.Price, calculator.QtyPack, calculator.Volume, calculator.Length, calculator.Width, calculator.Height, calculator.WmsCode, calculator.NewProd, calculator.FavProd, calculator.ClosuresID, material.PlasticType, calculator.MaterialsID } into grouped
										select new Product
										{
											ID = calculator.ID,
											RefID = calculator.RefID,
											Name = calculator.Name,
											Image = calculator.Image,
											Weight = calculator.Weight,
											Price = calculator.Price,
											QtyPack = calculator.QtyPack,
											Volume = calculator.Volume,
											Length = calculator.Length,
											Width = calculator.Width,
											Height = calculator.Height,
											Code = calculator.WmsCode,
											CategoriesID = calculator.CategoriesID,
											CategoryName = categorys_detail.Name,
											NewProd = calculator.NewProd,
											FavProd = calculator.FavProd,
											ClosuresID = calculator.ClosuresID,
											LidsID = 0,
											StockIndicator = stockindicator.StockIndicator,

											Neck = neck.Code,

											Color = color.Name,

											Wishlist = _context.WishLists.Any(w => w.ProductId == calculator.ID && w.UserId == userid && w.Type == "bottle") ? 1 : 0,

											PlasticType = material.PlasticType,
											MaterialsID = calculator.MaterialsID,

											CountColor = _context.ProductDetails2
														.Where(pd => pd.Name.Contains(calculator.Name + ","))
														.Select(pd => pd.Name)
														.Distinct()
														.Count(),

											RealImage = imagesCalculator.ContainsKey(calculator.ID) ? imagesCalculator[calculator.ID] : null,

											//Orderdate = grouped.Select(g => g.transactionorderdetail.OrderDate).FirstOrDefault(),
											TotalQty = bestseller.Qty,
											//QtyCart = grouped.Select(g => g.qtyCart).FirstOrDefault() == null ? null : Convert.ToInt64(grouped.Select(g => g.qtyCart).FirstOrDefault()),

											Type = "bottle"

										};

					query = getBestSeller;
				}
				else if (type.ToLower() == "random")
				{
					var imageQueryCalculator = from calculator in _context.Calculators
											   join image in _context.Images on calculator.RefID equals image.ObjectID into imagesGroup
											   where calculator.IsDeleted == false
													 && calculator.Status == 3
													 && calculator.ColorsID == 14
													 && imagesGroup.Any(img => img.Type == "Calculators")
											   select new
											   {
												   CalculatorId = calculator.ID,
												   RealImages = imagesGroup
																 .Where(img => img.Type == "Calculators")
																 .Select(img => img.ProductImage)
																 .ToList()
											   };

					var imagesCalculator = imageQueryCalculator.ToDictionary(x => x.CalculatorId, x => x.RealImages.FirstOrDefault());

					var getCalculator = from calculator in _context.Calculators
										join color in _context.Colors on calculator.ColorsID equals color.RefID
										join neck in _context.Necks on calculator.NecksID equals neck.RefID
										join material in _context.Materials on calculator.MaterialsID equals material.RefID
										let categorys_detail = _context.Categorys.Where(x => x.RefID == calculator.CategoriesID && x.Type == "Bottles").FirstOrDefault()
										let stockindicator = (from a in _context.ProductDetails2 where calculator.RefID == a.RefID select a).FirstOrDefault()
										let countcolor = (from a in _context.ProductDetails2
															  //join b in _context.Colors on a.ColorsID equals b.RefID
														  where a.Name.Contains(calculator.Name + ",")
														  select a.Name).Distinct().ToList()
										let wishlist = _context.WishLists
											.Where(si => calculator.ID == si.ProductId && si.UserId == userid && si.Type == "bottle")
											.FirstOrDefault()
										//                          let qtyCart = (from c in _context.Carts
										//                                         join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
										//                                         from carts_detail in carts.DefaultIfEmpty()
										//                                         where c.CustomersID == userid
										//                                             && carts_detail.ObjectID == calculator.RefID
										//                                         && carts_detail.Type == "bottle"
										//                                         && carts_detail.IsDeleted == false
										//                                         select carts_detail.Qty
										//).Sum()
										where calculator.IsDeleted == false
											  && calculator.Status == 3
											  && calculator.ColorsID == 14
										orderby Guid.NewGuid()
										select new Product
										{
											ID = calculator.ID,
											RefID = calculator.RefID,
											Name = calculator.Name,
											Image = calculator.Image,
											Weight = calculator.Weight,
											Price = calculator.Price,
											QtyPack = calculator.QtyPack,
											Volume = calculator.Volume,
											Length = calculator.Length,
											Width = calculator.Width,
											Height = calculator.Height,
											Code = calculator.WmsCode,
											CategoriesID = calculator.CategoriesID,
											CategoryName = categorys_detail.Name,
											Stock = calculator.Stock,
											NewProd = calculator.NewProd,
											FavProd = calculator.FavProd,
											TotalViews = calculator.TotalViews,
											ClosuresID = calculator.ClosuresID,
											LidsID = 0,
											StockIndicator = stockindicator.StockIndicator,

											PlasticType = material.PlasticType,
											MaterialsID = calculator.MaterialsID,
											Neck = neck.Code,
											Rim = null,

											Color = color.Name,

											CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),

											Wishlist = wishlist != null ? 1 : 0,
											//QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											Type = "bottle"
										};

					var imageQueryThermo = from thermo in _context.Thermos
										   join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
										   where thermo.IsDeleted == false
													 && thermo.Status == 3
													 && imagesGroup.Any(img => img.Type == "Thermos")
										   select new
										   {
											   ThermoId = thermo.ID,
											   RealImages = imagesGroup
															 .Where(img => img.Type == "Thermos")
															 .Select(img => img.ProductImage)
															 .ToList()
										   };

					var imagesThermo = imageQueryThermo.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

					var getThermo = from thermo in _context.Thermos
									join color in _context.Colors on thermo.ColorsID equals color.RefID
									join rim in _context.Rims on thermo.RimsID equals rim.RefID
									join material in _context.Materials on thermo.MaterialsID equals material.RefID
									let categorys_detail = _context.Categorys.Where(x => x.RefID == thermo.CategoriesID && x.Type == "Thermos").FirstOrDefault()
									let stockindicator = _context.StockIndicators
									   .Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
									   .FirstOrDefault()
									let wishlist = _context.WishLists
											.Where(si => thermo.ID == si.ProductId && si.UserId == userid && si.Type == "thermo")
											.FirstOrDefault()
									let countcolor = (from a in _context.ProductDetails2
														  //join b in _context.Colors on a.ColorsID equals b.RefID
													  where a.Name.Contains(thermo.Name + ",")
													  select a.Name).Distinct().ToList()
									//                       let qtyCart = (from c in _context.Carts
									//                                      join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
									//                                      from carts_detail in carts.DefaultIfEmpty()
									//                                      where c.CustomersID == userid
									//                               && carts_detail.ObjectID == thermo.RefID
									//                               && carts_detail.Type == (thermo.CategoriesID == 5 ? "cup" : "tray")
									//                               && carts_detail.IsDeleted == false
									//                                      select carts_detail.Qty
									//).Sum()
									where thermo.IsDeleted == false
									  && thermo.Status == 3
									orderby Guid.NewGuid()
									select new Product
									{
										ID = thermo.ID,
										RefID = thermo.RefID,
										Name = thermo.Name,
										Image = thermo.Image,
										Weight = thermo.Weight,
										Price = thermo.Price,
										QtyPack = thermo.Qty,
										Volume = thermo.Volume,
										Length = thermo.Length,
										Width = thermo.Width,
										Height = thermo.Height,
										Code = thermo.WmsCode,
										CategoriesID = thermo.CategoriesID,
										CategoryName = categorys_detail.Name,
										Stock = thermo.Stock,
										NewProd = thermo.NewProd,
										FavProd = thermo.FavProd,
										LidsID = thermo.LidsID,
										TotalViews = thermo.TotalViews,
										ClosuresID = 0,
										StockIndicator = thermo.Stock <= 0 ? "Pre Order" : stockindicator.Name,

										Neck = null,
										Rim = rim.Name,

										Color = color.Name,

										PlasticType = material.PlasticType,
										MaterialsID = thermo.MaterialsID,
										CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),

										Wishlist = wishlist != null ? 1 : 0,
										//QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
										Type = thermo.CategoriesID == 5 ? "cup" : "tray"
									};
					var combinedQuery = getCalculator
										.Union(getThermo);

					var finalQuery = (from product in combinedQuery
									  orderby Guid.NewGuid()
									  select new Product
									  {
										  ID = product.ID,
										  RefID = product.RefID,
										  Name = product.Name,
										  Image = product.Image,
										  Weight = product.Weight,
										  Price = product.Price,
										  QtyPack = product.QtyPack,
										  Volume = product.Volume,
										  Length = product.Length,
										  Width = product.Width,
										  Height = product.Height,
										  Neck = product.Neck,
										  Color = product.Color,
										  Rim = product.Rim,
										  Code = product.Code,
										  CategoriesID = product.CategoriesID,
										  CategoryName = product.CategoryName,
										  Stock = product.Stock,
										  NewProd = product.NewProd,
										  FavProd = product.FavProd,
										  ClosuresID = product.ClosuresID,
										  LidsID = product.LidsID,
										  TotalViews = product.TotalViews,
										  RealImage = product.Type == "bottle" ?
											  imagesCalculator.ContainsKey(product.ID) ? imagesCalculator[product.ID] : null :
											  imagesThermo.ContainsKey(product.ID) ? imagesThermo[product.ID] : null,
										  StockIndicator = product.StockIndicator,
										  PlasticType = product.PlasticType,
										  MaterialsID = product.MaterialsID,
										  CountColor = product.CountColor,
										  Wishlist = product.Wishlist,
										  //QtyCart = product.QtyCart,
										  Type = product.Type
									  });

					query = finalQuery;
				}
				else
				{
					return new ListResponse<Sopra.Entities.Product>(
							data: new List<Sopra.Entities.Product>(),
							total: 0,
							page: 0
						);
				}

				// Searching
				if (!string.IsNullOrEmpty(search))
				{
					query = query.Where(x => x.RefID.ToString().Contains(search)
						|| x.Name.Contains(search)
						);
				}

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
								"CategoriesID" => query.Where(x => x.CategoriesID.ToString().Contains(value)),
								"name" => query.Where(x => x.Name.Contains(value)),
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
							"name" => query.OrderByDescending(x => x.Name),
							_ => query
						};
					}
					else
					{
						query = orderBy.ToLower() switch
						{
							"refid" => query.OrderBy(x => x.RefID),
							"name" => query.OrderBy(x => x.Name),
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

				// Adjust limit if it exceeds total count
				if (limit != 0 && limit > total)
				{
					limit = total; // Set limit to total count
				}

				// Calculate the maximum page number based on the limit
				int maxPage = (int)Math.Ceiling((double)total / limit);

				// Adjust the page number if it exceeds the maximum page number
				if (maxPage > 0 && page >= maxPage)
				{
					page = maxPage - 1;
				}

				// Set Limit and Page
				if (limit != 0)
					query = query.Skip(page * limit).Take(limit);

				// Get Data
				var data = await query.ToListAsync();

				// order product home page
				if (type.ToLower() == "launched")
				{
					if (sort.ToLower() == "volume" && order.ToLower() == "asc")
					{
						data = data.OrderBy(p => p.Volume.Value).ToList();
					}
					else if (sort.ToLower() == "volume" && order.ToLower() == "desc")
					{
						data = data.OrderByDescending(p => p.Volume.Value).ToList();
					}
					else if (sort.ToLower() == "price" && order.ToLower() == "asc")
					{
						data = data.OrderBy(p => p.Price.Value).ToList();
					}
					else if (sort.ToLower() == "price" && order.ToLower() == "desc")
					{
						data = data.OrderByDescending(p => p.Price.Value).ToList();
					}
					else
					{
						data = data.OrderByDescending(p => p.NewProdDate ?? DateTime.MinValue).ToList();
					}

				}
				if (type.ToLower() == "recommended")
				{
					if (sort.ToLower() == "volume" && order.ToLower() == "asc")
					{
						data = data.OrderBy(p => p.Volume.Value).ToList();
					}
					else if (sort.ToLower() == "volume" && order.ToLower() == "desc")
					{
						data = data.OrderByDescending(p => p.Volume.Value).ToList();
					}
					else if (sort.ToLower() == "price" && order.ToLower() == "asc")
					{
						data = data.OrderBy(p => p.Price.Value).ToList();
					}
					else if (sort.ToLower() == "price" && order.ToLower() == "desc")
					{
						data = data.OrderByDescending(p => p.Price.Value).ToList();
					}
					else
					{
						data = data.OrderBy(p => p.Volume).OrderByDescending(p => p.FavProd).ToList();
					}

				}
				if (type.ToLower() == "favourite")
				{
					if (sort.ToLower() == "volume" && order.ToLower() == "asc")
					{
						data = data.OrderBy(p => p.Volume.Value).ToList();
					}
					else if (sort.ToLower() == "volume" && order.ToLower() == "desc")
					{
						data = data.OrderByDescending(p => p.Volume.Value).ToList();
					}
					else if (sort.ToLower() == "price" && order.ToLower() == "asc")
					{
						data = data.OrderBy(p => p.Price.Value).ToList();
					}
					else if (sort.ToLower() == "price" && order.ToLower() == "desc")
					{
						data = data.OrderByDescending(p => p.Price.Value).ToList();
					}
					else
					{
						data = data.OrderBy(p => Guid.NewGuid())
													.ThenByDescending(p => p.TotalViews.Value)
												   .ToList();
					}

				}
				if (type.ToLower() == "random")
				{
					if (sort.ToLower() == "volume" && order.ToLower() == "asc")
					{
						data = data.OrderBy(p => p.Volume.Value).ToList();
					}
					else if (sort.ToLower() == "volume" && order.ToLower() == "desc")
					{
						data = data.OrderByDescending(p => p.Volume.Value).ToList();
					}
					else if (sort.ToLower() == "price" && order.ToLower() == "asc")
					{
						data = data.OrderBy(p => p.Price.Value).ToList();
					}
					else if (sort.ToLower() == "price" && order.ToLower() == "desc")
					{
						data = data.OrderByDescending(p => p.Price.Value).ToList();
					}
					else
					{
						data = data.OrderBy(p => Guid.NewGuid())
												  .ThenBy(p => p.TotalViews.Value)
												  .ToList();
					}

				}
				if (type.ToLower() == "bestseller")
				{
					if (sort.ToLower() == "volume" && order.ToLower() == "asc")
					{
						data = data.OrderBy(p => p.Volume.Value).ToList();
					}
					else if (sort.ToLower() == "volume" && order.ToLower() == "desc")
					{
						data = data.OrderByDescending(p => p.Volume.Value).ToList();
					}
					else if (sort.ToLower() == "price" && order.ToLower() == "asc")
					{
						data = data.OrderBy(p => p.Price.Value).ToList();
					}
					else if (sort.ToLower() == "price" && order.ToLower() == "desc")
					{
						data = data.OrderByDescending(p => p.Price.Value).ToList();
					}
					else if (sort.ToLower() == "totalqty" && order.ToLower() == "desc")
					{
						data = data.OrderByDescending(p => p.TotalQty.Value).ToList();
					}
					else
					{
						data = data.OrderBy(p => Guid.NewGuid())
												.ThenByDescending(p => p.TotalQty.Value)
												.ToList();
					}

				}

				if (data.Count <= 0 && page > 0)
				{
					page = 0;
					return await GetAllAsync(limit, page, total, search, sort, order, filter, date, type, userid, CategoriesID, categoriesId, productId);
				}

				//if (type.ToLower() == "bestseller")
				//{

				//}

				if (userid != 0)
				{
					foreach (var item in data)
					{
						if (item.Type.ToLower().Equals("bottle") || item.Type.ToLower().Equals("tray") || item.Type.ToLower().Equals("cup"))
						{
							item.QtyCart = Convert.ToInt64((from cart in _context.Carts
															join cartdetail in _context.CartDetails on cart.ID equals cartdetail.CartsID into carts
															from carts_detail in carts.DefaultIfEmpty()
															where cart.CustomersID == userid
																&& carts_detail.ObjectID == item.RefID
															&& carts_detail.Type == (item.Type == "closure" ? "closures" : item.Type)
															&& carts_detail.IsDeleted == false
															select carts_detail.Qty
													).Sum());
						}
					}
				}

				return new ListResponse<Product>(data, total, page);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				throw;
			}
		}

		public async Task<Product> GetByIdAsync(string type, long id, int? userid)
		{
			try
			{
				if (type.ToLower() == "bottle")
				{
					// return await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
					var imageQuery = from calculator in _context.Calculators
									 join image in _context.Images on calculator.RefID equals image.ObjectID into imagesGroup
									 where calculator.IsDeleted == false
										   && imagesGroup.Any(img => img.Type == "Calculators")
									 select new
									 {
										 CalculatorId = calculator.ID,
										 RealImages = imagesGroup
													   .Where(img => img.Type == "Calculators")
													   .Select(img => img.ProductImage)
													   .ToList()
									 };

					var images = imageQuery.ToDictionary(x => x.CalculatorId, x => x.RealImages.FirstOrDefault());

					var product = await (from calculator in _context.Calculators
										 join color in _context.Colors on calculator.ColorsID equals color.RefID into colors
										 from colors_detail in colors.DefaultIfEmpty()

											 //                              join productstatus in _context.ProductStatuses on calculator.RefID equals productstatus.ProductID into productstatuses
											 //from productstatuses_detail in productstatuses.DefaultIfEmpty()

										 let categorys_detail = _context.Categorys.Where(x => x.RefID == calculator.CategoriesID && x.Type == "Bottles").FirstOrDefault()

										 join material in _context.Materials on calculator.MaterialsID equals material.RefID into materials
										 from materials_detail in materials.DefaultIfEmpty()

										 join neck in _context.Necks on calculator.NecksID equals neck.RefID into necks
										 from necks_detail in necks.DefaultIfEmpty()

										 join packaging in _context.Packagings on calculator.PackagingsID equals packaging.RefID into packagings
										 from packagings_detail in packagings.DefaultIfEmpty()

										 let stockindicator = (from a in _context.ProductDetails2 where calculator.RefID == a.RefID select a).FirstOrDefault()

										 let countcolor = (from a in _context.ProductDetails2
															   //join b in _context.Colors on a.ColorsID equals b.RefID
														   where a.Name.Contains(calculator.Name + ",")
														   select a.Name).Distinct().ToList()
										 let imgList = (from a in _context.Images
														join b in _context.Calculators on a.ObjectID equals b.RefID
														where a.ObjectID == calculator.RefID
														  && a.Type == "Calculators"
														  && a.IsDeleted == false
														select new Sopra.Entities.Image
														{
															ID = a.ID,
															RefID = a.RefID,
															ProductImage = a.ProductImage,
															Type = a.Type,
															ObjectID = a.ObjectID
														}
													   ).OrderBy(x => x.RefID).ToList()
										 let funcList = (from a in _context.FunctionDetails
														 join b in _context.Functions on a.FunctionsID equals b.RefID
														 where a.ObjectID == calculator.RefID
														 && a.Type == "Bottle"
														 && a.IsDeleted == false
														 select new Sopra.Entities.Function
														 {
															 ID = b.ID,
															 RefID = b.RefID,
															 Name = b.Name
														 }
														).ToList()
										 let tagList = (from a in _context.TagDetails
														join b in _context.Tags on a.TagsID equals b.RefID
														where a.ObjectID == calculator.RefID
														&& a.Type == "Bottle"
														&& a.IsDeleted == false
														select new Tag
														{
															ID = b.ID,
															RefID = b.RefID,
															Name = b.Name
														}
														).ToList()
										 let tagVideo = (from a in _context.TagVideos
														 join b in _context.Tags on a.TagsID equals b.RefID
														 join c in _context.TagDetails on b.RefID equals c.TagsID
														 where a.IsDeleted == false
														 && c.ObjectID == calculator.RefID
														 && c.Type == "Bottle"
														 select new TagVideo
														 {
															 ID = b.ID,
															 RefID = b.RefID,
															 VideoLink = a.VideoLink,
															 Description = a.Description,
														 }
														).Distinct().ToList()
										 let packangings = (from a in _context.Packagings
															where a.IsDeleted == false
															&& a.RefID == calculator.PackagingsID
															select new Packaging
															{
																Name = a.Name,
																Length = a.Length,
																Width = a.Width,
																Thickness = a.Thickness,
																Height = a.Height,
																Tipe = a.Tipe,
															}
														).FirstOrDefault()
										 let qtyCart = (from c in _context.Carts
														join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
														from carts_detail in carts.DefaultIfEmpty()
														where c.CustomersID == userid
															&& carts_detail.ObjectID == calculator.RefID
														&& carts_detail.Type == "bottle"
														&& carts_detail.IsDeleted == false
														select carts_detail.Qty
														).Sum()
										 where calculator.RefID == id
											 && calculator.IsDeleted == false
										 select new Product
										 {
											 ID = calculator.ID,
											 RefID = calculator.RefID,
											 Name = calculator.Name,
											 NewProdDate = calculator.NewProdDate,
											 Image = calculator.Image,
											 Weight = calculator.Weight,
											 Price = calculator.Price,
											 CategoriesID = calculator.CategoriesID,
											 CategoryName = categorys_detail.Name,
											 Stock = calculator.Stock,
											 NewProd = calculator.NewProd,
											 FavProd = calculator.FavProd,
											 ClosuresID = calculator.ClosuresID,
											 Height = calculator.Height,
											 Length = calculator.Length,
											 Width = calculator.Width,
											 Volume = calculator.Volume,
											 Code = calculator.WmsCode,
											 QtyPack = calculator.QtyPack,
											 TotalViews = calculator.TotalViews,
											 TotalShared = calculator.TotalShared,
											 Note = calculator.Note,
											 TokpedUrl = calculator.TokpedUrl,
											 Plug = calculator.Plug,
											 PrintedHet = calculator.PrintedHet,

											 Color = colors_detail.Name,

											 StockIndicator = stockindicator.StockIndicator,

											 PlasticType = materials_detail.PlasticType,
											 MaterialsID = calculator.MaterialsID,
											 CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),

											 Neck = necks_detail.Code,

											 Packaging = packagings_detail.Tipe != 1 ?
														$"{packagings_detail.Length:F0} mm x {packagings_detail.Width:F0} mm x {packagings_detail.Height:F0} mm ({((packagings_detail.Length * packagings_detail.Width * packagings_detail.Height) / 1000000000):F2} m³)" :
														$"{((calculator.Length * calculator.Length * calculator.Height * calculator.QtyPack) / 1000000000):F2} m³",

											 RealImage = images.ContainsKey(calculator.ID) ? images[calculator.ID] : null,

											 Type = "bottle",
											 QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											 Images = imgList,
											 Functions = funcList,
											 Tags = tagList,
											 TagVideos = tagVideo,
											 LeadTime = _context.ProductStatuses.Where(x => x.ProductName.Contains(calculator.Name)).Select(x => x.LeadTime).FirstOrDefault(),
											 Packagings = packangings

										 }).FirstOrDefaultAsync();

					var data = await _context.Calculators.FirstOrDefaultAsync(x => x.RefID == id);
					if (data != null)
					{
						data.TotalViews++;
						_context.Calculators.Update(data);
						await _context.SaveChangesAsync();
					}
					return product;

				}
				else if (type.ToLower() == "closures")
				{
					var imageQuery = from closures in _context.Closures
									 join image in _context.Images on closures.RefID equals image.ObjectID into imagesGroup
									 where closures.IsDeleted == false
										   && closures.Status == 3
										   && imagesGroup.Any(img => img.Type == "Closures")
									 select new
									 {
										 ClosuresId = closures.ID,
										 RealImages = imagesGroup
													   .Where(img => img.Type == "Closures")
													   .Select(img => img.ProductImage)
													   .ToList()
									 };

					var images = imageQuery.ToDictionary(x => x.ClosuresId, x => x.RealImages.FirstOrDefault());

					var product = await (from closures in _context.Closures

										 join color in _context.Colors on closures.ColorsID equals color.RefID into colors
										 from colors_detail in colors.DefaultIfEmpty()

											 //                              join productstatus in _context.ProductStatuses on closures.RefID equals productstatus.ProductID into productstatuses
											 //from productstatuses_detail in productstatuses.DefaultIfEmpty()

										 join neck in _context.Necks on closures.NecksID equals neck.RefID into necks
										 from necks_detail in necks.DefaultIfEmpty()

										 let stockindicator = _context.StockIndicators
											 .Where(si => closures.Stock >= si.MinQty && closures.Stock <= si.MaxQty)
											 .FirstOrDefault()
										 let countcolor = (from a in _context.ProductDetails2
															   //join b in _context.Colors on a.ColorsID equals b.RefID
														   where a.Name.Contains(closures.Name + ",")
														   select a.Name).Distinct().ToList()
										 let qtyCart = 0
										 where closures.RefID == id
												&& closures.IsDeleted == false
										 select new Product
										 {
											 ID = closures.ID,
											 RefID = closures.RefID,
											 Name = closures.Name,
											 Image = closures.Image,
											 Weight = closures.Weight,
											 Price = closures.Price,
											 Height = closures.Height,
											 Diameter = closures.Diameter,
											 Code = closures.WmsCode,
											 QtyPack = closures.QtyPack,

											 Color = colors_detail.Name,

											 Neck = necks_detail.Code,

											 RealImage = images.ContainsKey(closures.ID) ? images[closures.ID] : null,

											 StockIndicator = stockindicator.Name,
											 MaterialsID = 0,
											 CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
											 Type = "closures",
											 QtyCart = Convert.ToInt64(qtyCart),
											 Images = _context.Images
													 .Where(image => image.ObjectID == closures.RefID)
													 .Where(image => image.Type == "Closures")
													 .Select(image => new Sopra.Entities.Image
													 {
														 ID = image.ID,
														 RefID = image.RefID,
														 ProductImage = image.ProductImage,
														 Type = image.Type,
														 ObjectID = image.ObjectID
													 }).OrderBy(x => x.RefID).ToList(),
											 LeadTime = _context.ProductStatuses.Where(x => x.ProductName.Contains(closures.Name)).Select(x => x.LeadTime).FirstOrDefault()

										 }).FirstOrDefaultAsync();

					var data = await _context.Closures.FirstOrDefaultAsync(x => x.RefID == id);
					if (data != null)
					{
						data.TotalViews++;
						_context.Closures.Update(data);
						await _context.SaveChangesAsync();
					}
					return product;
				}
				else if (type.ToLower() == "tray")
				{
					var imageQueryThermo = from thermo in _context.Thermos
										   join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
										   where thermo.IsDeleted == false
													 && thermo.Status == 3
													 && imagesGroup.Any(img => img.Type == "Thermos")
										   select new
										   {
											   ThermoId = thermo.ID,
											   RealImages = imagesGroup
															 .Where(img => img.Type == "Thermos")
															 .Select(img => img.ProductImage)
															 .ToList()
										   };

					var images = imageQueryThermo.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

					var product = await (from thermo in _context.Thermos
										 join color in _context.Colors on thermo.ColorsID equals color.RefID into colors
										 from colors_detail in colors.DefaultIfEmpty()

										 join rim in _context.Rims on thermo.RimsID equals rim.RefID into rims
										 from rims_detail in rims.DefaultIfEmpty()

											 //                              join productstatus in _context.ProductStatuses on thermo.RefID equals productstatus.ProductID into productstatuses
											 //from productstatuses_detail in productstatuses.DefaultIfEmpty()

										 join packaging in _context.Packagings on thermo.PackagingsID equals packaging.RefID into packagings
										 from packagings_detail in packagings.DefaultIfEmpty()

										 join material in _context.Materials on thermo.MaterialsID equals material.RefID into materials
										 from materials_detail in materials.DefaultIfEmpty()

										 let categorys_detail = _context.Categorys.Where(x => x.RefID == thermo.CategoriesID && x.Type == "Thermos").FirstOrDefault()

										 let stockindicator = _context.StockIndicators
											.Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
											.FirstOrDefault()
										 let countcolor = (from a in _context.ProductDetails2
															   //join b in _context.Colors on a.ColorsID equals b.RefID
														   where a.Name.Contains(thermo.Name + ",")
														   select a.Name).Distinct().ToList()
										 let funcList = (from a in _context.FunctionDetails
														 join b in _context.Functions on a.FunctionsID equals b.RefID
														 where a.ObjectID == thermo.RefID
														 && a.Type == "Thermo"
														 && a.IsDeleted == false
														 select new Sopra.Entities.Function
														 {
															 ID = b.ID,
															 RefID = b.RefID,
															 Name = b.Name
														 }
														).ToList()
										 let tagList = (from a in _context.TagDetails
														join b in _context.Tags on a.TagsID equals b.RefID
														where a.ObjectID == thermo.RefID
														&& a.Type == "Thermo"
														&& a.IsDeleted == false
														select new Tag
														{
															ID = b.ID,
															RefID = b.RefID,
															Name = b.Name
														}
														).ToList()
										 let tagVideo = (from a in _context.TagVideos
														 join b in _context.Tags on a.TagsID equals b.RefID
														 join c in _context.TagDetails on b.RefID equals c.TagsID
														 where a.IsDeleted == false
														 && c.ObjectID == thermo.RefID
														 && c.Type == "Thermo"
														 select new TagVideo
														 {
															 ID = b.ID,
															 RefID = b.RefID,
															 VideoLink = a.VideoLink,
															 Description = a.Description,
														 }
														).Distinct().ToList()
										 let packangings = (from a in _context.Packagings
															where a.IsDeleted == false
															&& a.RefID == thermo.PackagingsID
															select new Packaging
															{
																Name = a.Name,
																Length = a.Length,
																Width = a.Width,
																Thickness = a.Thickness,
																Height = a.Height,
																Tipe = a.Tipe,
															}
														).FirstOrDefault()
										 let qtyCart = (from c in _context.Carts
														join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
														from carts_detail in carts.DefaultIfEmpty()
														where c.CustomersID == userid
												 && carts_detail.ObjectID == thermo.RefID
												 && carts_detail.Type == "tray"
												 && carts_detail.IsDeleted == false
														select carts_detail.Qty
															).Sum()
										 where thermo.RefID == id
											   && thermo.IsDeleted == false
										 select new Product
										 {
											 ID = thermo.ID,
											 RefID = thermo.RefID,
											 Name = thermo.Name,
											 Image = thermo.Image,
											 Weight = thermo.Weight,
											 Price = thermo.Price,
											 CategoriesID = thermo.CategoriesID,
											 CategoryName = categorys_detail.Name,
											 Stock = thermo.Stock,
											 NewProd = thermo.NewProd,
											 FavProd = thermo.FavProd,
											 LidsID = thermo.LidsID,
											 Volume = thermo.Volume,
											 Height = thermo.Height,
											 Length = thermo.Length,
											 Width = thermo.Width,
											 QtyPack = thermo.Qty,
											 Code = thermo.WmsCode,
											 TotalShared = thermo.TotalShared,
											 TotalViews = thermo.TotalViews,
											 Note = thermo.Note,
											 TokpedUrl = thermo.TokpedUrl,
											 Rim = rims_detail.Name,
											 CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
											 Packaging = $"{packagings_detail.Length:F0} mm x {packagings_detail.Width:F0} mm x {packagings_detail.Height:F0} mm ({(packagings_detail.Length * packagings_detail.Width * packagings_detail.Height / 1000000000m):F2} m³)",
											 PrintedHet = Convert.ToInt32(thermo.PrintedHet),
											 Color = colors_detail.Name,

											 RealImage = images.ContainsKey(thermo.ID) ? images[thermo.ID] : null,

											 StockIndicator = stockindicator.Name,

											 PlasticType = materials_detail.PlasticType,
											 MaterialsID = thermo.MaterialsID,

											 Type = "tray",
											 QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											 Images = _context.Images
														   .Where(image => image.ObjectID == thermo.RefID)
														   .Where(image => image.Type == "Thermos")
														   .Select(image => new Sopra.Entities.Image
														   {
															   ID = image.ID,
															   RefID = image.RefID,
															   ProductImage = image.ProductImage,
															   Type = image.Type,
															   ObjectID = image.ObjectID
														   }).OrderBy(x => x.RefID).ToList(),
											 Functions = funcList,
											 Tags = tagList,
											 TagVideos = tagVideo,
											 LeadTime = _context.ProductStatuses.Where(x => x.ProductName.Contains(thermo.Name)).Select(x => x.LeadTime).FirstOrDefault(),
											 Packagings = packangings

										 }).FirstOrDefaultAsync();

					var data = await _context.Thermos.FirstOrDefaultAsync(x => x.RefID == id && x.CategoriesID != 5);
					if (data != null)
					{
						data.TotalViews++;
						_context.Thermos.Update(data);
						await _context.SaveChangesAsync();
					}

					return product;
				}
				else if (type.ToLower() == "cup")
				{
					var imageQueryThermo = from thermo in _context.Thermos
										   join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
										   where thermo.IsDeleted == false
													 && thermo.Status == 3
													 && imagesGroup.Any(img => img.Type == "Thermos")
										   select new
										   {
											   ThermoId = thermo.ID,
											   RealImages = imagesGroup
															 .Where(img => img.Type == "Thermos")
															 .Select(img => img.ProductImage)
															 .ToList()
										   };

					var images = imageQueryThermo.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

					var product = await (from thermo in _context.Thermos
										 join color in _context.Colors on thermo.ColorsID equals color.RefID into colors
										 from colors_detail in colors.DefaultIfEmpty()

											 //join productstatus in _context.ProductStatuses on thermo.RefID equals productstatus.ProductID into productstatuses
											 // from productstatuses_detail in productstatuses.DefaultIfEmpty()

										 join rim in _context.Rims on thermo.RimsID equals rim.RefID into rims
										 from rims_detail in rims.DefaultIfEmpty()

										 join packaging in _context.Packagings on thermo.PackagingsID equals packaging.RefID into packagings
										 from packagings_detail in packagings.DefaultIfEmpty()

										 join material in _context.Materials on thermo.MaterialsID equals material.RefID into materials
										 from materials_detail in materials.DefaultIfEmpty()
										 let countcolor = (from a in _context.ProductDetails2
															   //join b in _context.Colors on a.ColorsID equals b.RefID
														   where a.Name.Contains(thermo.Name + ",")
														   select a.Name).Distinct().ToList()
										 let qtyCart = (from c in _context.Carts
														join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
														from carts_detail in carts.DefaultIfEmpty()
														where c.CustomersID == userid
												 && carts_detail.ObjectID == thermo.RefID
												 && carts_detail.Type == "cup"
												 && carts_detail.IsDeleted == false
														select carts_detail.Qty
																).Sum()
										 let categorys_detail = _context.Categorys.Where(x => x.RefID == thermo.CategoriesID && x.Type == "Thermos").FirstOrDefault()
										 let stockindicator = _context.StockIndicators
											.Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
											.FirstOrDefault()
										 let funcList = (from a in _context.FunctionDetails
														 join b in _context.Functions on a.FunctionsID equals b.RefID
														 where a.ObjectID == thermo.RefID
														 && a.Type == "Thermo"
														 && a.IsDeleted == false
														 select new Sopra.Entities.Function
														 {
															 ID = b.ID,
															 RefID = b.RefID,
															 Name = b.Name
														 }
														).ToList()
										 let tagList = (from a in _context.TagDetails
														join b in _context.Tags on a.TagsID equals b.RefID
														where a.ObjectID == thermo.RefID
														&& a.Type == "Thermo"
														&& a.IsDeleted == false
														select new Tag
														{
															ID = b.ID,
															RefID = b.RefID,
															Name = b.Name
														}
														).ToList()
										 let tagVideo = (from a in _context.TagVideos
														 join b in _context.Tags on a.TagsID equals b.RefID
														 join c in _context.TagDetails on b.RefID equals c.TagsID
														 where a.IsDeleted == false
														 && c.ObjectID == thermo.RefID
														 && c.Type == "Thermo"
														 select new TagVideo
														 {
															 ID = b.ID,
															 RefID = b.RefID,
															 VideoLink = a.VideoLink,
															 Description = a.Description,
														 }
														).Distinct().ToList()
										 let packangings = (from a in _context.Packagings
															where a.IsDeleted == false
															&& a.RefID == thermo.PackagingsID
															select new Packaging
															{
																Name = a.Name,
																Length = a.Length,
																Width = a.Width,
																Thickness = a.Thickness,
																Height = a.Height,
																Tipe = a.Tipe,
															}
														).FirstOrDefault()
										 where thermo.RefID == id
											   && thermo.IsDeleted == false
										 select new Product
										 {
											 ID = thermo.ID,
											 RefID = thermo.RefID,
											 Name = thermo.Name,
											 Image = thermo.Image,
											 Weight = thermo.Weight,
											 Price = thermo.Price,
											 CategoriesID = thermo.CategoriesID,
											 CategoryName = categorys_detail.Name,
											 Stock = thermo.Stock,
											 NewProd = thermo.NewProd,
											 FavProd = thermo.FavProd,
											 LidsID = thermo.LidsID,
											 Volume = thermo.Volume,
											 Height = thermo.Height,
											 Length = thermo.Length,
											 Width = thermo.Width,
											 QtyPack = thermo.Qty,
											 Code = thermo.WmsCode,
											 TotalShared = thermo.TotalShared,
											 TotalViews = thermo.TotalViews,
											 Note = thermo.Note,
											 TokpedUrl = thermo.TokpedUrl,
											 Rim = rims_detail.Name,
											 PrintedHet = Convert.ToInt32(thermo.PrintedHet),
											 Packaging = $"{packagings_detail.Length:F0} mm x {packagings_detail.Width:F0} mm x {packagings_detail.Height:F0} mm ({(packagings_detail.Length * packagings_detail.Width * packagings_detail.Height / 1000000000m):F2} m³)",

											 Color = colors_detail.Name,

											 RealImage = images.ContainsKey(thermo.ID) ? images[thermo.ID] : null,

											 StockIndicator = stockindicator.Name,

											 PlasticType = materials_detail.PlasticType,
											 MaterialsID = thermo.MaterialsID,
											 CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
											 Type = "cup",
											 QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											 Images = _context.Images
														   .Where(image => image.ObjectID == thermo.RefID)
														   .Where(image => image.Type == "Thermos")
														   .Select(image => new Sopra.Entities.Image
														   {
															   ID = image.ID,
															   RefID = image.RefID,
															   ProductImage = image.ProductImage,
															   Type = image.Type,
															   ObjectID = image.ObjectID
														   }).OrderBy(x => x.RefID).ToList(),
											 Functions = funcList,
											 Tags = tagList,
											 TagVideos = tagVideo,
											 LeadTime = _context.ProductStatuses.Where(x => x.ProductName.Contains(thermo.Name)).Select(x => x.LeadTime).FirstOrDefault(),
											 Packagings = packangings

										 }).FirstOrDefaultAsync();

					var data = await _context.Thermos.FirstOrDefaultAsync(x => x.RefID == id && x.CategoriesID == 5);
					if (data != null)
					{
						data.TotalViews++;
						_context.Thermos.Update(data);
						await _context.SaveChangesAsync();
					}

					return product;
				}
				else if (type.ToLower() == "lid")
				{
					var imageQueryLid = from lid in _context.Lids
										join image in _context.Images on lid.RefID equals image.ObjectID into imagesGroup
										where lid.IsDeleted == false
												  && lid.Status == 3
												  && imagesGroup.Any(img => img.Type == "Lids")
										select new
										{
											LidId = lid.ID,
											RealImages = imagesGroup
														  .Where(img => img.Type == "Lids")
														  .Select(img => img.ProductImage)
														  .ToList()
										};

					var images = imageQueryLid.ToDictionary(x => x.LidId, x => x.RealImages.FirstOrDefault());

					// var CategoriesIDIds = new List<long> { 6, 8, 9 };

					var product = await (from lid in _context.Lids
										 join color in _context.Colors on lid.ColorsID equals color.RefID into colors
										 from colors_detail in colors.DefaultIfEmpty()

										 join rim in _context.Rims on lid.RimsID equals rim.RefID into rims
										 from rims_detail in rims.DefaultIfEmpty()

											 //                              join productstatus in _context.ProductStatuses on lid.RefID equals productstatus.ProductID into productstatuses
											 //from productstatuses_detail in productstatuses.DefaultIfEmpty()

										 join material in _context.Materials on lid.MaterialsID equals material.RefID into materials
										 from materials_detail in materials.DefaultIfEmpty()
											 //   let stockindicator = _context.StockIndicators
											 //      .Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
											 //      .FirstOrDefault()
										 let funcList = (from a in _context.FunctionDetails
														 join b in _context.Functions on a.FunctionsID equals b.RefID
														 where a.ObjectID == lid.RefID
														 && a.Type == "Lid"
														 && a.IsDeleted == false
														 select new Sopra.Entities.Function
														 {
															 ID = b.ID,
															 RefID = b.RefID,
															 Name = b.Name
														 }
														).ToList()
										 let qtyCart = 0
										 let tagList = (from a in _context.TagDetails
														join b in _context.Tags on a.TagsID equals b.RefID
														where a.ObjectID == lid.RefID
														&& a.Type == "Lid"
														&& a.IsDeleted == false
														select new Tag
														{
															ID = b.ID,
															RefID = b.RefID,
															Name = b.Name
														}
														).ToList()
										 let tagVideo = (from a in _context.TagVideos
														 join b in _context.Tags on a.TagsID equals b.RefID
														 join c in _context.TagDetails on b.RefID equals c.TagsID
														 where a.IsDeleted == false
														 && c.ObjectID == lid.RefID
														 && c.Type == "Lid"
														 select new TagVideo
														 {
															 ID = b.ID,
															 RefID = b.RefID,
															 VideoLink = a.VideoLink,
															 Description = a.Description,
														 }
														).Distinct().ToList()
										 let countcolor = (from a in _context.ProductDetails2
															   //join b in _context.Colors on a.ColorsID equals b.RefID
														   where a.Name.Contains(lid.Name + ",")
														   select a.Name).Distinct().ToList()
										 where lid.RefID == id
											   && lid.IsDeleted == false
										 // && CategoriesIDIds.Contains(thermo.CategoriesID)
										 select new Product
										 {
											 ID = lid.ID,
											 RefID = lid.RefID,
											 Name = lid.Name,
											 Image = lid.Image,
											 Weight = lid.Weight,
											 Price = lid.Price,
											 NewProd = lid.NewProd,
											 FavProd = lid.FavProd,
											 Height = lid.Height,
											 Length = lid.Length,
											 Width = lid.Width,
											 QtyPack = lid.Qty,
											 Code = lid.WmsCode,
											 Note = lid.Note,

											 Rim = rims_detail.Name,

											 Color = colors_detail.Name,

											 RealImage = images.ContainsKey(lid.ID) ? images[lid.ID] : null,

											 //   StockIndicator = stockindicator.Name,

											 PlasticType = materials_detail.PlasticType,
											 MaterialsID = lid.MaterialsID,

											 Type = "lid",
											 QtyCart = Convert.ToInt64(qtyCart),
											 Images = _context.Images
																   .Where(image => image.ObjectID == lid.RefID)
																   .Where(image => image.Type == "Lids")
																   .Select(image => new Sopra.Entities.Image
																   {
																	   ID = image.ID,
																	   RefID = image.RefID,
																	   ProductImage = image.ProductImage,
																	   Type = image.Type,
																	   ObjectID = image.ObjectID
																   }).OrderBy(x => x.RefID).ToList(),
											 Functions = funcList,
											 Tags = tagList,
											 TagVideos = tagVideo,
											 CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
											 LeadTime = _context.ProductStatuses.Where(x => x.ProductName.Contains(lid.Name)).Select(x => x.LeadTime).FirstOrDefault()
										 }).FirstOrDefaultAsync();

					var data = await _context.Lids.FirstOrDefaultAsync(x => x.RefID == id);
					if (data != null)
					{
						data.TotalViews++;
						_context.Lids.Update(data);
						await _context.SaveChangesAsync();
					}

					return product;
				}
				else
				{
					return null;
				}

			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				throw;
			}
		}

		public async Task<long> GetIncreaseShare(string type, long refid)
		{
			try
			{
				if (type.ToLower() == "bottle")
				{
					var data = await _context.Calculators.FirstOrDefaultAsync(x => x.RefID == refid);
					if (data != null)
					{
						data.TotalShared++;
						_context.Calculators.Update(data);
						await _context.SaveChangesAsync();
						return Convert.ToInt64(data.TotalShared);
					}
					return 0;
				}
				else if (type.ToLower() == "closures")
				{
					var data = await _context.Closures.FirstOrDefaultAsync(x => x.RefID == refid);
					if (data != null)
					{
						data.TotalShared++;
						_context.Closures.Update(data);
						await _context.SaveChangesAsync();
						return Convert.ToInt64(data.TotalShared);
					}
					return 0;
				}
				else if (type.ToLower() == "tray" || type.ToLower() == "cup")
				{
					var data = await _context.Thermos.Where(x => x.RefID == refid && (type.ToLower() == "cup" ? x.CategoriesID == 5 : x.CategoriesID != 5)).FirstOrDefaultAsync();
					if (data != null)
					{
						data.TotalShared++;
						_context.Thermos.Update(data);
						await _context.SaveChangesAsync();
						return Convert.ToInt64(data.TotalShared);
					}
					return 0;
				}
				else if (type.ToLower() == "lid")
				{
					var data = await _context.Lids.Where(x => x.RefID == refid).FirstOrDefaultAsync();
					if (data != null)
					{
						data.TotalShared++;
						_context.Lids.Update(data);
						await _context.SaveChangesAsync();
						return Convert.ToInt64(data.TotalShared);
					}
					return 0;
				}
				else
				{
					return 0;
				}
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				throw;
			}
		}

		public async Task<ListResponse<Product>> GetSearchAsync(int limit, int total, int page, string content, string sort, string order)
		{
			IQueryable<Product> query = null;

			try
			{
				// bottle
				var imageQueryBottle = from calculator in _context.Calculators
									   join image in _context.Images on calculator.RefID equals image.ObjectID into imagesGroup
									   where calculator.IsDeleted == false
											 && calculator.Status == 3
											 && imagesGroup.Any(img => img.Type == "Calculators")
									   select new
									   {
										   CalculatorId = calculator.ID,
										   RealImages = imagesGroup
														 .Where(img => img.Type == "Calculators")
														 .Select(img => img.ProductImage)
														 .ToList()
									   };

				var imagesBottle = imageQueryBottle.ToDictionary(x => x.CalculatorId, x => x.RealImages.FirstOrDefault());

				var productBottle = from calculator in _context.Calculators
									join color in _context.Colors on calculator.ColorsID equals color.RefID
									join material in _context.Materials on calculator.MaterialsID equals material.RefID
									where calculator.Name.Contains(content)
										&& calculator.IsDeleted == false
										&& calculator.Status == 3
									select new Product
									{
										ID = calculator.ID,
										RefID = calculator.RefID,
										Name = calculator.Name,
										Image = calculator.Image,
										Weight = calculator.Weight,
										Price = calculator.Price,
										Color = color.Name,

										PlasticType = material.PlasticType,

										Type = "bottle",

									};

				// closures
				var imageQueryClosures = from closures in _context.Closures
										 join image in _context.Images on closures.RefID equals image.ObjectID into imagesGroup
										 where closures.IsDeleted == false
											   && closures.Status == 3
											   && imagesGroup.Any(img => img.Type == "Closures")
										 select new
										 {
											 ClosuresId = closures.ID,
											 RealImages = imagesGroup
														   .Where(img => img.Type == "Closures")
														   .Select(img => img.ProductImage)
														   .ToList()
										 };

				var imagesClosures = imageQueryClosures.ToDictionary(x => x.ClosuresId, x => x.RealImages.FirstOrDefault());

				var productClosures = from closures in _context.Closures
									  join color in _context.Colors on closures.ColorsID equals color.RefID
									  where closures.Name.Contains(content)
											 && closures.IsDeleted == false
											 && closures.Status == 3
									  select new Product
									  {
										  ID = closures.ID,
										  RefID = closures.RefID,
										  Name = closures.Name,
										  Image = closures.Image,
										  Weight = closures.Weight,
										  Price = closures.Price,

										  Color = color.Name,

										  PlasticType = null,

										  Type = "closures"

									  };

				// tray
				var imageQueryTray = from thermo in _context.Thermos
									 join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
									 where thermo.IsDeleted == false
											   && thermo.Status == 3
											   && imagesGroup.Any(img => img.Type == "Thermos")
									 select new
									 {
										 ThermoId = thermo.ID,
										 RealImages = imagesGroup
													   .Where(img => img.Type == "Thermos")
													   .Select(img => img.ProductImage)
													   .ToList()
									 };

				var imagesTray = imageQueryTray.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

				var productTray = from thermo in _context.Thermos
								  join color in _context.Colors on thermo.ColorsID equals color.RefID
								  join material in _context.Materials on thermo.MaterialsID equals material.RefID
								  where thermo.Name.Contains(content)
										&& thermo.IsDeleted == false
										&& thermo.Status == 3
								  select new Product
								  {
									  ID = thermo.ID,
									  RefID = thermo.RefID,
									  Name = thermo.Name,
									  Image = thermo.Image,
									  Weight = thermo.Weight,
									  Price = thermo.Price,

									  Color = color.Name,

									  PlasticType = material.PlasticType,

									  Type = "tray"

								  };

				// cup
				var imageQueryCup = from thermo in _context.Thermos
									join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
									where thermo.IsDeleted == false
											  && thermo.Status == 3
											  && imagesGroup.Any(img => img.Type == "Thermos")
									select new
									{
										ThermoId = thermo.ID,
										RealImages = imagesGroup
													  .Where(img => img.Type == "Thermos")
													  .Select(img => img.ProductImage)
													  .ToList()
									};

				var imagesCup = imageQueryCup.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

				var productCup = from thermo in _context.Thermos
								 join color in _context.Colors on thermo.ColorsID equals color.RefID
								 join material in _context.Materials on thermo.MaterialsID equals material.RefID
								 where thermo.Name.Contains(content)
									   && thermo.IsDeleted == false
									   && thermo.Status == 3
								 select new Product
								 {
									 ID = thermo.ID,
									 RefID = thermo.RefID,
									 Name = thermo.Name,
									 Image = thermo.Image,
									 Weight = thermo.Weight,
									 Price = thermo.Price,

									 Color = color.Name,

									 PlasticType = material.PlasticType,

									 Type = "cup"

								 };


				// lid
				var imageQueryLid = from lid in _context.Lids
									join image in _context.Images on lid.RefID equals image.ObjectID into imagesGroup
									where lid.IsDeleted == false
											  && lid.Status == 3
											  && imagesGroup.Any(img => img.Type == "Lids")
									select new
									{
										LidId = lid.ID,
										RealImages = imagesGroup
													  .Where(img => img.Type == "Lids")
													  .Select(img => img.ProductImage)
													  .ToList()
									};

				var imagesLid = imageQueryLid.ToDictionary(x => x.LidId, x => x.RealImages.FirstOrDefault());

				// var CategoriesIDIds = new List<long> { 6, 8, 9 };

				var productLid = from lid in _context.Lids
								 join color in _context.Colors on lid.ColorsID equals color.RefID
								 join material in _context.Materials on lid.MaterialsID equals material.RefID
								 where lid.Name.Contains(content)
									   && lid.IsDeleted == false
									   && lid.Status == 3
								 select new Product
								 {
									 ID = lid.ID,
									 RefID = lid.RefID,
									 Name = lid.Name,
									 Image = lid.Image,
									 Weight = lid.Weight,
									 Price = lid.Price,

									 Color = color.Name,

									 PlasticType = material.PlasticType,

									 Type = "lid"

								 };

				var TagBottle = from a in _context.Tags
								join b in _context.TagDetails on a.RefID equals b.TagsID
								join c in _context.Calculators on b.ObjectID equals c.RefID
								join color in _context.Colors on c.ColorsID equals color.RefID
								join material in _context.Materials on c.MaterialsID equals material.RefID
								where a.Name.Contains(content)
								&& b.Type == "Bottle"
								&& c.IsDeleted == false
								&& c.Status == 3
								group a by new { c.ID, c.RefID, c.Name, c.Image, c.Weight, c.Price, colorName = color.Name, material.PlasticType } into grouped
								select new Product
								{
									ID = grouped.Key.ID,
									RefID = grouped.Key.RefID,
									Name = grouped.Key.Name,
									Image = grouped.Key.Image,
									Weight = grouped.Key.Weight,
									Price = grouped.Key.Price,
									Color = grouped.Key.colorName,
									PlasticType = grouped.Key.PlasticType,
									Type = "Tag bottle",
								};

				var TagThermo = from a in _context.Tags
								join b in _context.TagDetails on a.RefID equals b.TagsID
								join c in _context.Thermos on b.ObjectID equals c.RefID
								join color in _context.Colors on c.ColorsID equals color.RefID
								join material in _context.Materials on c.MaterialsID equals material.RefID
								where a.Name.Contains(content)
								&& b.Type == "Thermo"
								&& c.IsDeleted == false
								&& c.Status == 3
								group a by new { c.ID, c.RefID, c.Name, c.Image, c.Weight, c.Price, colorName = color.Name, material.PlasticType } into grouped
								select new Product
								{
									ID = grouped.Key.ID,
									RefID = grouped.Key.RefID,
									Name = grouped.Key.Name,
									Image = grouped.Key.Image,
									Weight = grouped.Key.Weight,
									Price = grouped.Key.Price,
									Color = grouped.Key.colorName,
									PlasticType = grouped.Key.PlasticType,
									Type = "Tag thermo",
								};

				var TagLid = from a in _context.Tags
							 join b in _context.TagDetails on a.RefID equals b.TagsID
							 join c in _context.Lids on b.ObjectID equals c.RefID
							 join color in _context.Colors on c.ColorsID equals color.RefID
							 join material in _context.Materials on c.MaterialsID equals material.RefID
							 where a.Name.Contains(content)
							 && b.Type == "Lid"
							 && c.IsDeleted == false
							 && c.Status == 3
							 group a by new { c.ID, c.RefID, c.Name, c.Image, c.Weight, c.Price, colorName = color.Name, material.PlasticType } into grouped
							 select new Product
							 {
								 ID = grouped.Key.ID,
								 RefID = grouped.Key.RefID,
								 Name = grouped.Key.Name,
								 Image = grouped.Key.Image,
								 Weight = grouped.Key.Weight,
								 Price = grouped.Key.Price,
								 Color = grouped.Key.colorName,
								 PlasticType = grouped.Key.PlasticType,
								 Type = "Tag lid",
							 };

				var FuntionBottle = from a in _context.Functions
									join b in _context.FunctionDetails on a.RefID equals b.FunctionsID
									join c in _context.Calculators on b.ObjectID equals c.RefID
									join color in _context.Colors on c.ColorsID equals color.RefID
									join material in _context.Materials on c.MaterialsID equals material.RefID
									where a.Name.Contains(content)
									&& b.Type == "Bottle"
									&& c.IsDeleted == false
									&& c.Status == 3
									group a by new { c.ID, c.RefID, c.Name, c.Image, c.Weight, c.Price, colorName = color.Name, material.PlasticType } into grouped
									select new Product
									{
										ID = grouped.Key.ID,
										RefID = grouped.Key.RefID,
										Name = grouped.Key.Name,
										Image = grouped.Key.Image,
										Weight = grouped.Key.Weight,
										Price = grouped.Key.Price,
										Color = grouped.Key.colorName,
										PlasticType = grouped.Key.PlasticType,
										Type = "Function bottle",
									};

				var FunctionThermo = from a in _context.Functions
									 join b in _context.FunctionDetails on a.RefID equals b.FunctionsID
									 join c in _context.Thermos on b.ObjectID equals c.RefID
									 join color in _context.Colors on c.ColorsID equals color.RefID
									 join material in _context.Materials on c.MaterialsID equals material.RefID
									 where a.Name.Contains(content)
									 && b.Type == "Thermo"
									 && c.IsDeleted == false
									 && c.Status == 3
									 group a by new { c.ID, c.RefID, c.Name, c.Image, c.Weight, c.Price, colorName = color.Name, material.PlasticType } into grouped
									 select new Product
									 {
										 ID = grouped.Key.ID,
										 RefID = grouped.Key.RefID,
										 Name = grouped.Key.Name,
										 Image = grouped.Key.Image,
										 Weight = grouped.Key.Weight,
										 Price = grouped.Key.Price,
										 Color = grouped.Key.colorName,
										 PlasticType = grouped.Key.PlasticType,
										 Type = "Function thermo",
									 };

				var FunctionLid = from a in _context.Functions
								  join b in _context.FunctionDetails on a.RefID equals b.FunctionsID
								  join c in _context.Lids on b.ObjectID equals c.RefID
								  join color in _context.Colors on c.ColorsID equals color.RefID
								  join material in _context.Materials on c.MaterialsID equals material.RefID
								  where a.Name.Contains(content)
								  && b.Type == "Lid"
								  && c.IsDeleted == false
								  && c.Status == 3
								  group a by new { c.ID, c.RefID, c.Name, c.Image, c.Weight, c.Price, colorName = color.Name, material.PlasticType } into grouped
								  select new Product
								  {
									  ID = grouped.Key.ID,
									  RefID = grouped.Key.RefID,
									  Name = grouped.Key.Name,
									  Image = grouped.Key.Image,
									  Weight = grouped.Key.Weight,
									  Price = grouped.Key.Price,
									  Color = grouped.Key.colorName,
									  PlasticType = grouped.Key.PlasticType,
									  Type = "Function lid",
								  };


				var combinedQuery = productBottle
									.Union(productClosures)
									.Union(productTray)
									.Union(productLid)
									.Union(TagBottle)
									.Union(TagThermo)
									.Union(TagLid)
									.Union(FuntionBottle)
									.Union(FunctionThermo)
									.Union(FunctionLid);

				var finalQuery = from product in combinedQuery
								 select new Product
								 {
									 ID = product.ID,
									 RefID = product.RefID,
									 Name = product.Name,
									 Image = product.Image,
									 Weight = product.Weight,
									 Price = product.Price,
									 Color = product.Color,
									 RealImage =
										product.Type == "bottle" ?
										imagesBottle.ContainsKey(product.ID) ? imagesBottle[product.ID] : null :

										product.Type == "closures" ?
										imagesClosures.ContainsKey(product.ID) ? imagesClosures[product.ID] : null :

										product.Type == "tray" ?
										imagesTray.ContainsKey(product.ID) ? imagesTray[product.ID] : null :

										 product.Type == "cup" ?
										imagesCup.ContainsKey(product.ID) ? imagesCup[product.ID] : null :

										product.Type == "Tag bottle" ?
										imagesBottle.ContainsKey(product.ID) ? imagesBottle[product.ID] : null :

										product.Type == "Tag thermo" ?
										imagesTray.ContainsKey(product.ID) ? imagesTray[product.ID] : null :

										product.Type == "Tag lid" ?
										imagesLid.ContainsKey(product.ID) ? imagesLid[product.ID] : null :

										product.Type == "Function bottle" ?
										imagesBottle.ContainsKey(product.ID) ? imagesBottle[product.ID] : null :

										product.Type == "Function thermo" ?
										imagesTray.ContainsKey(product.ID) ? imagesTray[product.ID] : null :

										product.Type == "Function lid" ?
										imagesLid.ContainsKey(product.ID) ? imagesLid[product.ID] : null :

										imagesLid.ContainsKey(product.ID) ? imagesLid[product.ID] : null,

									 PlasticType = product.PlasticType,
									 Type = product.Type
								 };

				query = finalQuery;

				// Get Total Before Limit and Page
				total = await query.CountAsync();

				// Adjust limit if it exceeds total count
				if (limit != 0 && limit > total)
				{
					limit = total; // Set limit to total count
				}

				// Calculate the maximum page number based on the limit
				int maxPage = (int)Math.Ceiling((double)total / limit);

				// Adjust the page number if it exceeds the maximum page number
				if (maxPage > 0 && page >= maxPage)
				{
					page = maxPage - 1;
				}

				// Set Limit and Page
				if (limit != 0)
					query = query.Skip(page * limit).Take(limit);

				// Get Data
				var data = await query.ToListAsync();

				if (sort.ToLower() != null && order.ToLower() != null)
				{
					if (sort.ToLower() == "volume" && order.ToLower() == "asc")
					{
						data = data.OrderBy(p => p.Volume.Value).ToList();
					}
					else if (sort.ToLower() == "volume" && order.ToLower() == "desc")
					{
						data = data.OrderByDescending(p => p.Volume.Value).ToList();
					}
					else if (sort.ToLower() == "price" && order.ToLower() == "asc")
					{
						data = data.OrderBy(p => p.Price.Value).ToList();
					}
					else if (sort.ToLower() == "price" && order.ToLower() == "desc")
					{
						data = data.OrderByDescending(p => p.Price.Value).ToList();
					}
				}

				if (data.Count <= 0 && page > 0)
				{
					page = 0;
					return await GetSearchAsync(limit, page, total, content, sort, order);
				}

				return new ListResponse<Product>(data, total, page);

			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				throw;
			}
		}

		public async Task<ListResponseProduct<Product>> GetListAsync(int limit, int total, int page, string search, string sort, string type, string CategoriesID, List<long> sub, int filterNew, int filterFavourite, decimal filterStockIndicatorMin, decimal filterStockIndicatorMax, List<long> filterColor, decimal filterVolumeMin, decimal filterVolumeMax, List<long> filterShape, List<long> filterNeck, List<long> filterRim, decimal filterPriceMin, decimal filterPriceMax, decimal filterDiameterMin, decimal filterDiameterMax, decimal? filterWidthMin, decimal filterWidthMax, decimal filterHeightMin, decimal filterHeightMax, string materialType, string lidType, int? userid)
		{
			IQueryable<Product> query = null;
			IEnumerable<Product> getCalculator = null;
			IEnumerable<Product> getClosures = null;
			IEnumerable<Product> getTray = null;
			IEnumerable<Product> getLid = null;
			var tipe = "";
			var isSort = false;
			var isFilter = false;

			if (sort != "" || type != "") isSort = true;

			if (filterNew != 0 || filterFavourite != 0 || filterStockIndicatorMin != 0 ||
				filterStockIndicatorMax != 0 || filterColor.Any() || filterVolumeMin != 0 ||
				filterVolumeMax != 0 || filterShape.Any() || filterNeck.Any() ||
				filterRim.Any() || filterPriceMin != 0 || filterPriceMax != 0 ||
				filterDiameterMin != 0 || filterDiameterMax != 0 || filterWidthMin != null ||
				filterWidthMax != 0 || filterHeightMin != 0 || filterHeightMax != 0 ||
				materialType != "" || lidType != "") isFilter = true;


			try
			{
				_context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

				// product list page
				if (CategoriesID.ToLower() == "bottles" && !sub.Contains(25))
				{
					//var imageQuery = from calculator in _context.Calculators
					//				 join image in _context.Images on calculator.RefID equals image.ObjectID into imagesGroup
					//				 where calculator.IsDeleted == false
					//					   && calculator.Status == 3
					//					   && imagesGroup.Any(img => img.Type == "Calculators")
					//				 select new
					//				 {
					//					 CalculatorId = calculator.ID,
					//					 RealImages = imagesGroup
					//								   .Where(img => img.Type == "Calculators")
					//								   .Select(img => img.ProductImage)
					//								   .ToList()
					//				 };

					//var images = imageQuery.ToDictionary(x => x.CalculatorId, x => x.RealImages.FirstOrDefault());

					getCalculator = from product_detail in _context.ProductDetails2
									let countcolor = (from a in _context.ProductDetails2
													  join b in _context.Colors on a.ColorsID equals b.RefID
													  where a.Name.Contains(product_detail.Name)
													  select b.Name).Distinct().ToList()
									where product_detail.IsDeleted == false
										  && sub.Contains((long)product_detail.CategoriesID)
										  && product_detail.Type == "bottle"
									select new Product
									{
										ID = (long)product_detail.OriginID,
										RefID = product_detail.RefID,
										Name = product_detail.Name,
										Image = product_detail.Image,
										Weight = product_detail.Weight,
										Price = product_detail.Price,
										CategoriesID = product_detail.CategoriesID,
										Stock = product_detail.Stock,
										NewProd = product_detail.NewProd,
										FavProd = product_detail.FavProd,
										ClosuresID = product_detail.ClosuresID,
										Volume = product_detail.Volume,
										Code = product_detail.WmsCode,
										QtyPack = product_detail.QtyPack,
										TotalViews = product_detail.TotalViews,
										TotalShared = product_detail.TotalShared,
										NewProdDate = product_detail.NewProdDate,
										Height = product_detail.Height,
										Length = product_detail.Length,
										ColorsID = product_detail.ColorsID,
										ShapesID = product_detail.ShapesID,
										NecksID = product_detail.NecksID,
										Width = product_detail.Width,

										//Color = color.Name,

										RealImage = product_detail.RealImage,

										StockIndicator = product_detail.StockIndicator,

										PlasticType = product_detail.PlasticType,

										CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
										//Wishlist = product_detail.Whistlist == null ? 0 : product_detail.Whistlist,

										Type = "bottle"
									};
					query = getCalculator.AsQueryable();
					tipe = "bottle";
				}
				else if (CategoriesID.ToLower() == "bottles" && sub.Contains(25))
				{
					//var imageQuery = from closures in _context.Closures
					//				 join image in _context.Images on closures.RefID equals image.ObjectID into imagesGroup
					//				 where closures.IsDeleted == false
					//					   && closures.Status == 3
					//					   && imagesGroup.Any(img => img.Type == "Closures")
					//				 select new
					//				 {
					//					 ClosuresId = closures.ID,
					//					 RealImages = imagesGroup
					//								   .Where(img => img.Type == "Closures")
					//								   .Select(img => img.ProductImage)
					//								   .ToList()
					//				 };

					//var images = imageQuery.ToDictionary(x => x.ClosuresId, x => x.RealImages.FirstOrDefault());

					getClosures = from product_detail in _context.ProductDetails2
								  where product_detail.IsDeleted == false
											&& product_detail.Type == "closure"
								  select new Product
								  {
									  ID = (long)product_detail.OriginID,
									  RefID = product_detail.RefID,
									  Name = product_detail.Name,
									  Image = product_detail.Image,
									  Weight = product_detail.Weight,
									  Price = product_detail.Price,
									  Height = product_detail.Height,
									  Code = product_detail.WmsCode,
									  QtyPack = product_detail.QtyPack,
									  Stock = product_detail.Stock,
									  //Color = color.Name,
									  ColorsID = product_detail.ColorsID,
									  Diameter = product_detail.Diameter,
									  RealImage = product_detail.RealImage,
									  NecksID = product_detail.NecksID,
									  StockIndicator = product_detail.StockIndicator,
									  //Wishlist = product_detail.Whistlist != null ? (long)product_detail.Whistlist : 0,
									  MaterialsID = 0,
									  Type = "closures"
								  };
					query = getClosures.AsQueryable();
					tipe = "closure";
				}
				else if (CategoriesID.ToLower() == "thermos" && !sub.Contains(6))
				{
					//var imageQueryThermo = from thermo in _context.Thermos
					//					   join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
					//					   where thermo.IsDeleted == false
					//								 && thermo.Status == 3
					//								 && imagesGroup.Any(img => img.Type == "Thermos")
					//					   select new
					//					   {
					//						   ThermoId = thermo.ID,
					//						   RealImages = imagesGroup
					//										 .Where(img => img.Type == "Thermos")
					//										 .Select(img => img.ProductImage)
					//										 .ToList()
					//					   };

					//var images = imageQueryThermo.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

					var CategoriesIDIdsTray = new List<long> { 4, 7 };
					var CategoriesIDIdsCup = new List<long> { 5 };

					getTray = from product_detail in _context.ProductDetails2
							  where product_detail.IsDeleted == false
									&& (product_detail.Type == "cup" || product_detail.Type == "tray")
									 && ((sub.Contains(4) && sub.Contains(5)) ? (CategoriesIDIdsTray.Contains((long)product_detail.CategoriesID) || CategoriesIDIdsCup.Contains((long)product_detail.CategoriesID)) :
										sub.Contains(4) ? CategoriesIDIdsTray.Contains((long)product_detail.CategoriesID) :
										sub.Contains(5) ? CategoriesIDIdsCup.Contains((long)product_detail.CategoriesID) :
										false)
							  select new Product
							  {
								  ID = (long)product_detail.OriginID,
								  RefID = product_detail.RefID,
								  Name = product_detail.Name,
								  Image = product_detail.Image,
								  Weight = product_detail.Weight,
								  Price = product_detail.Price,
								  CategoriesID = product_detail.CategoriesID,
								  Stock = product_detail.Stock,
								  NewProd = product_detail.NewProd,
								  FavProd = product_detail.FavProd,
								  ClosuresID = product_detail.LidsID,
								  Volume = product_detail.Volume,
								  Height = product_detail.Height,
								  Length = product_detail.Length,
								  Width = product_detail.Width,
								  QtyPack = product_detail.QtyPack,
								  Code = product_detail.WmsCode,
								  TotalShared = product_detail.TotalShared,
								  TotalViews = product_detail.TotalViews,
								  RimsID = product_detail.RimsID,

								  ShapesID = product_detail.ShapesID,
								  ColorsID = product_detail.ColorsID,
								  NecksID = null,

								  RealImage = product_detail.RealImage,

								  StockIndicator = product_detail.StockIndicator,

								  PlasticType = product_detail.PlasticType,


								  //Wishlist = product_detail.Whistlist != null ? product_detail.Whistlist : 0,
								  Type = sub.Contains(5) && !sub.Contains(4) ? "cup" :
										 sub.Contains(4) && !sub.Contains(5) ? "tray" :
										 (sub.Contains(4) && sub.Contains(5)) ? "tray" :
										 null
							  };

					query = getTray.AsQueryable();
					tipe = getTray.FirstOrDefault().Type;
				}
				//else if (CategoriesID.ToLower() == "thermos" && sub.Contains(5))
				//{
				//	var imageQueryThermo = from thermo in _context.Thermos
				//						   join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
				//						   where thermo.IsDeleted == false
				//									 && thermo.Status == 3
				//									 && imagesGroup.Any(img => img.Type == "Thermos")
				//						   select new
				//						   {
				//							   ThermoId = thermo.ID,
				//							   RealImages = imagesGroup
				//											 .Where(img => img.Type == "Thermos")
				//											 .Select(img => img.ProductImage)
				//											 .ToList()
				//						   };

				//	var images = imageQueryThermo.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

				//	var CategoriesIDIds = new List<long> { 5 };

				//	var getCup = from thermo in _context.Thermos
				//				 join color in _context.Colors on thermo.ColorsID equals color.RefID
				//				 join material in _context.Materials on thermo.MaterialsID equals material.RefID
				//				 let stockindicator = _context.StockIndicators
				//					.Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
				//					.FirstOrDefault()
				//				 where thermo.IsDeleted == false
				//				   && thermo.Status == 3
				//				   && CategoriesIDIds.Contains(thermo.CategoriesID)
				//				 select new Product
				//				 {
				//					 ID = thermo.ID,
				//					 RefID = thermo.RefID,
				//					 Name = thermo.Name,
				//					 Image = thermo.Image,
				//					 Weight = thermo.Weight,
				//					 Price = thermo.Price,
				//					 CategoriesID = thermo.CategoriesID,
				//					 Stock = thermo.Stock,
				//					 NewProd = thermo.NewProd,
				//					 FavProd = thermo.FavProd,
				//					 Closure = thermo.LidsID,
				//					 Volume = thermo.Volume,
				//					 Height = thermo.Height,
				//					 Length = thermo.Length,
				//					 Width = thermo.Width,
				//					 QtyPack = thermo.Qty,
				//					 Code = thermo.Code,
				//					 TotalShared = thermo.TotalShared,
				//					 TotalViews = thermo.TotalViews,

				//					 ShapesID = null,
				//					 NecksID = null,

				//					 Color = color.Name,

				//					 RealImage = images.ContainsKey(thermo.ID) ? images[thermo.ID] : null,

				//					 StockIndicator = stockindicator.Name,

				//					 PlasticType = material.PlasticType,

				//					 Type = "cup"
				//				 };

				//	query = getCup;
				//}
				else if (CategoriesID.ToLower() == "thermos" && sub.Contains(6))
				{
					//var imageQueryLid = from lid in _context.Lids
					//					join image in _context.Images on lid.RefID equals image.ObjectID into imagesGroup
					//					where lid.IsDeleted == false
					//							  && lid.Status == 3
					//							  && imagesGroup.Any(img => img.Type == "Lids")
					//					select new
					//					{
					//						LidId = lid.ID,
					//						RealImages = imagesGroup
					//									  .Where(img => img.Type == "Lids")
					//									  .Select(img => img.ProductImage)
					//									  .ToList()
					//					};

					//var images = imageQueryLid.ToDictionary(x => x.LidId, x => x.RealImages.FirstOrDefault());

					// var CategoriesIDIds = new List<long> { 6, 8, 9 };

					getLid = from product_detail in _context.ProductDetails2
							 where product_detail.IsDeleted == false
								   && product_detail.Type == "lid"
							 let thermoCategoriesID = _context.ProductDetails2
												.Where(p => product_detail.RimsID == p.RimsID && (p.Type == "tray" || p.Type == "cup"))
												.FirstOrDefault()
							 select new Product
							 {
								 ID = (long)product_detail.OriginID,
								 RefID = product_detail.RefID,
								 Name = product_detail.Name,
								 Image = product_detail.Image,
								 Weight = product_detail.Weight,
								 Price = product_detail.Price,
								 NewProd = product_detail.NewProd,
								 FavProd = product_detail.FavProd,
								 Height = product_detail.Height,
								 Length = product_detail.Length,
								 Width = product_detail.Width,
								 QtyPack = product_detail.QtyPack,
								 RimsID = product_detail.RimsID,
								 //Color = color.Name,
								 ColorsID = product_detail.ColorsID,
								 CategoriesID = thermoCategoriesID.CategoriesID,
								 RealImage = product_detail.RealImage,

								 //   StockIndicator = stockindicator.Name,

								 PlasticType = product_detail.PlasticType,
								 //Wishlist = product_detail.Whistlist != null ? product_detail.Whistlist : 0,

								 Type = "lid"
							 };

					query = getLid.AsQueryable();
					tipe = "lid";
				}
				else
				{
					return new ListResponseProduct<Sopra.Entities.Product>(
							data: new List<Sopra.Entities.Product>(),
							total: 0,
							page: 0,
							filters: null
						);
				}

				// filter run
				if (CategoriesID.ToLower() == "bottles" && !sub.Contains(25) && isFilter == true)
				{
					//filter new product
					if (filterNew != 0)
					{
						query = query.Where(p => p.NewProd != 0);
					}

					//filter favourite product
					if (filterFavourite != 0)
					{
						query = query.Where(p => p.FavProd != 0);
					}

					//filter stock indicator
					if (filterStockIndicatorMin != 0 && filterStockIndicatorMax != 0)
					{
						query = query.Where(p => p.Stock >= filterStockIndicatorMin && p.Stock <= filterStockIndicatorMax);
					}

					//filter color
					if (filterColor != null && filterColor.Any())
					{
						query = query.Where(p => filterColor.Contains(p.ColorsID.GetValueOrDefault()));
					}
					//else
					//{
					//	query = query.Where(p => p.ColorsID == 14);
					//}

					//filter volume
					if (filterVolumeMin != 0 && filterVolumeMax != 0)
					{
						query = query.Where(p => p.Volume >= Math.Floor(filterVolumeMin) && p.Volume <= Math.Round(filterVolumeMax));
					}

					//filter shpae
					if (filterShape != null && filterShape.Any())
					{
						query = query.Where(p => filterShape.Contains(p.ShapesID.GetValueOrDefault()));
					}

					//filter neck
					if (filterNeck != null && filterNeck.Any())
					{
						query = query.Where(p => filterNeck.Contains(p.NecksID.GetValueOrDefault()));
					}

					//filter price
					if (filterPriceMin != 0 && filterPriceMax != 0)
					{
						query = query.Where(p => p.Price >= Math.Floor(filterPriceMin) && p.Price <= Math.Round(filterPriceMax));
					}

					//filter diameter
					if (filterDiameterMin != 0 && filterDiameterMax != 0)
					{
						query = query.Where(p => p.Length >= Math.Floor(filterDiameterMin) && p.Length <= Math.Round(filterDiameterMax));
					}

					//filter width
					if (filterWidthMin != null && filterWidthMax != 0)
					{
						query = query.Where(p => p.Width >= Math.Floor(filterWidthMin.GetValueOrDefault()) && p.Width <= Math.Round(filterWidthMax));
					}

					//filter height
					if (filterHeightMin != 0 && filterHeightMax != 0)
					{
						query = query.Where(p => p.Height >= Math.Floor(filterHeightMin) && p.Height <= Math.Round(filterHeightMax));
					}
				}
				else if (tipe == "closure" && isFilter == true)
				{
					//filter color
					if (filterColor != null && filterColor.Any())
					{
						query = query.Where(p => filterColor.Contains(p.ColorsID.GetValueOrDefault()));
					}
					//else
					//{
					//    query = query.Where(p => p.ColorsID == 14);
					//}

					//filter neck
					if (filterNeck != null && filterNeck.Any())
					{
						query = query.Where(p => filterNeck.Contains(p.NecksID.GetValueOrDefault()));
					}

					//filter price
					if (filterPriceMin != 0 && filterPriceMax != 0)
					{
						query = query.Where(p => p.Price >= Math.Floor(filterPriceMin) && p.Price <= Math.Round(filterPriceMax));
					}

					//filter diameter
					if (filterDiameterMin != 0 && filterDiameterMax != 0)
					{
						query = query.Where(p => p.Length >= Math.Floor(filterDiameterMin) && p.Length <= Math.Round(filterDiameterMax));
					}

					//filter height
					if (filterHeightMin != 0 && filterHeightMax != 0)
					{
						query = query.Where(p => p.Height >= Math.Floor(filterHeightMin) && p.Height <= Math.Round(filterHeightMax));
					}
				}
				else if ((tipe == "tray" || tipe == "lid" || tipe == "cup") && isFilter == true)
				{
					//filter new product
					if (filterNew != 0)
					{
						query = query.Where(p => p.NewProd != 0);
					}

					//filter favourite product
					if (filterFavourite != 0)
					{
						query = query.Where(p => p.FavProd != 0);
					}

					//filter rim
					if (filterRim != null && filterRim.Any())
					{
						query = query.Where(p => filterRim.Contains(p.RimsID.GetValueOrDefault()));
					}

					//filter price
					if (filterPriceMin != 0 && filterPriceMax != 0)
					{
						query = query.Where(p => p.Price >= Math.Floor(filterPriceMin) && p.Price <= Math.Round(filterPriceMax));
					}

					//filter diameter
					if (filterDiameterMin != 0 && filterDiameterMax != 0)
					{
						query = query.Where(p => p.Length >= Math.Floor(filterDiameterMin) && p.Length <= Math.Round(filterDiameterMax));
					}

					//filter height
					if (filterHeightMin != 0 && filterHeightMax != 0)
					{
						query = query.Where(p => p.Height >= Math.Floor(filterHeightMin) && p.Height <= Math.Round(filterHeightMax));
					}

					//filter volume
					if (filterVolumeMin != 0 && filterVolumeMax != 0)
					{
						query = query.Where(p => p.Volume >= Math.Floor(filterVolumeMin) && p.Volume <= Math.Round(filterVolumeMax));
					}

					//filter shpae
					if (filterShape != null && filterShape.Any())
					{
						query = query.Where(p => filterShape.Contains(p.ShapesID.GetValueOrDefault()));
					}

					//filter material type
					if (materialType != "" && (materialType == "PP" || materialType == "PET"))
					{
						query = query.Where(p => p.PlasticType == materialType);
					}

					//filter tray or cup for lid
					if (lidType != "" && tipe == "lid")
					{
						if (lidType.ToLower() == "cup") query = query.Where(p => p.CategoriesID == 5);
						if (lidType.ToLower() == "tray") query = query.Where(p => p.CategoriesID != 5);
					}
				}

				//cek stock indicator
				var distinctStockIndicators = _context.StockIndicators
					.Where(si => si.IsDeleted == false)
					.Select(si => si.Name)
					.Distinct()
					.ToList();

				// cek shape
				var distinctShape = _context.Shapes
					.Where(si => si.IsDeleted == false)
					.Select(si => new { si.Name, si.Type })
					.Distinct()
					.ToList();

				//cek color
				var distinctColors = _context.Colors
					.Where(si => si.IsDeleted == false)
					.Select(si => si.Name)
					.Distinct()
					.ToList();

				//cek color
				var distinctNeck = _context.Necks
					.Where(si => si.IsDeleted == false)
					.Select(si => si.Code)
					.Distinct()
					.ToList();

				//cek rim
				var distinctRim = _context.Rims
					.Where(si => si.IsDeleted == false)
					.Select(si => si.Name)
					.Distinct()
					.ToList();

				List<FilterGroup> filterGroups = new List<FilterGroup>();

				if (CategoriesID.ToLower() == "bottles" && !sub.Contains(25))
				{

					filterGroups = new List<FilterGroup>
					{
						new FilterGroup
						{
							GroupName = "Popular",
							Filter = new List<FilterInfo>
							{
								new FilterInfo { Name = "New Product", Value = 1, Count = query.Count(p => (p.NewProd ?? 0) != 0) },
								new FilterInfo { Name = "Favourite Product", Value = 1, Count = query.Count(p => (p.FavProd ?? 0) != 0) },
							}.Where(filterInfo => filterInfo.Count > 0).ToList()
						},
						new FilterGroup
						{
							GroupName = "Stock Indicator",
							Filter = distinctStockIndicators.Select(stockIndicatorValue =>
							{
								var stockIndicatorInfo = _context.StockIndicators
									.Where(si => si.Name == stockIndicatorValue)
									.FirstOrDefault();

								int count = query.Count(p =>
										p.StockIndicator == stockIndicatorValue &&
										p.Stock >= stockIndicatorInfo.MinQty &&
										p.Stock <= stockIndicatorInfo.MaxQty);

								return count > 0 ? new FilterInfo
								{
									Name = stockIndicatorValue,
									MinValue = stockIndicatorInfo?.MinQty,
									MaxValue = stockIndicatorInfo?.MaxQty,
									Count = count
								}: null;
							})
							.Where(filterInfo => filterInfo != null)
							.ToList()
						},
						new FilterGroup
						{
							GroupName = "Volume",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Volume",
													MinValue = Math.Floor(query.Min(p => p.Volume) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Volume) ?? 0)
												},
							}
						},
						new FilterGroup
						{
							GroupName = "Shape",
							Filter = distinctShape.Select(shapeValue =>
							{
								var shapeInfo = _context.Shapes
									.Where(si => si.Name == shapeValue.Name && si.Type == shapeValue.Type)
									.FirstOrDefault();

								int count = getCalculator.Count(p => p.ShapesID == shapeInfo.RefID);

								 return count > 0 ? new FilterInfo
								{
									Name = shapeValue.Name,
									ID = shapeInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						},
						new FilterGroup
						{
							GroupName = "Color",
							Filter = distinctColors.Select(colorValue =>
							{
								var colorInfo = _context.Colors
									.Where(si => si.Name == colorValue)
									.FirstOrDefault();

								int count = getCalculator.Count(p => p.ColorsID == colorInfo.RefID);

								 return count > 0 ? new FilterInfo
								{
									Name = colorInfo.Name,
									ID = colorInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						},
						new FilterGroup
						{
							GroupName = "Neck",
							Filter = distinctNeck.Select(neckValue =>
							{
								var neckInfo = _context.Necks
									.Where(si => si.Code == neckValue)
									.FirstOrDefault();

								int count = getCalculator.Count(p => p.NecksID == neckInfo.RefID);

								 return count > 0 ? new FilterInfo
								{
									Name = neckInfo.Code,
									ID = neckInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						},
						new FilterGroup
						{
							GroupName = "Diameter",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Diameter",
													MinValue = Math.Floor(query.Min(p => p.Length) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Length) ?? 0)
												},
							}
						},
						new FilterGroup
						{
							GroupName = "Width",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Width",
													MinValue = Math.Floor(query.Min(p => p.Width) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Width) ?? 0)
												},
							}
						},
						//new FilterGroup
						//{
						//	GroupName = "Height",
						//	Filter = new List<FilterInfo>
						//	{
						//		new FilterInfo {
						//							Name = "Height",
						//							MinValue = query.Min(p => p.Height),
						//							MaxValue = query.Max(p => p.Height)
						//						},
						//	}
						//},
						new FilterGroup
						{
							GroupName = "Price",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Price",
													MinValue = query.Min(p => p.Price),
													MaxValue = query.Max(p => p.Price)
												},
							}
						},
					};

				}
				else if (tipe == "closure")
				{
					filterGroups = new List<FilterGroup>
					{
                        //new FilterGroup
                        //{
                        //    GroupName = "Popular",
                        //    Filter = new List<FilterInfo>
                        //    {
                        //        new FilterInfo { Name = "New Product", Value = 1, Count = query.Count(p => (p.NewProd ?? 0) != 0) },
                        //        new FilterInfo { Name = "Favourite Product", Value = 1, Count = query.Count(p => (p.FavProd ?? 0) != 0) },
                        //    }.Where(filterInfo => filterInfo.Count > 0).ToList()
                        //},
                        new FilterGroup
						{
							GroupName = "Color",
							Filter = distinctColors.Select(colorValue =>
							{
								var colorInfo = _context.Colors
									.Where(si => si.Name == colorValue)
									.FirstOrDefault();

								int count = getClosures.Count(p => p.ColorsID == colorInfo.RefID );

								 return count > 0 ? new FilterInfo
								{
									Name = colorInfo.Name,
									ID = colorInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						},
						new FilterGroup
						{
							GroupName = "Neck",
							Filter = distinctNeck.Select(neckValue =>
							{
								var neckInfo = _context.Necks
									.FirstOrDefault(si => si.Code == neckValue);

								int count = getClosures.Count(p => p.NecksID == neckInfo.RefID  );

								 return count > 0 ? new FilterInfo
								{
									Name = neckInfo.Code,
									ID = neckInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						},
                        //new FilterGroup
                        //{
                        //    GroupName = "Body Dimension",
                        //    Filter = new List<FilterInfo>
                        //    {
                        //        new FilterInfo {
                        //                            Name = "Body Diameter",
                        //                            MinValue = Math.Floor(query.Min(p => p.Diameter) ?? 0),
                        //                            MaxValue = Math.Round(query.Max(p => p.Diameter) ?? 0)
                        //                        },
                        //        new FilterInfo {
                        //                            Name = "Height",
                        //                            MinValue = Math.Floor(query.Min(p => p.Height) ?? 0),
                        //                            MaxValue = Math.Round(query.Max(p => p.Height) ?? 0)
                        //                        },
                        //    }
                        //},
						new FilterGroup
						{
							GroupName = "Body Diameter",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Body Diameter",
													MinValue = Math.Floor(query.Min(p => p.Diameter) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Diameter) ?? 0)
												},
							}
						},
						new FilterGroup
						{
							GroupName = "Height",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Height",
													MinValue = Math.Floor(query.Min(p => p.Height) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Height) ?? 0)
												},
							}
						},
						new FilterGroup
						{
							GroupName = "Price",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Price",
													MinValue = query.Min(p => p.Price),
													MaxValue = query.Max(p => p.Price)
												},
							}
						},
					};
				}
				else if (tipe == "tray" || tipe == "lid" || tipe == "cup")
				{

					filterGroups = new List<FilterGroup>
					{
						new FilterGroup
						{
							GroupName = "Popular",
							Filter = new List<FilterInfo>
							{
								new FilterInfo { Name = "New Product", Value = 1, Count = query.Count(p => (p.NewProd ?? 0) != 0) },
								new FilterInfo { Name = "Favourite Product", Value = 1, Count = query.Count(p => (p.FavProd ?? 0) != 0) },
							}.Where(filterInfo => filterInfo.Count > 0).ToList()
						},
                        //new FilterGroup
                        //{
                        //    GroupName = "Body Dimension",
                        //    Filter = new List<FilterInfo>
                        //    {
                        //        new FilterInfo {
                        //                            Name = "Body Diameter",
                        //                            MinValue = Math.Floor(query.Min(p => p.Length) ?? 0),
                        //                            MaxValue = Math.Round(query.Max(p => p.Length) ?? 0)
                        //                        },
                        //        new FilterInfo {
                        //                            Name = "Height",
                        //                            MinValue = Math.Floor(query.Min(p => p.Height) ?? 0),
                        //                            MaxValue = Math.Round(query.Max(p => p.Height) ?? 0)
                        //                        },
                        //    }
                        //},
                        new FilterGroup
						{
							GroupName = "Body Diameter",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Body Diameter",
													MinValue = Math.Floor(query.Min(p => p.Length) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Length) ?? 0)
												},
							}
						},
						new FilterGroup
						{
							GroupName = "Height",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Height",
													MinValue = Math.Floor(query.Min(p => p.Height) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Height) ?? 0)
												},
							}
						},
						new FilterGroup
						{
							GroupName = "Rim",
							Filter = distinctRim.Select(RimValue =>
							{
								var RimInfo = _context.Rims
									.FirstOrDefault(si => si.Name == RimValue);
								int count = 0;
								if (tipe == "tray" ) count = getTray.Count(p => p.Rim == RimInfo.Name );
								if (tipe == "lid" ) count = getLid.Count(p => p.Rim == RimInfo.Name );

								 return count > 0 ? new FilterInfo
								{
									Name = RimInfo.Name,
									ID = RimInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						},
						new FilterGroup
						{
							GroupName = "Price",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Price",
													MinValue = query.Min(p => p.Price),
													MaxValue = query.Max(p => p.Price)
												},
							}
						},
						new FilterGroup
						{
							GroupName = "Material Type",
							Filter = new List<FilterInfo>
							{
								new FilterInfo { Name = "PET"},
								new FilterInfo { Name = "PP" },
							}
						},
					};

					if (tipe == "cup")
					{
						filterGroups.Add(new FilterGroup
						{
							GroupName = "Volume",
							Filter = new List<FilterInfo>{
											new FilterInfo {
												Name = "Volume",
												MinValue = Math.Floor(query.Min(p => p.Volume) ?? 0),
												MaxValue = Math.Round(query.Max(p => p.Volume) ?? 0)
											},
										}
						});

						filterGroups.Add(new FilterGroup
						{
							GroupName = "Shape",
							Filter = distinctShape.Select(shapeValue =>
							{
								var shapeInfo = _context.Shapes
									.FirstOrDefault(si => si.Name == shapeValue.Name && si.Type == shapeValue.Type);

								int count = getTray.Count(p => p.ShapesID == shapeInfo.RefID);

								return count > 0 ? new FilterInfo
								{
									Name = shapeValue.Name,
									ID = shapeInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						});
					}

					if (tipe == "tray")
					{
						filterGroups.Add(new FilterGroup
						{
							GroupName = "Shape",
							Filter = distinctShape.Select(shapeValue =>
							{
								var shapeInfo = _context.Shapes
									.FirstOrDefault(si => si.Name == shapeValue.Name && si.Type == shapeValue.Type);

								int count = getTray.Count(p => p.ShapesID == shapeInfo.RefID);

								return count > 0 ? new FilterInfo
								{
									Name = shapeValue.Name,
									ID = shapeInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						});
					}

					if (tipe == "lid")
					{
						filterGroups.Add(new FilterGroup
						{
							GroupName = "Lid Type",
							Filter = new List<FilterInfo>
							{
								new FilterInfo { Name = "Cup",Count = getLid.Count(x => x.CategoriesID == 5)},
								new FilterInfo { Name = "Tray",Count = getLid.Count(x => x.CategoriesID != 5) },
							}
						});
					}

				}
				var filters = filterGroups.ToList();

				// Searching
				if (!string.IsNullOrEmpty(search))
					query = query.Where(x => x.RefID.ToString().Contains(search)
						|| x.Name.Contains(search)
						);

				// Get Total Before Limit and Page
				total = await query.CountAsync();

				// Adjust limit if it exceeds total count
				if (limit != 0 && limit > total)
				{
					limit = total; // Set limit to total count
				}

				// Calculate the maximum page number based on the limit
				int maxPage = (int)Math.Ceiling((double)total / limit);

				// Adjust the page number if it exceeds the maximum page number
				if (maxPage > 0 && page >= maxPage)
				{
					page = maxPage - 1;
				}

				// order product list bottles and thermos
				if ((CategoriesID.ToLower() == "bottles" && !sub.Contains(25)) || (CategoriesID.ToLower() == "thermos" && (sub.Contains(4) || sub.Contains(5))) && isSort == true)
				{
					if (sort.ToLower() == "volume" && type.ToLower() == "asc")
					{
						query = query.OrderBy(p => p.Volume);
					}
					else if (sort.ToLower() == "volume" && type.ToLower() == "desc")
					{
						query = query.OrderByDescending(p => p.Volume);
					}
					else if (sort.ToLower() == "price" && type.ToLower() == "asc")
					{
						query = query.OrderBy(p => p.Price);
					}
					else if (sort.ToLower() == "price" && type.ToLower() == "desc")
					{
						query = query.OrderByDescending(p => p.Price);
					}
					else
					{
						query = query.OrderByDescending(p => p.NewProd)
							   .ThenByDescending(p => p.FavProd)
							   .ThenBy(p => p.Volume);
					}

				}
				if ((CategoriesID.ToLower() == "bottles" && sub.Contains(25)) && isSort == true)
				{
					if (sort.ToLower() == "price" && type.ToLower() == "asc")
					{
						query = query.OrderBy(p => p.Price);
					}
					else if (sort.ToLower() == "price" && type.ToLower() == "desc")
					{
						query = query.OrderByDescending(p => p.Price);
					}
					else
					{
						query = query.OrderBy(p => p.Name);
					}
				}

				// order product list thermos
				if ((CategoriesID.ToLower() == "thermos" && sub.Contains(6)) && isSort == true)
				{
					if (sort.ToLower() == "price" && type.ToLower() == "asc")
					{
						query = query.OrderBy(p => p.Price);
					}
					else if (sort.ToLower() == "price" && type.ToLower() == "desc")
					{
						query = query.OrderByDescending(p => p.Price);
					}
					else
					{
						query = query.OrderByDescending(p => p.NewProd)
								  .ThenByDescending(p => p.FavProd)
								  .ThenBy(p => p.RefID);
					}
				}

				// Set Limit and Page
				if (limit != 0)
					query = query.Skip(page * limit).Take(limit);

				// Get Data
				var data = await query.ToListAsync();

				if (data.Count <= 0 && page > 0)
				{
					page = 0;
					return await GetListAsync(limit, page, total, search, sort, type, CategoriesID, sub, filterNew, filterFavourite, filterStockIndicatorMin, filterStockIndicatorMax, filterColor, filterVolumeMin, filterVolumeMax, filterShape, filterNeck, filterRim, filterPriceMin, filterPriceMax, filterDiameterMin, filterDiameterMax, filterWidthMin, filterWidthMax, filterHeightMin, filterHeightMax, materialType, lidType, userid);
				}

				return new ListResponseProduct<Product>(data, total, page, filters);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				throw;
			}
		}

		public async Task<ListResponseProduct<Product>> GetListNewAsync(int limit, int total, int page, string searchKey, string searchProduct, string CategoriesID, List<long> sub, string sort, string type, int filterNew, int filterFavourite, decimal filterStockIndicatorMin, decimal filterStockIndicatorMax, List<long> filterColor, decimal filterVolumeMin, decimal filterVolumeMax, List<long> filterShape, List<long> filterNeck, List<long> filterRim, decimal filterPriceMin, decimal filterPriceMax, decimal filterDiameterMin, decimal filterDiameterMax, decimal? filterWidthMin, decimal filterWidthMax, decimal filterHeightMin, decimal filterHeightMax, List<string> materialType, string lidType, int? userid, List<decimal> filterVolumeCup)
		{
			//IQueryable<Product> query = null;
			IEnumerable<Product> filteredProducts = null;
			IEnumerable<Product> getCalculator = null;
			IEnumerable<Product> getClosures = null;
			IEnumerable<Product> getTray = null;
			IEnumerable<Product> getCup = null;
			IEnumerable<Product> getLid = null;
			List<Product> products = new List<Product>();
			var tipe = "";
			var isSort = (sort != "" || type != "");
			var isFilter = (filterNew != 0 || filterFavourite != 0 || filterStockIndicatorMin != 0 ||
				filterStockIndicatorMax != 0 || filterColor.Any() || filterVolumeMin != 0 ||
				filterVolumeMax != 0 || filterShape.Any() || filterNeck.Any() ||
				filterRim.Any() || filterPriceMin != 0 || filterPriceMax != 0 ||
				filterDiameterMin != 0 || filterDiameterMax != 0 || filterWidthMin != null ||
				filterWidthMax != 0 || filterHeightMin != 0 || filterHeightMax != 0 ||
				materialType.Any() || lidType != "" || filterVolumeCup.Any());


			try
			{
				//_context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
				if (sub.Any())
				{
					if (CategoriesID.ToLower() == "bottles" && !sub.Contains(25))
					{
						getCalculator = from product_detail in _context.ProductDetails2
										where product_detail.IsDeleted == false
											  && sub.Contains((long)product_detail.CategoriesID)
											  && product_detail.Type == "bottle"
										let qtyCart = (from c in _context.Carts
													   join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
													   from carts_detail in carts.DefaultIfEmpty()
													   where c.CustomersID == userid
														   && carts_detail.ObjectID == product_detail.RefID
													   && carts_detail.Type == "bottle"
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
											LidsID = product_detail.LidsID,
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
											Type = product_detail.Type
										};
						filteredProducts = getCalculator.AsQueryable();
						tipe = "bottle";
						products = filteredProducts.ToList();


					}
					else if (CategoriesID.ToLower() == "bottles" && sub.Contains(25))
					{

						getClosures = await (from product_detail in _context.ProductDetails2
											 where product_detail.IsDeleted == false
												   && sub.Contains((long)product_detail.CategoriesID)
												   && product_detail.Type == "closure"
											 let qtyCart = (from c in _context.Carts
															join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
															from carts_detail in carts.DefaultIfEmpty()
															where c.CustomersID == userid
																&& carts_detail.ObjectID == product_detail.RefID
															&& carts_detail.Type == "closures"
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
												 LidsID = product_detail.LidsID,
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
												 QtyCart = 0,
												 Type = product_detail.Type
											 }).ToListAsync();
						filteredProducts = getClosures.AsQueryable();
						tipe = "closure";
						products = filteredProducts.ToList();
					}
					else if (CategoriesID.ToLower() == "thermos" && (sub.Contains(6) && sub.Contains(5) && sub.Contains(4)))
					{

						var getData = await (from product_detail in _context.ProductDetails2
											 where product_detail.IsDeleted == false
												   && product_detail.Type != "bottle"
												   && product_detail.Type != "closure"
											 let thermoCategoriesID = _context.ProductDetails2
																.Where(p => product_detail.RimsID == p.RimsID && (p.Type == "tray" || p.Type == "cup"))
																.FirstOrDefault()
											 let qtyCart = (from c in _context.Carts
															join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
															from carts_detail in carts.DefaultIfEmpty()
															where c.CustomersID == userid
																&& carts_detail.ObjectID == product_detail.RefID
															&& carts_detail.Type == "lid"
															&& carts_detail.IsDeleted == false
															select carts_detail.Qty
															).Sum()
											 select new Product
											 {
												 ID = (long)product_detail.OriginID,
												 RefID = product_detail.RefID,
												 Name = product_detail.Name,
												 Image = product_detail.Image,
												 RealImage = product_detail.RealImage,
												 Weight = product_detail.Weight,
												 Price = product_detail.Price,
												 CategoriesID = product_detail.CategoriesID,
												 CategoryName = product_detail.CategoryName,
												 Stock = product_detail.Stock,
												 NewProd = product_detail.NewProd,
												 FavProd = product_detail.FavProd,
												 ClosuresID = product_detail.ClosuresID,
												 LidsID = product_detail.LidsID,
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
												 QtyCart = product_detail.Type == "lid" ? 0 : (qtyCart == null ? null : Convert.ToInt64(qtyCart)),
												 Type = product_detail.Type
											 }).ToListAsync();
						filteredProducts = getData.AsQueryable();
						getLid = getData.Where(x => x.Type == "lid");
						getTray = getData.Where(x => x.Type != "lid");
						tipe = System.String.Join(" ", filteredProducts.Select(x => x.Type).Distinct().ToList());
						products = filteredProducts.ToList();
					}
					else if (CategoriesID.ToLower() == "thermos" && !sub.Contains(6))
					{

						var CategoriesIDIdsTray = new List<long> { 4, 7 };
						var CategoriesIDIdsCup = new List<long> { 5 };

						getTray = await (from product_detail in _context.ProductDetails2
										 where product_detail.IsDeleted == false
											   && (product_detail.Type == "cup" || product_detail.Type == "tray")
												&& ((sub.Contains(4) && sub.Contains(5)) ? (CategoriesIDIdsTray.Contains((long)product_detail.CategoriesID) || CategoriesIDIdsCup.Contains((long)product_detail.CategoriesID)) :
												   sub.Contains(4) ? CategoriesIDIdsTray.Contains((long)product_detail.CategoriesID) :
												   sub.Contains(5) ? CategoriesIDIdsCup.Contains((long)product_detail.CategoriesID) :
												   false)
										 let qtyCart = (from c in _context.Carts
														join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
														from carts_detail in carts.DefaultIfEmpty()
														where c.CustomersID == userid
															&& carts_detail.ObjectID == product_detail.RefID
														&& (carts_detail.Type == "tray" || carts_detail.Type == "cup")
														&& carts_detail.IsDeleted == false
														select carts_detail.Qty
														).Sum()
										 select new Product
										 {
											 ID = (long)product_detail.OriginID,
											 RefID = product_detail.RefID,
											 Name = product_detail.Name,
											 Image = product_detail.Image,
											 RealImage = product_detail.RealImage,
											 Weight = product_detail.Weight,
											 Price = product_detail.Price,
											 CategoriesID = product_detail.CategoriesID,
											 CategoryName = product_detail.CategoryName,
											 Stock = product_detail.Stock,
											 NewProd = product_detail.NewProd,
											 FavProd = product_detail.FavProd,
											 ClosuresID = product_detail.ClosuresID,
											 LidsID = product_detail.LidsID,
											 Volume = product_detail.Volume,
											 Height = product_detail.Height,
											 Length = product_detail.Length,
											 ColorsID = product_detail.ColorsID,
											 ShapesID = product_detail.ShapesID,
											 NecksID = product_detail.NecksID,
											 Width = product_detail.Width,
											 StockIndicator = product_detail.StockIndicator,
											 RimsID = product_detail.RimsID,
											 PlasticType = product_detail.PlasticType,
											 QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											 Type = sub.Contains(5) && !sub.Contains(4) ? "cup" :
													sub.Contains(4) && !sub.Contains(5) ? "tray" :
													(sub.Contains(4) && sub.Contains(5)) ? "tray" :
													null
										 }).ToListAsync();

						filteredProducts = getTray.AsQueryable();
						tipe = getTray.FirstOrDefault().Type;
						products = filteredProducts.ToList();
					}
					else if (CategoriesID.ToLower() == "thermos" && sub.Contains(6))
					{

						getLid = await (from product_detail in _context.ProductDetails2
										where product_detail.IsDeleted == false
											  && (product_detail.Type == "lid")
										let thermoCategoriesID = _context.ProductDetails2
														   .Where(p => product_detail.RimsID == p.RimsID && (p.Type == "tray" || p.Type == "cup"))
														   .FirstOrDefault()
										let qtyCart = 0
										select new Product
										{
											ID = (long)product_detail.OriginID,
											RefID = product_detail.RefID,
											Name = product_detail.Name,
											Image = product_detail.Image,
											RealImage = product_detail.RealImage,
											Weight = product_detail.Weight,
											Price = product_detail.Price,
											CategoriesID = thermoCategoriesID.CategoriesID,
											CategoryName = product_detail.CategoryName,
											Stock = product_detail.Stock,
											NewProd = product_detail.NewProd,
											FavProd = product_detail.FavProd,
											ClosuresID = product_detail.ClosuresID,
											LidsID = product_detail.LidsID,
											Volume = product_detail.Volume,
											Height = product_detail.Height,
											Length = product_detail.Length,
											ColorsID = product_detail.ColorsID,
											RimsID = product_detail.RimsID,
											ShapesID = product_detail.ShapesID,
											NecksID = product_detail.NecksID,
											Width = product_detail.Width,
											StockIndicator = product_detail.StockIndicator,
											PlasticType = product_detail.PlasticType,
											QtyCart = Convert.ToInt64(qtyCart),
											Type = "lid"
										}).ToListAsync();

						filteredProducts = getLid.AsQueryable();
						tipe = "lid";
						products = filteredProducts.ToList();
					}
					else
					{
						return null;
					}
				}
				else
				{
					var (a, b, c) = await _svc.GetSearch(searchKey, 0, 0);

					foreach (DataRow row in a)
					{
						Product p = new Product
						{
							ID = row.IsNull("OriginID") ? 0 : Convert.ToInt64(row["OriginID"]),
							RefID = row.IsNull("RefID") ? 0 : Convert.ToInt64(row["RefID"]),
							Name = row.IsNull("Name") ? string.Empty : Convert.ToString(row["Name"]),
							Image = row.IsNull("Image") ? string.Empty : Convert.ToString(row["Image"]),
							Weight = row.IsNull("Weight") ? 0 : Convert.ToDecimal(row["Weight"]),
							Width = row.IsNull("Width") ? 0 : Convert.ToDecimal(row["Width"]),
							Price = row.IsNull("Price") ? 0 : Convert.ToDecimal(row["Price"]),
							CategoriesID = row.IsNull("CategoriesID") ? 0 : Convert.ToInt64(row["CategoriesID"]),
							CategoryName = row.IsNull("CategoryName") ? string.Empty : Convert.ToString(row["CategoryName"]),
							Stock = row.IsNull("Stock") ? 0 : Convert.ToDecimal(row["Stock"]),
							NewProd = row.IsNull("NewProd") ? 0 : Convert.ToInt64(row["NewProd"]),
							FavProd = row.IsNull("FavProd") ? 0 : Convert.ToInt64(row["FavProd"]),
							ClosuresID = row.IsNull("ClosuresID") ? 0 : Convert.ToInt64(row["ClosuresID"]),
							LidsID = row.IsNull("LidsID") ? 0 : Convert.ToInt64(row["LidsID"]),
							Volume = row.IsNull("Volume") ? 0 : Convert.ToDecimal(row["Volume"]),
							Code = row.IsNull("WmsCode") ? string.Empty : Convert.ToString(row["WmsCode"]),
							QtyPack = row.IsNull("QtyPack") ? 0 : Convert.ToDecimal(row["QtyPack"]),
							TotalViews = row.IsNull("TotalViews") ? 0 : Convert.ToDecimal(row["TotalViews"]),
							TotalShared = row.IsNull("TotalShared") ? 0 : Convert.ToDecimal(row["TotalShared"]),
							NewProdDate = row.IsNull("NewProdDate") ? null : Convert.ToDateTime(row["NewProdDate"]),
							Height = row.IsNull("Height") ? 0 : Convert.ToDecimal(row["Height"]),
							Length = row.IsNull("Length") ? 0 : Convert.ToDecimal(row["Length"]),
							ColorsID = row.IsNull("ColorsID") ? default(long) : Convert.ToInt64(row["ColorsID"]),
							ShapesID = row.IsNull("ShapesID") ? default(long) : Convert.ToInt64(row["ShapesID"]),
							NecksID = row.IsNull("NecksID") ? default(long) : Convert.ToInt64(row["NecksID"]),
							RimsID = row.IsNull("RimsID") ? default(long) : Convert.ToInt64(row["RimsID"]),
							Diameter = row.IsNull("Diameter") ? default(decimal) : Convert.ToDecimal(row["Diameter"]),
							RealImage = row.IsNull("RealImage") ? string.Empty : Convert.ToString(row["RealImage"]),

							StockIndicator = row.IsNull("StockIndicator") ? string.Empty : Convert.ToString(row["StockIndicator"]),

							PlasticType = row.IsNull("PlasticType") ? string.Empty : Convert.ToString(row["PlasticType"]),

							Type = row.IsNull("Type") ? string.Empty : Convert.ToString(row["Type"]),
							CountColor = Convert.ToInt32(await _svc.GetCountColor(Convert.ToString(row["Type"]), Convert.ToString(row["Name"])))
						};
						if (p.Type.ToLower().Equals("tray") || p.Type.ToLower().Equals("cup") || p.Type.ToLower().Equals("bottle"))
						{
							p.QtyCart = Convert.ToInt64((from cart in _context.Carts
														 join cartdetail in _context.CartDetails on cart.ID equals cartdetail.CartsID into carts
														 from carts_detail in carts.DefaultIfEmpty()
														 where cart.CustomersID == userid
															 && carts_detail.ObjectID == p.RefID
														 && carts_detail.Type == (p.Type == "closure" ? "closures" : p.Type)
														 && carts_detail.IsDeleted == false
														 select carts_detail.Qty
									).Sum());
						}
						products.Add(p);
					}

					filteredProducts = products;

					var dataRaw = _svc.insertProductDetail("", "");
					List<Product> RawProducts = new List<Product>();
					foreach (DataRow row in dataRaw.Rows)
					{
						Product p = new Product
						{
							ID = row.IsNull("OriginID") ? 0 : Convert.ToInt64(row["OriginID"]),
							RefID = row.IsNull("RefID") ? 0 : Convert.ToInt64(row["RefID"]),
							Name = row.IsNull("Name") ? string.Empty : Convert.ToString(row["Name"]),
							Image = row.IsNull("Image") ? string.Empty : Convert.ToString(row["Image"]),
							Weight = row.IsNull("Weight") ? 0 : Convert.ToDecimal(row["Weight"]),
							Width = row.IsNull("Width") ? 0 : Convert.ToDecimal(row["Width"]),
							Price = row.IsNull("Price") ? 0 : Convert.ToDecimal(row["Price"]),
							CategoriesID = row.IsNull("CategoriesID") ? 0 : Convert.ToInt64(row["CategoriesID"]),
							CategoryName = row.IsNull("CategoryName") ? string.Empty : Convert.ToString(row["CategoryName"]),
							Stock = row.IsNull("Stock") ? 0 : Convert.ToDecimal(row["Stock"]),
							NewProd = row.IsNull("NewProd") ? 0 : Convert.ToInt64(row["NewProd"]),
							FavProd = row.IsNull("FavProd") ? 0 : Convert.ToInt64(row["FavProd"]),
							ClosuresID = row.IsNull("ClosuresID") ? 0 : Convert.ToInt64(row["ClosuresID"]),
							LidsID = row.IsNull("LidsID") ? 0 : Convert.ToInt64(row["LidsID"]),
							Volume = row.IsNull("Volume") ? 0 : Convert.ToDecimal(row["Volume"]),
							Code = row.IsNull("WmsCode") ? string.Empty : Convert.ToString(row["WmsCode"]),
							QtyPack = row.IsNull("QtyPack") ? 0 : Convert.ToDecimal(row["QtyPack"]),
							TotalViews = row.IsNull("TotalViews") ? 0 : Convert.ToDecimal(row["TotalViews"]),
							TotalShared = row.IsNull("TotalShared") ? 0 : Convert.ToDecimal(row["TotalShared"]),
							NewProdDate = row.IsNull("NewProdDate") ? null : Convert.ToDateTime(row["NewProdDate"]),
							Height = row.IsNull("Height") ? 0 : Convert.ToDecimal(row["Height"]),
							Length = row.IsNull("Length") ? 0 : Convert.ToDecimal(row["Length"]),
							ColorsID = row.IsNull("ColorsID") ? default(long) : Convert.ToInt64(row["ColorsID"]),
							ShapesID = row.IsNull("ShapesID") ? default(long) : Convert.ToInt64(row["ShapesID"]),
							NecksID = row.IsNull("NecksID") ? default(long) : Convert.ToInt64(row["NecksID"]),
							RimsID = row.IsNull("RimsID") ? default(long) : Convert.ToInt64(row["RimsID"]),
							Diameter = row.IsNull("Diameter") ? default(decimal) : Convert.ToDecimal(row["Diameter"]),
							RealImage = row.IsNull("RealImage") ? string.Empty : Convert.ToString(row["RealImage"]),

							StockIndicator = row.IsNull("StockIndicator") ? string.Empty : Convert.ToString(row["StockIndicator"]),

							PlasticType = row.IsNull("PlasticType") ? string.Empty : Convert.ToString(row["PlasticType"]),

							Type = row.IsNull("Type") ? string.Empty : Convert.ToString(row["Type"]),
							CountColor = Convert.ToInt32(await _svc.GetCountColor(Convert.ToString(row["Type"]), Convert.ToString(row["Name"])))
						};
						if (p.Type.ToLower().Equals("tray") || p.Type.ToLower().Equals("cup") || p.Type.ToLower().Equals("bottle"))
						{
							p.QtyCart = Convert.ToInt64((from cart in _context.Carts
														 join cartdetail in _context.CartDetails on cart.ID equals cartdetail.CartsID into carts
														 from carts_detail in carts.DefaultIfEmpty()
														 where cart.CustomersID == userid
															 && carts_detail.ObjectID == p.RefID
														 && carts_detail.Type == (p.Type == "closure" ? "closures" : p.Type)
														 && carts_detail.IsDeleted == false
														 select carts_detail.Qty
											).Sum());
						}
						RawProducts.Add(p);
					}
					IEnumerable<Product> filteredRawProducts = RawProducts;


					if (products.Any(p => p.Type == "bottle")) getCalculator = RawProducts.Where(p => p.Type == "bottle");
					if (products.Any(p => p.Type == "closure")) getClosures = RawProducts.Where(p => p.Type == "closure");
					if (products.Any(p => p.Type == "cup")) getCup = RawProducts.Where(p => p.Type == "cup");
					if (products.Any(p => p.Type == "tray")) getTray = RawProducts.Where(p => p.Type == "tray");
					if (products.Any(p => p.Type == "lid")) getLid = RawProducts.Where(p => p.Type == "lid" || p.Type == "cup" || p.Type == "tray");
				}



				#region Filter Section
				// filter run
				if (products.Any(p => p.Type == "bottle") && isFilter == true)
				{
					// Apply combined filter for new or favourite products
					if (filterNew != 0 || filterFavourite != 0)
					{
						filteredProducts = filteredProducts.Where(p =>
							(filterNew != 0 && p.NewProd != 0) || (filterFavourite != 0 && p.FavProd != 0)
						);
					}

					//filter stock indicator
					if (filterStockIndicatorMin != 0 && filterStockIndicatorMax != 0)
					{
						filteredProducts = filteredProducts.Where(p => p.Stock >= filterStockIndicatorMin && p.Stock <= filterStockIndicatorMax);
					}

					//filter color
					if (filterColor != null && filterColor.Any())
					{
						filteredProducts = filteredProducts.Where(p => filterColor.Contains(p.ColorsID.GetValueOrDefault()));
					}
					//else
					//{
					//	query = query.Where(p => p.ColorsID == 14);
					//}

					//filter volume
					if (filterVolumeMin != 0 && filterVolumeMax != 0)
					{
						filteredProducts = filteredProducts.Where(p => p.Volume >= Math.Floor(filterVolumeMin) && p.Volume <= Math.Round(filterVolumeMax));
					}

					//filter shpae
					if (filterShape != null && filterShape.Any())
					{
						filteredProducts = filteredProducts.Where(p => filterShape.Contains(p.ShapesID.GetValueOrDefault()));
					}

					//filter neck
					if (filterNeck != null && filterNeck.Any())
					{
						filteredProducts = filteredProducts.Where(p => filterNeck.Contains(p.NecksID.GetValueOrDefault()));
					}

					//filter price
					if (filterPriceMin != 0 && filterPriceMax != 0)
					{
						filteredProducts = filteredProducts.Where(p => p.Price >= Math.Floor(filterPriceMin) && p.Price <= Math.Round(filterPriceMax));
					}

					//filter diameter
					if (filterDiameterMin != 0 && filterDiameterMax != 0)
					{
						filteredProducts = filteredProducts.Where(p => p.Length >= Math.Floor(filterDiameterMin) && p.Length <= Math.Round(filterDiameterMax));
					}

					//filter width
					if (filterWidthMin != null && filterWidthMax != 0)
					{
						filteredProducts = filteredProducts.Where(p => p.Width >= Math.Floor(filterWidthMin.GetValueOrDefault()) && p.Width <= Math.Round(filterWidthMax));
					}

					//filter height
					if (filterHeightMin != 0 && filterHeightMax != 0)
					{
						filteredProducts = filteredProducts.Where(p => p.Height >= Math.Floor(filterHeightMin) && p.Height <= Math.Round(filterHeightMax));
					}
				}
				if (products.Any(p => p.Type == "closure") && isFilter == true)
				{
					//filter color
					if (filterColor != null && filterColor.Any())
					{
						filteredProducts = filteredProducts.Where(p => filterColor.Contains(p.ColorsID.GetValueOrDefault()));
					}
					//else
					//{
					//    query = query.Where(p => p.ColorsID == 14);
					//}

					//filter neck
					if (filterNeck != null && filterNeck.Any())
					{
						filteredProducts = filteredProducts.Where(p => filterNeck.Contains(p.NecksID.GetValueOrDefault()));
					}

					//filter price
					if (filterPriceMin != 0 && filterPriceMax != 0)
					{
						filteredProducts = filteredProducts.Where(p => p.Price >= Math.Floor(filterPriceMin) && p.Price <= Math.Round(filterPriceMax));
					}

					//filter diameter
					if (filterDiameterMin != 0 && filterDiameterMax != 0)
					{
						filteredProducts = filteredProducts.Where(p => p.Length >= Math.Floor(filterDiameterMin) && p.Length <= Math.Round(filterDiameterMax));
					}

					//filter height
					if (filterHeightMin != 0 && filterHeightMax != 0)
					{
						filteredProducts = filteredProducts.Where(p => p.Height >= Math.Floor(filterHeightMin) && p.Height <= Math.Round(filterHeightMax));
					}
				}
				if ((products.Any(p => p.Type == "cup") || products.Any(p => p.Type == "lid") || products.Any(p => p.Type == "tray")) && isFilter == true)
				{
					// Apply combined filter for new or favourite products
					if (filterNew != 0 || filterFavourite != 0)
					{
						filteredProducts = filteredProducts.Where(p =>
							(filterNew != 0 && p.NewProd != 0) || (filterFavourite != 0 && p.FavProd != 0)
						);
					}

					//filter rim
					if (filterRim != null && filterRim.Any())
					{
						filteredProducts = filteredProducts.Where(p => filterRim.Contains(p.RimsID.GetValueOrDefault()));
					}

					//filter price
					if (filterPriceMin != 0 && filterPriceMax != 0)
					{
						filteredProducts = filteredProducts.Where(p => p.Price >= Math.Floor(filterPriceMin) && p.Price <= Math.Round(filterPriceMax));
					}

					//filter diameter
					if (filterDiameterMin != 0 && filterDiameterMax != 0)
					{
						filteredProducts = filteredProducts.Where(p => p.Length >= Math.Floor(filterDiameterMin) && p.Length <= Math.Round(filterDiameterMax));
					}

					//filter height
					if (filterHeightMin != 0 && filterHeightMax != 0)
					{
						filteredProducts = filteredProducts.Where(p => p.Height >= Math.Floor(filterHeightMin) && p.Height <= Math.Round(filterHeightMax));
					}

					//filter volume
					if (products.Any(p => p.Type == "cup"))
					{
						if (filterVolumeCup != null && filterVolumeCup.Any())
						{
							filteredProducts = filteredProducts.Where(p => filterVolumeCup.Contains(Convert.ToInt64(p.Volume.GetValueOrDefault() / 30)));
						}

					}
					else
					{
						if (filterVolumeMin != 0 && filterVolumeMax != 0)
						{
							filteredProducts = filteredProducts.Where(p => p.Volume >= Math.Floor(filterVolumeMin) && p.Volume <= Math.Round(filterVolumeMax));
						}
					}

					//filter shpae
					if (filterShape != null && filterShape.Any())
					{
						filteredProducts = filteredProducts.Where(p => filterShape.Contains(p.ShapesID.GetValueOrDefault()));
					}

					//filter material type
					if (materialType != null && materialType.Any())
					{
						//filteredProducts = filteredProducts.Where(p => p.PlasticType == materialType);
						filteredProducts = filteredProducts.Where(p => materialType.Contains(p.PlasticType.ToUpper()));
					}

					//filter tray or cup for lid
					if (lidType != "" && products.Any(p => p.Type == "lid"))
					{
						if (lidType.ToLower() == "cup") filteredProducts = filteredProducts.Where(p => p.CategoriesID == 5);
						if (lidType.ToLower() == "tray") filteredProducts = filteredProducts.Where(p => p.CategoriesID != 5 && p.CategoriesID != 6);
					}
				}
				#endregion

				#region Stock Indicator
				////cek stock indicator
				//var distinctStockIndicators = _context.StockIndicators
				//	.Where(si => si.IsDeleted == false)
				//	.Select(si => si.Name)
				//	.Distinct()
				//	.ToList();

				//// cek shape
				//var distinctShape = _context.Shapes
				//				.Where(si => si.IsDeleted == false)
				//				.Select(si => new { si.Name, si.Type })
				//				.Distinct()
				//				.ToList();

				////cek color
				//var distinctColors = _context.Colors
				//	.Where(si => si.IsDeleted == false)
				//	.Select(si => si.Name)
				//	.Distinct()
				//	.ToList();

				////cek color
				//var distinctNeck = _context.Necks
				//	.Where(si => si.IsDeleted == false)
				//	.Select(si => si.Code)
				//	.Distinct()
				//	.ToList();

				////cek rim
				//var distinctRim = _context.Rims
				//	.Where(si => si.IsDeleted == false)
				//	.Select(si => si.Name)
				//	.Distinct()
				//	.ToList();

				//List<FilterGroup> filterGroups = new List<FilterGroup>();

				//if (products.Any(p => p.Type == "bottle"))
				//{

				//	filterGroups = new List<FilterGroup>
				//							 {
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Popular",
				//									 Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo { Name = "New Product", Value = 1, Count = getCalculator.Count(p => (p.NewProd ?? 0) != 0) },
				//										 new FilterInfo { Name = "Favourite Product", Value = 1, Count = getCalculator.Count(p => (p.FavProd ?? 0) != 0) },
				//									 }.Where(filterInfo => filterInfo.Count > 0).ToList()
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Stock Indicator",
				//									 Filter = distinctStockIndicators.Select(stockIndicatorValue =>
				//									 {
				//										 var stockIndicatorInfo = _context.StockIndicators
				//											 .Where(si => si.Name == stockIndicatorValue)
				//											 .FirstOrDefault();

				//										 int count = getCalculator.Count(p =>
				//												 p.StockIndicator == stockIndicatorValue &&
				//												 p.Stock >= stockIndicatorInfo.MinQty &&
				//												 p.Stock <= stockIndicatorInfo.MaxQty);

				//										 return count > 0 ? new FilterInfo
				//										 {
				//											 Name = stockIndicatorValue,
				//											 MinValue = stockIndicatorInfo?.MinQty,
				//											 MaxValue = stockIndicatorInfo?.MaxQty,
				//											 Count = count
				//										 }: null;
				//									 })
				//									 .Where(filterInfo => filterInfo != null)
				//									 .ToList()
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Volume",
				//									 Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo {
				//															 Name = "Volume",
				//															 MinValue = Math.Floor(getCalculator.Min(p => p.Volume) ?? 0),
				//															 MaxValue = Math.Round(getCalculator.Max(p => p.Volume) ?? 0)
				//														 },
				//									 }
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Shape",
				//									 Filter = distinctShape.Select(shapeValue =>
				//									 {
				//										 var shapeInfo = _context.Shapes
				//											 .Where(si => si.Name == shapeValue.Name && si.Type == shapeValue.Type)
				//											 .FirstOrDefault();

				//										 int count = getCalculator.Count(p => p.ShapesID == shapeInfo.RefID );

				//										  return count > 0 ? new FilterInfo
				//										 {
				//											 Name = shapeValue.Name,
				//											 ID = shapeInfo?.RefID,
				//											 Count = count
				//										 } : null;
				//									 })
				//									  .Where(filterInfo => filterInfo != null)
				//									 .ToList()
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Color",
				//									 Filter = distinctColors.Select(colorValue =>
				//									 {
				//										 var colorInfo = _context.Colors
				//											 .Where(si => si.Name == colorValue)
				//											 .FirstOrDefault();

				//										 int count = getCalculator.Count(p => p.ColorsID == colorInfo.RefID );

				//										  return count > 0 ? new FilterInfo
				//										 {
				//											 Name = colorInfo.Name,
				//											 ID = colorInfo?.RefID,
				//											 Count = count
				//										 } : null;
				//									 })
				//									  .Where(filterInfo => filterInfo != null)
				//									 .ToList()
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Neck",
				//									 Filter = distinctNeck.Select(neckValue =>
				//									 {
				//										 var neckInfo = _context.Necks
				//											 .Where(si => si.Code == neckValue)
				//											 .FirstOrDefault();

				//										 int count = getCalculator.Count(p => p.NecksID == neckInfo.RefID);

				//										  return count > 0 ? new FilterInfo
				//										 {
				//											 Name = neckInfo.Code,
				//											 ID = neckInfo?.RefID,
				//											 Count = count
				//										 } : null;
				//									 })
				//									  .Where(filterInfo => filterInfo != null)
				//									 .ToList()
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Diameter",
				//									 Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo {
				//															 Name = "Diameter",
				//															 MinValue = Math.Floor(getCalculator.Min(p => p.Length) ?? 0),
				//															 MaxValue = Math.Round(getCalculator.Max(p => p.Length) ?? 0)
				//														 },
				//									 }
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Width",
				//									 Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo {
				//															 Name = "Width",
				//															 MinValue = Math.Floor(getCalculator.Min(p => p.Width) ?? 0),
				//															 MaxValue = Math.Round(getCalculator.Max(p => p.Width) ?? 0)
				//														 },
				//									 }
				//								 },
				////new FilterGroup
				////{
				////	GroupName = "Height",
				////	Filter = new List<FilterInfo>
				////	{
				////		new FilterInfo {
				////							Name = "Height",
				////							MinValue = filteredProducts.Min(p => p.Height),
				////							MaxValue = filteredProducts.Max(p => p.Height)
				////						},
				////	}
				////},
				//new FilterGroup
				//								 {
				//									 GroupName = "Price",
				//									 Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo {
				//															 Name = "Price",
				//															 MinValue = getCalculator.Min(p => p.Price),
				//															 MaxValue = getCalculator.Max(p => p.Price)
				//														 },
				//									 }
				//								 },
				//							 };

				//}
				//else if (products.Any(p => p.Type == "closure"))
				//{
				//	filterGroups = new List<FilterGroup>
				//							 {
				//                  //new FilterGroup
				//                  //{
				//                  //    GroupName = "Popular",
				//                  //    Filter = new List<FilterInfo>
				//                  //    {
				//                  //        new FilterInfo { Name = "New Product", Value = 1, Count = query.Count(p => (p.NewProd ?? 0) != 0) },
				//                  //        new FilterInfo { Name = "Favourite Product", Value = 1, Count = query.Count(p => (p.FavProd ?? 0) != 0) },
				//                  //    }.Where(filterInfo => filterInfo.Count > 0).ToList()
				//                  //},
				//                  new FilterGroup
				//								 {
				//									 GroupName = "Color",
				//									 Filter = distinctColors.Select(colorValue =>
				//									 {
				//										 var colorInfo = _context.Colors
				//											 .Where(si => si.Name == colorValue)
				//											 .FirstOrDefault();

				//										 int count = getClosures.Count(p => p.ColorsID == colorInfo.RefID && p.Type == "closure");

				//										  return count > 0 ? new FilterInfo
				//										 {
				//											 Name = colorInfo.Name,
				//											 ID = colorInfo?.RefID,
				//											 Count = count
				//										 } : null;
				//									 })
				//									  .Where(filterInfo => filterInfo != null)
				//									 .ToList()
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Neck",
				//									 Filter = distinctNeck.Select(neckValue =>
				//									 {
				//										 var neckInfo = _context.Necks
				//											 .FirstOrDefault(si => si.Code == neckValue);

				//										 int count = getClosures.Count(p => p.NecksID == neckInfo.RefID);

				//										  return count > 0 ? new FilterInfo
				//										 {
				//											 Name = neckInfo.Code,
				//											 ID = neckInfo?.RefID,
				//											 Count = count
				//										 } : null;
				//									 })
				//									  .Where(filterInfo => filterInfo != null)
				//									 .ToList()
				//								 },
				//                  //new FilterGroup
				//                  //{
				//                  //    GroupName = "Body Dimension",
				//                  //    Filter = new List<FilterInfo>
				//                  //    {
				//                  //        new FilterInfo {
				//                  //                            Name = "Body Diameter",
				//                  //                            MinValue = Math.Floor(query.Min(p => p.Diameter) ?? 0),
				//                  //                            MaxValue = Math.Round(query.Max(p => p.Diameter) ?? 0)
				//                  //                        },
				//                  //        new FilterInfo {
				//                  //                            Name = "Height",
				//                  //                            MinValue = Math.Floor(query.Min(p => p.Height) ?? 0),
				//                  //                            MaxValue = Math.Round(query.Max(p => p.Height) ?? 0)
				//                  //                        },
				//                  //    }
				//                  //},
				//new FilterGroup
				//								 {
				//									 GroupName = "Body Diameter",
				//									 Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo {
				//															 Name = "Body Diameter",
				//															 MinValue = Math.Floor(getClosures.Min(p => p.Diameter) ?? 0),
				//															 MaxValue = Math.Round(getClosures.Max(p => p.Diameter) ?? 0)
				//														 },
				//									 }
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Height",
				//									 Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo {
				//															 Name = "Height",
				//															 MinValue = Math.Floor(getClosures.Min(p => p.Height) ?? 0),
				//															 MaxValue = Math.Round(getClosures.Max(p => p.Height) ?? 0)
				//														 },
				//									 }
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Price",
				//									 Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo {
				//															 Name = "Price",
				//															 MinValue = getClosures.Min(p => p.Price),
				//															 MaxValue = getClosures.Max(p => p.Price)
				//														 },
				//									 }
				//								 },
				//							 };
				//}
				//else if (products.Any(p => p.Type == "cup") || products.Any(p => p.Type == "lid") || products.Any(p => p.Type == "tray"))
				//{

				//	filterGroups = new List<FilterGroup>
				//							 {
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Popular",
				//									 Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo { Name = "New Product", Value = 1, Count = filteredRawProducts.Count(p => (p.NewProd ?? 0) != 0 && (p.Type == "cup" || p.Type == "lid" || p.Type == "tray")) },
				//										 new FilterInfo { Name = "Favourite Product", Value = 1, Count = filteredRawProducts.Count(p => (p.FavProd ?? 0) != 0 && (p.Type == "cup" || p.Type == "lid" || p.Type == "tray")) },
				//									 }.Where(filterInfo => filterInfo.Count > 0).ToList()
				//								 },
				//                  //new FilterGroup
				//                  //{
				//                  //    GroupName = "Body Dimension",
				//                  //    Filter = new List<FilterInfo>
				//                  //    {
				//                  //        new FilterInfo {
				//                  //                            Name = "Body Diameter",
				//                  //                            MinValue = Math.Floor(query.Min(p => p.Length) ?? 0),
				//                  //                            MaxValue = Math.Round(query.Max(p => p.Length) ?? 0)
				//                  //                        },
				//                  //        new FilterInfo {
				//                  //                            Name = "Height",
				//                  //                            MinValue = Math.Floor(query.Min(p => p.Height) ?? 0),
				//                  //                            MaxValue = Math.Round(query.Max(p => p.Height) ?? 0)
				//                  //                        },
				//                  //    }
				//                  //},
				//                  new FilterGroup
				//								 {
				//									 GroupName = "Body Diameter",
				//									 Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo {
				//															 Name = "Body Diameter",
				//															 MinValue = Math.Floor((decimal)filteredRawProducts.Min(p => p.Length) ),
				//															 MaxValue = Math.Round((decimal)filteredRawProducts.Max(p => p.Length) )
				//														 },
				//									 }
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Height",
				//									 Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo {
				//															 Name = "Height",
				//															 MinValue = Math.Floor(filteredRawProducts.Min(p => p.Height) ?? 0 ),
				//															 MaxValue = Math.Round(filteredRawProducts.Max(p => p.Height) ?? 0 )
				//														 },
				//									 }
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Rim",
				//									 Filter = distinctRim.Select(RimValue =>
				//									 {
				//										 var RimInfo = _context.Rims
				//											 .FirstOrDefault(si => si.Name == RimValue);
				//										 int count = 0;
				//										 if (tipe == "tray" ) count = getTray.Count(p => p.Rim == RimInfo.Name);
				//										 if (tipe == "lid" ) count = getLid.Count(p => p.Rim == RimInfo.Name);

				//										  return count > 0 ? new FilterInfo
				//										 {
				//											 Name = RimInfo.Name,
				//											 ID = RimInfo?.RefID,
				//											 Count = count
				//										 } : null;
				//									 })
				//									  .Where(filterInfo => filterInfo != null)
				//									 .ToList()
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Price",
				//									 Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo {
				//															 Name = "Price",
				//															 MinValue = filteredRawProducts.Min(p => p.Price),
				//															 MaxValue = filteredRawProducts.Max(p => p.Price)
				//														 },
				//									 }
				//								 },
				//								 new FilterGroup
				//								 {
				//									 GroupName = "Material Type",
				//									 Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo { Name = "PET"},
				//										 new FilterInfo { Name = "PP" },
				//									 }
				//								 },
				//							 };

				//	if (products.Any(p => p.Type == "cup"))
				//	{
				//		filterGroups.Add(new FilterGroup
				//		{
				//			GroupName = "Volume",
				//			Filter = new List<FilterInfo>{
				//													 new FilterInfo {
				//														 Name = "Volume",
				//														 MinValue = Math.Floor(getCup.Min(p => p.Volume) ?? 0),
				//														 MaxValue = Math.Round(getCup.Max(p => p.Volume) ?? 0)
				//													 },
				//												 }
				//		});

				//		filterGroups.Add(new FilterGroup
				//		{
				//			GroupName = "Shape",
				//			Filter = distinctShape.Select(shapeValue =>
				//			{
				//				var shapeInfo = _context.Shapes
				//					.FirstOrDefault(si => si.Name == shapeValue.Name && si.Type == shapeValue.Type);

				//				int count = getCup.Count(p => p.ShapesID == shapeInfo.RefID);

				//				return count > 0 ? new FilterInfo
				//				{
				//					Name = shapeValue.Name,
				//					ID = shapeInfo?.RefID,
				//					Count = count
				//				} : null;
				//			})
				//			 .Where(filterInfo => filterInfo != null)
				//			.ToList()
				//		});
				//	}

				//	if (products.Any(p => p.Type == "tray"))
				//	{
				//		filterGroups.Add(new FilterGroup
				//		{
				//			GroupName = "Shape",
				//			Filter = distinctShape.Select(shapeValue =>
				//			{
				//				var shapeInfo = _context.Shapes
				//					.FirstOrDefault(si => si.Name == shapeValue.Name && si.Type == shapeValue.Type);

				//				int count = getTray.Count(p => p.ShapesID == shapeInfo.RefID);

				//				return count > 0 ? new FilterInfo
				//				{
				//					Name = shapeValue.Name,
				//					ID = shapeInfo?.RefID,
				//					Count = count
				//				} : null;
				//			})
				//			 .Where(filterInfo => filterInfo != null)
				//			.ToList()
				//		});
				//	}

				//	if (products.Any(p => p.Type == "lid"))
				//	{
				//		filterGroups.Add(new FilterGroup
				//		{
				//			GroupName = "Lid Type",
				//			Filter = new List<FilterInfo>
				//									 {
				//										 new FilterInfo { Name = "Cup",Count = getLid.Count(x => x.CategoriesID == 5)},
				//										 new FilterInfo { Name = "Tray",Count = getLid.Count(x => x.CategoriesID != 5 ) },
				//									 }
				//		});
				//	}

				//}
				//var filters = filterGroups.ToList();
				#endregion

				//Searching
				if (!string.IsNullOrEmpty(searchProduct))
					filteredProducts = filteredProducts.Where(x => x.RefID.ToString().Contains(searchProduct)
						|| x.Name.Contains(searchProduct)
						);

				// Get Total Before Limit and Page
				total = filteredProducts.Count();

				// Adjust limit if it exceeds total count
				if (limit != 0 && limit > total)
				{
					limit = total; // Set limit to total count
				}

				// Calculate the maximum page number based on the limit
				int maxPage = (int)Math.Ceiling((double)total / limit);

				// Adjust the page number if it exceeds the maximum page number
				if (maxPage > 0 && page >= maxPage)
				{
					page = maxPage - 1;
				}

				#region Sorting
				// order product list bottles and thermos
				if ((products.Any(p => p.Type == "bottle") || (products.Any(p => p.Type == "cup") || products.Any(p => p.Type == "tray"))) && isSort == true)
				{
					if (sort.ToLower() == "volume" && type.ToLower() == "asc")
					{
						filteredProducts = filteredProducts.OrderBy(p => p.Volume);
					}
					else if (sort.ToLower() == "volume" && type.ToLower() == "desc")
					{
						filteredProducts = filteredProducts.OrderByDescending(p => p.Volume);
					}
					else if (sort.ToLower() == "price" && type.ToLower() == "asc")
					{
						filteredProducts = filteredProducts.OrderBy(p => p.Price);
					}
					else if (sort.ToLower() == "price" && type.ToLower() == "desc")
					{
						filteredProducts = filteredProducts.OrderByDescending(p => p.Price);
					}
					else
					{
						filteredProducts = filteredProducts.OrderByDescending(p => p.NewProd)
							   .ThenByDescending(p => p.FavProd)
							   .ThenBy(p => p.Volume);
					}

				}
				if (products.Any(p => p.Type == "closure") && isSort == true)
				{
					if (sort.ToLower() == "price" && type.ToLower() == "asc")
					{
						filteredProducts = filteredProducts.OrderBy(p => p.Price);
					}
					else if (sort.ToLower() == "price" && type.ToLower() == "desc")
					{
						filteredProducts = filteredProducts.OrderByDescending(p => p.Price);
					}
					else
					{
						filteredProducts = filteredProducts.OrderBy(p => p.Name);
					}
				}

				// order product list thermos
				if (products.Any(p => p.Type == "lid") && isSort == true)
				{
					if (sort.ToLower() == "price" && type.ToLower() == "asc")
					{
						filteredProducts = filteredProducts.OrderBy(p => p.Price);
					}
					else if (sort.ToLower() == "price" && type.ToLower() == "desc")
					{
						filteredProducts = filteredProducts.OrderByDescending(p => p.Price);
					}
					else
					{
						filteredProducts = filteredProducts.OrderByDescending(p => p.NewProd)
								  .ThenByDescending(p => p.FavProd)
								  .ThenBy(p => p.RefID);
					}
				}
				#endregion

				//Set Limit and Page
				if (limit != 0)
					filteredProducts = filteredProducts.Skip(page * limit).Take(limit);

				// Get Data
				//var categories = filteredProducts.Select(x => x.CategoryName).Distinct().ToList();

				//var productGroup = categories.Select(CategoriesID => new ProductGroup
				//{
				//    Name = CategoriesID,
				//    Total = Convert.ToInt32(filteredProducts.Where(p => p.CategoryName == CategoriesID).Skip((page != 0 ? page : 0) * (limit != 0 ? limit : filteredProducts.Where(p => p.CategoryName == CategoriesID).Count())).Take(limit == 0 ? Convert.ToInt32(filteredProducts.Where(p => p.CategoryName == CategoriesID).Count()) : limit).Count()),
				//    Products = filteredProducts.Where(p => p.CategoryName == CategoriesID).Skip((page != 0 ? page : 0) * (limit != 0 ? limit : filteredProducts.Where(p => p.CategoryName == CategoriesID).Count())).Take(limit == 0 ? Convert.ToInt32(filteredProducts.Where(p => p.CategoryName == CategoriesID).Count()) : limit).ToList(),
				//}).ToList();
				//var data = productGroup.ToList();

				var data = filteredProducts.ToList();

				if (data.Count <= 0 && page > 0)
				{
					page = 0;
					return await GetListNewAsync(limit, page, total, searchKey, searchProduct, CategoriesID, sub, sort, type, filterNew, filterFavourite, filterStockIndicatorMin, filterStockIndicatorMax, filterColor, filterVolumeMin, filterVolumeMax, filterShape, filterNeck, filterRim, filterPriceMin, filterPriceMax, filterDiameterMin, filterDiameterMax, filterWidthMin, filterWidthMax, filterHeightMin, filterHeightMax, materialType, lidType, userid, filterVolumeCup);
				}

				return new ListResponseProduct<Product>(data, total, page, null);

			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				throw;
			}
		}

		public async Task<ListResponseProduct<ProductGroup>> GetProductCategory(int? userid, bool flagDetail)
		{
			try
			{
				var (a, b, c) = await _svc.GetSearch("Beverage,Chemical,Clamshell,Closures,Cosmetic,Cup,Food,Home Care,Inner,Jar,Lid,Personal Care,PET Can,Pharmaceutical,Tray", 0, 0);
				string temp = "";
				List<Product> RawProducts = new List<Product>();
				foreach (DataRow row in a)
				{
					Product p = new Product
					{
						ID = row.IsNull("OriginID") ? 0 : Convert.ToInt64(row["OriginID"]),
						RefID = row.IsNull("RefID") ? 0 : Convert.ToInt64(row["RefID"]),
						Name = row.IsNull("Name") ? string.Empty : Convert.ToString(row["Name"]),
						Image = row.IsNull("Image") ? string.Empty : Convert.ToString(row["Image"]),
						Weight = row.IsNull("Weight") ? 0 : Convert.ToDecimal(row["Weight"]),
						Width = row.IsNull("Width") ? 0 : Convert.ToDecimal(row["Width"]),
						Price = row.IsNull("Price") ? 0 : Convert.ToDecimal(row["Price"]),
						CategoriesID = row.IsNull("CategoriesID") ? 0 : Convert.ToInt64(row["CategoriesID"]),
						CategoryName = row.IsNull("CategoryName") ? string.Empty : Convert.ToString(row["CategoryName"]),
						Stock = row.IsNull("Stock") ? 0 : Convert.ToDecimal(row["Stock"]),
						NewProd = row.IsNull("NewProd") ? 0 : Convert.ToInt64(row["NewProd"]),
						FavProd = row.IsNull("FavProd") ? 0 : Convert.ToInt64(row["FavProd"]),
						ClosuresID = row.IsNull("ClosuresID") ? 0 : Convert.ToInt64(row["ClosuresID"]),
						LidsID = row.IsNull("LidsID") ? 0 : Convert.ToInt64(row["LidsID"]),
						Volume = row.IsNull("Volume") ? 0 : Convert.ToDecimal(row["Volume"]),
						Code = row.IsNull("WmsCode") ? string.Empty : Convert.ToString(row["WmsCode"]),
						QtyPack = row.IsNull("QtyPack") ? 0 : Convert.ToDecimal(row["QtyPack"]),
						TotalViews = row.IsNull("TotalViews") ? 0 : Convert.ToDecimal(row["TotalViews"]),
						TotalShared = row.IsNull("TotalShared") ? 0 : Convert.ToDecimal(row["TotalShared"]),
						NewProdDate = row.IsNull("NewProdDate") ? null : Convert.ToDateTime(row["NewProdDate"]),
						Height = row.IsNull("Height") ? 0 : Convert.ToDecimal(row["Height"]),
						Length = row.IsNull("Length") ? 0 : Convert.ToDecimal(row["Length"]),
						ColorsID = row.IsNull("ColorsID") ? default(long) : Convert.ToInt64(row["ColorsID"]),
						ShapesID = row.IsNull("ShapesID") ? default(long) : Convert.ToInt64(row["ShapesID"]),
						NecksID = row.IsNull("NecksID") ? default(long) : Convert.ToInt64(row["NecksID"]),
						RimsID = row.IsNull("RimsID") ? default(long) : Convert.ToInt64(row["RimsID"]),
						Diameter = row.IsNull("Diameter") ? default(decimal) : Convert.ToDecimal(row["Diameter"]),
						RealImage = row.IsNull("RealImage") ? string.Empty : Convert.ToString(row["RealImage"]),

						StockIndicator = row.IsNull("StockIndicator") ? string.Empty : Convert.ToString(row["StockIndicator"]),

						PlasticType = row.IsNull("PlasticType") ? string.Empty : Convert.ToString(row["PlasticType"]),
						PackagingsID = row.IsNull("PackagingsID") ? 0 : Convert.ToInt64(row["PackagingsID"]),
						Type = row.IsNull("Type") ? string.Empty : Convert.ToString(row["Type"]),
						TokpedUrl = row.IsNull("TokpedUrl") ? string.Empty : Convert.ToString(row["TokpedUrl"]),
						Note = row.IsNull("Note") ? string.Empty : Convert.ToString(row["Note"]),
						CountColor = row.IsNull("CountColor") ? 0 : Convert.ToInt32(row["CountColor"])
					};
					if (temp == p.Name) continue;
					if (temp != p.Name)
					{
						if ((p.Type.ToLower().Equals("tray") || p.Type.ToLower().Equals("cup") || p.Type.ToLower().Equals("bottle")) && userid != 0)
						{
							var dataQtyCart = await (from cart in _context.Carts
													 join cartdetail in _context.CartDetails on cart.ID equals cartdetail.CartsID into carts
													 from carts_detail in carts.DefaultIfEmpty()
													 where cart.CustomersID == userid
														 && carts_detail.ObjectID == p.RefID
													 && carts_detail.Type == (p.Type == "closure" ? "closures" : p.Type)
													 && carts_detail.IsDeleted == false
													 select carts_detail.Qty
											).SumAsync();
							p.QtyCart = Convert.ToInt64(dataQtyCart);
						}

						var neck = await _context.Necks.Where(x => x.RefID == p.NecksID).Select(x => x.Code).FirstOrDefaultAsync();
						var rim = await _context.Rims.Where(x => x.RefID == p.RimsID).Select(x => x.Name).FirstOrDefaultAsync();
						var color = await _context.Colors.Where(x => x.RefID == p.ColorsID).Select(x => x.Name).FirstOrDefaultAsync();
						if (neck != null) p.Neck = Convert.ToString(neck);
						if (rim != null) p.Rim = Convert.ToString(rim);
						if (color != null) p.Color = Convert.ToString(color);

						var dataRim = await _context.Rims.Where(x => x.RefID == p.RimsID).Select(x => x.Name).FirstOrDefaultAsync();
						var dataNeck = await _context.Necks.Where(x => x.RefID == p.NecksID).Select(x => x.Code).FirstOrDefaultAsync();
						if (dataRim != null) p.Rim = dataRim;
						if (dataNeck != null) p.Neck = dataNeck;

						if (flagDetail)
						{
							//var dataLeadtime = await _context.ProductStatuses.Where(x => x.ProductName.Contains(p.Name)).Select(x => x.LeadTime).FirstOrDefaultAsync();
							var dataWishlist = await _context.WishLists.Where(si => p.RefID == si.ProductId && si.UserId == userid && si.Type == (p.Type == "closure" ? "closures" : p.Type)).FirstOrDefaultAsync();
							var dataColor = await _context.Colors.Where(x => x.RefID == p.ColorsID).Select(x => x.Name).FirstOrDefaultAsync();
							var dataPackaging = await _context.Packagings.FirstOrDefaultAsync(x => x.RefID == p.PackagingsID);
							//var dataTags = await (from td in _context.TagDetails
							//                      join t in _context.Tags on td.TagsID equals t.RefID
							//                      where td.ObjectID == p.RefID
							//                      && td.Type.ToLower() == ((p.Type == "cup" || p.Type == "tray") ? "thermo" : p.Type)
							//                      && td.IsDeleted == false
							//                      select new Tag
							//                      {
							//                          ID = t.ID,
							//                          RefID = t.RefID,
							//                          Name = t.Name
							//                      }
							//                        ).ToListAsync();
							//var funcType = "";
							//if (p.Type == "bottle") funcType = "Bottle";
							//else if (p.Type == "lid") funcType = "Lid";
							//else if (p.Type == "cup" || p.Type == "tray") funcType = "Thermo";
							//var dataFunction = await (from fd in _context.FunctionDetails
							//                          join f in _context.Functions on fd.FunctionsID equals f.RefID
							//                          where fd.ObjectID == p.RefID
							//                          && fd.Type.ToLower() == funcType
							//                          && fd.IsDeleted == false
							//                          select new Sopra.Entities.Function
							//                          {
							//                              ID = f.ID,
							//                              RefID = f.RefID,
							//                              Name = f.Name
							//                          }
							//                          ).ToListAsync();

							//var imageType = "";
							//if (p.Type == "bottle") imageType = "Calculators";
							//else if (p.Type == "lid") imageType = "Lid";
							//else if (p.Type == "cup" || p.Type == "tray") imageType = "Thermos";
							//else if (p.Type == "closure" || p.Type == "tray") imageType = "Closures";
							//var dataImage = await (from im in _context.Images
							//                       where im.ObjectID == p.RefID
							//                         && im.Type == imageType
							//                         && im.IsDeleted == false
							//                       select new Sopra.Entities.Image
							//                       {
							//                           ID = im.ID,
							//                           RefID = im.RefID,
							//                           ProductImage = im.ProductImage,
							//                           Type = im.Type,
							//                           ObjectID = im.ObjectID
							//                       }
							//                       ).OrderBy(x => x.RefID).ToListAsync();

							//if (dataLeadtime != null) p.LeadTime = dataLeadtime;
							if (dataWishlist != null) p.Wishlist = 1;
							if (dataColor != null) p.Color = dataColor;
							if (dataPackaging != null)
							{
								if (dataPackaging.Tipe != 1) p.Packaging = $"{dataPackaging.Length:F0} mm x {dataPackaging.Width:F0} mm x {dataPackaging.Height:F0} mm ({((dataPackaging.Length * dataPackaging.Width * dataPackaging.Height) / 1000000000):F2} m³)";
								else p.Packaging = $"{((p.Length * p.Length * p.Height * p.QtyPack) / 1000000000):F2} m³";
							}
							//if (dataTags != null) p.Tags = dataTags;
							//if (dataFunction != null) p.Functions = dataFunction;
							//if (dataImage != null) p.Images = dataImage;
						}

						RawProducts.Add(p);
						temp = p.Name;
					}


				}
				var categories = RawProducts.Select(x => x.CategoryName).Distinct().ToList();

				var productGroup = categories.Select(CategoriesID => new ProductGroup
				{
					Name = CategoriesID,
					Total = Convert.ToInt32(RawProducts.Where(p => p.CategoryName == CategoriesID).Count()),
					Products = RawProducts.Where(p => p.CategoryName == CategoriesID).ToList(),
				}).ToList();
				var data = productGroup.ToList();
				return new ListResponseProduct<ProductGroup>(data, 0, 0, null);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				throw;
			}
		}
		public async Task<ListResponse<Product>> GetAlternativeAsync(int limit, int total, int page, string search, string CategoriesID, string alternative, int refid, int? provinceid, string provincename, int? userid)
		{
			IQueryable<Product> query = null;

			try
			{
				_context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

				// product alternative
				if (CategoriesID.ToLower() == "bottle" && alternative.ToLower() == "color" && refid != 0)
				{
					var getName = _context.Calculators
								.Where(calculator => calculator.RefID == refid)
								.Select(calculator => new { calculator.Name, calculator.ColorsID })
								.FirstOrDefault();

					string[] prodName = getName?.Name?.Split(',');

					var getCalculator = from calculator in _context.Calculators
										join color in _context.Colors on calculator.ColorsID equals color.RefID
										join material in _context.Materials on calculator.MaterialsID equals material.RefID
										let stockindicator = _context.StockIndicators
											.Where(si => calculator.Stock >= si.MinQty && calculator.Stock <= si.MaxQty)
											.FirstOrDefault()
										let realImage = _context.Images.Where(x => x.ObjectID == calculator.RefID && x.Type == "Calculators")
												 .OrderBy(x => x.RefID)
												 .Select(x => x.ProductImage)
												 .FirstOrDefault()
										let qtyCart = (from c in _context.Carts
													   join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
													   from carts_detail in carts.DefaultIfEmpty()
													   where c.CustomersID == userid
														   && carts_detail.ObjectID == calculator.RefID
													   && carts_detail.Type == "bottle"
													   && carts_detail.IsDeleted == false
													   select carts_detail.Qty
														).Sum()
										where calculator.IsDeleted == false
											  && calculator.Status == 3
											  && calculator.RefID != refid
											  && calculator.ColorsID != getName.ColorsID
										select new Product
										{
											ID = calculator.ID,
											RefID = calculator.RefID,
											Name = calculator.Name,
											Image = calculator.Image,
											Weight = calculator.Weight,
											Price = calculator.Price,
											CategoriesID = calculator.CategoriesID,
											Stock = calculator.Stock,
											NewProd = calculator.NewProd,
											FavProd = calculator.FavProd,
											ClosuresID = calculator.ClosuresID,
											Volume = calculator.Volume,
											Code = calculator.WmsCode,
											QtyPack = calculator.QtyPack,
											TotalViews = calculator.TotalViews,
											TotalShared = calculator.TotalShared,
											NewProdDate = calculator.NewProdDate,
											Height = calculator.Height,
											Length = calculator.Length,

											Color = color.Name,

											RealImage = realImage,

											StockIndicator = stockindicator.Name,

											PlasticType = material.PlasticType,
											MaterialsID = calculator.MaterialsID,
											QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											Type = "bottle"
										};

					if (prodName != null && prodName.Length > 0)
					{
						getCalculator = getCalculator.Where(calculator =>
						calculator.Name.StartsWith(prodName[0])
						&& !calculator.Name.Contains("premium")
						&& !calculator.Name.Contains("square"));
					}
					else
					{
						return new ListResponse<Sopra.Entities.Product>(
							data: new List<Sopra.Entities.Product>(),
							total: 0,
							page: 0
						);
					}

					query = getCalculator;
				}
				else if (CategoriesID.ToLower() == "bottle" && alternative.ToLower() == "volume" && refid != 0)
				{
					var altVol = _context.AltVolumeDetails
								.Where(altvolumedetail => altvolumedetail.CalculatorsID == refid)
								.Select(altvolumedetail => altvolumedetail.AltVolumesID)
								.FirstOrDefault();


					var getVolList = from altvolumedetail in _context.AltVolumeDetails
									 join calculator in _context.Calculators on altvolumedetail.CalculatorsID equals calculator.RefID
									 join color in _context.Colors on calculator.ColorsID equals color.RefID
									 join material in _context.Materials on calculator.MaterialsID equals material.RefID
									 let stockindicator = _context.StockIndicators
											.Where(si => calculator.Stock >= si.MinQty && calculator.Stock <= si.MaxQty)
											.FirstOrDefault()
									 let realImage = _context.Images.Where(x => x.ObjectID == calculator.RefID && x.Type == "Calculators")
													 .OrderBy(x => x.RefID)
													 .Select(x => x.ProductImage)
													 .FirstOrDefault()
									 let qtyCart = (from c in _context.Carts
													join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
													from carts_detail in carts.DefaultIfEmpty()
													where c.CustomersID == userid
														&& carts_detail.ObjectID == calculator.RefID
													&& carts_detail.Type == "bottle"
													&& carts_detail.IsDeleted == false
													select carts_detail.Qty
													).Sum()
									 where altvolumedetail.AltVolumesID == altVol
										   && calculator.IsDeleted == false
										   && calculator.Status == 3
										   && altvolumedetail.CalculatorsID != refid
									 select new Product
									 {
										 ID = calculator.ID,
										 RefID = calculator.RefID,
										 Name = calculator.Name,
										 Image = calculator.Image,
										 Weight = calculator.Weight,
										 Price = calculator.Price,
										 CategoriesID = calculator.CategoriesID,
										 Stock = calculator.Stock,
										 NewProd = calculator.NewProd,
										 FavProd = calculator.FavProd,
										 ClosuresID = calculator.ClosuresID,
										 Volume = calculator.Volume,
										 Code = calculator.WmsCode,
										 QtyPack = calculator.QtyPack,
										 TotalViews = calculator.TotalViews,
										 TotalShared = calculator.TotalShared,
										 NewProdDate = calculator.NewProdDate,
										 Height = calculator.Height,
										 Length = calculator.Length,

										 Color = color.Name,

										 RealImage = realImage,

										 StockIndicator = stockindicator.Name,

										 PlasticType = material.PlasticType,
										 MaterialsID = calculator.MaterialsID,
										 QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
										 Type = "bottle"
									 };

					query = getVolList;
				}
				else if ((CategoriesID.ToLower() == "cup" || CategoriesID.ToLower() == "tray") && alternative.ToLower() == "volume" && refid != 0)
				{
					var altVol = _context.AltSizeDetails
								.Where(altvolumedetail => altvolumedetail.ThermosID == refid)
								.Select(altvolumedetail => altvolumedetail.AltSizeID)
								.FirstOrDefault();


					var getVolList = from altvolumedetail in _context.AltSizeDetails
									 join calculator in _context.Thermos on altvolumedetail.ThermosID equals (int?)calculator.RefID
									 join color in _context.Colors on calculator.ColorsID equals color.RefID
									 join material in _context.Materials on calculator.MaterialsID equals material.RefID
									 let stockindicator = _context.StockIndicators
											.Where(si => calculator.Stock >= si.MinQty && calculator.Stock <= si.MaxQty)
											.FirstOrDefault()
									 let realImage = _context.Images.Where(x => x.ObjectID == calculator.RefID && x.Type == "Thermos")
													 .OrderBy(x => x.RefID)
													 .Select(x => x.ProductImage)
													 .FirstOrDefault()
									 let qtyCart = (from c in _context.Carts
													join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
													from carts_detail in carts.DefaultIfEmpty()
													where c.CustomersID == userid
														&& carts_detail.ObjectID == calculator.RefID
													&& carts_detail.Type == "bottle"
													&& carts_detail.IsDeleted == false
													select carts_detail.Qty
													).Sum()
									 where altvolumedetail.AltSizeID == altVol
										   && calculator.IsDeleted == false
										   && calculator.Status == 3
										   && (CategoriesID.ToLower() == "cup" ? calculator.CategoriesID == 5 : calculator.CategoriesID != 5)
										   && altvolumedetail.ThermosID != refid
									 select new Product
									 {
										 ID = calculator.ID,
										 RefID = calculator.RefID,
										 Name = calculator.Name,
										 Image = calculator.Image,
										 Weight = calculator.Weight,
										 Price = calculator.Price,
										 CategoriesID = calculator.CategoriesID,
										 Stock = calculator.Stock,
										 NewProd = calculator.NewProd,
										 FavProd = calculator.FavProd,
										 ClosuresID = calculator.LidsID,
										 Volume = calculator.Volume,
										 Code = calculator.WmsCode,
										 QtyPack = calculator.Qty,
										 TotalViews = calculator.TotalViews,
										 TotalShared = calculator.TotalShared,
										 NewProdDate = calculator.NewProdDate,
										 Height = calculator.Height,
										 Length = calculator.Length,

										 Color = color.Name,

										 RealImage = realImage,

										 StockIndicator = stockindicator.Name,

										 PlasticType = material.PlasticType,
										 MaterialsID = calculator.MaterialsID,
										 QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
										 Type = CategoriesID.ToLower()
									 };


					query = getVolList;
				}
				else if (CategoriesID.ToLower() == "bottle" && alternative.ToLower() == "weight" && refid != 0)
				{
					var altWeight = _context.AltWeightDetails
								.Where(altWeightdetail => altWeightdetail.ObjectID == refid)
								.Where(altWeightdetail => altWeightdetail.Type == "Bottle")
								.Select(altWeightdetail => altWeightdetail.AltWeightsID)
								.FirstOrDefault();


					var getWeightList = from altWeightdetail in _context.AltWeightDetails
										join calculator in _context.Calculators on altWeightdetail.ObjectID equals calculator.RefID
										join color in _context.Colors on calculator.ColorsID equals color.RefID
										join material in _context.Materials on calculator.MaterialsID equals material.RefID
										let stockindicator = _context.StockIndicators
											   .Where(si => calculator.Stock >= si.MinQty && calculator.Stock <= si.MaxQty)
											   .FirstOrDefault()
										let realImage = _context.Images.Where(x => x.ObjectID == calculator.RefID && x.Type == "Calculators")
											  .OrderBy(x => x.RefID)
											  .Select(x => x.ProductImage)
											  .FirstOrDefault()
										let qtyCart = (from c in _context.Carts
													   join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
													   from carts_detail in carts.DefaultIfEmpty()
													   where c.CustomersID == userid
														   && carts_detail.ObjectID == calculator.RefID
													   && carts_detail.Type == "bottle"
													   && carts_detail.IsDeleted == false
													   select carts_detail.Qty
														).Sum()
										where altWeightdetail.AltWeightsID == altWeight
											  && altWeightdetail.Type == "Bottle"
											  && calculator.IsDeleted == false
											  && calculator.Status == 3
											  && altWeightdetail.ObjectID != refid
										select new Product
										{
											ID = calculator.ID,
											RefID = calculator.RefID,
											Name = calculator.Name,
											Image = calculator.Image,
											Weight = calculator.Weight,
											Price = calculator.Price,
											CategoriesID = calculator.CategoriesID,
											Stock = calculator.Stock,
											NewProd = calculator.NewProd,
											FavProd = calculator.FavProd,
											ClosuresID = calculator.ClosuresID,
											Volume = calculator.Volume,
											Code = calculator.WmsCode,
											QtyPack = calculator.QtyPack,
											TotalViews = calculator.TotalViews,
											TotalShared = calculator.TotalShared,
											NewProdDate = calculator.NewProdDate,
											Height = calculator.Height,
											Length = calculator.Length,

											Color = color.Name,

											RealImage = realImage,

											StockIndicator = stockindicator.Name,

											PlasticType = material.PlasticType,
											MaterialsID = calculator.MaterialsID,
											QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											Type = "bottle"
										};

					query = getWeightList;
				}
				else if ((CategoriesID.ToLower() == "tray" || CategoriesID.ToLower() == "cup") && alternative.ToLower() == "weight" && refid != 0)
				{
					var altWeight = _context.AltWeightDetails
								.Where(altWeightdetail => altWeightdetail.ObjectID == refid)
								.Where(altWeightdetail => altWeightdetail.Type == "Thermo")
								.Select(altWeightdetail => altWeightdetail.AltWeightsID)
								.FirstOrDefault();


					var getWeightList = from altWeightdetail in _context.AltWeightDetails
										join thermo in _context.Thermos on altWeightdetail.ObjectID equals thermo.RefID
										join color in _context.Colors on thermo.ColorsID equals color.RefID
										join material in _context.Materials on thermo.MaterialsID equals material.RefID
										let stockindicator = _context.StockIndicators
											.Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
											.FirstOrDefault()
										let realImage = _context.Images.Where(x => x.ObjectID == thermo.RefID && x.Type == "Thermo")
											  .OrderBy(x => x.RefID)
											  .Select(x => x.ProductImage)
											  .FirstOrDefault()
										let qtyCart = (from c in _context.Carts
													   join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
													   from carts_detail in carts.DefaultIfEmpty()
													   where c.CustomersID == userid
												&& carts_detail.ObjectID == thermo.RefID
												&& carts_detail.Type == (thermo.CategoriesID == 5 ? "cup" : "tray")
												&& carts_detail.IsDeleted == false
													   select carts_detail.Qty
														).Sum()
										where altWeightdetail.AltWeightsID == altWeight
											  && altWeightdetail.Type == "Thermo"
											  && thermo.IsDeleted == false
											  && thermo.Status == 3
											  && altWeightdetail.ObjectID != refid
										select new Product
										{
											ID = thermo.ID,
											RefID = thermo.RefID,
											Name = thermo.Name,
											Image = thermo.Image,
											Weight = thermo.Weight,
											Price = thermo.Price,
											CategoriesID = thermo.CategoriesID,
											Stock = thermo.Stock,
											NewProd = thermo.NewProd,
											FavProd = thermo.FavProd,
											LidsID = thermo.LidsID,
											Volume = thermo.Volume,
											Height = thermo.Height,
											Length = thermo.Length,
											QtyPack = thermo.Qty,
											Code = thermo.WmsCode,
											TotalShared = thermo.TotalShared,
											TotalViews = thermo.TotalViews,

											Color = color.Name,

											RealImage = realImage,

											StockIndicator = stockindicator.Name,

											PlasticType = material.PlasticType,
											MaterialsID = thermo.MaterialsID,
											QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											Type = thermo.CategoriesID == 5 ? "cup" : "tray"
										};

					query = getWeightList;
				}
				else if (CategoriesID.ToLower() == "bottle" && alternative.ToLower() == "neck" && refid != 0)
				{
					//var imageQuery = from calculator in _context.Calculators
					//				 join image in _context.Images on calculator.RefID equals image.ObjectID into imagesGroup
					//				 where calculator.IsDeleted == false
					//					 && calculator.Status == 3
					//					 && calculator.ColorsID == 14
					//					 && imagesGroup.Any(img => img.Type == "Calculators")
					//				 select new
					//				 {
					//					 CalculatorId = calculator.ID,
					//					 RealImages = imagesGroup
					//								 .Where(img => img.Type == "Calculators")
					//								 .Select(img => img.ProductImage)
					//								 .ToList()
					//				 };

					//var images = imageQuery.ToDictionary(x => x.CalculatorId, x => x.RealImages.FirstOrDefault());

					var altNeck = _context.AltNeckDetails
								.Where(altneckDetail => altneckDetail.CalculatorsID == refid)
								.Select(altneckDetail => altneckDetail.AltNecksID)
								.FirstOrDefault();


					var getNeckList = from altneckDetail in _context.AltNeckDetails
									  join calculator in _context.Calculators on altneckDetail.CalculatorsID equals calculator.RefID
									  join color in _context.Colors on calculator.ColorsID equals color.RefID
									  join material in _context.Materials on calculator.MaterialsID equals material.RefID
									  join neck in _context.Necks on calculator.NecksID equals neck.RefID
									  let stockindicator = _context.StockIndicators
											 .Where(si => calculator.Stock >= si.MinQty && calculator.Stock <= si.MaxQty)
											 .FirstOrDefault()
									  let realImage = _context.Images.Where(x => x.ObjectID == calculator.RefID && x.Type == "Calculators")
									   .OrderBy(x => x.RefID)
									   .Select(x => x.ProductImage)
									   .FirstOrDefault()
									  let qtyCart = (from c in _context.Carts
													 join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
													 from carts_detail in carts.DefaultIfEmpty()
													 where c.CustomersID == userid
														 && carts_detail.ObjectID == calculator.RefID
													 && carts_detail.Type == "bottle"
													 && carts_detail.IsDeleted == false
													 select carts_detail.Qty
														).Sum()
									  where altneckDetail.AltNecksID == altNeck
											&& calculator.IsDeleted == false
											&& calculator.Status == 3
											&& altneckDetail.CalculatorsID != refid
									  select new Product
									  {
										  ID = calculator.ID,
										  RefID = calculator.RefID,
										  Name = calculator.Name,
										  Image = calculator.Image,
										  Weight = calculator.Weight,
										  Price = calculator.Price,
										  CategoriesID = calculator.CategoriesID,
										  Stock = calculator.Stock,
										  NewProd = calculator.NewProd,
										  FavProd = calculator.FavProd,
										  ClosuresID = calculator.ClosuresID,
										  Volume = calculator.Volume,
										  Code = calculator.WmsCode,
										  QtyPack = calculator.QtyPack,
										  TotalViews = calculator.TotalViews,
										  TotalShared = calculator.TotalShared,
										  NewProdDate = calculator.NewProdDate,
										  Height = calculator.Height,
										  Length = calculator.Length,
										  Neck = neck.Code,

										  Color = color.Name,

										  RealImage = realImage,

										  StockIndicator = stockindicator.Name,

										  PlasticType = material.PlasticType,
										  MaterialsID = calculator.MaterialsID,
										  QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
										  Type = "bottle"
									  };

					query = getNeckList;
				}
				else if (CategoriesID.ToLower() == "bottle" && alternative.ToLower() == "acc" && refid != 0)
				{
					var getClosures = from closures in _context.Closures
									  join caculator in _context.Calculators on closures.NecksID equals caculator.NecksID into caculators
									  from caculators_detail in caculators.DefaultIfEmpty()

									  join color in _context.Colors on closures.ColorsID equals color.RefID into colors
									  from colors_detail in colors.DefaultIfEmpty()
									  orderby closures.Ranking
									  let stockindicator = _context.StockIndicators
										  .Where(si => closures.Stock >= si.MinQty && closures.Stock <= si.MaxQty)
										  .FirstOrDefault()
									  //   let realImage = _context.Images.Where(x => x.ObjectID == closures.RefID && x.Type == "Closures")
									  //.OrderBy(x => x.RefID)
									  //.Select(x => x.ProductImage)
									  //.FirstOrDefault()
									  let qtyCart = 0
									  where closures.IsDeleted == false
											&& closures.Status == 3
											&& caculators_detail.RefID == refid
											&& closures.ClosureType == 1
											&& caculators_detail.IsDeleted == false
									  select new Product
									  {
										  ID = closures.ID,
										  RefID = closures.RefID,
										  Name = closures.Name,
										  Image = closures.Image,
										  Weight = closures.Weight,
										  Price = closures.Price,
										  Height = closures.Height,
										  Code = closures.WmsCode,
										  QtyPack = closures.QtyPack,
										  MaterialsID = 0,

										  Color = colors_detail.Name,

										  //RealImage = realImage,

										  StockIndicator = stockindicator.Name,
										  QtyCart = Convert.ToInt64(qtyCart),
										  Type = "closures"
									  };
					query = getClosures;
				}
				else if (CategoriesID.ToLower() == "bottle" && alternative.ToLower() == "acc2" && refid != 0)
				{
					var getClosures = from closures in _context.Closures
									  join caculator in _context.Calculators on closures.NecksID equals caculator.NecksID
									  join color in _context.Colors on closures.ColorsID equals color.RefID
									  orderby closures.Ranking
									  let stockindicator = _context.StockIndicators
										  .Where(si => closures.Stock >= si.MinQty && closures.Stock <= si.MaxQty)
										  .FirstOrDefault()
									  //                         let realImage = _context.Images.Where(x => x.ObjectID == closures.RefID && x.Type == "Closures")
									  //.OrderBy(x => x.RefID)
									  //.Select(x => x.ProductImage)
									  //.FirstOrDefault()
									  let qtyCart = 0
									  where closures.IsDeleted == false
											&& closures.Status == 3
											&& caculator.RefID == refid
											&& closures.ClosureType != 1
											&& caculator.IsDeleted == false
									  select new Product
									  {
										  ID = closures.ID,
										  RefID = closures.RefID,
										  Name = closures.Name,
										  Image = closures.Image,
										  Weight = closures.Weight,
										  Price = closures.Price,
										  Height = closures.Height,
										  Code = closures.WmsCode,
										  QtyPack = closures.QtyPack,
										  MaterialsID = 0,

										  Color = color.Name,

										  //RealImage = realImage,

										  StockIndicator = stockindicator.Name,
										  QtyCart = Convert.ToInt64(qtyCart),
										  Type = "closures"
									  };
					query = getClosures;
				}
				else if ((CategoriesID.ToLower() == "tray" || CategoriesID.ToLower() == "cup") && alternative.ToLower() == "acc" && refid != 0)
				{
					var getLid = from lid in _context.Lids
								 join thermo in _context.Thermos on lid.RimsID equals thermo.RimsID
								 join color in _context.Colors on lid.ColorsID equals color.RefID
								 join material in _context.Materials on lid.MaterialsID equals material.RefID
								 //   let stockindicator = _context.StockIndicators
								 //      .Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
								 //      .FirstOrDefault()
								 let realImage = _context.Images.Where(x => x.ObjectID == lid.RefID && x.Type == "Lids")
								   .OrderBy(x => x.RefID)
								   .Select(x => x.ProductImage)
								   .FirstOrDefault()
								 let qtyCart = 0
								 where lid.IsDeleted == false
								   && lid.Status == 3
								   && thermo.RefID == refid
								   && thermo.IsDeleted == false
								 select new Product
								 {
									 ID = lid.ID,
									 RefID = lid.RefID,
									 Name = lid.Name,
									 Image = lid.Image,
									 Weight = lid.Weight,
									 Price = lid.Price,
									 NewProd = lid.NewProd,
									 FavProd = lid.FavProd,
									 Height = lid.Height,
									 Length = lid.Length,
									 QtyPack = lid.Qty,

									 Color = color.Name,

									 RealImage = realImage,

									 //   StockIndicator = stockindicator.Name,

									 PlasticType = material.PlasticType,
									 MaterialsID = lid.MaterialsID,
									 QtyCart = Convert.ToInt64(qtyCart),
									 Type = "lid"
								 };

					query = getLid;
				}
				else if ((CategoriesID.ToLower() == "tray" || CategoriesID.ToLower() == "cup") && alternative.ToLower() == "acc2 inner" && refid != 0)
				{
					var getLid = from thermo in _context.ProductDetails2
								 join color in _context.Colors on thermo.ColorsID equals color.RefID
								 //join material in _context.Materials on thermo.MaterialsID equals material.RefID
								 //   let stockindicator = _context.StockIndicators
								 //      .Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
								 //      .FirstOrDefault()
								 //let realImage = _context.Images.Where(x => x.ObjectID == thermo.RefID && x.Type == "Thermos")
								 //  .OrderBy(x => x.RefID)
								 //  .Select(x => x.ProductImage)
								 //  .FirstOrDefault()
								 let qtyCart = (from c in _context.Carts
												join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
												from carts_detail in carts.DefaultIfEmpty()
												where c.CustomersID == userid
										 && carts_detail.ObjectID == thermo.RefID
										 && carts_detail.Type == (thermo.CategoriesID == 5 ? "cup" : "tray")
										 && carts_detail.IsDeleted == false
												select carts_detail.Qty
												).Sum()
								 where thermo.CategoriesID == 11
								   && thermo.IsDeleted == false
								 select new Product
								 {
									 ID = (long)thermo.OriginID,
									 RefID = thermo.RefID,
									 Name = thermo.Name,
									 Image = thermo.Image,
									 Weight = thermo.Weight,
									 Price = thermo.Price,
									 NewProd = thermo.NewProd,
									 FavProd = thermo.FavProd,
									 Height = thermo.Height,
									 Length = thermo.Length,
									 QtyPack = thermo.QtyPack,
									 LidsID = thermo.LidsID,
									 Color = color.Name,

									 RealImage = thermo.RealImage,

									 //   StockIndicator = stockindicator.Name,
									 QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
									 PlasticType = thermo.PlasticType,
									 //MaterialsID = thermo.MaterialsID,

									 Type = CategoriesID.ToLower()
								 };

					query = getLid;
				}
				else if (CategoriesID.ToLower() == "bottle" && alternative.ToLower() == "safety" && refid != 0)
				{

					var getProduct = (from a in _context.Calculators
									  join m in _context.Materials on a.MaterialsID equals m.RefID
									  where a.IsDeleted == false
									  && a.RefID == refid
									  && a.Status == 3
									  select new { ProductList = a, Material = m }).ToList();

					var getSafety = (from b in _context.Safetys where b.IsDeleted == false select b).ToList();

					var propertyToSafetyMap = new Dictionary<string, string>
						{
							{ "Halal", "Halal" },
							{ "FoodGrade", "Food Grade" },
							{ "BpaFree", "Bebas BPA" },
							{ "EcoFriendly", "Ramah Lingku" },
							{ "Recyclable", "Daur Ulang" }
						};

					var calculatorPropertyToSafetyMap = new Dictionary<string, string>
						{
							{ "Microwable", "Bisa Microwave" },
							{ "LessThan60", "< 60°C" },
							{ "LeakProof", "Tidak bocor" },
							{ "TamperEvident", "Segel" },
							{ "AirTight", "Kedap Udara" },
							{ "BreakResistant", "Tidak mudah pecah" },
							{ "SpillProof", "Anti Tumpah" }
						};

					var result = new List<Sopra.Entities.Product>();

					foreach (var calculatorItem in getProduct)
					{
						if (calculatorItem.ProductList.MaterialsID != 0)
						{
							foreach (var prop in calculatorItem.Material.GetType().GetProperties())
							{
								if (propertyToSafetyMap.TryGetValue(prop.Name, out var safetyName) && (long)prop.GetValue(calculatorItem.Material) == 1)
								{
									var safetyItem = getSafety.FirstOrDefault(s => s.Name == safetyName);

									if (safetyItem != null)
									{
										result.Add(new Sopra.Entities.Product { Image = safetyItem.Image });
									}
								}
							}
							foreach (var prop in calculatorItem.ProductList.GetType().GetProperties())
							{
								if (calculatorPropertyToSafetyMap.TryGetValue(prop.Name, out var safetyName) && (long)prop.GetValue(calculatorItem.ProductList) == 1)
								{
									var safetyItem = getSafety.FirstOrDefault(s => s.Name == safetyName);

									if (safetyItem != null)
									{
										result.Add(new Sopra.Entities.Product { Image = safetyItem.Image });
									}
								}
							}
						}
						else
						{
							foreach (var prop in calculatorItem.ProductList.GetType().GetProperties())
							{
								if (calculatorPropertyToSafetyMap.TryGetValue(prop.Name, out var safetyName) && (long)prop.GetValue(calculatorItem.ProductList) == 1)
								{
									var safetyItem = getSafety.FirstOrDefault(s => s.Name == safetyName);

									if (safetyItem != null)
									{
										result.Add(new Sopra.Entities.Product { Image = safetyItem.Image });
									}
								}
							}
						}
					}

					var listResponse = new ListResponse<Sopra.Entities.Product>(result, result.Count, 1);

					return listResponse;
				}
				else if ((CategoriesID.ToLower() == "tray" || CategoriesID.ToLower() == "cup") && alternative.ToLower() == "safety" && refid != 0)
				{
					var getProduct = (from a in _context.Thermos
									  join m in _context.Materials on a.MaterialsID equals m.RefID
									  where a.IsDeleted == false
									  && a.RefID == refid
									  && a.Status == 3
									  select new { ProductList = a, Material = m }).ToList();

					var getSafety = (from b in _context.Safetys where b.IsDeleted == false select b).ToList();

					var propertyToSafetyMap = new Dictionary<string, string>
						{
							{ "Halal", "Halal" },
							{ "FoodGrade", "Food Grade" },
							{ "BpaFree", "Bebas BPA" },
							{ "EcoFriendly", "Ramah Lingku" },
							{ "Recyclable", "Daur Ulang" }
						};

					var calculatorPropertyToSafetyMap = new Dictionary<string, string>
						{
							{ "Microwable", "Bisa Microwave" },
							{ "LessThan60", "< 60°C" },
							{ "LeakProof", "Tidak bocor" },
							{ "TamperEvident", "Segel" },
							{ "AirTight", "Kedap Udara" },
							{ "BreakResistant", "Tidak mudah pecah" },
							{ "SpillProof", "Anti Tumpah" }
						};

					var result = new List<Sopra.Entities.Product>();

					foreach (var calculatorItem in getProduct)
					{
						if (calculatorItem.ProductList.MaterialsID != 0)
						{
							foreach (var prop in calculatorItem.Material.GetType().GetProperties())
							{
								if (propertyToSafetyMap.TryGetValue(prop.Name, out var safetyName) && (long)prop.GetValue(calculatorItem.Material) == 1)
								{
									var safetyItem = getSafety.FirstOrDefault(s => s.Name == safetyName);

									if (safetyItem != null)
									{
										result.Add(new Sopra.Entities.Product { Image = safetyItem.Image });
									}
								}
							}
							foreach (var prop in calculatorItem.ProductList.GetType().GetProperties())
							{
								if (calculatorPropertyToSafetyMap.TryGetValue(prop.Name, out var safetyName) && (long)prop.GetValue(calculatorItem.ProductList) == 1)
								{
									var safetyItem = getSafety.FirstOrDefault(s => s.Name == safetyName);

									if (safetyItem != null)
									{
										result.Add(new Sopra.Entities.Product { Image = safetyItem.Image });
									}
								}
							}
						}
						else
						{
							foreach (var prop in calculatorItem.ProductList.GetType().GetProperties())
							{
								if (calculatorPropertyToSafetyMap.TryGetValue(prop.Name, out var safetyName) && (long)prop.GetValue(calculatorItem.ProductList) == 1)
								{
									var safetyItem = getSafety.FirstOrDefault(s => s.Name == safetyName);

									if (safetyItem != null)
									{
										result.Add(new Sopra.Entities.Product { Image = safetyItem.Image });
									}
								}
							}
						}
					}

					var listResponse = new ListResponse<Sopra.Entities.Product>(result, result.Count, 1);

					return listResponse;
				}
				else if (CategoriesID.ToLower() == "bottle" && alternative.ToLower() == "reseller" && refid != 0)
				{
					var getDate = Utility.getCurrentTimestamps().AddMonths(-3);
					var CustomerReseller = from a in _context.TransactionOrderDetails
										   join b in _context.Customers on a.CustomersID equals b.RefID
										   where a.ObjectID == refid
										   && a.OrderStatus.ToLower() == "active"
										   && b.Status == 1
										   && a.ProvinceName != null
										   && a.OrderDate >= getDate
										   && (provinceid == null || b.DeliveryProvinceID == provinceid)
										   && (provincename == null || a.ProvinceName.ToLower() == provincename.ToLower())
										   group a by new { a.ObjectID, a.ProvinceName, a.RegencyName, a.DistrictName, b.Name, b.Mobile1, b.DeliveryAddress } into grouped
										   select new Product
										   {
											   RefID = grouped.Key.ObjectID,
											   Name = grouped.Key.Name,
											   Mobile = grouped.Key.Mobile1,
											   Address = grouped.Key.DeliveryAddress,
											   ProvinceName = grouped.Key.ProvinceName,
											   RegencyName = grouped.Key.RegencyName,
											   DistrictName = grouped.Key.DistrictName,
											   TotalQty = grouped.Sum(x => x.Qty)
										   };

					query = CustomerReseller;
				}
				else
				{
					return new ListResponse<Sopra.Entities.Product>(
							data: new List<Sopra.Entities.Product>(),
							total: 0,
							page: 0
						);
				}

				// Searching
				if (!string.IsNullOrEmpty(search))
					query = query.Where(x => x.RefID.ToString().Contains(search)
						|| x.Name.Contains(search)
						);

				// Get Total Before Limit and Page
				total = await query.CountAsync();

				// Adjust limit if it exceeds total count
				if (limit != 0 && limit > total)
				{
					limit = total; // Set limit to total count
				}

				// Calculate the maximum page number based on the limit
				int maxPage = 0;
				if (total != 0 && limit != 0) maxPage = (int)Math.Ceiling((double)total / limit);

				// Adjust the page number if it exceeds the maximum page number
				if (maxPage != 0 && page >= maxPage)
				{
					page = maxPage - 1;
				}

				// Set Limit and Page
				if (limit != 0)
					query = query.Skip(page * limit).Take(limit);

				// Get Data
				var data = await query.ToListAsync();

				// order product alternative
				if (CategoriesID.ToLower() == "bottle" && alternative.ToLower() == "color" && refid != 0)
				{
					data = data.OrderBy(p => p.Name)
						.ToList();
				}
				if (CategoriesID.ToLower() == "bottle" && alternative.ToLower() == "volume" && refid != 0)
				{
					data = data.OrderBy(p => p.Volume.Value)
											.ToList();
				}
				if (CategoriesID.ToLower() == "cup" && alternative.ToLower() == "volume" && refid != 0)
				{
					data = data.OrderBy(p => p.Volume.Value)
											.ToList();
				}
				if ((CategoriesID.ToLower() == "bottle" || CategoriesID.ToLower() == "tray" || CategoriesID.ToLower() == "cup") && alternative.ToLower() == "weight" && refid != 0)
				{
					data = data.OrderBy(p => p.Weight.Value)
											.ToList();
				}
				if (CategoriesID.ToLower() == "bottle" && alternative.ToLower() == "neck" && refid != 0)
				{
					data = data.OrderBy(p => p.Neck)
											.ToList();
				}

				if (CategoriesID.ToLower() == "bottle" && alternative.ToLower() == "reseller" && refid != 0)
				{
					data = data.OrderByDescending(p => p.TotalQty)
											.ToList();
				}

				if (data.Count <= 0 && page > 0)
				{
					page = 0;
					return await GetAlternativeAsync(limit, page, total, search, CategoriesID, alternative, refid, provinceid, provincename, userid);
				}

				return new ListResponse<Product>(data, total, page);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				throw;
			}
		}

		public async Task<ListResponse<Product>> GetSimilarAsync(int limit, int total, int page, string search, string CategoriesID, string similar, int refid, int source, string neck, string rim, string func, int CategoriesIDId, int? userid)
		{
			IQueryable<Product> query = null;

			try
			{
				_context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

				// product similar
				if (CategoriesID.ToLower() == "bottle" && refid != 0)
				{
					//var imageQuery = from calculator in _context.Calculators
					//				 join image in _context.Images on calculator.RefID equals image.ObjectID into imagesGroup
					//				 where calculator.IsDeleted == false
					//					   && calculator.Status == 3
					//					   && calculator.ColorsID == 14
					//					   && imagesGroup.Any(img => img.Type == "Calculators")
					//				 select new
					//				 {
					//					 CalculatorId = calculator.ID,
					//					 RealImages = imagesGroup
					//								   .Where(img => img.Type == "Calculators")
					//								   .Select(img => img.ProductImage)
					//								   .ToList()
					//				 };

					//var images = imageQuery.ToDictionary(x => x.CalculatorId, x => x.RealImages.FirstOrDefault());

					if (CategoriesIDId != 0)
					{
						var getCalculator = from calculator in _context.Calculators
											join color in _context.Colors on calculator.ColorsID equals color.RefID into colors
											from colors_detail in colors.DefaultIfEmpty()
											join material in _context.Materials on calculator.MaterialsID equals material.RefID into materials
											from materials_detail in materials.DefaultIfEmpty()

											let categorys_detail = _context.Categorys.Where(x => x.RefID == calculator.CategoriesID && x.Type == "Bottles").FirstOrDefault()

											let stockindicator = _context.StockIndicators
												.Where(si => calculator.Stock >= si.MinQty && calculator.Stock <= si.MaxQty)
												.FirstOrDefault()
											let wishlist = _context.WishLists
												  .Where(si => calculator.ID == si.ProductId && si.UserId == userid && si.Type == "bottle")
												  .FirstOrDefault()
											let realImage = _context.Images.Where(x => x.ObjectID == calculator.RefID && x.Type == "Calculators")
													 .OrderBy(x => x.RefID)
													 .Select(x => x.ProductImage)
													 .FirstOrDefault()
											let countcolor = (from a in _context.Calculators
															  join b in _context.Colors on a.ColorsID equals b.RefID
															  where a.Status == 3
																  && a.Name.Contains(calculator.Name + ",")
															  select b.Name).Distinct().ToList()
											let qtyCart = (from c in _context.Carts
														   join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
														   from carts_detail in carts.DefaultIfEmpty()
														   where c.CustomersID == userid
															   && carts_detail.ObjectID == calculator.RefID
														   && carts_detail.Type == "bottle"
														   && carts_detail.IsDeleted == false
														   select carts_detail.Qty
															).Sum()
											where calculator.IsDeleted == false
												  && calculator.Status == 3
												  && calculator.ColorsID == 14
												  && calculator.RefID != refid
												  && calculator.CategoriesID == CategoriesIDId
											select new Product
											{
												ID = calculator.ID,
												RefID = calculator.RefID,
												Name = calculator.Name,
												Image = calculator.Image,
												Weight = calculator.Weight,
												Price = calculator.Price,
												CategoriesID = calculator.CategoriesID,
												CategoryName = categorys_detail.Name,
												Stock = calculator.Stock,
												NewProd = calculator.NewProd,
												FavProd = calculator.FavProd,
												ClosuresID = calculator.ClosuresID,
												Volume = calculator.Volume,
												Code = calculator.WmsCode,
												QtyPack = calculator.QtyPack,
												TotalViews = calculator.TotalViews,
												TotalShared = calculator.TotalShared,
												NewProdDate = calculator.NewProdDate,
												Height = calculator.Height,
												Length = calculator.Length,
												CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
												Color = colors_detail.Name,

												RealImage = realImage,

												StockIndicator = calculator.Stock <= 0 ? "Pre Order" : stockindicator.Name,

												PlasticType = materials_detail.PlasticType,
												MaterialsID = calculator.MaterialsID,
												Wishlist = wishlist != null ? 1 : 0,
												QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
												Type = "bottle"
											};
						query = getCalculator;

					}

					if (source != 0)
					{
						var getCalculator = from calculator in _context.Calculators
											join color in _context.Colors on calculator.ColorsID equals color.RefID into colors
											from colors_detail in colors.DefaultIfEmpty()
											join material in _context.Materials on calculator.MaterialsID equals material.RefID into materials
											from materials_detail in materials.DefaultIfEmpty()
												//join function_detail in _context.FunctionDetails on calculator.RefID equals function_detail.ObjectID into function_details
												//from calculator_function_detail in function_details.DefaultIfEmpty()
												//join function in _context.Functions on calculator_function_detail.FunctionsID equals function.RefID into functions
												//from functions_detail in functions.DefaultIfEmpty()
												//join n in _context.Necks on calculator.NecksID equals n.RefID into necks
												//from necks_detail in necks.DefaultIfEmpty()
											let categorys_detail = _context.Categorys.Where(x => x.RefID == calculator.CategoriesID && x.Type == "Bottles").FirstOrDefault()
											let stockindicator = _context.StockIndicators
												.Where(si => calculator.Stock >= si.MinQty && calculator.Stock <= si.MaxQty)
												.FirstOrDefault()
											let realImage = _context.Images.Where(x => x.ObjectID == calculator.RefID && x.Type == "Calculators")
												 .OrderBy(x => x.RefID)
												 .Select(x => x.ProductImage)
												 .FirstOrDefault()
											let countcolor = (from a in _context.Calculators
															  join b in _context.Colors on a.ColorsID equals b.RefID
															  where a.Status == 3
																  && a.Name.Contains(calculator.Name + ",")
															  select b.Name).Distinct().ToList()
											let qtyCart = (from c in _context.Carts
														   join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
														   from carts_detail in carts.DefaultIfEmpty()
														   where c.CustomersID == userid
															   && carts_detail.ObjectID == calculator.RefID
														   && carts_detail.Type == "bottle"
														   && carts_detail.IsDeleted == false
														   select carts_detail.Qty
															).Sum()
											let wishlist = _context.WishLists
												  .Where(si => calculator.ID == si.ProductId && si.UserId == userid && si.Type == "bottle")
												  .FirstOrDefault()
											where calculator.IsDeleted == false
												  && calculator.Status == 3
												  && calculator.ColorsID == 14
												  && calculator.RefID != refid
												  && calculator.Volume == source
											select new Product
											{
												ID = calculator.ID,
												RefID = calculator.RefID,
												Name = calculator.Name,
												Image = calculator.Image,
												Weight = calculator.Weight,
												Price = calculator.Price,
												CategoriesID = calculator.CategoriesID,
												CategoryName = categorys_detail.Name,
												Stock = calculator.Stock,
												NewProd = calculator.NewProd,
												FavProd = calculator.FavProd,
												ClosuresID = calculator.ClosuresID,
												Volume = calculator.Volume,
												Code = calculator.WmsCode,
												QtyPack = calculator.QtyPack,
												TotalViews = calculator.TotalViews,
												TotalShared = calculator.TotalShared,
												NewProdDate = calculator.NewProdDate,
												Height = calculator.Height,
												Length = calculator.Length,
												CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
												Color = colors_detail.Name,

												RealImage = realImage,

												StockIndicator = calculator.Stock <= 0 ? "Pre Order" : stockindicator.Name,

												PlasticType = materials_detail.PlasticType,
												MaterialsID = calculator.MaterialsID,
												Wishlist = wishlist != null ? 1 : 0,

												QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
												Type = "bottle"
											};
						query = getCalculator;

					}

					if (neck != "")
					{
						var getCalculator = from calculator in _context.Calculators
											join color in _context.Colors on calculator.ColorsID equals color.RefID into colors
											from colors_detail in colors.DefaultIfEmpty()
											join material in _context.Materials on calculator.MaterialsID equals material.RefID into materials
											from materials_detail in materials.DefaultIfEmpty()
											join n in _context.Necks on calculator.NecksID equals n.RefID into necks
											from necks_detail in necks.DefaultIfEmpty()
											let categorys_detail = _context.Categorys.Where(x => x.RefID == calculator.CategoriesID && x.Type == "Bottles").FirstOrDefault()
											let stockindicator = _context.StockIndicators
												.Where(si => calculator.Stock >= si.MinQty && calculator.Stock <= si.MaxQty)
												.FirstOrDefault()
											let wishlist = _context.WishLists
												  .Where(si => calculator.ID == si.ProductId && si.UserId == userid && si.Type == "bottle")
												  .FirstOrDefault()
											let realImage = _context.Images.Where(x => x.ObjectID == calculator.RefID && x.Type == "Calculators")
											   .OrderBy(x => x.RefID)
											   .Select(x => x.ProductImage)
											   .FirstOrDefault()
											let countcolor = (from a in _context.Calculators
															  join b in _context.Colors on a.ColorsID equals b.RefID
															  where a.Status == 3
																  && a.Name.Contains(calculator.Name + ",")
															  select b.Name).Distinct().ToList()
											let qtyCart = (from c in _context.Carts
														   join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
														   from carts_detail in carts.DefaultIfEmpty()
														   where c.CustomersID == userid
															   && carts_detail.ObjectID == calculator.RefID
														   && carts_detail.Type == "bottle"
														   && carts_detail.IsDeleted == false
														   select carts_detail.Qty
															).Sum()
											where calculator.IsDeleted == false
												  && calculator.Status == 3
												  && calculator.ColorsID == 14
												  && calculator.RefID != refid
												  && necks_detail.Code.Contains(neck)
											select new Product
											{
												ID = calculator.ID,
												RefID = calculator.RefID,
												Name = calculator.Name,
												Image = calculator.Image,
												Weight = calculator.Weight,
												Price = calculator.Price,
												CategoriesID = calculator.CategoriesID,
												CategoryName = categorys_detail.Name,
												Stock = calculator.Stock,
												NewProd = calculator.NewProd,
												FavProd = calculator.FavProd,
												ClosuresID = calculator.ClosuresID,
												Volume = calculator.Volume,
												Code = calculator.WmsCode,
												QtyPack = calculator.QtyPack,
												TotalViews = calculator.TotalViews,
												TotalShared = calculator.TotalShared,
												NewProdDate = calculator.NewProdDate,
												Height = calculator.Height,
												Length = calculator.Length,
												CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
												Color = colors_detail.Name,
												QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
												RealImage = realImage,

												StockIndicator = calculator.Stock <= 0 ? "Pre Order" : stockindicator.Name,

												PlasticType = materials_detail.PlasticType,
												MaterialsID = calculator.MaterialsID,
												Wishlist = wishlist != null ? 1 : 0,

												Type = "bottle"
											};
						query = getCalculator;

					}

					if (func != "")
					{
						var getCalculator = from calculator in _context.Calculators
											join color in _context.Colors on calculator.ColorsID equals color.RefID into colors
											from colors_detail in colors.DefaultIfEmpty()
											join material in _context.Materials on calculator.MaterialsID equals material.RefID into materials
											from materials_detail in materials.DefaultIfEmpty()
											join function_detail in _context.FunctionDetails on calculator.RefID equals function_detail.ObjectID into function_details
											from calculator_function_detail in function_details.DefaultIfEmpty()
											join function in _context.Functions on calculator_function_detail.FunctionsID equals function.RefID into functions
											from functions_detail in functions.DefaultIfEmpty()
											let categorys_detail = _context.Categorys.Where(x => x.RefID == calculator.CategoriesID && x.Type == "Bottles").FirstOrDefault()
											let stockindicator = _context.StockIndicators
												.Where(si => calculator.Stock >= si.MinQty && calculator.Stock <= si.MaxQty)
												.FirstOrDefault()
											let wishlist = _context.WishLists
												  .Where(si => calculator.ID == si.ProductId && si.UserId == userid && si.Type == "bottle")
												  .FirstOrDefault()
											let realImage = _context.Images.Where(x => x.ObjectID == calculator.RefID && x.Type == "Calculators")
											   .OrderBy(x => x.RefID)
											   .Select(x => x.ProductImage)
											   .FirstOrDefault()
											let qtyCart = (from c in _context.Carts
														   join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
														   from carts_detail in carts.DefaultIfEmpty()
														   where c.CustomersID == userid
															   && carts_detail.ObjectID == calculator.RefID
														   && carts_detail.Type == "bottle"
														   && carts_detail.IsDeleted == false
														   select carts_detail.Qty
															).Sum()
											let countcolor = (from a in _context.Calculators
															  join b in _context.Colors on a.ColorsID equals b.RefID
															  where a.Status == 3
																  && a.Name.Contains(calculator.Name + ",")
															  select b.Name).Distinct().ToList()
											where calculator.IsDeleted == false
												  && calculator.Status == 3
												  && calculator.ColorsID == 14
												  && calculator.RefID != refid
												  && functions_detail.Name.Contains(func) || functions_detail.NameEN.Contains(func)
											select new Product
											{
												ID = calculator.ID,
												RefID = calculator.RefID,
												Name = calculator.Name,
												Image = calculator.Image,
												Weight = calculator.Weight,
												Price = calculator.Price,
												CategoriesID = calculator.CategoriesID,
												CategoryName = categorys_detail.Name,
												Stock = calculator.Stock,
												NewProd = calculator.NewProd,
												FavProd = calculator.FavProd,
												ClosuresID = calculator.ClosuresID,
												Volume = calculator.Volume,
												Code = calculator.WmsCode,
												QtyPack = calculator.QtyPack,
												TotalViews = calculator.TotalViews,
												TotalShared = calculator.TotalShared,
												NewProdDate = calculator.NewProdDate,
												Height = calculator.Height,
												Length = calculator.Length,
												CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
												Color = colors_detail.Name,

												RealImage = realImage,

												StockIndicator = calculator.Stock <= 0 ? "Pre Order" : stockindicator.Name,

												PlasticType = materials_detail.PlasticType,
												MaterialsID = calculator.MaterialsID,
												Wishlist = wishlist != null ? 1 : 0,
												QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
												Type = "bottle"
											};
						query = getCalculator;

					}


					//query = getCalculator;
				}
				else if (CategoriesID.ToLower() == "tray" && refid != 0)
				{
					//var imageQueryThermo = from thermo in _context.Thermos
					//                       join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
					//                       where thermo.IsDeleted == false
					//                                 && thermo.Status == 3
					//                                 && imagesGroup.Any(img => img.Type == "Thermos")
					//                       select new
					//                       {
					//                           ThermoId = thermo.ID,
					//                           RealImages = imagesGroup
					//                                         .Where(img => img.Type == "Thermos")
					//                                         .Select(img => img.ProductImage)
					//                                         .ToList()
					//                       };

					//var images = imageQueryThermo.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

					if (CategoriesIDId != 0)
					{
						var getThermo = from thermo in _context.Thermos
										join color in _context.Colors on thermo.ColorsID equals color.RefID into colors
										from colors_detail in colors.DefaultIfEmpty()
										join material in _context.Materials on thermo.MaterialsID equals material.RefID into materials
										from materials_detail in materials.DefaultIfEmpty()
										let categorys_detail = _context.Categorys.Where(x => x.RefID == thermo.CategoriesID && x.Type == "Thermos").FirstOrDefault()
										let stockindicator = _context.StockIndicators
											.Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
											.FirstOrDefault()
										let wishlist = _context.WishLists
										  .Where(si => thermo.ID == si.ProductId && si.UserId == userid && si.Type == "thermo")
										  .FirstOrDefault()
										let realImage = _context.Images.Where(x => x.ObjectID == thermo.RefID && x.Type == "Thermos")
												   .OrderBy(x => x.RefID)
												   .Select(x => x.ProductImage)
												   .FirstOrDefault()
										let countcolor = (from a in _context.Thermos
														  join b in _context.Colors on a.ColorsID equals b.RefID
														  where a.Status == 3
															  && a.Name.Contains(thermo.Name + ",")
														  select b.Name).Distinct().ToList()
										let qtyCart = (from c in _context.Carts
													   join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
													   from carts_detail in carts.DefaultIfEmpty()
													   where c.CustomersID == userid
												&& carts_detail.ObjectID == thermo.RefID
												&& carts_detail.Type == (thermo.CategoriesID == 5 ? "cup" : "tray")
												&& carts_detail.IsDeleted == false
													   select carts_detail.Qty
														).Sum()
										where thermo.IsDeleted == false
											  && thermo.Status == 3
											  && thermo.ColorsID == 14
											  && thermo.RefID != refid
											  && thermo.CategoriesID == CategoriesIDId
										select new Product
										{
											ID = thermo.ID,
											RefID = thermo.RefID,
											Name = thermo.Name,
											Image = thermo.Image,
											Weight = thermo.Weight,
											Price = thermo.Price,
											CategoriesID = thermo.CategoriesID,
											CategoryName = categorys_detail.Name,
											Stock = thermo.Stock,
											NewProd = thermo.NewProd,
											FavProd = thermo.FavProd,
											LidsID = thermo.LidsID,
											Volume = thermo.Volume,
											Code = thermo.WmsCode,
											QtyPack = thermo.Qty,
											TotalViews = thermo.TotalViews,
											TotalShared = thermo.TotalShared,
											NewProdDate = thermo.NewProdDate,
											Height = thermo.Height,
											Length = thermo.Length,

											Color = colors_detail.Name,
											CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
											RealImage = realImage,

											StockIndicator = thermo.Stock <= 0 ? "Pre Order" : stockindicator.Name,

											PlasticType = materials_detail.PlasticType,
											MaterialsID = thermo.MaterialsID,
											Wishlist = wishlist != null ? 1 : 0,
											QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											Type = thermo.CategoriesID == 5 ? "cup" : "tray"
										};

						query = getThermo;
					}

					if (source != 0)
					{
						var getThermo = from thermo in _context.Thermos
										join color in _context.Colors on thermo.ColorsID equals color.RefID into colors
										from colors_detail in colors.DefaultIfEmpty()
										join material in _context.Materials on thermo.MaterialsID equals material.RefID into materials
										from materials_detail in materials.DefaultIfEmpty()
										let categorys_detail = _context.Categorys.Where(x => x.RefID == thermo.CategoriesID && x.Type == "Thermos").FirstOrDefault()
										let stockindicator = _context.StockIndicators
											.Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
											.FirstOrDefault()
										let wishlist = _context.WishLists
										.Where(si => thermo.ID == si.ProductId && si.UserId == userid && si.Type == "thermo")
										.FirstOrDefault()
										let realImage = _context.Images.Where(x => x.ObjectID == thermo.RefID && x.Type == "Thermos")
												   .OrderBy(x => x.RefID)
												   .Select(x => x.ProductImage)
												   .FirstOrDefault()
										let countcolor = (from a in _context.Thermos
														  join b in _context.Colors on a.ColorsID equals b.RefID
														  where a.Status == 3
															  && a.Name.Contains(thermo.Name + ",")
														  select b.Name).Distinct().ToList()
										let qtyCart = (from c in _context.Carts
													   join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
													   from carts_detail in carts.DefaultIfEmpty()
													   where c.CustomersID == userid
												&& carts_detail.ObjectID == thermo.RefID
												&& carts_detail.Type == (thermo.CategoriesID == 5 ? "cup" : "tray")
												&& carts_detail.IsDeleted == false
													   select carts_detail.Qty
													).Sum()
										where thermo.IsDeleted == false
											  && thermo.Status == 3
											  && thermo.ColorsID == 14
											  && thermo.RefID != refid
											  && thermo.Volume == source
										select new Product
										{
											ID = thermo.ID,
											RefID = thermo.RefID,
											Name = thermo.Name,
											Image = thermo.Image,
											Weight = thermo.Weight,
											Price = thermo.Price,
											CategoriesID = thermo.CategoriesID,
											CategoryName = categorys_detail.Name,
											Stock = thermo.Stock,
											NewProd = thermo.NewProd,
											FavProd = thermo.FavProd,
											LidsID = thermo.LidsID,
											Volume = thermo.Volume,
											Code = thermo.WmsCode,
											QtyPack = thermo.Qty,
											TotalViews = thermo.TotalViews,
											TotalShared = thermo.TotalShared,
											NewProdDate = thermo.NewProdDate,
											Height = thermo.Height,
											Length = thermo.Length,

											Color = colors_detail.Name,
											CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
											RealImage = realImage,

											StockIndicator = thermo.Stock <= 0 ? "Pre Order" : stockindicator.Name,
											QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											PlasticType = materials_detail.PlasticType,
											MaterialsID = thermo.MaterialsID,
											Wishlist = wishlist != null ? 1 : 0,
											Type = thermo.CategoriesID == 5 ? "cup" : "tray"
										};

						query = getThermo;
					}

					if (rim != "")
					{
						var getThermo = from thermo in _context.Thermos
										join color in _context.Colors on thermo.ColorsID equals color.RefID into colors
										from colors_detail in colors.DefaultIfEmpty()
										join material in _context.Materials on thermo.MaterialsID equals material.RefID into materials
										from materials_detail in materials.DefaultIfEmpty()
										join r in _context.Rims on thermo.RimsID equals r.RefID into rims
										from rims_detail in rims.DefaultIfEmpty()
										let categorys_detail = _context.Categorys.Where(x => x.RefID == thermo.CategoriesID && x.Type == "Thermos").FirstOrDefault()
										let stockindicator = _context.StockIndicators
											.Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
											.FirstOrDefault()
										let wishlist = _context.WishLists
											.Where(si => thermo.ID == si.ProductId && si.UserId == userid && si.Type == "thermo")
											.FirstOrDefault()
										let realImage = _context.Images.Where(x => x.ObjectID == thermo.RefID && x.Type == "Thermos")
											   .OrderBy(x => x.RefID)
											   .Select(x => x.ProductImage)
											   .FirstOrDefault()
										let countcolor = (from a in _context.Thermos
														  join b in _context.Colors on a.ColorsID equals b.RefID
														  where a.Status == 3
															  && a.Name.Contains(thermo.Name + ",")
														  select b.Name).Distinct().ToList()
										let qtyCart = (from c in _context.Carts
													   join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
													   from carts_detail in carts.DefaultIfEmpty()
													   where c.CustomersID == userid
												&& carts_detail.ObjectID == thermo.RefID
												&& carts_detail.Type == (thermo.CategoriesID == 5 ? "cup" : "tray")
												&& carts_detail.IsDeleted == false
													   select carts_detail.Qty
														).Sum()
										where thermo.IsDeleted == false
											  && thermo.Status == 3
											  && thermo.ColorsID == 14
											  && thermo.RefID != refid
											  && rims_detail.Name.Contains(rim)
										select new Product
										{
											ID = thermo.ID,
											RefID = thermo.RefID,
											Name = thermo.Name,
											Image = thermo.Image,
											Weight = thermo.Weight,
											Price = thermo.Price,
											CategoriesID = thermo.CategoriesID,
											CategoryName = categorys_detail.Name,
											Stock = thermo.Stock,
											NewProd = thermo.NewProd,
											FavProd = thermo.FavProd,
											LidsID = thermo.LidsID,
											Volume = thermo.Volume,
											Code = thermo.WmsCode,
											QtyPack = thermo.Qty,
											TotalViews = thermo.TotalViews,
											TotalShared = thermo.TotalShared,
											NewProdDate = thermo.NewProdDate,
											Height = thermo.Height,
											Length = thermo.Length,
											CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
											Color = colors_detail.Name,

											RealImage = realImage,

											StockIndicator = thermo.Stock <= 0 ? "Pre Order" : stockindicator.Name,

											PlasticType = materials_detail.PlasticType,
											MaterialsID = thermo.MaterialsID,
											Wishlist = wishlist != null ? 1 : 0,
											QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											Type = thermo.CategoriesID == 5 ? "cup" : "tray"
										};

						query = getThermo;
					}

					if (func != "")
					{
						var getThermo = from thermo in _context.Thermos
										join color in _context.Colors on thermo.ColorsID equals color.RefID into colors
										from colors_detail in colors.DefaultIfEmpty()
										join material in _context.Materials on thermo.MaterialsID equals material.RefID into materials
										from materials_detail in materials.DefaultIfEmpty()
										join function_detail in _context.FunctionDetails on thermo.RefID equals function_detail.ObjectID into function_details
										from calculator_function_detail in function_details.DefaultIfEmpty()
										join function in _context.Functions on calculator_function_detail.FunctionsID equals function.RefID into functions
										from functions_detail in functions.DefaultIfEmpty()
										let categorys_detail = _context.Categorys.Where(x => x.RefID == thermo.CategoriesID && x.Type == "Thermos").FirstOrDefault()
										let stockindicator = _context.StockIndicators
											.Where(si => thermo.Stock >= si.MinQty && thermo.Stock <= si.MaxQty)
											.FirstOrDefault()
										let wishlist = _context.WishLists
											.Where(si => thermo.ID == si.ProductId && si.UserId == userid && si.Type == "thermo")
											.FirstOrDefault()
										let realImage = _context.Images.Where(x => x.ObjectID == thermo.RefID && x.Type == "Thermos")
											   .OrderBy(x => x.RefID)
											   .Select(x => x.ProductImage)
											   .FirstOrDefault()
										let countcolor = (from a in _context.Thermos
														  join b in _context.Colors on a.ColorsID equals b.RefID
														  where a.Status == 3
															  && a.Name.Contains(thermo.Name + ",")
														  select b.Name).Distinct().ToList()
										let qtyCart = (from c in _context.Carts
													   join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
													   from carts_detail in carts.DefaultIfEmpty()
													   where c.CustomersID == userid
												&& carts_detail.ObjectID == thermo.RefID
												&& carts_detail.Type == (thermo.CategoriesID == 5 ? "cup" : "tray")
												&& carts_detail.IsDeleted == false
													   select carts_detail.Qty
														).Sum()
										where thermo.IsDeleted == false
											  && thermo.Status == 3
											  && thermo.ColorsID == 14
											  && thermo.RefID != refid
											  && functions_detail.Name.Contains(func) || functions_detail.NameEN.Contains(func)
										select new Product
										{
											ID = thermo.ID,
											RefID = thermo.RefID,
											Name = thermo.Name,
											Image = thermo.Image,
											Weight = thermo.Weight,
											Price = thermo.Price,
											CategoriesID = thermo.CategoriesID,
											CategoryName = categorys_detail.Name,
											Stock = thermo.Stock,
											NewProd = thermo.NewProd,
											FavProd = thermo.FavProd,
											LidsID = thermo.LidsID,
											Volume = thermo.Volume,
											Code = thermo.WmsCode,
											QtyPack = thermo.Qty,
											TotalViews = thermo.TotalViews,
											TotalShared = thermo.TotalShared,
											NewProdDate = thermo.NewProdDate,
											Height = thermo.Height,
											Length = thermo.Length,
											CountColor = countcolor.Count() < 1 ? 0 : countcolor.Count(),
											Color = colors_detail.Name,

											RealImage = realImage,

											StockIndicator = thermo.Stock <= 0 ? "Pre Order" : stockindicator.Name,
											Wishlist = wishlist != null ? 1 : 0,
											PlasticType = materials_detail.PlasticType,
											MaterialsID = thermo.MaterialsID,
											QtyCart = qtyCart == null ? null : Convert.ToInt64(qtyCart),
											Type = thermo.CategoriesID == 5 ? "cup" : "tray"
										};

						query = getThermo;
					}
				}
				else
				{
					return new ListResponse<Sopra.Entities.Product>(
							data: new List<Sopra.Entities.Product>(),
							total: 0,
							page: 0
						);
				}

				// Searching
				if (!string.IsNullOrEmpty(search))
					query = query.Where(x => x.RefID.ToString().Contains(search)
						|| x.Name.Contains(search)
						);

				// Get Total Before Limit and Page
				total = await query.CountAsync();

				// Adjust limit if it exceeds total count
				if (limit != 0 && limit > total)
				{
					limit = total; // Set limit to total count
				}

				// Calculate the maximum page number based on the limit
				int maxPage = (int)Math.Ceiling((double)total / limit);

				// Adjust the page number if it exceeds the maximum page number
				if (maxPage > 0 && page >= maxPage)
				{
					page = maxPage - 1;
				}

				// Set Limit and Page
				if (limit != 0)
					query = query.Skip(page * limit).Take(limit);

				// Get Data
				var data = await query.Distinct().ToListAsync();
				if (data.Count <= 0 && page > 0)
				{
					page = 0;
					return await GetSimilarAsync(limit, page, total, search, CategoriesID, similar, refid, source, neck, rim, func, CategoriesIDId, userid);
				}

				return new ListResponse<Product>(data, total, page);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				throw;
			}
		}

		public async Task<ListResponse<Product>> GetCompareAsync(int limit, int total, int page, string[] source)
		{
			IQueryable<Product> query = null;
			List<IQueryable<Product>> productQueries = new List<IQueryable<Product>>();
			Dictionary<long, string> imagesBottle = new Dictionary<long, string>();
			Dictionary<long, string> imagesClosures = new Dictionary<long, string>();
			Dictionary<long, string> imagesTray = new Dictionary<long, string>();
			Dictionary<long, string> imagesCup = new Dictionary<long, string>();
			Dictionary<long, string> imagesLid = new Dictionary<long, string>();

			try
			{
				_context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;

				for (int i = 0; i < source.Length; i++)
				{
					var explode = source[i].Split("&");
					var id = int.Parse(explode[0]);
					var type = explode[1];

					IQueryable<Product> productQuery = null;

					if (type.ToLower() == "bottle")
					{
						var imageQueryBottle = from calculator in _context.Calculators
											   join image in _context.Images on calculator.RefID equals image.ObjectID into imagesGroup
											   where calculator.IsDeleted == false
													 && calculator.Status == 3
													 && imagesGroup.Any(img => img.Type == "Calculators")
											   select new
											   {
												   CalculatorId = calculator.ID,
												   RealImages = imagesGroup
																 .Where(img => img.Type == "Calculators")
																 .Select(img => img.ProductImage)
																 .ToList()
											   };

						imagesBottle = imageQueryBottle.ToDictionary(x => x.CalculatorId, x => x.RealImages.FirstOrDefault());

						productQuery = from calculator in _context.Calculators
									   join color in _context.Colors on calculator.ColorsID equals color.RefID
									   join neck in _context.Necks on calculator.NecksID equals neck.RefID
									   join material in _context.Materials on calculator.MaterialsID equals material.RefID
									   where calculator.IsDeleted == false
										   && calculator.Status == 3
										   && calculator.ID == id
									   select new Product
									   {
										   ID = calculator.ID,
										   RefID = calculator.RefID,
										   Name = calculator.Name,
										   Image = calculator.Image,
										   Weight = calculator.Weight,
										   Price = calculator.Price,
										   QtyPack = calculator.QtyPack,
										   Volume = calculator.Volume,
										   Length = calculator.Length,
										   Width = calculator.Width,
										   Height = calculator.Height,
										   Code = calculator.WmsCode,
										   Neck = neck.Code,

										   Color = color.Name,

										   PlasticType = material.PlasticType,
										   MaterialsID = calculator.MaterialsID,

										   Type = "bottle",

									   };

					}
					else if (type.ToLower() == "closures")
					{
						var imageQueryClosures = from closures in _context.Closures
												 join image in _context.Images on closures.RefID equals image.ObjectID into imagesGroup
												 where closures.IsDeleted == false
													   && closures.Status == 3
													   && imagesGroup.Any(img => img.Type == "Closures")
												 select new
												 {
													 ClosuresId = closures.ID,
													 RealImages = imagesGroup
																   .Where(img => img.Type == "Closures")
																   .Select(img => img.ProductImage)
																   .ToList()
												 };

						imagesClosures = imageQueryClosures.ToDictionary(x => x.ClosuresId, x => x.RealImages.FirstOrDefault());

						var productClosures = from closures in _context.Closures
											  join color in _context.Colors on closures.ColorsID equals color.RefID
											  join neck in _context.Necks on closures.NecksID equals neck.RefID
											  where closures.IsDeleted == false
													 && closures.Status == 3
													 && closures.ID == id
											  select new Product
											  {
												  ID = closures.ID,
												  RefID = closures.RefID,
												  Name = closures.Name,
												  Image = closures.Image,
												  Weight = closures.Weight,
												  Price = closures.Price,
												  QtyPack = closures.QtyPack,
												  Height = closures.Height,
												  Diameter = closures.Diameter,
												  Code = closures.WmsCode,
												  Neck = neck.Code,

												  Color = color.Name,

												  PlasticType = null,
												  MaterialsID = 0,

												  Type = "closures"

											  };
					}
					else if (type.ToLower() == "tray")
					{
						var imageQueryTray = from thermo in _context.Thermos
											 join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
											 where thermo.IsDeleted == false
													   && thermo.Status == 3
													   && imagesGroup.Any(img => img.Type == "Thermos")
											 select new
											 {
												 ThermoId = thermo.ID,
												 RealImages = imagesGroup
															   .Where(img => img.Type == "Thermos")
															   .Select(img => img.ProductImage)
															   .ToList()
											 };

						imagesTray = imageQueryTray.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

						productQuery = from thermo in _context.Thermos
									   join color in _context.Colors on thermo.ColorsID equals color.RefID
									   join rim in _context.Rims on thermo.RimsID equals rim.RefID
									   join material in _context.Materials on thermo.MaterialsID equals material.RefID
									   where thermo.IsDeleted == false
											 && thermo.Status == 3
											 && thermo.ID == id
									   select new Product
									   {
										   ID = thermo.ID,
										   RefID = thermo.RefID,
										   Name = thermo.Name,
										   Image = thermo.Image,
										   Weight = thermo.Weight,
										   Price = thermo.Price,
										   QtyPack = thermo.Qty,
										   Volume = thermo.Volume,
										   Length = thermo.Length,
										   Width = thermo.Width,
										   Height = thermo.Height,
										   Code = thermo.WmsCode,
										   Rim = rim.Name,

										   Color = color.Name,

										   PlasticType = material.PlasticType,
										   MaterialsID = thermo.MaterialsID,

										   Type = "tray"

									   };

					}
					else if (type.ToLower() == "cup")
					{
						var imageQueryCup = from thermo in _context.Thermos
											join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
											where thermo.IsDeleted == false
													  && thermo.Status == 3
													  && imagesGroup.Any(img => img.Type == "Thermos")
											select new
											{
												ThermoId = thermo.ID,
												RealImages = imagesGroup
															  .Where(img => img.Type == "Thermos")
															  .Select(img => img.ProductImage)
															  .ToList()
											};

						imagesCup = imageQueryCup.ToDictionary(x => x.ThermoId, x => x.RealImages.FirstOrDefault());

						var productCup = from thermo in _context.Thermos
										 join color in _context.Colors on thermo.ColorsID equals color.RefID
										 join rim in _context.Rims on thermo.RimsID equals rim.RefID
										 join material in _context.Materials on thermo.MaterialsID equals material.RefID
										 where thermo.IsDeleted == false
											   && thermo.Status == 3
											   && thermo.ID == id
										 select new Product
										 {
											 ID = thermo.ID,
											 RefID = thermo.RefID,
											 Name = thermo.Name,
											 Image = thermo.Image,
											 Weight = thermo.Weight,
											 Price = thermo.Price,
											 QtyPack = thermo.Qty,
											 Volume = thermo.Volume,
											 Length = thermo.Length,
											 Width = thermo.Width,
											 Height = thermo.Height,
											 Code = thermo.WmsCode,
											 Rim = rim.Name,

											 Color = color.Name,

											 PlasticType = material.PlasticType,
											 MaterialsID = thermo.MaterialsID,

											 Type = "cup"

										 };
					}
					else
					{
						var imageQueryLid = from lid in _context.Lids
											join image in _context.Images on lid.RefID equals image.ObjectID into imagesGroup
											where lid.IsDeleted == false
													  && lid.Status == 3
													  && imagesGroup.Any(img => img.Type == "Lids")
											select new
											{
												LidId = lid.ID,
												RealImages = imagesGroup
															  .Where(img => img.Type == "Lids")
															  .Select(img => img.ProductImage)
															  .ToList()
											};

						imagesLid = imageQueryLid.ToDictionary(x => x.LidId, x => x.RealImages.FirstOrDefault());

						// var CategoriesIDIds = new List<long> { 6, 8, 9 };

						var productLid = from lid in _context.Lids
										 join color in _context.Colors on lid.ColorsID equals color.RefID
										 join rim in _context.Rims on lid.RimsID equals rim.RefID
										 join material in _context.Materials on lid.MaterialsID equals material.RefID
										 where lid.IsDeleted == false
											   && lid.Status == 3
											   && lid.ID == 3
										 select new Product
										 {
											 ID = lid.ID,
											 RefID = lid.RefID,
											 Name = lid.Name,
											 Image = lid.Image,
											 Weight = lid.Weight,
											 Price = lid.Price,
											 QtyPack = lid.Qty,
											 Length = lid.Length,
											 Width = lid.Width,
											 Height = lid.Height,
											 Code = lid.WmsCode,
											 Rim = rim.Name,

											 Color = color.Name,

											 PlasticType = material.PlasticType,
											 MaterialsID = lid.MaterialsID,

											 Type = "lid"

										 };
					}

					if (productQuery != null)
					{
						productQueries.Add(productQuery);
					}
				}

				var combinedQuery = productQueries.FirstOrDefault();

				if (productQueries.Count > 1)
				{
					combinedQuery = productQueries.Skip(1).Aggregate(combinedQuery, (current, next) => current.Union(next));
				}


				var finalQuery = from product in combinedQuery
								 select new Product
								 {
									 ID = product.ID,
									 RefID = product.RefID,
									 Name = product.Name,
									 Image = product.Image,
									 Weight = product.Weight,
									 Price = product.Price,
									 Color = product.Color,
									 QtyPack = product.QtyPack,
									 Volume = product.Volume,
									 Neck = product.Neck,
									 Rim = product.Rim,
									 Length = product.Length,
									 Width = product.Width,
									 Height = product.Height,
									 RealImage =
										product.Type == "bottle" ?
										imagesBottle.ContainsKey(product.ID) ? imagesBottle[product.ID] : null :

										product.Type == "closures" ?
										imagesClosures.ContainsKey(product.ID) ? imagesClosures[product.ID] : null :

										product.Type == "tray" ?
										imagesTray.ContainsKey(product.ID) ? imagesTray[product.ID] : null :

										product.Type == "cup" ?
										imagesCup.ContainsKey(product.ID) ? imagesCup[product.ID] : null :

										imagesLid.ContainsKey(product.ID) ? imagesLid[product.ID] : null,

									 PlasticType = product.PlasticType,
									 MaterialsID = product.MaterialsID,
									 Code = product.Code,
									 Type = product.Type
								 };

				query = finalQuery;

				// Get Total Before Limit and Page
				total = await query.CountAsync();

				// Adjust limit if it exceeds total count
				if (limit != 0 && limit > total)
				{
					limit = total; // Set limit to total count
				}

				// Calculate the maximum page number based on the limit
				int maxPage = (int)Math.Ceiling((double)total / limit);

				// Adjust the page number if it exceeds the maximum page number
				if (page >= maxPage)
				{
					page = maxPage - 1;
				}

				// Set Limit and Page
				if (limit != 0)
					query = query.Skip(page * limit).Take(limit);

				// Get Data
				var data = await query.ToListAsync();

				if (data.Count <= 0 && page > 0)
				{
					page = 0;
					return await GetCompareAsync(limit, page, total, source);
				}

				return new ListResponse<Product>(data, total, page);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				throw;
			}
		}

		public async Task<ListResponse<ProductStatus>> GetStatusAsync(int limit, int page, string search, string sort, string filter, int total)
		{
			try
			{
				_context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
				var query = from a in _context.ProductStatuses select a;

				// Searching
				if (!string.IsNullOrEmpty(search))
					query = query.Where(x => x.ProductID.ToString().Equals(search)
									|| x.WmsCode.ToString().Contains(search)
									|| x.ProductName.ToString().Contains(search)
									|| x.TotalQty.ToString().Equals(search)
									|| x.ShippingQty.ToString().Equals(search)
									|| x.Outstanding.ToString().Equals(search)
									|| x.DataStock.ToString().Equals(search)
									|| x.StockAvail.ToString().Equals(search)
									|| x.StockStatus.ToString().Contains(search)
									|| x.CountOrder.ToString().Equals(search)
									|| x.WishList.ToString().Equals(search)
									|| x.TotalShared.ToString().Equals(search)
									|| x.TotalViews.ToString().Equals(search)
									|| x.Score.ToString().Equals(search)
									|| x.PrepTime.ToString().Equals(search)
									|| x.ProdTime.ToString().Equals(search)
									|| x.LeadTime.ToString().Equals(search)
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
								"productid" => query.Where(x => x.ProductID.ToString().Equals(value)),
								"wmscode" => query.Where(x => x.WmsCode.ToString().Contains(value)),
								"productname" => query.Where(x => x.ProductName.ToString().Contains(value)),
								"totalqty" => query.Where(x => x.TotalQty.ToString().Equals(value)),
								"shippingqty" => query.Where(x => x.ShippingQty.ToString().Equals(value)),
								"outstanding" => query.Where(x => x.Outstanding.ToString().Equals(value)),
								"datastock" => query.Where(x => x.DataStock.ToString().Equals(value)),
								"stockavail" => query.Where(x => x.StockAvail.ToString().Equals(value)),
								"stockstatus" => query.Where(x => x.StockStatus.ToString().Equals(value)),
								"countorder" => query.Where(x => x.CountOrder.ToString().Equals(value)),
								"wishlist" => query.Where(x => x.WishList.ToString().Equals(value)),
								"totalshared" => query.Where(x => x.TotalShared.ToString().Equals(value)),
								"totalviews" => query.Where(x => x.TotalViews.ToString().Equals(value)),
								"score" => query.Where(x => x.Score.ToString().Equals(value)),
								"preptime" => query.Where(x => x.PrepTime.ToString().Equals(value)),
								"prodtime" => query.Where(x => x.ProdTime.ToString().Equals(value)),
								"leadTime" => query.Where(x => x.LeadTime.ToString().Equals(value)),
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
							"productid" => query.OrderByDescending(x => x.ProductID),
							"wmscode" => query.OrderByDescending(x => x.WmsCode),
							"productname" => query.OrderByDescending(x => x.ProductName),
							"totalqty" => query.OrderByDescending(x => x.TotalQty),
							"shippingqty" => query.OrderByDescending(x => x.ShippingQty),
							"outstanding" => query.OrderByDescending(x => x.Outstanding),
							"datastock" => query.OrderByDescending(x => x.DataStock),
							"stockavail" => query.OrderByDescending(x => x.StockAvail),
							"stockstatus" => query.OrderByDescending(x => x.StockStatus),
							"countorder" => query.OrderByDescending(x => x.CountOrder),
							"wishlist" => query.OrderByDescending(x => x.WishList),
							"totalshared" => query.OrderByDescending(x => x.TotalShared),
							"totalviews" => query.OrderByDescending(x => x.TotalViews),
							"score" => query.OrderByDescending(x => x.Score),
							"preptime" => query.OrderByDescending(x => x.PrepTime),
							"prodtime" => query.OrderByDescending(x => x.ProdTime),
							"leadtime" => query.OrderByDescending(x => x.LeadTime),
							_ => query
						};
					}
					else
					{
						query = orderBy.ToLower() switch
						{
							"productid" => query.OrderBy(x => x.ProductID),
							"wmscode" => query.OrderBy(x => x.WmsCode),
							"productname" => query.OrderBy(x => x.ProductName),
							"totalqty" => query.OrderBy(x => x.TotalQty),
							"shippingqty" => query.OrderBy(x => x.ShippingQty),
							"outstanding" => query.OrderBy(x => x.Outstanding),
							"datastock" => query.OrderBy(x => x.DataStock),
							"stockavail" => query.OrderBy(x => x.StockAvail),
							"stockstatus" => query.OrderBy(x => x.StockStatus),
							"countorder" => query.OrderBy(x => x.CountOrder),
							"wishlist" => query.OrderBy(x => x.WishList),
							"totalshared" => query.OrderBy(x => x.TotalShared),
							"totalviews" => query.OrderBy(x => x.TotalViews),
							"score" => query.OrderBy(x => x.Score),
							"preptime" => query.OrderBy(x => x.PrepTime),
							"prodtime" => query.OrderBy(x => x.ProdTime),
							"leadtime" => query.OrderBy(x => x.LeadTime),
							_ => query
						};
					}
				}
				else
				{
					query = query.OrderByDescending(x => x.ProductID);
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
					return await GetStatusAsync(limit, page, search, sort, filter, total);
				}

				return new ListResponse<ProductStatus>(data, total, page);
			}
			catch (Exception ex)
			{
				Trace.WriteLine(ex.Message);
				if (ex.StackTrace != null)
					Trace.WriteLine(ex.StackTrace);

				throw;
			}

		}

		public async Task<ListResponseFilter<Product>> GetFilterAsync(string CategoriesID, List<long> sub, int filterNew, int filterFavourite, decimal filterStockIndicatorMin, decimal filterStockIndicatorMax, List<long> filterColor, decimal filterVolumeMin, decimal filterVolumeMax, List<decimal> filterVolumeCup, List<long> filterShape, List<long> filterNeck, List<long> filterRim, decimal filterPriceMin, decimal filterPriceMax, decimal filterDiameterMin, decimal filterDiameterMax, decimal? filterWidthMin, decimal filterWidthMax, decimal filterHeightMin, decimal filterHeightMax, List<string> materialType, string lidType)
		{
			IQueryable<Product> query = null;
			IEnumerable<Product> getCalculator = null;
			IEnumerable<Product> getClosures = null;
			IEnumerable<Product> getTray = null;
			IEnumerable<Product> getLid = null;
			var tipe = "";

			try
			{
				_context.ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;


				if (CategoriesID.ToLower() == "bottles" && !sub.Contains(25))
				{
					getCalculator = from product_detail in _context.ProductDetails2
									where product_detail.IsDeleted == false
										  && sub.Contains((long)product_detail.CategoriesID)
										  && product_detail.Type == "bottle"
									select new Product
									{
										ID = (long)product_detail.OriginID,
										RefID = product_detail.RefID,
										Name = product_detail.Name,
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
										Type = product_detail.Type
									};
					query = getCalculator.AsQueryable();
					tipe = "bottle";

				}
				else if (CategoriesID.ToLower() == "bottles" && sub.Contains(25))
				{

					getClosures = await (from product_detail in _context.ProductDetails2
										 where product_detail.IsDeleted == false
											   && sub.Contains((long)product_detail.CategoriesID)
											   && product_detail.Type == "closure"
										 select new Product
										 {
											 ID = (long)product_detail.OriginID,
											 RefID = product_detail.RefID,
											 Name = product_detail.Name,
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
											 RimsID = product_detail.RimsID,
											 NecksID = product_detail.NecksID,
											 Width = product_detail.Width,
											 StockIndicator = product_detail.StockIndicator,
											 PlasticType = product_detail.PlasticType,

											 Type = product_detail.Type
										 }).ToListAsync();
					query = getClosures.AsQueryable();
					tipe = "closure";
				}
				else if (CategoriesID.ToLower() == "thermos" && (sub.Contains(6) && sub.Contains(5) && sub.Contains(4)))
				{

					var getData = await (from product_detail in _context.ProductDetails2
										 where product_detail.IsDeleted == false
											   && product_detail.Type != "bottle"
											   && product_detail.Type != "closure"
										 let thermoCategoriesID = _context.ProductDetails2
															.Where(p => product_detail.RimsID == p.RimsID && (p.Type == "tray" || p.Type == "cup"))
															.FirstOrDefault()
										 select new Product
										 {
											 ID = (long)product_detail.OriginID,
											 RefID = product_detail.RefID,
											 Name = product_detail.Name,
											 Image = product_detail.Image,
											 Weight = product_detail.Weight,
											 RimsID = product_detail.RimsID,
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
											 Type = product_detail.Type
										 }).ToListAsync();
					query = getData.AsQueryable();
					getLid = getData.Where(x => x.Type == "lid");
					getTray = getData.Where(x => x.Type != "lid");
					tipe = System.String.Join(" ", query.Select(x => x.Type).Distinct().ToList());
				}
				else if (CategoriesID.ToLower() == "thermos" && !sub.Contains(6))
				{

					var CategoriesIDIdsTray = new List<long> { 4, 7 };
					var CategoriesIDIdsCup = new List<long> { 5 };

					getTray = await (from product_detail in _context.ProductDetails2
									 where product_detail.IsDeleted == false
										   && (product_detail.Type == "cup" || product_detail.Type == "tray")
											&& ((sub.Contains(4) && sub.Contains(5)) ? (CategoriesIDIdsTray.Contains((long)product_detail.CategoriesID) || CategoriesIDIdsCup.Contains((long)product_detail.CategoriesID)) :
											   sub.Contains(4) ? CategoriesIDIdsTray.Contains((long)product_detail.CategoriesID) :
											   sub.Contains(5) ? CategoriesIDIdsCup.Contains((long)product_detail.CategoriesID) :
											   false)
									 select new Product
									 {
										 ID = (long)product_detail.OriginID,
										 RefID = product_detail.RefID,
										 Name = product_detail.Name,
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
										 RimsID = product_detail.RimsID,
										 Width = product_detail.Width,
										 StockIndicator = product_detail.StockIndicator,
										 PlasticType = product_detail.PlasticType,
										 Type = sub.Contains(5) && !sub.Contains(4) ? "cup" :
												sub.Contains(4) && !sub.Contains(5) ? "tray" :
												(sub.Contains(4) && sub.Contains(5)) ? "tray" :
												null
									 }).ToListAsync();

					query = getTray.AsQueryable();
					tipe = getTray.FirstOrDefault().Type;
				}
				else if (CategoriesID.ToLower() == "thermos" && sub.Contains(6))
				{

					getLid = await (from product_detail in _context.ProductDetails2
									where product_detail.IsDeleted == false
										  && (product_detail.Type == "lid")
									let thermoCategoriesID = _context.ProductDetails2
													   .Where(p => product_detail.RimsID == p.RimsID && (p.Type == "tray" || p.Type == "cup"))
													   .FirstOrDefault()
									select new Product
									{
										ID = (long)product_detail.OriginID,
										RefID = product_detail.RefID,
										Name = product_detail.Name,
										Image = product_detail.Image,
										Weight = product_detail.Weight,
										Price = product_detail.Price,
										CategoriesID = thermoCategoriesID.CategoriesID,
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
										RimsID = product_detail.RimsID,
										Width = product_detail.Width,
										StockIndicator = product_detail.StockIndicator,
										PlasticType = product_detail.PlasticType,
										Type = "lid"
									}).ToListAsync();

					query = getLid.AsQueryable();
					tipe = "lid";
				}
				else
				{
					return null;
				}

				// filter run
				if (CategoriesID.ToLower() == "bottles" && !sub.Contains(25))
				{
					// Apply combined filter for new or favourite products
					if (filterNew != 0 || filterFavourite != 0)
					{
						query = query.Where(p =>
							(filterNew != 0 && p.NewProd != 0) || (filterFavourite != 0 && p.FavProd != 0)
						);
					}

					//filter stock indicator
					if (filterStockIndicatorMin != 0 && filterStockIndicatorMax != 0)
					{
						query = query.Where(p => p.Stock >= filterStockIndicatorMin && p.Stock <= filterStockIndicatorMax);
					}

					//filter color
					if (filterColor != null && filterColor.Any())
					{
						query = query.Where(p => filterColor.Contains(p.ColorsID.GetValueOrDefault()));
					}
					//else
					//{
					//	query = query.Where(p => p.ColorsID == 14);
					//}

					//filter volume
					if (filterVolumeMin != 0 && filterVolumeMax != 0)
					{
						query = query.Where(p => p.Volume >= Math.Floor(filterVolumeMin) && p.Volume <= Math.Round(filterVolumeMax));
					}

					//filter shpae
					if (filterShape != null && filterShape.Any())
					{
						query = query.Where(p => filterShape.Contains(p.ShapesID.GetValueOrDefault()));
					}

					//filter neck
					if (filterNeck != null && filterNeck.Any())
					{
						query = query.Where(p => filterNeck.Contains(p.NecksID.GetValueOrDefault()));
					}

					//filter price
					if (filterPriceMin != 0 && filterPriceMax != 0)
					{
						query = query.Where(p => p.Price >= Math.Floor(filterPriceMin) && p.Price <= Math.Round(filterPriceMax));
					}

					//filter diameter
					if (filterDiameterMin != 0 && filterDiameterMax != 0)
					{
						query = query.Where(p => p.Length >= Math.Floor(filterDiameterMin) && p.Length <= Math.Round(filterDiameterMax));
					}

					//filter width
					if (filterWidthMin != null && filterWidthMax != 0)
					{
						query = query.Where(p => p.Width >= Math.Floor(filterWidthMin.GetValueOrDefault()) && p.Width <= Math.Round(filterWidthMax));
					}

					//filter height
					if (filterHeightMin != 0 && filterHeightMax != 0)
					{
						query = query.Where(p => p.Height >= Math.Floor(filterHeightMin) && p.Height <= Math.Round(filterHeightMax));
					}
				}
				else if (tipe.Contains("closure"))
				{
					//filter color
					if (filterColor != null && filterColor.Any())
					{
						query = query.Where(p => filterColor.Contains(p.ColorsID.GetValueOrDefault()));
					}
					//else
					//{
					//    query = query.Where(p => p.ColorsID == 14);
					//}

					//filter neck
					if (filterNeck != null && filterNeck.Any())
					{
						query = query.Where(p => filterNeck.Contains(p.NecksID.GetValueOrDefault()));
					}

					//filter price
					if (filterPriceMin != 0 && filterPriceMax != 0)
					{
						query = query.Where(p => p.Price >= Math.Floor(filterPriceMin) && p.Price <= Math.Round(filterPriceMax));
					}

					//filter diameter
					if (filterDiameterMin != 0 && filterDiameterMax != 0)
					{
						query = query.Where(p => p.Length >= Math.Floor(filterDiameterMin) && p.Length <= Math.Round(filterDiameterMax));
					}

					//filter height
					if (filterHeightMin != 0 && filterHeightMax != 0)
					{
						query = query.Where(p => p.Height >= Math.Floor(filterHeightMin) && p.Height <= Math.Round(filterHeightMax));
					}
				}
				else if (tipe.Contains("tray") || tipe.Contains("lid") || tipe.Contains("cup"))
				{
					// Apply combined filter for new or favourite products
					if (filterNew != 0 || filterFavourite != 0)
					{
						query = query.Where(p =>
							(filterNew != 0 && p.NewProd != 0) || (filterFavourite != 0 && p.FavProd != 0)
						);
					}

					//filter rim
					if (filterRim != null && filterRim.Any())
					{
						query = query.Where(p => filterRim.Contains(p.RimsID.GetValueOrDefault()));
					}

					//filter price
					if (filterPriceMin != 0 && filterPriceMax != 0)
					{
						query = query.Where(p => p.Price >= Math.Floor(filterPriceMin) && p.Price <= Math.Round(filterPriceMax));
					}

					//filter diameter
					if (filterDiameterMin != 0 && filterDiameterMax != 0)
					{
						query = query.Where(p => p.Length >= Math.Floor(filterDiameterMin) && p.Length <= Math.Round(filterDiameterMax));
					}

					//filter height
					if (filterHeightMin != 0 && filterHeightMax != 0)
					{
						query = query.Where(p => p.Height >= Math.Floor(filterHeightMin) && p.Height <= Math.Round(filterHeightMax));
					}

					//filter volume
					if (tipe.Contains("cup"))
					{
						if (filterVolumeCup != null && filterVolumeCup.Any())
						{
							query = query.Where(p => filterVolumeCup.Contains(Convert.ToInt64(p.Volume.GetValueOrDefault() / 30)));
						}

					}
					else
					{
						if (filterVolumeMin != 0 && filterVolumeMax != 0)
						{
							query = query.Where(p => p.Volume >= Math.Floor(filterVolumeMin) && p.Volume <= Math.Round(filterVolumeMax));
						}
					}


					//filter shpae
					if (filterShape != null && filterShape.Any())
					{
						query = query.Where(p => filterShape.Contains(p.ShapesID.GetValueOrDefault()));
					}

					//filter material type
					if (materialType != null && materialType.Any())
					{
						//query = query.Where(p => p.PlasticType == materialType);
						query = query.Where(p => materialType.Contains(p.PlasticType.ToUpper()));
					}

					//filter tray or cup for lid
					if (lidType != "" && tipe.Contains("lid"))
					{
						if (lidType.ToLower() == "cup") query = query.Where(p => p.CategoriesID == 5);
						if (lidType.ToLower() == "tray") query = query.Where(p => p.CategoriesID != 5 && p.CategoriesID != 6);
					}
				}

				//cek stock indicator
				var distinctStockIndicators = _context.StockIndicators
					.Where(si => si.IsDeleted == false)
					.Select(si => si.Name)
					.Distinct()
					.ToList();

				// cek shape
				var distinctShape = _context.Shapes
					.Where(si => si.IsDeleted == false)
					.Select(si => new { si.Name, si.Type })
					.Distinct()
					.ToList();

				//cek color
				var distinctColors = _context.Colors
					.Where(si => si.IsDeleted == false)
					.Select(si => si.Name)
					.Distinct()
					.ToList();

				//cek color
				var distinctNeck = _context.Necks
					.Where(si => si.IsDeleted == false)
					.Select(si => si.Code)
					.Distinct()
					.ToList();

				//cek rim
				var distinctRim = _context.Rims
					.Where(si => si.IsDeleted == false)
					.Select(si => si.Name)
					.Distinct()
					.ToList();

				var distinctCupVolume = _context.ProductDetails2
					.Where(si => si.Type == "cup")
					.Select(si => Convert.ToInt64((si.Volume / 30)))
					.ToList();

				distinctCupVolume = distinctCupVolume.Distinct().OrderBy(x => x).ToList();

				List<FilterGroup> filterGroups = new List<FilterGroup>();

				if (CategoriesID.ToLower() == "bottles" && !sub.Contains(25))
				{

					filterGroups = new List<FilterGroup>
					{
						new FilterGroup
						{
							GroupName = "Volume",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Volume",
													MinValue = Math.Floor(query.Min(p => p.Volume) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Volume) ?? 0)
												},
							}
						},
						new FilterGroup
						{
							GroupName = "Popular",
							Filter = new List<FilterInfo>
							{
								new FilterInfo { Name = "New Product", Value = 1, Count = query.Count(p => (p.NewProd ?? 0) != 0) },
								new FilterInfo { Name = "Favourite Product", Value = 1, Count = query.Count(p => (p.FavProd ?? 0) != 0) },
							}.Where(filterInfo => filterInfo.Count > 0).ToList()
						},
						new FilterGroup
						{
							GroupName = "Stock Indicator",
							Filter = distinctStockIndicators.Select(stockIndicatorValue =>
							{
								var stockIndicatorInfo = _context.StockIndicators
									.Where(si => si.Name == stockIndicatorValue)
									.FirstOrDefault();

								int count = query.Count(p =>
										p.StockIndicator == stockIndicatorValue &&
										p.Stock >= stockIndicatorInfo.MinQty &&
										p.Stock <= stockIndicatorInfo.MaxQty);

								return count > 0 ? new FilterInfo
								{
									Name = stockIndicatorValue,
									MinValue = stockIndicatorInfo?.MinQty,
									MaxValue = stockIndicatorInfo?.MaxQty,
									Count = count
								}: null;
							})
							.Where(filterInfo => filterInfo != null)
							.ToList()
						},
						new FilterGroup
						{
							GroupName = "Shape",
							Filter = distinctShape.Select(shapeValue =>
							{
								var shapeInfo = _context.Shapes
									.Where(si => si.Name == shapeValue.Name && si.Type == shapeValue.Type)
									.FirstOrDefault();

								int count = getCalculator.Count(p => p.ShapesID == shapeInfo.RefID);

								 return count > 0 ? new FilterInfo
								{
									Name = shapeValue.Name,
									ID = shapeInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						},
						new FilterGroup
						{
							GroupName = "Color",
							Filter = distinctColors.Select(colorValue =>
							{
								var colorInfo = _context.Colors
									.Where(si => si.Name == colorValue)
									.FirstOrDefault();

								int count = getCalculator.Count(p => p.ColorsID == colorInfo.RefID);

								 return count > 0 ? new FilterInfo
								{
									Name = colorInfo.Name,
									ID = colorInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						},
						new FilterGroup
						{
							GroupName = "Neck",
							Filter = distinctNeck.Select(neckValue =>
							{
								var neckInfo = _context.Necks
									.Where(si => si.Code == neckValue)
									.FirstOrDefault();

								int count = getCalculator.Count(p => p.NecksID == neckInfo.RefID);

								 return count > 0 ? new FilterInfo
								{
									Name = neckInfo.Code,
									ID = neckInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						},
						new FilterGroup
						{
							GroupName = "Diameter",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Diameter",
													MinValue = Math.Floor(query.Min(p => p.Length) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Length) ?? 0)
												},
							}
						},
						new FilterGroup
						{
							GroupName = "Width",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Width",
													MinValue = Math.Floor(query.Min(p => p.Width) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Width) ?? 0)
												},
							}
						},
						//new FilterGroup
						//{
						//	GroupName = "Height",
						//	Filter = new List<FilterInfo>
						//	{
						//		new FilterInfo {
						//							Name = "Height",
						//							MinValue = query.Min(p => p.Height),
						//							MaxValue = query.Max(p => p.Height)
						//						},
						//	}
						//},
						new FilterGroup
						{
							GroupName = "Price",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Price",
													MinValue = query.Min(p => p.Price),
													MaxValue = query.Max(p => p.Price)
												},
							}
						},
					};

				}
				else if (tipe.Contains("closure"))
				{
					filterGroups = new List<FilterGroup>
					{
                        //new FilterGroup
                        //{
                        //    GroupName = "Popular",
                        //    Filter = new List<FilterInfo>
                        //    {
                        //        new FilterInfo { Name = "New Product", Value = 1, Count = query.Count(p => (p.NewProd ?? 0) != 0) },
                        //        new FilterInfo { Name = "Favourite Product", Value = 1, Count = query.Count(p => (p.FavProd ?? 0) != 0) },
                        //    }.Where(filterInfo => filterInfo.Count > 0).ToList()
                        //},
                        new FilterGroup
						{
							GroupName = "Color",
							Filter = distinctColors.Select(colorValue =>
							{
								var colorInfo = _context.Colors
									.Where(si => si.Name == colorValue)
									.FirstOrDefault();

								int count = getClosures.Count(p => p.ColorsID == colorInfo.RefID );

								 return count > 0 ? new FilterInfo
								{
									Name = colorInfo.Name,
									ID = colorInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						},
						new FilterGroup
						{
							GroupName = "Neck",
							Filter = distinctNeck.Select(neckValue =>
							{
								var neckInfo = _context.Necks
									.FirstOrDefault(si => si.Code == neckValue);

								int count = getClosures.Count(p => p.NecksID == neckInfo.RefID  );

								 return count > 0 ? new FilterInfo
								{
									Name = neckInfo.Code,
									ID = neckInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						},
                        //new FilterGroup
                        //{
                        //    GroupName = "Body Dimension",
                        //    Filter = new List<FilterInfo>
                        //    {
                        //        new FilterInfo {
                        //                            Name = "Body Diameter",
                        //                            MinValue = Math.Floor(query.Min(p => p.Diameter) ?? 0),
                        //                            MaxValue = Math.Round(query.Max(p => p.Diameter) ?? 0)
                        //                        },
                        //        new FilterInfo {
                        //                            Name = "Height",
                        //                            MinValue = Math.Floor(query.Min(p => p.Height) ?? 0),
                        //                            MaxValue = Math.Round(query.Max(p => p.Height) ?? 0)
                        //                        },
                        //    }
                        //},
						new FilterGroup
						{
							GroupName = "Body Diameter",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Body Diameter",
													MinValue = Math.Floor(query.Min(p => p.Diameter) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Diameter) ?? 0)
												},
							}
						},
						new FilterGroup
						{
							GroupName = "Height",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Height",
													MinValue = Math.Floor(query.Min(p => p.Height) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Height) ?? 0)
												},
							}
						},
						new FilterGroup
						{
							GroupName = "Price",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Price",
													MinValue = query.Min(p => p.Price),
													MaxValue = query.Max(p => p.Price)
												},
							}
						},
					};
				}
				else if (tipe.Contains("tray") || tipe.Contains("lid") || tipe.Contains("cup"))
				{

					filterGroups = new List<FilterGroup>
					{
						new FilterGroup
						{
							GroupName = "Material Type",
							Filter = new List<FilterInfo>
							{
								new FilterInfo { Name = "PET"},
								new FilterInfo { Name = "PP" },
							}
						},
						new FilterGroup
						{
							GroupName = "Rim",
							Filter = distinctRim.Select(RimValue =>
							{
								var RimInfo = _context.Rims
									.FirstOrDefault(si => si.Name == RimValue);
								int count = 0;
								if (tipe == "tray" ) count = getTray.Count(p => p.RimsID == RimInfo.RefID );
								if (tipe == "lid" ) count = getLid.Count(p => p.RimsID == RimInfo.RefID);

								 return count > 0 ? new FilterInfo
								{
									Name = RimInfo.Name,
									ID = RimInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						},
						new FilterGroup
						{
							GroupName = "Popular",
							Filter = new List<FilterInfo>
							{
								new FilterInfo { Name = "New Product", Value = 1, Count = query.Count(p => (p.NewProd ?? 0) != 0) },
								new FilterInfo { Name = "Favourite Product", Value = 1, Count = query.Count(p => (p.FavProd ?? 0) != 0) },
							}.Where(filterInfo => filterInfo.Count > 0).ToList()
						},
                        //new FilterGroup
                        //{
                        //    GroupName = "Body Dimension",
                        //    Filter = new List<FilterInfo>
                        //    {
                        //        new FilterInfo {
                        //                            Name = "Body Diameter",
                        //                            MinValue = Math.Floor(query.Min(p => p.Length) ?? 0),
                        //                            MaxValue = Math.Round(query.Max(p => p.Length) ?? 0)
                        //                        },
                        //        new FilterInfo {
                        //                            Name = "Height",
                        //                            MinValue = Math.Floor(query.Min(p => p.Height) ?? 0),
                        //                            MaxValue = Math.Round(query.Max(p => p.Height) ?? 0)
                        //                        },
                        //    }
                        //},
                        new FilterGroup
						{
							GroupName = "Body Diameter",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Body Diameter",
													MinValue = Math.Floor(query.Min(p => p.Length) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Length) ?? 0)
												},
							}
						},
						new FilterGroup
						{
							GroupName = "Height",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Height",
													MinValue = Math.Floor(query.Min(p => p.Height) ?? 0),
													MaxValue = Math.Round(query.Max(p => p.Height) ?? 0)
												},
							}
						},
						new FilterGroup
						{
							GroupName = "Price",
							Filter = new List<FilterInfo>
							{
								new FilterInfo {
													Name = "Price",
													MinValue = query.Min(p => p.Price),
													MaxValue = query.Max(p => p.Price)
												},
							}
						},

					};

					if (tipe.Contains("cup"))
					{
						filterGroups.Insert(0, new FilterGroup
						{
							GroupName = "Volume",
							Filter = distinctCupVolume.Select(cupVolumeValue =>
							{
								//var volumeInfo = _context.Shapes
								//    .FirstOrDefault(si => si.Name == shapeValue.Name && si.Type == shapeValue.Type);

								int count = getTray.Count(p => Convert.ToInt64(p.Volume / 30) == cupVolumeValue);

								return count > 0 ? new FilterInfo
								{
									Name = string.Format(@"{0} Oz ({1} ml)", cupVolumeValue, Convert.ToInt64(cupVolumeValue * 30)),
									Value = cupVolumeValue,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						});

						//               filterGroups.Add(new FilterGroup
						//               {
						//                   GroupName = "Volume",
						//                   Filter = distinctCupVolume.Select(cupVolumeValue =>
						//                   {
						//                       //var volumeInfo = _context.Shapes
						//                       //    .FirstOrDefault(si => si.Name == shapeValue.Name && si.Type == shapeValue.Type);

						//                       int count = getTray.Count(p => p.Volume == cupVolumeValue);

						//                       return count > 0 ? new FilterInfo
						//                       {
						//                           Name = string.Format(@"{0} Oz ({1} ml)", Convert.ToInt64((cupVolumeValue / 30)), Convert.ToInt64(cupVolumeValue)),
						//Value = Convert.ToInt64(cupVolumeValue),
						//                           Count = count
						//                       } : null;
						//                   })
						//                    .Where(filterInfo => filterInfo != null)
						//                   .ToList()
						//               });

						filterGroups.Add(new FilterGroup
						{
							GroupName = "Shape",
							Filter = distinctShape.Select(shapeValue =>
							{
								var shapeInfo = _context.Shapes
									.FirstOrDefault(si => si.Name == shapeValue.Name && si.Type == shapeValue.Type);

								int count = getTray.Count(p => p.ShapesID == shapeInfo.RefID);

								return count > 0 ? new FilterInfo
								{
									Name = shapeValue.Name,
									ID = shapeInfo?.RefID,
									Count = count
								} : null;
							})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
						});
					}

					if (tipe.Contains("tray"))
					{
						if (!filterGroups.Any(x => x.GroupName == "Shape"))
						{
							filterGroups.Add(new FilterGroup
							{
								GroupName = "Shape",
								Filter = distinctShape.Select(shapeValue =>
								{
									var shapeInfo = _context.Shapes
										.FirstOrDefault(si => si.Name == shapeValue.Name && si.Type == shapeValue.Type);

									int count = getTray.Count(p => p.ShapesID == shapeInfo.RefID);

									return count > 0 ? new FilterInfo
									{
										Name = shapeValue.Name,
										ID = shapeInfo?.RefID,
										Count = count
									} : null;
								})
							 .Where(filterInfo => filterInfo != null)
							.ToList()
							});
						}
					}

					if (tipe.Contains("lid"))
					{
						filterGroups.Add(new FilterGroup
						{
							GroupName = "Lid Type",
							Filter = new List<FilterInfo>
							{
								new FilterInfo { Name = "Cup",Count = getLid.Count(x => x.CategoriesID == 5)},
								new FilterInfo { Name = "Tray",Count = getLid.Count(x => x.CategoriesID != 5 && x.CategoriesID != 6)},
							}
						});
					}

				}
				var filters = filterGroups.ToList();

				return new ListResponseFilter<Product>(filters);

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
