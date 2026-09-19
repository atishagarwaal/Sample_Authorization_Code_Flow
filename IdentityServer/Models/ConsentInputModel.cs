namespace IdentityServer.Models;

public class ConsentInputModel
{
    public string? Button { get; set; }
    public List<string> ScopesConsented { get; set; } = [];
    public bool RememberConsent { get; set; }
    public string? ReturnUrl { get; set; }
}
