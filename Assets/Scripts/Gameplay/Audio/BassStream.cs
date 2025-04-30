using System;
using System.Runtime.InteropServices;
using ManagedBass;
using UnityEngine;

namespace ArcCreate.Gameplay.Audio
{
    public sealed class BassStream:IDisposable
    {
        private int streamHandle;
        private GCHandle audioBytesHandle;
        private float lastTiming = -10.00f;
        private const float MinInterval = 0.04f;
        
        public void Play()
        {
            var now = Time.unscaledTime;

            if (now - lastTiming < MinInterval)
            {
                return;
            }

            lastTiming = Time.unscaledTime;
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
        
        public BassStream(byte[] audioBytes, BassFlags flags = BassFlags.Default)
        {
            var gcPin = GCHandle.Alloc(audioBytes, GCHandleType.Pinned);
            var handle = Bass.CreateStream(gcPin.AddrOfPinnedObject(), 0, audioBytes.Length, flags);
            if (handle == 0)
            {
                gcPin.Free();
                Debug.LogError("Failed to create Bass stream.");
            }
            streamHandle = handle;
            audioBytesHandle = gcPin;
        }

        public void Dispose()
        {
            if (streamHandle != 0)
            {
                Bass.StreamFree(streamHandle);
                streamHandle = 0;
            }

            if (audioBytesHandle.IsAllocated)
            {
                audioBytesHandle.Free();
            }
        }
    }
}