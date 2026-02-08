using UnityEngine;

namespace ArcCreate.Utility.Extension
{
    public static class MathfExtension
    {
        public static float EvaluateEllipse(float degree, float a, float b)
        {
            var cos = Mathf.Cos(degree * Mathf.Deg2Rad);
            var sin = Mathf.Sin(degree * Mathf.Deg2Rad);
            return Mathf.Abs(a * b / Mathf.Sqrt(b * b * cos * cos + a * a * sin * sin));
        }
    }
}