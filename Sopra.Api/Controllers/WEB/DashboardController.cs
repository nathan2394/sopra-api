using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

using Sopra.Services;
using Sopra.Entities;
using Sopra.Responses;
using Microsoft.AspNetCore.Authorization;

namespace Sopra.Controllers
{
    [ApiController]
    [Route("Dashboard")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly DashboardInterface _service;

        public DashboardController(DashboardInterface service)
        {
            _service = service;
        }

        [HttpGet("Overview")]
        public async Task<IActionResult> GetOverview(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int companyID)
        {
            try
            {
                var result = await _service.LoadOverview(startDate, endDate, companyID);
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
                Trace.WriteLine(message, $"DashboardController: Get Overview");
                return BadRequest(new { message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboardTable(
            [FromQuery] string key,
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate,
            [FromQuery] int companyID)
        {
            try
            {
                var result = await _service.LoadTableData(key, startDate, endDate, companyID);
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
                Trace.WriteLine(message, $"DashboardController: Get {key}");
                return BadRequest(new { message });
            }
        }
    }
}