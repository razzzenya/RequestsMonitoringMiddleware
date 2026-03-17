using Microsoft.EntityFrameworkCore;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Extensions;
using RequestMonitoring.Library.Middleware;
using RequestMonitoring.Library.Middleware.Services.DomainCheck;
using RequestMonitoring.Library.Middleware.Services.OpenSearchLog;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<DomainListsContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddOpenSearchClient(builder.Configuration);

builder.Services.AddScoped<IDomainCheckService, DomainCheckService>();
builder.Services.AddScoped<IOpenSearchLogService, OpenSearchLogService>();
builder.AddRedisDistributedCache("cache");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<RequestMonitoringMiddleware>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
