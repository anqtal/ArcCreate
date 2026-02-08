using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcCreate.ChartFormat;
using ArcCreate.Gameplay;
using ArcCreate.Gameplay.Audio;
using ArcCreate.Utility.Extension;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ArcCreate.Storage
{
    public class GameplayLoader
    {
        private readonly GameplayManager gameplayControl;
        private readonly GameplayData gameplayData;

        public GameplayLoader(GameplayManager gameplayControl, GameplayData gameplayData)
        {
            this.gameplayControl = gameplayControl;
            this.gameplayData = gameplayData;
        }

        public async UniTask Load(SongList level, Difficulty difficulty)
        {
            await UniTask.DelayFrame(5);
            await SongDownloadService.EnsureDownloaded(level, difficulty);
            LoadMetadata(level, difficulty);
            LoadChart(level, difficulty);
            LoadScenecontrol(level, difficulty);

            // Avoid jacket flickering after reload
            var audioTask = LoadAudio(level, difficulty);
            var bgTask = LoadBackground(level, difficulty);

            await UniTask.WhenAll(audioTask, bgTask);
            await UniTask.WaitUntil(() => gameplayControl.IsLoaded);
        }

        private static async UniTask LoadAudio(SongList level, Difficulty difficulty)
        {
            var localPath = SongDownloadService.GetAudioPath(level.id);
            if (!File.Exists(localPath)) throw new Exception("Audio file is missing after download.");

            await BassAudioService.Instance.LoadAudioAsync(localPath);
        }

        private async UniTask LoadBackground(SongList level, Difficulty difficulty)
        {
            var bgId = SongDifficultyUtility.GetBackgroundPath(level);
            var localBgPath = DxResource.GetLocalPath(bgId, DxResource.FileType.BackgroundImage);
            if (!string.IsNullOrWhiteSpace(localBgPath) && File.Exists(localBgPath))
            {
                await gameplayData.LoadBackgroundFromHttp(new Uri(localBgPath));
                return;
            }

            if (string.IsNullOrWhiteSpace(bgId)) return;

            var bgPath = Path.Combine(Application.streamingAssetsPath, "bg", bgId + ".jpg");
            if (File.Exists(bgPath)) await gameplayData.LoadBackgroundFromHttp(new Uri(bgPath));
        }

        private void LoadScenecontrol(SongList level, Difficulty difficulty)
        {
            // change to touch
            Settings.InputMode.Value = (int)InputMode.Touch;
            var fileAccess = new SongFileAccessWrapper(level.id);

            gameplayControl.Scenecontrol.WaitForSceneLoad();
        }

        private void LoadChart(SongList level, Difficulty difficulty)
        {
            var fileAccess = new SongFileAccessWrapper(level.id);
            var reader = ChartReaderFactory.GetReader(fileAccess, SongDifficultyUtility.GetChartPath(difficulty));
            reader.Parse();
            BassAudioService.Instance.ChartEndTiming = reader.Events.Max(e => e.Timing + e.Length);
            BassAudioService.Instance.MyAnswerSoundPlayer = new AnswerSoundPlayer(reader.GuideSoundTiming);
            gameplayData.LoadChart(reader, "", fileAccess);
        }

        private void LoadMetadata(SongList level, Difficulty difficulty)
        {
            gameplayData.BaseBpm.Value = level.bpm_base;
            var skin = SongDifficultyUtility.GetSkin(level);
            gameplayControl.Skin.AlignmentSkin = skin;
            gameplayControl.Skin.AccentSkin = skin;
            gameplayControl.Skin.NoteSkin = skin;
            gameplayControl.Skin.ParticleSkin = skin;
            gameplayControl.Skin.SingleLineSkin = skin;
            gameplayControl.Skin.TrackSkin = skin;

            var arcColor = new List<string>();
            var arcColorLow = new List<string>();
            var finalColor = new List<Color>();
            var finalColorLow = new List<Color>();

            var defaultArc = gameplayControl.Skin.DefaultArcColors;
            var defaultArcLow = gameplayControl.Skin.DefaultArcLowColors;
            var trace = gameplayControl.Skin.DefaultTraceColor;
            var shadow = gameplayControl.Skin.DefaultShadowColor;

            // Keep default colors when playing remote songs.

            var definedColorCount = Mathf.Min(arcColor.Count, arcColorLow.Count);
            for (var i = 0; i < definedColorCount; i++)
            {
                arcColor[i].ConvertHexToColor(out var high);
                arcColorLow[i].ConvertHexToColor(out var low);
                finalColor.Add(high);
                finalColorLow.Add(low);
            }

            for (var i = definedColorCount; i < defaultArc.Count; i++)
            {
                finalColor.Add(defaultArc[i]);
                finalColorLow.Add(defaultArcLow[i]);
            }

            gameplayControl.Skin.SetTraceColor(trace);
            gameplayControl.Skin.SetArcColors(finalColor, finalColorLow);
            gameplayControl.Skin.SetShadowColor(shadow);

            ColorUtility.TryParseHtmlString(SongDifficultyUtility.GetDifficultyColor(difficulty), out var c);
            gameplayData.DifficultyColor.Value = c;

            gameplayData.LoadVideoBackground(null, false);
        }
    }
}