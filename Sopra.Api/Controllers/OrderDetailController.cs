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
    public class OrderDetailController : ControllerBase
    {
        private readonly IServiceAsync<OrderDetail> _service;

        public OrderDetailController(IServiceAsync<OrderDetail> service)
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
                Trace.WriteLine(message, "OrderDetailController");
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

                var response = new Response<OrderDetail>(result);
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
                Trace.WriteLine(message, "OrderDetailController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] OrderDetail obj)
        {
            try
            {
                obj.UserIn = Convert.ToInt64(User.FindFirstValue("id"));

                var result = await _service.CreateAsync(obj);
                var response = new Response<OrderDetail>(result);
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
                Trace.WriteLine(message, "OrderDetailController");
                return BadRequest(new { message });
            }

        }

        //[Authorize]
        [HttpPut]
        public async Task<IActionResult> Edit([FromBody] OrderDetail obj)
        {
            try
            {
                obj.UserUp = Convert.ToInt64(User.FindFirstValue("id"));

                var result = await _service.EditAsync(obj);
                var response = new Response<OrderDetail>(result);
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
                Trace.WriteLine(message, "OrderDetailController");
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
                Trace.WriteLine(message, "OrderDetailController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        //[HttpGet("accs/{id}/{customerid}")]
        //public async Task<IActionResult> GetAccIdAsync(long id, long customerid)
        //{
        //    try
        //    {
        //        var result = await _service.GetAccIdAsync<AccsExtOrder>(id, customerid);
        //        if (result == null)
        //            return BadRequest(new { message = "Invalid ID" });

        //        //var response = new Response<AccsExt>(result);
        //        return Ok(result);
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
        //        Trace.WriteLine(message, "OrderDetailController");
        //        return BadRequest(new { message });
        //    }
        //}

    }
}

