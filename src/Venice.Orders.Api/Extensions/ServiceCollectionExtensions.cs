using Microsoft.Extensions.DependencyInjection;
using Venice.Orders.Application.Services;
using Venice.Orders.Infrastructure.Kafka;

namespace Venice.Orders.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddKafkaMessaging(this IServiceCollection services)
    {
        // Registra o KafkaProducer
        services.AddSingleton<IKafkaProducer, KafkaProducer>();
        
        // Registra o serviço de eventos usando Kafka
        services.AddScoped<IPedidoEventService, KafkaPedidoEventService>();
        
        return services;
    }
}
