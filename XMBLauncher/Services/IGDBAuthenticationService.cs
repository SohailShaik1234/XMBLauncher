using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace XMBLauncher.Services;

public class IGDBAuthenticationService
{
    private readonly HttpClient httpClient;

    private readonly string clientId;

    private readonly string clientSecret;


    public IGDBAuthenticationService(
        string clientId,
        string clientSecret)
    {
        httpClient =
            new HttpClient();

        this.clientId =
            clientId;

        this.clientSecret =
            clientSecret;
    }


    public async Task<string> GetAccessTokenAsync()
    {
        if (string.IsNullOrWhiteSpace(clientId))
        {
            throw new InvalidOperationException(
                "IGDB Client ID is missing.");
        }


        if (string.IsNullOrWhiteSpace(clientSecret))
        {
            throw new InvalidOperationException(
                "IGDB Client Secret is missing.");
        }


        string url =
            "https://id.twitch.tv/oauth2/token";


        string requestUrl =
            $"{url}" +
            $"?client_id={Uri.EscapeDataString(clientId)}" +
            $"&client_secret={Uri.EscapeDataString(clientSecret)}" +
            "&grant_type=client_credentials";


        using HttpResponseMessage response =
            await httpClient.PostAsync(
                requestUrl,
                null);


        string responseContent =
            await response.Content.ReadAsStringAsync();


        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"Twitch authentication failed.\n\n" +
                $"Status: {(int)response.StatusCode} " +
                $"{response.StatusCode}\n\n" +
                responseContent);
        }


        using JsonDocument json =
            JsonDocument.Parse(
                responseContent);


        if (!json.RootElement.TryGetProperty(
                "access_token",
                out JsonElement tokenElement))
        {
            throw new Exception(
                "Twitch authentication succeeded, " +
                "but no access token was returned.");
        }


        string? accessToken =
            tokenElement.GetString();


        if (string.IsNullOrWhiteSpace(accessToken))
        {
            throw new Exception(
                "Twitch returned an empty access token.");
        }


        return accessToken;
    }
}