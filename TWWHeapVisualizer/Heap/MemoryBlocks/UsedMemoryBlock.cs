using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWWHeapVisualizer.Dolphin;
using TWWHeapVisualizer.Helpers;

namespace TWWHeapVisualizer.Heap.MemoryBlocks
{
    public class UsedMemoryBlock : IMemoryBlock
    {
        public static Color color = Color.FromArgb(255, 125, 98);
        public const UInt64 unkownOffset = 0x2;
        public const UInt64 usedOrFreeOffset = 0x3; //0x00 for free blocks, 0xFF for used blocks?
        public const UInt64 sizeOffset = 0x4;
        public const UInt64 prevBlockOffset = 0x8;
        public const UInt64 nextBlockOffset = 0xC;
        public const UInt64 itemID_Offset = 0x1E;
        public const UInt64 gamePtrOffset = 0x100;
        public const UInt64 actorDataOffset = 0x10;
        public const uint actorTypeOffset = 192 + 16;
        public const uint ACTOR_TYPE = 152240133;
        public const uint MIN_ACTOR_SIZE = 192 + 16;
        public byte unknownValue { get; set; }
        public byte usedOrFree { get; set; }
        public uint startAddress { get; set; }
        public uint endAddress { get; set; }
        public uint size { get; set; }
        public uint prevBlock { get; set; }
        public uint nextBlock { get; set; }
        public int index { get; set; }
        public ushort itemID { get; set; }
        public uint bsPcId { get; set; }
        public fopAc_ac_c actor { get; set; }
        public ObjectName data { get; set; }
        public string relFileName { get; set; }
        public uint relPointer { get; set; }
        public uint gamePtr { get; set; }
        public bool filled { get; set; } //whether or not block won't be cleared
        public uint actorType { get; set; } //used to determine if struct is an actor...in game, this value is 152240133
        public string actorTypeString { get; set; }
        public UsedMemoryBlock(uint startAddress, int index)
        {
            this.index = index;
            this.startAddress = startAddress;
            this.unknownValue = Memory.ReadMemory<byte>((ulong)startAddress + (ulong)unkownOffset);
            this.usedOrFree = Memory.ReadMemory<byte>((ulong)startAddress + (ulong)usedOrFreeOffset);
            this.size = Memory.ReadMemory<uint>((ulong)startAddress + (ulong)sizeOffset) + 0x10; //Include CMemBlock meta data 
            this.prevBlock = Memory.ReadMemory<uint>((ulong)startAddress + (ulong)prevBlockOffset);
            this.nextBlock = Memory.ReadMemory<uint>((ulong)startAddress + (ulong)nextBlockOffset);
            this.endAddress = this.startAddress + this.size;

            if(this.size >= MIN_ACTOR_SIZE)
            {
                this.actorType = Memory.ReadMemory<uint>((ulong)startAddress + actorTypeOffset);
                if (ActorData.Instance.ActorTypes.ContainsKey(this.actorType))
                {
                    this.actorTypeString = ActorData.Instance.ActorTypes[this.actorType];
                    this.itemID = Memory.ReadMemory<ushort>((ulong)startAddress + (ulong)itemID_Offset);
                    if (ActorData.Instance.ObjectNameTable.ContainsKey(this.itemID))
                    {
                        this.data = ActorData.Instance.ObjectNameTable[this.itemID];
                    }
                    else
                    {
                        this.data = null;
                    }
                    this.bsPcId = Memory.ReadMemory<uint>((ulong)startAddress + 0x10 + 4);
                    this.gamePtr = Memory.ReadMemory<uint>((ulong)startAddress + (ulong)gamePtrOffset);
                }

            }
            

        }
        public ListViewItem GetDisplayInfo()
        {
            ListViewItem lvi = new ListViewItem();
            lvi.SubItems[0].Text = this.index.ToString();
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, this.startAddress.ToString("X")));
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, this.endAddress.ToString("X")));
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, this.bsPcId == 0 ? "" : this.bsPcId.ToString()));
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, this.size.ToString()));

            //lvi.BackColor = color;
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, "Used"));
            //string relPointerStr = "";
            //if(relPointer != 0)
            //{
            //    relPointerStr = this.relPointer.ToString("X") + " " + $"({this.relFileName})";
            //}
            //lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, relPointerStr));
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, this.data?.ToString() ?? ""));
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, ""));
            return lvi;
        }

        //public MemoryBlockInfo GetMemoryBlock(int scale, int totalWidth)
        //{
        //    Color color = Color.FromArgb(255, 255, 200); // Adjust RGB values as needed
        //    return new MemoryBlockInfo(Math.Min((int)this.size / scale, 65535), color);
        //}
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType())
                return false;

            var other = (IMemoryBlock)obj;
            return startAddress == other.startAddress &&
                   endAddress == other.endAddress &&
                   size == other.size;
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                hash = hash * 23 + startAddress.GetHashCode();
                hash = hash * 23 + endAddress.GetHashCode();
                hash = hash * 23 + size.GetHashCode();
                return hash;
            }
        }

    }
}
