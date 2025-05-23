using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

using Sopra.Entities;
using Sopra.Responses;
using Sopra.Services;

namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ResellerController : ControllerBase
    {
        private readonly IServiceResellerAsync<Reseller> _service;

        public ResellerController(IServiceResellerAsync<Reseller> service)
        {
            _service = service;
        }

        //[Authorize]
        [HttpGet]
        public async Task<IActionResult> Get(int limit = 0, int page = 0, string search = "", int provinceid = 0)
        {
            try
            {
                var total = 0;
                var result = await _service.GetResellerAsync(limit, page, total, search, provinceid);
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
                Trace.WriteLine(message, "ResellerController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null)
                    return BadRequest(new { message = "Invalid ID" });

                var response = new Response<Reseller>(result);
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
                Trace.WriteLine(message, "ResellerController");
                return BadRequest(new { message });
            }
        }

    }
}

