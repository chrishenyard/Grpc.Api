using System.Text.Json.Nodes;

namespace Grpc.Service.Client.Tests;

public static class TestHelpers
{
    public static async Task<GrpcHeaders> GetAuthenticationAsync(string method, string group, string server, HttpClient httpClient)
    {
        var responseBody = await httpClient.GetStringAsync($"https://{server}/Authorization/{method}/{group}");

        if (string.IsNullOrEmpty(responseBody))
            throw new Exception("Empty response from authorization endpoint");

        var jsonNode = JsonNode.Parse(responseBody);
        var dataNode = jsonNode?["data"];

        if (dataNode == null)
            throw new Exception("Missing 'data' node in authorization response");

        var headers = new GrpcHeaders
        {
            TimeStamp = dataNode?["timestamp"]?.ToString() ?? string.Empty,
            Signature = dataNode?["signature"]?.ToString() ?? string.Empty
        };

        if (string.IsNullOrEmpty(headers.TimeStamp) || string.IsNullOrEmpty(headers.Signature))
            throw new Exception("Invalid authorization headers returned");

        return headers;
    }

    public static async Task CreateTestJobAsync(string server, HttpClient httpClient)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"https://{server}/CreateTestJob");
        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public static Grpc.Core.Metadata CreateMetadata(string apiKey, string apiSecret, GrpcHeaders headers)
    {
        return new Grpc.Core.Metadata
        {
            { "x-timestamp", headers.TimeStamp },
            { "x-signature", headers.Signature },
            { "x-api-key", apiKey },
            { "x-api-secret", apiSecret }
        };
    }
}