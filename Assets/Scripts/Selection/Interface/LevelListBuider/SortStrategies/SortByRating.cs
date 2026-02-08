using System.Collections.Generic;
using System.Linq;
using ArcCreate.Storage;

namespace ArcCreate.Selection.Interface
{
    public class SortByRating : ISortStrategy
    {
        public const string Typename = "rating";

        public List<LevelCellData> Sort(List<LevelCellData> cells)
        {
            if (cells.Count == 0) return cells;

            return cells
                .OrderBy(cell => SongDifficultyUtility.GetChartConstant(cell.DifficultyToDisplay))
                .ThenBy(cell => SongDifficultyUtility.GetTitle(cell.Song))
                .ToList();
        }
    }
}