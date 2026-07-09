using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YtDownloader.Api.DTOs.Response
{
    public sealed class StartDownloadResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}