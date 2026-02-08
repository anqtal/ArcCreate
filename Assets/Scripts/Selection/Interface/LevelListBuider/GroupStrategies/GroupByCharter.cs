using System.Collections.Generic;
using System.Linq;
using ArcCreate.Storage;
using ArcCreate.Utility.InfiniteScroll;

namespace ArcCreate.Selection.Interface
{
    public class GroupByCharter : IGroupStrategy
    {
        public const string Typename = "charter";

        public List<CellData> GroupCells(List<LevelCellData> cells, ISortStrategy sortStrategy)
        {
            if (cells.Count == 0) return new List<CellData>();

            List<(string name, List<LevelCellData> cells)> groups = new();

            cells = cells
                .OrderBy(cell => SongDifficultyUtility.GetCharter(cell.DifficultyToDisplay))
                .ThenBy(cell => SongDifficultyUtility.GetTitle(cell.Song))
                .ToList();

            // Sort to folders
            var cname = GetCharterName(SongDifficultyUtility.GetCharter(cells[0].DifficultyToDisplay));
            groups.Add((cname, new List<LevelCellData>()));

            foreach (var level in cells)
            {
                var name = GetCharterName(SongDifficultyUtility.GetCharter(level.DifficultyToDisplay));
                if (name != cname)
                {
                    cname = name;
                    groups.Add((name, new List<LevelCellData>()));
                }

                groups[groups.Count - 1].cells.Add(level);
            }

            var groupCells = new List<CellData>();
            foreach (var (name, group) in groups)
            {
                var newGroup = new GroupCellData
                {
                    Pool = Pools.Get<Cell>("GroupCell"),
                    Size = LevelList.GroupCellSize,
                    Children = sortStrategy.Sort(group).ToList<CellData>(),
                    Title = name
                };
                groupCells.Add(newGroup);
            }

            return groupCells;
        }

        private string GetCharterName(string charter)
        {
            return string.IsNullOrWhiteSpace(charter) ? "Unknown" : charter;
        }
    }
}