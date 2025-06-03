using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

using Sopra.Services;
using Sopra.Entities;
using Sopra.Responses;

namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("Promos")]
    public class PromosController : ControllerBase
    {
        private readonly PromosInterface _service;

        public PromosController(PromosInterface service)
        {
            _service = service;
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
    }
}