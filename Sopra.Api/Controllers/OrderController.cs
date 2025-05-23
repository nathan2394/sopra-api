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

namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly OrderInterface _service;

        public OrderController(OrderInterface service)
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
                Trace.WriteLine(message, "OrderController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        [HttpGet("transaction-order-detail")]
        public async Task<IActionResult> GetTransactionOrderDetail(int orderid = 0)
        {
            try
            {
                var result = await _service.GetTransactionOrderDetailAsync(orderid);
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
                Trace.WriteLine(message, "CartDetailController");
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

                var response = new Response<Order>(result);
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
                Trace.WriteLine(message, "OrderController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] List<Order> obj,long userId=0)
        {
            try
            {

                var result = await _service.CreateAsync(obj, userId);
                var response = new Response<Order>(result);
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
                Trace.WriteLine(message, "OrderController");
                return BadRequest(new { message });
            }

        }

        //[Authorize]
        [HttpPut]
        public async Task<IActionResult> Edit([FromBody] List<Order> obj)
        {
            try
            {
                //obj.UserUp = Convert.ToInt64(User.FindFirstValue("id"));

                var result = await _service.EditAsync(obj, Convert.ToInt64(User.FindFirstValue("id")));
                var response = new Response<Order>(result);
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
                Trace.WriteLine(message, "OrderController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var result = await _service.DeleteAsync(id, Convert.ToInt64(User.FindFirstValue("id")));

                var response = new Response<object>(result);
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
                Trace.WriteLine(message, "OrderController");
                return BadRequest(new { message });
            }
        }

    }
}

