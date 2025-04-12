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

    /// <summary> Slightly rotates a color's hue, to generate an alternate one for emphasis. </summary>
    public static uint Emphasis(this uint color)
    {
        var shift = (color & 0xF8F8F8) >> 3;
        return color + (((shift & 0xFFFF) << 8) | ((shift & 0xFF0000) >> 16)) - shift;
    }

    public static uint ToUInt32(this ConsoleColor color)
        => color switch
        {
            ConsoleColor.Black       => 0xFF000000u,
            ConsoleColor.DarkBlue    => 0xFF8B0000u,
            ConsoleColor.DarkGreen   => 0xFF006400u,
            ConsoleColor.DarkCyan    => 0xFF8B8B00u,
            ConsoleColor.DarkRed     => 0xFF00008Bu,
            ConsoleColor.DarkMagenta => 0xFF8B008Bu,
            ConsoleColor.DarkYellow  => 0xFF008080u,
            ConsoleColor.Gray        => 0xFFA9A9A9u,
            ConsoleColor.DarkGray    => 0xFF808080u,
            ConsoleColor.Blue        => 0xFFFF0000u,
            ConsoleColor.Green       => 0xFF008000u,
            ConsoleColor.Cyan        => 0xFFFFFF00u,
            ConsoleColor.Red         => 0xFF0000FFu,
            ConsoleColor.Magenta     => 0xFFFF00FFu,
            ConsoleColor.Yellow      => 0xFF00FFFFu,
            ConsoleColor.White       => 0xFFFFFFFFu,
            _                        => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };

    public static ushort ToSeStringColor(this ConsoleColor color)
        => color switch
        {
            ConsoleColor.Black       => 7,
            ConsoleColor.DarkBlue    => 38,
            ConsoleColor.DarkGreen   => 46,
            ConsoleColor.DarkCyan    => 36,
            ConsoleColor.DarkRed     => 18,
            ConsoleColor.DarkMagenta => 49,
            ConsoleColor.DarkYellow  => 32,
            ConsoleColor.Gray        => 3,
            ConsoleColor.DarkGray    => 4,
            ConsoleColor.Blue        => 37,
            ConsoleColor.Green       => 45,
            ConsoleColor.Cyan        => 35,
            ConsoleColor.Red         => 16,
            ConsoleColor.Magenta     => 48,
            ConsoleColor.Yellow      => 31,
            ConsoleColor.White       => 1,
            _                        => throw new ArgumentOutOfRangeException(nameof(color), color, null)
        };
}
