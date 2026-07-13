using YtDownloader.Api.Services.Impl;
using YtDownloader.Api.Services.Interface;
using Microsoft.EntityFrameworkCore;
using YtDownloader.Api.Data;
using YtDownloader.Api.Hubs;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddHttpClient<IYtDlpService, YtDlpService>();
builder.Services.AddScoped<IDownloadService, DownloadService>();
builder.Services.AddSignalR();

builder.Services.AddHostedService<FileCleanupService>();
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHub<DownloadHub>("/hub/download");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();