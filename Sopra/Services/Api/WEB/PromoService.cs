using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Sopra.Helpers;
using Sopra.Responses;
using Sopra.Entities;
using System.Data;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using System.Runtime.Serialization;

namespace Sopra.Services
{
    public interface PromosInterface
    {
        Task<ListResponse<Promos>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date);
        Task<ListResponse<dynamic>> GetPendingPromoApproval();
    }

    public class PromosService : PromosInterface
    {
        private readonly EFContext _context;

        public PromosService(EFContext context)
        {
            _context = context;
        }

        public Task<ListResponse<Promos>> GetAllAsync(int limit, int page, int total, string search, string sort, string filter, string date)
        {
            try
            {
                var query = string.Format(@"
                    EXEC spCreateProductDetails '{0}','{1}','{2}'
                ", "ECOM_GetPromoMix", "", "");

                var result = Utility.SQLGetObjects(query, Utility.SQLDBConnection);

                if (result != null && result.Rows.Count > 0)
                {
                    DataView dataView = result.DefaultView;
                    dataView.Sort = "PromoName ASC";

                    result = dataView.ToTable();
                }

                var promosList = new List<Promos>();

                if (result != null && result.Rows.Count > 0)
                {
                    var rawDataList = new List<Dictionary<string, object>>();
                    foreach (DataRow row in result.Rows)
                    {
                        var dict = new Dictionary<string, object>();
                        foreach (DataColumn col in result.Columns)
                        {
                            dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col]; // Handle DBNull
                        }
                        rawDataList.Add(dict);
                    }

                    var groupedByPromo = rawDataList.GroupBy(item => Convert.ToInt64(item["RefID"]));

                    foreach (var promoGroup in groupedByPromo)
                    {
                        var firstItemInGroup = promoGroup.First();

                        var promo = new Promos
                        {
                            ID = Convert.ToInt64(firstItemInGroup["RefID"]),
                            RefID = Convert.ToInt64(firstItemInGroup["RefID"]),
                            Name = Convert.ToString(firstItemInGroup["PromoName"]),
                            Image = Convert.ToString(firstItemInGroup["Image"]),
                            StartDate = firstItemInGroup["StartDate"] != null ? Convert.ToDateTime(firstItemInGroup["StartDate"]) : (DateTime?)null,
                            EndDate = firstItemInGroup["EndDate"] != null ? Convert.ToDateTime(firstItemInGroup["EndDate"]) : (DateTime?)null,
                            Products = new List<Products>(),
                            Quantities = new Quantities
                            {
                                Qty1 = firstItemInGroup["Qty1"] != null ? Convert.ToInt64(firstItemInGroup["Qty1"]) : (long?)null,
                                Qty2 = firstItemInGroup["Qty2"] != null ? Convert.ToInt64(firstItemInGroup["Qty2"]) : (long?)null,
                                Qty3 = firstItemInGroup["Qty3"] != null ? Convert.ToInt64(firstItemInGroup["Qty3"]) : (long?)null
                            }
                        };

                        var productsInPromoGroup = promoGroup.GroupBy(pItem => Convert.ToInt64(pItem["ProductsID"]));
                        foreach (var productGroup in productsInPromoGroup)
                        {
                            var product = new Products
                            {
                                ProductID = productGroup.Key,
                                Details = new List<Details>()
                            };

                            foreach (var detailRow in productGroup)
                            {
                                product.Details.Add(new Details
                                {
                                    Accs1ID = detailRow["Accs1Id"] != null ? Convert.ToInt64(detailRow["Accs1Id"]) : (long?)null,
                                    Accs2ID = detailRow["Accs2Id"] != null ? Convert.ToInt64(detailRow["Accs2Id"]) : (long?)null,
                                    Price1 = detailRow["Price1"] != null ? Convert.ToInt64(detailRow["Price1"]) : (long?)null,
                                    Price2 = detailRow["Price2"] != null ? Convert.ToInt64(detailRow["Price2"]) : (long?)null,
                                    Price3 = detailRow["Price3"] != null ? Convert.ToInt64(detailRow["Price3"]) : (long?)null
                                });
                            }

                            promo.Products.Add(product);
                        }
                        promosList.Add(promo);
                    }
                }

                return Task.FromResult(new ListResponse<Promos>(promosList, promosList.Count, page));
            }
            catch (Exception ex)
            {
                Trace.WriteLine(ex.Message);
                if (ex.StackTrace != null)
                    Trace.WriteLine(ex.StackTrace);

                throw;
            }
        }


        public async Task<ListResponse<dynamic>> GetPendingPromoApproval()
        {
            var query = (from o in _context.Orders
                        join od in _context.OrderDetails on o.ID equals od.OrdersID
                        join p in _context.Promos on od.PromosID equals p.RefID
                        join pd in _context.PromoProducts on p.RefID equals pd.PromoMixId
                        join pq in _context.PromoQuantities on p.RefID equals pq.PromoMixId
                        where od.Type == "Mix" && o.OrderStatus != "CANCEL"
                        select new
                        {
                            OrdersID = o.ID,
                            DetailID = od.ID,
                            Type = od.Type,
                            ProductID = od.ObjectID,
                            PromoName = p.Name
                        });

            var total = await query.CountAsync();
            var data = await query.ToListAsync();

            var resData = data.Select(x =>
            {
                return new
                {
                    OrdersID = x.OrdersID,
                    DetailID = x.DetailID,
                    Type = x.Type,
                    ProductID = x.ProductID,
                    PromoName = x.PromoName
                };
            })
            .ToList();
                
            return new ListResponse<dynamic>(resData, total, 0);
        }
    }
}