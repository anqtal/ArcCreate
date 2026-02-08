using System;
using System.Collections.Generic;
using EmmySharp;
using MoonSharp.Interpreter;

namespace ArcCreate.Gameplay.Scenecontrol
{
    [MoonSharpUserData]
    [EmmyDoc("Text channel that's combined from multiple other text channel")]
    public class ConcatTextChannel : TextChannel
    {
        private char[] charArray = new char[0];
        private List<TextChannel> components;

        public ConcatTextChannel()
        {
        }

        public ConcatTextChannel(TextChannel a, TextChannel b)
        {
            components = new List<TextChannel> { a, b };
            EnsureArraySize();
        }

        public override int MaxLength => charArray.Length;

        [MoonSharpUserDataMetamethod("__concat")]
        public static ConcatTextChannel Concat(ConcatTextChannel concat, TextChannel channel)
        {
            concat.components.Add(channel);
            concat.EnsureArraySize();
            return concat;
        }

        [MoonSharpUserDataMetamethod("__concat")]
        public static ConcatTextChannel Concat(TextChannel channel, ConcatTextChannel concat)
        {
            return Concat(concat, channel);
        }

        public override void DeserializeProperties(List<object> properties, ScenecontrolDeserialization deserialization)
        {
            components = new List<TextChannel>();
            foreach (var prop in properties) components.Add(deserialization.GetUnitFromId<TextChannel>(prop));

            EnsureArraySize();
        }

        public override List<object> SerializeProperties(ScenecontrolSerialization serialization)
        {
            var result = new List<object>();
            foreach (var comp in components) result.Add(serialization.AddUnitAndGetId(comp));

            return result;
        }

        public override char[] ValueAt(int timing, out int length, out bool hasChanged)
        {
            length = 0;
            EnsureArraySize();
            hasChanged = false;
            for (var i = 0; i < components.Count; i++)
            {
                var c = components[i];
                var partial = c.ValueAt(timing, out var partialLength, out var partialHasChanged);
                Array.Copy(partial, 0, charArray, length, partialLength);
                length += partialLength;
                hasChanged = hasChanged || partialHasChanged;
            }

            return charArray;
        }

        private void EnsureArraySize()
        {
            var length = 0;
            foreach (var c in components) length += c.MaxLength;

            if (charArray == null)
            {
                charArray = new char[length];
                return;
            }

            if (length > charArray.Length) Array.Resize(ref charArray, length);
        }
    }
}