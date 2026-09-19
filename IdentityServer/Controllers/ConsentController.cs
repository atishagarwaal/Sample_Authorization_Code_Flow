using Duende.IdentityServer.Events;
using Duende.IdentityServer.Extensions;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Services;
using IdentityServer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityServer.Controllers;

[AllowAnonymous]
public class ConsentController : Controller
{
    private readonly IIdentityServerInteractionService _interaction;
    private readonly IEventService _events;
    public ConsentController(
        IIdentityServerInteractionService interaction,
        IEventService events)
    {
        _interaction = interaction;
        _events = events;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? returnUrl, CancellationToken ct)
    {
        var vm = await BuildViewModelAsync(returnUrl, ct);
        if (vm is null)
        {
            return RedirectToAction("Index", "Home");
        }

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(ConsentInputModel model, CancellationToken ct)
    {
        var request = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl, ct);
        if (request is null)
        {
            return RedirectToAction("Index", "Home");
        }

        if (model.Button == "no")
        {
            await _events.RaiseAsync(new ConsentDeniedEvent(
                User.GetSubjectId(),
                request.Client.ClientId,
                request.ValidatedResources.RawScopeValues), ct);
            await _interaction.DenyAuthorizationAsync(request, InteractionError.AccessDenied, ct);
            return Redirect(model.ReturnUrl!);
        }

        if (model.Button != "yes")
        {
            ModelState.AddModelError(string.Empty, "Invalid selection.");
            return View(await BuildViewModelAsync(model.ReturnUrl, model, ct));
        }

        var scopes = model.ScopesConsented ?? [];
        foreach (var identity in request.ValidatedResources.Resources.IdentityResources.Where(x => x.Required))
        {
            scopes.Add(identity.Name);
        }

        scopes = scopes.Distinct().ToList();
        if (scopes.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "You must pick at least one permission.");
            return View(await BuildViewModelAsync(model.ReturnUrl, model, ct));
        }

        var grantedConsent = new ConsentResponse
        {
            RememberConsent = model.RememberConsent,
            ScopesValuesConsented = scopes
        };

        var requestedScopes = request.ValidatedResources.RawScopeValues;
        await _events.RaiseAsync(new ConsentGrantedEvent(
            User.GetSubjectId(),
            request.Client.ClientId,
            requestedScopes,
            grantedConsent.ScopesValuesConsented,
            grantedConsent.RememberConsent), ct);

        await _interaction.GrantConsentAsync(request, grantedConsent, ct);
        return Redirect(model.ReturnUrl!);
    }

    private Task<ConsentViewModel?> BuildViewModelAsync(string? returnUrl, CancellationToken ct) =>
        BuildViewModelAsync(returnUrl, null, ct);

    private async Task<ConsentViewModel?> BuildViewModelAsync(
        string? returnUrl,
        ConsentInputModel? model,
        CancellationToken ct)
    {
        var request = await _interaction.GetAuthorizationContextAsync(returnUrl, ct);
        if (request is null)
        {
            return null;
        }

        var consentedScopes = model?.ScopesConsented?.ToArray();
        var identityScopes = request.ValidatedResources.Resources.IdentityResources.Select(resource =>
            CreateScopeViewModel(
                resource.Name,
                resource.DisplayName,
                resource.Description,
                resource.Emphasize,
                resource.Required,
                (consentedScopes?.Contains(resource.Name) ?? true) || resource.Required));

        var apiScopes = request.ValidatedResources.ParsedScopes
            .Where(parsedScope => request.ValidatedResources.Resources.ApiScopes
                .Any(x => x.Name == parsedScope.ParsedName))
            .Select(parsedScope =>
            {
                var apiScope = request.ValidatedResources.Resources.ApiScopes
                    .First(x => x.Name == parsedScope.ParsedName);
                return CreateScopeViewModel(
                    parsedScope.RawValue,
                    apiScope.DisplayName,
                    apiScope.Description,
                    apiScope.Emphasize,
                    false,
                    consentedScopes?.Contains(parsedScope.RawValue) ?? true);
            });

        return new ConsentViewModel
        {
            ReturnUrl = returnUrl,
            ClientName = request.Client.ClientName ?? request.Client.ClientId,
            ClientUrl = request.Client.ClientUri,
            ClientLogoUrl = request.Client.LogoUri,
            AllowRememberConsent = request.Client.AllowRememberConsent,
            RememberConsent = model?.RememberConsent ?? false,
            IdentityScopes = identityScopes,
            ApiScopes = apiScopes
        };
    }

    private static ScopeViewModel CreateScopeViewModel(
        string value,
        string? displayName,
        string? description,
        bool emphasize,
        bool required,
        bool checkedScope) =>
        new()
        {
            Value = value,
            DisplayName = displayName ?? value,
            Description = description,
            Emphasize = emphasize,
            Required = required,
            Checked = checkedScope
        };
}
