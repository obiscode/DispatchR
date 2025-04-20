   using Microsoft.Extensions.DependencyInjection;

    namespace  Dispatcher;
   public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDispatcher(this IServiceCollection services, params Type[] markers)
    {
        // Register the dispatcher
        services.AddScoped<IDispatchR, DispatchR>();

        // Find and register handlers
        foreach (var marker in markers)
        {
            var assembly = marker.Assembly;

            // Register request handlers
            RegisterRequestHandlers(services, assembly);

            // Register notification handlers
            RegisterNotificationHandlers(services, assembly);
        }

        return services;
    }

    private static void RegisterRequestHandlers(IServiceCollection services, System.Reflection.Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                .Select(i => new { HandlerType = t, InterfaceType = i }));

        foreach (var handler in handlerTypes)
        {
            services.AddTransient(handler.InterfaceType, handler.HandlerType);
        }
    }

    private static void RegisterNotificationHandlers(IServiceCollection services, System.Reflection.Assembly assembly)
    {
        var handlerTypes = assembly.GetTypes()
            .Where(t => !t.IsAbstract && !t.IsInterface)
            .SelectMany(t => t.GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(INotificationHandler<>))
                .Select(i => new { HandlerType = t, InterfaceType = i }));

        foreach (var handler in handlerTypes)
        {
            services.AddTransient(handler.InterfaceType, handler.HandlerType);
        }
    }
}
