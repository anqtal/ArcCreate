using System.Collections.Generic;
using System.Linq;
using ArcCreate.Storage;

namespace ArcCreate.Selection.Interface
{
    public class SortByPlayCount : ISortStrategy
    {
        public const string Typename = "playcount";

        public List<LevelCellData> Sort(List<LevelCellData> cells)
        {
            if (cells.Count == 0) return cells;

            return cells
                .OrderBy(cell => 0)
                .ThenBy(cell => SongDifficultyUtility.GetTitle(cell.Song))
                .ToList();
        }
    }
}