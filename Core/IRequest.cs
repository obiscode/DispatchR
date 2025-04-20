namespace Dispatcher;

/// <summary>
/// The IRequest interface is a marker interface for requests.
/// It is used to identify classes that represent requests in the system.
/// Requests are typically used to perform operations or retrieve data.
/// </summary>  
public interface IRequest { }

/// <summary>
/// The IRequest interface is a generic version of the IRequest interface.
/// It is used to identify classes that represent requests with a specific response type.
/// This interface is typically used in the context of a request-response pattern.
/// </summary>
/// <typeparam name="TResponse">The type of the response.</typeparam>
 public interface IRequest<out TResponse> : IRequest { }