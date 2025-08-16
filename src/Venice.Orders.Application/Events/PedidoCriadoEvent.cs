namespace Venice.Orders.Application.Events;

public class PedidoCriadoEvent
{
    public Guid PedidoId { get; set; }
    public int ClienteId { get; set; }
    public DateTime Data { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = default!;
    public List<PedidoItemEvent> Itens { get; set; } = new();
    public DateTime EventoPublicadoEm { get; set; } = DateTime.UtcNow;
}

public class PedidoItemEvent
{
    public string ProdutoId { get; set; } = default!;
    public string NomeProduto { get; set; } = default!;
    public int Quantidade { get; set; }
    public decimal PrecoUnitario { get; set; }
}
