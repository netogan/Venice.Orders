using Venice.Orders.Domain.Enums;

namespace Venice.Orders.Domain.Entities;

public class Pedido
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public int ClienteId { get; private set; }
    public DateTime Data { get; private set; }
    public PedidoStatus Status { get; private set; }
    public decimal Total { get; private set; }

    // Construtor privado para Entity Framework
    private Pedido() { }

    // Construtor de domínio
    public Pedido(int clienteId)
    {
        ClienteId = clienteId;
        Data = DateTime.UtcNow;
        Status = PedidoStatus.Criado;
        Total = 0m;
    }

    public void DefinirTotal(decimal total) => Total = total;
    public void AlterarStatus(PedidoStatus status) => Status = status;
}
