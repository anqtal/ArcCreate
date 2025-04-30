using System.Collections.Generic;

namespace ArcCreate.Gameplay.Scenecontrol
{
    internal class ScenecontrolVersioning : ISerializableUnit
    {
        public ScenecontrolVersioning(EnabledFeatures features)
        {
            this.Features = features;
        }

        public EnabledFeatures Features { get; private set; }

        public void DeserializeProperties(List<object> properties, EnabledFeatures features,
            ScenecontrolDeserialization deserialization)
        {
            this.Features = (EnabledFeatures)(long)properties[0];
        }

        public List<object> SerializeProperties(ScenecontrolSerialization serialization)
        {
            return new List<object> { (long)Features };
        }
    }
}