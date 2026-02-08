using System;
using System.Runtime.InteropServices;
using Cysharp.Threading.Tasks;
using ManagedBass;
using UnityEngine;

namespace ArcCreate.Gameplay.BassAudio
{
    public class Stream : IDisposable
    {
        private readonly int streamHandle;
        private GCHandle audioBytesHandle;


        public Stream(byte[] audioBytes, BassFlags flags = BassFlags.Default)
        {
            var gcPin = GCHandle.Alloc(audioBytes, GCHandleType.Pinned);
            var stream = Bass.CreateStream(gcPin.AddrOfPinnedObject(), 0, audioBytes.Length, flags);
            if (stream == 0) gcPin.Free();

            streamHandle = stream;
            audioBytesHandle = gcPin;
        }

        protected Stream()
        {
        }

        public int Position
        {
            get
            {
                if (streamHandle == 0)
                    return 0;

                var posBytes = Bass.ChannelGetPosition(streamHandle);
                var seconds = Bass.ChannelBytes2Seconds(streamHandle, posBytes);
                return Mathf.RoundToInt((float)(seconds * 1000));
            }
            set
            {
                if (streamHandle == 0)
                    return;

                var seconds = value / 1000.0;
                var bytePos = Bass.ChannelSeconds2Bytes(streamHandle, seconds);
                Bass.ChannelSetPosition(streamHandle, bytePos);
            }
        }

        public int Length
        {
            get
            {
                if (streamHandle == 0)
                    return 0;

                var lengthBytes = Bass.ChannelGetLength(streamHandle);
                var seconds = Bass.ChannelBytes2Seconds(streamHandle, lengthBytes);
                return Mathf.RoundToInt((float)(seconds * 1000));
            }
        }

        public float Volume
        {
            get
            {
                if (streamHandle == 0)
                    return 1.0f;

                Bass.ChannelGetAttribute(streamHandle, ChannelAttribute.Volume, out var vol);
                return vol;
            }
            set
            {
                if (streamHandle == 0)
                    return;

                var clamped = Mathf.Clamp(value, 0.0f, 2.0f);
                Bass.ChannelSetAttribute(streamHandle, ChannelAttribute.Volume, clamped);
            }
        }

        public bool IsPlaying => Bass.ChannelIsActive(streamHandle) == PlaybackState.Playing;

        public void Dispose()
        {
            if (streamHandle != 0) Bass.StreamFree(streamHandle);

            if (audioBytesHandle.IsAllocated) audioBytesHandle.Free();
        }

        public void Play()
        {
            Bass.ChannelPlay(streamHandle, true);
        }

        public void Pause()
        {
            Bass.ChannelPause(streamHandle);
        }

        public void Resume()
        {
            Bass.ChannelPlay(streamHandle);
        }

        public async UniTask FadeOutAsync(float duration, Action onComplete = null)
        {
            var startVolume = Volume;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                Volume = Mathf.Lerp(startVolume, 0f, t);

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            Volume = 0f;
            onComplete?.Invoke();
        }

        public async UniTask FadeInAsync(float duration, Action onComplete = null)
        {
            Volume = 0f;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                Volume = Mathf.Lerp(0f, 1f, t);

                await UniTask.Yield(PlayerLoopTiming.Update);
            }

            Volume = 1f;
            onComplete?.Invoke();
        }

        public async UniTask PlayScheduled(double dspTime)
        {
            var currentDspTime = AudioSettings.dspTime;
            var delay = dspTime - currentDspTime;

            if (delay <= 0)
            {
                Play();
                return;
            }

            var delayMs = Mathf.Max(0f, (float)(delay * 1000));
            var deadline = Time.realtimeSinceStartup + (float)delay;

            while (Time.realtimeSinceStartup < deadline - 0.01f) await UniTask.Delay(1);

            while (Time.realtimeSinceStartup < deadline)
            {
            }

            Play();
        }
    }
}