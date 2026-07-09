using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YtDownloader.Api.DTOs;

namespace YtDownloader.Api.Services.Interface
{
    public interface IYtDlpService
    {
      Task<VideoInfoResponse> GetVideoInfoAsync(string url, CancellationToken cancellationToken);   
    }
}