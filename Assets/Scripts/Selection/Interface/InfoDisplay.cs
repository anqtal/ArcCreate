using System;
using System.Collections.Generic;
using System.Threading;
using ArcCreate.Data;
using ArcCreate.Gameplay.Score;
using ArcCreate.SceneTransition;
using ArcCreate.Storage;
using ArcCreate.Utility;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ArcCreate.Selection.Interface
{
    public class InfoDisplay : MonoBehaviour
    {
        [SerializeField] private StorageData storage;

        [Header("Info")] [SerializeField] private TMP_Text title;

        [SerializeField] private TMP_Text composer;
        [SerializeField] private TMP_Text bpm;
        [SerializeField] private TMP_Text charter;
        [SerializeField] private Button switchDiffButton;
        [SerializeField] private Button nextDiffButton;
        [SerializeField] private Button nextNextDiffButton;
        [SerializeField] private StarRating ratingDisplay;

        [Header("Difficulty")] [SerializeField]
        private TMP_Text currDiffName;

        [SerializeField] private TMP_Text[] diffNumbers;
        [SerializeField] private DifficultyCell[] diffColors;
        [SerializeField] private Color inactiveDiffColor;

        [Header("Images")] [SerializeField] private RawImage jacket;

        [Header("Score")] [SerializeField] private TMP_Text score;

        [SerializeField] private GradeDisplay gradeDisplay;

        [Header("Transition")] [SerializeField]
        private SpriteSO transitionJacketSprite;

        [SerializeField] private StringSO transitionTitle;
        [SerializeField] private StringSO transitionComposer;
        [SerializeField] private StringSO transitionIllustrator;
        [SerializeField] private StringSO transitionCharter;
        [SerializeField] private StringSO transitionAlias;
        [SerializeField] private StringSO transitionDifficulty;
        [SerializeField] private ColorSO transitionDifficultyColor;
        [SerializeField] private ThemeGroup themeGroup;

        [Header("Exception")] [SerializeField] private Dialog exceptionDialog;

        [SerializeField] private TMP_Text exceptionText;
        private readonly CancellationTokenSource cts = new();
        private Sprite jacketSprite;


        private void Awake()
        {
            storage.SelectedChart.OnValueChange += OnChartChange;
            storage.OnStorageChange += OnStorageChange;
            ScoreCache.OnUpdated += OnScoreCacheUpdated;
            storage.OnSwitchToGameplaySceneException += OnGameplayException;
            if (switchDiffButton != null) switchDiffButton.onClick.AddListener(SwitchDifficulty);

            if (nextDiffButton != null) nextDiffButton.onClick.AddListener(SwitchDifficulty);

            if (nextNextDiffButton != null) nextNextDiffButton.onClick.AddListener(SwitchNextDifficulty);

            if (storage.IsLoaded) OnStorageChange();
        }

        private void OnDestroy()
        {
            storage.SelectedChart.OnValueChange -= OnChartChange;
            storage.OnStorageChange -= OnStorageChange;
            ScoreCache.OnUpdated -= OnScoreCacheUpdated;
            if (switchDiffButton != null) switchDiffButton.onClick.RemoveListener(SwitchDifficulty);

            if (nextDiffButton != null) nextDiffButton.onClick.RemoveListener(SwitchDifficulty);

            if (nextNextDiffButton != null) nextNextDiffButton.onClick.RemoveListener(SwitchNextDifficulty);
            storage.OnSwitchToGameplaySceneException -= OnGameplayException;
            cts.Cancel();
        }

        private void OnGameplayException(Exception e)
        {
            exceptionDialog.Show();
            exceptionText.text = I18n.S("Gameplay.Exception.Load", new Dictionary<string, object>
            {
                { "Identifier", storage.SelectedChart.Value.song?.id ?? "unknown" },
                {
                    "ChartPath", storage.SelectedChart.Value.difficulty == null
                        ? "unknown"
                        : SongDifficultyUtility.GetChartPath(storage.SelectedChart.Value.difficulty)
                },
                { "Message", e.Message },
                { "StackTrace", e.StackTrace }
            });
        }

        private void OnStorageChange()
        {
            OnChartChange(storage.SelectedChart.Value);
        }

        private void OnChartChange((SongList level, Difficulty difficulty) obj)
        {
            var (level, chart) = obj;
            if (level == null) return;

            var titleText = SongDifficultyUtility.GetTitle(level);
            var composerText = SongDifficultyUtility.GetComposer(level);
            var charterText = SongDifficultyUtility.GetCharter(chart);
            if (title != null)
                title.text = string.IsNullOrEmpty(titleText)
                    ? I18n.S("Gameplay.Selection.Info.Undefined.Title")
                    : titleText;

            if (composer != null)
                composer.text = string.IsNullOrEmpty(composerText)
                    ? I18n.S("Gameplay.Selection.Info.Undefined.Composer")
                    : composerText;

            if (bpm != null)
                bpm.text = string.IsNullOrEmpty(level.bpm) ? "BPM: " + level.bpm_base : "BPM: " + level.bpm;

            if (charter != null)
                charter.text = string.IsNullOrEmpty(charterText)
                    ? I18n.S("Gameplay.Selection.Info.Undefined.Charter")
                    : I18n.S("Gameplay.Selection.Info.Charter", charterText);

            if (transitionTitle != null && title != null) transitionTitle.Value = title.text;

            if (transitionComposer != null && composer != null) transitionComposer.Value = composer.text;

            if (transitionIllustrator != null) transitionIllustrator.Value = string.Empty;

            if (transitionCharter != null) transitionCharter.Value = charterText;

            if (transitionAlias != null) transitionAlias.Value = string.Empty;

            if (transitionDifficulty != null)
                transitionDifficulty.Value = SongDifficultyUtility.GetDifficultyName(chart);

            if (score != null) score.text = "--";

            if (gradeDisplay != null)
            {
                gradeDisplay.Display(Grade.Unknown);
                gradeDisplay.gameObject.SetActive(false);
            }

            if (ratingDisplay != null) ratingDisplay.Value = 0;

            var sideString = SongDifficultyUtility.GetSkin(level).ToLower();

            if (jacket != null)
            {
                if (jacket.texture) storage.ReleasePersistent(jacket.texture);

                StorageData.AssignSongJacket(jacket, level).ContinueWith(() =>
                {
                    if (jacketSprite) Destroy(jacketSprite);

                    var texture = jacket.texture;
                    storage.EnsurePersistent(texture);
                    jacketSprite = Sprite.Create(texture as Texture2D, new Rect(0, 0, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f));
                    if (transitionJacketSprite != null) transitionJacketSprite.Value = jacketSprite;
                });
            }

            switch (sideString)
            {
                case "conflict":
                    themeGroup.Value = Theme.Dark;
                    break;
                case "light":
                case "colorless":
                default:
                    themeGroup.Value = Theme.Light;
                    break;
            }

            var (currDiffName, currDiffNum) = SongDifficultyUtility.ParseDifficultyName(chart, 3);
            if (this.currDiffName != null) this.currDiffName.text = currDiffName.ToUpper();

            if (diffNumbers != null && diffNumbers.Length > 0)
                diffNumbers[0].text = InterfaceUtility.AlignedDiffNumber(currDiffNum);

            ColorUtility.TryParseHtmlString(SongDifficultyUtility.GetDifficultyColor(chart), out var currDiffColor);
            if (diffColors != null && diffColors.Length > 0) diffColors[0].Color = currDiffColor;

            if (transitionDifficultyColor != null) transitionDifficultyColor.Value = currDiffColor;

            var i = 1;

            var charts = SongDifficultyUtility.GetPlayableDifficulties(level);
            var indexOfCurrentChart = 0;
            for (var j = 0; j < charts.Count; j++)
            {
                var otherChart = charts[j];
                if (SongDifficultyUtility.IsSameDifficulty(otherChart, chart))
                {
                    indexOfCurrentChart = j;
                    break;
                }
            }

            UpdateScoreFromCache(level.id, SongDifficultyUtility.GetApiDifficulty(chart));

            for (var j = 1; j < charts.Count; j++)
            {
                var otherChart = charts[(j + indexOfCurrentChart) % charts.Count];
                if (SongDifficultyUtility.IsSameDifficulty(otherChart, chart)) break;

                var (diffName, diffNum) = SongDifficultyUtility.ParseDifficultyName(otherChart, 3);
                ColorUtility.TryParseHtmlString(SongDifficultyUtility.GetDifficultyColor(otherChart),
                    out var diffColor);

                if (diffNumbers != null && i < diffNumbers.Length)
                {
                    diffNumbers[i].gameObject.SetActive(true);
                    diffNumbers[i].text = InterfaceUtility.AlignedDiffNumber(diffNum);
                }

                if (diffColors != null && i < diffColors.Length) diffColors[i].Color = diffColor;

                i += 1;
                if (diffNumbers == null || i >= diffNumbers.Length) break;
            }

            if (diffNumbers != null)
                for (; i < diffNumbers.Length; i++)
                {
                    diffNumbers[i].gameObject.SetActive(false);
                    if (diffColors != null && i < diffColors.Length) diffColors[i].Color = inactiveDiffColor;
                }
        }

        private void OnScoreCacheUpdated()
        {
            var (level, chart) = storage.SelectedChart.Value;
            if (level == null || chart == null) return;

            UpdateScoreFromCache(level.id, SongDifficultyUtility.GetApiDifficulty(chart));
        }

        private void UpdateScoreFromCache(string songId, int difficulty)
        {
            if (ScoreCache.TryGetScore(songId, difficulty, out var cachedScore, out _))
            {
                if (score != null) score.text = FormatAccPercent(cachedScore);

                if (ratingDisplay != null) ratingDisplay.Value = GetStarCount(cachedScore);
            }
            else
            {
                if (score != null) score.text = "--";

                if (ratingDisplay != null) ratingDisplay.Value = 0;
            }
        }

        private static string FormatAccPercent(double accPercent)
        {
            return $"{accPercent:0.0000}%";
        }

        private static int GetStarCount(double accPercent)
        {
            if (accPercent > 99.5) return 5;

            if (accPercent > 99) return 4;

            if (accPercent > 98.5) return 3;

            if (accPercent > 98) return 2;

            if (accPercent > 97) return 1;

            return 0;
        }

        private void SwitchDifficulty()
        {
            ChangeDifficulty(1);
        }

        private void SwitchNextDifficulty()
        {
            ChangeDifficulty(2);
        }

        private void ChangeDifficulty(int distance)
        {
            var (level, chart) = storage.SelectedChart.Value;
            if (level == null) return;

            if (chart == null)
            {
                var charts = SongDifficultyUtility.GetPlayableDifficulties(level);
                if (charts.Count == 0) return;

                storage.SelectedChart.Value = (level, charts[0]);
                return;
            }

            var chartList = SongDifficultyUtility.GetPlayableDifficulties(level);
            if (chartList.Count == 0) return;

            var indexOfCurrentChart = 0;
            for (var i = 0; i < chartList.Count; i++)
            {
                var otherChart = chartList[i];
                if (SongDifficultyUtility.IsSameDifficulty(otherChart, chart))
                {
                    indexOfCurrentChart = i;
                    break;
                }
            }

            var next = chartList[(indexOfCurrentChart + distance) % chartList.Count];
            storage.SelectedChart.Value = (level, next);
        }
    }
}