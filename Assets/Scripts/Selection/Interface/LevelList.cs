using System.Linq;
using ArcCreate.Gameplay.Score;
using ArcCreate.Storage;
using ArcCreate.Utility.InfiniteScroll;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCreate.Selection.Interface
{
    public class LevelList : MonoBehaviour
    {
        private static bool lastWasInLevelList;

        [SerializeField] private StorageData storageData;
        [SerializeField] private InfiniteScroll scroll;
        [SerializeField] private LevelListOptions options;
        [SerializeField] private RectTransform scrollRect;
        [SerializeField] private GameObject levelCellPrefab;
        [SerializeField] private GameObject difficultyCellPrefab;
        [SerializeField] private GameObject groupCellPrefab;
        [SerializeField] private float levelCellSize;
        [SerializeField] private float groupCellSize;
        [SerializeField] private float autoScrollDuration = 1f;
        [SerializeField] private float rebuildDuration = 0.3f;
        [SerializeField] private PackList packList;
        [SerializeField] private Button randomButton;
        [SerializeField] private Button jumpToTopButton;
        [SerializeField] private Button jumpToBottomButton;
        private Difficulty currentChart;
        private Pack currentPack;
        private SongList currentSong;
        private Tween scrollTween;

        public static float LevelCellSize { get; set; }

        public static float GroupCellSize { get; set; }

        private void Awake()
        {
            scroll.Value = 0;
            options.Setup();
            Pools.New<Cell>("LevelCell", levelCellPrefab, scroll.transform, 5);
            Pools.New<DifficultyCell>("DifficultyCell", difficultyCellPrefab, scroll.transform, 30);
            Pools.New<Cell>("GroupCell", groupCellPrefab, scroll.transform, 3);

            storageData.OnStorageChange += OnStorageChange;
            storageData.SelectedChart.OnValueChange += OnSelectedChart;
            storageData.SelectedPack.OnValueChange += OnSelectedPack;
            options.OnNeedRebuild += RebuildList;
            ScoreCache.OnUpdated += OnScoreCacheUpdated;

            randomButton.onClick.AddListener(SelectRandom);
            jumpToTopButton.onClick.AddListener(SelectTop);
            jumpToBottomButton.onClick.AddListener(SelectBottom);

            LevelCellSize = levelCellSize;
            GroupCellSize = groupCellSize;

            scroll.OnPointerEvent += KillTween;

            if (storageData.IsLoaded && lastWasInLevelList) OnStorageChange();
        }

        private void OnDestroy()
        {
            Pools.Destroy<Cell>("LevelCell");
            Pools.Destroy<DifficultyCell>("DifficultyCell");
            Pools.Destroy<Cell>("GroupCell");

            storageData.OnStorageChange -= OnStorageChange;
            storageData.SelectedChart.OnValueChange -= OnSelectedChart;
            storageData.SelectedPack.OnValueChange -= OnSelectedPack;
            options.OnNeedRebuild -= RebuildList;
            ScoreCache.OnUpdated -= OnScoreCacheUpdated;

            randomButton.onClick.RemoveListener(SelectRandom);
            jumpToTopButton.onClick.RemoveListener(SelectTop);
            jumpToBottomButton.onClick.RemoveListener(SelectBottom);

            scroll.OnPointerEvent -= KillTween;
        }

        private void OnStorageChange()
        {
            currentPack = storageData.SelectedPack.Value;
            (currentSong, currentChart) = storageData.SelectedChart.Value;
            RebuildList();
        }

        private void OnSelectedPack(Pack pack)
        {
            lastWasInLevelList = true;
            if (pack == null)
            {
                var (level, chart) = storageData.GetLastSelectedChart(null);
                if (level != null && chart != null)
                {
                    currentSong = level;
                    storageData.SelectedChart.Value = (level, chart);
                }

                RebuildList();
                currentPack = pack;
                return;
            }

            var packSongs = StorageData.GetSongsForPack(pack);
            var found = false;
            foreach (var level in packSongs)
                if (currentSong != null && InterfaceUtility.AreTheSame(level, currentSong))
                    found = true;

            if (!found)
            {
                var (level, chart) = storageData.GetLastSelectedChart(pack?.id);
                if (level != null && chart != null)
                {
                    currentSong = level;
                    storageData.SelectedChart.Value = (level, chart);
                }
            }
            else
            {
                RebuildList();
            }

            currentPack = pack;
        }

        private void OnSelectedChart((SongList, Difficulty) obj)
        {
            var (level, chart) = obj;
            var chartChanged = !SongDifficultyUtility.IsSameDifficulty(chart, currentChart);
            var packChanged = storageData.SelectedPack.Value != currentPack;
            if (chartChanged || packChanged) RebuildList();

            FocusOnLevel(level);
            currentChart = chart;
            currentSong = level;
        }

        private void OnScoreCacheUpdated()
        {
            RebuildList();
        }

        private void RebuildList()
        {
            if (!lastWasInLevelList) return;

            var prevCount = scroll.Data.Count;
            var levels = storageData.SelectedPack.Value == null
                ? StorageData.GetAllSongs().ToList()
                : StorageData.GetSongsForPack(storageData.SelectedPack.Value);

            if (levels?.Count == 0)
            {
                packList.BackToPackList();
                return;
            }

            if (string.IsNullOrWhiteSpace(options.SearchQuery))
            {
                var data = LevelListBuilder.Build(
                    levels,
                    storageData.SelectedChart.Value.difficulty,
                    options.GroupStrategy,
                    options.SortStrategy);
                scroll.SetDataWithoutRebuild(data);
            }
            else
            {
                var data = LevelListBuilder.Filter(levels, storageData.SelectedChart.Value.difficulty,
                    options.SearchQuery);
                scroll.SetDataWithoutRebuild(data);
            }

            // Only play animation on the second load onward
            if (prevCount > 0)
            {
                scrollRect.anchorMin = new Vector2(-0.4f, 0);
                scrollRect.DOAnchorMin(Vector2.zero, rebuildDuration).SetEase(Ease.OutCubic);
            }

            FocusOnLevelImmediate(currentSong);
        }

        private void FocusOnLevel(SongList level)
        {
            HierarchyData item = null;
            if (level == null) return;

            for (var i = 0; i < scroll.Data.Count; i++)
            {
                var data = scroll.Data[i];
                if (data is LevelCellData levelCell && InterfaceUtility.AreTheSame(levelCell.Song, level))
                {
                    item = scroll.Hierarchy[i];
                    break;
                }
            }

            if (item == null) return;

            var scrollFrom = scroll.Value;
            var scrollTo = item.ValueToCenterCell;
            KillTween();
            scrollTween = DOTween.To(val => scroll.Value = val, scrollFrom, scrollTo, autoScrollDuration)
                .SetEase(Ease.OutExpo);
        }

        private void FocusOnLevelImmediate(SongList level)
        {
            HierarchyData item = null;
            if (level == null) return;

            for (var i = 0; i < scroll.Data.Count; i++)
            {
                var data = scroll.Data[i];
                if (data is LevelCellData levelCell && InterfaceUtility.AreTheSame(levelCell.Song, level))
                {
                    item = scroll.Hierarchy[i];
                    break;
                }
            }

            if (item == null) return;

            var scrollTo = item.ValueToCenterCell;
            scroll.Value = scrollTo;
            scroll.Rebuild();
        }

        private void SelectRandom()
        {
            var levels = storageData.SelectedPack.Value == null
                ? StorageData.GetAllSongs().ToList()
                : StorageData.GetSongsForPack(storageData.SelectedPack.Value);
            if (levels?.Count <= 0) return;

            SongList level = null;

            do
            {
                var index = Random.Range(0, levels.Count);
                level = levels[index];
            } while (InterfaceUtility.AreTheSame(level, storageData.SelectedChart.Value.song));

            LevelCellData item = null;
            for (var i = 0; i < scroll.Data.Count; i++)
            {
                var cell = scroll.Data[i];
                if (cell is LevelCellData lvCell && InterfaceUtility.AreTheSame(lvCell.Song, level))
                {
                    item = lvCell;
                    break;
                }
            }

            if (item == null) return;

            storageData.SelectedChart.Value = (item.Song, item.DifficultyToDisplay);
        }

        private void SelectTop()
        {
            scrollTween = DOTween.To(val => scroll.Value = val, scroll.Value, 0, autoScrollDuration / 2)
                .SetEase(Ease.OutExpo);
        }

        private void SelectBottom()
        {
            float v = 0;
            if (scroll.Hierarchy.Count >= 1) v = scroll.Hierarchy[scroll.Hierarchy.Count - 1].ValueToCenterCell;

            scrollTween = DOTween.To(val => scroll.Value = val, scroll.Value, v, autoScrollDuration / 2)
                .SetEase(Ease.OutExpo);
        }

        private void OnScroll(float arg)
        {
            KillTween();
        }

        private void KillTween()
        {
            scrollTween?.Kill();
        }
    }
}