using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using System.Diagnostics;

using Sopra.Services;
using Microsoft.AspNetCore.Authorization;

namespace Sopra.Api.Controllers
{
    [ApiController]
    [Route("UserLog")]
    [Authorize]
    public class UserLogController : ControllerBase
    {
        private readonly UserLogInterface _service;

        public UserLogController(UserLogInterface service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> Get(long objectId = 0, long moduleId = 0)
        {
            try
            {
                var result = await _service.GetAllAsync(objectId, moduleId);
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
                Trace.WriteLine(message, "UserLogController: Get All");
                return BadRequest(new { message });
            }
        }
    }
}