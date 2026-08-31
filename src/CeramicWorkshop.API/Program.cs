using System.Globalization;
using System.Text;
using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.API.Filters;
using CeramicWorkshop.API.Middleware;
using CeramicWorkshop.Application;
using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Infrastructure;
using CeramicWorkshop.Infrastructure.Authentication;
using CeramicWorkshop.Infrastructure.Data;
using CeramicWorkshop.Infrastructure.Data.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Langue et formats : français, format algérien pour les montants et les dates.
// ---------------------------------------------------------------------------
var cultureAtelier = MontantFormatter.CultureAtelier;
CultureInfo.DefaultThreadCurrentCulture = cultureAtelier;
CultureInfo.DefaultThreadCurrentUICulture = cultureAtelier;

// ---------------------------------------------------------------------------
// Services applicatifs et accès aux données.
// ---------------------------------------------------------------------------
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());

// Les erreurs de format (JSON invalide, type incorrect) sont renvoyées en français.
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = contexte =>
    {
        var erreur = new ErreurApi
        {
            Message = "Les informations envoyées sont incomplètes ou incorrectes.",
            Erreurs = contexte.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => string.IsNullOrWhiteSpace(e.Key) ? "Formulaire" : e.Key,
                    e => e.Value!.Errors
                        .Select(m => string.IsNullOrWhiteSpace(m.ErrorMessage)
                            ? "Valeur invalide."
                            : m.ErrorMessage)
                        .ToArray())
        };

        return new BadRequestObjectResult(erreur);
    };
});

// ---------------------------------------------------------------------------
// Authentification par jeton JWT.
// ---------------------------------------------------------------------------
var optionsJwt = builder.Configuration.GetSection(JwtOptions.Section).Get<JwtOptions>() ?? new JwtOptions();

if (string.IsNullOrWhiteSpace(optionsJwt.Cle) || optionsJwt.Cle.Length < JwtOptions.LongueurCleMinimale)
{
    throw new InvalidOperationException(
        "La clé de signature des jetons est absente ou trop courte. Renseignez « Jwt:Cle » " +
        $"(au moins {JwtOptions.LongueurCleMinimale} caractères) dans la configuration ou " +
        "dans la variable d'environnement Jwt__Cle.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = optionsJwt.Emetteur,
            ValidAudience = optionsJwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(optionsJwt.Cle)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAutorisationParDroits();

// ---------------------------------------------------------------------------
// Accès depuis l'interface web.
// ---------------------------------------------------------------------------
const string PolitiqueCors = "InterfaceAtelier";
var originesAutorisees = builder.Configuration.GetSection("Cors:OriginesAutorisees").Get<string[]>()
                         ?? new[] { "https://localhost:7225", "http://localhost:5192" };

builder.Services.AddCors(options => options.AddPolicy(PolitiqueCors, politique => politique
    .WithOrigins(originesAutorisees)
    .AllowAnyHeader()
    .AllowAnyMethod()));

// ---------------------------------------------------------------------------
// Documentation de l'API.
// ---------------------------------------------------------------------------
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "CERAMIPRO — API de gestion d'atelier",
        Version = "v1",
        Description = "Interface de programmation du logiciel de gestion d'atelier de céramique et de décoration."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Saisissez le jeton d'accès obtenu par « /api/authentification/connexion »."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    var fichierXml = Path.Combine(AppContext.BaseDirectory,
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(fichierXml))
    {
        options.IncludeXmlComments(fichierXml);
    }
});

var app = builder.Build();

// ---------------------------------------------------------------------------
// Préparation de la base de données au démarrage.
// ---------------------------------------------------------------------------
if (builder.Configuration.GetValue("BaseDeDonnees:MigrerAuDemarrage", true))
{
    await using var portee = app.Services.CreateAsyncScope();
    var journal = portee.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Démarrage");

    try
    {
        var contexte = portee.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await contexte.Database.MigrateAsync();

        var semeur = portee.ServiceProvider.GetRequiredService<DatabaseSeeder>();
        await semeur.ExecuterAsync();

        journal.LogInformation("Base de données « CeramicWorkshopDB » prête.");
    }
    catch (Npgsql.NpgsqlException ex)
    {
        // Cas de loin le plus courant au premier démarrage : le serveur
        // PostgreSQL n'est pas lancé, ou les identifiants sont incorrects.
        // Un message clair évite d'avoir à déchiffrer une trace technique.
        journal.LogError(
            "Le serveur PostgreSQL est injoignable.\n" +
            "  · Vérifiez qu'il est démarré (sous Linux : sudo systemctl start postgresql).\n" +
            "  · Vérifiez le nom de la base, l'utilisateur et le mot de passe dans\n" +
            "    « ConnectionStrings:CeramicWorkshopDB ».\n" +
            "  Détail technique : {Detail}", ex.Message);

        return 1;
    }
    catch (Exception ex)
    {
        journal.LogError(ex, "Impossible de préparer la base de données au démarrage.");
        throw;
    }
}

app.UseMiddleware<EntetesSecuriteMiddleware>();
app.UseMiddleware<GestionExceptionsMiddleware>();

app.UseRequestLocalization(new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(cultureAtelier),
    SupportedCultures = new List<CultureInfo> { cultureAtelier },
    SupportedUICultures = new List<CultureInfo> { cultureAtelier }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "CERAMIPRO — API v1");
        options.DocumentTitle = "CERAMIPRO — Documentation de l'API";
    });
}
else
{
    app.UseHsts();
}

app.UseHttpsRedirection();

// Photos des produits et justificatifs déposés par l'atelier.
app.UseStaticFiles();

app.UseCors(PolitiqueCors);
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

await app.RunAsync();

return 0;

/// <summary>Rend la classe de démarrage visible pour les tests d'intégration.</summary>
public partial class Program;
