using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YtDownloader.Api.DTOs.Request
{
    public sealed class StartDownloadRequest
    {
        public string Url { get; set; } = string.Empty;
        public string Format { get; set; } = "mp4";
        public string Quality { get; set; } = "best";
    }
}