using System;
using ArcCreate.Gameplay;
using ArcCreate.Gameplay.Audio;
using ArcCreate.Gameplay.Auth;
using ArcCreate.Storage;
using ArcCreate.Utility.Animation;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCreate.SceneTransition
{
    public class GreetingScene : SceneRepresentative
    {
        [SerializeField] private Button button;
        [SerializeField] private GameObject clickToStartText;
        [SerializeField] private ScriptedAnimator startupAnimator;
        [SerializeField] private ScriptedAnimator proceedAnimator;
        private TMP_Text clickToStartLabel;
        private float lastStatusTime;

        private void OnDestroy()
        {
        }

        protected override void OnSceneLoad()
        {
            TransitionScene.Instance.TriangleTileGameObject.SetActive(true);
            TransitionScene.Instance.UpdateCameraStatus();
            startupAnimator.Show();
            TransitionScene.Instance.EnterGreetingScene();
            button.interactable = false;
            if (clickToStartText != null) clickToStartLabel = clickToStartText.GetComponent<TMP_Text>();
            lastStatusTime = 0f;
            ShowStatus("Initializing...").Forget();
            InitializeSystems().Forget();
        }

        private void Transition()
        {
            button.gameObject.SetActive(false);
            if (clickToStartText != null) clickToStartText.SetActive(false);
            TransitionScene.Instance.EnterSelectScene();
            Shader.WarmupAllShaders();
            StartTransition().Forget();
        }

        private async UniTaskVoid InitializeSystems()
        {
            _ = BassAudioService.Instance;
            await ShowStatus("Loading resources...");
            await DxResource.Init();
            await ShowStatus("Checking login...");
            LoginDialog.ShowIfNeeded();
            if (string.IsNullOrWhiteSpace(LoginState.UserId))
            {
                await ShowStatus("Waiting for user ID...");
                await UniTask.WaitUntil(() => !string.IsNullOrWhiteSpace(LoginState.UserId));
            }

            await ShowStatus("Loading pack list...");
            await PackData.Instance.WaitUntilLoaded();
            await ShowStatus("Entering selection...");
            AutoTransition();
        }

        private void AutoTransition()
        {
            button.gameObject.SetActive(false);
            if (clickToStartText != null) clickToStartText.SetActive(false);
            TransitionScene.Instance.EnterSelectScene();
            Shader.WarmupAllShaders();
            StartTransition().Forget();
        }

        private async UniTask ShowStatus(string message)
        {
            if (clickToStartText == null || clickToStartLabel == null) return;

            clickToStartText.SetActive(true);
            clickToStartLabel.text = message;
            var now = Time.realtimeSinceStartup;
            var wait = Mathf.Max(0f, 1f - (now - lastStatusTime));
            lastStatusTime = now + wait;
            if (wait > 0f) await UniTask.Delay(TimeSpan.FromSeconds(wait));
        }

        private async UniTask StartTransition()
        {
            proceedAnimator.Hide();
            await UniTask.Delay((int)(proceedAnimator.Length * 1000));
            SceneTransitionManager.Instance.SwitchScene(SceneNames.SelectScene).Forget();
        }
    }
}