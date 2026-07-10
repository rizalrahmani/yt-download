using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YtDownloader.Api.DTOs;
using YtDownloader.Api.Models;

namespace YtDownloader.Api.Services.Interface
{
    public interface IYtDlpService
    {
      Task<VideoInfoResponse> GetVideoInfoAsync(string url, CancellationToken cancellationToken);
      Task DownloadAsync(
        DownloadJob job, Func<DownloadJob, CancellationToken, Task> onJobUpdated,
        CancellationToken cancellationToken);
    }
}