using System.Collections.Generic;
using System.Linq;
using ArcCreate.Gameplay.Score;

namespace ArcCreate.Selection.Interface
{
    public class SortByScore : ISortStrategy
    {
        public const string Typename = "score";

        public List<LevelCellData> Sort(List<LevelCellData> cells)
        {
            if (cells.Count == 0) return cells;

            return cells
                .OrderBy(cell =>
                    ScoreCache.GetScoreOrDefault(cell.Song?.id, cell.DifficultyToDisplay?.ratingClass ?? 0))
                .ToList();
        }
    }
}