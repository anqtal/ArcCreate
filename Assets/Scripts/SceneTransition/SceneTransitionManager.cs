using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ArcCreate.SceneTransition
{
    public enum TransitionState
    {
        Idle,
        Ending,
        Starting,
        Waiting,
        ReadyToEnd
    }

    // Code yoinked from ArcCore

    /// <summary>
    ///     Manager for scene transitioning. Allows for easy data transfer between scenes.
    /// </summary>
    public class SceneTransitionManager : MonoBehaviour
    {
        private static SceneRepresentative currentSceneRepresentative;
        private static string currentScene;
        private readonly List<(string sceneName, SceneRepresentative representative)> additivelyLoadedScenes = new();

        private SceneRepresentative loadingSceneRep;
        private TransitionSequence transition;

        public static SceneTransitionManager Instance { get; private set; }

        public Action OnTransitionEnd { get; set; }

        public TransitionState TransitionState { get; private set; } = TransitionState.Idle;

        public bool IsTransitioning => TransitionState != TransitionState.Idle;

        public bool SceneRegistered => currentSceneRepresentative != null;

        private void Awake()
        {
            Instance = this;
            Time.timeScale = 1;
            LoadDefaultScene().Forget();
        }

        /// <summary>
        ///     Called if game started without boot scene.
        /// </summary>
        /// <param name="rep">The representative to be set as active.</param>
        public static void StartBootSceneDev(SceneRepresentative rep)
        {
            currentSceneRepresentative = rep;
            currentScene = rep.gameObject.scene.name;
        }

        public void SetTransition(TransitionSequence transition)
        {
            this.transition = transition;
        }

        /// <summary>
        ///     Start the transition and switch to a new scene.
        ///     Load the scene defined by sceneName, and unload the currently active scene.
        /// </summary>
        /// <param name="sceneName">The scene to switch to.</param>
        /// <param name="passData">Action for passing data between old and new scene.</param>
        /// <param name="onException">Action for when exception occurs while passing data.</param>
        /// <returns>UniTask instance.</returns>
        public async UniTask SwitchScene(string sceneName, Func<SceneRepresentative, UniTask> passData = null,
            Action<Exception> onException = null)
        {
            await UniTask.WaitUntil(() => TransitionState == TransitionState.Idle);
            TransitionState = TransitionState.Starting;

            if (transition != null)
            {
                await transition.Show();
                TransitionState = TransitionState.Waiting;
            }

            var waitTask = UniTask.Delay(transition?.WaitDurationMs ?? 0);
            var rep = await LoadScene(sceneName);
            Exception ex = null;

            try
            {
                if (passData != null) await passData.Invoke(rep);

                UnloadCurrentScene();
                currentScene = sceneName;
                currentSceneRepresentative = rep;
            }
            catch (Exception e)
            {
                ex = e;
                Debug.LogError(e);
                await SceneManager.UnloadSceneAsync(sceneName);
            }
            finally
            {
                await UniTask.WaitUntil(() => waitTask.Status == UniTaskStatus.Succeeded);
                TransitionState = TransitionState.Ending;

                await UniTask.NextFrame();
                if (transition != null) await transition.Hide();

                if (ex != null) onException?.Invoke(ex);

                OnTransitionEnd?.Invoke();
                OnTransitionEnd = null;
                TransitionState = TransitionState.Idle;
            }
        }

        /// <summary>
        ///     Additively load a new scene.
        ///     All additively loaded scene are destroyed along with the currently active scene.
        /// </summary>
        /// <param name="sceneName">The scene to load.</param>
        /// <param name="passData">Action for passing data between scenes.</param>
        /// <returns>Unitask instance.</returns>
        public async UniTask LoadSceneAdditive(string sceneName, Action<SceneRepresentative> passData = null)
        {
            await LoadScene(sceneName).ContinueWith(rep =>
            {
                passData?.Invoke(rep);
                additivelyLoadedScenes.Add((sceneName, rep));
            });
        }

        /// <summary>
        ///     Called by SceneRepresentative on awake, to notify that the scene has completely loaded.
        /// </summary>
        /// <param name="rep">The representative of the loaded scene.</param>
        public void LoadSceneComplete(SceneRepresentative rep)
        {
            loadingSceneRep = rep;
        }

        private async UniTask LoadDefaultScene()
        {
            await I18n.Initialize();

            foreach (var scene in SceneNames.RequiredScenes) SceneManager.LoadScene(scene, LoadSceneMode.Additive);

            if (SceneManager.sceneCount == 1 + SceneNames.RequiredScenes.Length)
            {
                SceneManager.LoadScene(SceneNames.DefaultScene, LoadSceneMode.Additive);
                currentScene = SceneNames.DefaultScene;
            }
        }

        private async UniTask<SceneRepresentative> LoadScene(string sceneName)
        {
            loadingSceneRep = null;
            var load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            while (!load.isDone) await UniTask.Yield();

            while (loadingSceneRep == null) await UniTask.Yield();

            return loadingSceneRep;
        }

        private void UnloadCurrentScene()
        {
            foreach (var (sceneName, representative) in additivelyLoadedScenes)
            {
                representative.OnUnloadScene();
                SceneManager.UnloadSceneAsync(sceneName);
            }

            additivelyLoadedScenes.Clear();

            if (currentSceneRepresentative != null) currentSceneRepresentative.OnUnloadScene();

            if (currentScene != null) SceneManager.UnloadSceneAsync(currentScene);
        }
    }
}