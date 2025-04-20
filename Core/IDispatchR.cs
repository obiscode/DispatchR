namespace Dispatcher;

/// <summary>
/// The IDispatchR interface defines the contract for dispatching requests and publishing notifications.
/// It provides methods for sending requests and publishing notifications.
/// </summary>
public interface IDispatchR
    {
        /// <summary>   
        /// Sends a request and returns the response of type <typeparamref name="TResponse"/>.
        /// </summary>
        /// <typeparam name="TResponse">The type of the response.</typeparam>
        /// <param name="request">The request to send, implementing <see cref="IRequest{TResponse}"/>.</param>
        /// <param name="cancellationToken">A token to cancel the operation if necessary.</param>
        /// <returns>A <see cref="Task{TResponse}"/> representing the asynchronous operation.</returns> 
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>

        Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
        /// <summary>
        /// Sends a request and returns the response of type <see cref="Unit"/>.
        /// </summary>
        /// <param name="request">The request to send, implementing <see cref="IRequest{Unit}"/>.</param>
        /// <param name="cancellationToken">A token to cancel the operation if necessary.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
        Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
        where TRequest : IRequest<Unit>;

        /// <summary>
        /// Publishes a notification to all registered handlers.
        /// </summary>
        /// <param name="notification">The notification to publish, implementing <see cref="INotification"/>.</param>
        /// <param name="cancellationToken">A token to cancel the operation if necessary.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the notification is null.</exception>
        Task Publish(INotification notification, CancellationToken cancellationToken = default);
    }
