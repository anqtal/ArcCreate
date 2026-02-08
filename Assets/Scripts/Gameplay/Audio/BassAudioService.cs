using System;
using System.IO;
using Cysharp.Threading.Tasks;
using ManagedBass;
using UnityEngine;
using UnityEngine.Networking;
using Random = System.Random;

namespace ArcCreate.Gameplay.Audio
{
    public sealed class BassAudioService : IDisposable
    {
        // Audio Path
        private const string ClockPath = "clock.wav";
        private const string AnswerPath = "answer.wav";
        private const string TapPath = "tap.wav";
        private const string ArcPath = "arc.wav";
        private static readonly Lazy<BassAudioService> _instance = new(() => new BassAudioService());
        public BassStream answerStream;
        private BassStream arcStream;
        public BassStream AudioPreviewStream;
        public BassStream AudioStream;
        public BassStream CalibrationStream;
        public int ChartEndTiming;

        // Stream Handle
        private BassStream clockStream;

        private bool initialized;
        public AnswerSoundPlayer MyAnswerSoundPlayer;

        public ChartTimer MyChartTimer;
        private BassStream tapStream;

        private BassAudioService()
        {
            Initialize();
            Application.quitting += OnApplicationQuit;
        }

        public static BassAudioService Instance => _instance.Value;

        public void Dispose()
        {
            if (!initialized) return;
            Settings.EffectAudio.OnValueChanged.RemoveListener(OnEffectAudioSettings);
            FreeInGameStream();
            Bass.Free();
            initialized = false;
        }

        // public static int PluginLoad(string FilePath)
        // {
        //     Debug.Log("Starting plugin load");
        //     if (Path.HasExtension(FilePath))
        //     {
        //         Debug.Log("Has Extension");
        //         return 0;
        //     }
        //
        //     string directoryName = Path.GetDirectoryName(FilePath);
        //     string fileName = Path.GetFileName(FilePath);
        //     string[] strArray = new string[3]
        //     {
        //         Path.Combine(directoryName, fileName + ".dll"),
        //         Path.Combine(directoryName, $"lib{fileName}.so"),
        //         Path.Combine(directoryName, $"lib{fileName}.dylib")
        //     };
        //     foreach (string str in strArray)
        //     {
        //         Debug.Log("No extension, load path:  " + str);
        //         if (File.Exists(str))
        //         {
        //             // int num = Bass.BASS_PluginLoad(str);
        //             // if (num != 0 || Bass.LastError == Errors.Already)
        //             //     return num;
        //             Debug.Log("File exists");
        //         }
        //     }
        //
        //     //return Bass.BASS_PluginLoad(FilePath);
        //     return 0;
        // }

        private void Initialize()
        {
            if (initialized) return;

            if (Bass.Init())
            {
                Settings.EffectAudio.OnValueChanged.AddListener(OnEffectAudioSettings);
                initialized = true;
            }
            else
            {
                Debug.LogError($"BASS初始化失败！错误代码: {Bass.LastError}");
                initialized = true;
            }
        }

        private void LoadSoundEffect()
        {
            var sePath = Path.Combine(Application.streamingAssetsPath, "audio", "SE");
            var seFiles = Directory.GetFiles(sePath, "*.wav");
            foreach (var seFile in seFiles)
            {
                var seFileName = Path.GetFileName(seFile);
                var seFilePath = Path.Combine(sePath, seFileName);
                var seFileBytes = File.ReadAllBytes(seFilePath);
                var seStream = new BassStream(seFileBytes);
                var seName = seFileName.Replace(".wav", "");
            }
        }

        private void PlaySoundEffect(SoundEffectType type)
        {
        }

        private void FreeSoundEffect()
        {
        }

        private async UniTask LoadInGameStreamAsync()
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
            OnEffectAudioSettings(Settings.EffectAudio.Value);
        }

        public void FreeInGameStream()
        {
            clockStream?.Dispose();
            answerStream?.Dispose();
            // tapStream?.Dispose();
            // arcStream?.Dispose();
        }

        public async UniTask<byte[]> ReadFileAsync(string filePath)
        {
            var uri = new Uri(filePath);
            using var request = UnityWebRequest.Get(uri);
            await request.SendWebRequest().ToUniTask();
            // if (request.result == UnityWebRequest.Result.Success)
            // {
            return request.downloadHandler.data;
            // }
            // Debug.LogError($"Failed to load file: {request.error}");
            // return null;
        }


        public async UniTask StartGameAudio(float bpm)
        {
            await LoadInGameStreamAsync();
            MyChartTimer?.StopTiming().ResetTiming();
            var timeStep = 60000 / bpm;
            var delay = (int)timeStep * 5;
            for (var i = 0; i < 4; i++)
            {
                clockStream.Play();
                await UniTask.Delay((int)timeStep);
            }

            DelayAndPlay(delay + Settings.GlobalAudioOffset.Value, AudioStream).Forget();
            MyChartTimer = new ChartTimer(delay + Values.ChartAudioOffset).StartTiming();
            MyAnswerSoundPlayer.PlayAllAnswerSoundsAsync().Forget();
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
            // var audioBytes = await ReadFileAsync(fullPath);
            var audioBytes = await DxResource.ReadFile(fullPath, DxResource.FileType.PreviewAudio);
            if (audioBytes == null || audioBytes.Length == 0)
            {
                Debug.LogWarning($"Preview audio missing for {fullPath}");
                return;
            }

            AudioPreviewStream = new BassStream(audioBytes, true);
            AudioPreviewStream.FadeInAsync(0.7f).Forget();
            AudioPreviewStream.Play();
        }

        private async UniTaskVoid LowerLpfOverTime()
        {
            var freq = 10000f;

            while (freq > 100f)
            {
                AudioPreviewStream.LpfFx(freq);
                freq -= 1000f;

                await UniTask.Delay(500); // 每 1 秒更新一次
            }

            // 最后一帧确保落在下限
            AudioPreviewStream.LpfFx(100f);
        }

        private async UniTaskVoid SetRandomPitch()
        {
            var random = new Random();
            while (true)
            {
                var newPitch = (float)(random.NextDouble() * 1.5 + 0.5);
                AudioPreviewStream.Pitch = newPitch;
                Debug.Log($"[PitchRandomizer] New Pitch: {newPitch:F2}");

                await UniTask.Delay(1000);
            }
        }

        public async UniTask LoadAudioAsync(string fullPath)
        {
            AudioStream?.Dispose();
            var audioBytes = await ReadFileAsync(fullPath);
            if (audioBytes == null || audioBytes.Length == 0) throw new Exception("Audio file is empty.");

            AudioStream = new BassStream(audioBytes, true);
            AudioStream.Volume = 1f;
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

        private void OnEffectAudioSettings(float volume)
        {
            if (tapStream != null) tapStream.Volume = volume;

            if (arcStream != null) arcStream.Volume = volume;
        }

        private enum SoundEffectType
        {
            OnClick
        }
    }
}