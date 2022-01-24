using Newtonsoft.Json;

namespace Spotimote.Model
{
    public class SpotimoteSettings
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
        [JsonProperty("clientId")]
        public string ClientId { get; set; } = string.Empty;
        [JsonProperty("expires")]
        public DateTimeOffset? Expires { get; set; }
        [JsonProperty("spotimoteBackend")]
        public string SpotimoteBackend { get; set; } = string.Empty;
    }
}
