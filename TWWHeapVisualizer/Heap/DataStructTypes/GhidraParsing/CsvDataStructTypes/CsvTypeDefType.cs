using TWWHeapVisualizer.DataStructTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CsvTypeDefType : IDataType
{
    public string DataTypeName { get; set; }
    public string BaseDataType { get; set; }
    public string BaseDataStructType { get; set; }
    public int Size { get { return 4; } } //TODO
    private List<IDataType> _dependencies = null;

    public List<IDataType> ListDependencies(List<IDataType> types, List<IDataType> nestedTypes)
    {
        if (_dependencies != null)
        {
            return _dependencies;
        }
        List<IDataType> dependencies = new List<IDataType>();
        //if (UnknownType.TypeSizes.ContainsKey(BaseDataType))
        //{
        //    return dependencies;
        //}

        IDataType baseDataType = types.FirstOrDefault(t => t.DataTypeName == BaseDataType);

        if (baseDataType == null)
        {
            throw new Exception($"Unable to locate {BaseDataType}");
        }
        List<IDataType> nested = new List<IDataType>(nestedTypes);
        nested.Add(baseDataType);
        dependencies.Add(baseDataType);
        dependencies.AddRange(baseDataType.ListDependencies(types, nestedTypes));
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