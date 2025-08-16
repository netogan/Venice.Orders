using Venice.Orders.Domain.Events;

namespace Venice.Orders.Application.Services;

public interface IPedidoEventService
{
    Task PublicarPedidoCriadoAsync(PedidoCriadoEvent evento, CancellationToken cancellationToken = default);
}
