namespace MyProject.API.Models;

public class VendorSelectionRequest
{
    public List<int> VendorIds { get; set; } = new();
    public Dictionary<int, string>? UseCaseDescriptions { get; set; }
}

public class CompanyVendorResponse
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string VendorType { get; set; } = string.Empty;
    public string? UseCaseDescription { get; set; }
    public bool IsActive { get; set; }
}

