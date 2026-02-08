using System.Collections.Generic;
using System.Linq;
using ArcCreate.Storage;

namespace ArcCreate.Selection.Interface
{
    public class SortByDifficulty : ISortStrategy
    {
        public const string Typename = "difficulty";

        public List<LevelCellData> Sort(List<LevelCellData> cells)
        {
            if (cells.Count == 0) return cells;

            return cells
                .OrderBy(cell =>
                {
                    var (diff, isPlus) = SongDifficultyUtility.ParseChartConstant(cell.DifficultyToDisplay);
                    return isPlus ? diff + 0.1f : diff;
                })
                .ThenBy(cell => SongDifficultyUtility.GetTitle(cell.Song))
                .ToList();
        }
    }
}