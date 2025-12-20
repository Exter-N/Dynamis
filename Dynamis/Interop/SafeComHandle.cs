using System.Runtime.InteropServices;
using TerraFX.Interop.Windows;

namespace Dynamis.Interop;

/// <summary> Not quite a <see cref="ComPtr{T}"/>. </summary>
public unsafe class SafeComHandle<T> : SafeHandle where T : unmanaged, IUnknown.Interface
{
    public T* Object
        => (T*)handle;

    public override bool IsInvalid
        => handle == 0;

    public SafeComHandle(T* @object, bool addRef, bool ownsHandle = true)
        : base(0, ownsHandle)
    {
        if (addRef && !ownsHandle) {
            throw new ArgumentException("Non-owning SafeComHandle with AddRef is unsupported");
        }

        if (addRef && @object != null) {
            @object->AddRef();
        }

        SetHandle((nint)@object);
    }

    protected override bool ReleaseHandle()
    {
        nint handle;
        lock (this)
        {
            handle      = this.handle;
            this.handle = 0;
        }

        if (handle != 0) {
            ((T*)handle)->Release();
        }

        return true;
    }
}
