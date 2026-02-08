using EmmySharp;
using MoonSharp.Interpreter;
using UnityEngine;

namespace ArcCreate.Utility.Lua
{
    [MoonSharpUserData]
    [EmmySingleton]
    [EmmyDoc("Utility class for converting between types")]
    public class Convert
    {
        [EmmyAlias("RGBAToHex")]
        public static string RGBAToHex(float r, float g, float b, float a)
        {
            return "#" + ((int)(r * 255)).ToString("X2") + ((int)(g * 255)).ToString("X2") +
                   ((int)(b * 255)).ToString("X2") + ((int)(a * 255)).ToString("X2");
        }

        [EmmyAlias("RGBAToHex")]
        public static string RGBAToHex(RGBA rgba)
        {
            return RGBAToHex(rgba.R, rgba.G, rgba.B, rgba.A);
        }

        [EmmyAlias("HSVAToHex")]
        public static string HSVAToHex(HSVA hsva)
        {
            return RGBAToHex(HSVAToRGBA(hsva));
        }

        [EmmyAlias("HexToRGBA")]
        public static RGBA HexToRGBA(string hex)
        {
            var converted = ColorUtility.TryParseHtmlString(hex, out var color);
            if (!converted) return default;

            return new RGBA(color);
        }

        [EmmyAlias("HexToHSVA")]
        public static HSVA HexToHSVA(string hex)
        {
            return RGBAToHSVA(HexToRGBA(hex));
        }

        [EmmyAlias("RGBAToHSVA")]
        public static HSVA RGBAToHSVA(float r, float g, float b, float a)
        {
            var c = new RGBA(r, g, b, a).ToColor();
            Color.RGBToHSV(c, out var h, out var s, out var v);
            var hsva = new HSVA(h * 360, s, v, a / 255);
            return hsva;
        }

        [EmmyAlias("RGBAToHSVA")]
        public static HSVA RGBAToHSVA(RGBA rgba)
        {
            return RGBAToHSVA(rgba.R, rgba.G, rgba.B, rgba.A);
        }

        [EmmyAlias("HSVAToRGBA")]
        public static RGBA HSVAToRGBA(float h, float s, float v, float a)
        {
            var rgb = Color.HSVToRGB(h / 360, s, v);
            var rgba = new RGBA(rgb) { A = a * 255 };
            return rgba;
        }

        [EmmyAlias("HSVAToRGBA")]
        public static RGBA HSVAToRGBA(HSVA hsva)
        {
            return HSVAToRGBA(hsva.H, hsva.S, hsva.V, hsva.A);
        }
    }
}