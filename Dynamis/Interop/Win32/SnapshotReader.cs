namespace Dynamis.Interop.Win32;

using Mount = (nint Address, ReadOnlyMemory<byte> Data);

public class SnapshotReader(Ipfd.Ipfd? ipfd)
{
    private static readonly IComparer<Mount> MountComparer =
        Comparer<Mount>.Create((lhs, rhs) => lhs.Address.CompareTo(rhs.Address));

    private readonly List<Mount> _mounts = [];

    public void Mount(nint address, ReadOnlyMemory<byte> data)
    {
        if (data.Length == 0) {
            return;
        }

        var i = _mounts.BinarySearch((address, data), MountComparer);
        if (i >= 0 || i < -1 && Overlaps(_mounts[~i - 1],             (address, data))
                   || ~i < _mounts.Count && Overlaps((address, data), _mounts[~i])) {
            throw new InvalidOperationException($"Overlapping mounted data at address {address:X}");
        }

        _mounts.Insert(~i, (address, data));
    }

    public void Mount(ObjectSnapshot? snapshot)
    {
        while (snapshot is not null) {
            if (snapshot.Address.HasValue) {
                Mount(snapshot.Address.Value, snapshot.Data);
            }

            snapshot = snapshot.AssociatedSnapshot;
        }
    }

    private unsafe void Copy(nint baseAddress, nint buffer, int size)
    {
        if (ipfd is not null) {
            ipfd.Copy<byte>(baseAddress, buffer, size);
        } else {
            new ReadOnlySpan<byte>((void*)baseAddress, size).CopyTo(new((void*)buffer, size));
        }
    }

    private unsafe void Copy(ReadOnlyMemory<byte> memory, nint buffer, int size)
    {
        if (memory.Length > size) {
            memory = memory[..size];
        }

        if (ipfd is not null) {
            ipfd.Copy(memory, buffer, size);
        } else {
            memory.Span.CopyTo(new((void*)buffer, size));
        }
    }

    private static void Advance(ref nint source, ref nint destination, ref uint destinationSize, int chunkSize)
    {
        source += chunkSize;
        destination += chunkSize;
        destinationSize -= (uint)chunkSize;
    }

    public unsafe void Read(nint address, Memory<byte> destination)
    {
        using var pin = destination.Pin();
        Read(address, (nint)pin.Pointer, (uint)destination.Length);
    }

    public void Read(nint address, nint destination, uint size)
    {
        if (size == 0) {
            return;
        }

        var iEnd = _mounts.BinarySearch(((nint)(address + size), ReadOnlyMemory<byte>.Empty), MountComparer);
        if (iEnd < 0) {
            iEnd = ~iEnd;
        }

        if (iEnd == 0) {
            Copy(address, destination, (int)size);
            return;
        }

        int chunkSize;
        var iStart = _mounts.BinarySearch((address, ReadOnlyMemory<byte>.Empty), MountComparer);
        if (iStart == -1) {
            chunkSize = Math.Min((int)(_mounts[0].Address - address), (int)size);
            Copy(address, destination, chunkSize);
            Advance(ref address, ref destination, ref size, chunkSize);
            if (size == 0) {
                return;
            }
        }

        if (iStart < 0) {
            iStart = ~iStart - 1;
        }

        for (var i = iStart; i < iEnd; i++) {
            var displacement = address - _mounts[i].Address;
            if (displacement < _mounts[i].Data.Length) {
                chunkSize = Math.Min((int)(_mounts[i].Data.Length - displacement), (int)size);
                Copy(_mounts[i].Data[(int)displacement..], destination, chunkSize);
                Advance(ref address, ref destination, ref size, chunkSize);
                if (size == 0) {
                    return;
                }
            }

            chunkSize = i + 1 < _mounts.Count
                ? Math.Min((int)(_mounts[i + 1].Address - address), (int)size)
                : (int)size;
            Copy(address, destination, chunkSize);
            Advance(ref address, ref destination, ref size, chunkSize);
        }
    }

    private bool Read(nint _, ulong qwBaseAddress, nint lpBuffer, uint nSize, out uint lpNumberOfBytesRead)
    {
        Read((nint)qwBaseAddress, lpBuffer, nSize);
        lpNumberOfBytesRead = nSize;
        return true;
    }

    private static bool Overlaps(Mount lhs, Mount rhs)
        => Math.Max(lhs.Address, rhs.Address) < Math.Min(lhs.Address + lhs.Data.Length, rhs.Address + rhs.Data.Length);

    public static implicit operator SymbolApi.ReadProcessMemoryRoutine(SnapshotReader reader)
        => reader.Read;
}
