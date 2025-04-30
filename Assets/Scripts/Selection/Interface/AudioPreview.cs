using System.IO;
using System.Threading;
using ArcCreate.Data;
using ArcCreate.Gameplay.Audio;
using ArcCreate.Storage;
using ArcCreate.Storage.Data;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;

namespace ArcCreate.Selection.Interface
{
    public class AudioPreview : MonoBehaviour
    {
        [SerializeField] private StorageData storage;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float audioFadeDuration = 1;
        [SerializeField] private float switchSceneAudioFadeDuration = 1;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private (LevelStorage level, string audioPath) currentlyPlaying;
        private float minPreviewLength = 5;

        public void StopPreview()
        {
            BassAudioService.Instance.PauseAudioPreview();
            // cts.Cancel();
            // cts.Dispose();
            // cts = new CancellationTokenSource();
            //
            // audioSource.DOFade(0, audioFadeDuration).OnComplete(audioSource.Stop);
        }

        public void ResumePreview()
        {
            BassAudioService.Instance.ResumeAudioPreview();
            // var (level, chart) = storage.SelectedChart.Value;
            // PlayPreviewAudio(level, chart, cts.Token).Forget();
        }


        public async UniTask PlayPreviewAudio(LevelStorage level, ChartSettings chart, CancellationToken ct)
        {
            var auPath = Path.Combine(Application.streamingAssetsPath,"songs",level.Identifier,"preview.ogg");
            await BassAudioService.Instance.PlayAudioPreview(auPath);
            // audioSource.Stop();
            // AudioClip clip = await storage.GetAudioClipStreaming(level, chart.AudioPath);
            // if (ct.IsCancellationRequested)
            // {
            //     return;
            // }

            // audioSource.clip = clip;
            //
            //
            // var start = 0;
            //
            // var end = clip.length;
            // end = Mathf.Min(end, Mathf.Max(clip.length, minPreviewLength));
            //
            // float fadeDuration = Mathf.Min(audioFadeDuration, (end - start) / 2);
            // if (fadeDuration < 0)
            // {
            //     return;
            // }
            //
            // while (true)
            // {
            //     audioSource.volume = 0;
            //     audioSource.time = start;
            //     audioSource.Play();
            //     audioSource.DOFade(1, fadeDuration);
            //
            //     while (audioSource.time < end - fadeDuration)
            //     {
            //         await UniTask.NextFrame();
            //         if (ct.IsCancellationRequested)
            //         {
            //             return;
            //         }
            //     }
            //
            //     audioSource.DOFade(0, fadeDuration);
            //
            //     while (audioSource.time < end)
            //     {
            //         await UniTask.NextFrame();
            //         if (ct.IsCancellationRequested)
            //         {
            //             return;
            //         }
            //     }
            //
            //     audioSource.Stop();
            // }
        }

        private void Awake()
        {
            minPreviewLength = Constants.MinPreviewSegmentLengthMs / 1000f;
            storage.SelectedChart.OnValueChange += OnChartChange;
            storage.OnStorageChange += OnStorageChange;
            storage.OnSwitchToGameplayScene += OnSwitchToGameplayScene;

            if (storage.IsLoaded)
            {
                OnStorageChange();
            }
        }

        private void OnDestroy()
        {
            storage.SelectedChart.OnValueChange -= OnChartChange;
            storage.OnStorageChange -= OnStorageChange;
            storage.OnSwitchToGameplayScene -= OnSwitchToGameplayScene;
            cts.Cancel();
            cts.Dispose();
        }

        private void OnSwitchToGameplayScene()
        {
            cts.Cancel();
            cts.Dispose();
            cts = new CancellationTokenSource();
            audioSource.DOFade(0, switchSceneAudioFadeDuration).OnComplete(audioSource.Stop);
        }

        private void OnStorageChange()
        {
            OnChartChange(storage.SelectedChart.Value);
        }

        private void OnChartChange((LevelStorage level, ChartSettings chart) obj)
        {
            var (level, chart) = obj;
            cts.Cancel();
            cts.Dispose();
            cts = new CancellationTokenSource();

            if (!InterfaceUtility.AreTheSame(level, currentlyPlaying.level) || chart.AudioPath != currentlyPlaying.audioPath)
            {
                //var auPath = Path.Combine(Application.streamingAssetsPath,"songs",level.Identifier,"preview.ogg");
                //BassAudioService.Instance.PlayAudioPreview(auPath).Forget();
                PlayPreviewAudio(level, chart, cts.Token).Forget();
            }

            currentlyPlaying = (level, chart.AudioPath);
        }
    }
}