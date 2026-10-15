
using Microsoft.EntityFrameworkCore;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Repositories;
using SupportDesk.Infrastructure.Data;

namespace SupportDesk.Infrastructure.Repositories;

public sealed class AgentRepository(SupportDbContext db) : IAgentRepository
{
    /// <summary>
    /// Loads the whole aggregate, specializations included.
    /// </summary>
    public Task<Agent?> GetByIdAsync(int id, CancellationToken ct) =>
        db.Set<Agent>()
            .Include(a => a.Specializations)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    /// <summary>
    /// Finds an agent by email address for login.
    /// </summary>
    public Task<Agent?> GetByEmailAsync(string email, CancellationToken ct)
    {
        var normalizedEmail = email.Trim();

        return db.Set<Agent>()
            .FirstOrDefaultAsync(
                a => a.Email == normalizedEmail,
                ct);
    }
}
