using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWWHeapVisualizer.Dolphin;

namespace TWWHeapVisualizer.DataStructTypes
{
    [Serializable()]
    public class PointerType : IMemoryAccessor
    {
        public string DataTypeName { get; set; }
        public string TargetDataTypeName { get; set; }
        public string TargetDataStructType { get; set; }
        public int Size { get { return 4; } }

        public List<IDataType> ListDependencies(List<IDataType> types, List<IDataType> nestedTypes)
        {
            return new List<IDataType>();
        }
        public bool DependenciesResolved(List<IDataType> types, List<IDataType> resolvedTypes)
        {
            return true;
        }

        public string Read(ulong address, int length)
        {
            int pointer = Memory.ReadMemory<int>(address);
            return pointer.ToString("X");
            //throw new NotImplementedException();
        }

        public void Write(ulong address, string value, int length)
        {
            return;
        }
    }
}
