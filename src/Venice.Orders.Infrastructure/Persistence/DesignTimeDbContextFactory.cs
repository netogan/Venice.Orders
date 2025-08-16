using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Venice.Orders.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        // Tenta pegar de env var ou usa fallback
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer")
                   ?? "Server=localhost,1433;User Id=sa;Password=SQLServer123$;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<OrdersDbContext>();
        optionsBuilder.UseSqlServer(conn);

        return new OrdersDbContext(optionsBuilder.Options);
    }
}
