using System.Text;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Zeno.Application.Interfaces;
using Zeno.Application.Services;
using Zeno.Infrastructure.SQL.Extentions;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// VALIDAÇÃO DE CONFIGURAÇÃO OBRIGATÓRIA
// ============================================
var requiredConfigs = new Dictionary<string, string>
{
    { "Jwt:Key", builder.Configuration["Jwt:Key"] ?? "" },
    { "Jwt:Issuer", builder.Configuration["Jwt:Issuer"] ?? "" },
    { "Database:ConnectionString", builder.Configuration["Database:ConnectionString"] ?? "" },
    { "Encryption:Key", builder.Configuration["Encryption:Key"] ?? "" }
};

var missingConfigs = requiredConfigs.Where(c => string.IsNullOrWhiteSpace(c.Value)).Select(c => c.Key).ToList();

if (missingConfigs.Any())
{
    var errorMessage = $"[CONFIG ERROR] Configurações obrigatórias faltando: {string.Join(", ", missingConfigs)}\n" +
                       "Por favor, configure as seguintes variáveis de ambiente:\n" +
                       "  - Jwt__Key (mínimo 32 caracteres)\n" +
                       "  - Jwt__Issuer\n" +
                       "  - Database__ConnectionString\n" +
                       "  - Encryption__Key";
    throw new InvalidOperationException(errorMessage);
}

// Validação específica para JWT Key (mínimo 32 caracteres)
var jwtKey = builder.Configuration["Jwt:Key"]!;
if (jwtKey.Length < 32)
{
    throw new InvalidOperationException($"[CONFIG ERROR] Jwt:Key deve ter pelo menos 32 caracteres. Tamanho atual: {jwtKey.Length}");
}

// Validação da string de conexão do banco de dados
var dbConnectionString = builder.Configuration["Database:ConnectionString"]!;
if (!string.IsNullOrEmpty(dbConnectionString) && dbConnectionString.Contains("Host=") && dbConnectionString.Contains("Port="))
{
    throw new InvalidOperationException(
        "[CONFIG ERROR] A string de conexão parece ser PostgreSQL/MySQL, mas o banco de dados atual é MongoDB.\n" +
        "Por favor, use uma string de conexão MongoDB (ex: mongodb://user:pass@host:port/database)");
}

var jwtIssuer = builder.Configuration["Jwt:Issuer"]!;
var jwtExpireHours = int.Parse(builder.Configuration["Jwt:ExpireHours"] ?? "2");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Zeno.API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header. Exemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddValidatorsFromAssemblyContaining<Zeno.Application.Validators.CreateEntryRequestValidator>();
var connStr = builder.Configuration["Database:ConnectionString"]!;
builder.Services.AddInfrastructureSQL(connStr, builder.Configuration["Encryption:Key"]!);
builder.Services.AddScoped<IEntryService, EntryService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<IBalanceService, BalanceService>();
builder.Services.AddScoped<ISummaryService, SummaryService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddSingleton<ITokenBlacklistService, TokenBlacklistService>();
builder.Services.AddMemoryCache();

builder.Services.AddHealthChecks();

var jwtSettings = builder.Configuration.GetSection("Jwt");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = "External";
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };

    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            Console.WriteLine($"[JWT AUTH FAILED] {context.Exception}");
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            var blacklist = context.HttpContext.RequestServices.GetRequiredService<ITokenBlacklistService>();
            var jti = context.Principal?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;
            if (jti is not null && blacklist.IsRevoked(jti))
            {
                context.Fail("Token revogado.");
            }
            return Task.CompletedTask;
        }
    };
})
.AddCookie("External");

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.AddAuthorization();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Zeno.API v1");
});

app.UseCors();

app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
});

app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Unhandled Exception] {ex.Message}");
        throw;
    }
});

app.UseExceptionHandler("/error");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");

app.MapControllers();

app.Run();