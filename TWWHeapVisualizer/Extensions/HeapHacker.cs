using System.Runtime.InteropServices;
using TWWHeapVisualizer.Dolphin;
using TWWHeapVisualizer.Heap;
using TWWHeapVisualizer.Heap.MemoryBlocks;

namespace TWWHeapVisualizer.Extensions
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct CMemBlock
    {
        public ushort mMagic;    // 'HM' = 0x4D48 for used, 0 for free
        public byte mFlags;    // low‑bit=tail‑alloc flag (0x80), low‑7 bits=alignment
        public byte mGroupId;  // block group ID
        public uint size;      // content size (excludes header)
        public uint mPrev;     // header address of previous block in list
        public uint mNext;     // header address of next block

        public static CMemBlock FromAddress(uint headerAddr)
            => new CMemBlock
            {
                mMagic = Memory.ReadMemory<ushort>(headerAddr + 0x0),
                mFlags = Memory.ReadMemory<byte>(headerAddr + 0x2),
                mGroupId = Memory.ReadMemory<byte>(headerAddr + 0x3),
                size = Memory.ReadMemory<uint>(headerAddr + 0x4),
                mPrev = Memory.ReadMemory<uint>(headerAddr + 0x8),
                mNext = Memory.ReadMemory<uint>(headerAddr + 0xC)
            };

        public void WriteToAddress(uint headerAddr)
        {
            Memory.WriteMemory<ushort>(headerAddr + 0x0, mMagic);
            Memory.WriteMemory<byte>(headerAddr + 0x2, mFlags);
            Memory.WriteMemory<byte>(headerAddr + 0x3, mGroupId);
            Memory.WriteMemory<uint>(headerAddr + 0x4, size);
            Memory.WriteMemory<uint>(headerAddr + 0x8, mPrev);
            Memory.WriteMemory<uint>(headerAddr + 0xC, mNext);
        }
    }

    public static class HeapHacker
    {
        const int HeaderSize = 0x10;    // sizeof(CMemBlock)
        const uint OffHeadFreeList = 0x78;
        const uint OffTailFreeList = 0x7C;
        const uint OffHeadUsedList = 0x80;
        const uint OffTailUsedList = 0x84;

        /// <summary>
        /// Carves out [regionStart, regionEnd) from freeBlock and marks it used,
        /// handling tail‐of‐heap allocations correctly.
        /// </summary>
        public static void FakeAllocate(
            IMemoryBlock freeBlock,
            uint regionStart,
            uint regionEnd
        )
        {
            // 1) get heap base
            uint heapBase = Memory.ReadMemory<uint>(ActorData.zeldaHeapPtr);

            // 2) compute header and content bounds
            uint oldHdr = freeBlock.startAddress;      // header address
            uint contentStart = oldHdr + HeaderSize;
            uint contentEnd = freeBlock.endAddress - (uint)HeaderSize;

            // 3) read original free block
            var oldFree = CMemBlock.FromAddress(oldHdr);
            uint prevFree = oldFree.mPrev;
            uint nextFree = oldFree.mNext;

            // 4) compute content sizes for new fragments
            uint leftContentSize = regionStart - contentStart;
            uint rightContentSize = contentEnd - regionEnd;
            bool hasLeft = leftContentSize >= HeaderSize;
            bool hasRight = rightContentSize >= HeaderSize;

            // header ptrs for new blocks
            uint leftHdr = oldHdr;
            uint usedHdr = regionStart - HeaderSize;
            uint rightHdr = regionEnd - HeaderSize;

            // 5) write left‐free fragment
            if (hasLeft)
            {
                var left = new CMemBlock
                {
                    mMagic = 0,
                    mFlags = 0,      // clear flags for free fragment,
                    mGroupId = 0,
                    size = leftContentSize,
                    mPrev = prevFree,
                    mNext = hasRight ? rightHdr : nextFree
                };
                left.WriteToAddress(leftHdr);
            }

            // 6) write right‐free fragment
            if (hasRight)
            {
                var right = new CMemBlock
                {
                    mMagic = 0,
                    mFlags = oldFree.mFlags,
                    mGroupId = 0,
                    size = rightContentSize,
                    mPrev = hasLeft ? leftHdr : prevFree,
                    mNext = nextFree
                };
                right.WriteToAddress(rightHdr);
            }

            // 7) relink free‐list
            if (!hasLeft && !hasRight)
            {
                // remove the only block
                if (prevFree != 0)
                {
                    var pf = CMemBlock.FromAddress(prevFree);
                    pf.mNext = nextFree;
                    pf.WriteToAddress(prevFree);
                }
                else Memory.WriteMemory<uint>(heapBase + OffHeadFreeList, nextFree);

                if (nextFree != 0)
                {
                    var nf = CMemBlock.FromAddress(nextFree);
                    nf.mPrev = prevFree;
                    nf.WriteToAddress(nextFree);
                }
                else Memory.WriteMemory<uint>(heapBase + OffTailFreeList, prevFree);
            }
            else if (hasLeft && !hasRight)
            {
                // keep only left
                if (prevFree != 0)
                {
                    var pf = CMemBlock.FromAddress(prevFree);
                    pf.mNext = leftHdr;
                    pf.WriteToAddress(prevFree);
                }
                else Memory.WriteMemory<uint>(heapBase + OffHeadFreeList, leftHdr);

                if (nextFree != 0)
                {
                    var nf = CMemBlock.FromAddress(nextFree);
                    nf.mPrev = leftHdr;
                    nf.WriteToAddress(nextFree);
                }
                else Memory.WriteMemory<uint>(heapBase + OffTailFreeList, leftHdr);
            }
            else if (!hasLeft && hasRight)
            {
                // keep only right
                if (prevFree != 0)
                {
                    var pf = CMemBlock.FromAddress(prevFree);
                    pf.mNext = rightHdr;
                    pf.WriteToAddress(prevFree);
                }
                else Memory.WriteMemory<uint>(heapBase + OffHeadFreeList, rightHdr);

                if (nextFree != 0)
                {
                    var nf = CMemBlock.FromAddress(nextFree);
                    nf.mPrev = rightHdr;
                    nf.WriteToAddress(nextFree);
                }
                else Memory.WriteMemory<uint>(heapBase + OffTailFreeList, rightHdr);
            }
            else // hasLeft && hasRight
            {
                if (prevFree != 0)
                {
                    var pf = CMemBlock.FromAddress(prevFree);
                    pf.mNext = leftHdr;
                    pf.WriteToAddress(prevFree);
                }
                else Memory.WriteMemory<uint>(heapBase + OffHeadFreeList, leftHdr);

                if (nextFree != 0)
                {
                    var nf = CMemBlock.FromAddress(nextFree);
                    nf.mPrev = rightHdr;
                    nf.WriteToAddress(nextFree);
                }
                else Memory.WriteMemory<uint>(heapBase + OffTailFreeList, rightHdr);
            }

            // 8) create used‐block header (handle tail flag)
            bool isTail = (regionEnd == contentEnd);
            var used = new CMemBlock
            {
                mMagic = 0x4D48,                  // 'HM'
                mFlags = isTail ? (byte)0x80 : (byte)0x00,
                mGroupId = 0xFF,
                size = regionEnd - regionStart,
                mPrev = Memory.ReadMemory<uint>(heapBase + OffTailUsedList),
                mNext = 0
            };
            used.WriteToAddress(usedHdr);

            // 9) append to used‐list
            uint oldTail = Memory.ReadMemory<uint>(heapBase + OffTailUsedList);
            if (oldTail != 0)
            {
                var tailBlk = CMemBlock.FromAddress(oldTail);
                tailBlk.mNext = usedHdr;
                tailBlk.WriteToAddress(oldTail);
            }
            else Memory.WriteMemory<uint>(heapBase + OffHeadUsedList, usedHdr);

            Memory.WriteMemory<uint>(heapBase + OffTailUsedList, usedHdr);
        }
    }
}
