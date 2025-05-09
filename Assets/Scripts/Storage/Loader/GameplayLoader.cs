using System;
using System.Collections.Generic;
using System.IO;
using ArcCreate.ChartFormat;
using ArcCreate.Data;
using ArcCreate.Gameplay;
using ArcCreate.Gameplay.Audio;
using ArcCreate.Storage.Data;
using ArcCreate.Utility.Extension;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ArcCreate.Storage
{
    public class GameplayLoader
    {
        private readonly IGameplayControl gameplayControl;
        private readonly GameplayData gameplayData;

        public GameplayLoader(IGameplayControl gameplayControl, GameplayData gameplayData)
        {
            this.gameplayControl = gameplayControl;
            this.gameplayData = gameplayData;
        }

        public async UniTask Load(LevelStorage level, ChartSettings chart)
        {
            await UniTask.DelayFrame(5);
            LoadMetadata(level, chart);
            LoadChart(level, chart);
            LoadScenecontrol(level, chart);

            // Avoid jacket flickering after reload
            var audioTask = LoadAudio(level, chart);
            var bgTask = LoadBackground(level, chart);

            await UniTask.WhenAll(audioTask, bgTask);
            await UniTask.WaitUntil(() => gameplayControl.IsLoaded);
        }

        private static async UniTask LoadAudio(LevelStorage level, ChartSettings chart)
        {
            var fileName = $"{level.Identifier}.ogg";
            var localDir = Path.Combine(Application.persistentDataPath, "dl");
            var localPath = Path.Combine(localDir, fileName);
            if (!Directory.Exists(localDir))
                Directory.CreateDirectory(localDir);
            
            if (!File.Exists(localPath))
            {
                var url = $"https://erc.osiom.cc/dl/song/{level.Identifier}/base.ogg";

                using var request = UnityWebRequest.Get(url);
                request.downloadHandler = new DownloadHandlerBuffer();
                await request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new Exception($"下载音频失败: {request.error}");
                }

                await File.WriteAllBytesAsync(localPath, request.downloadHandler.data);
            }
            await BassAudioService.Instance.LoadAudioAsync(localPath);
        }

        private async UniTask LoadBackground(LevelStorage level, ChartSettings chart)
        {
            var bgPath = Path.Combine(Application.streamingAssetsPath, "bg", chart.BackgroundPath+".jpg");
            var uri = new Uri(bgPath);
            await gameplayData.LoadBackgroundFromHttp(uri);
        }

        private void LoadScenecontrol(LevelStorage level, ChartSettings chart)
        {
            // change to touch
            Settings.InputMode.Value = (int)InputMode.Touch;
            StorageFileAccessWrapper fileAccess = new StorageFileAccessWrapper(level);
            Option<string> scJsonRealPath = level.GetRealPath(Path.ChangeExtension(chart.ChartPath, ".sc.json"));
            if (scJsonRealPath.HasValue)
            {
                string json = File.ReadAllText(scJsonRealPath.Value);
                gameplayControl.Scenecontrol.Import(json, fileAccess);
            }

            gameplayControl.Scenecontrol.WaitForSceneLoad();
        }

        private void LoadChart(LevelStorage level, ChartSettings chart)
        {
            var fileAccess = new StorageFileAccessWrapper(level);
            var path = level.Identifier+chart.ChartPath;
            var reader = ChartReaderFactory.GetReader(fileAccess, path);
            reader.Parse();
            gameplayData.LoadChart(reader, "", fileAccess);
        }

        private void LoadMetadata(LevelStorage level, ChartSettings chart)
        {
            gameplayData.BaseBpm.Value = chart.BaseBpm;
            gameplayControl.Skin.AlignmentSkin = chart.Skin?.Side ?? string.Empty;
            gameplayControl.Skin.AccentSkin = chart.Skin?.Accent ?? string.Empty;
            gameplayControl.Skin.NoteSkin = chart.Skin?.Note ?? string.Empty;
            gameplayControl.Skin.ParticleSkin = chart.Skin?.Particle ?? string.Empty;
            gameplayControl.Skin.SingleLineSkin = chart.Skin?.SingleLine ?? string.Empty;
            gameplayControl.Skin.TrackSkin = chart.Skin?.Track ?? string.Empty;

            List<string> arcColor = new List<string>();
            List<string> arcColorLow = new List<string>();
            List<Color> finalColor = new List<Color>();
            List<Color> finalColorLow = new List<Color>();

            List<Color> defaultArc = gameplayControl.Skin.DefaultArcColors;
            List<Color> defaultArcLow = gameplayControl.Skin.DefaultArcLowColors;
            Color trace = gameplayControl.Skin.DefaultTraceColor;
            Color shadow = gameplayControl.Skin.DefaultShadowColor;

            if (chart.Colors != null)
            {
                arcColor = chart.Colors.Arc;
                arcColorLow = chart.Colors.ArcLow;
                chart.Colors.Trace.ConvertHexToColor(out trace);
                chart.Colors.Shadow.ConvertHexToColor(out shadow);
            }

            int definedColorCount = Mathf.Min(arcColor.Count, arcColorLow.Count);
            for (int i = 0; i < definedColorCount; i++)
            {
                arcColor[i].ConvertHexToColor(out Color high);
                arcColorLow[i].ConvertHexToColor(out Color low);
                finalColor.Add(high);
                finalColorLow.Add(low);
            }

            for (int i = definedColorCount; i < defaultArc.Count; i++)
            {
                finalColor.Add(defaultArc[i]);
                finalColorLow.Add(defaultArcLow[i]);
            }

            gameplayControl.Skin.SetTraceColor(trace);
            gameplayControl.Skin.SetArcColors(finalColor, finalColorLow);
            gameplayControl.Skin.SetShadowColor(shadow);

            ColorUtility.TryParseHtmlString(chart.DifficultyColor, out Color c);
            gameplayData.DifficultyColor.Value = c;

            bool enableVideoBackground = !string.IsNullOrEmpty(chart.VideoPath);
            Option<string> videoPath = chart.VideoPath == null ? null : level.GetRealPath(chart.VideoPath);
            if (enableVideoBackground && videoPath.HasValue)
            {
                gameplayData.LoadVideoBackground(videoPath.Value.Replace("\\", "/"), false);
            }
            else
            {
                gameplayData.LoadVideoBackground(null, false);
            }
        }
    }
}