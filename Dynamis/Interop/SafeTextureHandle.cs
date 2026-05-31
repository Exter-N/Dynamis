using System.Runtime.InteropServices;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures.TextureWraps;
using FFXIVClientStructs.FFXIV.Client.Graphics.Kernel;

namespace Dynamis.Interop;

public unsafe class SafeTextureHandle : SafeHandle, IDalamudTextureWrap
{
    public Texture* Texture
        => (Texture*)handle;

    public override bool IsInvalid
        => handle == 0;

    public ImTextureID Handle
        => new(Texture->D3D11ShaderResourceView);

    public int Width
        => (int)Texture->AllocatedWidth;

    public int Height
        => (int)Texture->AllocatedHeight;

    public SafeTextureHandle(Texture* handle, bool incRef, bool ownsHandle = true)
        : base(0, ownsHandle)
    {
        if (incRef && !ownsHandle) {
            throw new ArgumentException("Non-owning SafeTextureHandle with IncRef is unsupported");
        }

        if (incRef && handle != null) {
            handle->IncRef();
        }

        SetHandle((nint)handle);
    }

    public static SafeTextureHandle CreateInvalid()
        => new(null, false);

    protected override bool ReleaseHandle()
    {
        nint handle;
        lock (this)
        {
            handle      = this.handle;
            this.handle = 0;
        }

        if (handle != 0) {
            ((Texture*)handle)->DecRef();
        }

        return true;
    }
}
