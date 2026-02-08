using System;
using System.Collections.Generic;
using System.IO;
using ArcCreate.ChartFormat;
using ArcCreate.Data;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ArcCreate.Gameplay
{
    /// <summary>
    ///     Scriptable object acting as data channel for scenes linking to gameplay scene.
    /// </summary>
    [CreateAssetMenu(fileName = "GameplayData", menuName = "ScriptableObject/GameplayData")]
    public class GameplayData : ScriptableObject
    {
        [SerializeField] private Sprite defaultJacket;
        private bool isUsingDefaultBackground = true;
        private bool isUsingDefaultJacket = true;

        public event Action OnChartFileLoad;

        public event Action OnSkinValuesChange;

        public event Action OnChartTimingEdit;

        public event Action OnChartCameraEdit;

        public event Action OnChartScenecontrolEdit;

        public event Action OnChartEdit;

        public event Action<int> OnGameplayUpdate;

        public event Action<PlayResult> OnPlayComplete;

        /// <summary>
        ///     Load the audio clip from the specified path.
        /// </summary>
        /// <param name="path">The path to load.</param>
        public void LoadAudio(string path)
        {
            if (AudioClip.Value != null) Destroy(AudioClip.Value);

            StartLoadingAudio(path).Forget();
        }

        /// <summary>
        ///     Load the background from specified file path.
        /// </summary>
        /// <param name="path">The path to load.</param>
        public void LoadBackground(string path)
        {
            // if (string.IsNullOrEmpty(path) || !File.Exists(path))
            // {
            //     Background.Value = Services.Skin.DefaultBackground;
            //     isUsingDefaultBackground = true;
            //     return;
            // }

            if (Background.Value != null && !isUsingDefaultBackground)
            {
                Destroy(Background.Value.texture);
                Destroy(Background.Value);
            }

            path = Path.Combine(Application.streamingAssetsPath, "bg", path + ".jpg");
            Debug.Log(path);
            var t = new Texture2D(1, 1);
            t.wrapMode = TextureWrapMode.Clamp;
            t.LoadImage(File.ReadAllBytes(path), true);
            var sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
            Background.Value = sprite;
            isUsingDefaultBackground = false;
        }

        /// <summary>
        ///     Load the jacket art from specified file path.
        /// </summary>
        /// <param name="path">The path to load.</param>
        public void LoadJacket(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                Jacket.Value = defaultJacket;
                isUsingDefaultJacket = true;
                return;
            }

            if (Jacket.Value != null && !isUsingDefaultJacket)
            {
                Destroy(Jacket.Value.texture);
                Destroy(Jacket.Value);
            }

            var t = new Texture2D(1, 1);
            t.wrapMode = TextureWrapMode.Clamp;
            t.LoadImage(File.ReadAllBytes(path), true);
            var sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
            Jacket.Value = sprite;
            isUsingDefaultJacket = false;
        }

        /// <summary>
        ///     Set the chart file for this system.
        /// </summary>
        /// <param name="reader">The chart reader defining the chart.</param>
        /// <param name="sfxParentFolder">The parent folder for loading custom SFX files.</param>
        /// <param name="fileAccess">Custom file accessor.</param>
        public void LoadChart(ChartReader reader, string sfxParentFolder, IFileAccessWrapper fileAccess = null)
        {
            Services.Chart.LoadChart(reader);
            //Services.Hitsound.LoadCustomSfxs(sfxParentFolder, fileAccess).Forget();
            OnChartFileLoad?.Invoke();
        }

        public void SetDefaultJacket()
        {
            Jacket.Value = defaultJacket;
            isUsingDefaultJacket = true;
        }

        public void SetDefaultBackground()
        {
            Background.Value = Services.Skin.DefaultBackground;
            isUsingDefaultBackground = true;
        }

        public async UniTask LoadAudioFromHttp(Uri uri, string ext)
        {
            if (AudioClip.Value != null) Destroy(AudioClip.Value);


            using (var req = UnityWebRequestMultimedia.GetAudioClip(
                       uri,
                       ext == ".ogg" ? AudioType.OGGVORBIS : AudioType.WAV))
            {
                Debug.Log(uri);
                await req.SendWebRequest();
                if (!string.IsNullOrWhiteSpace(req.error))
                    throw new IOException(I18n.S("Gameplay.Exception.LoadAudio", new Dictionary<string, object>
                    {
                        { "Path", uri },
                        { "Error", req.error }
                    }));

                AudioClip.Value = DownloadHandlerAudioClip.GetContent(req);
            }
        }

        public async UniTask LoadJacketFromHttp(Uri uri)
        {
            if (Jacket.Value != null && !isUsingDefaultJacket)
            {
                Destroy(Jacket.Value.texture);
                Destroy(Jacket.Value);
            }

            using (var req = UnityWebRequestTexture.GetTexture(uri))
            {
                await req.SendWebRequest();
                if (!string.IsNullOrWhiteSpace(req.error))
                {
                    Jacket.Value = defaultJacket;
                    isUsingDefaultJacket = true;

                    Debug.LogWarning(I18n.S("Gameplay.Exception.Skin", new Dictionary<string, object>
                    {
                        { "Path", uri },
                        { "Error", req.error }
                    }));
                    return;
                }

                var t = DownloadHandlerTexture.GetContent(req);
                var sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
                Jacket.Value = sprite;
                isUsingDefaultJacket = false;
            }
        }

        public async UniTask LoadBackgroundFromHttp(Uri uri)
        {
            if (Background.Value != null && !isUsingDefaultBackground)
            {
                Destroy(Background.Value.texture);
                Destroy(Background.Value);
            }

            using var req = UnityWebRequestTexture.GetTexture(uri);
            await req.SendWebRequest();
            if (!string.IsNullOrWhiteSpace(req.error))
            {
                Background.Value = Services.Skin.DefaultBackground;
                isUsingDefaultBackground = true;

                Debug.LogWarning(I18n.S("Gameplay.Exception.Skin", new Dictionary<string, object>
                {
                    { "Path", uri },
                    { "Error", req.error }
                }));
                return;
            }

            var t = DownloadHandlerTexture.GetContent(req);
            var sprite = Sprite.Create(t, new Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f));
            Background.Value = sprite;
            isUsingDefaultBackground = false;
        }

        public void LoadVideoBackground(string path, bool isUri)
        {
            Services.Skin.SetVideoBackground(path, isUri);
        }

        internal async UniTask StartLoadingAudio(string path)
        {
            using (var req = UnityWebRequestMultimedia.GetAudioClip(
                       new Uri(path),
                       path.EndsWith("wav") ? AudioType.WAV : AudioType.OGGVORBIS))
            {
                await req.SendWebRequest();
                if (!string.IsNullOrWhiteSpace(req.error))
                    throw new IOException(I18n.S("Gameplay.Exception.LoadAudio", new Dictionary<string, object>
                    {
                        { "Path", path },
                        { "Error", req.error }
                    }));

                AudioClip.Value = DownloadHandlerAudioClip.GetContent(req);
            }
        }

        internal void NotifySkinValuesChange()
        {
            if (isUsingDefaultBackground) SetDefaultBackground();

            OnSkinValuesChange?.Invoke();
        }

        internal void NotifyChartTimingEdit()
        {
            OnChartTimingEdit?.Invoke();
        }

        internal void NotifyChartCameraEdit()
        {
            OnChartCameraEdit?.Invoke();
        }

        internal void NotifyChartScenecontrolEdit()
        {
            OnChartScenecontrolEdit?.Invoke();
        }

        internal void NotifyChartEdit()
        {
            OnChartEdit?.Invoke();
        }

        internal void NotifyUpdate(int currentTiming)
        {
            OnGameplayUpdate?.Invoke(currentTiming);
        }

        internal void NotifyPlayComplete(PlayResult result)
        {
            OnPlayComplete?.Invoke(result);
        }

#pragma warning disable
        /// <summary>
        ///     The background sprite.
        /// </summary>
        public State<Sprite> Background { get; } = new();

        /// <summary>
        ///     The jacket art sprite.
        /// </summary>
        public State<Sprite> Jacket { get; } = new();

        /// <summary>
        ///     The song's title.
        /// </summary>
        public State<string> Title { get; } = new();

        /// <summary>
        ///     The composer's name.
        /// </summary>
        public State<string> Composer { get; } = new();

        /// <summary>
        ///     The illustrator's name.
        /// </summary>
        public State<string> Illustrator { get; } = new();

        /// <summary>
        ///     The charter's name.
        /// </summary>
        public State<string> Charter { get; } = new();

        /// <summary>
        ///     The charter's alias.
        /// </summary>
        public State<string> Alias { get; } = new();

        /// <summary>
        ///     The text of the difficulty display.
        /// </summary>
        public State<string> DifficultyName { get; } = new();

        /// <summary>
        ///     The color of the difficulty text's background image.
        /// </summary>
        public State<Color> DifficultyColor { get; } = new();

        /// <summary>
        ///     The audio clip to play.
        /// </summary>
        public State<AudioClip> AudioClip { get; } = new();

        /// <summary>
        ///     The audio offset value per chart.
        ///     Use <see cref="Settings.GlobalAudioOffset" /> for global audio offset.
        ///     Setting this value will cause a score reset.
        /// </summary>
        public State<int> AudioOffset { get; } = new();

        /// <summary>
        ///     The base bpm value.
        ///     Setting this value will cause a score reset.
        /// </summary>
        public State<float> BaseBpm { get; } = new();

        /// <summary>
        ///     The timing point density factor value.
        ///     Setting this value will cause a score reset.
        /// </summary>
        public State<float> TimingPointDensityFactor { get; } = new();

        /// <summary>
        ///     The url to be played by video background renderer.
        ///     Setting it to null or empty string will disable the renderer.
        /// </summary>
        public State<string> VideoBackgroundUrl { get; } = new();

        /// <summary>
        ///     Whether or not to enable practice mode for gameplay scene.
        /// </summary>
        public State<bool> EnablePracticeMode { get; } = new(false);

        /// <summary>
        ///     Whether or not to force enable autoplay mode for gameplay scene.
        /// </summary>
        public State<bool> EnableAutoplayMode { get; } = new(false);

        /// <summary>
        ///     The audio playback speed.
        /// </summary>
        public State<float> PlaybackSpeed { get; } = new(1);
#pragma warning restore
    }
}