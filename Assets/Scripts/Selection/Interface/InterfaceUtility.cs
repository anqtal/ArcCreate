using ArcCreate.Storage;
using UnityEngine;

namespace ArcCreate.Selection.Interface
{
    public static class InterfaceUtility
    {
        public const float HueShift = 0f;
        public const float SatShift = -0.01f;
        public const float ValueShift = 0.35f;

        public static string AlignedDiffNumber(string number)
        {
            if (string.IsNullOrEmpty(number)) return string.Empty;

            var end = number[number.Length - 1];
            if (end == '+' || end == '-') return ' ' + number;

            return number;
        }

        public static Color LightenDiffColor(Color color)
        {
            Color.RGBToHSV(color, out var h, out var s, out var v);
            var rgb = Color.HSVToRGB(h + HueShift, s + SatShift, v + ValueShift);
            rgb.a = color.a;
            return rgb;
        }

        public static bool AreTheSame(SongList a, SongList b)
        {
            if (a == null || b == null) return false;

            if (!string.IsNullOrEmpty(a.id) && !string.IsNullOrEmpty(b.id)) return a.id == b.id;

            return a.idx == b.idx;
        }
    }
}