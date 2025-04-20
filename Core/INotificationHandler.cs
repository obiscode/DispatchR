namespace Dispatcher;

/// <summary>
/// The INotificationHandler interface defines the contract for handling notifications.
/// It provides a method for processing notifications of type <typeparamref name="TNotification"/>.
/// </summary>
/// <typeparam name="TNotification">The type of the notification to handle, implementing <see cref="INotification"/>.</typeparam>

public interface INotificationHandler<in TNotification> where TNotification : INotification
    {
        /// <summary>   
        /// Handles the specified notification.
        /// </summary>
        /// 
        Task Handle(TNotification notification, CancellationToken cancellationToken);
    }
