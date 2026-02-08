using System.Collections.Generic;
using ArcCreate.Storage;
using ArcCreate.Utility.InfiniteScroll;

namespace ArcCreate.Selection.Interface
{
    public class LevelCellData : CellData
    {
        public SongList Song { get; set; }

        public Difficulty DifficultyToDisplay { get; set; }

        public List<Difficulty> Difficulties { get; set; }
    }
}