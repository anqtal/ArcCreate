using UnityEngine;

namespace ArcCreate.Gameplay.Scenecontrol
{
    public class StringKey
    {
        public int Timing { get; set; }

        public string Value { get; set; }

        public int OverrideIndex { get; set; } = 0;

        public void Deserialize(string str)
        {
            var i = str.IndexOf(',');
            var timingString = str.Substring(0, i);
            var valueString = str.Substring(i + 1);
            Timing = Mathf.RoundToInt(float.Parse(timingString));
            Value = valueString;
        }

        public object Serialize()
        {
            return $"{Timing},{Value}";
        }
    }
}