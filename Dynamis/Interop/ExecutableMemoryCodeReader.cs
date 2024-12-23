using Dynamis.Interop.Win32;
using Iced.Intel;

namespace Dynamis.Interop;

public sealed class ExecutableMemoryCodeReader(nint startingAddress, Ipfd.Ipfd? ipfd) : CodeReader
{
    private static readonly int PageSize = Environment.SystemPageSize;

    private readonly byte[] _snapshot = new byte[PageSize];

    private nint _page   = startingAddress & -PageSize;
    private int  _offset = (int)(startingAddress & (PageSize - 1));
    private bool _loaded = false;

    public override unsafe int ReadByte()
    {
        if (!_loaded) {
            if (!VirtualMemory.GetProtection(_page).CanExecute()) {
                return -1;
            }

            if (ipfd is not null) {
                ipfd.Copy<byte>(_page, PageSize, _snapshot);
            } else {
                new ReadOnlySpan<byte>((void*)_page, PageSize).CopyTo(_snapshot);
            }
            _loaded = true;
        }

        var ret = _snapshot[_offset];
        _offset = (_offset + 1) & (PageSize - 1);
        if (_offset == 0) {
            _page += PageSize;
            _loaded = false;
        }

        return ret;
    }
}
