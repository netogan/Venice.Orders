using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Venice.Orders.Api.Tests.Fixtures;
using Venice.Orders.Application.Contracts;
using Venice.Orders.Domain.Entities;
using Venice.Orders.Domain.Enums;
using Venice.Orders.Infrastructure.Mongo;
using Xunit;

namespace Venice.Orders.Api.Tests.Controllers;

public class OrdersControllerIntegrationTests : IClassFixture<OrdersControllerFixture>
{
    private readonly OrdersControllerFixture _fixture;

    public OrdersControllerIntegrationTests(OrdersControllerFixture fixture)
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
        response.Total.Should().Be(21.00m); // 2 * 10.50
        response.Itens.Should().HaveCount(1);
        response.Itens[0].ProdutoId.Should().Be("PROD001");
        response.Itens[0].NomeProduto.Should().Be("Produto Teste");
        response.Itens[0].Quantidade.Should().Be(2);
        response.Itens[0].PrecoUnitario.Should().Be(10.50m);

        // Verify the pedido was saved in the database
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

        // Verify no pedido was saved
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

        // Verify no pedido was saved
        var pedidoCount = await _fixture.DbContext.Pedidos.CountAsync();
        pedidoCount.Should().Be(0);
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
        response.Total.Should().Be(96.00m); // (3 * 15.00) + (2 * 25.50) = 45 + 51 = 96
        response.Itens.Should().HaveCount(2);

        // Verify the pedido was saved with correct total
        var savedPedido = await _fixture.DbContext.Pedidos.FirstOrDefaultAsync(p => p.ClienteId == 789);
        savedPedido.Should().NotBeNull();
        savedPedido!.Total.Should().Be(96.00m);
    }
}
