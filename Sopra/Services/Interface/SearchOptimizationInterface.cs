using Sopra.Entities;
using Sopra.Responses;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace Sopra.Services
{
    public interface SearchOptimizationInterface
    {
        Task<DataRowCollection> GetSearchFunctionOrTag(string param);
        Task<(DataRowCollection dtc, string tipe, int total)> GetSearch(string param, int limit, int page);
        string[] RotateArray(string[] array, int positions);
        string ExplodeQueryString(string word);
        DataTable insertProductDetail(string param, string type, string productKey = "");
        string[] RemoveElement(string[] array, string element);
        string[] getSeasonalName();
        Task<object> GetRealImage(string type, long refid);
        Task<object> GetCountColor(string type, string name);
        Task<long> GetQtyCart(string type, long refid, long userid);
        Task<string> GetColor(long colorid);
        Task<(List<ProductDetail2> data, int total)> GetRecommended(string tipe);
        Task<List<ProductDetail2>> GetDataSeasonal(string param, int userid);
        Task<string> GetCategoryType(string name);
    }
}
