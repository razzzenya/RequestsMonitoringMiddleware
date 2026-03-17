using Microsoft.EntityFrameworkCore;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Extensions;
using RequestMonitoring.Library.Middleware.Services.DomainCache;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<DomainListsContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.AddRedisDistributedCache("cache");

builder.Services.AddScoped<IDomainCacheService, DomainCacheService>();

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

app.UseCors("AllowAdminPanel");
app.UseHttpsRedirection();
app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
