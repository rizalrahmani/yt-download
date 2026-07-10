using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YtDownloader.Api.DTOs.Request;
using YtDownloader.Api.DTOs.Response;

namespace YtDownloader.Api.Services.Interface
{
    public interface IDownloadService
    {
        Task<StartDownloadResponse> StartDownloadAsync(StartDownloadRequest request, CancellationToken cancellationToken);
        Task<DownloadStatusResponse> GetDownloadStatusAsync(string id, CancellationToken cancellationToken);
        Task DeleteFileAsync(string id, CancellationToken cancellationToken);
        Task UpdateLastAccessedAsync(string id, CancellationToken cancellationToken);
    }
}