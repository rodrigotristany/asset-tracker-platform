using System.Net.Http.Json;
using AssetTracker.Application.Dtos;

namespace AssetTracker.Tests.Integration;

public static class TestAuthHelper
{
    public static async Task<string> GetAdminJwtAsync(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login",
            new LoginRequestDto { Username = "admin", Password = "ChangeMe123!" });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return body!.Token;
    }
}
