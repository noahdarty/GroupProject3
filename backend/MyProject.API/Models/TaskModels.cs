namespace MyProject.API.Models;

public class AssignTaskRequest
{
    public int VulnerabilityId { get; set; }
    public int AssignedToUserId { get; set; }
    public string? Priority { get; set; }
    public string? Notes { get; set; }
}

public class UpdateTaskRequest
{
    public string? Status { get; set; }
    public string? Notes { get; set; }
}

public class ClaimTaskRequest
{
    public int VulnerabilityId { get; set; }
    public string? Notes { get; set; }
}

