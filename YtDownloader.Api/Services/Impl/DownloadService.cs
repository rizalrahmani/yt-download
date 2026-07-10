using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YtDownloader.Api.Data;
using YtDownloader.Api.DTOs.Request;
using YtDownloader.Api.DTOs.Response;
using YtDownloader.Api.Models;
using YtDownloader.Api.Services.Interface;

namespace YtDownloader.Api.Services.Impl
{
    public sealed class DownloadService : IDownloadService
    {
      private static readonly SemaphoreSlim _downloadThrottle = new(1, 1);

      private readonly ILogger<DownloadService> _logger;
      private readonly AppDbContext _dbContext;
      private readonly IServiceScopeFactory _scopeFactory;

      public DownloadService(
        ILogger<DownloadService> logger,
        AppDbContext dbContext,
        IServiceScopeFactory scopeFactory)
      {
        _logger = logger;
        _dbContext = dbContext;
        _scopeFactory = scopeFactory;
      }


      public async Task<StartDownloadResponse> StartDownloadAsync(StartDownloadRequest request, CancellationToken cancellationToken)
      {
        if (string.IsNullOrWhiteSpace(request?.Url))
          throw new ArgumentException("URL wajib di isi.");
        
        var job = new DownloadJob
        {
          Id = Guid.NewGuid().ToString("N"),
          Url = request.Url,
          Format = string.IsNullOrWhiteSpace(request.Format) ? "mp4" : request.Format,
          Quality = string.IsNullOrWhiteSpace(request.Quality) ? "best" : request.Quality,
          Status = DownloadStatus.Queued,
          Progress = 0,
          CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.DownloadJobs.Add(job);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _ = Task.Run(() => RunDownloadAsync(job.Id));

        return new StartDownloadResponse
        {
          Id = job.Id,
          Status = job.Status.ToString()
        };
      }

      private async Task RunDownloadAsync(string jobId)
      {
        await _downloadThrottle.WaitAsync();
        try
        {
          using var scope = _scopeFactory.CreateScope();
          var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
          var ytDlpService = scope.ServiceProvider.GetRequiredService<IYtDlpService>();

          var job = await dbContext.DownloadJobs.FirstOrDefaultAsync(x => x.Id == jobId);
          if (job == null)
          {
            _logger.LogWarning("Download job {JobId} tidak ditemukan saat akan diproses.", jobId);
            return;
          }

          await ytDlpService.DownloadAsync(job, UpdateDownloadJobAsync, CancellationToken.None);
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Gagal memproses download job {JobId}.", jobId);
        }
        finally
        {
          _downloadThrottle.Release();
        }
      }

      private async Task UpdateDownloadJobAsync(DownloadJob job, CancellationToken cancellationToken)
      {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var existingJob = await dbContext.DownloadJobs
          .FirstOrDefaultAsync(x => x.Id == job.Id, cancellationToken);

        if (existingJob == null)
          return;

        existingJob.Status = job.Status;
        existingJob.Progress = job.Progress;
        existingJob.OutputPath = job.OutputPath;
        existingJob.Error = job.Error;
        existingJob.LastMessage = job.LastMessage;
        existingJob.FinishedAt = job.FinishedAt;

        await dbContext.SaveChangesAsync(cancellationToken);
      }
      
      public async Task<DownloadStatusResponse> GetDownloadStatusAsync(string id, CancellationToken cancellationToken)
      {
        var job = await _dbContext.DownloadJobs
          .AsNoTracking()
          .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
          
        if (job == null)
          throw new KeyNotFoundException($"Download job dengan ID '{id}' tidak ditemukan.");

        return new DownloadStatusResponse
        {
            Id = job.Id,
            Url = job.Url,
            Format = job.Format,
            Quality = job.Quality,
            Status = job.Status.ToString(),
            Progress = job.Progress,
            OutputPath = job.OutputPath,
            Error = job.Error,
            LastMessage = job.LastMessage,
            CreatedAt = job.CreatedAt,
            FinishedAt = job.FinishedAt
        };
      }

      public async Task DeleteFileAsync(string id, CancellationToken cancellationToken)
      {
        var job = await _dbContext.DownloadJobs
          .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (job == null)
          throw new KeyNotFoundException($"Download job dengan ID '{id}' tidak ditemukan.");

        if (string.IsNullOrWhiteSpace(job.OutputPath))
          throw new InvalidOperationException("File sudah dihapus.");

        var filePath = Path.Combine(Directory.GetCurrentDirectory(), job.OutputPath);

        if (System.IO.File.Exists(filePath))
        {
          System.IO.File.Delete(filePath);
        }

        job.OutputPath = null;
        job.LastMessage = "File telah dihapus.";
        await _dbContext.SaveChangesAsync(cancellationToken);
      }

      public async Task UpdateLastAccessedAsync(string id, CancellationToken cancellationToken)
      {
        var job = await _dbContext.DownloadJobs
          .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (job == null)
          throw new KeyNotFoundException($"Download job dengan ID '{id}' tidak ditemukan.");

        job.LastAccessedAt = DateTimeOffset.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
      }

    }
}
