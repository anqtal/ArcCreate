using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using ArcCreate.Data;
using ArcCreate.Gameplay;
using ArcCreate.Gameplay.Audio;
using ArcCreate.SceneTransition;
using ArcCreate.Storage.Data;
using ArcCreate.Utility.Extension;
using ArcCreate.Utility.LRUCache;
using Cysharp.Threading.Tasks;
using UltraLiteDB;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using YamlDotNet.Serialization;
using Object = UnityEngine.Object;

namespace ArcCreate.Storage
{
    [CreateAssetMenu(fileName = "StorageData", menuName = "ScriptableObject/StorageData")]
    public class StorageData : ScriptableObject
    {
        private static readonly LRUCache<string, Incompletable<Texture>> JacketCache =
            new LRUCache<string, Incompletable<Texture>>(50, DestroyCache);

        private static readonly HashSet<Object> PersistentCache = new HashSet<Object>();
        private static readonly HashSet<Object> QueuedForDelete = new HashSet<Object>();
        [SerializeField] private Texture defaultJacket;
        [SerializeField] private GameplayData gameplayData;
        [SerializeField] private StringSO transitionPlayCount;
        [SerializeField] private StringSO transitionRetryCount;
        private (LevelStorage level, ChartSettings chart) currentGameplayChart;

        public event Action OnStorageChange;

        public event Action OnSwitchToGameplayScene;

        public event Action OnOpenFilePicker;

        public event Action<Exception> OnSwitchToGameplaySceneException;

        public State<PackStorage> SelectedPack { get; } = new State<PackStorage>();

        public State<(LevelStorage level, ChartSettings chart)> SelectedChart { get; } =
            new State<(LevelStorage, ChartSettings)>();

        public UltraLiteCollection<LevelStorage> LevelCollection { get; private set; }

        public UltraLiteCollection<PackStorage> PackCollection { get; private set; }

        public UltraLiteCollection<CharacterStorage> CharacterCollection { get; private set; }

        public bool IsTransitioning =>
            SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsTransitioning;

        public bool IsLoaded => LevelCollection != null && PackCollection != null && CharacterCollection != null;

        public LevelStorage GetLevel(string id)
        {
            var levels = LoadYaml();
            return levels[0];
            //return LevelCollection.FindOne(Query.EQ("Identifier", id));
        }


        private static List<LevelStorage> LoadYaml()
        {
            var a = SongData.Instance.Songs;
            Debug.Log(a.Count);
            var yamlFilePath = Path.Combine(Application.streamingAssetsPath, "songs.yaml");
            var uri = new Uri(yamlFilePath);
            var request = UnityWebRequest.Get(uri);
            request.SendWebRequest();
            while (!request.isDone)
            {
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load YAML file: " + request.error);
                return null;
            }

            var yamlText = request.downloadHandler.text;
            var levels = ParseYaml(yamlText);
            return levels;
        }

        private static List<LevelStorage> ParseYaml(string yamlText)
        {
            var deserializer = new DeserializerBuilder()
                .Build();
            var songs = deserializer.Deserialize<List<Dictionary<string, object>>>(yamlText);
            var levels = new List<LevelStorage>();
            foreach (var song in songs)
            {
                var id = song.TryGetValue("id", out var value0) ? value0.ToString() : "Unknown";
                var title = song.TryGetValue("title", out var value) ? value.ToString() : "Unknown";
                var artist = song.TryGetValue("artist", out var value1) ? value1.ToString() : "Unknown";
                var bpm = song.TryGetValue("bpm_base", out var value2) ? value2.ToString() : "Unknown";
                var bg = song.TryGetValue("bg", out var value3) ? value3.ToString() : "Unknown";
                var bgInverse = song.TryGetValue("bg_inverse", out var value4) ? value4.ToString() : "Unknown";
                var side = song.TryGetValue("side", out var value5) ? value5.ToString() : "Unknown";
                var notedesigner = "Unknown";
                var difficultyId = "Unknown";
                var notedesigner2 = "Unknown";
                var difficultyId2 = "Unknown";
                var diff = song.TryGetValue("difficulties", out var diffObj) ? diffObj as List<object> : null;
                var ratingPlusSymbol = "";
                var ratingPlusSymbol2 = "";
                if (diff != null)
                {
                    var masDiff = diff[2];
                    var masDiffDict = masDiff as Dictionary<object, object>;
                    notedesigner = (string)masDiffDict?["chartDesigner"];
                    difficultyId = (string)masDiffDict?["rating"];
                    ratingPlusSymbol = masDiffDict.TryGetValue("ratingPlus", out var ratingPlusObj)
                        ? ratingPlusObj.ToString()
                        : "";
                    if (diff.Count == 4)
                    {
                        var masDiff2 = diff[3];
                        var masDiffDict2 = masDiff2 as Dictionary<object, object>;
                        notedesigner2 = (string)masDiffDict2?["chartDesigner"];
                        difficultyId2 = (string)masDiffDict2?["rating"];
                        ratingPlusSymbol2 = masDiffDict.TryGetValue("ratingPlus", out var ratingPlusObj2)
                            ? ratingPlusObj2.ToString()
                            : "";
                    }

                    if (ratingPlusSymbol == "true") ratingPlusSymbol = "+";
                    if (ratingPlusSymbol2 == "true") ratingPlusSymbol2 = "+";
                }


                var bpmValue = float.TryParse(bpm, out float result) ? result : 0f;
                var customLevel = new LevelStorage
                {
                    Id = levels.Count + 1,
                    Identifier = id,
                    Settings = new ProjectSettings
                    {
                        EditorSettings = new EditorProjectSettings(),
                        Charts = new List<ChartSettings>
                        {
                            new()
                            {
                                Title = title,
                                ChartPath = $"{id}-2",
                                AudioPath = $"{id}",
                                Difficulty = $"MASTER {difficultyId}{ratingPlusSymbol}",
                                BackgroundPath = side == "1" ? bg : bgInverse,
                                Charter = notedesigner,
                                Composer = artist,
                                SearchTags = title,
                                BpmText = bpm,
                                BaseBpm = bpmValue,
                                DifficultyColor = "#9851d3",
                                PreviewStart = 1,
                                PreviewEnd = 100,
                                Skin = new SkinSettings()
                                {
                                    Accent = "conflict", Note = "conflict", Particle = "conflict", Track = "conflict",
                                    Side = "conflict"
                                }
                            }
                        }
                    },
                    AddedDate = DateTime.Now
                };
                if (notedesigner2 != "Unknown" && difficultyId2 != "Unknown")
                {
                    customLevel.Settings.Charts.Add(new ChartSettings()
                    {
                        Title = title,
                        ChartPath = $"{id}-4",
                        AudioPath = $"{id}",
                        BackgroundPath = side == "1" ? bg : bgInverse,
                        Difficulty = $"Re: MASTER {difficultyId2}{ratingPlusSymbol2}",
                        Charter = notedesigner2,
                        Composer = artist,
                        SearchTags = title,
                        BpmText = bpm,
                        BaseBpm = bpmValue,
                        DifficultyColor = "#dba9fe",
                        PreviewStart = 1,
                        PreviewEnd = 100,
                        Skin = new SkinSettings()
                        {
                            Accent = "conflict", Note = "conflict", Particle = "conflict", Track = "conflict",
                            Side = "conflict"
                        }
                    });
                }

                levels.Add(customLevel);
            }

            return levels;
        }

        private static List<LevelStorage> BuildLevels(string pack = null)
        {
            var levels = new List<LevelStorage>();
            var songData = SongData.Instance.Songs;
            foreach (var song in songData)
            {
                foreach (var difficulty in song.difficulties)
                {
                    var skin = (song.side == 1) ? "conflict" : "light";
                    if (pack != null && pack != song.set) continue; 
                    levels.Add(new LevelStorage
                    {
                        Id = levels.Count + 1,
                        Identifier = song.id,
                        Settings = new ProjectSettings
                        {
                            EditorSettings = new EditorProjectSettings(),
                            Charts = new List<ChartSettings>
                            {
                                new()
                                {
                                    Title = song.title_localized.en,
                                    ChartPath = $"{(difficulty.ratingClass == 2?"2":"4")}",
                                    AudioPath = $"{song.id}",
                                    Difficulty = 
                                        $"{(difficulty.ratingClass == 2?"MASTER":"Re:Master")} {difficulty.rating}{(difficulty.ratingPlus ? "+" : "")}",
                                    BackgroundPath = song.bg,
                                    Charter = difficulty.chartDesigner,
                                    Composer = song.artist,
                                    SearchTags = song.title_localized.en,
                                    BpmText = song.bpm,
                                    BaseBpm = song.bpm_base,
                                    DifficultyColor = (difficulty.ratingClass == 2?"#9851d3":"#dba9fe"),
                                    PreviewStart = 1,
                                    PreviewEnd = 100,
                                    Skin = new SkinSettings()
                                    {
                                        Accent = skin, Note = skin, Particle = skin,
                                        Track = skin,
                                        Side = skin
                                    }
                                }
                            }
                        },
                        AddedDate = DateTime.Now
                    });
                }
            }

            return levels;
        }

        public static IEnumerable<LevelStorage> GetAllLevels()
        {
            var levels = BuildLevels();
            return levels;
            //return LevelCollection.FindAll();
        }

        public void ClearLevels()
        {
            LevelCollection.Delete(Query.All());
        }

        public PackStorage GetPack(string id)
        {
            PackStorage pack = PackCollection.FindOne(Query.EQ("Identifier", id));
            if (pack == null)
            {
                return null;
            }

            FetchLevelsForPack(pack);
            return pack;
        }

        public static IEnumerable<PackStorage> GetAllPacks()
        {
            var packData = PackData.Instance.Packs;
            var packs = (from pack in packData
                let imgPath = Path.Combine(Application.streamingAssetsPath, $"pack", $"1080_select_{pack.id}.png")
                select new PackStorage() { PackName = pack.name, ImagePath = imgPath, Identifier = pack.id }).ToList();

            foreach (var pack in packs)
            {
                FetchLevelsForPack(pack);
            }

            return packs;
        }

        public void ClearPacks()
        {
            PackCollection.Delete(Query.All());
        }

        public static void FetchLevelsForPack(PackStorage pack)
        {
            pack.Levels = BuildLevels(pack.Identifier);
            //
            // foreach (var lvid in pack.LevelIdentifiers)
            // {
            //     LevelStorage lv = GetLevel(lvid);
            //     if (lv != null)
            //     {
            //         pack.Levels.Add(lv);
            //     }
            // }
        }

        public CharacterStorage GetCharacter(string id)
        {
            CharacterStorage character = CharacterCollection.FindOne(Query.EQ("Identifier", id));
            if (character == null)
            {
                return null;
            }

            return character;
        }

        public void NotifyStorageChange()
        {
            LevelCollection = Database.Current.GetCollection<LevelStorage>();
            PackCollection = Database.Current.GetCollection<PackStorage>();
            CharacterCollection = Database.Current.GetCollection<CharacterStorage>();

            SelectedPack.SetValueWithoutNotify(GetLastSelectedPack());
            SelectedChart.SetValueWithoutNotify(GetLastSelectedChart(SelectedPack.Value?.Identifier));

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
            {
                jacketPath = packPath;
            }
            else
            {
                jacketPath = Application.streamingAssetsPath + "/songs/" + storage.Identifier + "/base.jpg";
            }
            //Option<string> realJacketPath = storage.GetRealPath(jacketPath);
            // if (!realJacketPath.HasValue)
            // {
            //     image.texture = defaultJacket;
            //     return;
            // }

            //jacketPath = realJacketPath.Value;
            Incompletable<Texture> cachedTexture = JacketCache.Get(jacketPath);
            if (cachedTexture != null)
            {
                while (!cachedTexture.Completed)
                {
                    await UniTask.NextFrame();
                    if (ct.IsCancellationRequested)
                    {
                        return;
                    }
                }

                if (cachedTexture.IsSuccess)
                {
                    image.texture = cachedTexture.Value;
                }

                return;
            }

            Incompletable<Texture> loading = new Incompletable<Texture>();
            JacketCache.Add(jacketPath, loading);
            Uri uri = new Uri(jacketPath);
            using var req = UnityWebRequestTexture.GetTexture(uri);
            await req.SendWebRequest();

            loading.Completed = true;
            if (string.IsNullOrEmpty(req.error))
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(req);
                loading.Value = texture;
                loading.IsSuccess = true;
                if (ct.IsCancellationRequested)
                {
                    return;
                }

                image.texture = texture;
            }
            else
            {
                loading.IsSuccess = false;
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

        public bool TryAssignTextureFromCache(RawImage jacket, IStorageUnit level, string jacketPath,
            string packPath = null)
        {
            if (packPath != null)
            {
                jacketPath = packPath;
            }
            else
            {
                jacketPath = Application.streamingAssetsPath + "/songs/" + level.Identifier + "/base.jpg";
            }

            Option<string> realJacketPath = level.GetRealPath(jacketPath);
            if (!realJacketPath.HasValue)
            {
                return false;
            }

            jacketPath = realJacketPath.Value;

            Incompletable<Texture> texture = JacketCache.Get(jacketPath);
            if (texture != null && texture.Completed && texture.IsSuccess && texture.Value != null)
            {
                jacket.texture = texture.Value;
                return true;
            }

            jacket.texture = defaultJacket;
            return false;
        }

        // public async UniTask<AudioClip> GetAudioClipStreaming(IStorageUnit level, string audioPath)
        // {
        //     var auPath = Path.Combine(Application.streamingAssetsPath,"songs",level.Identifier,"preview.ogg");
        //     Uri uri = new Uri(auPath);
        //     using (UnityWebRequest req = UnityWebRequestMultimedia.GetAudioClip(
        //                uri,
        //                AudioType.OGGVORBIS))
        //     {
        //         req.disposeDownloadHandlerOnDispose = true; // 确保每次请求都会清除下载处理器
        //         ((DownloadHandlerAudioClip)req.downloadHandler).streamAudio = true;
        //         req.SetRequestHeader("Cache-Control", "no-cache"); // 禁用缓存
        //         await req.SendWebRequest();
        //
        //         while (req.result == UnityWebRequest.Result.ConnectionError && req.downloadedBytes < 1024)
        //         {
        //             await UniTask.NextFrame();
        //         }
        //
        //         if (string.IsNullOrEmpty(req.error))
        //         {
        //             AudioClip clip = ((DownloadHandlerAudioClip)req.downloadHandler).audioClip;
        //             return clip;
        //         }
        //         else
        //         {
        //             return null;
        //         }
        //     }
        // }

        public void SwitchToPlayScene((LevelStorage level, ChartSettings chart) selection)
        {
            if (SceneTransitionManager.Instance.IsTransitioning)
            {
                return;
            }

            currentGameplayChart = selection;
            var (level, chart) = selection;

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
                PlayHistory history = PlayHistory.GetHistoryForChart(level.Identifier, chart.ChartPath);
                transitionPlayCount.Value = TextFormat.FormatPlayCount(history.PlayCount + 1);
                transitionRetryCount.Value = TextFormat.FormatRetryCount(1);
            }

            TransitionSequence sequence = new TransitionSequence()
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
            IGameplayControl gameplay = null;
            OnSwitchToGameplayScene?.Invoke();

            // Set the values first to avoid some scencontrol object only reading these values
            // once on awake.
            // Hacky but I'm too tired.
            gameplayData.BaseBpm.Value = chart.BaseBpm;
            gameplayData.Title.Value = chart.Title;
            gameplayData.Composer.Value = chart.Composer;
            gameplayData.DifficultyName.Value = chart.Difficulty;
            SceneTransitionManager.Instance.SwitchScene(
                    SceneNames.GameplayScene,
                    async (rep) =>
                    {
                        if (rep is IGameplayControl gameplayControl)
                        {
                            await new GameplayLoader(gameplayControl, gameplayData).Load(level, chart);
                            gameplay = gameplayControl;
                            gameplay.ShouldNotifyOnAudioEnd = true;
                            gameplay.EnablePauseMenu = true;
                            //gameplay.Audio.AudioTiming = -Values.DelayBeforeAudioStart;
                            gameplayData.PlaybackSpeed.Value = 1;
                            BassAudioService.Instance.AudioPreviewStream.FadeOutAsync(4f).Forget();
                        }
                    },
                    e => { OnSwitchToGameplaySceneException?.Invoke(e); })
                .ContinueWith(() => gameplay?.Audio.PlayWithDelay(0, Values.DelayBeforeAudioStart));


            gameplayData.OnPlayComplete -= OnPlayComplete;
            gameplayData.OnPlayComplete += OnPlayComplete;
        }

        private void SwitchToResultScene(LevelStorage level, ChartSettings chart, PlayResult result, bool isAuto)
        {
            TransitionSequence transition = new TransitionSequence()
                .OnShow()
                .AddTransition(new TriangleTileTransition())
                .OnBoth()
                .AddTransition(new DecorationTransition());
            SceneTransitionManager.Instance.SetTransition(transition);
            SceneTransitionManager.Instance.SwitchScene(
                SceneNames.ResultScene,
                (rep) =>
                {
                    rep.PassData(level, chart, result, isAuto);
                    return default;
                }).Forget();
        }

        public (LevelStorage level, ChartSettings chart) GetLastSelectedChart(string packId)
        {
            string levelId = PlayerPrefs.GetString($"Selection.LastLevel.{packId ?? "all"}", null);
            string chartPath = PlayerPrefs.GetString($"Selection.LastChartPath", null);
            string difficultyName = PlayerPrefs.GetString($"Selection.LastDifficultyName", null);
            double cc = PlayerPrefs.GetFloat($"Selection.LastCc", 0);

            LevelStorage lv = null;
            // if (string.IsNullOrEmpty(levelId))
            // {
            //     PackStorage pack = GetPack(packId);
            //     if (pack == null || pack.Levels.Count <= 0)
            //     {
            //         lv = LevelCollection.FindOne(Query.All());
            //     }
            //     else
            //     {
            //         lv = pack.Levels[0];
            //     }
            // }
            // else
            // {
            //     lv = GetLevel(levelId);
            // }
            //
            // if (lv == null)
            // {
            //     if (SelectedPack.Value != null)
            //     {
            //         lv = SelectedPack.Value.Levels.First();
            //     }
            //     else
            //     {
            //         lv = GetAllLevels().First();
            //     }
            // }

            lv = GetAllLevels().First();

            if (SelectedChart.Value.chart != null)
            {
                foreach (var c in lv.Settings.Charts)
                {
                    if (c.IsSameDifficulty(SelectedChart.Value.chart))
                    {
                        return (lv, c);
                    }
                }

                return (lv, lv.Settings.GetClosestDifficultyToChart(SelectedChart.Value.chart));
            }
            else
            {
                foreach (var c in lv.Settings.Charts)
                {
                    if (c.IsSameDifficulty(chartPath, difficultyName))
                    {
                        return (lv, c);
                    }
                }

                return (lv, lv.Settings.GetClosestDifficultyToConstant(cc, string.Empty));
            }
        }

        public PackStorage GetLastSelectedPack()
        {
            string id = PlayerPrefs.GetString("Selection.LastPack", null);
            if (id == null)
            {
                return null;
            }

            return GetPack(id);
        }

        public CharacterStorage GetSelectedCharacter()
        {
            if (CharacterCollection.Count() == 1)
            {
                return CharacterCollection.FindAll().First();
            }

            string id = PlayerPrefs.GetString("Selection.LastCharacter", null);
            if (id == null)
            {
                return null;
            }

            return GetCharacter(id);
        }

        private static void DestroyCache<T>(Incompletable<T> obj)
            where T : Object
        {
            if (!PersistentCache.Contains(obj.Value))
            {
                Destroy(obj.Value);
            }
            else
            {
                QueuedForDelete.Add(obj.Value);
            }
        }

        private void OnPlayComplete(PlayResult result)
        {
            var (currentLevel, currentChart) = currentGameplayChart;
            PlayHistory history = PlayHistory.GetHistoryForChart(currentLevel.Identifier, currentChart.ChartPath);
            if (!gameplayData.EnableAutoplayMode.Value && !gameplayData.EnablePracticeMode.Value)
            {
                result.BestScore = history.BestScorePlayOrDefault.Score;
                result.PlayCount = history.PlayCount + 1;
                history.AddPlay(result);
                history.Save();
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