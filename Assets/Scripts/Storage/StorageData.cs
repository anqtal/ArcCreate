using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ArcCreate.Data;
using ArcCreate.Gameplay;
using ArcCreate.Gameplay.Audio;
using ArcCreate.Gameplay.Score;
using ArcCreate.SceneTransition;
using ArcCreate.Utility.Extension;
using ArcCreate.Utility.LRUCache;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ArcCreate.Storage
{
    [CreateAssetMenu(fileName = "StorageData", menuName = "ScriptableObject/StorageData")]
    public class StorageData : ScriptableObject
    {
        private static readonly LRUCache<string, Incompletable<Texture>> JacketCache = new(50, DestroyCache);

        private static readonly HashSet<Object> PersistentCache = new();
        private static readonly HashSet<Object> QueuedForDelete = new();
        [SerializeField] private Texture defaultJacket;
        [SerializeField] private GameplayData gameplayData;
        [SerializeField] private StringSO transitionPlayCount;
        [SerializeField] private StringSO transitionRetryCount;
        private (SongList song, Difficulty difficulty) currentGameplayChart;
        private bool isPreparingPlayScene;

        public State<Pack> SelectedPack { get; } = new();

        public State<(SongList song, Difficulty difficulty)> SelectedChart { get; } = new();

        public bool IsTransitioning =>
            SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsTransitioning;

        public bool IsLoaded => true;

        public event Action OnStorageChange;

        public event Action OnSwitchToGameplayScene;

        public event Action OnOpenFilePicker;

        public event Action<Exception> OnSwitchToGameplaySceneException;

        private static List<SongList> BuildSongs(string pack = null)
        {
            var songs = SongData.Instance.Songs ?? new List<SongList>();
            if (string.IsNullOrEmpty(pack)) return songs.ToList();

            return songs.Where(song => song.set == pack).ToList();
        }

        public static IEnumerable<SongList> GetAllSongs()
        {
            return BuildSongs();
        }

        public static Pack GetPack(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;

            return GetAllPacks().FirstOrDefault(pack => pack.id == id);
        }

        public static IEnumerable<Pack> GetAllPacks()
        {
            return PackData.Instance.Packs ?? Enumerable.Empty<Pack>();
        }

        public static List<SongList> GetSongsForPack(Pack pack)
        {
            return BuildSongs(pack?.id);
        }

        public CharacterStorage GetCharacter(string id)
        {
            return null;
        }

        public void NotifyStorageChange()
        {
            SelectedPack.SetValueWithoutNotify(GetLastSelectedPack());
            SelectedChart.SetValueWithoutNotify(GetLastSelectedChart(SelectedPack.Value?.id));

            OnStorageChange?.Invoke();
        }

        public void NotifyOpenFilePicker()
        {
            OnOpenFilePicker?.Invoke();
        }

        public async UniTask AssignTexture(RawImage image, IStorageUnit storage, string jacketPath,
            CancellationToken ct = default, string packPath = null)
        {
            if (packPath != null)
                jacketPath = packPath;
            else
                // jacketPath = Application.streamingAssetsPath + "/songs/" + storage.Identifier + "/base.jpg";
                jacketPath = Application.persistentDataPath + "/res/" + $"dl_{storage.Identifier}" + "/1080_base.image";
            var cachedTexture = JacketCache.Get(jacketPath);
            if (cachedTexture != null)
            {
                while (!cachedTexture.Completed)
                {
                    await UniTask.NextFrame();
                    if (ct.IsCancellationRequested) return;
                }

                if (cachedTexture.IsSuccess) image.texture = cachedTexture.Value;
            }
        }

        public static async UniTask AssignSongJacket(RawImage image, SongList song)
        {
            if (song == null || string.IsNullOrEmpty(song.id)) return;

            var cacheKey = GetSongJacketCacheKey(song.id);
            var cachedTexture = JacketCache.Get(cacheKey);
            if (cachedTexture != null && cachedTexture.Completed && cachedTexture.IsSuccess &&
                cachedTexture.Value != null)
            {
                image.texture = cachedTexture.Value;
                return;
            }

            var data = await DxResource.ReadFile(song.id, DxResource.FileType.SongImage);
            if (data == null || data.Length == 0) return;

            var texture = new Texture2D(2, 2);
            texture.LoadImage(data);
            image.texture = texture;

            if (cachedTexture == null)
            {
                try
                {
                    JacketCache.Add(cacheKey, new Incompletable<Texture>
                    {
                        Completed = true,
                        IsSuccess = true,
                        Value = texture
                    });
                }
                catch (ArgumentException)
                {
                    cachedTexture = JacketCache.Get(cacheKey);
                    if (cachedTexture != null)
                    {
                        cachedTexture.Value = texture;
                        cachedTexture.IsSuccess = true;
                        cachedTexture.Completed = true;
                    }
                }
            }
            else
            {
                cachedTexture.Value = texture;
                cachedTexture.IsSuccess = true;
                cachedTexture.Completed = true;
            }
        }

        public bool TryAssignSongJacketFromCache(RawImage jacket, SongList song)
        {
            if (song == null || string.IsNullOrEmpty(song.id)) return false;

            var texture = JacketCache.Get(GetSongJacketCacheKey(song.id));
            if (texture != null && texture.Completed && texture.IsSuccess && texture.Value != null)
            {
                jacket.texture = texture.Value;
                return true;
            }

            jacket.texture = defaultJacket;
            return false;
        }

        public static async UniTask AssignPackJacket(RawImage image, Pack pack)
        {
            if (pack == null || string.IsNullOrEmpty(pack.id)) return;

            var cacheKey = GetPackJacketCacheKey(pack.id);
            var cachedTexture = JacketCache.Get(cacheKey);
            if (cachedTexture != null && cachedTexture.Completed && cachedTexture.IsSuccess &&
                cachedTexture.Value != null)
            {
                image.texture = cachedTexture.Value;
                return;
            }

            var data = await DxResource.ReadFile(pack.id, DxResource.FileType.PackImage);
            if (data == null || data.Length == 0) return;

            var texture = new Texture2D(2, 2);
            texture.LoadImage(data);
            image.texture = texture;

            if (cachedTexture == null)
            {
                try
                {
                    JacketCache.Add(cacheKey, new Incompletable<Texture>
                    {
                        Completed = true,
                        IsSuccess = true,
                        Value = texture
                    });
                }
                catch (ArgumentException)
                {
                    cachedTexture = JacketCache.Get(cacheKey);
                    if (cachedTexture != null)
                    {
                        cachedTexture.Value = texture;
                        cachedTexture.IsSuccess = true;
                        cachedTexture.Completed = true;
                    }
                }
            }
            else
            {
                cachedTexture.Value = texture;
                cachedTexture.IsSuccess = true;
                cachedTexture.Completed = true;
            }
        }

        public void EnsurePersistent(Object obj)
        {
            PersistentCache.Add(obj);
        }

        public void ReleasePersistent(Object obj)
        {
            PersistentCache.Remove(obj);
            if (!QueuedForDelete.Contains(obj)) return;
            Destroy(obj);
            QueuedForDelete.Remove(obj);
        }

        public bool TryAssignPackJacketFromCache(RawImage jacket, Pack pack)
        {
            if (pack == null || string.IsNullOrEmpty(pack.id)) return false;

            var texture = JacketCache.Get(GetPackJacketCacheKey(pack.id));
            if (texture != null && texture.Completed && texture.IsSuccess && texture.Value != null)
            {
                jacket.texture = texture.Value;
                return true;
            }

            jacket.texture = defaultJacket;
            return false;
        }

        public bool TryAssignTextureFromCache(RawImage jacket, IStorageUnit level, string jacketPath,
            string packPath = null)
        {
            if (packPath != null)
                jacketPath = packPath;
            else
                jacketPath = Application.streamingAssetsPath + "/songs/" + level.Identifier + "/base.jpg";

            var realJacketPath = level.GetRealPath(jacketPath);
            if (!realJacketPath.HasValue) return false;

            jacketPath = realJacketPath.Value;

            var texture = JacketCache.Get(jacketPath);
            if (texture != null && texture.Completed && texture.IsSuccess && texture.Value != null)
            {
                jacket.texture = texture.Value;
                return true;
            }

            jacket.texture = defaultJacket;
            return false;
        }

        public void SwitchToPlayScene((SongList song, Difficulty difficulty) selection)
        {
            if (SceneTransitionManager.Instance.IsTransitioning || isPreparingPlayScene) return;

            PrepareAndSwitchToPlayScene(selection).Forget();
        }

        private async UniTask PrepareAndSwitchToPlayScene((SongList song, Difficulty difficulty) selection)
        {
            isPreparingPlayScene = true;
            try
            {
                var (song, difficulty) = selection;
                var missingItems = SongDownloadService.GetMissingItems(song, difficulty);
                var downloadedNow = false;
                if (missingItems.Count > 0)
                {
                    var dialog = await SongDownloadDialog.Show(missingItems.Count);
                    if (dialog == null) return;

                    await SongDownloadService.DownloadMissing(
                        song,
                        difficulty,
                        missingItems,
                        (completed, total) => dialog.UpdateProgress(completed, total));
                    dialog.Close();
                    downloadedNow = true;
                }

                if (downloadedNow) return;

                currentGameplayChart = selection;

                if (gameplayData.EnableAutoplayMode.Value)
                {
                    transitionPlayCount.Value = "AUTOPLAY";
                    transitionRetryCount.Value = string.Empty;
                }
                else if (gameplayData.EnablePracticeMode.Value)
                {
                    transitionPlayCount.Value = "PRACTICE MODE";
                    transitionRetryCount.Value = string.Empty;
                }
                else
                {
                    transitionPlayCount.Value = TextFormat.FormatPlayCount(1);
                    transitionRetryCount.Value = TextFormat.FormatRetryCount(1);
                }

                var sequence = new TransitionSequence()
                    .OnShow()
                    .AddTransition(new SoundTransition(TransitionScene.Sound.EnterGameplay))
                    .AddTransition(new TriangleTileTransition())
                    .AddTransition(new DecorationTransition())
                    .AddTransition(new InfoTransition())
                    .OnHide()
                    .AddTransition(new SoundTransition(TransitionScene.Sound.GameplayLoadComplete))
                    .AddTransition(new InfoTransition())
                    .AddTransitionReversed(new PlayRetryCountTransition())
                    .AddTransition(new PlayRetryCountTransition(), 1200)
                    .AddTransition(new TriangleTileTransition(), 1200)
                    .AddTransition(new DecorationTransition(), 1200)
                    .SetWaitDuration(2000);

                SceneTransitionManager.Instance.SetTransition(sequence);
                GameplayManager gameplay = null;
                OnSwitchToGameplayScene?.Invoke();

                // Set the values first to avoid some scencontrol object only reading these values
                // once on awake.
                gameplayData.BaseBpm.Value = song.bpm_base;
                gameplayData.Title.Value = SongDifficultyUtility.GetTitle(song);
                gameplayData.Composer.Value = SongDifficultyUtility.GetComposer(song);
                gameplayData.DifficultyName.Value = SongDifficultyUtility.GetDifficultyName(difficulty);
                SceneTransitionManager.Instance.SwitchScene(
                        SceneNames.GameplayScene,
                        async rep =>
                        {
                            if (rep is GameplayManager gameplayControl)
                            {
                                await new GameplayLoader(gameplayControl, gameplayData).Load(song, difficulty);
                                gameplay = gameplayControl;
                                gameplay.ShouldNotifyOnAudioEnd = true;
                                gameplay.EnablePauseMenu = true;
                                //gameplay.Audio.AudioTiming = -Values.DelayBeforeAudioStart;
                                gameplayData.PlaybackSpeed.Value = 1;
                                BassAudioService.Instance.AudioPreviewStream.FadeOutAsync(4f).Forget();
                            }
                        },
                        e => { OnSwitchToGameplaySceneException?.Invoke(e); })
                    .ContinueWith(() => gameplay?.Audio.PlayWithDelay(0, Values.DelayBeforeAudioStart))
                    .Forget();

                gameplayData.OnPlayComplete -= OnPlayComplete;
                gameplayData.OnPlayComplete += OnPlayComplete;
            }
            finally
            {
                isPreparingPlayScene = false;
            }
        }

        private void SwitchToResultScene(SongList song, Difficulty difficulty, PlayResult result, bool isAuto)
        {
            var transition = new TransitionSequence()
                .OnShow()
                .AddTransition(new TriangleTileTransition())
                .OnBoth()
                .AddTransition(new DecorationTransition());
            SceneTransitionManager.Instance.SetTransition(transition);
            SceneTransitionManager.Instance.SwitchScene(
                SceneNames.ResultScene,
                rep =>
                {
                    rep.PassData(song, difficulty, result, isAuto);
                    return default;
                }).Forget();
        }

        public (SongList song, Difficulty difficulty) GetLastSelectedChart(string packId)
        {
            var levelId = PlayerPrefs.GetString($"Selection.LastLevel.{packId ?? "all"}", null);
            var chartPath = PlayerPrefs.GetString("Selection.LastChartPath", null);
            var difficultyName = PlayerPrefs.GetString("Selection.LastDifficultyName", null);

            SongList lv = null;
            //     if (SelectedPack.Value != null)
            //     {
            //         lv = SelectedPack.Value.Levels.First();
            //     }
            //     else
            //     {
            //         lv = GetAllLevels().First();
            //     }
            // }

            var songs = BuildSongs(packId);
            if (!string.IsNullOrEmpty(levelId)) lv = songs.FirstOrDefault(song => song.id == levelId);

            lv ??= songs.FirstOrDefault();
            if (lv == null) return (null, null);

            var difficulties = SongDifficultyUtility.GetPlayableDifficulties(lv);
            if (difficulties.Count == 0) return (lv, null);

            if (SelectedChart.Value.difficulty != null)
                foreach (var c in difficulties)
                    if (SongDifficultyUtility.IsSameDifficulty(c, SelectedChart.Value.difficulty))
                        return (lv, c);

            foreach (var c in difficulties)
                if (SongDifficultyUtility.GetChartPath(c) == chartPath
                    || SongDifficultyUtility.GetDifficultyName(c) == difficultyName)
                    return (lv, c);

            return (lv, difficulties[0]);
        }

        public static Pack GetLastSelectedPack()
        {
            var id = PlayerPrefs.GetString("Selection.LastPack", null);
            return id == null ? null : GetPack(id);
        }

        public CharacterStorage GetSelectedCharacter()
        {
            return null;
            // if (CharacterCollection.Count() == 1)
            // {
            //     return CharacterCollection.FindAll().First();
            // }
            //
            // string id = PlayerPrefs.GetString("Selection.LastCharacter", null);
            // if (id == null)
            // {
            //     return null;
            // }
            //
            // return GetCharacter(id);
        }

        private static void DestroyCache<T>(Incompletable<T> obj)
            where T : Object
        {
            if (!PersistentCache.Contains(obj.Value))
                Destroy(obj.Value);
            else
                QueuedForDelete.Add(obj.Value);
        }

        private static string GetSongJacketCacheKey(string songId)
        {
            return $"song:{songId}";
        }

        private static string GetPackJacketCacheKey(string packId)
        {
            return $"pack:{packId}";
        }

        private void OnPlayComplete(PlayResult result)
        {
            var (currentLevel, currentChart) = currentGameplayChart;
            if (!gameplayData.EnableAutoplayMode.Value && !gameplayData.EnablePracticeMode.Value)
            {
                if (ScoreCache.TryGetScore(currentLevel.id, SongDifficultyUtility.GetApiDifficulty(currentChart),
                        out var bestScore, out _))
                    result.BestScore = bestScore;

                result.PlayCount = 1;
                BassAudioService.Instance.AudioStream.Dispose();
            }

            SwitchToResultScene(currentLevel, currentChart, result, gameplayData.EnableAutoplayMode.Value);
        }

        private class Incompletable<T>
        {
            public bool Completed { get; set; }

            public T Value { get; set; }

            public bool IsSuccess { get; set; }
        }
    }
}