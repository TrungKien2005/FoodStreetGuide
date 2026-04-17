// doanC_Admin/Models/OwnerCreateLocationModel.cs
namespace doanC_Admin.Models
{
    public class OwnerCreateLocationModel
    {
        public int PointId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Address { get; set; }
        public string? Category { get; set; }
        public string? Image { get; set; }
        public string? OpeningHours { get; set; }
        public string? PriceRange { get; set; }
    }
}