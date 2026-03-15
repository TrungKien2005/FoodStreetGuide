namespace doanC_.Models
{
    public class LocationPoint
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public LocationPoint(string name, string description, double latitude, double longitude)
        {
            Name = name;
            Description = description;
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}