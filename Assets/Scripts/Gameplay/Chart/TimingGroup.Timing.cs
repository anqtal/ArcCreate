using System;
using System.Collections.Generic;
using ArcCreate.Gameplay.Data;
using ArcCreate.Utility.Extension;
using UnityEngine;

namespace ArcCreate.Gameplay.Chart
{
    /// <summary>
    ///     Contains methods related to timing events.
    /// </summary>
    public partial class TimingGroup
    {
        public List<TimingEvent> Timings { get; private set; } = new();

        public TimingEvent GetEventAt(int timing)
        {
            var index = Timings.BisectRight(timing, ev => ev.Timing) - 1;
            index = Mathf.Max(index, 0);
            return Timings[index];
        }

        public float GetBpm(int timing)
        {
            return GetEventAt(timing).Bpm;
        }

        public float GetCurrentBpm()
        {
            return GetBpm(Services.Audio.ChartTiming);
        }

        public double GetFloorPosition(int timing)
        {
            var note = GetEventAt(timing);
            var baseFloorPosition = note.FloorPosition;
            return baseFloorPosition + (double)note.Bpm * (timing - note.Timing);
        }

        public float GetDivisor(int timing)
        {
            return GetEventAt(timing).Divisor;
        }

        /// <summary>
        ///     Reverses the z world position to the timing value (relative to current chart timing).
        /// </summary>
        /// <param name="z">The z position.</param>
        /// <returns>The timing value corresponding to the value.</returns>
        public int GetTimingFromZPosition(float z)
        {
            var fp = ArcFormula.ZToFloorPosition(z);
            var currentFp = GetFloorPosition(Services.Audio.ChartTiming);
            return GetTimingFromFloorPosition(fp + currentFp);
        }

        /// <summary>
        ///     Get the timing value corresponding to a floor position value.
        ///     In the case that there are multiple valid timing values, the one closest to the current audio timing will be
        ///     returned.
        /// </summary>
        /// <param name="fp">The floor position value.</param>
        /// <returns>The timing value.</returns>
        public int GetTimingFromFloorPosition(double fp)
        {
            var length = Timings.Count;
            var first = Timings[0];

            var timing = Services.Audio.ChartTiming;
            var closestMatch = 0;
            var closestDiff = int.MaxValue;

            for (var i = 0; i < length - 1; i++)
            {
                var curr = Timings[i];
                var next = Timings[i + 1];

                // Floor position sandwiched between two timing events
                if ((curr.FloorPosition <= fp && next.FloorPosition > fp)
                    || (curr.FloorPosition >= fp && next.FloorPosition < fp))
                {
                    var val = (int)(Math.Round((fp - curr.FloorPosition) / curr.Bpm) + curr.Timing);
                    var diff = Mathf.Abs(val - timing);
                    if (diff < closestDiff)
                    {
                        closestDiff = diff;
                        closestMatch = val;
                    }
                }
            }

            var last = Timings[length - 1];
            {
                if ((last.FloorPosition <= fp && last.Bpm > 0)
                    || (last.FloorPosition >= fp && last.Bpm < 0))
                {
                    var val = (int)Math.Round((fp - last.FloorPosition) / last.Bpm + last.Timing);
                    var diff = Mathf.Abs(val - timing);
                    if (diff < closestDiff)
                    {
                        closestDiff = diff;
                        closestMatch = val;
                    }
                }
            }

            return Mathf.Clamp(closestMatch, 0, Services.Audio.AudioLength);
        }

        /// <summary>
        ///     Get the floor position relative to the current audio timing.
        /// </summary>
        /// <param name="timing">The timing to calculate from.</param>
        /// <returns>The floor position.</returns>
        public double GetFloorPositionFromCurrent(int timing)
        {
            var fp = GetFloorPosition(timing);
            var curfp = GetFloorPosition(Services.Audio.ChartTiming);
            return fp - curfp;
        }

        private void AddTimings(IEnumerable<TimingEvent> timings)
        {
            this.Timings.AddRange(timings);
            OnTimingListChange();
        }

        private void RemoveTimings(IEnumerable<TimingEvent> timings)
        {
            var exclusion = new HashSet<TimingEvent>(timings);
            this.Timings.RemoveAll(t => exclusion.Contains(t));
            OnTimingListChange();
        }

        private void UpdateTimings()
        {
            OnTimingListChange();
        }

        private void OnTimingListChange()
        {
            Sort();
            RecalculateFloorPosition();
            RecalculateNoteFloorPosition();
            if (GroupNumber == 0) Services.Chart.ReloadBeatline();
        }

        private void Sort()
        {
            Timings.Sort((a, b) => a.Timing.CompareTo(b.Timing));
        }

        private void RecalculateFloorPosition()
        {
            double floorPosition = 0;
            for (var i = 0; i < Timings.Count - 1; i++)
            {
                var curr = Timings[i];
                var next = Timings[i + 1];
                curr.FloorPosition = floorPosition;
                floorPosition += (double)curr.Bpm * (next.Timing - curr.Timing);
            }

            Timings[Timings.Count - 1].FloorPosition = floorPosition;
        }

        private void RecalculateNoteFloorPosition()
        {
            taps.Notes.ForEach(n => { n.Rebuild(); });
            taps.RebuildList();

            arcTaps.Notes.ForEach(n => { n.Rebuild(); });
            arcTaps.RebuildList();

            holds.Notes.ForEach(n => { n.Rebuild(); });
            holds.RebuildList();

            arcs.Notes.ForEach(n => { n.Rebuild(); });
            arcs.RebuildList();
        }

        private IEnumerable<TimingEvent> FindTimingEventsByTiming(int from, int to)
        {
            var i = Timings.BisectLeft(from, n => n.Timing);

            while (i >= 0 && i < Timings.Count && Timings[i].Timing >= from && Timings[i].Timing <= to)
            {
                yield return Timings[i];
                i++;
            }
        }

        private IEnumerable<TimingEvent> FindTimingEventsWithinRange(int from, int to)
        {
            var fromI = Timings.BisectLeft(from, n => n.Timing);
            var toI = Timings.BisectRight(to, n => n.Timing);

            for (var i = fromI; i <= toI; i++) yield return Timings[i];
        }
    }
}