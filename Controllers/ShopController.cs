using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;

namespace SmartShopAPI.Controllers
{
    [Route("api/shop")]
    [ApiController]
    public class ShopController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public ShopController(IConfiguration configuration, HttpClient httpClient)
        {
            _configuration = configuration;
            _httpClient = httpClient;
        }

        [HttpPost("chat")]
        public async Task<IActionResult> ChatWithAssistant([FromBody] ChatDto chatDto)
        {
            // 1. Check for the API key in User Secrets
            var apiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { message = "Gemini API key is not configured." });
            }

            if (chatDto == null || string.IsNullOrEmpty(chatDto.Message))
            {
                return BadRequest(new { message = "Message content (Message) cannot be empty." });
            }

            try
            {
                // 2. Use the Gemini model
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.8-flash:generateContent?key={apiKey}";

                // 3. Define the chatbot's persona in the system instruction
                string systemInstruction = "You are a helpful, polite, and friendly AI shopping assistant for an e-commerce website named 'Smart Shop'. Help customers find products, answer questions about shopping policies, and give great product recommendations.";

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
                            parts = new[] { new { text = chatDto.Message } }
                        }
                    }
                };

                string jsonContent = JsonSerializer.Serialize(requestBody);
                var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // 4. Send the request to the Google Gemini API
                var response = await _httpClient.PostAsync(url, httpContent);
                if (!response.IsSuccessStatusCode)
                {
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { message = "Error calling the Gemini API.", error = errorDetails });
                }

                string jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                
                // Extract the AI's response text
                var aiReply = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return Ok(new { reply = aiReply });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "System error.", error = ex.Message });
            }
        }
    }

    public class ChatDto
    {
        public string Message { get; set; } = string.Empty;
    }
}