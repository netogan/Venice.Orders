using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Venice.Orders.Infrastructure.Mongo;

public interface IMongoContext
{
    IMongoDatabase Database { get; }
    IMongoCollection<PedidoItemDocument> PedidoItens { get; }
}

public class MongoContext : IMongoContext
{
    public IMongoDatabase Database { get; }
    public IMongoCollection<PedidoItemDocument> PedidoItens { get; }

    public MongoContext(IOptions<MongoSettings> options)
    {
        var settings = options.Value;
        var client = new MongoClient(settings.ConnectionString);
        Database = client.GetDatabase(settings.Database);
        PedidoItens = Database.GetCollection<PedidoItemDocument>(settings.PedidoItensCollection);
    }
}
