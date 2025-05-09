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
        // [SerializeField] private AudioSource audioSource;
        // [SerializeField] private float audioFadeDuration = 1;
        // [SerializeField] private float switchSceneAudioFadeDuration = 1;
        //private CancellationTokenSource cts = new CancellationTokenSource();
        private (LevelStorage level, string audioPath) currentlyPlaying;
        //private float minPreviewLength = 5;
        //
        // public static void StopPreview()
        // {
        //     BassAudioService.Instance.AudioPreviewStream?.Pause();
        // }

        // public static void ResumePreview()
        // {
        //     BassAudioService.Instance.AudioPreviewStream?.Resume();
        // }


        // private static async UniTask PlayPreviewAudio(LevelStorage level, ChartSettings chart, CancellationToken ct)
        // {
        //     
        // }

        private void Awake()
        {
            storage.SelectedChart.OnValueChange += OnChartChange;
            storage.OnStorageChange += OnStorageChange;
            //storage.OnSwitchToGameplayScene += OnSwitchToGameplayScene;

            if (storage.IsLoaded)
            {
                OnStorageChange();
            }
        }

        private void OnDestroy()
        {
            storage.SelectedChart.OnValueChange -= OnChartChange;
            storage.OnStorageChange -= OnStorageChange;
            // storage.OnSwitchToGameplayScene -= OnSwitchToGameplayScene;
            // cts.Cancel();
            // cts.Dispose();
        }

        // private void OnSwitchToGameplayScene()
        // {
        //     // cts.Cancel();
        //     // cts.Dispose();
        //     // cts = new CancellationTokenSource();
        //     audioSource.DOFade(0, switchSceneAudioFadeDuration).OnComplete(audioSource.Stop);
        // }

        private void OnStorageChange()
        {
            OnChartChange(storage.SelectedChart.Value);
        }

        private void OnChartChange((LevelStorage level, ChartSettings chart) obj)
        {
            var (level, chart) = obj;
            // cts.Cancel();
            // cts.Dispose();
            // cts = new CancellationTokenSource();

            if (!InterfaceUtility.AreTheSame(level, currentlyPlaying.level) || chart.AudioPath != currentlyPlaying.audioPath)
            {
                var audioPath = Path.Combine(Application.streamingAssetsPath,"songs",level.Identifier,"preview.ogg");
                BassAudioService.Instance.PlayAudioPreview(audioPath).Forget();
            }

            currentlyPlaying = (level, chart.AudioPath);
        }
    }
}