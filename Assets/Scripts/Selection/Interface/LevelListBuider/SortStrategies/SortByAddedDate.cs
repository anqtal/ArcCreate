using System.Collections.Generic;
using System.Linq;
using ArcCreate.Storage;

namespace ArcCreate.Selection.Interface
{
    public class SortByAddedDate : ISortStrategy
    {
        public const string Typename = "addeddate";

        public List<LevelCellData> Sort(List<LevelCellData> cells)
        {
            if (cells.Count == 0) return cells;

            return cells
                .OrderBy(cell => cell.Song?.date ?? 0)
                .ThenBy(cell => SongDifficultyUtility.GetTitle(cell.Song))
                .ToList();
        }
    }
}