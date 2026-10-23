using Microsoft.EntityFrameworkCore;
using SmartShopAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Register the MySQL connection with Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SmartShopContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. Register controller services (required for controller-based APIs)
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// 3. Map controller routes to the application
app.MapControllers();

// (Optional) Remove the unused WeatherForecast sample code

app.Run();