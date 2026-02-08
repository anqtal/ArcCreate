using System.Collections.Generic;
using EmmySharp;
using MoonSharp.Interpreter;

namespace ArcCreate.Gameplay.Scenecontrol
{
    [MoonSharpUserData]
    [EmmyDoc("Class for accessing context value channels")]
    [EmmySingleton]
    public class Context : ISerializableUnit, ISceneController
    {
        private ValueChannel laneFrom = new ConstantChannel(1);
        private ValueChannel laneTo = new ConstantChannel(4);

        public static DropRateChannel DropRate => new();

        public static GlobalOffsetChannel GlobalOffset => new();

        public static CurrentScoreChannel CurrentScore => new();

        public static CurrentComboChannel CurrentCombo => new();

        public static CurrentTimingChannel CurrentTiming => new();

        public static ScreenWidthChannel ScreenWidth => new();

        public static ScreenHeightChannel ScreenHeight => new();

        public static ProductChannel ScreenAspectRatio => ScreenWidth / ScreenHeight;

        public static ScreenIs16By9Channel Is16By9 => new();

        public static IsMirrorOnChannel IsMirrorOn => new();

        public ValueChannel LaneFrom
        {
            get => laneFrom;
            set
            {
                laneFrom = value;
                Services.Scenecontrol.AddReferencedController(this);
            }
        }

        public ValueChannel LaneTo
        {
            get => laneTo;
            set
            {
                laneTo = value;
                Services.Scenecontrol.AddReferencedController(this);
            }
        }

        [MoonSharpHidden] public string SerializedType { get; set; } = "context";

        [MoonSharpHidden]
        public void UpdateController(int timing)
        {
            Values.LaneFrom = laneFrom.ValueAt(timing);
            Values.LaneTo = laneTo.ValueAt(timing);
            var x = Values.LaneFrom;
            var y = Values.LaneTo;
        }

        [MoonSharpHidden]
        public void CleanController()
        {
            laneFrom = new ConstantChannel(1);
            laneTo = new ConstantChannel(4);
            Values.LaneFrom = 1;
            Values.LaneTo = 4;
        }

        [MoonSharpHidden]
        public List<object> SerializeProperties(ScenecontrolSerialization serialization)
        {
            return new List<object>
            {
                serialization.AddUnitAndGetId(LaneFrom),
                serialization.AddUnitAndGetId(LaneTo)
            };
        }

        [MoonSharpHidden]
        public void DeserializeProperties(List<object> properties, EnabledFeatures features,
            ScenecontrolDeserialization deserialization)
        {
            var offset = 0;
            laneFrom = deserialization.GetUnitFromId<ValueChannel>(properties[offset++]);
            laneTo = deserialization.GetUnitFromId<ValueChannel>(properties[offset++]);
        }

        public static ProductChannel BeatLength(int timingGroup = 0)
        {
            return 60000 / Bpm(timingGroup);
        }

        public static BPMChannel Bpm(int timingGroup = 0)
        {
            return new BPMChannel(timingGroup);
        }

        public static DivisorChannel Divisor(int timingGroup = 0)
        {
            return new DivisorChannel(timingGroup);
        }

        public static FloorPositionChannel FloorPosition(int timingGroup = 0)
        {
            return new FloorPositionChannel(timingGroup);
        }
    }
}