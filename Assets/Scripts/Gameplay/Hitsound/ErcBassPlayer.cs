using System;
using System.Collections.Generic;
using System.IO;
using ArcCreate.Gameplay.Audio;
using Cysharp.Threading.Tasks;
using ManagedBass;
using UnityEngine;

namespace ArcCreate.Gameplay.Hitsound
{
    public class ErcBassPlayer : IDisposable
    {
        private int clockStreamHandle;
        private int answerStreamHandle;
        private int hitSoundStreamHandle;
        private const string AudioPath = "Answer.wav";
        private const string ClockPath = "answer_clock.wav";
        private readonly List<float> playedAnswerSound = new();
        private int playCount;

        public ErcBassPlayer()
        {
            // var initialized = Bass.Init();
            // if (initialized)
            // {
            //     Debug.LogError("BASS初始化失败！");
            // }
            // else
            // {
            //     Debug.Log("BASS已启动，不需要");
            // }
        }

        public void LoadAudio()
        {
            // string fullPath = Path.Combine(Application.streamingAssetsPath, "audio", AudioPath);
            // answerStreamHandle = Bass.CreateStream(fullPath);
            // Bass.ChannelSetAttribute(answerStreamHandle, ChannelAttribute.Volume, 1.0f);
            // if (answerStreamHandle == 0)
            // {
            //     Debug.LogError("加载音频失败！错误代码：" + Bass.LastError);
            // }
        }

        public void LoadClock()
        {
            // string fullPath = Path.Combine(Application.streamingAssetsPath, "audio", ClockPath);
            // clockStreamHandle = Bass.CreateStream(fullPath);
            // Bass.ChannelSetAttribute(clockStreamHandle, ChannelAttribute.Volume, 2.0f);
            // if (clockStreamHandle == 0)
            // {
            //     Debug.LogError("加载音频失败！错误代码：" + Bass.LastError);
            // }
        }

        private static void Play(int streamHandle)
        {
            // var success = Bass.ChannelPlay(streamHandle, true);
            // if (!success)
            // {
            //     Debug.LogError("播放音频失败！错误代码：" + Bass.LastError);
            // }
        }

        public void PlayAnswerSound(int timing, int delay = 0)
        {
            var currentTime = DateTime.Now.Ticks / 10000;
            var scheduledTime = currentTime + delay;
            if (playedAnswerSound.Contains(timing)) return;
            if (delay == 0)
            {
                Play(answerStreamHandle);
                playedAnswerSound.Add(timing);
                return;
            }
            SchedulePlay(delay);
            playedAnswerSound.Add(timing);
        }

        private void SchedulePlay(int delay)
        {
            DelayAndPlay(delay).Forget();
        }

        private UniTask DelayAndPlay(int delayMilliseconds)
        {
            return UniTask.Delay(delayMilliseconds).ContinueWith(() =>
            {
                playCount++;
                Play(playCount > 4 ? answerStreamHandle : clockStreamHandle);
            });
        }


        // public void Stop()
        // {
        //     Bass.BASS_ChannelStop(streamHandle);
        // }

        // public void Update()
        // {
        //     var currentTime = DateTime.Now.Ticks / 10000;
        //     for (var i = scheduledTimes.Count - 1; i >= 0; i--)
        //     {
        //         if (!(currentTime >= scheduledTimes[i])) continue;
        //         playCount++;
        //         Play(playCount > 4 ? answerStreamHandle : clockStreamHandle);
        //         scheduledTimes.RemoveAt(i);
        //     }
        // }

        public void ResetPlayCount()
        {
            playCount = 0;
        }

        public void ResetScheduledTimes()
        {
            playCount = 5;
            playedAnswerSound.Clear();
        }

        public void Dispose()
        {
            Bass.StreamFree(clockStreamHandle);
            Bass.StreamFree(answerStreamHandle);
        }
    }
}