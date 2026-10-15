
using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Domain.Repositories;

/// <summary>
/// Loads Agent aggregates.
/// </summary>
public interface IAgentRepository
{
    Task<Agent?> GetByIdAsync(int id, CancellationToken ct);

    /// <summary>
    /// Finds an agent by email address for authentication.
    /// </summary>
    Task<Agent?> GetByEmailAsync(string email, CancellationToken ct);
}
