using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Venice.Orders.Infrastructure.Kafka;

public interface IKafkaProducer
{
    Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default);
}

public class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public KafkaProducer(IConfiguration configuration, ILogger<KafkaProducer> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        var config = new ProducerConfig
        {
            BootstrapServers = configuration["Kafka:BootstrapServers"],
            Acks = Acks.All,
            MessageSendMaxRetries = 3,
            EnableIdempotence = true,
            CompressionType = CompressionType.Snappy,
            MessageTimeoutMs = 5000,
            RequestTimeoutMs = 5000,
            SocketTimeoutMs = 5000,
            BatchSize = 16384,
            LingerMs = 10
        };

        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, e) => _logger.LogError("Kafka Producer Error: {Error}", e))
            .SetLogHandler((_, log) => 
            {
                if (log.Level <= SyslogLevel.Warning)
                    _logger.LogWarning("Kafka Log [{Level}]: {Message}", log.Level, log.Message);
            })
            .Build();
    }

    public async Task PublishAsync<T>(string topic, T message, CancellationToken cancellationToken = default)
    {
        try
        {
            var messageKey = Guid.NewGuid().ToString();
            var messageValue = JsonSerializer.Serialize(message, _jsonOptions);

            var kafkaMessage = new Message<string, string>
            {
                Key = messageKey,
                Value = messageValue,
                Timestamp = new Timestamp(DateTime.UtcNow)
            };

            _logger.LogInformation("Publicando mensagem no tópico {Topic} com chave {Key}", topic, messageKey);

            var result = await _producer.ProduceAsync(topic, kafkaMessage, cancellationToken);

            _logger.LogInformation(
                "Mensagem publicada com sucesso no tópico {Topic}, Partition: {Partition}, Offset: {Offset}",
                result.Topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Erro ao publicar mensagem no Kafka. Tópico: {Topic}, Error Code: {ErrorCode}, Reason: {Reason}",
                topic, ex.Error.Code, ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro inesperado ao publicar mensagem no Kafka. Tópico: {Topic}", topic);
            throw;
        }
    }

    public void Dispose()
    {
        try
        {
            _producer?.Flush(TimeSpan.FromSeconds(10));
            _producer?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao fazer dispose do Kafka Producer");
        }
    }
}
