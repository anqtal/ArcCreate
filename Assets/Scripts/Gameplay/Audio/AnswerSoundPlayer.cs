using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace ArcCreate.Gameplay.Audio
{
    public class AnswerSoundPlayer
    {
        private readonly List<int> notes;

        public AnswerSoundPlayer(HashSet<int> notes)
        {
            this.notes = notes.OrderBy(n => n).ToList();
        }

        private static bool IsPaused => !BassAudioService.Instance.AudioStream.IsPlaying;

        public async UniTask PlayAllAnswerSoundsAsync()
        {
            var timingQueue = new Queue<int>(notes);
            while (timingQueue.Count > 0)
            {
                if (IsPaused)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update);
                    continue;
                }

                var nextTiming = timingQueue.Peek();
                var currentTiming = BassAudioService.Instance.MyChartTimer.GetElapsedMilliseconds();

                if (currentTiming >= nextTiming)
                    //BassAudioService.Instance.answerStream.Play();
                    timingQueue.Dequeue();
                else
                    await UniTask.Yield(PlayerLoopTiming.Update);
            }
        }
    }
}