using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using XMBLauncher.Models;

namespace XMBLauncher.Services;

public class IGDBService
{
    private readonly HttpClient httpClient;

    private readonly IGDBAuthenticationService authenticationService;

    private readonly string clientId;


    public IGDBService(
        string clientId,
        string clientSecret)
    {
        httpClient =
            new HttpClient();

        authenticationService =
            new IGDBAuthenticationService(
                clientId,
                clientSecret);

        this.clientId =
            clientId;
    }


    public async Task<string> TestConnectionAsync()
    {
        string accessToken =
            await authenticationService
                .GetAccessTokenAsync();


        string query =
            """
            fields id, name;
            limit 5;
            """;


        using HttpRequestMessage request =
            CreateRequest(query, accessToken);


        using HttpResponseMessage response =
            await httpClient.SendAsync(request);


        string responseContent =
            await response.Content.ReadAsStringAsync();


        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"IGDB request failed.\n\n" +
                $"Status: {(int)response.StatusCode} " +
                $"{response.StatusCode}\n\n" +
                responseContent);
        }


        return responseContent;
    }


    public async Task<List<IGDBGameResult>> SearchGamesAsync(
        string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return new List<IGDBGameResult>();
        }


        string accessToken =
            await authenticationService
                .GetAccessTokenAsync();


        string escapedSearchTerm =
            searchTerm
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");


        string query =
            $"""
            search "{escapedSearchTerm}";
            fields
                id,
                name,
                summary,
                first_release_date,
                cover.url,
                artworks.url,
                involved_companies.company.name,
                involved_companies.developer,
                involved_companies.publisher,
                genres.name;
            limit 20;
            """;


        using HttpRequestMessage request =
            CreateRequest(
                query,
                accessToken);


        using HttpResponseMessage response =
            await httpClient.SendAsync(request);


        string responseContent =
            await response.Content.ReadAsStringAsync();


        if (!response.IsSuccessStatusCode)
        {
            throw new Exception(
                $"IGDB search failed.\n\n" +
                $"Status: {(int)response.StatusCode} " +
                $"{response.StatusCode}\n\n" +
                responseContent);
        }


        return ParseSearchResults(
            responseContent);
    }

    private HttpRequestMessage CreateRequest(
        string query,
        string accessToken)
    {
        HttpRequestMessage request =
            new HttpRequestMessage(
                HttpMethod.Post,
                "https://api.igdb.com/v4/games");


        request.Headers.Add(
            "Client-ID",
            clientId);


        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);


        request.Content =
            new StringContent(
                query,
                Encoding.UTF8,
                "text/plain");


        return request;
    }

    private List<IGDBGameResult> ParseSearchResults(
        string json)
    {
        List<IGDBGameResult> results =
            new List<IGDBGameResult>();


        using JsonDocument document =
            JsonDocument.Parse(json);


        foreach (JsonElement gameElement
                 in document.RootElement.EnumerateArray())
        {
            IGDBGameResult result =
                new IGDBGameResult();



            if (gameElement.TryGetProperty(
                    "id",
                    out JsonElement idElement))
            {
                result.Id =
                    idElement.GetInt32();
            }



            if (gameElement.TryGetProperty(
                    "name",
                    out JsonElement nameElement))
            {
                result.Name =
                    nameElement.GetString() ?? "";
            }



            if (gameElement.TryGetProperty(
                    "summary",
                    out JsonElement summaryElement))
            {
                result.Summary =
                    summaryElement.GetString() ?? "";
            }


            if (gameElement.TryGetProperty(
                    "first_release_date",
                    out JsonElement releaseElement))
            {
                long timestamp =
                    releaseElement.GetInt64();


                DateTime releaseDate =
                    DateTimeOffset
                        .FromUnixTimeSeconds(timestamp)
                        .DateTime;


                result.FirstReleaseDate =
                    releaseDate.ToString("yyyy-MM-dd");
            }


            if (gameElement.TryGetProperty(
                    "cover",
                    out JsonElement coverElement))
            {
                if (coverElement.TryGetProperty(
                        "url",
                        out JsonElement coverUrlElement))
                {
                    result.CoverUrl =
                        ConvertIGDBImageUrl(
                            coverUrlElement.GetString());
                }
            }


            if (gameElement.TryGetProperty(
                    "artworks",
                    out JsonElement artworksElement))
            {
                JsonElement[] artworks =
                    artworksElement
                        .EnumerateArray()
                        .ToArray();


                if (artworks.Length > 0)
                {
                    if (artworks[0].TryGetProperty(
                            "url",
                            out JsonElement artworkUrlElement))
                    {
                        result.BackgroundUrl =
                            ConvertIGDBImageUrl(
                                artworkUrlElement.GetString());
                    }
                }
            }


            if (gameElement.TryGetProperty(
                    "involved_companies",
                    out JsonElement companiesElement))
            {
                foreach (
                    JsonElement companyElement
                    in companiesElement.EnumerateArray())
                {
                    if (!companyElement.TryGetProperty(
                            "company",
                            out JsonElement companyObject))
                    {
                        continue;
                    }


                    if (!companyObject.TryGetProperty(
                            "name",
                            out JsonElement companyNameElement))
                    {
                        continue;
                    }


                    string companyName =
                        companyNameElement.GetString() ?? "";


                    bool isDeveloper =
                        companyElement.TryGetProperty(
                            "developer",
                            out JsonElement developerElement)
                        &&
                        developerElement.GetBoolean();


                    bool isPublisher =
                        companyElement.TryGetProperty(
                            "publisher",
                            out JsonElement publisherElement)
                        &&
                        publisherElement.GetBoolean();


                    if (isDeveloper &&
                        string.IsNullOrWhiteSpace(
                            result.Developer))
                    {
                        result.Developer =
                            companyName;
                    }


                    if (isPublisher &&
                        string.IsNullOrWhiteSpace(
                            result.Publisher))
                    {
                        result.Publisher =
                            companyName;
                    }
                }
            }

            if (gameElement.TryGetProperty(
                    "genres",
                    out JsonElement genresElement))
            {
                List<string> genres =
                    new List<string>();


                foreach (
                    JsonElement genreElement
                    in genresElement.EnumerateArray())
                {
                    if (genreElement.TryGetProperty(
                            "name",
                            out JsonElement genreNameElement))
                    {
                        string genreName =
                            genreNameElement.GetString() ?? "";


                        if (!string.IsNullOrWhiteSpace(
                                genreName))
                        {
                            genres.Add(
                                genreName);
                        }
                    }
                }


                result.Genre =
                    string.Join(
                        ", ",
                        genres);
            }


            results.Add(result);
        }


        return results;
    }


    private string ConvertIGDBImageUrl(
        string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return "";
        }

        string fullUrl =
            url.StartsWith("//")
                ? "https:" + url
                : url;


        return fullUrl.Replace(
            "/t_thumb/",
            "/t_1080p/");
    }
}