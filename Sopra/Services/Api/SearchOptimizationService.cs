using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Sopra.Entities;
using Sopra.Helpers;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Sopra.Services
{
    public static class ListExtensions
    {
        private static Random rng = new Random();

        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    public class SearchOptimizationService : SearchOptimizationInterface
    {
        private readonly EFContext _context;
        private readonly IConfiguration config;
        public SearchOptimizationService(EFContext context, IConfiguration config)
        {
            _context = context;
            config = config;
        }

        public string ExplodeQueryString(string word)
        {
            string pattern = @"'%([^%]*)%'";
            MatchCollection matches = Regex.Matches(word, pattern);

            // Initialize a list to hold the extracted parts
            var parts = new List<string>();

            // Loop through the matches and add the captured groups to the list
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    parts.Add(match.Groups[1].Value);
                }
            }

            // Join the parts with spaces
            string result = string.Join(" ", parts);
            return result;
        }

        public DataTable insertProductDetail(string param, string type, string productKey = "")
        {
            var query = string.Format(@"
                    EXEC spCreateProductDetails '{0}','{1}','{2}'
                ", param, type, productKey);

            var result = Utility.SQLGetObjects(query, Utility.SQLDBConnection);

            // Check if result has rows to sort
            if (result != null && result.Rows.Count > 0)
            {
                // Use DataView to apply sorting on the DataTable
                DataView dataView = result.DefaultView;
                dataView.Sort = "Type ASC"; // Specify your column sorting here

                // Return the sorted DataTable
                return dataView.ToTable();
            }

            return result;
        }

        public async Task<string> GetColor(long colorid)
        {
            var color = await _context.Colors.Where(x => x.RefID == colorid).Select(x => x.Name).FirstOrDefaultAsync();
            if (color != null) return color;
            return "";
        }

        public async Task<long> GetQtyCart(string type, long refid, long userid)
        {
            if (type.ToLower().Equals("tray") || type.ToLower().Equals("cup") || type.ToLower().Equals("bottle"))
            {
                var qtyCart = Convert.ToInt64(await (from c in _context.Carts
                                                     join cd in _context.CartDetails on c.ID equals cd.CartsID into carts
                                                     from carts_detail in carts.DefaultIfEmpty()
                                                     where c.CustomersID == userid
                                                         && carts_detail.ObjectID == refid
                                                     && carts_detail.Type == type
                                                     && carts_detail.IsDeleted == false
                                                     select carts_detail.Qty
                            ).SumAsync());
                return qtyCart;
            }
            return 0;

        }

        public async Task<List<ProductDetail2>> GetDataSeasonal(string param, int userid)
        {
            var result = await (from msc in _context.SectionCategorys

                                join msck in _context.SectionCategoryKeys on msc.RefID equals msck.SectionCategoriesID into msckJoin
                                from msck in msckJoin.DefaultIfEmpty()

                                join mpc in _context.ProductCategorys on msck.PoductCategoriesID equals mpc.RefID into mpcJoin
                                from mpc in mpcJoin.DefaultIfEmpty()

                                join mpc2 in _context.ProductCategoriesKeywords on mpc.RefID equals mpc2.ProductCategoriesID into mpc2Join
                                from mpc2 in mpc2Join.DefaultIfEmpty()

                                join mpck in _context.ProductKeywords on mpc2.ProductKeywordID equals mpck.ID into mpckJoin
                                from mpck in mpckJoin.DefaultIfEmpty()

                                where mpc.Name == param && msc.Status == "Active"
                                select mpck.Name).ToListAsync();

            result = result.Where(name => name != null).ToList();

            if (result.Count > 0)
            {
                string likeClause = string.Join(" OR ", result.Select(term => $"Name LIKE '%{term}%'"));
                var matchingProducts = _context.ProductDetails2
                                      .FromSqlRaw($"SELECT * FROM ProductDetails2 WHERE {likeClause} ORDER BY Volume")
                                      .ToList();
                foreach (var item in matchingProducts)
                {
                    var neck = await _context.Necks.Where(x => x.RefID == item.NecksID).Select(x => x.Code).FirstOrDefaultAsync();
                    var rim = await _context.Rims.Where(x => x.RefID == item.RimsID).Select(x => x.Name).FirstOrDefaultAsync();
                    var color = await _context.Colors.Where(x => x.RefID == item.ColorsID).Select(x => x.Name).FirstOrDefaultAsync();

                    item.QtyCart = await GetQtyCart(item.Type, Convert.ToInt64(item.RefID), userid);
                    item.Code = item.WmsCode;
                    item.Neck = Convert.ToString(neck);
                    item.Rim = Convert.ToString(rim);
                    item.Color = Convert.ToString(color);
                }

                if (matchingProducts != null) return matchingProducts;
            }

            return new List<ProductDetail2>();
        }

        public string[] RemoveElement(string[] array, string element)
        {
            int index = Array.IndexOf(array, element);

            if (index >= 0)
            {
                string[] newArray = new string[array.Length - 1];
                if (index > 0)
                {
                    Array.Copy(array, 0, newArray, 0, index);
                }
                if (index < array.Length - 1)
                {
                    Array.Copy(array, index + 1, newArray, index, array.Length - index - 1);
                }
                return newArray;
            }
            return array;
        }

        public string[] getSeasonalName()
        {
            // Assuming _context is your DbContext and Categories is your DbSet
            var categoriesData = _context.ProductCategorys.Select(c => c.Name).ToList<string>().ToArray();

            // Convert to array and return
            return categoriesData;
        }

        public async Task<(DataRowCollection dtc, string tipe, int total)> GetSearch(string param, int limit, int page, ProductKey productKey = null)
        {
            // add dictionary
            Dictionary<string, string> dictionary = new Dictionary<string, string>();
            dictionary.Add("mliter", "ml");
            dictionary.Add("mltr", "ml");
            dictionary.Add("mlltr", "ml");
            dictionary.Add("mililiter", "ml");
            dictionary.Add("mili liter", "ml");
            dictionary.Add("liter", "ml");
            dictionary.Add("ltr", "ml");
            dictionary.Add("lit", "ml");
            dictionary.Add("L", "ml");
            dictionary.Add("l", "ml");

            dictionary.Add("botol", "bottle");
            dictionary.Add("btl", "bottle");
            dictionary.Add("btol", "bottle");
            dictionary.Add("botl", "bottle");
            dictionary.Add("bottle", "bottle");
            dictionary.Add("Bottle", "bottle");

            dictionary.Add("tray", "tray");
            dictionary.Add("Tray", "tray");
            dictionary.Add("try", "tray");
            dictionary.Add("teray", "tray");
            dictionary.Add("tary", "tray");

            dictionary.Add("cup", "cup");
            dictionary.Add("Cup", "cup");

            dictionary.Add("lid", "lid");
            dictionary.Add("Lid", "lid");

            dictionary.Add("silinder", "cylinder");

            //lower case
            param = param.ToLower();

            // check param contains cup and ml
            if (param.ToLower().Contains("cup") && param.ToLower().Contains("ml"))
            {
                param = param.ToLower().Replace("ml", "oz");
                //param = param.Replace("ML", "oz");
                //param = param.Replace("Ml", "oz");
            }

            int[] ozSize = new int[] { 8, 10, 12, 14, 16, 18, 20, 22, 24 };
            string[] arr = new string[10];
            string[] condition = new string[10];
            string[] words = param.Split(' ');
            string join = "";
            Regex rNumeric = new Regex("^[0-9]*$");
            Regex rNumericWithDotComma = new Regex("^[0-9],.*$");
            Regex rLetter = new Regex("^[a-zA-Z]*$");
            Regex r = new Regex("^[a-zA-Z0-9]*$");
            int find = 0;
            int findOz = 0;
            bool changed = false;
            bool arrChange = false;
            string type = "";
            string firstParam = "";
            int militerConverter = 1000;
            bool ozChange = false;
            for (int i = 0; i < words.Length; i++)
            {
                // check isalphanum
                if (r.IsMatch(words[i]) && !rNumeric.IsMatch(words[i]) && !rLetter.IsMatch(words[i]) && words[i].ToLower() != "d88")
                {
                    arr = Regex.Matches(words[i], @"\D+|\d+")
                                     .Cast<Match>()
                                     .Select(m => m.Value)
                                     .ToArray();
                    find = i;
                }

                // check volume
                if (dictionary.ContainsKey(words[i]))
                {
                    // check param contains comma number and liter
                    if ((words[i] == "liter" || words[i] == "ltr" || words[i] == "lit" || words[i] == "l" || words[i] == "L") && rNumericWithDotComma.IsMatch(words[i - 1]))
                    {
                        if (words[i - 1].Contains(',') || words[i - 1].Contains('.'))
                        {
                            words[i - 1] = words[i - 1].Replace(',', '.');
                            words[i - 1] = Convert.ToString(Convert.ToInt32(Convert.ToDecimal(words[i - 1]) * militerConverter));
                        }
                        else words[i - 1] = Convert.ToString(Convert.ToInt32(words[i - 1]) * militerConverter);
                        changed = true;
                    }

                    // check param contains liter
                    if ((words[i] == "liter" || words[i] == "ltr" || words[i] == "lit" || words[i] == "l" || words[i] == "L") && !changed)
                    {
                        if (words[i - 1].Contains(',') || words[i - 1].Contains('.'))
                        {
                            words[i - 1] = words[i - 1].Replace(',', '.');
                            words[i - 1] = Convert.ToString(Convert.ToInt32(Convert.ToDecimal(words[i - 1]) * militerConverter));
                        }
                        else words[i - 1] = Convert.ToString(Convert.ToInt32(words[i - 1]) * militerConverter);
                    }

                    // check arr equal cup and doesnt contains oz
                    if (words[i] == "cup" && !words.Contains("oz"))
                    {
                        words[i] = dictionary[words[i]];
                        arrChange = true;
                    }

                    // check arr contains dictionary
                    if (dictionary.ContainsKey(words[i]) && !arrChange && !words.Contains("cup") && !words.Contains("oz")) words[i] = dictionary[words[i]];
                    if (words[i] == "bottle" || words[i] == "lid" || words[i] == "cup" || words[i] == "tray" || words[i] == "lid") type = words[i];
                }

                // calculation for oz
                if (words[i] == "oz" && words.Contains("cup"))
                {
                    //if param contain oz and check size oz is exists or not
                    if (ozSize.Any(x => x == Convert.ToInt32(words[i - 1])))
                    {
                        continue;
                    }
                    else
                    {
                        if (words[i - 1].Contains(',') || words[i - 1].Contains('.'))
                        {
                            words[i - 1] = words[i - 1].Replace(',', '.');
                            words[i - 1] = Convert.ToString(Convert.ToInt32(Convert.ToDecimal(words[i - 1]) / 30));
                        }
                        else words[i - 1] = Convert.ToString(Convert.ToInt32(words[i - 1]) / 30);
                        findOz = i - 1;
                        ozChange = true;
                    }
                }
            }

            // find the nearest size of oz 
            if (words.Contains("oz") && words.Contains("cup") && ozChange)
            {
                for (int i = 0; i < ozSize.Length; i++)
                {
                    if (Convert.ToInt32(words[findOz]) <= ozSize[i])
                    {
                        words[findOz] = ozSize[i].ToString();
                        break;
                    }
                }
            }

            // remove element if found the type
            if (type != "")
            {
                if (words.Length > 1)
                {
                    words = RemoveElement(words, type);
                    find -= 1;
                }
            }

            if (type == "")
            {
                var categoryType = await GetCategoryType(param);
                type = categoryType == "" ? "" : categoryType;
            }


            //if withoud space -> 250ml
            if (arr.Count() != 10)
            {
                if (dictionary.ContainsKey(arr[1]))
                {
                    if (rNumeric.IsMatch(arr[0]) && (arr[1] == "liter" || arr[1] == "ltr") || arr[1] == "lit" || arr[1] == "l" || arr[1] == "L")
                    {
                        arr[0] = Convert.ToString(Convert.ToInt32(arr[0]) * militerConverter);
                    }
                    arr[1] = dictionary[arr[1]];
                }
                join = string.Join(" ", arr);
            }
            if (join != "")
            {
                words[find] = join;
                condition = words.Select(m => $"Name LIKE '%{m}%'").ToArray();
                join = string.Join(" AND ", condition);
                if (type != "") join = join + " AND Type ='" + type + "'";
                //join = join.Replace(" ", "%");
                ////param = join;
            }
            else
            {
                condition = words.Select(m => $"Name LIKE '%{m}%'").ToArray();
                join = string.Join(" AND ", condition);
                if (type != "") join = join + " AND Type ='" + type + "'";
            }
            param = join;
            firstParam = string.Join(" ", words);

            //var result = Utility.SQLGetObjects(query, Utility.SQLDBConnection);

            Utility.ConnectSQL(Utility.SQL_Server, Utility.SQL_Database, Utility.SQL_UserID, Utility.SQL_Password);
            var result = insertProductDetail(firstParam, type, productKey.ProductKeys);

            if (result != null)
            {

                if (Convert.ToInt32(result.Rows[0]["Flag"]) == 1)
                {
                    var rows = result.AsEnumerable().Skip(page * limit).Take(limit);
                    var totalData = result.Rows.Count;
                    if (limit != 0)
                    {
                        DataTable pagedData = rows.Any() ? rows.CopyToDataTable() : result.Clone();
                        if (pagedData.Rows.Count > 0) result = pagedData;
                    }
                    if (type != "")
                    {
                        DataRow[] foundTypeRows = result.Select($"Type = '{type}'");
                        if (foundTypeRows.Length != 0)
                        {
                            DataTable filteredTable = result.Clone();
                            foreach (DataRow row in foundTypeRows)
                            {
                                filteredTable.ImportRow(row); // Import each found row
                            }
                            return (dtc: await Task.FromResult(filteredTable.Rows), tipe: type, total: totalData);
                        }
                    }
                    return (dtc: await Task.FromResult(result.Rows), tipe: type, total: totalData);
                }
                else
                { // data not found
                    try
                    {
                        for (int i = 1; i <= words.Count(); i++)
                        {
                            DataRow[] foundRows = result.Select(param);
                            if (foundRows.Length != 0)
                            {
                                DataTable filteredTable = result.Clone();
                                foreach (DataRow row in foundRows)
                                {
                                    filteredTable.ImportRow(row); // Import each found row
                                }
                                var totalData = filteredTable.Rows.Count;
                                var rows = filteredTable.AsEnumerable().Skip(page * limit).Take(limit);
                                if (limit != 0)
                                {
                                    DataTable pagedData = rows.Any() ? rows.CopyToDataTable() : filteredTable.Clone();
                                    if (pagedData.Rows.Count > 0) filteredTable = pagedData;
                                }

                                return (dtc: await Task.FromResult(filteredTable.Rows), tipe: type, total: totalData);
                            }
                            join = string.Join(" AND ", RotateArray(words, i));
                            param = join;
                            if (ExplodeQueryString(param) == firstParam)
                            {
                                DataRow[] foundFirstWordParamRows = result.Select($"Name LIKE '%{firstParam.Split(' ')[0]}%' AND Type = '{type}'");
                                if (foundFirstWordParamRows.Length != 0)
                                {
                                    DataTable filteredTable = result.Clone();
                                    foreach (DataRow row in foundFirstWordParamRows)
                                    {
                                        filteredTable.ImportRow(row); // Import each found row
                                    }
                                    var totalData = filteredTable.Rows.Count;
                                    var rows = filteredTable.AsEnumerable().Skip(page * limit).Take(limit);
                                    if (limit != 0)
                                    {
                                        DataTable pagedData = rows.Any() ? rows.CopyToDataTable() : filteredTable.Clone();
                                        if (pagedData.Rows.Count > 0) filteredTable = pagedData;
                                    }

                                    return (dtc: await Task.FromResult(filteredTable.Rows), tipe: type, total: totalData);
                                }
                                else
                                {
                                    int stop = 0;
                                    DataRow[] foundRecommendedRows = result.Select($"Type = '{type}'");
                                    if (foundRecommendedRows.Length != 0)
                                    {
                                        DataTable filteredTable = result.Clone();
                                        foreach (DataRow row in foundRecommendedRows)
                                        {
                                            if (stop == 10) break;
                                            filteredTable.ImportRow(row); // Import each found row
                                            stop++;
                                        }
                                        return (dtc: await Task.FromResult(filteredTable.Rows), tipe: type, total: stop);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine($"Error : {ex.Message}");
                    }
                }

            }

            //var result = await _context.Calculators.Where(x => x.Name.Contains(param)).ToListAsync();
            //if(result.Count() > 0) return result;
            //var result = await _context.Database.(query);
            //if (result != null) return result;
            return (dtc: null, tipe: type, total: 0);
        }

        public string[] RotateArray(string[] array, int positions)
        {
            int length = array.Length;
            string[] rotatedArray = new string[length];
            string[] condition = new string[10];

            for (int i = 0; i < length; i++)
            {
                int newIndex = (i + positions) % length;
                rotatedArray[newIndex] = array[i];
            }

            condition = rotatedArray.Select(m => $"Name LIKE '%{m}%'").ToArray();

            return condition;
        }

        public async Task<DataRowCollection> GetSearchFunctionOrTag(string param)
        {
            var query = string.Format(@"
                    select *
                    from ProductDetails2 pd
                    where Functions like '%{0}%'
                    or Tags like '%{0}%'
                    order by CategoriesID,Name 
                ", param);



            Utility.ConnectSQL(Utility.SQL_Server, Utility.SQL_Database, Utility.SQL_UserID, Utility.SQL_Password);
            var result = Utility.SQLGetObjects(query, Utility.SQLDBConnection);
            //DataRow[] rows = result.Rows;
            if (result != null)
            {
                return await Task.FromResult(result.Rows);
            }

            return null;
        }

        public async Task<object> GetRealImage(string type, long refid)
        {
            if (type.ToLower() == "bottle")
            {
                // return await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                var imageQuery = await (from calculator in _context.Calculators
                                        join image in _context.Images on calculator.RefID equals image.ObjectID into imagesGroup
                                        where calculator.IsDeleted == false
                                              && imagesGroup.Any(img => img.Type == "Calculators")
                                              && calculator.RefID == refid
                                              && calculator.Status == 3
                                        select new
                                        {
                                            RealImages = imagesGroup
                                                          .Where(img => img.Type == "Calculators")
                                                          .Select(img => img.ProductImage)
                                                          .FirstOrDefault()
                                        }).FirstOrDefaultAsync();
                return imageQuery == null ? null : imageQuery.RealImages;
            }
            else if (type.ToLower() == "closure")
            {
                var imageQuery = await (from closures in _context.Closures
                                        join image in _context.Images on closures.RefID equals image.ObjectID into imagesGroup
                                        where closures.IsDeleted == false
                                              && closures.Status == 3
                                              && closures.RefID == refid
                                              && imagesGroup.Any(img => img.Type == "Closures")
                                        select new
                                        {
                                            ClosuresId = closures.ID,
                                            RealImages = imagesGroup
                                                          .Where(img => img.Type == "Closures")
                                                          .Select(img => img.ProductImage)
                                                          .FirstOrDefault()
                                        }).FirstOrDefaultAsync();
                return imageQuery == null ? null : imageQuery.RealImages;
            }
            else if (type.ToLower() == "tray" || type.ToLower() == "cup")
            {
                var imageQuery = await (from thermo in _context.Thermos
                                        join image in _context.Images on thermo.RefID equals image.ObjectID into imagesGroup
                                        where thermo.IsDeleted == false
                                                  && thermo.Status == 3
                                                  && thermo.RefID == refid
                                                  && imagesGroup.Any(img => img.Type == "Thermos")
                                        select new
                                        {
                                            ThermoId = thermo.ID,
                                            RealImages = imagesGroup
                                                          .Where(img => img.Type == "Thermos")
                                                          .Select(img => img.ProductImage)
                                                          .FirstOrDefault()
                                        }).FirstOrDefaultAsync();
                return imageQuery == null ? null : imageQuery.RealImages;
            }
            else if (type.ToLower() == "lid")
            {
                var imageQueryLid = await (from lid in _context.Lids
                                           join image in _context.Images on lid.RefID equals image.ObjectID into imagesGroup
                                           where lid.IsDeleted == false
                                                     && lid.Status == 3
                                                     && lid.RefID == refid
                                                     && imagesGroup.Any(img => img.Type == "Lids")
                                           select new
                                           {
                                               LidId = lid.ID,
                                               RealImages = imagesGroup
                                                             .Where(img => img.Type == "Lids")
                                                             .Select(img => img.ProductImage)
                                                             .FirstOrDefault()
                                           }).FirstOrDefaultAsync();
                return imageQueryLid == null ? null : imageQueryLid.RealImages;
            }
            return null;
        }

        public async Task<object> GetCountColor(string type, string name)
        {
            if (type.ToLower() == "bottle")
            {
                // return await _context.Products.AsNoTracking().FirstOrDefaultAsync(x => x.ID == id && x.IsDeleted == false);
                var Countcolor = await (from calculator in _context.Calculators
                                        join color in _context.Colors on calculator.ColorsID equals color.RefID
                                        where calculator.Name.Contains(name + ",")
                                              && calculator.Status == 3
                                        select color.Name).ToListAsync();
                return Countcolor.Count() < 1 ? 0 : Countcolor.Count();
            }
            else if (type.ToLower() == "closure")
            {
                var Countcolor = await (from closures in _context.Closures
                                        join color in _context.Colors on closures.ColorsID equals color.RefID
                                        where closures.Name.Contains(name + ",")
                                              && closures.Status == 3
                                        select color.Name).ToListAsync();
                return Countcolor.Count() < 1 ? 0 : Countcolor.Count();
            }
            else if (type.ToLower() == "thermo")
            {
                var Countcolor = await (from thermo in _context.Thermos
                                        join color in _context.Colors on thermo.ColorsID equals color.RefID
                                        where thermo.Name.Contains(name + ",")
                                              && thermo.Status == 3
                                        select color.Name).ToListAsync();
                return Countcolor.Count() < 1 ? 0 : Countcolor.Count();
            }
            else if (type.ToLower() == "lid")
            {
                var Countcolor = await (from lid in _context.Lids
                                        join color in _context.Colors on lid.ColorsID equals color.RefID
                                        where lid.Name.Contains(name + ",")
                                              && lid.Status == 3
                                        select color.Name).ToListAsync();
                return Countcolor.Count() < 1 ? 0 : Countcolor.Count();
            }
            return null;
        }

        public async Task<string> GetCategoryType(string name)
        {
            var dataCategory = await _context.Categorys.Where(x => x.Name.Contains(name)).FirstOrDefaultAsync();
            if (dataCategory != null)
            {
                if (dataCategory.Type == "Bottles" && dataCategory.Name != "Closures") return "bottle";
                else if (dataCategory.Type == "Bottles" && dataCategory.Name == "Closures") return "closure";
                else
                {
                    if (dataCategory.Type == "Thermos" && dataCategory.Name == "Clamshell") return "";
                    else return dataCategory.Name.ToLower();
                }
            }
            return "";
        }

        public async Task<(List<ProductDetail2> data, int total)> GetRecommended(string tipe)
        {
            var query1 = await (from pd in _context.ProductDetails2
                                join c in _context.Calculators on pd.RefID equals c.RefID into gj
                                from subC in gj.DefaultIfEmpty()
                                let stockindicator = _context.StockIndicators
                                                     .Where(si => pd.Stock >= si.MinQty && pd.Stock <= si.MaxQty)
                                                     .FirstOrDefault()
                                where subC.Status == 3 && subC.ColorsID == 14 && pd.FavProd == 1 && pd.Type == "bottle"
                                select new ProductDetail2
                                {
                                    Type = pd.Type,
                                    OriginID = pd.OriginID,
                                    RefID = pd.RefID,
                                    Name = pd.Name,
                                    NewProd = pd.NewProd,
                                    FavProd = pd.FavProd,
                                    Image = pd.Image,
                                    Weight = pd.Weight,
                                    Price = pd.Price,
                                    ClosuresID = pd.ClosuresID,
                                    CategoriesID = pd.CategoriesID,
                                    PlasticType = pd.PlasticType,
                                    Functions = pd.Functions,
                                    Tags = pd.Tags,
                                    StockIndicator = pd.Stock <= 0 ? "Pre Order" : stockindicator.Name,
                                    RealImage = pd.RealImage
                                }).ToListAsync();

            // Query 2
            var query2 = await (from pd in _context.ProductDetails2
                                join t in _context.Thermos on pd.RefID equals t.RefID into gj
                                from subT in gj.DefaultIfEmpty()
                                let stockindicator = _context.StockIndicators
                                                     .Where(si => pd.Stock >= si.MinQty && pd.Stock <= si.MaxQty)
                                                     .FirstOrDefault()
                                where subT.Status == 3 && pd.FavProd == 1 && (pd.Type == "cup" || pd.Type == "tray")
                                select new ProductDetail2
                                {
                                    Type = pd.Type,
                                    OriginID = pd.OriginID,
                                    RefID = pd.RefID,
                                    Name = pd.Name,
                                    NewProd = pd.NewProd,
                                    FavProd = pd.FavProd,
                                    Image = pd.Image,
                                    Weight = pd.Weight,
                                    Price = pd.Price,
                                    ClosuresID = pd.ClosuresID,
                                    CategoriesID = pd.CategoriesID,
                                    PlasticType = pd.PlasticType,
                                    Functions = pd.Functions,
                                    Tags = pd.Tags,
                                    RealImage = pd.RealImage,
                                    StockIndicator = pd.Stock <= 0 ? "Pre Order" : stockindicator.Name,
                                }).ToListAsync();


            // Combine results
            var combinedResult = query1.Union(query2).ToList();

            if (tipe != "") combinedResult.Where(x => x.Type == tipe).ToList();

            // Shuffle the combined result
            combinedResult.Shuffle();
            var total = combinedResult.Count;

            // Take the first 10 results after shuffling
            if (combinedResult != null) return (combinedResult.Take(20).ToList(), total);
            return (null, 0);
        }
    }
}
