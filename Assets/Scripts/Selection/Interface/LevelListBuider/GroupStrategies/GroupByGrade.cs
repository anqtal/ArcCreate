using System.Collections.Generic;
using System.Linq;
using ArcCreate.Gameplay.Score;
using ArcCreate.Storage;
using ArcCreate.Utility.InfiniteScroll;

namespace ArcCreate.Selection.Interface
{
    public class GroupByGrade : IGroupStrategy
    {
        public const string Typename = "grade";

        public List<CellData> GroupCells(List<LevelCellData> cells, ISortStrategy sortStrategy)
        {
            if (cells.Count == 0) return new List<CellData>();

            var groups = new Dictionary<int, List<LevelCellData>>();
            foreach (var cell in cells)
            {
                var grade = GetGrade(cell);
                if (!groups.TryGetValue(grade, out var list))
                {
                    list = new List<LevelCellData>();
                    groups[grade] = list;
                }

                list.Add(cell);
            }

            var result = new List<CellData>();
            foreach (var pair in groups.OrderBy(pair => pair.Key))
            {
                var newGroup = new GroupCellData
                {
                    Pool = Pools.Get<Cell>("GroupCell"),
                    Size = LevelList.GroupCellSize,
                    Children = sortStrategy.Sort(pair.Value).ToList<CellData>(),
                    Title = $"Grade {FormatGradeLabel(pair.Key)}"
                };
                result.Add(newGroup);
            }

            return result;
        }

        private static int GetGrade(LevelCellData cell)
        {
            if (cell?.Song == null || cell.DifficultyToDisplay == null) return -1;

            var difficulty = SongDifficultyUtility.GetApiDifficulty(cell.DifficultyToDisplay);
            return ScoreCache.TryGetScore(cell.Song.id, difficulty, out _, out var grade) ? grade : -1;
        }

        private static string FormatGradeLabel(int grade)
        {
            if (grade < 0) return "Unknown";

            return grade switch
            {
                6 => "EX+",
                5 => "EX",
                4 => "AA",
                3 => "A",
                2 => "B",
                1 => "C",
                0 => "D",
                _ => grade.ToString()
            };
        }
    }
}