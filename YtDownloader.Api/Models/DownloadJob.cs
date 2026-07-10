using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YtDownloader.Api.Models
{
    public sealed class DownloadJob
    {
        public string Id { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string Format { get; set; } = "mp4";
        public string Quality { get; set; } = "best";
        public DownloadStatus Status { get; set; } = DownloadStatus.Queued;
        public int Progress { get; set; }
        public string? OutputPath { get; set; }
        public string? Error { get; set; }
        public string? LastMessage { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset? FinishedAt { get; set; }
        public DateTimeOffset? LastAccessedAt { get; set; }

    }
}