#if WITH_SMA
namespace Dynamis.UI.PsHost.Output;

public interface IParagraph
{
    private static int _nextId = 0;

    void Draw(ParagraphDrawFlags flags);

    protected static int AllocateId()
        => Interlocked.Increment(ref _nextId);
}
#endif
