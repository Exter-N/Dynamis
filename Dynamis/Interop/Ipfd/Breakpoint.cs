namespace Dynamis.Interop.Ipfd;

public sealed class Breakpoint : IDisposable
{
    private readonly IpfdModule               _ipfd;
    private          byte                     _index;
    private readonly Action<byte, Breakpoint> _free;

    private nint            _address;
    private BreakpointFlags _flags;

    public byte Index
        => _index;

    public bool IsValid
        => _index != byte.MaxValue;

    public nint Address
        => _address;

    public BreakpointFlags Flags
        => _flags;

    public event EventHandler<BreakpointEventArgs>? Hit;

    public Breakpoint(IpfdModule ipfd, byte index, Action<byte, Breakpoint> free)
    {
        _ipfd = ipfd;
        _index = index;
        _free = free;

        _address = 0;
        _flags = 0;

        _ipfd.Breakpoint += OnIpfdBreakpoint;
    }

    ~Breakpoint()
        => Dispose(false);

    public Task? ModifyAsync(nint address, BreakpointFlags flags, bool returnTask = true)
    {
        if ((flags & BreakpointFlags.DataReadsAndWrites) == BreakpointFlags.InstructionExecution) {
            flags &= ~BreakpointFlags.LengthFour;
        }

        lock (this) {
            ObjectDisposedException.ThrowIf(_index == byte.MaxValue, this);

            if (address == _address && flags == _flags) {
                return returnTask ? Task.CompletedTask : null;
            }

            _address = address;
            _flags = flags;

            _ipfd.SetBreakpoint(_index, address, flags);
            return returnTask ? _ipfd.SyncAsync() : null;
        }
    }

    public Task? DisableAsync(bool returnTask = true)
    {
        const BreakpointFlags enable = BreakpointFlags.LocalEnable | BreakpointFlags.GlobalEnable;
        lock (this) {
            if (_index == byte.MaxValue || (_flags & enable) == 0) {
                return returnTask ? Task.CompletedTask : null;
            }

            _flags &= ~enable;
            _ipfd.SetBreakpoint(_index, _address, _flags);
            return returnTask ? _ipfd.SyncAsync() : null;
        }
    }

    public void Disable()
        => DisableAsync()!.Wait();

    public Task<T> WaitForHitAsync<T>(Func<BreakpointEventArgs, T> inspection, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<T>();
        void OnHit(object? sender, BreakpointEventArgs e)
        {
            Hit -= OnHit;
            tcs.TrySetResult(inspection(e));
        }

        Hit += OnHit;

        cancellationToken.Register(
            () =>
            {
                Hit -= OnHit;
                tcs.TrySetCanceled(cancellationToken);
            }
        );

        return tcs.Task;
    }

    private void OnIpfdBreakpoint(object? sender, BreakpointEventArgs e)
    {
        lock (this) {
            if (_index == byte.MaxValue || (e.Which & (1 << _index)) == 0) {
                return;
            }
        }

        Hit?.Invoke(this, e);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        byte index;
        lock (this) {
            if (_address != 0 || _flags != 0) {
                _address = 0;
                _flags = 0;

                _ipfd.SetBreakpoint(_index, 0, 0);
            }

            _ipfd.Breakpoint -= OnIpfdBreakpoint;

            index = _index;
            _index = byte.MaxValue;
        }
        _free(index, this);
    }
}
