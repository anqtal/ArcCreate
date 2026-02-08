using System;
using System.Collections.Generic;
using ArcCreate.Gameplay.Data;
using ArcCreate.Utility.Extension;

namespace ArcCreate.Gameplay.Chart
{
    /// <summary>
    ///     Base class for taps and arc taps note groups.
    /// </summary>
    /// <typeparam name="Note">The note type.</typeparam>
    public abstract class ShortNoteGroup<Note> : NoteGroup<Note>
        where Note : ArcEvent, INote
    {
        private readonly List<Note> lastRenderingNotes = new();
        private CachedBisect<Note, double> floorPositionSearch;
        private CachedBisect<Note, int> timingSearch;

        public override int ComboAt(int timing)
        {
            return timingSearch.List.BisectRight(timing, note => note.Timing);
        }

        public override void UpdateJudgement(int timing, double floorPosition, GroupProperties groupProperties)
        {
            if (Notes.Count == 0 || groupProperties.NoInput) return;

            var judgeFrom = timing - Values.MissJudgeWindow;
            var judgeTo = timing + Values.MissJudgeWindow;
            var judgeIndex = timingSearch.Bisect(judgeFrom);
            while (judgeIndex < timingSearch.List.Count)
            {
                var note = timingSearch.List[judgeIndex];
                if (note.Timing > judgeTo) break;
                timingSearch.List[judgeIndex].UpdateJudgement(timing, groupProperties);
                judgeIndex++;
            }
        }

        public override void UpdateRender(int timing, double floorPosition, GroupProperties groupProperties)
        {
            lastRenderingNotes.Clear();
            if (Notes.Count == 0 || !groupProperties.Visible) return;

            var fpDistForward = Math.Abs(ArcFormula.ZToFloorPosition(Values.TrackLengthForward));
            var fpDistBackward = Math.Abs(ArcFormula.ZToFloorPosition(Values.TrackLengthBackward));
            var renderFrom =
                groupProperties.NoInput && !groupProperties.NoClip ? floorPosition : floorPosition - fpDistBackward;
            var renderTo = floorPosition + fpDistForward;

            var renderIndex = floorPositionSearch.Bisect(renderFrom);

            // Update notes
            while (renderIndex < floorPositionSearch.List.Count)
            {
                var note = floorPositionSearch.List[renderIndex];
                if (note.FloorPosition > renderTo) break;
                note.UpdateRender(timing, floorPosition, groupProperties);
                lastRenderingNotes.Add(note);
                renderIndex++;
            }
        }

        public override void RebuildList()
        {
            timingSearch = new CachedBisect<Note, int>(Notes, note => note.Timing);
            floorPositionSearch = new CachedBisect<Note, double>(Notes, note => note.FloorPosition);
        }

        public override void UpdateList()
        {
            timingSearch.Sort();
            floorPositionSearch.Sort();
        }

        public override IEnumerable<Note> FindByTiming(int from, int to)
        {
            // Avoid modifying the cache of search tree.
            if (Notes.Count == 0) yield break;

            var i = timingSearch.List.BisectLeft(from, n => n.Timing);

            while (i >= 0 && i < timingSearch.List.Count && timingSearch.List[i].Timing >= from &&
                   timingSearch.List[i].Timing <= to)
            {
                yield return timingSearch.List[i];
                i++;
            }
        }

        public override IEnumerable<Note> FindEventsWithinRange(int from, int to, bool overlapCompletely = true)
        {
            return FindByTiming(from, to);
        }

        public override IEnumerable<Note> GetRenderingNotes()
        {
            return lastRenderingNotes;
        }
    }
}