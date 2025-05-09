using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using ManagedBass;
using UnityEngine;
using UnityEngine.Networking;

namespace ArcCreate.Gameplay.Audio
{
    public sealed class BassAudioService : IDisposable
    {
        private static readonly Lazy<BassAudioService> _instance = new(() => new BassAudioService());

        public static BassAudioService Instance => _instance.Value;

        private bool initialized = false;

        // Audio Path
        private const string ClockPath = "clock.wav";
        private const string AnswerPath = "answer.wav";
        private const string TapPath = "tap.wav";
        private const string ArcPath = "arc.wav";

        // Stream Handle
        private BassStream clockStream;
        private BassStream answerStream;
        private BassStream tapStream;
        private BassStream arcStream;
        public BassStream AudioStream;
        public BassStream AudioPreviewStream;
        public BassStream CalibrationStream;

        private const float FadeDuration = 0.5f;

        private BassAudioService()
        {
            Initialize();
            Application.quitting += OnApplicationQuit;
        }


        private void Initialize()
        {
            if (initialized) return;

            if (Bass.Init())
            {
                Debug.Log("BASS初始化成功！");
                initialized = true;
                LoadAudioAsync().Forget();
            }
            else
            {
                Debug.LogError($"BASS初始化失败！错误代码: {Bass.LastError}");
                initialized = true;
            }
        }

        private async UniTask LoadAudioAsync()
        {
            var clockFilePath = Path.Combine(Application.streamingAssetsPath, "audio", ClockPath);
            var answerFilePath = Path.Combine(Application.streamingAssetsPath, "audio", AnswerPath);
            var tapFilePath = Path.Combine(Application.streamingAssetsPath, "audio", TapPath);
            var arcFilePath = Path.Combine(Application.streamingAssetsPath, "audio", ArcPath);
            

            var clockFileBytes = await ReadFileAsync(clockFilePath);
            var answerFileBytes = await ReadFileAsync(answerFilePath);
            var tapFileBytes = await ReadFileAsync(tapFilePath);
            var arcFileBytes = await ReadFileAsync(arcFilePath);

            clockStream = new BassStream(clockFileBytes);
            answerStream = new BassStream(answerFileBytes);
            clockStream.Volume = 2.0f;
            tapStream = new BassStream(tapFileBytes);
            arcStream = new BassStream(arcFileBytes);
        }

        private static async UniTask<byte[]> ReadFileAsync(string filePath)
        {
            var uri = new Uri(filePath);
            using var request = UnityWebRequest.Get(uri);
            await request.SendWebRequest().ToUniTask();
            if (request.result == UnityWebRequest.Result.Success)
            {
                return request.downloadHandler.data;
            }

            Debug.LogError($"Failed to load file: {request.error}");
            return null;
        }


        public void PlayClock(int delayMilliseconds)
        {
            UniTask.Delay(delayMilliseconds).ContinueWith(() => { clockStream.Play(); });
        }

        public void PlayAnswer(int timing, int delay = 0)
        {
            if (delay == 0)
            {
                answerStream.Play();
                return;
            }

            DelayAndPlay(delay, answerStream).Forget();
        }

        private static UniTask DelayAndPlay(int delayMilliseconds, BassStream stream)
        {
            return UniTask.Delay(delayMilliseconds).ContinueWith(stream.Play);
        }

        public void PlayTapHitSound(int timing)
        {
            tapStream.Play();
        }

        public void PlayArcHitSound(int timing)
        {
            arcStream.Play();
        }

        public async UniTask PlayAudioPreview(string fullPath)
        {
            AudioPreviewStream?.Dispose();
            var audioBytes = await ReadFileAsync(fullPath);
            AudioPreviewStream = new BassStream(audioBytes, BassFlags.Loop);
            AudioPreviewStream.FadeInAsync(0.7f).Forget();
            AudioPreviewStream.Play();
        }

        public async UniTask LoadAudioAsync(string fullPath)
        {
            AudioStream?.Dispose();
            var audioBytes = await ReadFileAsync(fullPath);
            AudioStream = new BassStream(audioBytes);
        }

        public void PlayAudio(int delay = 0)
        {
            if (delay > 0)
            {
                DelayAndPlay(delay, AudioStream).Forget();
                return;
            }

            AudioStream.Play();
        }


        public int? GetAudioPosition()
        {
            return AudioStream?.Position;
        }

        public int? GetAudioLength()
        {
            return AudioStream?.Length;
        }

        public async UniTask LoadCalibrationStream()
        {
            var calibrationFilePath = Path.Combine(Application.streamingAssetsPath, "audio", "Calibrate.ogg");
            var calibrationFileBytes = await ReadFileAsync(calibrationFilePath);
            CalibrationStream = new BassStream(calibrationFileBytes);
        }

        private void OnApplicationQuit()
        {
            Dispose();
        }

        public void Dispose()
        {
            tapStream.Dispose();
            if (!initialized) return;
            Bass.Free();
            Debug.Log("BASS已释放");
            initialized = false;
        }
    }
}