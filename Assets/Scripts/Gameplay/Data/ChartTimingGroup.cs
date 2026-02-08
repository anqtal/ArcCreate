using System.Collections.Generic;
using ArcCreate.ChartFormat;

namespace ArcCreate.Gameplay.Data
{
    public class ChartTimingGroup
    {
        public RawTimingGroup Properties { get; set; }

        public List<Tap> Taps { get; set; } = new();

        public List<Hold> Holds { get; set; } = new();

        public List<Arc> Arcs { get; set; } = new();

        public List<ArcTap> ArcTaps { get; set; } = new();

        public List<TimingEvent> Timings { get; set; } = new();

        public List<ArcEvent> ReferenceEvents { get; set; } = new();
    }
}