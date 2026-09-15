using System.Text;
using Assets.Api.BackgroundServices;
using Assets.Api.Services;
using Assets.Domain.MeterData;
using Assets.Domain.Settlement;
using Assets.Infrastructure.MeterData;
using Assets.Infrastructure.Persistence;
using Assets.Infrastructure.Security;
using Assets.Infrastructure.Settlement;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---- Configuration -------------------------------------------------------
builder.Services.Configure<MongoSettings>(builder.Configuration.GetSection(MongoSettings.SectionName));
builder.Services.Configure<AuthenticationSettings>(builder.Configuration.GetSection(AuthenticationSettings.SectionName));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));
builder.Services.Configure<MeterDataSettings>(builder.Configuration.GetSection(MeterDataSettings.SectionName));
builder.Services.Configure<SettlementSettings>(builder.Configuration.GetSection(SettlementSettings.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt configuration section is missing.");

// ---- DI --------------------------------------------------------------
builder.Services.AddSingleton<MongoContext>();
builder.Services.AddScoped<IAssetRepository, MongoAssetRepository>();
builder.Services.AddScoped<IUserRepository, ConfigUserRepository>();
builder.Services.AddScoped<IMeterReadingRepository, MongoMeterReadingRepository>();
builder.Services.AddScoped<IImportJobRepository, MongoImportJobRepository>();
builder.Services.AddScoped<IMeterDataParser, CsvMeterDataParser>();
builder.Services.AddScoped<IMeterDataParser, ExcelMeterDataParser>();
builder.Services.AddScoped<MeterDataImportService>();
builder.Services.AddSingleton<IImportJobQueue, InMemoryImportJobQueue>();
builder.Services.AddHostedService<ImportJobWorker>();
builder.Services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>();

// FakeSpotPriceProvider for now - the real Spot Price API isn't available
// yet (per the brief). Swap for HttpSpotPriceProvider (already written,
// see Assets.Infrastructure/Settlement) via this one line once it is.
builder.Services.AddScoped<ISpotPriceProvider, FakeSpotPriceProvider>();
builder.Services.AddScoped<SettlementCalculator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Renewable Asset System API",
        Version = "v1",
        Description = "Task 1 (assets), Task 2 (meter data import), Task 3 (settlement).",
    });

    // Lets you click "Authorize" in the Swagger UI, paste a bearer token
    // (from POST /api/auth/login), and have it attached to every
    // subsequent request - needed to exercise any [Authorize] endpoint
    // directly from the docs page.
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Paste the token returned from POST /api/auth/login (without the word 'Bearer').",
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference { Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme, Id = "Bearer" },
            },
            Array.Empty<string>()
        },
    });

    // Swashbuckle's DateOnly support varies by version - mapped explicitly
    // so /api/settlements' start/end query params render correctly rather
    // than risking a generation error.
    options.MapType<DateOnly>(() => new Microsoft.OpenApi.Models.OpenApiSchema { Type = "string", Format = "date" });
});

// ---- CORS: allow only the configured UI origin, never "*" -----------------
var uiOrigin = builder.Configuration["Cors:UiOrigin"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddPolicy("UiOnly", policy =>
        policy.WithOrigins(uiOrigin)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

// ---- Auth: JWT bearer, role claims drive [Authorize(Roles=...)] -----------
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SigningKey)),
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// Swagger is on by default here (Swagger:Enabled in appsettings.json) so
// the API is directly testable without a separate frontend - set
// Swagger:Enabled to false (or just rely on the Development check) in a
// real production deployment to avoid exposing the API shape publicly.
var swaggerEnabled = app.Environment.IsDevelopment() || builder.Configuration.GetValue("Swagger:Enabled", true);
if (swaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Renewable Asset System API v1");
    });
}

var hasHttpsAddress = app.Urls.Any(url => url.StartsWith("https://", StringComparison.OrdinalIgnoreCase));
if (hasHttpsAddress)
{
    app.UseHttpsRedirection();
}

// ---- Security headers (XSS / clickjacking / MIME sniffing hardening) -----
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append(
        "Content-Security-Policy",
        "default-src 'self'; script-src 'self'; object-src 'none'; frame-ancestors 'none';");
    await next();
});

app.UseCors("UiOnly");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory-based integration tests.
public partial class Program { }
