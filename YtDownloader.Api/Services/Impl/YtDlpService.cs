using System.Diagnostics;
using System.Text.Json;
using System.Web;
using YtDownloader.Api.DTOs;
using YtDownloader.Api.Models;
using YtDownloader.Api.Services.Interface;
using ProgressHelper = YtDownloader.Api.Helper.Helper;

namespace YtDownloader.Api.Services.Impl
{
    public sealed class YtDlpService : IYtDlpService
    {
      private readonly ILogger<YtDlpService> _logger;
      private readonly HttpClient _httpClient;
      private static readonly string InvidiousApi = "https://inv.nadeko.net/api/v1";

      public YtDlpService(ILogger<YtDlpService> logger, HttpClient httpClient)
      {
        _logger = logger;
        _httpClient = httpClient;
      }

      private static string ExtractVideoId(string url)
      {
          var uri = new Uri(url);
          if (uri.Host.Contains("youtu.be"))
              return uri.AbsolutePath.TrimStart('/');

          var query = HttpUtility.ParseQueryString(uri.Query);
          return query["v"] ?? string.Empty;
      }

      public async Task<VideoInfoResponse> GetVideoInfoAsync(string url, CancellationToken cancellationToken)
      {
        var videoId = ExtractVideoId(url);
        if (string.IsNullOrWhiteSpace(videoId))
            throw new InvalidOperationException("URL YouTube tidak valid.");

        var response = await _httpClient.GetAsync($"{InvidiousApi}/videos/{videoId}", cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new VideoInfoResponse
        {
            Id = root.TryGetProperty("videoId", out var id) ? id.GetString() : videoId,
            Title = root.TryGetProperty("title", out var title) ? title.GetString() : null,
            Duration = root.TryGetProperty("lengthSeconds", out var dur) ? dur.GetInt32() : 0,
            Uploader = root.TryGetProperty("author", out var author) ? author.GetString() : null,
            Thumbnail = root.TryGetProperty("videoThumbnails", out var thumbs) && thumbs.GetArrayLength() > 0
                ? thumbs[0].GetProperty("url").GetString() : null,
            WebpageUrl = url
        };
      }

      public async Task DownloadAsync(
        DownloadJob job, Func<DownloadJob, CancellationToken, Task> onJobUpdated,
        CancellationToken cancellationToken)
      {
        Directory.CreateDirectory("downloads");

        job.Status = DownloadStatus.Downloading;
        job.LastMessage = "Mendapatkan URL download...";
        await onJobUpdated(job, cancellationToken);

        var videoId = ExtractVideoId(job.Url);
        if (string.IsNullOrWhiteSpace(videoId))
        {
            job.Status = DownloadStatus.Failed;
            job.Error = "URL YouTube tidak valid.";
            await onJobUpdated(job, cancellationToken);
            return;
        }

        var apiResponse = await _httpClient.GetAsync($"{InvidiousApi}/videos/{videoId}", cancellationToken);
        apiResponse.EnsureSuccessStatusCode();

        var json = await apiResponse.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        string? downloadUrl = null;
        string? ext = null;

        if (job.Format.Equals("mp3", StringComparison.OrdinalIgnoreCase))
        {
            var audioFormats = root.TryGetProperty("adaptiveFormats", out var af)
                ? af.EnumerateArray().Where(f =>
                    f.TryGetProperty("type", out var t) && t.GetString()?.StartsWith("audio/") == true)
                  .ToList() : new();

            var best = audioFormats.OrderByDescending(f =>
                f.TryGetProperty("bitrate", out var br) ? br.GetInt32() : 0).FirstOrDefault();

            if (best.ValueKind != JsonValueKind.Undefined)
            {
                downloadUrl = best.GetProperty("url").GetString();
                ext = "mp3";
            }
        }
        else
        {
            var streams = root.TryGetProperty("formatStreams", out var fs)
                ? fs.EnumerateArray().ToList() : new();

            if (streams.Count > 0)
            {
                var best = streams
                    .Where(s => s.TryGetProperty("container", out var c) &&
                                c.GetString()?.Contains("mp4") == true)
                    .OrderByDescending(s => s.TryGetProperty("resolution", out var r)
                        ? int.TryParse(r.GetString()?.Split('x').LastOrDefault(), out var h) ? h : 0 : 0)
                    .FirstOrDefault();

                if (best.ValueKind != JsonValueKind.Undefined)
                {
                    downloadUrl = best.GetProperty("url").GetString();
                    ext = "mp4";
                }
            }
        }

        if (string.IsNullOrWhiteSpace(downloadUrl))
        {
            job.Status = DownloadStatus.Failed;
            job.Error = "Tidak ada format yang tersedia.";
            await onJobUpdated(job, cancellationToken);
            return;
        }

        job.LastMessage = "Mendownload file...";
        await onJobUpdated(job, cancellationToken);

        var title = root.TryGetProperty("title", out var t) ? t.GetString() : videoId;
        var fileName = $"{SanitizeFileName(title)}.{ext}";
        var filePath = Path.Combine("downloads", fileName);

        using var stream = await _httpClient.GetStreamAsync(downloadUrl, cancellationToken);
        using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

        var buffer = new byte[8192];
        long totalRead = 0;
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await fileStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalRead += bytesRead;

            job.Progress = Math.Min(99, (int)(totalRead / 1024 / 1024 * 5));
            job.LastMessage = $"Mendownload... ({totalRead / 1024 / 1024} MB)";
        }

        job.Progress = 100;
        job.Status = DownloadStatus.Completed;
        job.OutputPath = filePath;
        job.LastMessage = "Download selesai.";
        job.FinishedAt = DateTimeOffset.UtcNow;
        await onJobUpdated(job, cancellationToken);
      }

      private static string SanitizeFileName(string name)
      {
          foreach (var c in Path.GetInvalidFileNameChars())
              name = name.Replace(c, '_');
          return name.Length > 100 ? name[..100] : name;
      }
    }
}
