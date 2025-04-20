namespace Dispatcher;

/// <summary>
/// The IPipelineBehavior interface defines the contract for pipeline behaviors.
/// It provides a method for handling requests and responses in a pipeline.
/// Pipeline behaviors can be used for cross-cutting concerns such as logging, validation, and transaction management.
/// </summary>
/// <typeparam name="TRequest">The type of the request, implementing <see cref="IRequest{TResponse}"/>.</typeparam>
/// <typeparam name="TResponse">The type of the response.</typeparam>
public interface IPipelineBehavior<TRequest, TResponse> 
{
    /// <summary>
    /// Handles the specified request and response in the pipeline.
    /// </summary>
    /// <param name="request">The request to handle.</param>        
    /// <param name="cancellationToken">A token to cancel the operation if necessary.</param>
    /// <param name="next">A function to call the next behavior in the pipeline.</param>
    /// <returns>A <see cref="Task{TResponse}"/> representing the asynchronous operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the request is null.</exception>
    Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, Func<Task<TResponse>>  next);
}
