using Microsoft.EntityFrameworkCore;
using YtDownloader.Api.Data;

namespace YtDownloader.Api.Services.Impl
{
    public sealed class FileCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<FileCleanupService> _logger;

        public FileCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<FileCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupFilesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gagal menjalankan cleanup file.");
                }

                await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
            }
        }

        private async Task CleanupFilesAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cutoff = DateTimeOffset.UtcNow.AddMinutes(-30);

            var expiredJobs = await dbContext.DownloadJobs
                .Where(x => x.Status == Models.DownloadStatus.Completed
                    && x.OutputPath != null
                    && x.LastAccessedAt == null
                    && x.CreatedAt <= cutoff)
                .ToListAsync(cancellationToken);

            foreach (var job in expiredJobs)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), job.OutputPath);

                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                    _logger.LogInformation("File {Path} dihapus (job {Id}).", job.OutputPath, job.Id);
                }

                job.OutputPath = null;
                job.LastMessage = "File dihapus otomatis (expired).";
            }

            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
