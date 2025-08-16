using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Venice.Orders.Domain.Entities;

namespace Venice.Orders.Infrastructure.Persistence.Configurations;

public class PedidoConfiguration : IEntityTypeConfiguration<Pedido>
{
    public void Configure(EntityTypeBuilder<Pedido> builder)
    {
        builder.ToTable("Pedidos");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.ClienteId)
            .IsRequired();

        builder.Property(p => p.Data)
            .IsRequired();

        builder.Property(p => p.Status)
            .IsRequired();

        builder.Property(p => p.Total)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.HasIndex(p => p.ClienteId);
        builder.HasIndex(p => p.Data);
        builder.HasIndex(p => new { p.ClienteId, p.Data });
    }
}
