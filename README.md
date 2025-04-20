# Dispatcher

The `Dispatcher` is a library designed to handle requests and publish notifications through a pipeline of behaviors. It is built to support flexible request handling and pipeline behaviors, making it easy to extend and customize.

## Features

- Request handling with customizable pipeline behaviors.
- Support for multiple request types and responses.
- Publish notifications to multiple handlers.

## Installation

To install the package, use NuGet:

```shell
dotnet add package Dispatcher
```

## Usage

### Sending Requests

```csharp
var response = await dispatcher.Send(new YourRequest(), cancellationToken);
```

## Publishing Notifications

```csharp
await dispatcher.Publish(new YourNotification(), cancellationToken);
```
## Contributing

If you'd like to contribute to this project, please fork the repository and submit a pull request. For bug reports or feature requests, create an issue.

## License

This project is licensed under the MIT License. See the LICENSE file for details.