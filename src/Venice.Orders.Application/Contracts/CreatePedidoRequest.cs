namespace Venice.Orders.Application.Contracts;

public class CreatePedidoRequest
{
    public int ClienteId { get; set; }
    public List<CreatePedidoItemRequest> Itens { get; set; } = new();
}

public class CreatePedidoItemRequest
{
    public string ProdutoId { get; set; } = default!;
    public string NomeProduto { get; set; } = default!;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}
