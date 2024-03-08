using TWWHeapVisualizer.DataStructTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class CsvStructureType : IDataType
{
    public string DataTypeName { get; set; }
    public int Size => 0;//Properties.Sum(p => p.DataType.Size);

    public List<CsvProperty> Properties { get; set; } // Store resolved properties
    public CsvStructureType()
    {
        Properties = new List<CsvProperty>();
    }

    public void AddProperty(string propertyName, string dataTypeName, string dataStructType, int offset, int length)
    {
        // Create a new Property instance with deferred resolution
        Properties.Add(new CsvProperty
        {
            Name = propertyName,
            DataTypeName = dataTypeName,
            DataStructType = dataStructType,
            Offset = offset,
            Length = length
        });
    }
    private List<IDataType> _dependencies = null;
    public List<IDataType> ListDependencies(List<IDataType> types, List<IDataType> nestedTypes)
    {
        if (_dependencies != null)
        {
            return _dependencies;
        }
        List<IDataType> dependencies = new List<IDataType>();
        List<string> distinctPropertyNames = Properties.Select(p => p.DataTypeName).Distinct().ToList();
        foreach (string propertyName in distinctPropertyNames)
        {
            //if (UnknownType.TypeSizes.ContainsKey(propertyName))
            //{
            //    continue;
            //}
            if (nestedTypes.Any(t => t.DataTypeName == propertyName))
            {
                List<string> circularDependencies = nestedTypes.Select(t => t.DataTypeName).ToList();
                circularDependencies.Add(propertyName);
                //Console.WriteLine($"Avoiding circular dependency {string.Join(",", circularDependencies)}");
                throw new Exception($"Cannot parse property {propertyName}. Circular dependency found: {string.Join(",", circularDependencies)}");
                //continue; //avoid circular dependency
            }
            IDataType propertyDataType = types.FirstOrDefault(t => t.DataTypeName == propertyName);

            if (propertyDataType == null)
            {
                throw new Exception($"Unable to locate {propertyName}");
            }
            List<IDataType> nested = new List<IDataType>(nestedTypes);
            nested.Add(propertyDataType);
            dependencies.Add(propertyDataType);
            dependencies.AddRange(propertyDataType.ListDependencies(types, nested));
        }
        List<string> distinctDependencies = dependencies.Select(d => d.DataTypeName).Distinct().ToList();
        dependencies = distinctDependencies.Select(dd => types.First(t => t.DataTypeName == dd)).ToList();
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
