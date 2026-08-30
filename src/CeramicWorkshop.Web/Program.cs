using System.Globalization;
using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Web.Components;
using CeramicWorkshop.Web.Models;
using CeramicWorkshop.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Langue et formats de l'interface : français, montants et dates au format algérien.
var cultureAtelier = MontantFormatter.CultureAtelier;
CultureInfo.DefaultThreadCurrentCulture = cultureAtelier;
CultureInfo.DefaultThreadCurrentUICulture = cultureAtelier;

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Adresse du serveur applicatif (API).
var adresseApi = builder.Configuration["Api:AdresseDeBase"]
    ?? throw new InvalidOperationException(
        "L'adresse du serveur applicatif est absente. Renseignez « Api:AdresseDeBase » dans appsettings.json.");

builder.Services.AddHttpClient("api", client =>
{
    client.BaseAddress = new Uri(adresseApi.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(60);
});

builder.Services.AddScoped<SessionUtilisateur>();
builder.Services.AddScoped<StockageSession>();
builder.Services.AddScoped<ServiceMessages>();

builder.Services.AddScoped(fournisseur => new ClientApi(
    fournisseur.GetRequiredService<IHttpClientFactory>().CreateClient("api"),
    fournisseur.GetRequiredService<SessionUtilisateur>(),
    fournisseur.GetRequiredService<ILogger<ClientApi>>()));

builder.Services.AddScoped<ServiceAuthentification>();
builder.Services.AddScoped<FournisseurEtatAuthentification>();
builder.Services.AddScoped<AuthenticationStateProvider>(fournisseur =>
    fournisseur.GetRequiredService<FournisseurEtatAuthentification>());

builder.Services.AddCascadingAuthenticationState();

// Chaque droit du catalogue devient une règle d'accès : les pages et les boutons
// ne s'affichent que pour les utilisateurs autorisés.
builder.Services.AddAuthorizationCore(options =>
{
    foreach (var droit in PermissionCodes.Catalogue)
    {
        options.AddPolicy(droit.Code, regle => regle
            .RequireAuthenticatedUser()
            .RequireClaim(FournisseurEtatAuthentification.ClaimDroit, droit.Code));
    }
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/erreur", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// La sécurité réelle est assurée par l'API (jeton et droits vérifiés à chaque appel).
// Côté interface, l'accès aux pages est contrôlé par le routeur Blazor : les points
// d'entrée HTTP restent donc anonymes, sinon le serveur tenterait une authentification
// par cookie qui n'existe pas dans cette architecture.
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AllowAnonymous();

await app.RunAsync();
