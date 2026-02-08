using ArcCreate.Gameplay.Audio.Practice;
using ArcCreate.SceneTransition;
using ArcCreate.Utility.Extension;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCreate.Gameplay.Audio
{
    public class PauseMenu : MonoBehaviour
    {
        public static bool IsPausing;
        [SerializeField] private StringSO retryCount;
        [SerializeField] private GameplayData gameplayData;
        [SerializeField] private PauseButton pauseButton;
        [SerializeField] private RectTransform pauseButtonRect;
        [SerializeField] private Camera uiCamera;
        [SerializeField] private GameObject pauseScreen;
        [SerializeField] private Button[] playButtons;
        [SerializeField] private Button[] retryButtons;
        [SerializeField] private Button[] returnButtons;
        [SerializeField] private PracticeMenu practiceMenu;
        [SerializeField] private PracticeTimingControl practiceTimingControl;
        [SerializeField] private GameObject pauseControl;
        [SerializeField] private GameObject normalLayout;
        [SerializeField] private GameObject reversedLayout;
        [SerializeField] private GameObject promptAudioConfigChange;
        private TransitionSequence retryTransition;

        private void Awake()
        {
            pauseButton.OnActivation.AddListener(OnPauseButton);
            foreach (var playButton in playButtons) playButton.onClick.AddListener(OnPlayButton);

            foreach (var retryButton in retryButtons) retryButton.onClick.AddListener(OnRetryButton);

            foreach (var returnButton in returnButtons) returnButton.onClick.AddListener(OnReturnButton);

            Application.focusChanged += OnFocusChange;
            gameplayData.EnablePracticeMode.OnValueChange += SetPracticeMode;
            SetPracticeMode(gameplayData.EnablePracticeMode.Value);

            Settings.SwitchResumeAndRetryPosition.OnValueChanged.AddListener(OnSwitchLayoutSettings);
            OnSwitchLayoutSettings(Settings.SwitchResumeAndRetryPosition.Value);

            retryTransition = new TransitionSequence()
                .OnShow()
                .AddTransition(new SoundTransition(TransitionScene.Sound.Retry))
                .OnBoth()
                .AddTransition(new TriangleTileTransition())
                .AddTransition(new PlayRetryCountTransition())
                .AddTransition(new DecorationTransition());

            if (Application.platform == RuntimePlatform.IPhonePlayer
                || Application.platform == RuntimePlatform.Android)
                AudioSettings.OnAudioConfigurationChanged += OnAudioConfig;
        }

        private void OnDestroy()
        {
            pauseButton.OnActivation.RemoveListener(OnPauseButton);
            foreach (var playButton in playButtons) playButton.onClick.RemoveListener(OnPlayButton);

            foreach (var retryButton in retryButtons) retryButton.onClick.RemoveListener(OnRetryButton);

            foreach (var returnButton in returnButtons) returnButton.onClick.RemoveListener(OnReturnButton);

            Application.focusChanged -= OnFocusChange;
            gameplayData.EnablePracticeMode.OnValueChange -= SetPracticeMode;

            if (Application.platform == RuntimePlatform.IPhonePlayer
                || Application.platform == RuntimePlatform.Android)
                AudioSettings.OnAudioConfigurationChanged -= OnAudioConfig;
        }

        private void OnSwitchLayoutSettings(bool reversed)
        {
            normalLayout.SetActive(!reversed);
            reversedLayout.SetActive(reversed);
        }

        private void OnAudioConfig(bool deviceWasChanged)
        {
            if (deviceWasChanged)
                //Services.Audio.Pause();
                promptAudioConfigChange.SetActive(true);
        }

        private void OnFocusChange(bool focused)
        {
            //OnPauseButton();
        }

        private void OnPauseButton()
        {
            // Hacky but whatever
            // if (Values.EnablePauseMenu
            // && (Services.Audio.IsPlayingAndNotStationary || (Services.Audio.AudioTiming >= Services.Audio.AudioLength - 1000)))
            // {
            //     int touchCount = Input.touchCount;
            //     for (int i = 0; i < touchCount; i++)
            //     {
            //         var touch = Input.GetTouch(i);
            //         if (!RectTransformUtility.RectangleContainsScreenPoint(pauseButtonRect, touch.position, uiCamera))
            //         {
            //             return;
            //         }
            //     }

            pauseScreen.SetActive(true);
            BassAudioService.Instance.AudioStream.Pause();
            BassAudioService.Instance.MyChartTimer.StopTiming();
            IsPausing = true;
            // }
        }

        private void OnPlayButton()
        {
            pauseScreen.SetActive(false);
            //Services.Audio.ResumeWithDelay(Values.DelayBeforeAudioResume, false);
            BassAudioService.Instance.AudioStream?.Resume();
            BassAudioService.Instance.MyChartTimer.StartTiming();
            Services.Judgement.RefreshInputHandler();
            DisablePauseButton().Forget();
            IsPausing = false;
        }

        private void OnRetryButton()
        {
            BassAudioService.Instance.MyChartTimer.StopTiming().ResetTiming();
            Values.RetryCount += 1;
            retryCount.Value = TextFormat.FormatRetryCount(Values.RetryCount + 1);
            pauseScreen.SetActive(false);
            Services.Judgement.RefreshInputHandler();
            StartRetry().Forget();
            IsPausing = false;
        }

        private async UniTask StartRetry()
        {
            await retryTransition.Show();
            //Services.Audio.AudioTiming = -Values.DelayBeforeAudioStart;
            await retryTransition.Hide();
            if (!pauseScreen.activeInHierarchy) Services.Audio.PlayWithDelay(0, Values.DelayBeforeAudioStart);

            await DisablePauseButton();
            IsPausing = false;
        }

        private async UniTask DisablePauseButton()
        {
            pauseButton.Interactable = false;
            await UniTask.Delay(1000);
            pauseButton.Interactable = true;
        }

        private void OnReturnButton()
        {
            BassAudioService.Instance.AudioStream?.Dispose();
            BassAudioService.Instance.MyChartTimer.StopTiming().ResetTiming();
            var transition = new TransitionSequence()
                .OnShow()
                .AddTransition(new TriangleTileTransition())
                .OnBoth()
                .AddTransition(new DecorationTransition());
            SceneTransitionManager.Instance.SetTransition(transition);
            SceneTransitionManager.Instance.SwitchScene(SceneNames.SelectScene).Forget();
        }

        private void SetPracticeMode(bool enable)
        {
            practiceMenu.gameObject.SetActive(enable);
            practiceTimingControl.gameObject.SetActive(enable);
            pauseControl.SetActive(!enable);
        }
    }
}