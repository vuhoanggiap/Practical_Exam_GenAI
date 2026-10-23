using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartShopAPI.Data;
using System.Text;
using System.Text.Json;

namespace SmartShopAPI.Controllers
{
    [Route("api/products")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly SmartShopContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ProductsController(SmartShopContext context, IConfiguration configuration, HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [HttpPost("{id}/generatedescription")]
        public async Task<IActionResult> GenerateDescription(int id)
        {
            // 1. Lấy API Key từ User Secrets
            var apiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { message = "Gemini API Key chưa được cấu hình!" });
            }

            // 2. Tìm sản phẩm trong cơ sở dữ liệu
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"Không tìm thấy sản phẩm với ID = {id}" });
            }

            try
            {
                // 3. Endpoint gọi Gemini REST API với model gemini-2.5-flash[cite: 3]
                // Đổi từ gemini-2.5-flash thành gemini-3.8-flash
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.8-flash:generateContent?key={apiKey}";

                // 4. Chuẩn bị payload theo chuẩn Gemini API (kèm System Instruction)
                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[]
                        {
                            new { text = "You are a professional copywriter. Write a product description of at most 80 words based on the product name, category and features." }
                        }
                    },
                    contents = new[]
                    {
                        new
                        {
                            parts = new[]
                            {
                                new { text = $"Product Name: {product.ProductName}, Category: {product.Category}, Features: {product.Features}" }
                            }
                        }
                    }
                };

                string jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 5. Gửi request đến Google Gemini
                var response = await _httpClient.PostAsync(url, httpContent);
                if (!response.IsSuccessStatusCode)
                {
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { message = "Lỗi khi gọi Gemini API", error = errorDetails });
                }

                // 6. Đọc và bóc tách kết quả JSON trả về từ Gemini
                string jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                
                var generatedDescription = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()?
                    .Trim() ?? string.Empty;

                // 7. Lưu mô tả mới vào cột Description trong CSDL
                product.Description = generatedDescription;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                // 8. Trả về kết quả JSON cho client
                return Ok(new
                {
                    productId = product.ProductId,
                    productName = product.ProductName,
                    category = product.Category,
                    price = product.Price,
                    features = product.Features,
                    description = product.Description
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }
    }
}