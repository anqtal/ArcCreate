using System.Collections.Generic;
using System.Threading;
using ArcCreate.Selection.SoundEffect;
using ArcCreate.Storage;
using ArcCreate.Utility.InfiniteScroll;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ArcCreate.Selection.Interface
{
    public class LevelCell : Cell, IPointerClickHandler
    {
        private static Pool<DifficultyCell> difficultyCellPool;
        [SerializeField] private StorageData storage;
        [SerializeField] private Image border;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text composer;
        [SerializeField] private TMP_Text difficulty;
        [SerializeField] private TMP_Text rating;
        [SerializeField] private RawImage jacket;
        [SerializeField] private RawImage jacketFill;
        [SerializeField] private DifficultyCell difficultyBackground;
        [SerializeField] private Image difficultyImageNormal;
        [SerializeField] private Image difficultyImageSelected;
        [SerializeField] private ClearResultDisplay clearResult;
        [SerializeField] private GradeDisplay grade;
        [SerializeField] private GameObject newIndicator;
        [SerializeField] private GameObject scoreDetailsParent;
        [SerializeField] private RectTransform difficultyCellParent;
        [SerializeField] private GameObject playIndicate;

        [Header("Animation")] [SerializeField] private RectTransform rect;

        [SerializeField] private Vector2 selectedSizeDelta;
        [SerializeField] private float animationDuration = 0.15f;
        [SerializeField] private Ease animationEase = Ease.OutExpo;
        private readonly List<DifficultyCell> difficultyCells = new();
        private Vector2 defaultSizeDelta;
        private Color diffColor;
        private bool isSelected;

        private SongList song;
        private Difficulty visibleDifficulty;

        private void Awake()
        {
            storage.SelectedChart.OnValueChange += OnChartChange;
            defaultSizeDelta = rect.sizeDelta;
        }

        private void OnDestroy()
        {
            storage.SelectedChart.OnValueChange -= OnChartChange;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (storage.IsTransitioning) return;

            if (isSelected)
            {
                storage.SwitchToPlayScene((song, visibleDifficulty));
            }
            else
            {
                storage.SelectedChart.Value = (song, visibleDifficulty);
                Services.SoundEffect.Play(Sound.CellSelect);
            }
        }

        public override void SetCellData(CellData cellData)
        {
            var data = cellData as LevelCellData;
            song = data.Song;
            visibleDifficulty = data.DifficultyToDisplay;
            var charts = data.Difficulties;

            ColorUtility.TryParseHtmlString(SongDifficultyUtility.GetDifficultyColor(visibleDifficulty), out diffColor);
            difficultyBackground.Color = diffColor;
            UpdateSelectedStateImmediate(InterfaceUtility.AreTheSame(song, storage.SelectedChart.Value.song));
            if (isSelected) visibleDifficulty = storage.SelectedChart.Value.difficulty;

            difficultyCellPool = difficultyCellPool == null || difficultyCellPool.IsDestroyed
                ? Pools.Get<DifficultyCell>("DifficultyCell")
                : difficultyCellPool;
            SetInfo(charts);
        }

        public override async UniTask LoadCellFully(CellData cellData, CancellationToken cancellationToken)
        {
            //await storage.AssignTexture(jacket, level, visibleChart.JacketPath, cancellationToken);
            await StorageData.AssignSongJacket(jacket, song);
            jacketFill.texture = jacket.texture;
        }

        private void SetInfo(List<Difficulty> charts)
        {
            title.text = SongDifficultyUtility.GetTitle(song);
            composer.text = SongDifficultyUtility.GetComposer(song);

            var (name, number) = SongDifficultyUtility.ParseDifficultyName(visibleDifficulty, 3);
            difficulty.text = string.IsNullOrEmpty(number) ? name : InterfaceUtility.AlignedDiffNumber(number);
            rating.text = ""; //playHistory.Rating == 0 ? "?" : playHistory.Rating.ToString();

            foreach (var im in difficultyCells) difficultyCellPool.Return(im);

            difficultyCells.Clear();
            foreach (var chart in charts)
            {
                if (SongDifficultyUtility.IsSameDifficulty(chart, visibleDifficulty)) continue;

                var im = difficultyCellPool.Get(difficultyCellParent);
                ColorUtility.TryParseHtmlString(SongDifficultyUtility.GetDifficultyColor(chart), out var c);
                im.Color = c;
                difficultyCells.Add(im);
            }

            if (storage.TryAssignSongJacketFromCache(jacket, song)) MarkFullyLoaded();

            jacketFill.texture = jacket.texture;
            //grade.Display(playHistory.BestScorePlayOrDefault.Grade);
            //clearResult.Display(playHistory.BestResultPlayOrDefault.ClearResult);
            //grade.gameObject.SetActive(playHistory.PlayCount > 0);
            //clearResult.gameObject.SetActive(playHistory.PlayCount > 0);
            //newIndicator.SetActive(playHistory.PlayCount <= 0);
            //scoreDetailsParent.SetActive(playHistory.PlayCount > 0);
        }

        private void OnChartChange((SongList level, Difficulty difficulty) obj)
        {
            var (level, chart) = obj;
            UpdateSelectedState(InterfaceUtility.AreTheSame(level, song));
        }

        private void UpdateSelectedState(bool isSelected)
        {
            playIndicate.SetActive(isSelected);
            difficulty.gameObject.SetActive(!isSelected);

            rect.DOSizeDelta(isSelected ? selectedSizeDelta : defaultSizeDelta, animationDuration)
                .SetEase(animationEase);
            border.DOColor(isSelected ? Color.white : new Color(1, 1, 1, 0.5f), animationDuration)
                .SetEase(animationEase);

            var diffColorClear = diffColor;
            diffColorClear.a = 0;
            difficultyImageNormal.DOColor(isSelected ? diffColorClear : diffColor, animationDuration)
                .SetEase(animationEase);
            difficultyImageSelected.DOColor(isSelected ? diffColor : diffColorClear, animationDuration)
                .SetEase(animationEase);
            this.isSelected = isSelected;
        }

        private void UpdateSelectedStateImmediate(bool isSelected)
        {
            playIndicate.SetActive(isSelected);
            difficulty.gameObject.SetActive(!isSelected);

            rect.DOKill();
            rect.localScale = Vector3.one;
            rect.sizeDelta = isSelected ? selectedSizeDelta : defaultSizeDelta;

            border.DOKill();
            border.color = isSelected ? Color.white : new Color(1, 1, 1, 0.5f);

            var diffColorClear = diffColor;
            diffColorClear.a = 0;

            difficultyImageNormal.DOKill();
            difficultyImageNormal.color = isSelected ? diffColorClear : diffColor;

            difficultyImageSelected.DOKill();
            difficultyImageSelected.color = isSelected ? diffColor : diffColorClear;
            this.isSelected = isSelected;
        }
    }
}