using System;
using System.Runtime.InteropServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using ManagedBass;
using ManagedBass.Fx;

namespace ArcCreate.Gameplay.BassAudio
{
    public sealed class FxStream : Stream, IDisposable
    {
        private readonly int streamHandle;
        private GCHandle audioBytesHandle;
        private int bpfHandle;
        private float currentPitch = 1f;
        private int distortionHandle;
        private int fxHandle;
        private int hpfHandle;
        private int lpfHandle;

        public FxStream(byte[] audioBytes, BassFlags flags = BassFlags.Default)
        {
            var gcPin = GCHandle.Alloc(audioBytes, GCHandleType.Pinned);
            var decodeStream = Bass.CreateStream(gcPin.AddrOfPinnedObject(), 0, audioBytes.Length,
                BassFlags.Decode | BassFlags.Float);
            var stream = BassFx.TempoCreate(decodeStream, flags);
            if (stream == 0) gcPin.Free();
            streamHandle = stream;
            audioBytesHandle = gcPin;
        }

        public float Pitch
        {
            get => currentPitch;
            set
            {
                currentPitch = value;

                if (fxHandle == 0) fxHandle = Bass.ChannelSetFX(streamHandle, EffectType.PitchShift, 0);

                var pitch = new PitchShiftParameters
                {
                    fPitchShift = value,
                    fSemitones = 0f,
                    lFFTsize = 2048,
                    lOsamp = 4,
                    lChannel = FXChannelFlags.All
                };

                Bass.FXSetParameters(fxHandle, pitch);
            }
        }

        public new void Dispose()
        {
            if (streamHandle != 0) Bass.StreamFree(streamHandle);

            if (audioBytesHandle.IsAllocated) audioBytesHandle.Free();
        }


        public void LpfFx(float cutoffFreq)
        {
            if (lpfHandle == 0) lpfHandle = Bass.ChannelSetFX(streamHandle, EffectType.BQF, 0);

            var lpf = new BQFParameters
            {
                lFilter = BQFType.LowPass, // 低通滤波器
                fCenter = cutoffFreq, // 来自 Slider 的参数
                fQ = 0.7f, // Q值
                fGain = 0.0f, // 增益
                lChannel = FXChannelFlags.All // 所有声道
            };

            Bass.FXSetParameters(lpfHandle, lpf);
        }

        public async UniTask GateFxAsync(float durationSeconds, float volume = 1.0f, CancellationToken token = default)
        {
            var interval = 0.2f;
            var half = interval / 2f;
            var elapsed = 0f;

            while (elapsed < durationSeconds)
            {
                if (token.IsCancellationRequested) break;

                // 静音
                Bass.ChannelSetAttribute(streamHandle, ChannelAttribute.Volume, 0f);
                await UniTask.Delay(TimeSpan.FromSeconds(half), cancellationToken: token);

                // 恢复音量
                Bass.ChannelSetAttribute(streamHandle, ChannelAttribute.Volume, volume);
                await UniTask.Delay(TimeSpan.FromSeconds(half), cancellationToken: token);

                elapsed += interval;
            }

            // 恢复音量
            Bass.ChannelSetAttribute(streamHandle, ChannelAttribute.Volume, volume);
        }

        public void DistortionFx(
            float drive = 0.8f,
            float dryMix = 0.7f,
            float wetMix = 0.3f,
            float feedback = 0f,
            float volume = 0.5f)
        {
            if (distortionHandle == 0) distortionHandle = Bass.ChannelSetFX(streamHandle, EffectType.Distortion, 0);

            var distortion = new DistortionParameters
            {
                fDrive = drive, // 驱动强度，通常0~1之间
                fDryMix = dryMix, // 干音混合量
                fWetMix = wetMix, // 湿音混合量（失真音量）
                fFeedback = feedback, // 反馈量，一般0~1之间
                fVolume = volume, // 效果音量控制
                lChannel = FXChannelFlags.All
            };

            Bass.FXSetParameters(distortionHandle, distortion);
        }


        public void HpfFx(float cutoffFreq)
        {
            if (hpfHandle == 0) hpfHandle = Bass.ChannelSetFX(streamHandle, EffectType.BQF, 0);

            var hpf = new BQFParameters
            {
                lFilter = BQFType.HighPass,
                fCenter = cutoffFreq, // 越高越接近原曲
                fQ = 0.7f,
                fGain = 0.0f,
                lChannel = FXChannelFlags.All
            };

            Bass.FXSetParameters(hpfHandle, hpf);
        }

        public void BpfFx(float centerFreq, float bandwidth = 1.0f)
        {
            if (bpfHandle == 0) bpfHandle = Bass.ChannelSetFX(streamHandle, EffectType.BQF, 0);

            var bpf = new BQFParameters
            {
                lFilter = BQFType.BandPass,
                fCenter = centerFreq, // 中心频率
                fBandwidth = bandwidth, // 带宽范围，建议 0.1 ~ 3.0
                fGain = 0.0f,
                lChannel = FXChannelFlags.All
            };

            Bass.FXSetParameters(bpfHandle, bpf);
        }

        public void RemoveFx()
        {
            if (fxHandle != 0)
            {
                Bass.ChannelRemoveFX(streamHandle, fxHandle);
                fxHandle = 0;
            }
        }

        public void RemoveLpfFx()
        {
            if (lpfHandle != 0)
            {
                Bass.ChannelRemoveFX(streamHandle, lpfHandle);
                lpfHandle = 0;
            }
        }

        public void RemoveHpfFx()
        {
            if (hpfHandle != 0)
            {
                Bass.ChannelRemoveFX(streamHandle, hpfHandle);
                hpfHandle = 0;
            }
        }

        public void RemoveBpfFx()
        {
            if (bpfHandle != 0)
            {
                Bass.ChannelRemoveFX(streamHandle, bpfHandle);
                bpfHandle = 0;
            }
        }

        public void RemoveDistortionFx()
        {
            if (distortionHandle != 0)
            {
                Bass.ChannelRemoveFX(streamHandle, distortionHandle);
                distortionHandle = 0;
            }
        }
    }
}