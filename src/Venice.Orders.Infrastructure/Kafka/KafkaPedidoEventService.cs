using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Venice.Orders.Application.Services;
using Venice.Orders.Domain.Events;

namespace Venice.Orders.Infrastructure.Kafka;

public class KafkaPedidoEventService : IPedidoEventService
{
    private readonly IKafkaProducer _kafkaProducer;
    private readonly IConfiguration _configuration;
    private readonly ILogger<KafkaPedidoEventService> _logger;

    public KafkaPedidoEventService(
        IKafkaProducer kafkaProducer, 
        IConfiguration configuration,
        ILogger<KafkaPedidoEventService> logger)
    {
        _kafkaProducer = kafkaProducer;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublicarPedidoCriadoAsync(PedidoCriadoEvent evento, CancellationToken cancellationToken = default)
    {
        try
        {
            var topico = _configuration["Kafka:TopicPedidos"] ?? "pedidos";
            
            _logger.LogInformation("Publicando evento PedidoCriado para pedido {PedidoId}", evento.PedidoId);
            
            await _kafkaProducer.PublishAsync(topico, evento, cancellationToken);
            
            _logger.LogInformation("Evento PedidoCriado publicado com sucesso para pedido {PedidoId}", evento.PedidoId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao publicar evento PedidoCriado para pedido {PedidoId}", evento.PedidoId);
            throw;
        }
    }
}
