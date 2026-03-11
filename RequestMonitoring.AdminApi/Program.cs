using Microsoft.EntityFrameworkCore;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Extensions;
using RequestMonitoring.Library.Middleware.Services.DomainCheck;
using RequestMonitoring.Library.Middleware.Services.OpenSearchLog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<DomainListsContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddOpenSearchClient(builder.Configuration);
builder.Services.AddScoped<IDomainCheckService, DomainCheckService>();
builder.Services.AddScoped<IOpenSearchLogService, OpenSearchLogService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAdminPanel", policy =>
    {
        policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() 
                ?? ["https://localhost:7043", "http://localhost:5157"]
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Request Monitoring Admin API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAdminPanel");
app.MapControllers();
app.Run();
