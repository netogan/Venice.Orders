using Venice.Orders.Domain.Events;

namespace Venice.Orders.Domain.Services;

public interface IPedidoEventService
{
    Task PublicarPedidoCriadoAsync(PedidoCriadoEvent evento, CancellationToken cancellationToken = default);
}
