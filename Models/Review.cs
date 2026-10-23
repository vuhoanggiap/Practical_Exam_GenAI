using System.ComponentModel.DataAnnotations;

namespace SmartShopAPI.Models
{
    public class Review
    {
        [Key]
        public int ReviewId { get; set; }
        public int ProductId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int Rating { get; set; }
        public string? Sentiment { get; set; }
        public double? Score { get; set; }
        public string? Summary { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}