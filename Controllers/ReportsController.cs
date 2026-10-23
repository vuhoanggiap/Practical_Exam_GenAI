using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShopAPI.Data;
using System.Text;
using System.Text.Json;

namespace SmartShopAPI.Controllers
{
    [Route("api/reports")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        private readonly SmartShopContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ReportsController(SmartShopContext context, IConfiguration configuration, HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [HttpGet("generate")]
        public async Task<IActionResult> GenerateBusinessReport()
        {
            // 1. Kiểm tra API Key từ User Secrets
            var apiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { message = "Gemini API Key chưa được cấu hình!" });
            }

            try
            {
                // 2. Truy vấn dữ liệu thực tế từ Database thông qua EF Core
                var totalProducts = await _context.Products.CountAsync();
                var sampleProducts = await _context.Products.Take(10).ToListAsync();

                // Đổi p.Name thành p.ProductName (hoặc p.Title nếu model của bạn dùng Title)
                string productDataSummary = string.Join("; ", sampleProducts.Select(p => $"{p.ProductName} (Giá: {p.Price})"));

                // 3. Cấu hình gọi Gemini API
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.8-flash:generateContent?key={apiKey}";

                string systemInstruction = "You are an expert e-commerce business analyst. Analyze the provided store data and generate a professional, structured business summary report with recommendations for growth.";

                var prompt = $"Store Performance Data:\n- Total Products in Catalog: {totalProducts}\n- Sample Products: {productDataSummary}\n\nPlease provide a professional business evaluation report, highlighting product assortment insights and practical recommendations to boost sales.";

                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = systemInstruction } }
                    },
                    contents = new[]
                    {
                        new
                        {
                            parts = new[] { new { text = prompt } }
                        }
                    }
                };

                string jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 4. Gửi request tới Google Gemini API
                var response = await _httpClient.PostAsync(url, httpContent);
                if (!response.IsSuccessStatusCode)
                {
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { message = "Lỗi khi gọi Gemini API", error = errorDetails });
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);

                // Trích xuất nội dung báo cáo do AI sinh ra
                var reportText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return Ok(new { report = reportText });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }
    }
}