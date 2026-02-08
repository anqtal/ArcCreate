using ArcCreate.Storage;
using ArcCreate.Utility.InfiniteScroll;

namespace ArcCreate.Selection.Interface
{
    public class PackCellData : CellData
    {
        public Pack Pack { get; set; }

        public int PackIndex { get; set; }
    }
}