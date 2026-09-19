using System.Diagnostics;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Duende.IdentityModel;
using Duende.IdentityModel.Client;


var client = new HttpClient();

// Discover endpoints from metadata
var disco = await client.GetDiscoveryDocumentAsync("https://localhost:5001"); // IdentityServer URL
if (disco.IsError)
{
    Console.WriteLine(disco.Error);
    return 1;
}

// Build the authorization request (authorization code + PKCE)
var codeVerifier = CryptoRandom.CreateUniqueId(32);
var codeChallenge = Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier)));
var state = CryptoRandom.CreateUniqueId();
var nonce = CryptoRandom.CreateUniqueId();

// Build the authorization request URL
var authorizeUrl = new RequestUrl(disco.AuthorizeEndpoint!).CreateAuthorizeUrl(
    clientId: "weather-client-app",
    responseType: "code",
    scope: "openid profile weather.read",
    redirectUri: "http://127.0.0.1:7890/",
    state: state,
    nonce: nonce,
    codeChallenge: codeChallenge,
    codeChallengeMethod: "S256");

// Listen for the redirect, then open the browser so the user can sign in
using var listener = new HttpListener();
listener.Prefixes.Add("http://127.0.0.1:7890/");
listener.Start();

Console.WriteLine("Opening the browser to sign in...");
Process.Start(new ProcessStartInfo
{
    FileName = authorizeUrl,
    UseShellExecute = true
});

var context = await listener.GetContextAsync();
// Parse the authorization response
var authorizeResponse = new AuthorizeResponse(context.Request.Url!.AbsoluteUri);

// Display the authorization code
var displayedCode = WebUtility.HtmlEncode(authorizeResponse.Code ?? "(none)");
var html = $"""
    <html>
    <body>
      <p>Login complete. You can close this window and return to the console.</p>
      <p><strong>Authorization code:</strong></p>
      <pre>{displayedCode}</pre>
    </body>
    </html>
    """;
var buffer = Encoding.UTF8.GetBytes(html);
context.Response.ContentType = "text/html; charset=utf-8";
await context.Response.OutputStream.WriteAsync(buffer);
context.Response.OutputStream.Close();
listener.Stop();

// Check if the authorization response is valid
if (authorizeResponse.IsError)
{
    Console.WriteLine(authorizeResponse.Error);
    Console.WriteLine(authorizeResponse.ErrorDescription);
    return 1;
}

if (!string.Equals(authorizeResponse.State, state, StringComparison.Ordinal))
{
    Console.WriteLine("Invalid state returned from the authorization server.");
    return 1;
}

if (string.IsNullOrWhiteSpace(authorizeResponse.Code))
{
    Console.WriteLine("No authorization code was returned.");
    return 1;
}

// Display the authorization code
Console.WriteLine("Authorization code:");
Console.WriteLine(authorizeResponse.Code);
Console.WriteLine();

// Exchange the authorization code for tokens
var tokenResponse = await client.RequestAuthorizationCodeTokenAsync(new AuthorizationCodeTokenRequest
{
    Address = disco.TokenEndpoint,
    ClientId = "weather-client-app",
    ClientSecret = "Pass@word123",
    Code = authorizeResponse.Code,
    RedirectUri = "http://127.0.0.1:7890/",
    CodeVerifier = codeVerifier
});

// Check if the token response is valid
if (tokenResponse.IsError)
{
    Console.WriteLine(tokenResponse.Error);
    Console.WriteLine(tokenResponse.ErrorDescription);
    return 1;
}

// Display the token response
Console.WriteLine(tokenResponse.Json);
Console.WriteLine("\n\n");

// Call API with the access token
var apiClient = new HttpClient();
apiClient.SetBearerToken(tokenResponse.AccessToken!);

var response = await apiClient.GetAsync("https://localhost:5003/weatherforecast");
if (!response.IsSuccessStatusCode)
{
    Console.WriteLine(response.StatusCode);
    Console.ReadLine();
    return 1;
}

var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
Console.WriteLine(JsonSerializer.Serialize(doc, new JsonSerializerOptions { WriteIndented = true }));
Console.ReadLine();
return 0;

static string Base64UrlEncode(byte[] input) =>
    Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');
