using RequestMonitoringLibrary.Middleware;
using RequestMonitoringLibrary.Middleware.Services.DomainCheck;
using RequestMonitoringLibrary.Middleware.Services.FileReader;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddSingleton<IFileReaderService, FileReaderService>();
builder.Services.AddSingleton<IDomainCheckService, DomainCheckService>();

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

app.UseMiddleware<RequestMonitoring>();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
