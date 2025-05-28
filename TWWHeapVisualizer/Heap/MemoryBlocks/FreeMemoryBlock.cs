using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWWHeapVisualizer.Heap.MemoryBlocks
{
    public class FreeMemoryBlock : IMemoryBlock
    {
        public static Color color = Color.FromArgb(164, 255, 173);
        public uint startAddress { get; set; }
        public uint endAddress { get; set; }
        public uint size { get; set; }
        public FreeMemoryBlock()
        {
            
        }
        public FreeMemoryBlock(uint startAddress, uint endAddress)
        {
            this.startAddress = startAddress;
            this.endAddress = endAddress;
            this.size = this.endAddress - this.startAddress;
        }

        public ListViewItem GetDisplayInfo()
        {
            ListViewItem lvi = new ListViewItem();
            lvi.SubItems[0].Text = "";
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, this.startAddress.ToString("X")));
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, this.endAddress.ToString("X")));
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, ""));
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, this.size.ToString()));
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, "Free"));
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, ""));
            //lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, ""));
            lvi.SubItems.Add(new ListViewItem.ListViewSubItem(lvi, ""));
            //lvi.BackColor = Color.Green;
            //lvi.BackColor = color;
            return lvi;
        }
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
