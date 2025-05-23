using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

using Sopra.Services;
using Sopra.Entities;

namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("AgentAI")]
    public class AgentAiController : ControllerBase
    {
        private readonly AgentAiInterface _service;

        public AgentAiController(AgentAiInterface service)
        {
            _service = service;
        }

        [HttpPost("{param}")]
        public async Task<IActionResult> Get(string param = "", int limit = 0, int page = 0, string search = "", string sort = "", string filter = "", string date = "", ProductKeyAisah productKeyAisah = null)
        {
            try
            {
                var total = 0;
                var result = await _service.GetAllAsync(param, limit, page, total, search, sort, filter, date, productKeyAisah);
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
                Trace.WriteLine(message, "AgentAiController");
                return BadRequest(new { message });
            }
        }
    }
}