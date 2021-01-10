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
        public static float[] Compute(int nSectionDivision, float[] values, float minValue, float maxValue)
        {
            if (nSectionDivision == 0)
            {
                throw new InvalidProgramException("nSectionDivision is Zero.");
            }

            float rcp = 1.0f / nSectionDivision;

            var results = new List<float>();

            for (int i = 0; i < values.Length - 1; ++i)
            {
                for (int j = 0; j < nSectionDivision; ++j)
                {
                    float t = j * rcp;
                    var value = ComputeSingle(i, t, values);
                    results.Add(Math.Min(Math.Max(value, minValue), maxValue));
                }

                results.Add(Math.Min(Math.Max(values[i + 1], minValue), maxValue));
            }

            return results.ToArray();
        }

        public static float[] Compute(int nSectionDivision, float[] values)
        {
            if(nSectionDivision == 0)
            {
                throw new InvalidProgramException("nSectionDivision is Zero.");
            }

            float rcp = 1.0f / nSectionDivision;

            var results = new List<float>();

            for (int i = 0; i < values.Length - 1; ++i)
            {
                for (int j = 0; j < nSectionDivision; ++j)
                {
                    float t = j * rcp;
                    results.Add(ComputeSingle(i, t, values));
                }

                results.Add(values[i + 1]);
            }

            return results.ToArray();
        }

        public static float ComputeSingle(int index, float t, float[] values)
        {
            // You must specify 0 for next index if start index is 0.
            // So first index is decremented.
            float p0 = values[Clamp(index - 1, 0, values.Length - 1)];
            float p1 = values[Clamp(index + 0, 0, values.Length - 1)];
            float p2 = values[Clamp(index + 1, 0, values.Length - 1)];
            float p3 = values[Clamp(index + 2, 0, values.Length - 1)];

            float v0 = (p2 - p0) * 0.5f;
            float v1 = (p3 - p1) * 0.5f;

            return CalculateInternal(p1, p2, v0, v1, t);
        }

        public static Point Compute(int index, float t, Vector[] points)
        {
            // You must specify 0 for next index if start index is 0.
            // So first index is decremented.
            Vector p0 = points[Clamp(index - 1, 0, points.Length - 1)];
            Vector p1 = points[Clamp(index + 0, 0, points.Length - 1)];
            Vector p2 = points[Clamp(index + 1, 0, points.Length - 1)];
            Vector p3 = points[Clamp(index + 2, 0, points.Length - 1)];

            Vector v0 = (p2 - p0) * 0.5f;
            Vector v1 = (p3 - p1) * 0.5f;

            float x = CalculateInternal((float)p1.X, (float)p2.X, (float)v0.X, (float)v1.X, t);
            float y = CalculateInternal((float)p1.Y, (float)p2.Y, (float)v0.Y, (float)v1.Y, t);

            return new Point(x, y);
        }

        static int Clamp(int value, int min, int max)
        {
            return Math.Max(Math.Min(value, max), min);
        }

        static float CalculateInternal(float p0, float p1, float v0, float v1, float t)
        {
            float t2 = t * t;
            return (2.0f * p0 - 2.0f * p1 + v0 + v1) * t2 * t + (-3.0f * p0 + 3.0f * p1 - 2.0f * v0 - v1) * t2 + v0 * t + p0;
        }
    }
}
