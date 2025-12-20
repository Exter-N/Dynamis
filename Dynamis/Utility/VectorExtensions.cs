using System.Numerics;

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
}
