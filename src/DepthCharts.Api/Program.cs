using DepthCharts.Api.Middlewares;
using DepthCharts.Application.Abstractions;
using DepthCharts.Application.Services;
using DepthCharts.Domain.Abstractions;
using DepthCharts.Infrastructure.Abstractions;
using DepthCharts.Infrastructure.Configuration;
using DepthCharts.Infrastructure.Repositories;
using DepthCharts.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<DataSettings>(builder.Configuration.GetSection(DataSettings.SectionName));

builder.Services.AddScoped<IDepthChartService, DepthChartService>();
builder.Services.AddSingleton<IDepthChartRepository, JsonDepthChartRepository>();
builder.Services.AddScoped<IDataSeeder, JsonDataSeeder>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/index.html"));
app.MapGet("/health", () => Results.Ok(new { 
    status = "healthy", 
    timestamp = DateTime.UtcNow 
}));

var seedingEnabled = app.Configuration.GetValue<bool>("DataSeeding:Enabled", false);
var seedSampleData = app.Configuration.GetValue<bool>("DataSeeding:SeedSampleDepthChart", false);

if (seedingEnabled && seedSampleData)
{
    using var scope = app.Services.CreateScope();
    var seeder = scope.ServiceProvider.GetRequiredService<IDataSeeder>();
    await seeder.SeedAsync();
}

app.Run();
