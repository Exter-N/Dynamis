using Dalamud.Plugin.Services;
using Microsoft.Extensions.Logging;

namespace Dynamis.Messaging;

public sealed class MessageHub
{
    private readonly ILogger<MessageHub>                 _logger;
    private readonly IFramework                          _framework;
    private readonly IEnumerable<Lazy<IMessageObserver>> _observers;

    public MessageHub(ILogger<MessageHub> logger, IFramework framework, IEnumerable<Lazy<IMessageObserver>> observers)
    {
        _logger = logger;
        _framework = framework;
        _observers = observers;
    }

    public void Publish<T>() where T : notnull, new()
        => Publish(new T());

    public Task PublishOnFrameworkThread<T>() where T : notnull, new()
        => PublishOnFrameworkThread(new T());

    public void Publish<T>(T message) where T : notnull
    {
        foreach (var observer in _observers) {
            if (observer.Value is IMessageObserver<T> typedObserver) {
                Dispatch(message, typedObserver);
            }
        }
    }

    public Task PublishOnFrameworkThread<T>(T message) where T : notnull
        => _framework.RunOnFrameworkThread(() => Publish(message));

    private void Dispatch<T>(T message, IMessageObserver<T> observer) where T : notnull
    {
        try {
            observer.HandleMessage(message);
        } catch (Exception e) {
            _logger.LogError(
                e, "Error while dispatching message of type {MessageType} to observer {ObserverType}", typeof(T),
                observer.GetType()
            );
        }
    }
}
