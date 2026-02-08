using System.Collections.Generic;
using System.Linq;
using ArcCreate.Storage;
using ArcCreate.Utility.InfiniteScroll;

namespace ArcCreate.Selection.Interface
{
    public class GroupByDifficulty : IGroupStrategy
    {
        public const string Typename = "difficulty";

        public List<CellData> GroupCells(List<LevelCellData> cells, ISortStrategy sortStrategy)
        {
            if (cells.Count == 0) return new List<CellData>();

            List<(int diff, bool isPlus, List<LevelCellData> cells)> groups = new();

            cells = cells
                .OrderBy(cell =>
                {
                    var (diff, isPlus) = SongDifficultyUtility.ParseChartConstant(cell.DifficultyToDisplay);
                    return isPlus ? diff + 0.1f : diff;
                })
                .ThenBy(cell => SongDifficultyUtility.GetTitle(cell.Song))
                .ToList();

            // Sort to folders
            var (cdiff, cisPlus) = SongDifficultyUtility.ParseChartConstant(cells[0].DifficultyToDisplay);
            groups.Add((cdiff, cisPlus, new List<LevelCellData>()));

            foreach (var level in cells)
            {
                var (diff, isPlus) = SongDifficultyUtility.ParseChartConstant(level.DifficultyToDisplay);
                if (diff != cdiff || cisPlus != isPlus)
                {
                    cdiff = diff;
                    cisPlus = isPlus;
                    groups.Add((diff, isPlus, new List<LevelCellData>()));
                }

                groups[groups.Count - 1].cells.Add(level);
            }

            var groupCells = new List<CellData>();
            foreach (var (diff, isPlus, group) in groups)
            {
                var newGroup = new GroupCellData
                {
                    Pool = Pools.Get<Cell>("GroupCell"),
                    Size = LevelList.GroupCellSize,
                    Children = sortStrategy.Sort(group).ToList<CellData>(),
                    Title = $"LEVEL {diff}{(isPlus ? "+" : "")}"
                };
                groupCells.Add(newGroup);
            }

            return groupCells;
        }
    }
}