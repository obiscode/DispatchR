using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Dispatcher.Tests;

public class DispatcherTests
{
    [Fact]
    public async Task Send_WithValidRequest_CallsHandler()
    {
        var services = new ServiceCollection();

        var handlerMock = new Mock<IRequestHandler<TestRequest, string>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<TestRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Test Response");

        services.AddSingleton(handlerMock.Object);
        services.AddTransient<IDispatchR, DispatchR>();

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IDispatchR>();

        var request = new TestRequest { Message = "Hello World" };

        var response = await dispatcher.Send(request);

        Assert.Equal("Test Response", response);
        handlerMock.Verify(h => h.Handle(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Send_WithNoHandler_ThrowsInvalidOperationException()
    {
        var services = new ServiceCollection();
        services.AddTransient<IDispatchR, DispatchR>();

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IDispatchR>();

        var request = new TestRequest { Message = "Hello World" };

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.Send(request));
    }

    [Fact]
    public async Task Send_WithNullRequest_ThrowsArgumentNullException()
    {
        var services = new ServiceCollection();
        services.AddTransient<IDispatchR, DispatchR>();

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IDispatchR>();

        await Assert.ThrowsAsync<ArgumentNullException>(() => dispatcher.Send<string>(null));
    }

    [Fact]
public async Task Send_WithBehaviors_CallsBehaviorsInCorrectOrder()
{
    var services = new ServiceCollection();
    services.AddScoped<IRequestHandler<TestRequest, string>, TestRequestHandler>();
    services.AddScoped<IPipelineBehavior<TestRequest, string>, Behavior1<TestRequest, string>>();
    services.AddScoped<IPipelineBehavior<TestRequest, string>, Behavior2<TestRequest, string>>();
    services.AddSingleton<IDispatchR, DispatchR>();
    services.AddScoped<OrderTracker>();

    var provider = services.BuildServiceProvider();
    var dispatcher = provider.GetRequiredService<IDispatchR>();

    var result = await dispatcher.Send(new TestRequest());

    var orderTracker = provider.GetRequiredService<OrderTracker>();
    var expectedOrder = new List<string>
    {
        "B1_Pre",
        "B2_Pre",
        "B2_Post",
        "B1_Post"
    };
    Assert.Equal(expectedOrder, orderTracker.ExecutionOrder);

    Assert.Equal("B1_B2_Original Response_B2_B1", result);
}

    [Fact]
    public async Task Publish_WithMultipleHandlers_CallsAllHandlers()
    {
        var services = new ServiceCollection();

        var handler1Mock = new Mock<INotificationHandler<TestNotification>>();
        handler1Mock
            .Setup(h => h.Handle(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler2Mock = new Mock<INotificationHandler<TestNotification>>();
        handler2Mock
            .Setup(h => h.Handle(It.IsAny<TestNotification>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        services.AddSingleton(handler1Mock.Object);
        services.AddSingleton(handler2Mock.Object);
        services.AddTransient<IDispatchR, DispatchR>();

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IDispatchR>();

        var notification = new TestNotification { Message = "Test Event" };

        await dispatcher.Publish(notification);

        handler1Mock.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
        handler2Mock.Verify(h => h.Handle(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Publish_WithNoHandlers_DoesNotThrow()
    {
        var services = new ServiceCollection();
        services.AddTransient<IDispatchR, DispatchR>();

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IDispatchR>();

        var notification = new TestNotification { Message = "Empty" };

        var ex = await Record.ExceptionAsync(() => dispatcher.Publish(notification));
        Assert.Null(ex); // Should not throw
    }

    [Fact]
    public async Task Send_WithNoResponse_CallsHandler()
    {
        var services = new ServiceCollection();

        var handlerMock = new Mock<IRequestHandler<TestRequestNoResponse, Unit>>();
        handlerMock
            .Setup(h => h.Handle(It.IsAny<TestRequestNoResponse>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Unit.Value);

        services.AddSingleton(handlerMock.Object);
        services.AddTransient<IDispatchR, DispatchR>();

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IDispatchR>();

        var request = new TestRequestNoResponse { Message = "Hello World" };

        await dispatcher.Send(request);

        handlerMock.Verify(h => h.Handle(request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ServiceCollectionExtensions_RegistersHandlers()
    {
        var services = new ServiceCollection();

        services.AddDispatcher(typeof(DispatcherTests));

        services.AddTransient<IRequestHandler<TestRegisterRequest, string>, TestRegisterRequestHandler>();
        services.AddTransient<INotificationHandler<TestRegisterNotification>, TestRegisterNotificationHandler>();

        var serviceProvider = services.BuildServiceProvider();
        var dispatcher = serviceProvider.GetRequiredService<IDispatchR>();

        var request = new TestRegisterRequest { Message = "Test" };
        var notification = new TestRegisterNotification { Message = "Test" };

        var response = await dispatcher.Send(request);
        Assert.Equal("Handled: Test", response);

        await dispatcher.Publish(notification);
    }
}

// === Test Models and Behaviors ===

public class TestRequest : IRequest<string>
{
    public string Message { get; set; }
}

public class TestRequestNoResponse : IRequest<Unit>
{
    public string Message { get; set; }
}

public class TestRegisterRequest : IRequest<string>
{
    public string Message { get; set; }
}

public class TestRegisterRequestHandler : IRequestHandler<TestRegisterRequest, string>
{
    public Task<string> Handle(TestRegisterRequest request, CancellationToken cancellationToken)
        => Task.FromResult($"Handled: {request.Message}");
}

public class TestRegisterNotification : INotification
{
    public string Message { get; set; }
}

public class TestRegisterNotificationHandler : INotificationHandler<TestRegisterNotification>
{
    public Task Handle(TestRegisterNotification notification, CancellationToken cancellationToken)
        => Task.CompletedTask;
}

public class TestNotification : INotification
{
    public string Message { get; set; }
}

public class OrderTracker
{
    public List<string> ExecutionOrder { get; } = new();
}


public class Behavior1<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
    private readonly OrderTracker _orderTracker;
    public Behavior1(OrderTracker orderTracker) => _orderTracker = orderTracker;
    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, Func<Task<TResponse>>  next)
    {
        _orderTracker.ExecutionOrder.Add("B1_Pre");
        var response = await next();
        _orderTracker.ExecutionOrder.Add("B1_Post");
        var modified = $"B1_{response}_B1";

        return (TResponse)(object)modified;
    }
}

public class Behavior2<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
{
         private readonly OrderTracker _orderTracker;
    public Behavior2(OrderTracker orderTracker) => _orderTracker = orderTracker;

    public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, Func<Task<TResponse>> next)
    {
        _orderTracker.ExecutionOrder.Add("B2_Pre");
        var response = await next();
        _orderTracker.ExecutionOrder.Add("B2_Post");
        var modified = $"B2_{response}_B2";

        return (TResponse)(object)modified;
    }
}

public class TestRequestHandler : IRequestHandler<TestRequest, string>
{
    public Task<string> Handle(TestRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult("Original Response");
    }
}
