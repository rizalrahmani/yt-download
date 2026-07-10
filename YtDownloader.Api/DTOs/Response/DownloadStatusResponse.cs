using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YtDownloader.Api.DTOs.Response
{
    public sealed class DownloadStatusResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Quality { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int Progress { get; set; }
        public string? OutputPath { get; set; }
        public string? Error { get; set; }
        public string? LastMessage { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset? FinishedAt { get; set; }
    }

}