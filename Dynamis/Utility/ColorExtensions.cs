using System.Numerics;

namespace Dynamis.Utility;

internal static class ColorExtensions
{
    public static Vector3 ToVector3(this uint color)
        => new((color & 0xFF) / 255.0f, ((color >> 8) & 0xFF) / 255.0f, ((color >> 16) & 0xFF) / 255.0f);

    public static Vector4 ToVector4(this uint color)
        => new((color & 0xFF) / 255.0f, ((color >> 8) & 0xFF) / 255.0f, ((color >> 16) & 0xFF) / 255.0f, ((color >> 24) & 0xFF) / 255.0f);

    public static uint ToUInt32(this Vector3 color)
        => (uint)Math.Round(Math.Clamp(color.X * 255.0f, 0.0f, 255.0f))
         | ((uint)Math.Round(Math.Clamp(color.Y * 255.0f, 0.0f, 255.0f)) << 8)
         | ((uint)Math.Round(Math.Clamp(color.Z * 255.0f, 0.0f, 255.0f)) << 16)
         | 0xFF000000u;

    public static uint ToUInt32(this Vector4 color)
        => (uint)Math.Round(Math.Clamp(color.X * 255.0f, 0.0f, 255.0f))
         | ((uint)Math.Round(Math.Clamp(color.Y * 255.0f, 0.0f, 255.0f)) << 8)
         | ((uint)Math.Round(Math.Clamp(color.Z * 255.0f, 0.0f, 255.0f)) << 16)
         | ((uint)Math.Round(Math.Clamp(color.W * 255.0f, 0.0f, 255.0f)) << 24);
}
