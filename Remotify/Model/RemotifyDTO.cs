using Newtonsoft.Json;

namespace Remotify.Model
{
    public class RemotifyDTO
    {
        [JsonProperty("accessToken")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonProperty("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
        [JsonProperty("expiresIn")]
        public int ExpiresIn { get; set; }
        [JsonProperty("type")]
        public int Type { get; set; }
        [JsonProperty("code")]
        public string Code { get; set; } = string.Empty;
    }
}