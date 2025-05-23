using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

using Sopra.Entities;
using Sopra.Responses;
using Sopra.Services;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ProductController : ControllerBase
    {
        private readonly IServiceProdAsync<Product> _service;

        public ProductController(IServiceProdAsync<Product> service)
        {
            _service = service;
        }

        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> Get(int limit = 0, int page = 0, string search = "", string sort = "", string order = "", string filter = "", string date = "", string type = "", int? userid = null, string category="",int categoryId=0,int productId=0)
        {
            try
            {
                var total = 0;
                var result = await _service.GetAllAsync(limit, page, total, search, sort, order, filter, date, type, userid, category, categoryId, productId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    message = inner.Message;
                    inner = inner.InnerException;
                }
                Trace.WriteLine(message, "ProductController");
                return BadRequest(new { message });
            }
        }

        [HttpPost("increase-shared")]
        public async Task<IActionResult> GetIncreaseShared(int refid = 0, string type = "")
        {
            try
            {
                var result = await _service.GetIncreaseShare(type, refid);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    message = inner.Message;
                    inner = inner.InnerException;
                }
                Trace.WriteLine(message, "ProductController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        //    [HttpGet("list")]
        //    public async Task<IActionResult> GetList(int limit = 0, int page = 0, string search = "", string sort = "", string type = "", string category = "", [FromQuery] long[] sub = null, int filterNew = 0, int filterFavourite = 0, decimal filterStockIndicatorMin = 0, decimal filterStockIndicatorMax = 0, [FromQuery] long[] filterColor = null, decimal filterVolumeMin = 0, decimal filterVolumeMax = 0, [FromQuery] long[] filterShape = null, [FromQuery] long[] filterNeck = null, [FromQuery] long[] filterRim = null, decimal filterPriceMin = 0, decimal filterPriceMax = 0, decimal filterDiameterMin = 0, decimal filterDiameterMax = 0, decimal? filterWidthMin = null, decimal filterWidthMax = 0, decimal filterHeightMin = 0, decimal filterHeightMax = 0, string materialType="", string lidType = "", int userid=0)
        //    {
        //        try
        //        {
        //sub ??= Array.Empty<long>();
        //filterColor ??= Array.Empty<long>();
        //filterShape ??= Array.Empty<long>();
        //filterNeck ??= Array.Empty<long>();
        //var total = 0;
        //            var result = await _service.GetListAsync(limit, total, page, search, sort, type, category, sub?.ToList() ?? new List<long>(), filterNew, filterFavourite, filterStockIndicatorMin, filterStockIndicatorMax, filterColor?.ToList() ?? new List<long>(), filterVolumeMin, filterVolumeMax, filterShape?.ToList() ?? new List<long>(), filterNeck?.ToList() ?? new List<long>(), filterRim?.ToList() ?? new List<long>(), filterPriceMin, filterPriceMax, filterDiameterMin, filterDiameterMax, filterWidthMin, filterWidthMax, filterHeightMin, filterHeightMax,materialType, lidType, userid);
        //            return Ok(result);
        //        }
        //        catch (Exception ex)
        //        {
        //            var message = ex.Message;
        //            var inner = ex.InnerException;
        //            while (inner != null)
        //            {
        //                message = inner.Message;
        //                inner = inner.InnerException;
        //            }
        //            Trace.WriteLine(message, "ProductController");
        //            return BadRequest(new { message });
        //        }
        //    }

        //[Authorize]
        [HttpGet("list")]
        public async Task<IActionResult> GetListNew(int limit = 0, int page = 0, string searchKey = "", string searchProduct = "", string category= "", [FromQuery] long[] sub = null, string sort = "", string type = "", int filterNew = 0, int filterFavourite = 0, decimal filterStockIndicatorMin = 0, decimal filterStockIndicatorMax = 0, [FromQuery] long[] filterColor = null, decimal filterVolumeMin = 0, decimal filterVolumeMax = 0, [FromQuery] long[] filterShape = null, [FromQuery] long[] filterNeck = null, [FromQuery] long[] filterRim = null, decimal filterPriceMin = 0, decimal filterPriceMax = 0, decimal filterDiameterMin = 0, decimal filterDiameterMax = 0, decimal? filterWidthMin = null, decimal filterWidthMax = 0, decimal filterHeightMin = 0, decimal filterHeightMax = 0, [FromQuery] string[] materialType = null, string lidType = "", int userid = 0, [FromQuery] decimal[] filterVolumeCup = null)
        {
            try
            {
                sub ??= Array.Empty<long>();
                filterColor ??= Array.Empty<long>();
                filterVolumeCup ??= Array.Empty<decimal>();
                filterShape ??= Array.Empty<long>();
                filterNeck ??= Array.Empty<long>();
                materialType ??= Array.Empty<string>();
                var total = 0;
                var result = await _service.GetListNewAsync(limit, total, page, searchKey, searchProduct, category, sub?.ToList() ?? new List<long>(), sort, type,filterNew, filterFavourite, filterStockIndicatorMin, filterStockIndicatorMax, filterColor?.ToList() ?? new List<long>(), filterVolumeMin, filterVolumeMax, filterShape?.ToList() ?? new List<long>(), filterNeck?.ToList() ?? new List<long>(), filterRim?.ToList() ?? new List<long>(), filterPriceMin, filterPriceMax, filterDiameterMin, filterDiameterMax, filterWidthMin, filterWidthMax, filterHeightMin, filterHeightMax, materialType?.ToList() ?? new List<string>(), lidType, userid, filterVolumeCup?.ToList() ?? new List<decimal>());
                return Ok(result);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    message = inner.Message;
                    inner = inner.InnerException;
                }
                Trace.WriteLine(message, "ProductController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        [HttpGet("category")]
        public async Task<IActionResult> GetProductCategory(int userid = 0,int flagging=0)
        {
            try
            {
                bool flag = flagging == 0 ? false : true;
                var result = await _service.GetProductCategory(userid, flag);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    message = inner.Message;
                    inner = inner.InnerException;
                }
                Trace.WriteLine(message, "ProductController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        [HttpGet("detail/{type}/{id}")]
        public async Task<IActionResult> GetById(string type, int id, int userid = 0)
        {
            try
            {
                var result = await _service.GetByIdAsync(type, id,userid);
                if (result == null)
                    return BadRequest(new { message = "Invalid ID" });

                var response = new Response<Product>(result);
                return Ok(response);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    message = inner.Message;
                    inner = inner.InnerException;
                }
                Trace.WriteLine(message, "ProductController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        [HttpGet("search/{content}")]
        public async Task<IActionResult> GetSearch(int limit = 0, int page = 0, string content = "", string sort = "", string order = "")
        {
            try
            {
                var total = 0;
                var result = await _service.GetSearchAsync(limit, total, page, content, sort, order);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    message = inner.Message;
                    inner = inner.InnerException;
                }
                Trace.WriteLine(message, "ProductController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        [HttpGet("alternative")]
        public async Task<IActionResult> GetAlternative(int limit = 0, int page = 0, string search = "", string category = "", string alternative = "", int refid = 0, int? provinceid = null, string? provincename = null, int userid = 0)
        {
            try
            {
                var total = 0;
                var result = await _service.GetAlternativeAsync(limit, total, page, search, category, alternative, refid, provinceid, provincename,userid);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    message = inner.Message;
                    inner = inner.InnerException;
                }
                Trace.WriteLine(message, "ProductController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        [HttpGet("similar")]
        public async Task<IActionResult> GetSimilar(int limit = 0, int page = 0, string search = "", string category = "", string similar = "", int refid = 0, int source = 0,string neck ="",string rim ="",string function="", int categoryId=0,int userid=0)
        {
            try
            {
                var total = 0;
                var result = await _service.GetSimilarAsync(limit, total, page, search, category, similar, refid, source,neck,rim, function, categoryId, userid);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    message = inner.Message;
                    inner = inner.InnerException;
                }
                Trace.WriteLine(message, "ProductController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        [HttpGet("compare")]
        public async Task<IActionResult> GetCompare(int limit = 0, int page = 0, [FromQuery] string[] source = null)
        {
            try
            {
                source ??= Array.Empty<string>();
                if (source == null || source.Length < 2)
                {
                    return BadRequest(new { message = "Invalid. It should contain at least 2 items." });
                }
                else if (source.Length > 5)
                {
                    return BadRequest(new { message = "Invalid. It should contain at most 5 items." });
                }
                var total = 0;
                var result = await _service.GetCompareAsync(limit, total, page, source);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    message = inner.Message;
                    inner = inner.InnerException;
                }
                Trace.WriteLine(message, "ProductController");
                return BadRequest(new { message });
            }
        }

		//[Authorize]
		[HttpGet("filter")]
        public async Task<IActionResult> GetFilter(string category = "", [FromQuery] long[] sub = null, int filterNew = 0, int filterFavourite = 0, decimal filterStockIndicatorMin = 0, decimal filterStockIndicatorMax = 0, [FromQuery] long[] filterColor = null, decimal filterVolumeMin = 0, decimal filterVolumeMax = 0, [FromQuery] decimal[] filterVolumeCup = null, [FromQuery] long[] filterShape = null, [FromQuery] long[] filterNeck = null, [FromQuery] long[] filterRim = null, decimal filterPriceMin = 0, decimal filterPriceMax = 0, decimal filterDiameterMin = 0, decimal filterDiameterMax = 0, decimal? filterWidthMin = null, decimal filterWidthMax = 0, decimal filterHeightMin = 0, decimal filterHeightMax = 0, [FromQuery] string[] materialType = null, string lidType = "")
        {
			try
			{
                //var total = 0;
                sub ??= Array.Empty<long>();
                filterVolumeCup ??= Array.Empty<decimal>();
                filterColor ??= Array.Empty<long>();
                filterShape ??= Array.Empty<long>();
                filterNeck ??= Array.Empty<long>();
                materialType ??= Array.Empty<string>();
                var result = await _service.GetFilterAsync(category, sub?.ToList() ?? new List<long>(), filterNew, filterFavourite, filterStockIndicatorMin, filterStockIndicatorMax, filterColor?.ToList() ?? new List<long>(), filterVolumeMin, filterVolumeMax, filterVolumeCup?.ToList() ?? new List<decimal>(), filterShape?.ToList() ?? new List<long>(), filterNeck?.ToList() ?? new List<long>(), filterRim?.ToList() ?? new List<long>(), filterPriceMin, filterPriceMax, filterDiameterMin, filterDiameterMax, filterWidthMin, filterWidthMax, filterHeightMin, filterHeightMax, materialType?.ToList() ?? new List<string>(), lidType);
				return Ok(result);
			}
			catch (Exception ex)
			{
				var message = ex.Message;
				var inner = ex.InnerException;
				while (inner != null)
				{
					message = inner.Message;
					inner = inner.InnerException;
				}
				Trace.WriteLine(message, "ProductController");
				return BadRequest(new { message });
			}
		}

        [HttpGet("status")]
        public async Task<IActionResult> GetStatusFilter(int limit = 0, int page = 0, string search = "", string sort = "",  string filter = "")
        {
            try
            {
                var total = 0;
                var result = await _service.GetStatusAsync(limit , page , search , sort , filter ,total);
                return Ok(result);
            }
            catch (Exception ex)
            {
                var message = ex.Message;
                var inner = ex.InnerException;
                while (inner != null)
                {
                    message = inner.Message;
                    inner = inner.InnerException;
                }
                Trace.WriteLine(message, "ProductController");
                return BadRequest(new { message });
            }
        }


    }
}

