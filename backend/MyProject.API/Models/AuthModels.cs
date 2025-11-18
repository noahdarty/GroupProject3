namespace MyProject.API.Models;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class VerifyTokenRequest
{
    public string IdToken { get; set; } = string.Empty;
    public string? Role { get; set; }
    public int? CompanyId { get; set; }
}

public class AuthResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public UserInfo? User { get; set; }
    public string? IdToken { get; set; }
}

public class UserInfo
{
    public string Uid { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Role { get; set; }
    public string? TlpRating { get; set; }
    public string? CompanyName { get; set; }
    public bool EmailVerified { get; set; }
}

