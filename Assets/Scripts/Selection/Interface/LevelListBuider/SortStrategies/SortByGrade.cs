using System.Collections.Generic;
using System.Linq;
using ArcCreate.Gameplay.Score;
using ArcCreate.Storage;

namespace ArcCreate.Selection.Interface
{
    public class SortByGrade : ISortStrategy
    {
        public const string Typename = "grade";

        public List<LevelCellData> Sort(List<LevelCellData> cells)
        {
            if (cells.Count == 0) return cells;

            return cells
                .OrderBy(cell => GetGrade(cell))
                .ThenBy(cell => SongDifficultyUtility.GetTitle(cell.Song))
                .ToList();
        }

        private static int GetGrade(LevelCellData cell)
        {
            if (cell?.Song == null || cell.DifficultyToDisplay == null) return -1;

            var difficulty = SongDifficultyUtility.GetApiDifficulty(cell.DifficultyToDisplay);
            return ScoreCache.TryGetScore(cell.Song.id, difficulty, out _, out var grade) ? grade : -1;
        }
    }
}