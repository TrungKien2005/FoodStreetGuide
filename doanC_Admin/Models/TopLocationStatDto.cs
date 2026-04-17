namespace doanC_Admin.Models
{
    public class TopLocationStatDto
    {
        public int PointId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int ScanCount { get; set; }
    }
}