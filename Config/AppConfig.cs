using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace DiscordStreamEchoFix.Config
{
    public class DeviceConfig
    {
        [JsonPropertyName("friendlyName")]
        public string FriendlyName { get; set; } = string.Empty;

        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("ignored")]
        public bool Ignored { get; set; } = false;
    }

    public class AppConfig
    {
        [JsonPropertyName("checkIntervalMs")]
        public int CheckIntervalMs { get; set; } = 1000;

        [JsonPropertyName("showNotifications")]
        public bool ShowNotifications { get; set; } = true;

        [JsonPropertyName("devices")]
        public List<DeviceConfig> Devices { get; set; } = new List<DeviceConfig>();

        // Helper property for retrieving list of ignored devices
        [JsonIgnore]
        public List<string> IgnoredDeviceNames =>
            Devices.Where(d => d.Ignored).Select(d => d.FriendlyName).ToList();
    }

    public class RootConfig
    {
        [JsonPropertyName("AppConfig")]
        public AppConfig AppConfig { get; set; } = new AppConfig();
    }
}
