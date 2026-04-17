namespace doanC_Admin.Models
{
    public class DashboardStatsDto
    {
        public int TotalLocations { get; set; }
        public int TotalScans { get; set; }
        public int TotalAudios { get; set; }
        public int TotalUsers { get; set; }
        public int TotalGeoFenceEntries { get; set; }
        public int TotalTTSPlays { get; set; }
    }
}