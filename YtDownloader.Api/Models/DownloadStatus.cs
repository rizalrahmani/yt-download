using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YtDownloader.Api.Models
{
    public enum DownloadStatus
    {
        Queued,
        Downloading,
        Completed,
        Failed
    }
}