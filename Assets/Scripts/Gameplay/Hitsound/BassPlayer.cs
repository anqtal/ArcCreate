using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Un4seen.Bass;


namespace ArcCreate.Gameplay.Hitsound
{
    public class BassPlayer: MonoBehaviour
    {
        private int clockStreamHandle;
        private int answerStreamHandle;
        public string audioPath = "Answer.wav";
        public string clockPath = "answer_clock.wav";
        private readonly List<float> scheduledTimes = new();
        private void Start()
        {
            bool initialized = Bass.BASS_Init(-1, 44100, BASSInit.BASS_DEVICE_DEFAULT, System.IntPtr.Zero);
            if (!initialized)
            {
                Debug.LogError("BASS初始化失败！");
                return;
            }
            Debug.Log("BASS初始化成功！");
            LoadAudio();
        }
        
        private void LoadAudio()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath,"audio",audioPath);
            answerStreamHandle = Bass.BASS_StreamCreateFile(fullPath, 0L, 0L, BASSFlag.BASS_DEFAULT);
            Bass.BASS_ChannelSetAttribute(answerStreamHandle, BASSAttribute.BASS_ATTRIB_VOL, 1.5f);
            if (answerStreamHandle == 0)
            {
                Debug.LogError("加载音频失败！错误代码：" + Bass.BASS_ErrorGetCode());
            }
            Debug.Log("加载音频成功！");
            
        }
        
        private void LoadClock()
        {
            string fullPath = Path.Combine(Application.streamingAssetsPath,"audio",clockPath);
            clockStreamHandle = Bass.BASS_StreamCreateFile(fullPath, 0L, 0L, BASSFlag.BASS_DEFAULT);
            Bass.BASS_ChannelSetAttribute(clockStreamHandle, BASSAttribute.BASS_ATTRIB_VOL, 2.5f);
            if (clockStreamHandle == 0)
            {
                Debug.LogError("加载音频失败！错误代码：" + Bass.BASS_ErrorGetCode());
            }
            Debug.Log("加载音频成功！");
        }
        
        private void Play()
        {
            var success = Bass.BASS_ChannelPlay(answerStreamHandle, true);
            if (!success)
            {
                Debug.LogError("播放音频失败！错误代码：" + Bass.BASS_ErrorGetCode());
            }
            Debug.Log("播放音频成功！");
        }
        
        public void Play(int timing)
        {
            Play();
        }


        // public void Stop()
        // {
        //     Bass.BASS_ChannelStop(streamHandle);
        // }
        public void PlayScheduled(float delayInMilliseconds)
        {
            var scheduledTime = Time.time + delayInMilliseconds / 1000f;
            scheduledTimes.Add(scheduledTime);
        }

        private void Update()
        {
            var currentTime = Time.time;
            for (var i = scheduledTimes.Count - 1; i >= 0; i--)
            {
                if (!(currentTime >= scheduledTimes[i])) continue;
                Play();
                scheduledTimes.RemoveAt(i);
            }
        }
        

        // private void OnDestroy()
        // {
        //     Bass.BASS_StreamFree(streamHandle);
        //     Bass.BASS_Free();
        // }
    }
}