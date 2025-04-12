namespace Dynamis.UI.PsHost.Input;

public interface IPrompt
{
    private static int _nextId = 0;

    float Height { get; }

    bool Draw(ref bool focus);

    void Cancel();

    protected static int AllocateId()
        => Interlocked.Increment(ref _nextId);
}

public interface IPrompt<out T> : IPrompt
{
    T Result { get; }
}
