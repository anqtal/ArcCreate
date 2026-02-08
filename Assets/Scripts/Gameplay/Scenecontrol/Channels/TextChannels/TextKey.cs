using System;
using System.Text;
using UnityEngine;

namespace ArcCreate.Gameplay.Scenecontrol
{
    public class TextKey
    {
        public int Timing { get; set; }

        public char[] Value { get; set; }

        public string EasingString { get; set; }

        public Func<float, float, float, float> Easing { get; set; }

        public int TransitionFrom { get; set; }

        public int OverrideIndex { get; set; } = 0;

        public void Deserialize(string str)
        {
            var i = str.IndexOf(',');
            var k = str.LastIndexOf(',');
            var j = str.LastIndexOf(',', k - 1);
            var timingString = str.Substring(0, i);
            var valueString = str.Substring(i + 1, j - i - 1);
            var fromString = str.Substring(j + 1, k - j - 1);
            var easingString = str.Substring(k + 1);
            Timing = Mathf.RoundToInt(float.Parse(timingString));
            Value = valueString.ToCharArray();
            TransitionFrom = Mathf.RoundToInt(float.Parse(fromString));
            EasingString = easingString;
        }

        public object Serialize()
        {
            var str = new StringBuilder();
            for (var i = 0; i < Value.Length; i++) str.Append(Value[i]);

            return $"{Timing},{str},{TransitionFrom},{EasingString}";
        }
    }
}