using Duende.IdentityServer.Services;
using IdentityServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServer.Controllers;

[AllowAnonymous]
public class HomeController : Controller
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IWebHostEnvironment _environment;

    public HomeController(IIdentityServerInteractionService interaction, IWebHostEnvironment environment)
    {
        _interaction = interaction;
        _environment = environment;
    }

    public IActionResult Index()
    {
        return View();
    }

    public async Task<IActionResult> Error(string? errorId, CancellationToken ct)
    {
        var vm = new ErrorViewModel();
        var message = await _interaction.GetErrorContextAsync(errorId, ct);
        if (message != null)
        {
            vm.Error = message.Error;
            vm.ErrorDescription = _environment.IsDevelopment() ? message.ErrorDescription : null;
        }

        return View(vm);
    }
}
