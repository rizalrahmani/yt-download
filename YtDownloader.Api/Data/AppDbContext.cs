using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using YtDownloader.Api.Models;

namespace YtDownloader.Api.Data
{
    public sealed class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<DownloadJob> DownloadJobs => Set<DownloadJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      modelBuilder.Entity<DownloadJob>(entity =>
      {
        entity.ToTable("download_jobs");
        
        entity.HasKey(x => x.Id);

        entity.Property(x => x.Id)
              .HasColumnName("id")
              .IsRequired()
              .HasMaxLength(32);

        entity.Property(x => x.Url)
              .HasColumnName("url")
              .IsRequired();

        entity.Property(x => x.Format)
              .HasColumnName("format")
              .HasMaxLength(20)
              .IsRequired();

        entity.Property(x => x.Quality)
              .HasColumnName("quality")
              .HasMaxLength(50)
              .IsRequired();

        entity.Property(x => x.Status)
              .HasColumnName("status")
              .HasConversion<string>()
              .HasMaxLength(30)
              .IsRequired();

        entity.Property(x => x.Progress)
              .HasColumnName("progress");

        entity.Property(x => x.OutputPath)
              .HasColumnName("output_path");

        entity.Property(x => x.Error)
              .HasColumnName("error");

        entity.Property(x => x.LastMessage)
              .HasColumnName("last_message");

        entity.Property(x => x.CreatedAt)
              .HasColumnName("created_at");

        entity.Property(x => x.FinishedAt)
              .HasColumnName("finished_at");

        entity.Property(x => x.LastAccessedAt)
              .HasColumnName("last_accessed_at");
      });
    }
  }
}