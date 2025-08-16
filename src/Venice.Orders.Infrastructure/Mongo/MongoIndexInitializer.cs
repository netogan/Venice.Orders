using MongoDB.Driver;

namespace Venice.Orders.Infrastructure.Mongo;

public static class MongoIndexInitializer
{
    public static async Task EnsureIndexesAsync(IMongoContext ctx, CancellationToken ct = default)
    {
        // Índice por pedidoId para leitura rápida dos itens
        var keys = Builders<PedidoItemDocument>.IndexKeys.Ascending(x => x.PedidoId);
        var model = new CreateIndexModel<PedidoItemDocument>(keys, new CreateIndexOptions
        {
            Name = "idx_pedidoId"
        });

        await ctx.PedidoItens.Indexes.CreateOneAsync(model, cancellationToken: ct);
    }
}
