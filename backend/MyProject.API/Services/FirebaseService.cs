using System.Text.Json;

namespace MyProject.API.Services;

public class FirebaseService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public FirebaseService(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    public async Task<FirebaseUserInfo> VerifyTokenAsync(string idToken)
    {
        if (string.IsNullOrEmpty(idToken))
        {
            throw new Exception("Token is required");
        }

        var apiKey = _configuration["Firebase:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new Exception("Firebase API key not configured");
        }

        try
        {
            // Use Firebase REST API to verify token
            var url = $"https://identitytoolkit.googleapis.com/v1/accounts:lookup?key={apiKey}";
            var requestBody = new { idToken };
            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(url, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"Firebase token verification failed: {responseContent}");
            }

            var result = JsonSerializer.Deserialize<FirebaseLookupResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result?.Users == null || result.Users.Length == 0)
            {
                throw new Exception("User not found in Firebase");
            }

            var user = result.Users[0];
            return new FirebaseUserInfo
            {
                Uid = user.LocalId,
                Email = user.Email,
                EmailVerified = user.EmailVerified,
                DisplayName = user.DisplayName
            };
        }
        catch (Exception ex)
        {
            throw new Exception($"Token verification failed: {ex.Message}");
        }
    }
}

public class FirebaseUserInfo
{
    public string Uid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string? DisplayName { get; set; }
}

public class FirebaseLookupResponse
{
    public FirebaseUser[]? Users { get; set; }
}

public class FirebaseUser
{
    public string LocalId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool EmailVerified { get; set; }
    public string? DisplayName { get; set; }
}

