using Microsoft.AspNetCore.Identity;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Repositories;
using SupportDesk.Application.Abstractions;

namespace SupportDesk.Application.Features.Auth.Login;

public sealed class LoginCommandHandler(
    IAgentRepository agents,
    IPasswordHasher<Agent> passwordHasher,
    IJwtTokenService tokenService)
{
    public async Task<LoginResponse?> HandleAsync(LoginRequest request, CancellationToken ct)
    {
        var agent = await agents.GetByEmailAsync(request.Email.Trim(), ct);

        // Same generic failure for wrong email, wrong password, or inactive agent
        if (agent is null || !agent.IsActive || string.IsNullOrEmpty(agent.PasswordHash))
        {
            return null;
        }

        var result = passwordHasher.VerifyHashedPassword(
            agent, agent.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        var (token, expires) = tokenService.CreateToken(agent);

        return new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expires,
            User = new AuthUserDto
            {
                Id = agent.Id,
                FullName = agent.FullName,
                Email = agent.Email
            }
        };
    }
}