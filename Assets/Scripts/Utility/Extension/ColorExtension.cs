using UnityEngine;

namespace ArcCreate.Utility.Extension
{
    public static class ColorExtension
    {
        public static bool ConvertHexToColor(this string str, out Color color)
        {
            if (ColorUtility.TryParseHtmlString(str, out var c))
            {
                color = c;
                return true;
            }

            color = Color.black;
            return false;
        }

        public static string ConvertToHexCode(this Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGBA(color);
        }
    }
}