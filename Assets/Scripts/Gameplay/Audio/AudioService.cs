using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace ArcCreate.Gameplay.Audio
{
    public class AudioService : MonoBehaviour
    {
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private VideoPlayer videoPlayer;
        [SerializeField] private GameplayData gameplayData;
        [SerializeField] private Slider timingSlider;

        // /// <summary>
        // /// Whether or not chart timing is being stationary. Example of this being true is during warming up period after a play with delay.
        // /// </summary>
        // private bool isStationary;

        // /// <summary>
        // /// Scalar to speed up or slow down chart update speed to sync with music.
        // /// </summary>
        // private float updatePace = 1;

        private bool audioEndReported;

        // /// <summary>
        // /// Timing at which the audio started playing from.
        // /// </summary>
        // private int startTime = 0;
        //
        // /// <summary>
        // /// Time (in dsp unit) at which the audio started playing.
        // /// </summary>
        // private double dspStartPlayingTime = 0;
        //
        // /// <summary>
        // /// Time (in realTimeSinceStartup unit) at which the audio started playing.
        // /// </summary>
        // private double realStartPlayingTime = 0;
        //
        // /// <summary>
        // /// Whether to let the timing stay unchanged until the audio start playing, or to increate it linearly.
        // /// </summary>
        // private bool stationaryBeforeStart;

        /// <summary>
        ///     The current timing value in ms.
        /// </summary>
        private int audioTiming;

        /// <summary>
        ///     Last timing the audio was paused at.
        /// </summary>
        private int lastPausedTiming;

        /// <summary>
        ///     Return to this timing point after next pause.
        /// </summary>
        private int onPauseReturnTo;

        /// <summary>
        ///     The audio playback speed.
        /// </summary>
        private float playbackSpeed = 1;

        /// <summary>
        ///     Whether to return to <see cref="onPauseReturnTo" /> timing point after next pause.
        /// </summary>
        private bool returnOnPause;

        public int ChartTiming
        {
            get => BassAudioService.Instance.MyChartTimer?.GetElapsedMilliseconds() ?? -2000;
            set
            {
                //AudioTiming = value + FullOffset;
            }
        }

        public int AudioTiming
        {
            get => audioTiming;
            set
            {
                audioTiming = value;
                UpdateSlider(value);
                if (IsPlaying)
                {
                    // audioSource.Stop();
                    // if (videoPlayer.enabled)
                    // {
                    //     videoPlayer.Pause();
                    // }
                    //
                    //Play(audioTiming, 0);
                }
                else if (videoPlayer.enabled)
                {
                    videoPlayer.time = Mathf.Clamp((value - GlobalOffset) / 1000f, 0, (float)videoPlayer.length);
                    videoPlayer.Play();
                    videoPlayer.Pause();
                }

                Services.Chart.ResetJudge();
            }
        }

        public int AudioLength { get; private set; }

        public bool IsPlaying => true; //audioSource.isPlaying;


        public bool IsLoaded => BassAudioService.Instance.AudioStream != null;

        public bool IsReadyForUpdate => BassAudioService.Instance.AudioStream != null;


        // public AudioClip TapHitsoundClip => Services.Hitsound.TapHitsoundClip;
        //
        // public AudioClip ArcHitsoundClip => Services.Hitsound.ArcHitsoundClip;
        //
        // public Dictionary<string, AudioClip> SfxAudioClips => Services.Hitsound.SfxAudioClips;

        private int FullOffset =>
            Values.ChartAudioOffset + Mathf.RoundToInt(Settings.GlobalAudioOffset.Value * playbackSpeed);

        private int GlobalOffset => Mathf.RoundToInt(Settings.GlobalAudioOffset.Value * playbackSpeed);

        private void Awake()
        {
            Settings.MusicAudio.OnValueChanged.AddListener(OnMusicAudioSettings);
            OnMusicAudioSettings(Settings.MusicAudio.Value);
            gameplayData.PlaybackSpeed.OnValueChange += OnPlaybackSpeedChange;
            OnPlaybackSpeedChange(gameplayData.PlaybackSpeed.Value);
        }

        private void OnDestroy()
        {
            Settings.MusicAudio.OnValueChanged.RemoveListener(OnMusicAudioSettings);
            gameplayData.PlaybackSpeed.OnValueChange -= OnPlaybackSpeedChange;
        }

        public void SetAudioTimingSilent(int timing)
        {
            audioTiming = timing;
            if (videoPlayer.enabled)
            {
                videoPlayer.time = Mathf.Clamp(timing / 1000f, 0, (float)videoPlayer.length);

                // Force video player to update the texture
                videoPlayer.Play();
                videoPlayer.Pause();
            }

            UpdateSlider(timing);
        }

        public void UpdateTime()
        {
            // double dspTime =   
            // if (!IsPlaying)
            // {
            // if (audioSource.clip != null && audioTiming >= Mathf.Max(0, AudioLength - 100))
            // {
            //     OnAudioEnd();
            // }
            //
            //     return;
            // }
            //
            // if (Application.isMobilePlatform || Settings.SyncToDSPTime.Value)
            // {
            //     isStationary = stationaryBeforeStart && dspTime <= dspStartPlayingTime;
            //
            //     if (stationaryBeforeStart)
            //     {
            //         dspTime = Math.Max(dspTime, dspStartPlayingTime);
            //     }
            //
            //     int dspTimePassedSinceAudioStart = Mathf.RoundToInt((float)((dspTime - dspStartPlayingTime) * 1000 * playbackSpeed));
            //     int realTimePassedSinceAudioStart = Mathf.RoundToInt((float)((Time.realtimeSinceStartup - realStartPlayingTime) * 1000 * playbackSpeed));
            //     updatePace = realTimePassedSinceAudioStart < 0 + Mathf.Epsilon ? 1
            //                : Mathf.Lerp(updatePace, (float)dspTimePassedSinceAudioStart / realTimePassedSinceAudioStart, 0.1f);
            //     int newTiming = Mathf.RoundToInt(realTimePassedSinceAudioStart * updatePace) + startTime - FullOffset;
            //
            //     if (!stationaryBeforeStart || dspTime > dspStartPlayingTime)
            //     {
            //         audioTiming = newTiming;
            //     }
            // }
            // else
            // {
            //     audioTiming = Mathf.RoundToInt(AudioSource.time * 1000f);
            // }
            // isStationary = false;
            //audioTiming = (BassAudioService.Instance.GetAudioPosition() ?? 0);
            //audioTiming = BassAudioService.Instance.MyChartTimer.GetElapsedMilliseconds();
            // if (BassAudioService.Instance.AudioStream != null)
            // {
            //     audioTiming = BassAudioService.Instance.AudioStream.Position > 0 ? BassAudioService.Instance.AudioStream.Position : 0;
            //     if (audioTiming != 0 && !BassAudioService.Instance.AudioStream.IsPlaying && !PauseMenu.IsPausing)
            //     {
            //         OnAudioEnd();
            //     }
            // }
            if (BassAudioService.Instance.AudioStream == null) return;

            if (ChartTiming > BassAudioService.Instance.AudioStream.Length) OnAudioEnd();

            // if (audioTiming >= Mathf.Max(0, BassAudioService.Instance.GetAudioLength() ?? 1000 - 100))
            // {
            //     OnAudioEnd();
            // }
            UpdateSlider(ChartTiming);
        }

        // public void PauseButtonPressed()
        // {
        //     if (!IsPlaying)
        //     {
        //         ResumeImmediately();
        //     }
        //     else
        //     {
        //         Pause();
        //     }
        // }

        public void Pause()
        {
            // lastPausedTiming = audioTiming;
            // audioSource.Stop();
            // if (videoPlayer.enabled)
            // {
            //     videoPlayer.Pause();
            // }
            //
            // if (returnOnPause)
            // {
            //     lastPausedTiming = onPauseReturnTo;
            //     AudioTiming = onPauseReturnTo;
            // }
            //
            // SetEnableAutorotation(true);
        }

        public void Stop()
        {
            audioSource.Stop();
            if (videoPlayer.enabled) videoPlayer.Stop();

            lastPausedTiming = 0;
            AudioTiming = 0;

            SetEnableAutorotation(true);
        }

        public void PlayImmediately(int timing)
        {
            // stationaryBeforeStart = false;
            returnOnPause = false;
            Play(timing);
        }

        public void PlayWithDelay(int timing, int delayMs)
        {
            BassAudioService.Instance.AudioPreviewStream.Dispose();
            var bpm = gameplayData.BaseBpm.Value;
            BassAudioService.Instance.StartGameAudio(bpm).Forget();
            // stationaryBeforeStart = false;
            returnOnPause = false;
            //Play(timing, delay);
            Services.Chart.ResetJudge();
        }

        public void ResumeImmediately(bool resetJudge = true)
        {
            // stationaryBeforeStart = true;
            returnOnPause = false;
            Play(lastPausedTiming, 0, resetJudge);
        }

        public void ResumeWithDelay(int delayMs, bool resetJudge = true)
        {
            // stationaryBeforeStart = true;
            returnOnPause = false;
            Play(lastPausedTiming, delayMs, resetJudge);
        }

        public void ResumeReturnableImmediately()
        {
            // stationaryBeforeStart = true;
            returnOnPause = true;
            onPauseReturnTo = audioTiming;
            Play(lastPausedTiming);
        }

        public void ResumeReturnableWithDelay(int delayMs)
        {
            // stationaryBeforeStart = true;
            returnOnPause = true;
            onPauseReturnTo = audioTiming;
            Play(lastPausedTiming, delayMs);
        }

        public void SetResumeAt(int timing)
        {
            lastPausedTiming = timing;
        }

        public void SetReturnOnPause(bool cond, int timing = 0)
        {
            returnOnPause = cond;
            onPauseReturnTo = timing;
        }

        public async UniTask PrepareVideoPlayback()
        {
            if (!videoPlayer.enabled) return;

            videoPlayer.Prepare();
            await UniTask.WaitUntil(() => videoPlayer.isPrepared);
        }

        private void Play(int timing = 0, int delay = 0, bool resetJudge = true)
        {
            // delay = Mathf.Max(delay, 0);
            // if (videoPlayer.enabled)
            // {
            //     delay = Mathf.Max(delay, 500);
            // }
            //
            // if (timing >= AudioLength - 1)
            // {
            //     timing = 0;
            // }
            //
            // if (timing < 0)
            // {
            //     stationaryBeforeStart = false;
            //     timing = 0;
            // }
            //
            // audioTiming = stationaryBeforeStart ? timing : timing - delay;
            // updatePace = 1;
            //
            // if (resetJudge)
            // {
            //     Services.Chart.ResetJudge();
            // }
            //
            // audioSource.time = Mathf.Max(0, timing) / 1000f;
            // if (timing < 0)
            // {
            //     delay += -timing;
            // }
            //
            // dspStartPlayingTime = AudioSettings.dspTime + ((double)delay / 1000);
            // realStartPlayingTime = Time.realtimeSinceStartup + ((double)delay / 1000);
            // startTime = timing + FullOffset;
            // if (delay > 0)
            // {
            //     
            //     BassAudioService.Instance.PlayAudio(delay);
            //     //audioSource.Play();
            //     //audioSource.PlayScheduled(dspStartPlayingTime);
            // }
            // else
            // {
            //     //audioSource.Play();
            // }
            //
            // if (videoPlayer.enabled)
            // {
            //     StartDelayedVideoPlayback(timing - GlobalOffset, delay).Forget();
            // }
            //
            // SetEnableAutorotation(false);
            // audioEndReported = false;
        }

        private async UniTask StartDelayedVideoPlayback(int timing, int delay)
        {
            videoPlayer.Pause();
            if (timing < 0)
            {
                delay += -timing;
                timing = 0;
            }

            videoPlayer.time = Mathf.Clamp(timing / 1000f, 0, (float)videoPlayer.length);
            videoPlayer.Prepare();
            await UniTask.Delay(delay);
            videoPlayer.Play();
        }

        private void OnPlaybackSpeedChange(float value)
        {
            playbackSpeed = value;
            audioSource.pitch = value;
            videoPlayer.playbackSpeed = value;
            if (Application.isMobilePlatform || Settings.SyncToDSPTime.Value)
            {
                Pause();
                ResumeWithDelay(200, false);
            }
        }


        private void SetEnableAutorotation(bool v)
        {
            Screen.autorotateToLandscapeLeft = v;
            Screen.autorotateToLandscapeRight = v;
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
        }

        private void OnMusicAudioSettings(float volume)
        {
            var stream = BassAudioService.Instance.AudioStream;
            if (stream == null) return;
            stream.Volume = Mathf.Clamp(volume, 0, 1);
        }

        private void OnAudioEnd()
        {
            if (!audioEndReported && Values.ShouldNotifyOnAudioEnd && !gameplayData.EnablePracticeMode.Value)
            {
                BassAudioService.Instance.AudioStream.Dispose();
                BassAudioService.Instance.MyChartTimer.StopTiming().ResetTiming();
                var result = Services.Score.GetPlayResult();
                gameplayData.NotifyPlayComplete(result);
                SetEnableAutorotation(true);
            }

            audioEndReported = true;
        }

        private void UpdateSlider(float timing)
        {
            var audioLength = BassAudioService.Instance.ChartEndTiming;
            timingSlider.value = audioLength > 0 ? Mathf.Clamp(timing / audioLength, 0, 1) : 0;
        }
    }
}