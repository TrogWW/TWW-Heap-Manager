using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWWHeapVisualizer.Heap.MemoryBlocks
{
    public interface IMemoryBlock
    {
        uint startAddress { get; set; }
        uint endAddress { get; set; }
        uint size { get; set; }
        ListViewItem GetDisplayInfo();
    }
}
