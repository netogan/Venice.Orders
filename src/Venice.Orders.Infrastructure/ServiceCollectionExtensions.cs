using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Venice.Orders.Infrastructure.Mongo;
using Venice.Orders.Infrastructure.Persistence;

namespace Venice.Orders.Infrastructure;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSqlServerPersistence(this IServiceCollection services, IConfiguration config)
    {
        var cs = config.GetConnectionString("SqlServer")
                 ?? Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer")
                 ?? "Server=localhost,1433;Database=VeniceOrdersDB;User Id=sa;Password=SQLServer123$;TrustServerCertificate=True;";

        services.AddDbContext<OrdersDbContext>(opt =>
        {
            opt.UseSqlServer(cs, sql =>
            {
                sql.MigrationsAssembly(typeof(OrdersDbContext).Assembly.FullName);
                sql.EnableRetryOnFailure();
            });
        });

        return services;
    }

    public static IServiceCollection AddMongoPersistence(this IServiceCollection services, IConfiguration config)
    {
        var settings = new MongoSettings
        {
            ConnectionString = config.GetConnectionString("Mongo") 
                               ?? Environment.GetEnvironmentVariable("ConnectionStrings__Mongo") 
                               ?? "mongodb://localhost:27017",
            Database = config["Mongo:Database"] ?? "venice_orders_db",
            PedidoItensCollection = config["Mongo:PedidoItensCollection"] ?? "PedidoItens"
        };

        services.Configure<MongoSettings>(o =>
        {
            o.ConnectionString = settings.ConnectionString;
            o.Database = settings.Database;
            o.PedidoItensCollection = settings.PedidoItensCollection;
        });

        services.AddSingleton<IMongoContext, MongoContext>();
        return services;
    }

    public static async Task ApplyEfMigrationsAsync(this IServiceProvider sp)
    {
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<OrdersDbContext>>();
        
        try
        {
            logger.LogInformation("Iniciando aplicação de migrações EF...");
            
            // Log da connection string (mascarando senha)
            var connectionString = db.Database.GetConnectionString();
            var maskedCs = connectionString?.Replace("SQLserver123$", "***");
            logger.LogInformation("Connection String: {ConnectionString}", maskedCs);
            
            // Verificar se pode conectar ao banco
            logger.LogInformation("Testando conectividade com o banco...");
            var canConnect = await db.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogWarning("Não foi possível conectar ao banco de dados. Pulando migrações.");
                return;
            }
            
            logger.LogInformation("Conexão com banco estabelecida. Aplicando migrações...");
            
            // Aplicar migrações com timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(60));
            await db.Database.MigrateAsync(cts.Token);
            
            logger.LogInformation("Migrações EF aplicadas com sucesso!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao aplicar migrações EF: {Message}", ex.Message);
            throw;
        }
    }

    public static async Task EnsureMongoIndexesAsync(this IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var mongo = scope.ServiceProvider.GetRequiredService<IMongoContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<IMongoContext>>();
        
        try
        {
            logger.LogInformation("Iniciando criação de índices MongoDB...");
            await MongoIndexInitializer.EnsureIndexesAsync(mongo, ct);
            logger.LogInformation("Índices MongoDB criados com sucesso!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erro ao criar índices MongoDB: {Message}", ex.Message);
            throw;
        }
    }
}
