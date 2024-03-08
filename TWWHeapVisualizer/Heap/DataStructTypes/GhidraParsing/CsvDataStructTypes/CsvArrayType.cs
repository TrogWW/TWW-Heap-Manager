using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWWHeapVisualizer.DataStructTypes;

public class CsvArrayType : IDataType
{
    public string DataTypeName { get; set; }
    public string ComponentDataType { get; set; }
    public string ComponentDataStructType { get; set; }
    public int NumberOfElements { get; set; }
    public int Size { get { return 4; } }
    private List<IDataType> _dependencies = null;
    public List<IDataType> ListDependencies(List<IDataType> types, List<IDataType> nestedTypes)
    {
        if (_dependencies != null)
        {
            return _dependencies;
        }
        List<IDataType> dependencies = new List<IDataType>();
        //if (UnknownType.TypeSizes.ContainsKey(ComponentDataType))
        //{
        //    return dependencies;
        //}
        IDataType baseDataType = types.FirstOrDefault(t => t.DataTypeName == ComponentDataType);

        if (baseDataType == null)
        {
            throw new Exception($"Unable to locate {ComponentDataType}");
        }
        List<IDataType> nested = new List<IDataType>(nestedTypes);
        nested.Add(baseDataType);
        dependencies.Add(baseDataType);
        dependencies.AddRange(baseDataType.ListDependencies(types, nested));
        _dependencies = dependencies;
        return dependencies;
    }
    public bool DependenciesResolved(List<IDataType> types, List<IDataType> resolvedTypes)
    {
        List<IDataType> dependencies = ListDependencies(types, new List<IDataType>());
        List<string> dependencyNames = dependencies.Select(d => d.DataTypeName).ToList();
        List<string> resolvedNames = resolvedTypes.Select(d => d.DataTypeName).ToList();
        foreach (string dependencyName in dependencyNames)
        {
            if (!resolvedNames.Contains(dependencyName))
            {
                return false;
            }
        }
        return true;
    }
}
