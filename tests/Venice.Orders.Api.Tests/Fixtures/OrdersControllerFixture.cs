using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Moq;
using MongoDB.Driver;
using Venice.Orders.Api.Controllers;
using Venice.Orders.Application.Services;
using Venice.Orders.Infrastructure.Mongo;
using Venice.Orders.Infrastructure.Persistence;

namespace Venice.Orders.Api.Tests.Fixtures;

public class OrdersControllerFixture : IDisposable
{
    public OrdersDbContext DbContext { get; }
    public Mock<IMongoContext> MockMongoContext { get; }
    public Mock<IDistributedCache> MockCache { get; }
    public Mock<IConfiguration> MockConfiguration { get; }
    public Mock<IPedidoEventService> MockEventService { get; }
    public Mock<IMongoCollection<PedidoItemDocument>> MockMongoCollection { get; }
    public Mock<IFindFluent<PedidoItemDocument, PedidoItemDocument>> MockFindFluent { get; }
    public Mock<IAsyncCursor<PedidoItemDocument>> MockAsyncCursor { get; }
    public OrdersController Controller { get; }

    public OrdersControllerFixture()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<OrdersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        DbContext = new OrdersDbContext(options);

        // Setup mocks
        MockMongoContext = new Mock<IMongoContext>();
        MockCache = new Mock<IDistributedCache>();
        MockConfiguration = new Mock<IConfiguration>();
        MockEventService = new Mock<IPedidoEventService>();
        MockMongoCollection = new Mock<IMongoCollection<PedidoItemDocument>>();
        MockFindFluent = new Mock<IFindFluent<PedidoItemDocument, PedidoItemDocument>>();
        MockAsyncCursor = new Mock<IAsyncCursor<PedidoItemDocument>>();

        // Setup mongo context mock
        MockMongoContext.Setup(x => x.PedidoItens).Returns(MockMongoCollection.Object);

        // Setup configuration mock
        MockConfiguration.Setup(x => x["Kafka:TopicPedidos"]).Returns("pedidos");

        // Create controller
        Controller = new OrdersController(
            DbContext,
            MockMongoContext.Object,
            MockCache.Object,
            MockConfiguration.Object,
            MockEventService.Object);
    }

    public void ResetMocks()
    {
        MockCache.Reset();
        MockEventService.Reset();
        MockMongoCollection.Reset();
        MockFindFluent.Reset();
        MockAsyncCursor.Reset();

        // Re-setup default mocks
        MockMongoContext.Setup(x => x.PedidoItens).Returns(MockMongoCollection.Object);
        MockConfiguration.Setup(x => x["Kafka:TopicPedidos"]).Returns("pedidos");
    }

    public void SetupCacheHit(string key, string value)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(value);
        MockCache.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
                 .ReturnsAsync(bytes);
    }

    public void SetupCacheMiss(string? key = null)
    {
        if (key != null)
        {
            MockCache.Setup(x => x.GetAsync(key, It.IsAny<CancellationToken>()))
                     .ReturnsAsync((byte[]?)null);
        }
        else
        {
            MockCache.Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                     .ReturnsAsync((byte[]?)null);
        }
    }

    public void SetupCacheSet()
    {
        MockCache.Setup(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<byte[]>(),
            It.IsAny<DistributedCacheEntryOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public void SetupEventService()
    {
        MockEventService.Setup(x => x.PublicarPedidoCriadoAsync(
            It.IsAny<Domain.Events.PedidoCriadoEvent>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public void SetupMongoFindResult(List<PedidoItemDocument> documents)
    {
        // Setup a fake cursor that returns our test data
        var mockCursor = new Mock<IAsyncCursor<PedidoItemDocument>>();
        mockCursor.Setup(_ => _.Current).Returns(documents);
        mockCursor.SetupSequence(_ => _.MoveNext(It.IsAny<CancellationToken>()))
                  .Returns(true)
                  .Returns(false);
        mockCursor.SetupSequence(_ => _.MoveNextAsync(It.IsAny<CancellationToken>()))
                  .ReturnsAsync(true)
                  .ReturnsAsync(false);

        // Setup the collection to return this cursor when Find is called
        MockMongoCollection.Setup(x => x.FindAsync(
            It.IsAny<MongoDB.Driver.FilterDefinition<PedidoItemDocument>>(),
            It.IsAny<MongoDB.Driver.FindOptions<PedidoItemDocument>>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockCursor.Object);
    }

    public void SetupMongoFind()
    {
        // Setup InsertManyAsync to complete successfully  
        MockMongoCollection.Setup(x => x.InsertManyAsync(
            It.IsAny<IEnumerable<PedidoItemDocument>>(),
            It.IsAny<MongoDB.Driver.InsertManyOptions>(),
            It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    public async Task ClearDatabase()
    {
        DbContext.Pedidos.RemoveRange(DbContext.Pedidos);
        await DbContext.SaveChangesAsync();
    }

    public void Dispose()
    {
        DbContext?.Dispose();
    }
}
