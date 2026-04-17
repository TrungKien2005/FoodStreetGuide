using System;
using System.Text.Json.Serialization;

namespace doanC_.Models
{
    public class DeviceInfoModel
    {
        [JsonPropertyName("deviceUniqueId")]
        public string DeviceUniqueId { get; set; } = string.Empty;

        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; } = string.Empty;

        [JsonPropertyName("platform")]
        public string Platform { get; set; } = string.Empty;

        [JsonPropertyName("osVersion")]
        public string OsVersion { get; set; } = string.Empty;

        [JsonPropertyName("appVersion")]
        public string AppVersion { get; set; } = string.Empty;

        [JsonPropertyName("lastLocationLat")]
        public double? LastLocationLat { get; set; }

        [JsonPropertyName("lastLocationLng")]
        public double? LastLocationLng { get; set; }
    }
}