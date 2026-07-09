using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using YtDownloader.Api.Services.Interface;

namespace YtDownloader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public sealed class VideoController : ControllerBase
    {
        private readonly IYtDlpService _ytDlpService;

        public VideoController(IYtDlpService ytDlpService)
        {
            _ytDlpService = ytDlpService;
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetVideoInfo([FromQuery] string url, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return BadRequest(new
                {
                    Error = "URL wajib diisi."
                });
            }

            try
            {
                var videoInfo = await _ytDlpService.GetVideoInfoAsync(url, cancellationToken);
                return Ok(videoInfo);
            }
            catch (InvalidOperationException ex)
            {
              return BadRequest(new
              {
                  Error = ex.Message
              });
            }
        }

    }
}