namespace Dynamis.Messaging;

public interface IMessageObserver;

public interface IMessageObserver<in T> : IMessageObserver where T : notnull
{
    public void HandleMessage(T message);
}
