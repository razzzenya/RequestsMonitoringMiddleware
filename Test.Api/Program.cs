using Microsoft.EntityFrameworkCore;
using RequestMonitoringLibrary.Context;
using RequestMonitoringLibrary.Middleware;
using RequestMonitoringLibrary.Middleware.Services.DomainCheck;
using RequestMonitoringLibrary.Middleware.Services.OpenSearchLog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DomainListsContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));
// Add services to the container.
builder.Services.AddScoped<IDomainCheckService, DomainCheckService>();
builder.Services.AddSingleton<IOpenSearchLogService, OpenSearchLogService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestLogging>();
app.UseMiddleware<RequestMonitoring>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
//for pr
public partial class Program { }