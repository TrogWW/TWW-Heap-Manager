using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWWHeapVisualizer.Dolphin;
using TWWHeapVisualizer.Helpers;

namespace TWWHeapVisualizer.Heap
{
    public class DynamicNameTable
    {
        
        public static UInt64 StartAddress = 0x803398D8;
        public Dictionary<ushort, string> Entries;
        private const int EntryCount = 430;
        private const int mPName = 0x0;
        private const int mRelFileName = 0x4;
        private const int Size = 8;
        public DynamicNameTable()
        {
            this.Entries = new Dictionary<ushort, string>();
            Read();
        }
        private void Read()
        {
            
            for (int i = 0; i < EntryCount; i++)
            {
                UInt64 currAddress = StartAddress + (ulong)(i * Size);
                ushort name = Memory.ReadMemory<ushort>(currAddress);
                if(name == ushort.MaxValue)
                {
                    break; //65535 signifies end of list
                }
                uint relPtr = Memory.ReadMemory<uint>(currAddress + (ulong)mRelFileName);
                string relFileName = null;
                if (MemoryHelpers.isValidAddress(relPtr))
                {
                    relFileName = Memory.ReadMemoryString(relPtr);
                }
                if (Entries.ContainsKey(name))
                {
                    throw new Exception($"DNT contains duplicate entry name: {name}");
                }
                Entries.Add(name, relFileName);
                
            }
        }
    }
}
