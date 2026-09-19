using Duende.IdentityServer;
using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Services;
using Duende.IdentityServer.Test;
using IdentityServer.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServer.Controllers;

[AllowAnonymous]
public class AccountController : Controller
{
    private readonly TestUserStore _users;
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IEventService _events;

    public AccountController(
        IIdentityServerInteractionService interaction,
        IEventService events,
        TestUserStore users)
    {
        _interaction = interaction;
        _events = events;
        _users = users;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl)
    {
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken ct)
    {
        var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl, ct);

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        if (_users.ValidateCredentials(model.Username, model.Password))
        {
            var user = _users.FindByUsername(model.Username);
            await _events.RaiseAsync(new UserLoginSuccessEvent(
                user.Username,
                user.SubjectId,
                user.Username,
                clientId: context?.Client.ClientId), ct);

            var identityServerUser = new IdentityServerUser(user.SubjectId)
            {
                DisplayName = user.Username
            };

            await HttpContext.SignInAsync(identityServerUser);

            if (context != null)
            {
                return Redirect(model.ReturnUrl!);
            }

            if (Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return Redirect("~/");
        }

        await _events.RaiseAsync(new UserLoginFailureEvent(
            model.Username,
            "invalid credentials",
            clientId: context?.Client.ClientId), ct);
        ModelState.AddModelError(string.Empty, "Invalid username or password");
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Logout(string? logoutId, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return await CompleteLogoutAsync(logoutId, ct);
        }

        var context = await _interaction.GetLogoutContextAsync(logoutId, ct);
        if (context?.ShowSignoutPrompt == false)
        {
            return await CompleteLogoutAsync(logoutId, ct);
        }

        return View(new LogoutViewModel { LogoutId = logoutId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Logout(LogoutViewModel model, CancellationToken ct)
    {
        return CompleteLogoutAsync(model.LogoutId, ct);
    }

    [HttpGet]
    public async Task<IActionResult> LoggedOut(string? logoutId, CancellationToken ct)
    {
        var logout = await _interaction.GetLogoutContextAsync(logoutId, ct);

        return View(new LoggedOutViewModel
        {
            PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
            ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
            SignOutIframeUrl = logout?.SignOutIFrameUrl
        });
    }

    private async Task<IActionResult> CompleteLogoutAsync(string? logoutId, CancellationToken ct)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            logoutId ??= await _interaction.CreateLogoutContextAsync(ct);
            await HttpContext.SignOutAsync();
            await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()), ct);
        }

        return RedirectToAction(nameof(LoggedOut), new { logoutId });
    }
}
