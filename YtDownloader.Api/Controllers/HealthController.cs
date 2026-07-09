using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace YtDownloader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                Status = "Ok",
                app = "YtDownloader.Api",
                Timestamp = DateTimeOffset.UtcNow
            });
        }
    }
}