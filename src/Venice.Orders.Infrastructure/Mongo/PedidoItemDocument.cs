using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Venice.Orders.Infrastructure.Mongo;

public class PedidoItemDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("PedidoId")]
    [BsonRepresentation(BsonType.String)]
    public Guid PedidoId { get; set; }

    [BsonElement("ProdutoId")]
    public string ProdutoId { get; set; } = default!;

    [BsonElement("NomeProduto")]
    public string NomeProduto { get; set; } = default!;

    [BsonElement("Quantidade")]
    public int Quantidade { get; set; }

    [BsonElement("PrecoUnitario")]
    public decimal PrecoUnitario { get; set; }
}
