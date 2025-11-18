namespace MyProject.API.Models;

public class CreateCompanyRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Industry { get; set; }
}

public class UpdateUserRequest
{
    public int CompanyId { get; set; }
    public string Role { get; set; } = string.Empty;
}



