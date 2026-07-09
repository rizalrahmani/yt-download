using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace YtDownloader.Api.DTOs
{
    public sealed class VideoInfoResponse
    {
      public string? Id { get; set; }
      public string? Title { get; set; }
      public int Duration { get; set; }
      public string? Uploader { get; set; }
      public string? Thumbnail { get; set; }
      public string? WebpageUrl { get; set; }
    }
}