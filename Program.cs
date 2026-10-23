using Microsoft.EntityFrameworkCore;
using SmartShopAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký kết nối MySQL với Entity Framework Core
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SmartShopContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. Đăng ký dịch vụ Controllers (bắt buộc để chạy các API dạng Controller)
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

// 3. Map các route của Controller vào ứng dụng
app.MapControllers();

// (Tùy chọn) Bạn có thể xóa đoạn code mẫu WeatherForecast cũ đi vì không dùng đến

app.Run();