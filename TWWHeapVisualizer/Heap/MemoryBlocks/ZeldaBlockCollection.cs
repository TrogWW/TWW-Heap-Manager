using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWWHeapVisualizer.Dolphin;
using TWWHeapVisualizer.Helpers;

namespace TWWHeapVisualizer.Heap.MemoryBlocks
{
    public class ZeldaBlockCollection : MemoryBlockCollection
    {
        private readonly uint _fopActQueueAddress;
        private Dictionary<uint, fopAc_ac_c> _actors { get; set; }
        public ZeldaBlockCollection(uint heapPtr, uint fopActQueueHead) : base(heapPtr)
        {
            _fopActQueueAddress = Memory.ReadMemory<uint>(fopActQueueHead);
            _actors = new Dictionary<uint, fopAc_ac_c>();
        }
        public override void UpdateBlocks()
        {
            base.UpdateBlocks();
            _actors = fopAc_ac_c.GetCreatedActors(_fopActQueueAddress);
            foreach (UsedMemoryBlock usedBlock in usedBlocks.Where(b => MemoryHelpers.isValidAddress(b.gamePtr)))
            {
                if (_actors.ContainsKey(usedBlock.gamePtr))
                {
                    usedBlock.actor = _actors[usedBlock.gamePtr];
                }
            }
        }
    }
}
