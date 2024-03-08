using System;
using System.Collections.Generic;
using System.Linq;
using TWWHeapVisualizer.DataStructTypes;

public class DataTypeResolver
{
    private readonly Dictionary<string, IMemoryAccessor> _resolvedTypesByName;

    public Dictionary<string, IMemoryAccessor> ResolvedTypes => _resolvedTypesByName;

    public DataTypeResolver()
    {
        _resolvedTypesByName = new Dictionary<string, IMemoryAccessor>();
    }

    public void Add(IMemoryAccessor resolvedType)
    {
        if (!_resolvedTypesByName.ContainsKey(resolvedType.DataTypeName))
        {
            _resolvedTypesByName.Add(resolvedType.DataTypeName, resolvedType);
        }
        else
        {
            throw new Exception("Type already resolved.");
        }
    }

    public void ResolveData(List<IDataType> parsedTypes)
    {
        var remainingTypesToResolve = new List<IDataType>(parsedTypes);

        while (remainingTypesToResolve.Count > 0)
        {
            foreach (var type in remainingTypesToResolve.ToList())
            {
                var dependencies = type.ListDependencies(parsedTypes, new List<IDataType>());
                var hasUnresolvedTypes = dependencies.Any(d => !_resolvedTypesByName.ContainsKey(d.DataTypeName));

                if (!hasUnresolvedTypes && !_resolvedTypesByName.ContainsKey(type.DataTypeName))
                {
                    try
                    {
                        var finalResolvedType = ResolveDataType(type);
                        Add(finalResolvedType);
                        remainingTypesToResolve.Remove(type);
                    }
                    catch (Exception)
                    {
                        throw new Exception("Attempted to resolve type with unresolved dependencies.");
                    }
                }
            }
        }
    }

    public IMemoryAccessor ResolveDataType(IDataType dataType)
    {
        return dataType switch
        {
            FunctionType or EnumType or UnknownType or PointerType => (IMemoryAccessor)dataType,
            CsvStructureType csvStructType => ResolveStructureType(csvStructType),
            CsvTypeDefType typedefType => ResolveTypeDefType(typedefType),
            CsvArrayType arrayType => ResolveArrayType(arrayType),
            _ => throw new Exception($"Invalid type: {dataType.GetType().Name}")
        };
    }

    public StructureType ResolveStructureType(CsvStructureType csvStructType)
    {
        var structType = new StructureType
        {
            DataTypeName = csvStructType.DataTypeName
        };

        foreach (var csvProperty in csvStructType.Properties)
        {
            if (!_resolvedTypesByName.TryGetValue(csvProperty.DataTypeName, out var dt))
            {
                throw new Exception("Unable to locate type in resolved types.");
            }

            var p = new Property
            {
                Name = csvProperty.Name,
                DataType = dt,
                Offset = csvProperty.Offset,
                Length = csvProperty.Length
            };

            structType.Properties.Add(p);
        }

        return structType;
    }

    public TypeDefType ResolveTypeDefType(CsvTypeDefType csvTypeDefType)
    {
        if (!_resolvedTypesByName.TryGetValue(csvTypeDefType.BaseDataType, out var baseType))
        {
            throw new Exception("Unable to locate base type in resolved types.");
        }

        var typeDefType = new TypeDefType
        {
            BaseDataType = baseType,
            DataStructType = "TypeDef",
            DataTypeName = csvTypeDefType.DataTypeName
        };

        return typeDefType;
    }

    public ArrayType ResolveArrayType(CsvArrayType csvArrayType)
    {
        if (!_resolvedTypesByName.TryGetValue(csvArrayType.ComponentDataType, out var componentType))
        {
            throw new Exception("Unable to locate component type in resolved types.");
        }

        var arrayType = new ArrayType
        {
            ComponentDataType = componentType,
            DataStructType = "Array",
            DataTypeName = csvArrayType.DataTypeName,
            NumberOfElements = csvArrayType.NumberOfElements
        };

        return arrayType;
    }
}
