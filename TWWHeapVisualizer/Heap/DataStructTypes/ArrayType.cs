using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWWHeapVisualizer.DataStructTypes
{
    [Serializable()]
    public class ArrayType : IMemoryAccessor
    {
        public string DataStructType { get; set; }
        public int NumberOfElements { get; set; }
        public IMemoryAccessor ComponentDataType { get; set; }
        public string DataTypeName { get; set; }

        public int Size => ComponentDataType.Size * NumberOfElements;

        public bool DependenciesResolved(List<IDataType> types, List<IDataType> resolvedTypes)
        {
            throw new NotImplementedException();
        }

        public List<IDataType> ListDependencies(List<IDataType> types, List<IDataType> nestedTypes)
        {
            throw new NotImplementedException();
        }

        public string Read(ulong address, int length)
        {
            return "";
        }

        public void Write(ulong address, string value, int length)
        {
            return;
        }
    }
}
