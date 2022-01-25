using Newtonsoft.Json;

namespace VSvsSpotify.Model
{
    public class VSvsSpotifySettings
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
        [JsonProperty("clientId")]
        public string ClientId { get; set; } = string.Empty;
        [JsonProperty("expires")]
        public DateTimeOffset? Expires { get; set; }
        [JsonProperty("vSvsSpotifyBackend")]
        public string VSvsSpotifyBackend { get; set; } = string.Empty;
    }
}
