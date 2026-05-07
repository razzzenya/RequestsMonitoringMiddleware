using Mapster;
using Microsoft.EntityFrameworkCore;
using RequestMonitoring.AdminApi.DTO;
using RequestMonitoring.Library.Context;
using RequestMonitoring.Library.Enitites;
using RequestMonitoring.Library.Extensions;
using RequestMonitoring.Library.Middleware.Services.DomainCache;
using RequestMonitoring.Library.Middleware.Services.QuotaCache;
using Scalar.AspNetCore;

TypeAdapterConfig<Domain, DomainDto>.NewConfig()
    .Map(dest => dest.DomainStatusName, src => src.DomainStatusType.Name);

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddDbContext<DomainListsContext>(opt =>
    opt.UseSqlite(builder.Configuration.GetConnectionString("Default")));

builder.AddRedisDistributedCache("cache");

builder.Services.AddScoped<IDomainCacheService, DomainCacheService>();
builder.Services.AddScoped<IQuotaCacheService, QuotaCacheService>();

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
builder.Services.AddAuthentication("Cookies")
    .AddCookie("Cookies", options =>
    {
        options.LoginPath = null;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true;
        options.Events.OnRedirectToLogin = ctx =>
        {
            ctx.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            ctx.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddOpenApi();

builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseCors("AllowAdminPanel");
app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.MapControllers();

app.MapDefaultEndpoints();

app.Run();
