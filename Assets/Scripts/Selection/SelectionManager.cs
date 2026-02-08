using ArcCreate.Gameplay;
using ArcCreate.SceneTransition;
using ArcCreate.Storage;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace ArcCreate.Selection
{
    public class SelectionManager : SceneRepresentative
    {
        [SerializeField] private GameplayData gameplayData;
        [SerializeField] private StorageData storageData;
        [SerializeField] private Camera selectionCamera;
        private bool isSubscribed;

        private void OnDestroy()
        {
            storageData.SelectedPack.OnValueChange -= OnPackChange;
            storageData.SelectedChart.OnValueChange -= OnChartChange;
            if (isSubscribed) SongData.Instance.OnLoaded -= OnSongDataLoaded;
        }

        protected override void OnSceneLoad()
        {
            storageData.SelectedPack.OnValueChange += OnPackChange;
            storageData.SelectedChart.OnValueChange += OnChartChange;
            InitializeAsync().Forget();
            TransitionScene.Instance.TriangleTileGameObject.SetActive(true);
            TransitionScene.Instance.UpdateCameraStatus();
            TransitionScene.Instance.EnsureDefaultTriangleScale();
        }

        private async UniTaskVoid InitializeAsync()
        {
            await DxResource.Init();
            SongData.Instance.OnLoaded += OnSongDataLoaded;
            isSubscribed = true;
            if (SongData.Instance.IsLoaded) OnSongDataLoaded();
        }

        private void OnSongDataLoaded()
        {
            storageData.NotifyStorageChange();
        }

        private void OnPackChange(Pack pack)
        {
            PlayerPrefs.SetString("Selection.LastPack", pack?.id);
        }

        private void OnChartChange((SongList level, Difficulty difficulty) obj)
        {
            var (level, chart) = obj;
            if (level != null && chart != null)
            {
                PlayerPrefs.SetString($"Selection.LastLevel.{storageData.SelectedPack.Value?.id ?? "all"}", level.id);
                PlayerPrefs.SetString("Selection.LastChartPath", SongDifficultyUtility.GetChartPath(chart));
                PlayerPrefs.SetString("Selection.LastDifficultyName", SongDifficultyUtility.GetDifficultyName(chart));
            }
        }
    }
}