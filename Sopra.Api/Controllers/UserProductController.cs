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
    public class UserProductController : ControllerBase
    {
        private readonly UserProductInterface _service;

        public UserProductController(UserProductInterface service)
        {
            _service = service;
        }

        [HttpGet("wishlist")]
        public async Task<IActionResult> GetWishlist(int customerId= 0)
        {
            try
            {
                var result = await _service.GetWishlist(customerId);
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
                Trace.WriteLine(message, "UserProductController");
                return BadRequest(new { message });
            }
        }


        [HttpGet("qtycart")]
        public async Task<IActionResult> GetQtyCart(int customerId = 0)
        {
            try
            {
                var result = await _service.GetQtyCart(customerId);
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
                Trace.WriteLine(message, "UserProductController");
                return BadRequest(new { message });
            }
        }

    }
}
