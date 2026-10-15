

using Microsoft.AspNetCore.Identity;
using SupportDesk.Application.Abstractions;
using SupportDesk.Application.Contracts.Auth;
using SupportDesk.Application.Features.Auth.Login;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Domain.Repositories;

namespace SupportDesk.UnitTests.Application.Features.Auth.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IAgentRepository> _agents = new();
    private readonly Mock<IJwtTokenService> _tokens = new();
    private readonly IPasswordHasher<Agent> _hasher =
        new PasswordHasher<Agent>();

    private readonly DateTime _expires =
        new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private LoginCommandHandler CreateHandler() =>
        new(_agents.Object, _hasher, _tokens.Object);

    private Agent CreateAgent(
        bool isActive = true,
        bool hasPassword = true)
    {
        var agent = new Agent(
            "Alex Turner",
            "alex.turner@northwind-support.example",
            10,
            DateTime.UtcNow);

        if (!isActive)
        {
            agent.Deactivate();
        }

        if (hasPassword)
        {
            agent.SetPasswordHash(
                _hasher.HashPassword(
                    agent,
                    "DevPassword123!"));
        }

        return agent;
    }

    private void SetupAgent(Agent? agent)
    {
        _agents
            .Setup(r => r.GetByEmailAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(agent);
    }

    private static LoginRequest Request(
        string password = "DevPassword123!") =>
        new()
        {
            Email = "alex.turner@northwind-support.example",
            Password = password
        };

    [Fact]
    public async Task HandleAsync_ValidCredentials_ReturnsTokenAndUser()
    {
        var agent = CreateAgent();
        SetupAgent(agent);

        _tokens
            .Setup(t => t.CreateToken(agent))
            .Returns(("test-jwt-token", _expires));

        var result = await CreateHandler().HandleAsync(
            Request(),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("test-jwt-token", result.Token);
        Assert.Equal(_expires, result.ExpiresAtUtc);
        Assert.Equal(agent.Id, result.User.Id);
        Assert.Equal(agent.FullName, result.User.FullName);
        Assert.Equal(agent.Email, result.User.Email);

        _tokens.Verify(
            t => t.CreateToken(agent),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WrongPassword_ReturnsNull()
    {
        var agent = CreateAgent();
        SetupAgent(agent);

        var result = await CreateHandler().HandleAsync(
            Request("WrongPassword123!"),
            CancellationToken.None);

        Assert.Null(result);

        _tokens.Verify(
            t => t.CreateToken(It.IsAny<Agent>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_UnknownEmail_ReturnsNull()
    {
        SetupAgent(null);

        var result = await CreateHandler().HandleAsync(
            Request(),
            CancellationToken.None);

        Assert.Null(result);

        _tokens.Verify(
            t => t.CreateToken(It.IsAny<Agent>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_InactiveAgent_ReturnsNull()
    {
        var agent = CreateAgent(isActive: false);
        SetupAgent(agent);

        var result = await CreateHandler().HandleAsync(
            Request(),
            CancellationToken.None);

        Assert.Null(result);

        _tokens.Verify(
            t => t.CreateToken(It.IsAny<Agent>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_MissingPasswordHash_ReturnsNull()
    {
        var agent = CreateAgent(hasPassword: false);
        SetupAgent(agent);

        var result = await CreateHandler().HandleAsync(
            Request(),
            CancellationToken.None);

        Assert.Null(result);

        _tokens.Verify(
            t => t.CreateToken(It.IsAny<Agent>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_TrimsEmailBeforeLookup()
    {
        var agent = CreateAgent();
        SetupAgent(agent);

        _tokens
            .Setup(t => t.CreateToken(agent))
            .Returns(("test-jwt-token", _expires));

        var request = Request();
        request.Email = "  alex.turner@northwind-support.example  ";

        var result = await CreateHandler().HandleAsync(
            request,
            CancellationToken.None);

        Assert.NotNull(result);

        _agents.Verify(
            r => r.GetByEmailAsync(
                "alex.turner@northwind-support.example",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
