using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using MongoDB.Driver;
using Venice.Orders.Application.Contracts;
using Venice.Orders.Application.Services;
using Venice.Orders.Domain.Entities;
using Venice.Orders.Domain.Enums;
using Venice.Orders.Domain.Events;
using Venice.Orders.Infrastructure.Mongo;
using Venice.Orders.Infrastructure.Persistence;

namespace Venice.Orders.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // << protege todos os endpoints
public class OrdersController : ControllerBase
{
    private readonly OrdersDbContext _sql;
    private readonly IMongoContext _mongo;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly IPedidoEventService _pedidoEventService;
    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    public OrdersController(
        OrdersDbContext sql, 
        IMongoContext mongo, 
        IDistributedCache cache,
        IConfiguration configuration,
        IPedidoEventService pedidoEventService)
    {
        _sql = sql;
        _mongo = mongo;
        _cache = cache;
        _configuration = configuration;
        _pedidoEventService = pedidoEventService;
    }

    /// <summary>Cria um pedido no SQL e seus itens no Mongo.</summary>
    [HttpPost]
    public async Task<ActionResult<PedidoResponse>> CreateAsync([FromBody] CreatePedidoRequest request, CancellationToken ct)
    {
        if (request.Itens is null || request.Itens.Count == 0)
            return BadRequest("Informe ao menos 1 item.");

        var pedido = new Pedido(request.ClienteId);
        await _sql.Pedidos.AddAsync(pedido, ct);
        await _sql.SaveChangesAsync(ct);

        var docs = request.Itens.Select(i => new PedidoItemDocument
        {
            PedidoId = pedido.Id,
            ProdutoId = i.ProdutoId,
            NomeProduto = i.NomeProduto,
            Quantidade = i.Quantidade,
            PrecoUnitario = i.PrecoUnitario
        }).ToList();

        if (docs.Count > 0)
            await _mongo.PedidoItens.InsertManyAsync(docs, cancellationToken: ct);

        var total = docs.Sum(d => d.PrecoUnitario * d.Quantidade);
        pedido.DefinirTotal(total);
        pedido.AlterarStatus(PedidoStatus.Criado);
        _sql.Pedidos.Update(pedido);
        await _sql.SaveChangesAsync(ct);

        var response = new PedidoResponse
        {
            Id = pedido.Id,
            ClienteId = pedido.ClienteId,
            Data = pedido.Data,
            Status = pedido.Status.ToString(),
            Total = pedido.Total,
            Itens = docs.Select(d => new PedidoItemResponse
            {
                ProdutoId = d.ProdutoId,
                NomeProduto = d.NomeProduto,
                Quantidade = d.Quantidade,
                PrecoUnitario = d.PrecoUnitario
            }).ToList()
        };

        // Opcional: já grava no cache para o primeiro GET ser hit
        var cacheKey = $"pedido:{pedido.Id}";
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response, _json), options, ct);

        // Publicar evento PedidoCriado no Kafka
        try
        {
            var evento = new PedidoCriadoEvent
            {
                PedidoId = pedido.Id,
                ClienteId = pedido.ClienteId,
                Data = pedido.Data,
                Status = pedido.Status.ToString(),
                Total = pedido.Total,
                Itens = docs.Select(d => new PedidoItemEvent
                {
                    ProdutoId = d.ProdutoId,
                    NomeProduto = d.NomeProduto,
                    Quantidade = d.Quantidade,
                    PrecoUnitario = d.PrecoUnitario
                }).ToList()
            };

            var topico = _configuration["Kafka:TopicPedidos"] ?? "pedidos";
            await _pedidoEventService.PublicarPedidoCriadoAsync(evento, ct);
        }
        catch (Exception ex)
        {
            // Log do erro, mas não falha a operação
            // Em produção, considere usar um padrão de outbox ou retry
            Console.WriteLine($"Erro ao publicar evento: {ex.Message}");
        }

        return Created($"/api/orders/{pedido.Id}", response);
    }

    /// <summary>Retorna o pedido e seus itens agregados (com cache de 120s).</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PedidoResponse>> GetByIdAsync([FromRoute] Guid id, CancellationToken ct)
    {
        var cacheKey = $"pedido:{id}";

        // 1) Tenta cache
        var cached = await _cache.GetStringAsync(cacheKey, ct);
        if (!string.IsNullOrEmpty(cached))
        {
            var hit = JsonSerializer.Deserialize<PedidoResponse>(cached, _json);
            if (hit is not null) return Ok(hit);
        }

        // 2) Miss: monta do SQL + Mongo
        var pedido = await _sql.Pedidos.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (pedido is null) return NotFound();

        var itens = await _mongo.PedidoItens.Find(x => x.PedidoId == id).ToListAsync(ct);

        var response = new PedidoResponse
        {
            Id = pedido.Id,
            ClienteId = pedido.ClienteId,
            Data = pedido.Data,
            Status = pedido.Status.ToString(),
            Total = pedido.Total,
            Itens = itens.Select(d => new PedidoItemResponse
            {
                ProdutoId = d.ProdutoId,
                NomeProduto = d.NomeProduto,
                Quantidade = d.Quantidade,
                PrecoUnitario = d.PrecoUnitario
            }).ToList()
        };

        // 3) Salva no cache por 2 minutos
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
        };
        await _cache.SetStringAsync(cacheKey, JsonSerializer.Serialize(response, _json), options, ct);

        return Ok(response);
    }
}
