using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using Venice.Orders.Api.Tests.Fixtures;
using Venice.Orders.Application.Contracts;
using Venice.Orders.Domain.Entities;
using Venice.Orders.Domain.Enums;
using Venice.Orders.Infrastructure.Mongo;
using Xunit;

namespace Venice.Orders.Api.Tests.Controllers;

public class OrdersControllerTests : IClassFixture<OrdersControllerFixture>
{
    private readonly OrdersControllerFixture _fixture;

    public OrdersControllerTests(OrdersControllerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ShouldReturnCreatedResult()
    {
        // Arrange
        await _fixture.ClearDatabase();
        _fixture.ResetMocks();
        _fixture.SetupCacheSet();
        _fixture.SetupEventService();
        _fixture.SetupMongoFind();
        
        var request = new CreatePedidoRequest
        {
            ClienteId = 123,
            Itens = new List<CreatePedidoItemRequest>
            {
                new CreatePedidoItemRequest
                {
                    ProdutoId = "PROD001",
                    NomeProduto = "Produto Teste",
                    Quantidade = 2,
                    PrecoUnitario = 10.50m
                }
            }
        };

        // Act
        var result = await _fixture.Controller.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result.Result.Should().BeOfType<CreatedResult>().Subject;
        
        var response = createdResult.Value.Should().BeOfType<PedidoResponse>().Subject;
        response.ClienteId.Should().Be(123);
        response.Status.Should().Be(PedidoStatus.Criado.ToString());
        response.Total.Should().Be(21.00m);
        response.Itens.Should().HaveCount(1);
        response.Itens[0].ProdutoId.Should().Be("PROD001");
        response.Itens[0].NomeProduto.Should().Be("Produto Teste");
        response.Itens[0].Quantidade.Should().Be(2);
        response.Itens[0].PrecoUnitario.Should().Be(10.50m);

        var savedPedido = await _fixture.DbContext.Pedidos.FirstOrDefaultAsync(p => p.ClienteId == 123);
        savedPedido.Should().NotBeNull();
        savedPedido!.Status.Should().Be(PedidoStatus.Criado);
        savedPedido.Total.Should().Be(21.00m);
    }

    [Fact]
    public async Task CreateAsync_WithEmptyItens_ShouldReturnBadRequest()
    {
        // Arrange
        await _fixture.ClearDatabase();
        _fixture.ResetMocks();
        
        var request = new CreatePedidoRequest
        {
            ClienteId = 123,
            Itens = new List<CreatePedidoItemRequest>()
        };

        // Act
        var result = await _fixture.Controller.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Informe ao menos 1 item.");

        var pedidoCount = await _fixture.DbContext.Pedidos.CountAsync();
        pedidoCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_WithNullItens_ShouldReturnBadRequest()
    {
        // Arrange
        await _fixture.ClearDatabase();
        _fixture.ResetMocks();
        
        var request = new CreatePedidoRequest
        {
            ClienteId = 123,
            Itens = null!
        };

        // Act
        var result = await _fixture.Controller.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var badRequestResult = result.Result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().Be("Informe ao menos 1 item.");
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingPedido_ShouldReturnOkResult()
    {
        // Arrange
        await _fixture.ClearDatabase();
        _fixture.ResetMocks();
        
        var pedido = new Pedido(456);
        pedido.DefinirTotal(50.00m);
        pedido.AlterarStatus(PedidoStatus.Criado);

        await _fixture.DbContext.Pedidos.AddAsync(pedido);
        await _fixture.DbContext.SaveChangesAsync();

        var pedidoId = pedido.Id; // Get the auto-generated ID

        var itensDocuments = new List<PedidoItemDocument>
        {
            new PedidoItemDocument
            {
                PedidoId = pedidoId,
                ProdutoId = "PROD001",
                NomeProduto = "Produto A",
                Quantidade = 1,
                PrecoUnitario = 25.00m
            },
            new PedidoItemDocument
            {
                PedidoId = pedidoId,
                ProdutoId = "PROD002",
                NomeProduto = "Produto B",
                Quantidade = 1,
                PrecoUnitario = 25.00m
            }
        };

        _fixture.SetupCacheMiss($"pedido:{pedidoId}");
        _fixture.SetupCacheSet();
        _fixture.SetupMongoFindResult(itensDocuments);
        _fixture.SetupMongoFind();

        // Act
        var result = await _fixture.Controller.GetByIdAsync(pedidoId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        
        var response = okResult.Value.Should().BeOfType<PedidoResponse>().Subject;
        response.Id.Should().Be(pedidoId);
        response.ClienteId.Should().Be(456);
        response.Status.Should().Be(PedidoStatus.Criado.ToString());
        response.Total.Should().Be(50.00m);
        response.Itens.Should().HaveCount(2);
        
        response.Itens[0].ProdutoId.Should().Be("PROD001");
        response.Itens[0].NomeProduto.Should().Be("Produto A");
        response.Itens[1].ProdutoId.Should().Be("PROD002");
        response.Itens[1].NomeProduto.Should().Be("Produto B");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistingPedido_ShouldReturnNotFound()
    {
        // Arrange
        await _fixture.ClearDatabase();
        _fixture.ResetMocks();
        
        var nonExistingId = Guid.NewGuid();

        _fixture.SetupCacheMiss($"pedido:{nonExistingId}");

        // Act
        var result = await _fixture.Controller.GetByIdAsync(nonExistingId, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task CreateAsync_WithMultipleItens_ShouldCalculateCorrectTotal()
    {
        // Arrange
        await _fixture.ClearDatabase();
        _fixture.ResetMocks();
        _fixture.SetupCacheSet();
        _fixture.SetupEventService();
        _fixture.SetupMongoFind();
        
        var request = new CreatePedidoRequest
        {
            ClienteId = 789,
            Itens = new List<CreatePedidoItemRequest>
            {
                new CreatePedidoItemRequest
                {
                    ProdutoId = "PROD001",
                    NomeProduto = "Produto A",
                    Quantidade = 3,
                    PrecoUnitario = 15.00m
                },
                new CreatePedidoItemRequest
                {
                    ProdutoId = "PROD002",
                    NomeProduto = "Produto B",
                    Quantidade = 2,
                    PrecoUnitario = 25.50m
                }
            }
        };

        // Act
        var result = await _fixture.Controller.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var createdResult = result.Result.Should().BeOfType<CreatedResult>().Subject;
        
        var response = createdResult.Value.Should().BeOfType<PedidoResponse>().Subject;
        response.ClienteId.Should().Be(789);
        response.Total.Should().Be(96.00m);
        response.Itens.Should().HaveCount(2);

        var savedPedido = await _fixture.DbContext.Pedidos.FirstOrDefaultAsync(p => p.ClienteId == 789);
        savedPedido.Should().NotBeNull();
        savedPedido!.Total.Should().Be(96.00m);
    }
}
