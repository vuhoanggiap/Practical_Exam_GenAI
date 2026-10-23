using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace SmartShopAPI.Controllers
{
    [Route("api/reviews")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ReviewsController(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [HttpPost("analyze")]
        public async Task<IActionResult> AnalyzeReview([FromBody] ReviewDto reviewDto)
        {
            // 1. Kiểm tra API Key từ User Secrets
            var apiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { message = "Gemini API Key chưa được cấu hình!" });
            }

            if (reviewDto == null || string.IsNullOrEmpty(reviewDto.Content))
            {
                return BadRequest(new { message = "Nội dung đánh giá (Content) không được để trống!" });
            }

            try
            {
                // 2. Sử dụng model gemini-3.8-flash (hoặc gemini-1.5-flash nếu cần)
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.8-flash:generateContent?key={apiKey}";

                // 3. System Instruction yêu cầu Gemini trả về cấu trúc JSON nghiêm ngặt
                string systemInstruction = "You are an AI that analyzes product reviews. Analyze the sentiment and return a STRICT JSON object with keys: sentiment (Positive, Negative, or Neutral), score (float between 0 and 1), and summary (a short summary of the review). Do not include any markdown formatting outside JSON.";

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
                            parts = new[] { new { text = $"Review Content: {reviewDto.Content}" } }
                        }
                    },
                    generationConfig = new
                    {
                        response_mime_type = "application/json" // Buộc Gemini trả về đúng định dạng JSON
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
                
                // Lấy chuỗi JSON mà Gemini sinh ra trong phần text
                var aiText = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()?.Trim();

                // Parse kết quả trả về dưới dạng đối tượng JSON để trả về client dạng đẹp
                var jsonResult = JsonDocument.Parse(aiText!).RootElement;

                return Ok(jsonResult);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Lỗi hệ thống", error = ex.Message });
            }
        }
    }

    public class ReviewDto
    {
        public string Content { get; set; } = string.Empty;
    }
}