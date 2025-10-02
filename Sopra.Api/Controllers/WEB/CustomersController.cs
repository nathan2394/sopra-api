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
    [Route("Customers")]
    public class CustomersController : ControllerBase
    {
        private readonly CustomersInterface _service;

        public CustomersController(CustomersInterface service)
        {
            _service = service;
        }

        [HttpGet]
        [Authorize]
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
                Trace.WriteLine(message, "CustomersController: Get All");
                return BadRequest(new { message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(long id)
        {
            try
            {
                var result = await _service.GetByIdAsync(id);
                if (result == null)
                    return BadRequest(new { message = "Invalid ID" });

                var response = new Response<Customer>(result);
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
                Trace.WriteLine(message, "CustomersController: Get by ID");
                return BadRequest(new { message });
            }
        }
    }
}