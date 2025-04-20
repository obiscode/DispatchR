using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace Dispatcher;

/// <summary>
/// The DispatchR class is responsible for handling requests and publishing notifications.
/// It manages the pipeline of behaviors, executes the handler logic, and dispatches responses.
/// </summary>
public class DispatchR : IDispatchR
{   
    /// <summary>
    /// Gets the service provider used to resolve dependencies.
    /// </summary>
    private readonly IServiceProvider _serviceProvider;
    /// <summary>
    /// A cache for storing handler executors to avoid reflection overhead.
    /// </summary>
    private static readonly ConcurrentDictionary<Type, Func<IServiceProvider, object, CancellationToken, Task<object>>> _handlerCache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="DispatchR"/> class.
    /// </summary>
    /// <param name="serviceProvider">The service provider used to resolve dependencies.</param>

    public DispatchR(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

/// <summary>
/// Sends a request and returns the response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
/// <param name="request">The request to send, implementing <see cref="IRequest{TResponse}"/>.</param>
/// <param name="cancellationToken">A token to cancel the operation if necessary.</param>
/// <returns>A <see cref="Task{TResponse}"/> representing the asynchronous operation.</returns>
/// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
/// <remarks>
/// This method is the core of the dispatchR, allowing for the execution of requests.
/// It uses a pipeline of behaviors to process the request before reaching the handler.
/// The pipeline can be used for cross-cutting concerns such as logging, validation, and transaction management.
/// </remarks>
    public async Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request == null) throw new ArgumentNullException(nameof(request));

        var requestType = request.GetType();

        // Initialize the executor from the handler cache or build it if not present
        var executor = _handlerCache.GetOrAdd(requestType, BuildExecutor<TResponse>);
        var result = await executor(_serviceProvider, request, cancellationToken);

        return (TResponse)result;
    }

    /// <summary>   
    /// Sends a request and returns the response of type <see cref="Unit"/>.
    /// </summary>
    /// <param name="request">The request to send, implementing <see cref="IRequest{Unit}"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation if necessary.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
    /// <remarks>
    /// This method is a convenience overload for requests that do not return a value.
    /// It allows sending requests without needing to specify a response type.
    /// </remarks>
    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<Unit>
    {
        return Send<Unit>(request, cancellationToken);
    }

    /// <summary>
    /// Publishes a notification to all registered handlers.
    /// </summary>
    /// <param name="notification">The notification to publish, implementing <see cref="INotification"/>.</param>
    /// <param name="cancellationToken">A token to cancel the operation if necessary.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the notification is null.</exception>
    /// <remarks>
    /// This method is used to send notifications to multiple handlers.
    /// It does not expect a response and is typically used for events or messages
    /// that need to be broadcasted to interested parties.
    /// The notification is dispatched to all registered handlers that implement <see cref="INotificationHandler{TNotification}"/>.
    /// The handlers are resolved from the service provider and executed asynchronously.
    /// </remarks>
    /// <example>
    /// var notification = new MyNotification();
    /// await dispatchR.Publish(notification);
    /// </example>
    public async Task Publish(INotification notification, CancellationToken cancellationToken = default)
    {
        if (notification is null)
            throw new ArgumentNullException(nameof(notification));

        var notificationType = notification.GetType();
        var handlerType = typeof(INotificationHandler<>).MakeGenericType(notificationType);

        var handlers = _serviceProvider.GetServices(handlerType);

        var tasks = new List<Task>();
        foreach (var handler in handlers)
        {
            var task = ((dynamic)handler!).Handle((dynamic)notification, cancellationToken);
            tasks.Add(task);
        }

        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Builds an executor function for the specified request type.
    /// This function is cached to improve performance on subsequent calls.
    /// </summary>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="requestType">The type of the request.</param>  
    private static Func<IServiceProvider, object, CancellationToken, Task<object>> BuildExecutor<TResponse>(Type requestType)
    {
        var responseType = typeof(TResponse);
        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, responseType);
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(requestType, responseType);

        // Get the InvokePipeline method dynamically
        var method = typeof(DispatchR).GetMethod(nameof(InvokePipeline), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(requestType, responseType);

        return (sp, request, ct) =>
        {
            var handler = sp.GetRequiredService(handlerType);
            var behaviors = sp.GetServices(behaviorType);
            return (Task<object>)method.Invoke(null, new object[] { request, handler, behaviors, ct })!;
        };
    }

    /// <summary>   
    /// Invokes the pipeline of behaviors for the specified request and handler.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam>
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request to send, implementing <see cref="IRequest{TResponse}"/>.</param>
    /// <param name="handler">The handler to execute the request.</param>
    /// <param name="behaviors">The pipeline behaviors to apply.</param>
    /// <param name="cancellationToken">A token to cancel the operation if necessary.</param>
    /// <returns>A <see cref="Task{TResponse}"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request or handler is null.</exception>
    /// <remarks>
    /// This method constructs a pipeline of behaviors that will be executed in reverse order.
    /// Each behavior can perform pre-processing, call the next behavior in the pipeline,
    /// and perform post-processing.
    /// The final result is obtained by invoking the handler.
    /// </remarks>  
    private static async Task<TResponse> InvokePipelineTyped<TRequest, TResponse>(
        TRequest request,
        IRequestHandler<TRequest, TResponse> handler,
        IEnumerable<IPipelineBehavior<TRequest, TResponse>> behaviors,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        Func<Task<TResponse>> next = () => handler.Handle(request, cancellationToken);

        // Iterate through the behaviors in reverse order to build the pipeline
        // Each behavior wraps the next one, allowing for pre- and post-processing
        foreach (var behavior in behaviors.Reverse())
        {
            var current = next;
            next = () => behavior.Handle(request,  cancellationToken, current);
        }

        return await next().ConfigureAwait(false); // await the final result in the pipeline.
    }

    /// <summary>   
    /// Invokes the pipeline of behaviors for the specified request and handler.    
    /// </summary>
    /// <typeparam name="TRequest">The type of the request.</typeparam> 
    /// <typeparam name="TResponse">The type of the response.</typeparam>
    /// <param name="request">The request to send, implementing <see cref="IRequest{TResponse}"/>.</param>
    /// <param name="handler">The handler to execute the request.</param>
    /// <param name="behaviors">The pipeline behaviors to apply.</param>
    /// <param name="cancellationToken">A token to cancel the operation if necessary.</param>
    /// <returns>A <see cref="Task{TResponse}"/> representing the asynchronous operation.</returns> 
    private static async Task<object> InvokePipeline<TRequest, TResponse>(
        TRequest request,
        IRequestHandler<TRequest, TResponse> handler,
        IEnumerable<object> behaviors,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        var typedBehaviors = behaviors.Cast<IPipelineBehavior<TRequest, TResponse>>();
        var result = await InvokePipelineTyped(request, handler, typedBehaviors, cancellationToken);
        return (object)result!;  // return the result as an object.
    }
}
