using backend.Models;
using backend.Services;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace backend.Tests.Services
{
    public class TokenServiceTests
    {
        private static IConfiguration CreateConfig()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                { "Jwt:Key", "this-is-a-test-signing-key-thats-long-enough-256bit" },
                { "Jwt:Issuer", "TestIssuer" },
                { "Jwt:Audience", "TestAudience" }
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public void HashToken_SameInput_ProducesSameHash()
        {
            var service = new TokenService(CreateConfig());

            var hash1 = service.HashToken("my-raw-token");
            var hash2 = service.HashToken("my-raw-token");

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void HashToken_DifferentInput_ProducesDifferentHash()
        {
            var service = new TokenService(CreateConfig());

            var hash1 = service.HashToken("token-a");
            var hash2 = service.HashToken("token-b");

            Assert.NotEqual(hash1, hash2);
        }

        [Fact]
        public void GenerateRefreshToken_ProducesUniqueValuesEachCall()
        {
            var service = new TokenService(CreateConfig());

            var token1 = service.GenerateRefreshToken();
            var token2 = service.GenerateRefreshToken();

            Assert.NotEqual(token1, token2);
        }

        [Fact]
        public void GenerateAccessToken_ContainsExpectedClaims()
        {
            var service = new TokenService(CreateConfig());
            var user = new User { Id = 42, Username = "diar", Email = "diar@test.com" };

            var jwt = service.GenerateAccessToken(user);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            Assert.Equal("42", token.Claims.First(c => c.Type == "userId").Value);
            Assert.Equal("diar", token.Claims.First(c => c.Type == "username").Value);
            Assert.Equal("diar@test.com", token.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        }

        [Fact]
        public void GenerateAccessToken_ExpiresApproximately15MinutesFromNow()
        {
            var service = new TokenService(CreateConfig());
            var user = new User { Id = 1, Username = "diar", Email = "diar@test.com" };

            var jwt = service.GenerateAccessToken(user);
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);

            var expectedExpiry = DateTime.UtcNow.AddMinutes(15);
            Assert.True(Math.Abs((token.ValidTo - expectedExpiry).TotalSeconds) < 30); // allow small timing slack
        }
    }
}