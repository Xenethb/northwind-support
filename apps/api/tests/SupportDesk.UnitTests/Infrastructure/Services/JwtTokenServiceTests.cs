
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SupportDesk.Application.Configuration;
using SupportDesk.Domain.Aggregates.Agents;
using SupportDesk.Infrastructure.Services;

namespace SupportDesk.UnitTests.Infrastructure.Services;

public class JwtTokenServiceTests
{
    private const string SigningKey =
        "TestSigningKey-OnlyForUnitTests-AtLeast32BytesLong!";

    private static JwtOptions CreateOptions() => new()
    {
        Issuer = "NorthwindSupportDesk",
        Audience = "NorthwindSupportDeskWeb",
        SigningKey = SigningKey,
        TokenLifetimeMinutes = 60
    };

    private static TokenValidationParameters CreateValidationParameters() =>
        new()
        {
            ValidateIssuer = true,
            ValidIssuer = "NorthwindSupportDesk",

            ValidateAudience = true,
            ValidAudience = "NorthwindSupportDeskWeb",

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(SigningKey)),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

    [Fact]
    public void CreateToken_ReturnsAValidSignedJwt()
    {
        var options = CreateOptions();
        var service = new JwtTokenService(Options.Create(options));

        var agent = new Agent(
            "Alex Turner",
            "alex.turner@northwind-support.example",
            10,
            DateTime.UtcNow);

        var (token, expiresAtUtc) = service.CreateToken(agent);

        var handler = new JwtSecurityTokenHandler();

        var principal = handler.ValidateToken(
            token,
            CreateValidationParameters(),
            out var validatedToken);

        Assert.NotNull(validatedToken);
        Assert.Equal(agent.FullName,
            principal.FindFirstValue(ClaimTypes.Name));
        Assert.Equal(
            agent.Id.ToString(),
            principal.FindFirst("agent_id")?.Value);
        Assert.True(expiresAtUtc > DateTime.UtcNow);
    }

    [Fact]
    public void ValidateToken_WhenExpired_ThrowsSecurityTokenExpiredException()
    {
        var now = DateTime.UtcNow;
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(SigningKey));

        var token = new JwtSecurityToken(
            issuer: "NorthwindSupportDesk",
            audience: "NorthwindSupportDeskWeb",
            claims:
            [
                new Claim(ClaimTypes.Name, "Alex Turner"),
                new Claim("agent_id", "1")
            ],
            notBefore: now.AddMinutes(-10),
            expires: now.AddMinutes(-5),
            signingCredentials: new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256));

        var encoded = new JwtSecurityTokenHandler()
            .WriteToken(token);

        var handler = new JwtSecurityTokenHandler();

        Assert.Throws<SecurityTokenExpiredException>(() =>
            handler.ValidateToken(
                encoded,
                CreateValidationParameters(),
                out _));
    }

    
  [Fact]
  public void ValidateToken_WithWrongSigningKey_RejectsToken()
  {
      var options = CreateOptions();
      var service = new JwtTokenService(Options.Create(options));

      var agent = new Agent(
          "Alex Turner",
          "alex.turner@northwind-support.example",
          10,
          DateTime.UtcNow);

      var (token, _) = service.CreateToken(agent);

      var parameters = CreateValidationParameters();

      parameters.IssuerSigningKey = new SymmetricSecurityKey(
          Encoding.UTF8.GetBytes(
              "AnotherTestSigningKey-AtLeast32BytesLong-Invalid!"));

      var exception = Record.Exception(() =>
          new JwtSecurityTokenHandler().ValidateToken(
              token,
              parameters,
              out _));

      Assert.NotNull(exception);

      Assert.True(
          exception is SecurityTokenInvalidSignatureException
              or SecurityTokenSignatureKeyNotFoundException,
          $"Unexpected exception type: {exception.GetType().Name}");
  }

}
