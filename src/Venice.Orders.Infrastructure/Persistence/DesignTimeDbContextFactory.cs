using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Venice.Orders.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var conn = Environment.GetEnvironmentVariable("ConnectionStrings__SqlServer")
                   ?? "Server=localhost,1433;Database=VeniceOrdersDB;User Id=sa;Password=SQLServer123$;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<OrdersDbContext>();
        optionsBuilder.UseSqlServer(conn);

        return new OrdersDbContext(optionsBuilder.Options);
    }
}
