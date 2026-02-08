using System;
using System.Threading;
using ArcCreate.Data;
using ArcCreate.Gameplay.Score;
using ArcCreate.SceneTransition;
using ArcCreate.Storage;
using ArcCreate.Utility.Animation;
using ArcCreate.Utility.Extension;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace ArcCreate.Selection.Interface
{
    public class ResultScreen : SceneRepresentative
    {
        [SerializeField] private StorageData storage;
        [SerializeField] private ScriptedAnimator animator;
        [SerializeField] private Image characterImage;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text composer;
        [SerializeField] private Image difficulty;
        [SerializeField] private TMP_Text difficultyText;
        [SerializeField] private GameObject charterFrame;
        [SerializeField] private TMP_Text charterName;
        [SerializeField] private GameObject aliasFrame;
        [SerializeField] private RectTransform aliasRect;
        [SerializeField] private TMP_Text aliasName;
        [SerializeField] private RawImage jacket;
        [SerializeField] private TMP_Text perfectEarly;
        [SerializeField] private TMP_Text perfectTotal;
        [SerializeField] private TMP_Text perfectLate;
        [SerializeField] private TMP_Text goodEarly;
        [SerializeField] private TMP_Text goodTotal;
        [SerializeField] private TMP_Text goodLate;
        [SerializeField] private TMP_Text missEarly;
        [SerializeField] private TMP_Text missTotal;
        [SerializeField] private TMP_Text missLate;
        [SerializeField] private TMP_Text offsetInfo;
        [SerializeField] private TMP_Text maxCombo;
        [SerializeField] private TMP_Text playCount;
        [SerializeField] private TMP_Text retryCount;
        [SerializeField] private TMP_Text bestScore;
        [SerializeField] private TMP_Text scoreIncrease;
        [SerializeField] private TMP_Text score;
        [SerializeField] private GradeDisplay gradeDisplay;
        [SerializeField] private ClearResultDisplay clearResultDisplay;
        [SerializeField] private Button returnButton;
        [SerializeField] private Button retryButton;
        [SerializeField] private StringSO transitionPlayCount;
        [SerializeField] private StringSO transitionRetryCount;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private GameObject playCountParent;
        [SerializeField] private GameObject autoNotifParent;
        [SerializeField] private float switchSceneAudioFadeDuration = 1;
        [SerializeField] private float characterImageHeight = 2048;
        private CancellationTokenSource cts = new();
        private Difficulty currentChart;
        private SongList currentSong;

        public void Display(SongList level, Difficulty chart, PlayResult play, bool isAuto)
        {
            StartDisplay(level, chart, play, isAuto).Forget();
        }

        private async UniTask StartDisplay(SongList level, Difficulty chart, PlayResult play, bool isAuto)
        {
            await DisplayCharacter();
            currentSong = level;
            currentChart = chart;
            title.text = SongDifficultyUtility.GetTitle(level);
            composer.text = SongDifficultyUtility.GetComposer(level);
            ColorUtility.TryParseHtmlString(SongDifficultyUtility.GetDifficultyColor(chart), out var c);
            difficulty.color = c;
            difficultyText.text = SongDifficultyUtility.GetDifficultyName(chart);
            storage.ReleasePersistent(jacket.texture);
            //storage.AssignTexture(jacket, level, chart.JacketPath).Forget();
            StorageData.AssignSongJacket(jacket, level).Forget();
            storage.EnsurePersistent(jacket.texture);
            perfectEarly.text = play.EarlyPerfectCount.ToString();
            perfectTotal.text = play.PerfectCount.ToString();
            perfectLate.text = play.LatePerfectCount.ToString();
            goodEarly.text = play.EarlyGoodCount.ToString();
            goodTotal.text = play.GoodCount.ToString();
            goodLate.text = play.LateGoodCount.ToString();
            missEarly.text = play.EarlyMissCount.ToString();
            missTotal.text = play.MissCount.ToString();
            missLate.text = play.LateMissCount.ToString();
            maxCombo.text = play.MaxCombo.ToString();
            gradeDisplay.Display(play.Grade);
            clearResultDisplay.Display(play.ClearResult);
            playCount.text = play.PlayCount.ToString();
            retryCount.text = play.RetryCount.ToString();

            playCountParent.SetActive(!isAuto);
            autoNotifParent.SetActive(isAuto);

            var currentAcc = ComputeAccPercent(play);
            score.text = FormatAccPercent(currentAcc);
            UpdateBestScoreFromCache(level.id, chart, currentAcc);
            offsetInfo.gameObject.SetActive(Settings.DisplayMsDifference.Value);
            offsetInfo.text = $"AVG: {play.OffsetMean:f2}ms  SD: {play.OffsetStd:f2}ms";

            var charterText = SongDifficultyUtility.GetCharter(chart);
            charterFrame.SetActive(!string.IsNullOrEmpty(charterText));
            aliasFrame.SetActive(false);
            charterName.text = charterText;
            aliasName.text = string.Empty;
            aliasRect.offsetMax = new Vector2(aliasRect.offsetMax.x, -charterName.preferredHeight);

            audioSource.Play();
            animator.Show();
        }

        private static string FormatAccPercent(double accPercent)
        {
            return $"{accPercent:0.0000}%";
        }

        private static string FormatAccPercentDelta(double delta)
        {
            var sign = delta >= 0 ? "+" : string.Empty;
            return $"{sign}{delta:0.0000}%";
        }

        private void UpdateBestScoreFromCache(string songId, Difficulty chart, double currentAcc)
        {
            if (ScoreCache.TryGetScore(songId, SongDifficultyUtility.GetApiDifficulty(chart), out var bestScoreValue,
                    out _))
            {
                var best = bestScoreValue;
                bestScore.text = FormatAccPercent(best);
                scoreIncrease.text = FormatAccPercentDelta(currentAcc - best);
            }
            else
            {
                bestScore.text = "--";
                scoreIncrease.text = "--";
            }
        }

        private static double ComputeAccPercent(PlayResult play)
        {
            var perfect = play.PerfectCount;
            var shinyPerfect = play.MappedPerfectCount;
            var near = play.GoodCount;
            var miss = play.MissCount;
            var totalNotes = perfect + near + miss;
            if (totalNotes <= 0) return 0;

            var accScore = perfect + near * 0.5 + shinyPerfect * 0.01;
            return accScore / totalNotes * 100.0;
        }

        private async UniTask DisplayCharacter()
        {
            var character = storage.GetSelectedCharacter();
            if (character == null || string.IsNullOrEmpty(character.ImagePath))
            {
                characterImage.sprite = null;
                characterImage.gameObject.SetActive(false);
                return;
            }

            var imagePath = character.GetRealPath(character.ImagePath);
            if (!imagePath.HasValue)
            {
                characterImage.sprite = null;
                characterImage.gameObject.SetActive(false);
                return;
            }

            using (var req = UnityWebRequestTexture.GetTexture(
                       Uri.EscapeUriString("file:///" + imagePath.Value.Replace("\\", "/"))))
            {
                await req.SendWebRequest();
                if (!string.IsNullOrWhiteSpace(req.error))
                {
                    characterImage.sprite = null;
                    characterImage.gameObject.SetActive(false);
                    return;
                }

                var t = DownloadHandlerTexture.GetContent(req);
                t.wrapMode = TextureWrapMode.Clamp;
                var sprite = Sprite.Create(
                    t,
                    new Rect(0, 0, t.width, t.height),
                    new Vector2(0.5f, 0.5f));

                characterImage.sprite = sprite;
                characterImage.gameObject.SetActive(true);
                var rect = characterImage.GetComponent<RectTransform>();
                rect.anchoredPosition = new Vector2(character.X, character.Y);
                rect.localScale = new Vector3(character.Scale, character.Scale, 1);

                var ratio = (float)t.width / t.height;
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, characterImageHeight);
                rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, characterImageHeight * ratio);
            }
        }

        public override void PassData(params object[] args)
        {
            var level = args[0] as SongList;
            var chart = args[1] as Difficulty;
            var result = (PlayResult)args[2];
            var isAuto = (bool)args[3];
            Display(level, chart, result, isAuto);
        }

        public override void OnUnloadScene()
        {
            returnButton.onClick.RemoveListener(ReturnToPreviousScene);
            retryButton.onClick.RemoveListener(RetryChart);
            cts.Cancel();
            cts.Dispose();
            cts = new CancellationTokenSource();
        }

        protected override void OnSceneLoad()
        {
            returnButton.onClick.AddListener(ReturnToPreviousScene);
            retryButton.onClick.AddListener(RetryChart);
            animator.HideImmediate();
        }

        private void ReturnToPreviousScene()
        {
            audioSource.DOFade(0, switchSceneAudioFadeDuration).OnComplete(audioSource.Stop);
            animator.GetHideTween(out var _).Play().OnComplete(() =>
            {
                var transition = new TransitionSequence();
                SceneTransitionManager.Instance.SetTransition(transition);
                SceneTransitionManager.Instance.SwitchScene(SceneNames.SelectScene).Forget();
            });
        }

        private void RetryChart()
        {
            if (currentSong != null && currentChart != null)
            {
                transitionPlayCount.Value = TextFormat.FormatPlayCount(1);
                transitionRetryCount.Value = TextFormat.FormatRetryCount(1);

                animator.Hide();
                storage.SwitchToPlayScene((currentSong, currentChart));
            }
        }
    }
}