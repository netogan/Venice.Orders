namespace Venice.Orders.Infrastructure.Mongo;

public class MongoSettings
{
    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string Database { get; set; } = "venice_orders_db";
    public string PedidoItensCollection { get; set; } = "PedidoItens";
}
