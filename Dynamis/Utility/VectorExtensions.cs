using System.Numerics;
using SharpDX;

namespace Dynamis.Utility;

internal static class VectorExtensions
{
    public static Vector2 Contain(this Vector2 vec, Vector2 max)
    {
        if (vec.X > max.X) {
            vec = max with
            {
                Y = vec.Y * max.X / vec.X,
            };
        }

        if (vec.Y > max.Y) {
            vec = max with
            {
                X = vec.X * max.Y / vec.Y,
            };
        }

        return vec;
    }

    public static Size2 ToSize(this Vector2 vec)
        => new((int)vec.X, (int)vec.Y);

    public static Vector2 ToVector(this Size2 size)
        => new(size.Width, size.Height);
}
