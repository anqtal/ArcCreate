using System.Collections.Generic;
using EmmySharp;
using MoonSharp.Interpreter;

namespace ArcCreate.Gameplay.Scenecontrol
{
    [MoonSharpUserData]
    [EmmyDoc("Channel defining a string value at any given input timing value")]
    public abstract class StringChannel : ISerializableUnit
    {
        [MoonSharpHidden]
        public abstract List<object> SerializeProperties(ScenecontrolSerialization serialization);

        void ISerializableUnit.DeserializeProperties(List<object> properties, EnabledFeatures features,
            ScenecontrolDeserialization deserialization)
        {
            DeserializeProperties(properties, deserialization);
        }

        public abstract string ValueAt(int timing);

        [MoonSharpHidden]
        public abstract void DeserializeProperties(List<object> properties,
            ScenecontrolDeserialization deserialization);
    }
}