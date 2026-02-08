using System;
using System.Collections.Generic;
using System.Linq;
using ArcCreate.Storage;
using ArcCreate.Utility.InfiniteScroll;
using FuzzySharp;
using FuzzySharp.PreProcess;
using UnityEngine;

namespace ArcCreate.Selection.Interface
{
    public class LevelListBuilder
    {
        public static List<CellData> Build(List<SongList> levels, Difficulty selectedDifficulty,
            IGroupStrategy groupStrategy, ISortStrategy sortStrategy)
        {
            var difficultyMatchLevelCells = new List<LevelCellData>();
            var otherLevelCells = new List<LevelCellData>();
            var pool = Pools.Get<Cell>("LevelCell");

            foreach (var level in levels)
            {
                var charts = SongDifficultyUtility.GetPlayableDifficulties(level);
                if (charts.Count == 0) continue;

                Difficulty matchingChart = null;
                foreach (var chart in charts)
                    if (SongDifficultyUtility.IsSameDifficulty(chart, selectedDifficulty))
                        matchingChart = chart;

                if (matchingChart != null)
                {
                    var cellData = new LevelCellData
                    {
                        Pool = pool,
                        DifficultyToDisplay = matchingChart,
                        Song = level,
                        Difficulties = charts,
                        Size = LevelList.LevelCellSize
                    };
                    difficultyMatchLevelCells.Add(cellData);
                }
                else
                {
                    var closestChart = charts[0];
                    var cellData = new LevelCellData
                    {
                        Pool = pool,
                        DifficultyToDisplay = closestChart,
                        Song = level,
                        Difficulties = charts,
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

        public static List<CellData> Filter(List<SongList> levels, Difficulty selectedDifficulty,
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
                var charts = SongDifficultyUtility.GetPlayableDifficulties(level);
                for (var i = charts.Count - 1; i >= 0; i--)
                {
                    var chart = charts[i];
                    var score = SearchScore(level, chart, searchQuery);
                    if (score < threshold)
                    {
                        charts.RemoveAt(i);
                        continue;
                    }

                    levelCellScore = Math.Max(levelCellScore, score);
                }

                if (levelCellScore > int.MinValue && charts.Count > 0)
                {
                    Difficulty matchingChart = null;
                    foreach (var chart in charts)
                        if (SongDifficultyUtility.IsSameDifficulty(chart, selectedDifficulty))
                            matchingChart = chart;

                    matchingChart ??= charts[0];

                    filtered.Add(levelCellScore, new LevelCellData
                    {
                        Pool = pool,
                        DifficultyToDisplay = matchingChart,
                        Song = level,
                        Difficulties = charts,
                        Size = LevelList.LevelCellSize
                    });
                }
            }

            return filtered.Values.ToList();
        }

        private static int SearchScore(SongList song, Difficulty difficulty, string query)
        {
            return Mathf.Max(
                FieldScore(query, SongDifficultyUtility.GetTitle(song)),
                FieldScore(query, SongDifficultyUtility.GetComposer(song)),
                FieldScore(query, SongDifficultyUtility.GetCharter(difficulty)),
                FieldScore(query, SongDifficultyUtility.GetSearchTags(song)));
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