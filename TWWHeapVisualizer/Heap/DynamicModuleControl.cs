using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWWHeapVisualizer.DataStructTypes;
using TWWHeapVisualizer.Dolphin;
using TWWHeapVisualizer.Helpers;

namespace TWWHeapVisualizer.Heap
{
    public class DynamicModuleControlEntry
    {
        public uint relPointer { get; set; }
        public string relFileName { get; set; }
    }
    public class DynamicModuleControl
    {
        public static UInt64 StartAddress = 0x803B9218;
        public Dictionary<ushort, DynamicModuleControlEntry> Entries;
        //private const int ActorCount = 502; //read from 80022852

        public DynamicModuleControl()
        {
            Entries = new Dictionary<ushort, DynamicModuleControlEntry>();
            Read();
        }
        private void Read()
        {
            foreach (ushort key in ActorData.Instance.DynamicNameTable.Entries.Keys)
            {
                ulong currAddress = StartAddress + (ulong)(key * 4);
                uint dmcPtr = Memory.ReadMemory<uint>(currAddress);
                if (MemoryHelpers.isValidAddress(dmcPtr))
                {
                    uint relPtr = Memory.ReadMemory<uint>((ulong)(dmcPtr + 0x10));

                    if (relPtr == 0)
                    {
                        continue; //rel is not currently loaded
                    }
                    Entries[key] = new DynamicModuleControlEntry 
                    { 
                        relPointer = relPtr,
                        relFileName = ActorData.Instance.DynamicNameTable.Entries[key]
                    };
                }
            }
        }
    }
}
