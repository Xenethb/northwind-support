namespace SupportDesk.Application.Contracts.Tickets;

/// <summary>
/// Human-readable explanation of the automatic triage decision
/// (priority, SLA window and assignment).
/// </summary>
public sealed class TriageResultDto
{
    public string PriorityReason { get; set; } = string.Empty;
    public string SlaReason { get; set; } = string.Empty;
    public string AssignmentReason { get; set; } = string.Empty;
}