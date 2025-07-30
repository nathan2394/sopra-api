using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sopra.Entities;
using Sopra.Requests;
using Sopra.Responses;

namespace Sopra.Services
{
	public interface IServiceAsync<T>
	{
		Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, string search, string sort,
		string filter, string date);
		Task<T> GetByIdAsync(long id);
		Task<T> CreateAsync(T data);
		Task<T> EditAsync(T data);
		Task<bool> DeleteAsync(long id, long userID);
		Task<T> ChangePassword(ChangePassword obj, long id);
        Task<List<VTransactionOrderDetail>> GetTransactionOrderDetailAsync(long orderid);
        //Task<List<AccsExt>> GetAccIdAsync(long id,long customerid);
        Task<List<T>> GetAccIdAsync<T>(long id, long customerId,long masterId,string objectType,long objectId) where T : class;
    }

	public interface IServiceProdAsync<T>
	{
		Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, string search, string sort,
	   string filter, string date, string type, string order, int? userid,string category,int categoriesId,int productId);
		Task<T> GetByIdAsync(string type, long id, int? userid);
        Task<long> GetIncreaseShare(string type, long refid);
        Task<ListResponse<T>> GetSearchAsync(int limit, int total, int page, string content, string sort, string order);
		Task<ListResponseProduct<T>> GetListAsync(int limit, int total, int page, string search, string sort, string type, string category, List<long> sub, int filterNew, int filterFavourite, decimal filterStockIndicatorMin, decimal filterStockIndicatorMax, List<long> filterColor, decimal filterVolumeMin, decimal filterVolumeMax, List<long> filterShape, List<long> filterNeck, List<long> filterRim, decimal filterPriceMin, decimal filterPriceMax, decimal filterDiameterMin, decimal filterDiameterMax, decimal? filterWidthMin, decimal filterWidthMax, decimal filterHeightMin, decimal filterHeightMax,string materialType,string lidType, int? userid);
        //Task<ListResponseProduct<T>> GetListNewAsync(int limit, int total, int page, string search, string sort, string type, string category, List<long> sub, int filterNew, int filterFavourite, decimal filterStockIndicatorMin, decimal filterStockIndicatorMax, List<long> filterColor, decimal filterVolumeMin, decimal filterVolumeMax, List<long> filterShape, List<long> filterNeck, List<long> filterRim, decimal filterPriceMin, decimal filterPriceMax, decimal filterDiameterMin, decimal filterDiameterMax, decimal? filterWidthMin, decimal filterWidthMax, decimal filterHeightMin, decimal filterHeightMax, string materialType, string lidType, int? userid,int isReset);
        Task<ListResponseProduct<T>> GetListNewAsync(int limit, int total, int page, string searchKey, string searchProduct, string category, List<long> sub, string sort, string type, int filterNew, int filterFavourite, decimal filterStockIndicatorMin, decimal filterStockIndicatorMax, List<long> filterColor, decimal filterVolumeMin, decimal filterVolumeMax, List<long> filterShape, List<long> filterNeck, List<long> filterRim, decimal filterPriceMin, decimal filterPriceMax, decimal filterDiameterMin, decimal filterDiameterMax, decimal? filterWidthMin, decimal filterWidthMax, decimal filterHeightMin, decimal filterHeightMax, List<string> materialType, string lidType, int? userid, List<decimal> filterVolumeCup);
        Task<ListResponseProduct<ProductGroup>> GetProductCategory(int? userid, bool flagDetail);
        Task<ListResponse<T>> GetAlternativeAsync(int limit, int total, int page, string search, string category, string alternative, int refid, int? provinceid, string provincename, int? userid);
		Task<ListResponse<T>> GetSimilarAsync(int limit, int total, int page, string search, string category, string similar, int refid, int source, string neck,string rim,string func, int categoryId, int? userid);
		Task<ListResponse<T>> GetCompareAsync(int limit, int total, int page, string[] source);
		Task<ListResponseFilter<T>> GetFilterAsync(string category, List<long> sub, int filterNew, int filterFavourite, decimal filterStockIndicatorMin, decimal filterStockIndicatorMax, List<long> filterColor, decimal filterVolumeMin, decimal filterVolumeMax, List<decimal> filterVolumeCup, List<long> filterShape, List<long> filterNeck, List<long> filterRim, decimal filterPriceMin, decimal filterPriceMax, decimal filterDiameterMin, decimal filterDiameterMax, decimal? filterWidthMin, decimal filterWidthMax, decimal filterHeightMin, decimal filterHeightMax, List<string> materialType, string lidType);
        Task<ListResponse<ProductStatus>> GetStatusAsync(int limit , int page , string search, string sort, string filter,int total);
        

    }

	public interface IServiceResellerAsync<T>
	{
		Task<ListResponse<T>> GetResellerAsync(int limit, int total, int page, string search, int provinceid);
		Task<T> GetByIdAsync(long id);
	}

    public interface IServiceGcpAsync<T>
    {
        Task<ListResponse<T>> GetAllAsync(int limit, int page, int total, string search, string sort,
        string filter, string date, string fileName);
        Task<T> GetByIdAsync(long id);
        Task<T> CreateAsync(T data);
        Task<T> EditAsync(T data);
        Task<bool> DeleteAsync(long id, long userID);
    }


}