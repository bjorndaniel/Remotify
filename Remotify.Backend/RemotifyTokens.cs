using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Remotify.Functions
{
    public static class RemotifyTokens
    {
        [FunctionName("RemotifyTokens")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("Remotify token endpoint called");
            var data = await JsonSerializer.DeserializeAsync<RemotifyDTO>(req.Body);
            log.LogInformation(JsonSerializer.Serialize(data));
            var clientId = Environment.GetEnvironmentVariable("ClientId", EnvironmentVariableTarget.Process);
            var clientSecret = Environment.GetEnvironmentVariable("ClientSecret", EnvironmentVariableTarget.Process);
            if (data.Type == 0)
            {
                var response = await new OAuthClient().RequestToken(
                    new AuthorizationCodeTokenRequest(clientId, clientSecret,
                    data.Code, new Uri("http://localhost:5781"))
                );
                if (response.IsExpired)
                {
                    var refreshResponse = await new OAuthClient().RequestToken(
                        new AuthorizationCodeRefreshRequest(clientId, clientSecret, data.RefreshToken)
                    );
                    return new OkObjectResult(new RemotifyDTO
                    {
                        AccessToken = refreshResponse.AccessToken,
                        ExpiresIn = refreshResponse.ExpiresIn
                    });
                }
                return new OkObjectResult(new RemotifyDTO
                {
                    AccessToken = response.AccessToken,
                    RefreshToken = response.RefreshToken,
                    ExpiresIn = response.ExpiresIn
                });
            }
            else if (data.Type == 1)
            {
                var response = await new OAuthClient().RequestToken(
                    new AuthorizationCodeRefreshRequest(clientId, clientSecret, data.RefreshToken)
                );
                return new OkObjectResult(new RemotifyDTO
                {
                    AccessToken = response.AccessToken,
                    ExpiresIn = response.ExpiresIn
                });
            }
            return new BadRequestResult();
        }
    }

    public class RemotifyDTO
    {
        [JsonPropertyName("accessToken")]
        public string AccessToken { get; set; } = string.Empty;
        [JsonPropertyName("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;
        [JsonPropertyName("expiresIn")]
        public int ExpiresIn { get; set; }
        [JsonPropertyName("type")]
        public int Type { get; set; }
        [JsonPropertyName("code")]
        public string Code { get; set; }
    }

}
