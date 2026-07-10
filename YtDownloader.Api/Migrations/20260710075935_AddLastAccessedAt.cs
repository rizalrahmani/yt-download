using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace YtDownloader.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddLastAccessedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "last_accessed_at",
                table: "download_jobs",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "last_accessed_at",
                table: "download_jobs");
        }
    }
}
