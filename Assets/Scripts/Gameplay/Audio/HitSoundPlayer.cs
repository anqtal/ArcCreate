using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ArcCreate.Gameplay.Audio
{
    public class HitSoundPlayer
    {
        // filenames
        private const string TapHitSoundPath = "tap.wav";
        private const string ArcHitSoundPath = "arc.wav";
        private BassStream arcHitSoundStream;

        // audio streams
        private BassStream tapHitSoundStream;

        public async UniTask LoadAudioStreamAsync()
        {
            var tapHitSoundFilePath = Path.Combine(Application.streamingAssetsPath, "audio", TapHitSoundPath);
            var arcHitSoundFilePath = Path.Combine(Application.streamingAssetsPath, "audio", ArcHitSoundPath);
            var tapHitSoundFileBytes = await BassAudioService.Instance.ReadFileAsync(tapHitSoundFilePath);
            var arcHitSoundFileBytes = await BassAudioService.Instance.ReadFileAsync(arcHitSoundFilePath);
            tapHitSoundStream = new BassStream(tapHitSoundFileBytes);
            arcHitSoundStream = new BassStream(arcHitSoundFileBytes);
        }

        public void PlayTapHitSound()
        {
            tapHitSoundStream.Play();
        }

        public void PlayArcHitSound()
        {
            arcHitSoundStream.Play();
        }

        public void UnloadAudioStream()
        {
            tapHitSoundStream?.Dispose();
            arcHitSoundStream?.Dispose();
        }
    }
}