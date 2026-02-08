using System.Globalization;
using UnityEngine;

namespace ArcCreate
{
    public static class Settings
    {
        public static readonly StringSetting Locale = new("System.Locale", null);

        // Gameplay
        public static readonly IntSetting DropRate = new("DropRate", 150, 0);
        public static readonly BoolSetting ShowEarlyLatePerfect = new("ShowEarlyLate", true);
        public static readonly BoolSetting EnableColorblind = new("EnableColorblind", false);

        public static readonly IntSetting FrPmIndicatorPosition = new(
            "IndicatorPosition",
            (int)(Application.isMobilePlatform ? FrPmPosition.Middle : FrPmPosition.Off));

        public static readonly BoolSetting DisableAdvancedGraphics = new("DisableAdvancedGraphics", false);

        public static readonly BoolSetting EnableMaxIndicator = new("EnableMaxIndicator", false);
        public static readonly IntSetting LateEarlyTextPosition = new("LateEarlyTextPosition", 0);
        public static readonly IntSetting ViewportAspectRatioSetting = new("ViewportAspectRatioSetting", 0);
        public static readonly BoolSetting ShowGameplayDebug = new("ShowGameplayDebug", false);
        public static readonly IntSetting InputMode = new("Gameplay.InputMode", 0);
        public static readonly IntSetting ForceTheme = new("UI.ForceTheme", 0);
        public static readonly IntSetting ScoreDisplayMode = new("UI.ScoreDisplayMode", 0);
        public static readonly BoolSetting SwitchResumeAndRetryPosition = new("UI.SwitchResumeAndRetryPosition", false);
        public static readonly BoolSetting MirrorNotes = new("Gameplay.Mirror", false);
        public static readonly BoolSetting HidePause = new("Gameplay.HidePause", false);
        public static readonly IntSetting PauseButtonMode = new("Gameplay.PauseMode", 0);

        // Judgement
        public static readonly BoolSetting ShowMaxJudgement = new("Gameplay.Judgement.ShowMax", true);
        public static readonly BoolSetting ShowPerfectJudgement = new("Gameplay.Judgement.ShowPerfect", true);
        public static readonly BoolSetting ShowGoodJudgement = new("Gameplay.Judgement.ShowGood", true);
        public static readonly BoolSetting ShowMissJudgement = new("Gameplay.Judgement.ShowMiss", true);
        public static readonly BoolSetting DisplayMsDifference = new("Gameplay.Judgement.DisplayMsDifference", false);

        // Audio
        public static readonly IntSetting GlobalAudioOffset = new("GlobalAudioOffset", 0);
        public static readonly FloatSetting MusicAudio = new("SoundPreferences.ChartAudio", 1f, 0, 1);
        public static readonly FloatSetting EffectAudio = new("SoundPreferences.EffectAudio", 0.4f, 0, 2);
        public static readonly IntSetting GuideAudio = new("SoundPreferences.GuideAudio", 5, 0, 10);

        // Display
        public static readonly IntSetting Framerate = new("DisplayFramerate", -1, 0, 360);
        public static readonly BoolSetting VSync = new("EnableVSync", false);
        public static readonly BoolSetting LimitFrameRate = new("LimitFrameRate", false);
        public static readonly BoolSetting ShowFPSCounter = new("ShowFrameCounter", false);

        // Input
        public static readonly IntSetting GridSlot = new("GridSlot", 0);
        public static readonly FloatSetting ScrollSensitivityVertical = new("Scroll.Vertical", 200);
        public static readonly FloatSetting ScrollSensitivityHorizontal = new("Scroll.Hozirontal", 100);
        public static readonly FloatSetting ScrollSensitivityTimeline = new("Scroll.Timeline", 0.2f);

        public static readonly FloatSetting TrackScrollThreshold = new("Scroll.TrackThreshold", 0.2f);
        public static readonly IntSetting TrackScrollMaxMovement = new("Scroll.MaxTiming", 200);
        public static readonly FloatSetting CameraSensitivity = new("CameraSensitivity", 10);
        public static readonly FloatSetting GridBpmLimit = new("GridBpmLimit", 1000);
        public static readonly BoolSetting ScenecontrolAutoRebuild = new("ScenecontrolAutoRebuild", false);

        // Export
        public static readonly IntSetting ChartSortMode = new("ChartSortMode", 0);
        public static readonly StringSetting FFmpegPath = new("RenderPreferences.FFmpegPath", "ffmpeg");
        public static readonly StringSetting LastUsedPublisherName = new("Editor.Export.LastUsedPublisherName", null);

        // Selection
        public static readonly StringSetting SelectionGroupStrategy = new("Selection.Group", "none");
        public static readonly StringSetting SelectionSortStrategy = new("Selection.Sort", "title");
        public static readonly StringSetting SelectionSortPackStrategy = new("Selection.SortPack", "addeddate");

        // Editor
        public static readonly BoolSetting ShouldAutosave = new("Editor.Autosave.Enable", true);
        public static readonly IntSetting AutosaveInterval = new("Editor.Autosave.Interval", 300, 10);
        public static readonly BoolSetting ShouldBackup = new("Editor.Backup.Enable", true);
        public static readonly IntSetting BackupCount = new("Editor.Backup.Count", 10, 1);
        public static readonly BoolSetting SyncToDSPTime = new("Editor.SyncToDSPTime", false);
        public static readonly BoolSetting AllowCreatingNotesBackward = new("Editor.AllowCreatingNotesBackward", true);
        public static readonly BoolSetting BlockOverlapNoteCreation = new("Editor.BlockOverlapNote", true);
        public static readonly BoolSetting EnableEasterEggs = new("Fun.EasterEggs", Application.isEditor);
        public static readonly BoolSetting UseNativeFileBrowser = new("Editor.UseNativeFileBrowser", false);

        public static readonly BoolSetting EnableKeybindHintDisplay = new("Editor.Navigation.KeybindHint", true);
        public static readonly BoolSetting EnableArctapWidthEditing = new("Editor.Secret.ArctapWidth", false);

        [RuntimeInitializeOnLoadMethod]
        public static void OnInitialize()
        {
            if (Application.isMobilePlatform)
            {
                LimitFrameRate.OnValueChanged.AddListener(value =>
                    Application.targetFrameRate = value ? 60 : Screen.currentResolution.refreshRate);
                Application.targetFrameRate = LimitFrameRate.Value ? 60 : Screen.currentResolution.refreshRate;

                QualitySettings.vSyncCount = 0;
            }
            else
            {
                Framerate.OnValueChanged.AddListener(value => Application.targetFrameRate = value);
                Application.targetFrameRate = Framerate.Value;

                VSync.OnValueChanged.AddListener(value => QualitySettings.vSyncCount = value ? 1 : 0);
                QualitySettings.vSyncCount = VSync.Value ? 1 : 0;
            }

            Application.quitting += OnApplicationQuit;
            CultureInfo.CurrentCulture = new CultureInfo("en");
        }

        private static void OnApplicationQuit()
        {
            if (!Application.isEditor) PlayerPrefs.Save();
        }
    }
}