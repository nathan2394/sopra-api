using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

using Sopra.Services;
using Sopra.Entities;
using Sopra.Responses;
using Microsoft.AspNetCore.Authorization;

namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("InvoiceBottle")]
    [Authorize]
    public class InvoiceBottleController : ControllerBase
    {
        private readonly InvoiceBottleInterface _service;

        public InvoiceBottleController(InvoiceBottleInterface service)
        {
            _service = service;
        }

        private async Task<int> getUserId()
        {
            var userIdClaim = User.FindFirst("id")?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                return 0;
            }

            return userId;
        }

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
                Trace.WriteLine(message, "InvoiceBottleController: Get All");
                return BadRequest(new { message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
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
                Trace.WriteLine(message, "InvoiceBottleController: Get by ID");
                return BadRequest(new { message });
            }
        }

        [HttpGet("Order/{id}")]
        public async Task<IActionResult> GetByOrderIdAsync(int id)
        {
            try
            {
                var result = await _service.GetByOrderIdAsync(id);
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
                Trace.WriteLine(message, "InvoiceBottleController: Get by Order ID");
                return BadRequest(new { message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create(InvoiceBottle obj)
        {
            try
            {
                var userId = await getUserId();
                if (userId == 0) return BadRequest("Invalid Token");

                var result = await _service.CreateAsync(obj, userId);
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
                Trace.WriteLine(message, "InvoiceBottleController: Create");
                return BadRequest(new { message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Edit([FromBody] InvoiceBottle obj)
        {
            try
            {
                var userId = await getUserId();
                if (userId == 0) return BadRequest("Invalid Token");

                var result = await _service.EditAsync(obj, userId);
                var response = new Response<Invoice>(result);
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
                Trace.WriteLine(message, "InvoiceBottleController: Edit");
                return BadRequest(new { message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id, [FromQuery] int reason)
        {
            try
            {
                var userId = await getUserId();
                if (userId == 0) return BadRequest("Invalid Token");

                var result = await _service.DeleteAsync(id, reason, userId);
                var response = new Response<bool>(result);

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
                Trace.WriteLine(message, "InvoiceBottleController: Delete");
                return BadRequest(new { message });
            }
        }
    }
}