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
    public class CartDetailController : ControllerBase
    {
        private readonly CartDetailInterface _service;

        public CartDetailController(CartDetailInterface service)
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

                var response = new Response<CartDetail>(result);
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
                Trace.WriteLine(message, "CartDetailController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        //[HttpGet("accs/{id}/{customerid}")]
        //public async Task<IActionResult> GetAccIdAsync(long id, long customerid)
        //{
        //    try
        //    {
        //        var result = await _service.GetAccIdAsync<AccsExt>(id, customerid);
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
        //        Trace.WriteLine(message, "CartDetailController");
        //        return BadRequest(new { message });
        //    }
        //}

        //[Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] List<CartDetail> obj,int isIncrease=0)
        {
            try
            {
                //obj.UserIn = Convert.ToInt64(User.FindFirstValue("id"));
                bool checkIncrease = isIncrease == 0 ? false : true;
                var result = await _service.CreateAsync(obj, Convert.ToInt64(User.FindFirstValue("id")), checkIncrease);
                var response = new Response<CartDetail>(result);
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
                Trace.WriteLine(message, "CartDetailController");
                return BadRequest(new { message });
            }

        }

        //[Authorize]
        [HttpPut]
        public async Task<IActionResult> Edit([FromBody] List<CartDetail> obj)
        {
            try
            {
                //obj.UserUp = Convert.ToInt64(User.FindFirstValue("id"));

                var result = await _service.EditAsync(obj, Convert.ToInt64(User.FindFirstValue("id")));
                var response = new Response<CartDetail>(result);
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
                Trace.WriteLine(message, "CartDetailController");
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
                Trace.WriteLine(message, "CartDetailController");
                return BadRequest(new { message });
            }
        }

    }
}

