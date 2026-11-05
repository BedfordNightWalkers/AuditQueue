using System.Security.Claims;
using Audit.Application;
using Audit.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// 1. Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// 2. Application Services
builder.Services.AddApplication();

// 3. Infrastructure services
builder.Services.AddInfrastructure(builder.Configuration);

// Register a real JWT bearer scheme or a development-only test scheme here.
// UseAuthentication requires a registered authentication scheme.

// 5. Authentication : validate access tokens issued by Auth0
var authDomain = builder.Configuration["Auth0:Domain"]
    ?? throw new InvalidOperationException(
        "Auth0:Domain is missing from configuration.");

var authAudience = builder.Configuration["Auth0:Audience"]
    ?? throw new InvalidOperationException(
        "Auth0:Audience is missing from configuration.");

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    }).AddJwtBearer(options =>
    {
        options.Authority = $"https://{authDomain}";
        options.Audience = authAudience;
    });

// 6. Authorisation : require the relevant OAuth scope
builder.Services
    .AddAuthorization(options =>
    {
        options.AddPolicy("QueueRead", policy =>
            policy.RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    HasScope(context.User, "queue:read")));

        options.AddPolicy("QueueWrite", policy =>
            policy.RequireAuthenticatedUser()
            .RequireAssertion(context =>
                    HasScope(context.User, "queue:write")));

        options.AddPolicy("QueueProcess", policy =>
            policy.RequireAuthenticatedUser()
                .RequireAssertion(context =>
                    HasScope(context.User, "queue:process")));
    });


var app = builder.Build();

// 7. Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Auth0 puts granted scopes in a single space-separated "scope" claim,
// and RBAC permissions (if enabled) in one "permissions" claim per value.
static bool HasScope(ClaimsPrincipal user, string scope) =>
    user.FindAll("scope")
        .SelectMany(c => c.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        .Concat(user.FindAll("permissions").Select(c => c.Value))
        .Contains(scope, StringComparer.Ordinal);
