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

        private bool _initialized = false;

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
        private BassStream audioStream;
        private BassStream audioPreviewStream;

        private const float FadeDuration = 0.5f;

        private BassAudioService()
        {
            Initialize();
            Application.quitting += OnApplicationQuit;
        }

        

        private void Initialize()
        {
            if (_initialized) return;

            if (Bass.Init())
            {
                Debug.Log("BASS初始化成功！");
                _initialized = true;
                LoadAudioAsync().Forget();
            }
            else
            {
                Debug.LogError($"BASS初始化失败！错误代码: {Bass.LastError}");
                _initialized = true;
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
            answerStream =  new BassStream(answerFileBytes);
            tapStream = new BassStream(tapFileBytes);
            arcStream = new BassStream(arcFileBytes);
            //Bass.ChannelSetAttribute(tapStream, ChannelAttribute.Volume, 1.0f);
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
            UniTask.Delay(delayMilliseconds).ContinueWith(() => {clockStream.Play();});
        }

        public void PlayAnswer(int timing, int delay = 0)
        {
            if (delay == 0)
            {
                answerStream.Play();
                return;
            }

            DelayAndPlay(delay).Forget();
        }

        private UniTask DelayAndPlay(int delayMilliseconds)
        {
            return UniTask.Delay(delayMilliseconds).ContinueWith(() => {answerStream.Play();});
        }

        public void PlayTapHitSound(int timing)
        {
            //Bass.ChannelPlay(tapStream, true);
        }

        public void PlayArcHitSound(int timing)
        {
            //Bass.ChannelPlay(arcStream, true);
        }

        public async UniTask PlayAudioPreview(string fullPath)
        {
            StopAudioPreview();
            var audioBytes = await ReadFileAsync(fullPath);
            audioPreviewStream = new BassStream(audioBytes,BassFlags.Loop);
            audioPreviewStream.Play();
        }

        public void PauseAudioPreview()
        {
            audioPreviewStream?.Pause();
        }
        
        public void ResumeAudioPreview()
        {
            audioPreviewStream?.Resume();
        }
        
        public void StopAudioPreview()
        {
            audioPreviewStream?.Dispose();
        }
        

        private void OnApplicationQuit()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_initialized) return;
            Bass.Free();
            Debug.Log("BASS已释放");
            _initialized = false;
        }
    }
}