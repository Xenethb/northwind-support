
using SupportDesk.Domain.Aggregates.Agents;

namespace SupportDesk.Application.Abstractions;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateToken(Agent agent);
}
