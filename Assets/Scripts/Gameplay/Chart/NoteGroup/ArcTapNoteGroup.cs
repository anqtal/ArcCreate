using ArcCreate.Gameplay.Data;

namespace ArcCreate.Gameplay.Chart
{
    public class ArcTapNoteGroup : ShortNoteGroup<ArcTap>
    {
        public override void SetupNotes()
        {
            for (var i = 0; i < Notes.Count; i++)
            {
                var arcTap = Notes[i];
                SetupConnection(arcTap);
            }
        }

        protected override void OnAdd(ArcTap note)
        {
            SetupConnection(note);
        }

        protected override void OnUpdate(ArcTap note)
        {
            SetupConnection(note);
        }

        protected override void OnRemove(ArcTap note)
        {
            RemoveConnection(note);
        }

        private void SetupConnection(ArcTap note)
        {
            RemoveConnection(note);

            var connectedTaps
                = Services.Chart.FindByTiming<Tap>(note.Timing - 1, note.Timing + 1);

            foreach (var tap in connectedTaps)
            {
                note.ConnectedTaps.Add(tap);
                tap.ConnectedArcTaps.Add(note);
                tap.Rebuild();
            }
        }

        private void RemoveConnection(ArcTap note)
        {
            foreach (var tap in note.ConnectedTaps)
            {
                tap.ConnectedArcTaps.Remove(note);
                tap.Rebuild();
            }

            note.ConnectedTaps.Clear();
        }
    }
}