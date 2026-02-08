using System.Collections.Generic;
using EmmySharp;
using MoonSharp.Interpreter;

namespace ArcCreate.Gameplay.Scenecontrol
{
    [MoonSharpUserData]
    [EmmyDoc("A generic channel that defines a value for a given input timing value")]
    public abstract class ValueChannel : ISerializableUnit
    {
        [MoonSharpHidden] public static ValueChannel ConstantZeroChannel { get; } = new ConstantChannel(0);

        [MoonSharpHidden] public static ValueChannel ConstantOneChannel { get; } = new ConstantChannel(1);

        [EmmyDoc("The name of the channel")] public string Name { get; set; }

        [MoonSharpHidden]
        public abstract List<object> SerializeProperties(ScenecontrolSerialization serialization);

        void ISerializableUnit.DeserializeProperties(List<object> properties, EnabledFeatures features,
            ScenecontrolDeserialization deserialization)
        {
            DeserializeProperties(properties, deserialization);
        }

        public static ValueChannel operator +(ValueChannel a)
        {
            return a;
        }

        public static NegateChannel operator -(ValueChannel a)
        {
            return new NegateChannel(a);
        }

        public static SumChannel operator +(ValueChannel a, ValueChannel b)
        {
            return new SumChannel(a, b);
        }

        public static SumChannel operator +(ValueChannel a, float b)
        {
            return a + new ConstantChannel(b);
        }

        public static SumChannel operator +(float b, ValueChannel a)
        {
            return a + new ConstantChannel(b);
        }

        public static SumChannel operator -(ValueChannel a, ValueChannel b)
        {
            return new SumChannel(a, -b);
        }

        public static SumChannel operator -(ValueChannel a, float b)
        {
            return a + new ConstantChannel(-b);
        }

        public static SumChannel operator -(float b, ValueChannel a)
        {
            return -a + new ConstantChannel(b);
        }

        public static ProductChannel operator *(ValueChannel a, float b)
        {
            return a * new ConstantChannel(b);
        }

        public static ProductChannel operator *(float b, ValueChannel a)
        {
            return a * new ConstantChannel(b);
        }

        public static ProductChannel operator *(ValueChannel a, ValueChannel b)
        {
            return new ProductChannel(a, b);
        }

        public static ProductChannel operator /(ValueChannel a, float b)
        {
            return a * new ConstantChannel(1 / b);
        }

        public static ProductChannel operator /(float b, ValueChannel a)
        {
            return new ConstantChannel(b) * new InverseChannel(a);
        }

        public static ProductChannel operator /(ValueChannel a, ValueChannel b)
        {
            return a * new InverseChannel(b);
        }

        [EmmyDoc("Gets the value of this channel at the provided timing point")]
        public abstract float ValueAt(int timing);

        [MoonSharpHidden]
        public virtual void Reset()
        {
        }

        [MoonSharpHidden]
        public virtual void Destroy()
        {
        }

        [MoonSharpHidden]
        public abstract void DeserializeProperties(List<object> properties,
            ScenecontrolDeserialization deserialization);

        [EmmyDoc("Find a channel with the provided name in the components of this channel")]
        public ValueChannel Find(string name)
        {
            if (Name == name) return this;

            foreach (var child in GetChildrenChannels())
            {
                var channel = child.Find(name);
                if (channel != null) return channel;
            }

            return null;
        }

        protected abstract IEnumerable<ValueChannel> GetChildrenChannels();
    }
}