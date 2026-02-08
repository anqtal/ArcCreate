using System;
using System.Collections.Generic;
using ArcCreate.Gameplay.Data;
using ArcCreate.Utility.RangeTree;

namespace ArcCreate.Gameplay.Chart
{
    /// <summary>
    ///     Base class for arcs and holds note groups.
    /// </summary>
    /// <typeparam name="Note">The note type.</typeparam>
    public abstract class LongNoteGroup<Note> : NoteGroup<Note>
        where Note : ArcEvent, ILongNote
    {
        protected RangeTree<Note> TimingTree { get; } = new();

        protected RangeTree<Note> FloorPositionTree { get; } = new();

        protected List<Note> LastRenderingNotes { get; } = new();

        public override void UpdateJudgement(int timing, double floorPosition, GroupProperties groupProperties)
        {
            if (Notes.Count == 0 || groupProperties.NoInput) return;

            var judgeFrom = timing - Values.MissJudgeWindow;
            var judgeTo = timing + Values.HoldMissLateJudgeWindow;
            var notesInRange = TimingTree[judgeFrom, judgeTo];

            var i = 0;
            while (notesInRange.MoveNext())
            {
                var note = notesInRange.Current;
                note.UpdateJudgement(timing, groupProperties);
                i++;
            }
        }

        public override void UpdateRender(int timing, double floorPosition, GroupProperties groupProperties)
        {
            LastRenderingNotes.Clear();
            if (Notes.Count == 0 || !groupProperties.Visible) return;

            var fpDistForward = Math.Abs(ArcFormula.ZToFloorPosition(Values.TrackLengthForward));
            var fpDistBackward = Math.Abs(ArcFormula.ZToFloorPosition(Values.TrackLengthBackward));
            var renderFrom =
                groupProperties.NoInput && !groupProperties.NoClip ? floorPosition : floorPosition - fpDistBackward;
            var renderTo = floorPosition + fpDistForward;

            var notesInRange = FloorPositionTree[renderFrom, renderTo];

            // Update notes
            while (notesInRange.MoveNext())
            {
                var note = notesInRange.Current;
                LastRenderingNotes.Add(note);
                note.UpdateRender(timing, floorPosition, groupProperties);
            }
        }

        public override int ComboAt(int timing)
        {
            var notes = TimingTree[int.MinValue, timing];
            var combo = 0;

            while (notes.MoveNext())
            {
                var note = notes.Current;
                combo += note.ComboAt(timing);
            }

            return combo;
        }

        public override void RebuildList()
        {
            TimingTree.Clear();
            FloorPositionTree.Clear();

            for (var i = 0; i < Notes.Count; i++)
            {
                var note = Notes[i];
                TimingTree.AddSilent(note.Timing, note.EndTiming, note);

                var fpStart = Math.Min(note.FloorPosition, note.EndFloorPosition);
                var fpEnd = Math.Max(note.FloorPosition, note.EndFloorPosition);
                FloorPositionTree.AddSilent(fpStart, fpEnd, note);
            }

            TimingTree.Rebuild();
            FloorPositionTree.Rebuild();
        }

        public override void UpdateList()
        {
            for (var i = TimingTree.Items.Count - 1; i >= 0; i--)
            {
                var pair = TimingTree.Items[i];
                var note = pair.Value;
                if (pair.From != note.Timing || pair.To != note.EndTiming)
                {
                    TimingTree.RemoveAt(i);
                    TimingTree.Add(note.Timing, note.EndTiming, note);
                }
            }

            for (var i = FloorPositionTree.Items.Count - 1; i >= 0; i--)
            {
                var pair = FloorPositionTree.Items[i];
                var note = pair.Value;
                var fpStart = Math.Min(note.FloorPosition, note.EndFloorPosition);
                var fpEnd = Math.Max(note.FloorPosition, note.EndFloorPosition);
                if (pair.From != fpStart || pair.To != fpEnd)
                {
                    FloorPositionTree.RemoveAt(i);
                    FloorPositionTree.Add(fpStart, fpEnd, note);
                }
            }
        }

        public override IEnumerable<Note> FindByTiming(int from, int to)
        {
            var overlap = TimingTree[from, to];
            while (overlap.MoveNext())
            {
                var note = overlap.Current;
                if (note.Timing >= from && note.Timing <= to) yield return note;
            }
        }

        public IEnumerable<Note> FindByEndTiming(int from, int to)
        {
            var overlap = TimingTree[from, to];
            while (overlap.MoveNext())
            {
                var note = overlap.Current;
                if (note.EndTiming >= from && note.EndTiming <= to) yield return note;
            }
        }

        public override IEnumerable<Note> FindEventsWithinRange(int from, int to, bool overlapCompletely = true)
        {
            var overlap = TimingTree[from, to];
            while (overlap.MoveNext())
            {
                var note = overlap.Current;
                if ((overlapCompletely && note.Timing >= from && note.EndTiming <= to)
                    || (!overlapCompletely && note.Timing <= to && note.EndTiming >= from))
                    yield return note;
            }
        }

        public override IEnumerable<Note> GetRenderingNotes()
        {
            return LastRenderingNotes;
        }
    }
}