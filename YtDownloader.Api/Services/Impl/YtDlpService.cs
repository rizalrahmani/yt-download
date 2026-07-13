using System.Diagnostics;
using System.Text.Json;
using YtDownloader.Api.DTOs;
using YtDownloader.Api.Models;
using YtDownloader.Api.Services.Interface;
using ProgressHelper = YtDownloader.Api.Helper.Helper;

namespace YtDownloader.Api.Services.Impl
{
    public sealed class YtDlpService : IYtDlpService
    {
      private readonly ILogger<YtDlpService> _logger;
      private static readonly string ConfigFile = "/home/.yt-dlp/config";

      public YtDlpService(ILogger<YtDlpService> logger)
      {
        _logger = logger;
      }

      private static void AddCommonArgs(ProcessStartInfo psi)
      {
          if (File.Exists(ConfigFile))
          {
              psi.ArgumentList.Add("--config-location");
              psi.ArgumentList.Add(ConfigFile);
          }
          psi.ArgumentList.Add("--extractor-args");
          psi.ArgumentList.Add("youtube:player_client=android");
      }

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

        AddCommonArgs(startInfo);
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
            WebpageUrl = root.TryGetProperty("webpage_url", out var webpageUrl) ? webpageUrl.GetString(): url
        };
      }

      public async Task DownloadAsync(
        DownloadJob job, Func<DownloadJob, CancellationToken, Task> onJobUpdated,
        CancellationToken cancellationToken)
      {
        Directory.CreateDirectory("downloads");

        job.Status = DownloadStatus.Downloading;
        job.LastMessage = "Memulai proses download...";
        await onJobUpdated(job, cancellationToken);

        var startInfo = new ProcessStartInfo
        {
            FileName = "yt-dlp",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        AddCommonArgs(startInfo);
        startInfo.ArgumentList.Add("--newline");
        startInfo.ArgumentList.Add("--print");
        startInfo.ArgumentList.Add("after_move:filepath");
        startInfo.ArgumentList.Add("-o");
        startInfo.ArgumentList.Add(Path.Combine("downloads", "%(title)s.%(ext)s"));

        if (job.Format.Equals("mp3", StringComparison.OrdinalIgnoreCase))
        {
            startInfo.ArgumentList.Add("-x");
            startInfo.ArgumentList.Add("--audio-format");
            startInfo.ArgumentList.Add("mp3");
        }
        else
        {
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add(
              job.Quality.Equals("best", StringComparison.OrdinalIgnoreCase) ? "bestvideo+bestaudio/best" : job.Quality
            );
        }
        
        startInfo.ArgumentList.Add(job.Url);

        using var process = new Process 
        { 
          StartInfo = startInfo
        };

        process.OutputDataReceived += (_, e) =>
        {
          if (string.IsNullOrWhiteSpace(e.Data))
            return;

          job.LastMessage = e.Data;

          if (e.Data.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
              e.Data.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) ||
              e.Data.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase) ||
              e.Data.EndsWith(".webm", StringComparison.OrdinalIgnoreCase))
          {
            job.OutputPath = e.Data;
          }
          
          var progress = ProgressHelper.TryParseProgress(e.Data);
          if (progress.HasValue)
          {
            job.Progress = progress.Value;
          }

          _ = onJobUpdated(job, cancellationToken);
        };

        process.ErrorDataReceived += (_, e) =>
        {
          if (string.IsNullOrWhiteSpace(e.Data))
            return;

          job.LastMessage = e.Data;
          _ = onJobUpdated(job, cancellationToken);
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(cancellationToken);

        if (process.ExitCode == 0)
        {
          job.Progress = 100;
          job.Status = DownloadStatus.Completed;
          job.LastMessage = "Download selesai.";
          job.FinishedAt = DateTimeOffset.UtcNow;
        }
        else
        {
          job.Status = DownloadStatus.Failed;
          job.Error = job.LastMessage ?? "Download gagal.";
          job.FinishedAt = DateTimeOffset.UtcNow;
        }

        await onJobUpdated(job, cancellationToken);
      }

    }
}
