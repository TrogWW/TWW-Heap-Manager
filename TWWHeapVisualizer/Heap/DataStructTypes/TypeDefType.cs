using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWWHeapVisualizer.DataStructTypes
{
    [Serializable()]
    public class TypeDefType : IMemoryAccessor
    {
        public string DataStructType { get; set; }
        public IMemoryAccessor BaseDataType { get; set; }
        public string DataTypeName { get; set; }

        public int Size => BaseDataType.Size;

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
            
            return BaseDataType.Read(address, length);
            //throw new NotImplementedException();
        }

        public void Write(ulong address, string value, int length)
        {
            BaseDataType.Write(address, value, length);
            //return;
        }
    }
}
