using Microsoft.AspNetCore.Mvc;

using System;
using System.Diagnostics;

namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TestController : ControllerBase
    {
        [HttpGet]
        public IActionResult Testing()
        {
            try
            {
                return Ok("Succeed");
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
                Trace.WriteLine(message, "TestController");
                return BadRequest(new { message });
            }
        }
    }
}
