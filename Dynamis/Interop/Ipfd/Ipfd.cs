using Dalamud.Game.Gui.Dtr;
using Dalamud.Plugin.Services;
using Dynamis.Configuration;
using Dynamis.Messaging;
using Dynamis.Resources;
using Dynamis.UI.Windows;
using Microsoft.Extensions.Logging;
using static Dynamis.Utility.ChatGuiUtility;
using static Dynamis.Utility.SeStringUtility;

namespace Dynamis.Interop.Ipfd;

public sealed class Ipfd : IMessageObserver<ConfigurationChangedMessage>, IDisposable
{
    private readonly ResourceProvider       _resourceProvider;
    private readonly ILogger<Ipfd>          _logger;
    private readonly ConfigurationContainer _configuration;
    private readonly MessageHub             _messageHub;
    private readonly IDtrBarEntry           _dtrEntry;

    private          IpfdModule?   _module;
    private readonly Breakpoint?[] _breakpoints;

    public bool Enabled
        => _configuration.Configuration.EnableIpfd;

    public bool Loaded
        => _module is not null;

    public Ipfd(ResourceProvider resourceProvider, ILogger<Ipfd> logger, ConfigurationContainer configuration,
        IDtrBar dtrBar, MessageHub messageHub)
    {
        _resourceProvider = resourceProvider;
        _logger = logger;
        _configuration = configuration;
        _messageHub = messageHub;
        _dtrEntry = dtrBar.Get("Dynamis IPFD", BuildSeString($"{UiGlow("IPFD", Gold)} Loaded"));
        _dtrEntry.Tooltip =
            BuildSeString($"{UiGlow("Dynamis IPFD", Gold)} is currently loaded.\nClick to open settings.");
        _dtrEntry.OnClick += messageHub.Publish<OpenWindowMessage<SettingsWindow>>;
        _dtrEntry.Shown = false;

        _breakpoints = new Breakpoint?[4];
    }

    ~Ipfd()
        => Dispose(false);

    public Breakpoint AllocateBreakpoint()
    {
        lock (this) {
            var module = GetModule();
            if (module is null) {
                throw new InvalidOperationException("Cannot use breakpoints when IPFD is not enabled.");
            }

            for (var i = 0; i < _breakpoints.Length; ++i) {
                if (_breakpoints[i] is not null) {
                    continue;
                }

                var newBreakpoint = new Breakpoint(module, (byte)i, FreeBreakpoint);
                _breakpoints[i] = newBreakpoint;
                return newBreakpoint;
            }

            throw new InvalidOperationException("All breakpoints are currently in use.");
        }
    }

    private void FreeBreakpoint(byte index, Breakpoint breakpoint)
    {
        lock (this) {
            if (index > _breakpoints.Length || _breakpoints[index] != breakpoint) {
                return;
            }

            _breakpoints[index] = null;

            FreeModuleNoLock(false);
        }

        _messageHub.Publish(new BreakpointDisposedMessage(breakpoint));
    }

    public unsafe T Read<T>(ReadOnlyMemory<T> source, int index) where T : unmanaged
    {
        if (index < 0 || index >= source.Length) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        using var sourcePin = source.Pin();
        var destination = stackalloc T[1];
        DoCopy((nint)sourcePin.Pointer + (nint)index * sizeof(T), (nint)destination, sizeof(T));
        return destination[0];
    }

    public unsafe T Read<T>(nint source) where T : unmanaged
    {
        var destination = stackalloc T[1];
        DoCopy(source, (nint)destination, sizeof(T));
        return destination[0];
    }

    public unsafe void Copy<T>(ReadOnlyMemory<T> source, Memory<T> destination) where T : unmanaged
    {
        if (source.Length > destination.Length) {
            throw new ArgumentOutOfRangeException(
                nameof(source), "The source buffer is larger than the destination buffer."
            );
        }

        using var sourcePin = source.Pin();
        using var destinationPin = destination.Pin();
        DoCopy((nint)sourcePin.Pointer, (nint)destinationPin.Pointer, (nint)source.Length * sizeof(T));
    }

    public unsafe void Copy<T>(nint source, int count, Memory<T> destination) where T : unmanaged
    {
        if (count > destination.Length) {
            throw new ArgumentOutOfRangeException(
                nameof(count), "The source buffer is larger than the destination buffer."
            );
        }

        using var destinationPin = destination.Pin();
        DoCopy(source, (nint)destinationPin.Pointer, (nint)count * sizeof(T));
    }

    public unsafe void Copy<T>(ReadOnlyMemory<T> source, nint destination, int count) where T : unmanaged
    {
        if (source.Length > count) {
            throw new ArgumentOutOfRangeException(
                nameof(source), "The source buffer is larger than the destination buffer."
            );
        }

        using var sourcePin = source.Pin();
        DoCopy((nint)sourcePin.Pointer, destination, (nint)count * sizeof(T));
    }

    public unsafe void Copy<T>(nint source, nint destination, int count) where T : unmanaged
        => DoCopy(source, destination, (nint)count * sizeof(T));

    private unsafe void DoCopy(nint source, nint destination, nint size)
    {
        lock (this) {
            if (_module is not null) {
                _module.MemoryCopy(source, destination, size);
                _module.Sync();
                return;
            }
        }

        Buffer.MemoryCopy((void*)source, (void*)destination, size, size);
    }

    public async Task<T> ReadAsync<T>(ReadOnlyMemory<T> source, int index) where T : unmanaged
    {
        if (index < 0 || index >= source.Length) {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        var destination = new T[1];
        using var sourcePin = source.Pin();
        using var destinationPin = ((Memory<T>)destination).Pin();
        Task copyTask;
        unsafe {
            copyTask = DoCopyAsync(
                (nint)sourcePin.Pointer + (nint)index * sizeof(T), (nint)destinationPin.Pointer, sizeof(T)
            );
        }

        await copyTask;
        return destination[0];
    }

    public async Task<T> ReadAsync<T>(nint source) where T : unmanaged
    {
        var destination = new T[1];
        using var destinationPin = ((Memory<T>)destination).Pin();
        Task copyTask;
        unsafe {
            copyTask = DoCopyAsync(source, (nint)destinationPin.Pointer, sizeof(T));
        }

        await copyTask;
        return destination[0];
    }

    public async Task CopyAsync<T>(ReadOnlyMemory<T> source, Memory<T> destination) where T : unmanaged
    {
        if (source.Length > destination.Length) {
            throw new ArgumentOutOfRangeException(
                nameof(source), "The source buffer is larger than the destination buffer."
            );
        }

        using var sourcePin = source.Pin();
        using var destinationPin = destination.Pin();
        Task copyTask;
        unsafe {
            copyTask = DoCopyAsync(
                (nint)sourcePin.Pointer, (nint)destinationPin.Pointer, (nint)source.Length * sizeof(T)
            );
        }

        await copyTask;
    }

    public async Task CopyAsync<T>(nint source, int count, Memory<T> destination) where T : unmanaged
    {
        if (count > destination.Length) {
            throw new ArgumentOutOfRangeException(
                nameof(count), "The source buffer is larger than the destination buffer."
            );
        }

        using var destinationPin = destination.Pin();

        Task copyTask;
        unsafe {
            copyTask = DoCopyAsync(source, (nint)destinationPin.Pointer, (nint)count * sizeof(T));
        }

        await copyTask;
    }

    private Task DoCopyAsync(nint source, nint destination, nint size)
    {
        lock (this) {
            if (_module is not null) {
                _module.MemoryCopy(source, destination, size);
                return _module.SyncAsync();
            }
        }

        unsafe {
            Buffer.MemoryCopy((void*)source, (void*)destination, size, size);
        }

        return Task.CompletedTask;
    }

    public Task SyncAsync(int millisecondsTimeout = Timeout.Infinite,
        CancellationToken cancellationToken = default)
    {
        lock (this) {
            return GetModule()?.SyncAsync(millisecondsTimeout, cancellationToken) ?? Task.CompletedTask;
        }
    }

    public void Sync()
    {
        lock (this) {
            GetModule()?.Sync();
        }
    }

    private IpfdModule? GetModule()
    {
        if (!_configuration.Configuration.EnableIpfd) {
            return null;
        }

        _module ??= new(_resourceProvider, _logger);
        _dtrEntry.Shown = true;
        return _module;
    }

    public void HandleMessage(ConfigurationChangedMessage message)
    {
        if (!message.IsPropertyChanged(nameof(_configuration.Configuration.EnableIpfd))) {
            return;
        }

        lock (this) {
            if (_configuration.Configuration.EnableIpfd) {
                return;
            }

            FreeModuleNoLock(true);
        }
    }

    public void Unload()
    {
        lock (this) {
            FreeModuleNoLock(true);
        }
    }

    private void FreeModuleNoLock(bool force)
    {
        if (force) {
            foreach (var breakpoint in _breakpoints) {
                breakpoint?.Dispose();
            }
        } else {
            if (_breakpoints.OfType<Breakpoint>().Any()) {
                return;
            }
        }

        _module?.Dispose();
        _module = null;
        _dtrEntry.Shown = false;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        Dispose(true);
    }

    private void Dispose(bool disposing)
    {
        lock (this) {
            FreeModuleNoLock(true);
            _dtrEntry.Remove();
        }
    }
}
