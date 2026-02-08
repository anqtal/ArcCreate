using System.Diagnostics;

namespace ArcCreate.Gameplay.Audio
{
    public class ChartTimer
    {
        private readonly int delay;
        private readonly Stopwatch stopwatch;

        public ChartTimer(int delay)
        {
            this.delay = delay;
            stopwatch = new Stopwatch();
        }

        public ChartTimer StartTiming()
        {
            stopwatch.Start();
            return this;
        }

        public int GetElapsedMilliseconds()
        {
            return (int)stopwatch.ElapsedMilliseconds - delay;
        }

        public ChartTimer StopTiming()
        {
            if (stopwatch is { IsRunning: true }) stopwatch.Stop();

            return this;
        }

        public ChartTimer ResetTiming()
        {
            stopwatch?.Reset();
            return this;
        }
    }
}