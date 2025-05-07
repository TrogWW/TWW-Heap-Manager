using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWWHeapVisualizer.Heap.MemoryBlocks
{
    public class GameBlockCollection : MemoryBlockCollection
    {
        private ZeldaBlockCollection _zeldaHeap;
        public GameBlockCollection(uint baseAddress, ZeldaBlockCollection zeldaHeap) : base(baseAddress)
        {
            _zeldaHeap = zeldaHeap;
        }
        public override void UpdateBlocks()
        {
            base.UpdateBlocks();
            foreach (UsedMemoryBlock uBlock in usedBlocks)
            {
                UsedMemoryBlock zeldaHeapBlock = _zeldaHeap.usedBlocks.FirstOrDefault(b => b.gamePtr >= uBlock.startAddress && b.gamePtr < uBlock.endAddress);
                if (zeldaHeapBlock != null)
                {
                    uBlock.data = zeldaHeapBlock.data;
                }
            }
        }
    }
}
