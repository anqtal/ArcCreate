using ArcCreate.Gameplay;
using ArcCreate.Selection.SoundEffect;
using ArcCreate.Storage;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace ArcCreate.Selection
{
    internal class Services : MonoBehaviour
    {
        [SerializeField] private SoundEffectService soundEffect;

        public static ISoundEffectService SoundEffect { get; set; }

        private void Awake()
        {
            SoundEffect = soundEffect;
            InitializeAsync().Forget();
            EnsureSingleEventSystem();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureSingleEventSystem();
        }

        private async UniTaskVoid InitializeAsync()
        {
            await DxResource.Init();
            var songData = SongData.Instance.Songs;
            var packData = PackData.Instance.Packs;
        }

        private static void EnsureSingleEventSystem()
        {
            var systems = FindObjectsOfType<EventSystem>(true);
            if (systems == null || systems.Length <= 1) return;

            EventSystem keep = null;
            for (var i = 0; i < systems.Length; i++)
                if (systems[i] != null && systems[i].isActiveAndEnabled)
                {
                    keep = systems[i];
                    break;
                }

            if (keep == null) keep = systems[0];

            keep.gameObject.SetActive(true);
            for (var i = 0; i < systems.Length; i++)
                if (systems[i] != null && systems[i] != keep)
                    Destroy(systems[i].gameObject);
        }
    }
}