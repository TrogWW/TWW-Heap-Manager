using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWWHeapVisualizer.Dolphin;
using TWWHeapVisualizer.Extensions;

namespace TWWHeapVisualizer.Heap.MemoryBlocks
{
    public class MemoryBlockCollection
    {
        protected uint _baseAddress;
        protected uint _mHeadFreeList; //0x78
        protected uint _mTailFreeList; //0x7C
        protected uint _mHeadUsedList; //0x80
        protected uint _mTailUsedList; //0x84
        public int size { get; set; }
        public List<IMemoryBlock> blocks { get; set; }
        public List<FreeMemoryBlock> freeBlocks { get; set; } 
        public List<UsedMemoryBlock> usedBlocks { get; set; }
        public List<uint> filledMemoryBlocks = new List<uint>();
        public int usedSlotsCount
        {
            get
            {
                return this.usedBlocks.Count();
            }
        }
        public int freeSlotsCount
        {
            get
            {
                return this.freeBlocks.Count();
            }
        }
        public MemoryBlockCollection(uint baseAddress)
        {
            _baseAddress = baseAddress;

            blocks = new List<IMemoryBlock>();
            freeBlocks = new List<FreeMemoryBlock>();
            usedBlocks = new List<UsedMemoryBlock>();
            filledMemoryBlocks = new List<uint>();
        }
        public virtual void UpdateBlocks()
        {
            uint heapPtr = Memory.ReadMemory<uint>(_baseAddress);
            size = Memory.ReadMemory<int>(heapPtr + 0x38);
            _mHeadFreeList = Memory.ReadMemory<uint>(heapPtr + 0x78);
            _mTailFreeList = Memory.ReadMemory<uint>(heapPtr + 0x7C);
            _mHeadUsedList = Memory.ReadMemory<uint>(heapPtr + 0x80);
            _mTailUsedList = Memory.ReadMemory<uint>(heapPtr + 0x84);

            UpdateFreeBlocks();
            UpdateUsedBlocks();
            blocks = new List<IMemoryBlock>();
            blocks.AddRange(freeBlocks);
            blocks.AddRange(usedBlocks);
            blocks = blocks.OrderBy(b => b.startAddress).ToList();
        }
        private void UpdateFreeBlocks()
        {
            freeBlocks = new List<FreeMemoryBlock>();
            uint ptr = _mHeadFreeList;
            while (ptr != 0)
            {
                var blk = CMemBlock.FromAddress(ptr);
                var free = new FreeMemoryBlock(ptr, ptr + (blk.size + 0x10));
                freeBlocks.Add(free);
                ptr = blk.mNext;
            };
        }
        private void UpdateUsedBlocks()
        {
            usedBlocks = new List<UsedMemoryBlock>();
            uint ptr = _mHeadUsedList;
            int index = 0;
            while (ptr != 0)
            {
                var blk = CMemBlock.FromAddress(ptr);
                var used = new UsedMemoryBlock(ptr, index);
                used.filled = filledMemoryBlocks.Contains(ptr);
                usedBlocks.Add(used);
                ptr = blk.mNext;
                index++;
            }
        }
    }
}
