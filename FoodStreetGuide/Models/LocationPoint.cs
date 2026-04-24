using SQLite;
using System;
using System.Text.Json.Serialization;

namespace doanC_.Models
{
    [Table("LocationPoints")]
    public class LocationPoint
    {
        [PrimaryKey]
        [JsonPropertyName("pointId")]
        public int PointId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("latitude")]
        public double Latitude { get; set; }

        [JsonPropertyName("longitude")]
        public double Longitude { get; set; }

        [JsonPropertyName("radius")]
        public double? Radius { get; set; }

        [JsonPropertyName("audioFile")]
        public string? AudioFile { get; set; }

        [JsonPropertyName("language")]
        public string? Language { get; set; }

        [JsonPropertyName("address")]
        public string? Address { get; set; }

        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("image")]
        public string? Image { get; set; }

        [JsonPropertyName("rating")]
        public double? Rating { get; set; }

        [JsonPropertyName("reviewCount")]
        public int? ReviewCount { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("openingHours")]
        public string? OpeningHours { get; set; }

        [JsonPropertyName("priceRange")]
        public string? PriceRange { get; set; }

        [JsonPropertyName("createdBy")]
        public int? CreatedBy { get; set; }

        [JsonPropertyName("isApproved")]
        public bool IsApproved { get; set; }

        // 👉 Xóa dòng này nếu có
        // public virtual AdminUser? Admin { get; set; }

        [JsonIgnore]
        public double Distance { get; set; } = 0;

        [JsonIgnore]
        public double RatingValue => Rating ?? 0;

        [JsonIgnore]
        public string DisplayRating => RatingValue > 0 ? $"⭐ {RatingValue:F1}" : "Chưa có đánh giá";

        [JsonIgnore]
        public string DisplayAddress => string.IsNullOrEmpty(Address) ? "Đang cập nhật" : Address;
    }
}