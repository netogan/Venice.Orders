using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
        await db.Database.MigrateAsync();
    }

    public static async Task EnsureMongoIndexesAsync(this IServiceProvider sp, CancellationToken ct = default)
    {
        using var scope = sp.CreateScope();
        var mongo = scope.ServiceProvider.GetRequiredService<IMongoContext>();
        await MongoIndexInitializer.EnsureIndexesAsync(mongo, ct);
    }
}
