namespace Dispatcher;

/// <summary>   
/// The IRequestHandler interface defines the contract for handling requests.
/// It provides a method for processing requests of type <typeparamref name="TRequest"/> and returning a response of type <typeparamref name="TResponse"/>.
/// </summary>
/// <typeparam name="TRequest">The type of the request to handle, implementing <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the response to return.</typeparam>
public interface IRequestHandler<in TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    /// <summary>
    /// Handles the specified request and returns a response of type <typeparamref name="TResponse"/>.
    /// </summary>
    /// <param name="request">The request to handle.</param>
    /// <param name="cancellationToken">A token to cancel the operation if necessary.</param>
    /// <returns>A <see cref="Task{TResponse}"/> representing the asynchronous operation.</returns> 
    /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken);
}
