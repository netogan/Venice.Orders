using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Venice.Orders.Api.Tests.Fixtures;
using Venice.Orders.Application.Contracts;
using Xunit;

namespace Venice.Orders.Api.Tests.Controllers;

public class SimpleOrdersApiTests : IClassFixture<OrdersControllerFixture>
{
    private readonly OrdersControllerFixture _fixture;

    public SimpleOrdersApiTests(OrdersControllerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CreateAsync_WithEmptyItems_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreatePedidoRequest
        {
            ClienteId = 123,
            Itens = new List<CreatePedidoItemRequest>()
        };

        // Act
        var result = await _fixture.Controller.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = actionResult as BadRequestObjectResult;
        badRequest!.Value.Should().Be("Informe ao menos 1 item.");
    }

    [Fact]
    public async Task CreateAsync_WithNullItems_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new CreatePedidoRequest
        {
            ClienteId = 123,
            Itens = null!
        };

        // Act
        var result = await _fixture.Controller.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        var actionResult = result.Result;
        actionResult.Should().BeOfType<BadRequestObjectResult>();
        var badRequest = actionResult as BadRequestObjectResult;
        badRequest!.Value.Should().Be("Informe ao menos 1 item.");
    }
}
