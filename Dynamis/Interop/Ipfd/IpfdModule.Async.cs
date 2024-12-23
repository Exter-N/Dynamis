using System.Runtime.InteropServices;

namespace Dynamis.Interop.Ipfd;

public sealed partial class IpfdModule
{
    public async Task SyncAsync(int millisecondsTimeout = Timeout.Infinite,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_ipfdLibrary.IsInvalid, this);
        using var @event = new ManualResetEvent(false);
        unsafe {
            Marshal.ThrowExceptionForHR(_setEventFn(@event.SafeWaitHandle.DangerousGetHandle()));
        }

        await @event.WaitOneAsync(millisecondsTimeout, cancellationToken);
    }
}
