using Newtonsoft.Json;

namespace Remotify.Model
{
    public class RemotifySettings
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
        [JsonProperty("clientId")]
        public string ClientId { get; set; } = string.Empty;
        [JsonProperty("expires")]
        public DateTimeOffset? Expires { get; set; }
        [JsonProperty("remotifyBackend")]
        public string RemotifyBackend { get; set; } = string.Empty;
    }
}