using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWWHeapVisualizer.DataStructTypes
{
    public interface IDataType
    {
        string DataTypeName { get; set; }
        int Size { get; }
        List<IDataType> ListDependencies(List<IDataType> types, List<IDataType> nestedTypes);
        bool DependenciesResolved(List<IDataType> types, List<IDataType> resolvedTypes);
    }
}
