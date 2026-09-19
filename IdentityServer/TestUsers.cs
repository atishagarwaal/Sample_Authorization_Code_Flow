using System.Security.Claims;
using Duende.IdentityServer.Test;

namespace IdentityServer;

internal static class TestUsers
{
    public static List<TestUser> Users { get; } =
    [
        new TestUser
        {
            SubjectId = "1",
            Username = "alice",
            Password = "password",
            Claims =
            {
                new Claim("name", "Alice Smith"),
                new Claim("given_name", "Alice"),
                new Claim("family_name", "Smith"),
                new Claim("email", "alice@example.com")
            }
        },
        new TestUser
        {
            SubjectId = "2",
            Username = "bob",
            Password = "password",
            Claims =
            {
                new Claim("name", "Bob Jones"),
                new Claim("given_name", "Bob"),
                new Claim("family_name", "Jones"),
                new Claim("email", "bob@example.com")
            }
        }
    ];
}
