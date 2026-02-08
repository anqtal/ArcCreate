using ArcCreate.Gameplay.Data;

namespace ArcCreate.Gameplay.Chart
{
    public class TapNoteGroup : ShortNoteGroup<Tap>
    {
        public override void SetupNotes()
        {
            for (var i = 0; i < Notes.Count; i++)
            {
                var tap = Notes[i];
                SetupConnection(tap);
            }
        }

        protected override void OnAdd(Tap note)
        {
            SetupConnection(note);
        }

        protected override void OnUpdate(Tap note)
        {
            SetupConnection(note);
        }

        protected override void OnRemove(Tap note)
        {
            RemoveConnection(note);
        }

        private void SetupConnection(Tap note)
        {
            RemoveConnection(note);

            var connectedArcTaps
                = Services.Chart.FindByTiming<ArcTap>(note.Timing - 1, note.Timing + 1);

            foreach (var arcTap in connectedArcTaps)
            {
                note.ConnectedArcTaps.Add(arcTap);
                arcTap.ConnectedTaps.Add(note);
            }

            note.Rebuild();
        }

        private void RemoveConnection(Tap note)
        {
            foreach (var arcTap in note.ConnectedArcTaps) arcTap.ConnectedTaps.Remove(note);

            note.ConnectedArcTaps.Clear();
        }
    }
}