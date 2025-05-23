using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

using System;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

using Sopra.Entities;
using Sopra.Responses;
using Sopra.Services;
using Google.Cloud.Storage.V1;
using System.IO;


namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GcpController : ControllerBase
    {
        private readonly IServiceGcpAsync<Gcp> _service;

        public GcpController(IServiceGcpAsync<Gcp> service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get(int limit = 0, int page = 0, string search = "", string sort = "", string filter = "", string date = "", string fileName = "")
        {
            try
            {
                var total = 0;
                var result = await _service.GetAllAsync(limit, page, total, search, sort, filter, date, fileName);
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
                Trace.WriteLine(message, "GcpController");
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

                var response = new Response<Gcp>(result);
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
                Trace.WriteLine(message, "GcpController");
                return BadRequest(new { message });
            }
        }

        //[Authorize]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Gcp obj)
        {
            try
            {
                obj.UserIn = Convert.ToInt64(User.FindFirstValue("id"));

                var result = await _service.CreateAsync(obj);
                var response = new Response<Gcp>(result);
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
                Trace.WriteLine(message, "GcpController");
                return BadRequest(new { message });
            }

        }

        //[Authorize]
        [HttpPut]
        public async Task<IActionResult> Edit([FromBody] Gcp obj)
        {
            try
            {
                obj.UserUp = Convert.ToInt64(User.FindFirstValue("id"));

                var result = await _service.EditAsync(obj);
                var response = new Response<Gcp>(result);
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
                Trace.WriteLine(message, "GcpController");
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
                Trace.WriteLine(message, "GcpController");
                return BadRequest(new { message });
            }
        }

    }
}

