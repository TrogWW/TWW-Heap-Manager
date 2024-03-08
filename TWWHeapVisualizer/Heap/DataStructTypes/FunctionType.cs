using System.Collections.Generic;
using TWWHeapVisualizer.DataStructTypes;


[Serializable()]
public class FunctionType : IMemoryAccessor
{
    public string DataTypeName { get; set; } // Name of the function
    public string ReturnType { get; set; } // Return type of the function
    public List<string> Parameters { get; } // List of parameter types

    public FunctionType()
    {
        Parameters = new List<string>();
    }

    public void AddParameter(string parameterType)
    {
        Parameters.Add(parameterType);
    }

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
        return "";
        //throw new NotImplementedException();
    }

    public void Write(ulong address, string value, int length)
    {
        return;
    }

    // Calculate the size of the function type
    public int Size
    {
        get
        {
            // Size of a function type can be defined based on the return type and parameter types
            // Here, we return a default size of 1, you can adjust this based on your requirements
            return 4;
        }
    }
}
