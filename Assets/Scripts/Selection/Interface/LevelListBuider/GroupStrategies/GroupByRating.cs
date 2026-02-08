using System.Collections.Generic;
using System.Linq;
using ArcCreate.Storage;
using ArcCreate.Utility.InfiniteScroll;

namespace ArcCreate.Selection.Interface
{
    public class GroupByRating : IGroupStrategy
    {
        public const string Typename = "rating";

        public List<CellData> GroupCells(List<LevelCellData> cells, ISortStrategy sortStrategy)
        {
            if (cells.Count == 0) return new List<CellData>();

            var groups = new List<(int rating, bool isPlus, List<LevelCellData> cells)>();
            cells = cells
                .OrderBy(cell => SongDifficultyUtility.GetChartConstant(cell.DifficultyToDisplay))
                .ThenBy(cell => SongDifficultyUtility.GetTitle(cell.Song))
                .ToList();

            var (rating, isPlus) = SongDifficultyUtility.ParseChartConstant(cells[0].DifficultyToDisplay);
            groups.Add((rating, isPlus, new List<LevelCellData>()));

            foreach (var cell in cells)
            {
                var (diff, plus) = SongDifficultyUtility.ParseChartConstant(cell.DifficultyToDisplay);
                if (diff != rating || plus != isPlus)
                {
                    rating = diff;
                    isPlus = plus;
                    groups.Add((diff, plus, new List<LevelCellData>()));
                }

                groups[groups.Count - 1].cells.Add(cell);
            }

            var result = new List<CellData>();
            foreach (var (diff, plus, group) in groups)
            {
                var newGroup = new GroupCellData
                {
                    Pool = Pools.Get<Cell>("GroupCell"),
                    Size = LevelList.GroupCellSize,
                    Children = sortStrategy.Sort(group).ToList<CellData>(),
                    Title = $"Rating {diff}{(plus ? "+" : string.Empty)}"
                };
                result.Add(newGroup);
            }

            return result;
        }
    }
}