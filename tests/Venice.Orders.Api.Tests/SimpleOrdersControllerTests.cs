using Xunit;
using Venice.Orders.Api.Tests.Fixtures;

namespace Venice.Orders.Api.Tests;

public class SimpleOrdersControllerTests : IClassFixture<OrdersControllerFixture>
{
    private readonly OrdersControllerFixture _fixture;

    public SimpleOrdersControllerTests(OrdersControllerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void Fixture_ShouldNotBeNull()
    {
        Assert.NotNull(_fixture);
        Assert.NotNull(_fixture.Controller);
    }
}
