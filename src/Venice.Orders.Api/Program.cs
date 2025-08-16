using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Venice.Orders.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using HealthChecks.UI.Client;
using MongoDB.Driver;
using Microsoft.Extensions.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Config base (appsettings + env)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();

// ===== Infra: SQL + Mongo =====
builder.Services.AddSqlServerPersistence(builder.Configuration);
builder.Services.AddMongoPersistence(builder.Configuration);

// ===== Redis Cache =====
var redisCs = builder.Configuration.GetConnectionString("Redis")
             ?? Environment.GetEnvironmentVariable("ConnectionStrings__Redis")
             ?? "localhost:6379";
builder.Services.AddStackExchangeRedisCache(opt => opt.Configuration = redisCs);

// ===== Controllers + Swagger =====
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger com suporte a JWT Bearer
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Venice.Orders API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Informe o token JWT como: Bearer {seu_token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Type = ReferenceType.SecurityScheme,
            Id = JwtBearerDefaults.AuthenticationScheme
        }
    };
    c.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

// ===== JWT Auth =====
var issuer     = builder.Configuration["Auth:Issuer"]     ?? "Venice";
var audience   = builder.Configuration["Auth:Audience"]   ?? "VeniceClients";
var signingKey = builder.Configuration["Auth:SigningKey"] ?? "dev-signing-key-please-change";

var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = key,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

// ===== Health Checks =====
var sqlCs   = builder.Configuration.GetConnectionString("SqlServer")
              ?? Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer");
var mongoCs = builder.Configuration.GetConnectionString("Mongo")
             ?? Environment.GetEnvironmentVariable("ConnectionStrings__Mongo")
             ?? "mongodb://mongodb:27017";
var mongoDbName = builder.Configuration["Mongo:Database"] ?? "venice_orders_db";
var kafkaBs = builder.Configuration["Kafka:BootstrapServers"]
              ?? Environment.GetEnvironmentVariable("Kafka__BootstrapServers");

builder.Services.AddHealthChecks()
    .AddSqlServer(sqlCs!, name: "sqlserver", tags: new[] { "ready" })
    .AddMongoDb(
        sp =>
        {
            var client = new MongoClient(mongoCs);
            return client.GetDatabase(mongoDbName);
        },
        name: "mongodb",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "ready" },
        timeout: TimeSpan.FromSeconds(3)
    )
    .AddRedis(redisCs!, name: "redis", tags: new[] { "ready" })
    .AddKafka(setup => setup.BootstrapServers = kafkaBs!, name: "kafka", tags: new[] { "ready" });


var app = builder.Build();

// ===== Migrações EF + índices Mongo no startup =====
await app.Services.ApplyEfMigrationsAsync();
await app.Services.EnsureMongoIndexesAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
