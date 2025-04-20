# DispatchR

A lightweight and extensible CQRS-style dispatchR (Mediator pattern) for .NET applications. Supports request/response handlers, notifications, and pipeline behaviors.

---

## 🚀 Quick Start

### 1. Install via NuGet

```bash
Install-Package DispatchR
```

or via .NET CLI:

```bash
dotnet add package DispatchR
```

---

### 2. Define a Request and Handler

```csharp
public class GreetUser : IRequest<string>
{
    public string Name { get; set; }
}

public class GreetUserHandler : IRequestHandler<GreetUser, string>
{
    public Task<string> Handle(GreetUser request, CancellationToken cancellationToken)
    {
        return Task.FromResult($"Hello, {request.Name}!");
    }
}
```

---

### 3. Register Services

```csharp
var services = new ServiceCollection();

// Register DispatchR
services.AddDispatchR();

// Register Handlers
services.AddTransient<IRequestHandler<GreetUser, string>, GreetUserHandler>();

// Optional: Register Pipeline Behaviors
services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

var provider = services.BuildServiceProvider();
```

---

### 4. Dispatch a Request

```csharp
var dispatchR = provider.GetRequiredService<IDispatchR>();

var response = await dispatchR.Send(new GreetUser { Name = "Ada" });
Console.WriteLine(response); // Hello, Ada!
```

---

## 🧩 Add Middleware with `IPipelineBehavior`

```csharp
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        Console.WriteLine($"Handling {typeof(TRequest).Name}");
        var response = await next();
        Console.WriteLine($"Handled {typeof(TResponse).Name}");
        return response;
    }
}
```

---

## 📣 Publish Notifications

```csharp
public class UserRegistered : INotification
{
    public string Email { get; set; }
}

public class SendWelcomeEmail : INotificationHandler<UserRegistered>
{
    public Task Handle(UserRegistered notification, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Welcome email sent to {notification.Email}");
        return Task.CompletedTask;
    }
}
```

Register and publish:

```csharp
services.AddTransient<INotificationHandler<UserRegistered>, SendWelcomeEmail>();

await dispatchR.Publish(new UserRegistered { Email = "ada@example.com" });
```

---

## ✅ Features

- Supports `IRequest<TResponse>` for commands/queries
- Supports `INotification` for events
- Pluggable `IPipelineBehavior<TRequest, TResponse>` middleware
- Thread-safe handler caching
- Zero dependencies beyond `Microsoft.Extensions.DependencyInjection`

---

---

## 🤝 Contributing

Contributions, issues, and feature requests are welcome! Here's how to get started:

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/awesome-feature`)
3. Commit your changes (`git commit -am 'Add awesome feature'`)
4. Push to the branch (`git push origin feature/awesome-feature`)
5. Open a Pull Request 🚀

Please ensure all code is covered with unit tests and follows existing patterns. Run tests locally before submitting.

---

## 📦 Building and Publishing

To build and pack the library as a NuGet package:

```bash
dotnet pack -c Release
```
This will generate a .nupkg file in the bin/Release folder. To push it to NuGet.org:
```bash
dotnet nuget push bin/Release/DispatchR.*.nupkg -k <your-api-key> -s https://api.nuget.org/v3/index.json
```
---

## 📚 Roadmap

 - Built-in validation behavior

 - Built-in performance metrics behavior

 - Roslyn-based code generator (optional)

 - Middleware pipelines for INotification events

---

## 🛠️ Requirements
- .NET 9 or later

- Microsoft.Extensions.DependencyInjection

---

## 💬 Questions or Feedback?
Feel free to open an issue or start a discussion on the GitHub repository.

---

## 📦 License

MIT License

Copyright (c) 2025 Obinna Ezeh