using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

using Sopra.Entities;
using Sopra.Responses;
using Sopra.Services;
using Sopra.Requests;

namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class DiscountController : ControllerBase
    {
        private readonly IServiceAsync<Discount> _service;

        public DiscountController(IServiceAsync<Discount> service)
        {
            _service = service;
        }

        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> Get(int limit = 0, int page = 0, string search = "", string sort = "", string filter = "", string date = "")
        {
            try
            {
                var total = 0;
                var result = await _service.GetAllAsync(limit, page, total, search, sort, filter, date);
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
                Trace.WriteLine(message, "DiscountController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        //[HttpPost]
        //public async Task<IActionResult> Create([FromBody] Discount obj)
        //{
        //    try
        //    {
        //        obj.UserIn = Convert.ToInt64(User.FindFirstValue("id"));

        //        var result = await _service.CreateAsync(obj);
        //        var response = new Response<Discount>(result);
        //        return Ok(response);
        //    }
        //    catch (Exception ex)
        //    {
        //        var message = ex.Message;
        //        var inner = ex.InnerException;
        //        while (inner != null)
        //        {
        //            message = inner.Message;
        //            inner = inner.InnerException;
        //        }
        //        Trace.WriteLine(message, "DiscountController");
        //        return BadRequest(new { message });
        //    }

        //}
    }
}
