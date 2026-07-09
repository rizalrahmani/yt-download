using System.Diagnostics;
using System.Text.Json;
using YtDownloader.Api.DTOs;
using YtDownloader.Api.Services.Interface;

namespace YtDownloader.Api.Services.Impl
{
    public sealed class YtDlpService : IYtDlpService
    {
      public async Task<VideoInfoResponse> GetVideoInfoAsync(string url, CancellationToken cancellationToken)
      {
        var startInfo = new ProcessStartInfo
        {
            FileName = "yt-dlp",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        startInfo.ArgumentList.Add("-J");
        startInfo.ArgumentList.Add(url);

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            throw new InvalidOperationException("Gagal menjalankan yt-dlp.");
        }

        var output = await process.StandardOutput.ReadToEndAsync(cancellationToken);
        var error = await process.StandardError.ReadToEndAsync(cancellationToken);

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"yt-dlp gagal dijalankan. Kode keluar: {process.ExitCode}. Pesan kesalahan: {error}");
        }

        using var document = JsonDocument.Parse(output);
        var root = document.RootElement;

        return new VideoInfoResponse
        {
            Id = root.TryGetProperty("id", out var id) ? id.GetString() : null,
            Title = root.TryGetProperty("title", out var title) ? title.GetString() : null,
            Duration = root.TryGetProperty("duration", out var duration) ? duration.GetInt32() : 0,
            Uploader = root.TryGetProperty("uploader", out var uploader) ? uploader.GetString() : null,
            Thumbnail = root.TryGetProperty("thumbnail", out var thumbnail) ? thumbnail.GetString() : null,
            WebpageUrl = root.TryGetProperty("webpage_url", out var webpageUrl) ? webpageUrl.GetString() : url
        };


      }
    }
}