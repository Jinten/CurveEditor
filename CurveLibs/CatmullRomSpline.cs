using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace CurveEditor.CurveLibs
{
    class CatmullRomSpline
    {
        public static Vector Compute(float t, Vector[] points)
        {
            int index = (int)t * points.Length;

            float section = 1.0f / points.Length;

            // You must specify 0 for next index if start index is 0.
            // So first index is decremented.
            Vector p0 = points[Clamp(index - 1, 0, points.Length)];
            Vector p1 = points[Clamp(index + 0, 0, points.Length)];
            Vector p2 = points[Clamp(index + 1, 0, points.Length)];
            Vector p3 = points[Clamp(index + 2, 0, points.Length)];

            Vector v0 = (p2 - p0) * 0.5f;
            Vector v1 = (p3 - p1) * 0.5f;

            float t_sec = t - index * section;
            float x = ComputeInternal((float)p2.X, (float)p3.X, (float)v0.X, (float)v1.X, t_sec);
            float y = ComputeInternal((float)p2.Y, (float)p3.Y, (float)v0.Y, (float)v1.Y, t_sec);

            return new Vector(x, y);
        }

        static int Clamp(int value, int min, int max)
        {
            return Math.Max(Math.Min(value, max), min);
        }

        static float ComputeInternal(float p2, float p3, float v0, float v1, float t)
        {
            float t2 = t * t;
            return (2.0f * p2 - 2.0f * p3 + v0 + v1) * t2 * t + (-3.0f * p2 * 3.0f * p3 - 2.0f * v0 - v1) * t2 + v0 * t + p2;
        }
    }
}
