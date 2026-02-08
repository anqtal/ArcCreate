using System.Collections.Generic;
using EmmySharp;
using MoonSharp.Interpreter;

namespace ArcCreate.Gameplay.Scenecontrol
{
    [MoonSharpUserData]
    [EmmyDoc("Channel whose string value is defined by keyframes")]
    public class KeyStringChannel : StringChannel, IComparer<StringKey>
    {
        private readonly List<StringKey> keys;
        private readonly CachedBinarySearch<StringKey, int> keySearch;

        public KeyStringChannel()
        {
            keySearch = new CachedBinarySearch<StringKey, int>(new List<StringKey>(), k => k.Timing, this);
            keys = keySearch.List;
        }

        public int KeyCount => keys.Count;

        [MoonSharpHidden]
        public int Compare(StringKey x, StringKey y)
        {
            if (x.Timing == y.Timing) return x.OverrideIndex.CompareTo(y.OverrideIndex);

            return x.Timing.CompareTo(y.Timing);
        }

        public override string ValueAt(int timing)
        {
            if (keys.Count == 0) return "";

            if (keys.Count == 1) return keys[0].Value;

            if (timing <= keys[0].Timing) return keys[0].Value;

            if (timing >= keys[keys.Count - 1].Timing) return keys[keys.Count - 1].Value;

            var index = keySearch.Search(timing);
            return keys[index].Value;
        }

        [EmmyDoc("Add a keyframe to this channel")]
        public KeyStringChannel AddKey(int timing, string value)
        {
            var overrideIndex = 0;
            if (keys.Count > 0 && keys[keySearch.Search(timing)].Timing == timing) overrideIndex += 1;

            keys.Add(new StringKey
            {
                Timing = timing,
                Value = value,
                OverrideIndex = overrideIndex
            });

            keySearch.Sort();
            return this;
        }

        [EmmyDoc("Remove the first key that has matching timing value")]
        public KeyStringChannel RemoveKeyAtTiming(int timing)
        {
            var index = keySearch.Search(timing);
            if (keys[index].Timing == timing) keys.RemoveAt(index);

            keySearch.Sort();

            return this;
        }

        public override List<object> SerializeProperties(ScenecontrolSerialization serialization)
        {
            var result = new List<object>(keys.Count);

            foreach (var key in keys) result.Add(key.Serialize());

            return result;
        }

        public override void DeserializeProperties(List<object> properties, ScenecontrolDeserialization deserialization)
        {
            keys.Clear();
            for (var i = 0; i < properties.Count; i++)
            {
                var obj = properties[i];
                var str = obj as string;
                var key = new StringKey();
                key.Deserialize(str);
                keys.Add(key);
            }

            keySearch.Sort();
        }
    }
}