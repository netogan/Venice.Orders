namespace Venice.Orders.Application.Contracts;

public class PedidoResponse
{
    public Guid Id { get; set; }
    public int ClienteId { get; set; }
    public DateTime Data { get; set; }
    public string Status { get; set; } = default!;
    public decimal Total { get; set; }
    public List<PedidoItemResponse> Itens { get; set; } = new();
}

public class PedidoItemResponse
{
    public string ProdutoId { get; set; } = default!;
    public string NomeProduto { get; set; } = default!;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}
