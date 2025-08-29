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
    [Route("OrderBottle")]
    [Authorize]
    public class OrderBottleController : ControllerBase
    {
        private readonly OrderBottleInterface _service;

        public OrderBottleController(OrderBottleInterface service)
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
                Trace.WriteLine(message, "OrderBottleController: Get All");
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
                Trace.WriteLine(message, "OrderBottleController: Get by ID");
                return BadRequest(new { message });
            }
        }

        [HttpGet("Attachment/{key}")]
        public async Task<IActionResult> GetByKey(string key)
        {
            try
            {
                var result = await _service.GetByKeyAsync(key);
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
                Trace.WriteLine(message, "OrderBottleController: Get by Key");
                return BadRequest(new { message });
            }
        }

        [HttpGet("CheckVoucher")]
        public async Task<IActionResult> CheckVoucher(string voucher, long amount)
        {
            try
            {
                var result = await _service.CheckVoucherAsync(voucher, amount);
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
                Trace.WriteLine(message, "OrderBottleController: Check Voucher");
                return BadRequest(new { message });
            }
        }

        [HttpGet("CheckInduk/{customerID}")]
        public async Task<IActionResult> CheckIndukAnak(long customerID)
        {
            try
            {
                var result = await _service.CheckIndukAnakAsync(customerID);
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
                Trace.WriteLine(message, "OrderBottleController: Check Induk Anak by Customer ID");
                return BadRequest(new { message });
            }
        }

        [HttpGet("CheckStatus/{id}")]
        public async Task<IActionResult> CheckOrderStatus(long id)
        {
            try
            {
                var result = await _service.CheckOrderStatusAsync(id);
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
                Trace.WriteLine(message, "OrderBottleController: Check Status by Order ID");
                return BadRequest(new { message });
            }
        }

        [HttpGet("CheckDealer/{customerID}")]
        public async Task<IActionResult> CheckDealer(long customerID)
        {
            try
            {
                var result = await _service.CheckDealerAsync(customerID);
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
                Trace.WriteLine(message, "OrderBottleController: Check Dealer by Customer ID");
                return BadRequest(new { message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderBottleDto obj)
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
                Trace.WriteLine(message, "OrderBottleController: Create");
                return BadRequest(new { message });
            }
        }

        [HttpPut]
        public async Task<IActionResult> Edit([FromBody] OrderBottleDto obj)
        {
            try
            {
                var userId = await getUserId();
                if (userId == 0) return BadRequest("Invalid Token");

                var result = await _service.EditAsync(obj, userId);
                var response = new Response<OrderBottleDto>(result);
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
                Trace.WriteLine(message, "OrderBottleController: Edit");
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
                Trace.WriteLine(message, "OrderBottleController: Delete");
                return BadRequest(new { message });
            }
        }
    }
}