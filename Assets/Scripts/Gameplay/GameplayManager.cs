using System;
using System.IO;
using ArcCreate.ChartFormat;
using ArcCreate.Gameplay.Audio;
using ArcCreate.Gameplay.Chart;
using ArcCreate.Gameplay.GameplayCamera;
using ArcCreate.Gameplay.Scenecontrol;
using ArcCreate.Gameplay.Skin;
using ArcCreate.SceneTransition;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ArcCreate.Gameplay
{
    /// <summary>
    ///     Gameplay loop.
    /// </summary>
    public class GameplayManager : SceneRepresentative
    {
        [SerializeField] private ChartService chartService;
        [SerializeField] private SkinService skinService;
        [SerializeField] private AudioService audioService;
        [SerializeField] private CameraService cameraService;
        [SerializeField] private ScenecontrolService scenecontrolService;
        [SerializeField] private AudioClip testAudio;
        [SerializeField] private GameplayData gameplayData;
        [SerializeField] private Camera backgroundCamera;
        [SerializeField] private Camera overlayCamera;
        [SerializeField] private string testPlayChartFileName = "test.aff";

        public ChartService Chart => chartService;

        public SkinService Skin => skinService;

        public AudioService Audio => audioService;

        public CameraService Camera => cameraService;

        public ScenecontrolService Scenecontrol => scenecontrolService;

        public bool IsLoaded =>
            Services.Chart.IsLoaded
            && Services.Scenecontrol.IsLoaded
            && Services.Render.IsLoaded
            && Services.Audio.IsReadyForUpdate;

        public bool EnablePauseMenu
        {
            get => Values.EnablePauseMenu;
            set => Values.EnablePauseMenu = value;
        }

        public bool ShouldNotifyOnAudioEnd
        {
            get => Values.ShouldNotifyOnAudioEnd;
            set => Values.ShouldNotifyOnAudioEnd = value;
        }

        private void Update()
        {
            if (!IsLoaded) return;

            Services.Audio.UpdateTime();
            Services.Particle.UpdateParticles();
            Services.InputFeedback.UpdateInputFeedback();

            var currentTiming = Services.Audio.ChartTiming;

            Services.Chart.UpdateChartJudgement(currentTiming);
            Services.Judgement.ProcessInput(currentTiming);

            Services.Score.UpdateScore(currentTiming);
            Services.Scenecontrol.UpdateScenecontrol(currentTiming);
            Services.Camera.UpdateCamera(currentTiming);
            Services.Chart.UpdateChartRender(currentTiming);
            Services.Score.UpdateDisplay();
            //Services.Hitsound.UpdateHitsoundHistory(currentTiming);
            Services.Render.UpdateRenderers();

            gameplayData.NotifyUpdate(currentTiming);
        }

        public void SetCameraViewportRect(Rect rect)
        {
            backgroundCamera.rect = rect;
            overlayCamera.rect = rect;
            Values.ScreenSize = new Vector2(backgroundCamera.pixelWidth, backgroundCamera.pixelHeight);
        }

        public void SetCameraEnabled(bool enable)
        {
            backgroundCamera.enabled = enable;
            overlayCamera.enabled = enable;
        }

        public void SetEnableArcDebug(bool enable)
        {
            Services.Judgement.SetDebugDisplayMode(enable);
        }

        public override void OnNoBootScene()
        {
            // Load test chart
            var path = Path.Combine(Application.streamingAssetsPath, testPlayChartFileName);
            if (Application.platform == RuntimePlatform.Android)
                ImportTestChartAndroid(path).Forget();
            else
                ImportTestChart(path);

            Settings.InputMode.Value = (int)InputMode.Auto;
            Services.Scenecontrol.WaitForSceneLoad();
        }

        protected override void OnSceneLoad()
        {
            if (Application.platform == RuntimePlatform.Android
                || Application.platform == RuntimePlatform.IPhonePlayer)
                Settings.InputMode.Value = (int)InputMode.Touch;

            Time.timeScale = 1;
            Services.Judgement.SetDebugDisplayMode(Settings.ShowGameplayDebug.Value);
            Values.RetryCount = 0;
        }

        private async UniTask ImportTestChartAndroid(string path)
        {
            var www = UnityWebRequest.Get(path);
            await www.SendWebRequest();

            if (!string.IsNullOrWhiteSpace(www.error)) throw new Exception("Cannot load test chart file");

            var data = www.downloadHandler.data;
            var copyPath = Path.Combine(Application.temporaryCachePath, "test_arc.aff");
            using (var fs = new FileStream(copyPath, FileMode.OpenOrCreate, FileAccess.Write))
            {
                fs.Write(data, 0, data.Length);
            }

            ImportTestChart(copyPath);
            File.Delete(copyPath);
        }

        private void ImportTestChart(string path)
        {
            var reader = ChartReaderFactory.GetReader(new PhysicalFileAccess(), path);
            reader.Parse();

            gameplayData.AudioClip.Value = testAudio;
            chartService.LoadChart(reader);

            Audio.PlayWithDelay(0, Values.DelayBeforeAudioStart);
        }
    }
}