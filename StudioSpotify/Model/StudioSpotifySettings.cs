using Newtonsoft.Json;

namespace StudioSpotify.Model
{
    public class StudioSpotifySettings
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
        [JsonProperty("clientId")]
        public string ClientId { get; set; } = string.Empty;
        [JsonProperty("expires")]
        public DateTimeOffset? Expires { get; set; }
        [JsonProperty("studioSpotifyBackend")]
        public string StudioSpotifyBackend { get; set; } = string.Empty;
    }
}