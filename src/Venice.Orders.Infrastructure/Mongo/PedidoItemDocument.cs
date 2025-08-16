using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Venice.Orders.Infrastructure.Mongo;

public class PedidoItemDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("pedidoId")]
    public Guid PedidoId { get; set; }

    [BsonElement("produtoId")]
    public string ProdutoId { get; set; } = default!;

    [BsonElement("nomeProduto")]
    public string NomeProduto { get; set; } = default!;

    [BsonElement("quantidade")]
    public int Quantidade { get; set; }

    [BsonElement("precoUnitario")]
    public decimal PrecoUnitario { get; set; }
}
