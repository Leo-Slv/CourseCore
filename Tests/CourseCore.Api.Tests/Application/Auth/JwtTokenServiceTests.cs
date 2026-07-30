using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using CourseCore.Api.Modules.Auth.Application.Constants;
using CourseCore.Api.Modules.Auth.Infrastructure.Security;
using CourseCore.Api.Tests.TestDoubles;
using Microsoft.Extensions.Options;

namespace CourseCore.Api.Tests.Application.Auth;

public class JwtTokenServiceTests
{
    [Fact]
    public async Task GenerateAccessTokenAsync_ShouldIncludeTokenVersionClaim()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = "CourseCore.UnitTests",
            Audience = "CourseCore.UnitTests",
            SecretKey = "unit-test-secret-key-32-characters-minimum",
            AccessTokenExpirationMinutes = 60,
            RefreshTokenExpirationDays = 7
        }));
        var user = TestEntityFactory.User(tokenVersion: 7);

        var token = await service.GenerateAccessTokenAsync(user, [], []);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Contains(jwt.Claims, claim =>
            claim.Type == AuthClaimTypes.TokenVersion
            && claim.Value == user.TokenVersion.ToString(CultureInfo.InvariantCulture));
    }
}
