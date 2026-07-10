using Microsoft.AspNetCore.Mvc;
using YtDownloader.Api.DTOs.Request;
using YtDownloader.Api.Services.Interface;

namespace YtDownloader.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DownloadController : ControllerBase
    {
        private readonly IDownloadService _downloadService;

        public DownloadController(IDownloadService downloadService)
        {
            _downloadService = downloadService;
        }

        [HttpPost("start")]
        public async Task<IActionResult> StartDownload(
            [FromBody] StartDownloadRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                var response = await _downloadService.StartDownloadAsync(request, cancellationToken);
                return Accepted(new
                {
                    Id = response.Id,
                    Status = response.Status
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("status/{id}")]
        public async Task<IActionResult> GetDownloadStatus(
            string id,
            CancellationToken cancellationToken)
        {
            try
            {
                var result = await _downloadService.GetDownloadStatusAsync(id, cancellationToken);
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { Error = ex.Message });
            }
        }


        [HttpGet("file/{id}")]
        public async Task<IActionResult> DownloadFile(string id, CancellationToken cancellationToken)
        {
          try
          {
            var status = await _downloadService.GetDownloadStatusAsync(id, cancellationToken);

            if (status.Status != "Completed")
            {
              return BadRequest(new 
              { 
                Error = "Download belum selesai." 
              });
            }

            
            if (string.IsNullOrWhiteSpace(status.OutputPath))
            {
              return NotFound(new 
              { 
                Error = "File tidak ditemukan." 
              });
            }

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), status.OutputPath);
            var fileName = Path.GetFileName(status.OutputPath);
            var contentType = status.Format == "mp3" ? "audio/mpeg" : "video/mp4";

            return PhysicalFile(filePath, contentType, fileName);
          }
          catch (KeyNotFoundException ex)
          {
            return NotFound(new 
            { 
              Error = ex.Message 
            });
          }
        }
    
        [HttpDelete("file/{id}")]
        public async Task<IActionResult> DeleteFile(string id, CancellationToken cancellationToken)
        {
          try
          {
            await _downloadService.DeleteFileAsync(id, cancellationToken);
            return Ok(new
            {
              Message = "File berhasil dihapus."
            });
          }
          catch (KeyNotFoundException ex)
          {
            return NotFound(new 
            { 
              Error = ex.Message 
            });
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