using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace IdentityServer;

public static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new IdentityResource[]
        {
            new IdentityResources.OpenId(),
            // Add the profile scope to include user profile information
            new IdentityResources.Profile(), 
        };

    // Defines the physical API
    public static IEnumerable<ApiResource> ApiResources =>
        new[]
        {
            new ApiResource("api-weather", "Weather API")
            {
                // Add permissions (scopes) to the API resource
                Scopes = { "weather.read" }
            }
        };


    // Defines the permissions (scopes) that can be requested by clients
    public static IEnumerable<ApiScope> ApiScopes =>
        new ApiScope[]
            {
                new ApiScope("weather.read", "Read Weather Data"),
                new ApiScope("weather.write", "Modify Weather Data")
            };

    public static IEnumerable<Client> Clients =>
        new Client[]
            {
                new Client
                {
                    ClientId = "weather-client-app",

                    ClientSecrets = { new Secret("Pass@word123".Sha256()) },

                    AllowedGrantTypes = GrantTypes.Code,
                    RequireConsent = false,

                    // Where to redirect to after login
                    RedirectUris =
                    {
                        "http://127.0.0.1:7890/" // SampleClient URL
                    },

                    // Set the pemissions (scopes)
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "weather.read"
                    }
                }
            };
}
