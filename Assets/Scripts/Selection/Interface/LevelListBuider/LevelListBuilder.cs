using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ArcCreate.Data;
using ArcCreate.Storage.Data;
using ArcCreate.Utility.InfiniteScroll;
using FuzzySharp;
using FuzzySharp.PreProcess;
using UnityEngine;
using YamlDotNet.Serialization;

namespace ArcCreate.Selection.Interface
{
    public class LevelListBuilder
    {

        private static List<LevelStorage> LoadYaml()
        {
            var yamlFilePath = Path.Combine(Application.streamingAssetsPath, "songs.yaml");
            var yamlText = File.ReadAllText(yamlFilePath);
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
                    ratingPlusSymbol = masDiffDict.TryGetValue("ratingPlus",out var ratingPlusObj) ? ratingPlusObj.ToString() : "";
                    if (diff.Count == 4)
                    {
                        var masDiff2 = diff[3];
                        var masDiffDict2 = masDiff2 as Dictionary<object, object>;
                        notedesigner2 = (string)masDiffDict2?["chartDesigner"];
                        difficultyId2 = (string)masDiffDict2?["rating"];
                        ratingPlusSymbol2 = masDiffDict.TryGetValue("ratingPlus",out var ratingPlusObj2) ? ratingPlusObj2.ToString() : "";
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
                                Charter = notedesigner,
                                Composer = artist,
                                SearchTags = title,
                                BpmText = bpm,
                                BaseBpm = bpmValue,
                                DifficultyColor = "#9851d3",
                                PreviewStart = 1,
                                PreviewEnd = 100,
                                Skin = new SkinSettings(){Accent = "conflict", Note = "conflict", Particle = "conflict", Track = "conflict", Side = "conflict"}
                            }
                        }
                    },
                    AddedDate = DateTime.Now
                };
                if (notedesigner2 != "Unknown" && difficultyId2 != "Unknown")
                {
                    customLevel.Settings.Charts.Add(new ChartSettings(){
                        Title = title,
                        ChartPath = $"{id}-4",
                        AudioPath = $"{id}",
                        Difficulty = $"Re: MASTER {difficultyId2}{ratingPlusSymbol2}",
                        Charter = notedesigner2,
                        Composer = artist,
                        SearchTags = title,
                        BpmText = bpm,
                        BaseBpm = bpmValue,
                        DifficultyColor = "#dba9fe",
                        PreviewStart = 1,
                        PreviewEnd = 100,
                        Skin = new SkinSettings(){Accent = "conflict", Note = "conflict", Particle = "conflict", Track = "conflict", Side = "conflict"}
                    });
                }

                levels.Add(customLevel);
            }

            return levels;
        }

        public static List<CellData> Build(List<LevelStorage> levels, ChartSettings selectedChart,
            IGroupStrategy groupStrategy, ISortStrategy sortStrategy)
        {
            var difficultyMatchLevelCells = new List<LevelCellData>();
            var otherLevelCells = new List<LevelCellData>();
            var pool = Pools.Get<Cell>("LevelCell");
            //levels = LoadYaml();

            foreach (var level in levels)
            {
                // string json = JsonUtility.ToJson(levels);
                ChartSettings matchingChart = null;
                foreach (var chart in level.Settings.Charts)
                    if (chart.IsSameDifficulty(selectedChart))
                        matchingChart = chart;

                if (matchingChart != null)
                {
                    var history = PlayHistory.GetHistoryForChart(level.Identifier, matchingChart.ChartPath);
                    var cellData = new LevelCellData
                    {
                        Pool = pool,
                        ChartToDisplay = matchingChart,
                        LevelStorage = level,
                        PlayHistory = history,
                        Size = LevelList.LevelCellSize
                    };
                    difficultyMatchLevelCells.Add(cellData);
                }
                else
                {
                    var closestChart = level.Settings.GetClosestDifficultyToChart(selectedChart);
                    // PlayHistory history = PlayHistory.GetHistoryForChart(level.Identifier, closestChart.ChartPath);
                    var cellData = new LevelCellData
                    {
                        Pool = pool,
                        ChartToDisplay = closestChart,
                        LevelStorage = level,
                        //PlayHistory = history,
                        Size = LevelList.LevelCellSize
                    };
                    otherLevelCells.Add(cellData);
                }
            }

            var result = groupStrategy.GroupCells(difficultyMatchLevelCells, sortStrategy);
            var otherDifficultiesGroup = groupStrategy.GroupCells(otherLevelCells, sortStrategy);

            var otherDifficultiesFolder = new GroupCellData
            {
                Title = I18n.S("Gameplay.Selection.List.OtherDifficulties"),
                Children = otherDifficultiesGroup,
                Pool = Pools.Get<Cell>("GroupCell"),
                Size = LevelList.GroupCellSize
            };

            result.Add(otherDifficultiesFolder);
            return result;
        }

        public static List<CellData> Filter(List<LevelStorage> levels, ChartSettings selectedChart,
            string searchQuery,
            int threshold = 60)
        {
            var pool = Pools.Get<Cell>("LevelCell");
            if (levels.Count == 0) return null;

            var filtered = new SortedDictionary<int, CellData>(new ChartScoreComparer());
            searchQuery = searchQuery.ToLower();
            foreach (var level in levels)
            {
                var levelCellScore = int.MinValue;
                for (var i = level.Settings.Charts.Count - 1; i >= 0; i--)
                {
                    var chart = level.Settings.Charts[i];
                    var score = SearchScore(chart, searchQuery);
                    if (score < threshold)
                    {
                        level.Settings.Charts.RemoveAt(i);
                        continue;
                    }

                    levelCellScore = Math.Max(levelCellScore, score);
                }

                if (levelCellScore > int.MinValue && level.Settings.Charts.Count > 0)
                {
                    ChartSettings matchingChart = null;
                    foreach (var chart in level.Settings.Charts)
                        if (chart.IsSameDifficulty(selectedChart))
                            matchingChart = chart;

                    matchingChart = matchingChart ?? level.Settings.GetClosestDifficultyToChart(selectedChart);

                    var history = PlayHistory.GetHistoryForChart(level.Identifier, matchingChart.ChartPath);
                    filtered.Add(levelCellScore, new LevelCellData
                    {
                        Pool = pool,
                        ChartToDisplay = matchingChart,
                        LevelStorage = level,
                        PlayHistory = history,
                        Size = LevelList.LevelCellSize
                    });
                }
            }

            return filtered.Values.ToList();
        }

        private static int SearchScore(ChartSettings chart, string query)
        {
            return Mathf.Max(
                FieldScore(query, chart.Title),
                FieldScore(query, chart.Composer),
                FieldScore(query, chart.Charter),
                FieldScore(query, chart.SearchTags));
        }

        private static int FieldScore(string query, string str)
        {
            if (string.IsNullOrWhiteSpace(str)) return 0;

            return Fuzz.PartialRatio(query, str.ToLower(), PreprocessMode.None);
        }

        public class ChartScoreComparer : IComparer<int>
        {
            public int Compare(int x, int y)
            {
                var result = x.CompareTo(y);

                return result == 0 ? -1 : -result;
            }
        }
    }
}