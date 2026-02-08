using System;
using System.Collections.Generic;

namespace ArcCreate.Gameplay.Chart
{
    public class BeatlineDisplay
    {
        private readonly Pool<BeatlineBehaviour> beatlinePool;

        private readonly IBeatlineGenerator generator;
        private readonly List<Beatline> previousBeatlinesInRange = new(32);
        private CachedBisect<Beatline, double> floorPositionSearch = new(new List<Beatline>(), x => x.FloorPosition);

        public BeatlineDisplay(IBeatlineGenerator generator, Pool<BeatlineBehaviour> beatlinePool)
        {
            this.generator = generator;
            this.beatlinePool = beatlinePool;
        }

        public List<Beatline> LoadFromTimingGroup(int tgNum, int audioLength)
        {
            beatlinePool.ReturnAll();
            previousBeatlinesInRange.Clear();
            var tg = Services.Chart.GetTimingGroup(tgNum);
            var beatlines = new List<Beatline>(generator.Generate(tg, audioLength));
            floorPositionSearch = new CachedBisect<Beatline, double>(beatlines, x => x.FloorPosition);

            return beatlines;
        }

        public void UpdateBeatlines(double floorPosition)
        {
            if (floorPositionSearch.List.Count == 0) return;

            var fpDistForward = Math.Abs(ArcFormula.ZToFloorPosition(Values.TrackLengthForward));
            var fpDistBackward = Math.Abs(ArcFormula.ZToFloorPosition(Values.TrackLengthBackward));
            var renderFrom = floorPosition - fpDistBackward;
            var renderTo = floorPosition + fpDistForward;

            var renderIndex = floorPositionSearch.Bisect(renderFrom);

            // Disable old notes
            for (var i = 0; i < previousBeatlinesInRange.Count; i++)
            {
                var beatline = previousBeatlinesInRange[i];
                if (beatline.FloorPosition < renderFrom || beatline.FloorPosition > renderTo)
                    beatlinePool.Return(beatline.RevokeInstance());
            }

            previousBeatlinesInRange.Clear();

            // Update notes
            while (renderIndex < floorPositionSearch.List.Count)
            {
                var beatline = floorPositionSearch.List[renderIndex];
                if (beatline.FloorPosition > renderTo) break;

                if (!beatline.IsAssignedInstance) beatline.AssignInstance(beatlinePool.Get());

                beatline.UpdateInstance(floorPosition);
                renderIndex++;
                previousBeatlinesInRange.Add(beatline);
            }
        }
    }
}