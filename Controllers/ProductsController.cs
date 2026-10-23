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
            // 1. Retrieve the API key from User Secrets
            var apiKey = _configuration["GeminiApiKey"];
            if (string.IsNullOrEmpty(apiKey))
            {
                return BadRequest(new { message = "Gemini API key is not configured." });
            }

            // 2. Find the product in the database
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound(new { message = $"Product with ID = {id} was not found." });
            }

            try
            {
                // 3. Call the Gemini REST API using the gemini-3.8-flash model
                string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-3.8-flash:generateContent?key={apiKey}";

                // 4. Prepare the payload according to the Gemini API format, including the system instruction
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

                // 5. Send the request to Google Gemini
                var response = await _httpClient.PostAsync(url, httpContent);
                if (!response.IsSuccessStatusCode)
                {
                    string errorDetails = await response.Content.ReadAsStringAsync();
                    return StatusCode((int)response.StatusCode, new { message = "Error calling the Gemini API.", error = errorDetails });
                }

                // 6. Read and extract the JSON response from Gemini
                string jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);
                
                var generatedDescription = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString()?
                    .Trim() ?? string.Empty;

                // 7. Save the new description to the Description column in the database
                product.Description = generatedDescription;
                _context.Products.Update(product);
                await _context.SaveChangesAsync();

                // 8. Return the JSON result to the client
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
                return StatusCode(500, new { message = "System error.", error = ex.Message });
            }
        }
    }
}