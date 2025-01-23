using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWWHeapVisualizer.DataStructTypes;
using TWWHeapVisualizer.Dolphin;

[Serializable()]
public class UnknownType : IMemoryAccessor
{
    // Dictionary to map type names to their sizes
    public static readonly Dictionary<string, int> TypeSizes = new Dictionary<string, int>
            {
                { "", 0 },
                { "ulonglong", 8 },
                { "bool", 1 },
                { "string", -1 },
                { "ushort", 2 },
                { "float", 4 },
                { "long", 8 },
                { "undefined", 1 },
                { "ulong", 8 },
                { "uchar", 1 },
                { "dword", 4 },
                { "longlong", 8 },
                { "wchar_t", 2 },
                { "void", 0 },
                { "byte", 1 },
                { "double", 8 },
                { "undefined4", 4 },
                { "uint", 4 },
                { "undefined1", 1 },
                { "int", 4 },
                { "undefined2", 2 },
                { "TerminatedCString", -1 }, // Placeholder for dynamic string size
                { "char", 1 },
                { "short", 2 },
                { "sbyte", 1 },
            };

    // Properties
    public string DataTypeName { get; set; }

    // Constructor
    public UnknownType(string dataTypeName)
    {
        DataTypeName = dataTypeName;
    }
    public bool DependenciesResolved(List<IDataType> types, List<IDataType> resolvedTypes)
    {
        return true;
    }
    // Size property implementation
    public int Size
    {
        get
        {
            // Lookup the size based on the data type name
            if (TypeSizes.ContainsKey(DataTypeName))
            {
                return TypeSizes[DataTypeName];
            }
            else
            {
                // Return a default size for unknown types
                Console.WriteLine($"Unknown data type: {DataTypeName}. Default size returned.");
                return -1; // or throw an exception, depending on the desired behavior
            }
        }
    }

    public List<IDataType> ListDependencies(List<IDataType> types, List<IDataType> nestedTypes)
    {
        return new List<IDataType>();
    }

    public string Read(ulong address, int length)
    {
        switch (DataTypeName)
        {
            case "bool":
                return Memory.ReadMemory<bool>(address).ToString();
            case "float":
                return Memory.ReadMemory<float>(address).ToString();
            case "ulonglong":
                return Memory.ReadMemory<ulong>(address).ToString();
            case "string":
                //return Memory.ReadSring(address + sizeof(int)); // Read string data after the size
                return "";
            case "ushort":
                return Memory.ReadMemory<ushort>(address).ToString();
            case "long":
                return Memory.ReadMemory<long>(address).ToString();
            case "undefined":
                return Memory.ReadMemory<byte>(address).ToString(); // Assuming undefined is byte-sized
            case "ulong":
                return Memory.ReadMemory<ulong>(address).ToString();
            case "uchar":
                return Memory.ReadMemory<byte>(address).ToString();
            case "dword":
                return Memory.ReadMemory<uint>(address).ToString(); // Assuming dword is uint
            case "longlong":
                return Memory.ReadMemory<long>(address).ToString(); // Assuming longlong is long
            case "wchar_t":
                return Memory.ReadMemory<char>(address).ToString(); // Assuming wchar_t is char
            case "void":
                return ""; // Assuming void does not have a value
            case "byte":
                return Memory.ReadMemory<byte>(address).ToString();
            case "double":
                return Memory.ReadMemory<double>(address).ToString();
            case "undefined4":
                return Memory.ReadMemory<int>(address).ToString(); // Assuming undefined4 is int
            case "uint":
                return Memory.ReadMemory<uint>(address).ToString();
            case "undefined1":
                return Memory.ReadMemory<byte>(address).ToString(); // Assuming undefined1 is byte
            case "int":
                return Memory.ReadMemory<int>(address).ToString();
            case "undefined2":
                return Memory.ReadMemory<short>(address).ToString(); // Assuming undefined2 is short
            case "TerminatedCString":
                //return Memory.ReadTerminatedCString(address); // Assuming TerminatedCString is a null-terminated string
                return "";
            case "char":
                return Memory.ReadMemory<char>(address).ToString();
            case "short":
                return Memory.ReadMemory<ushort>(address).ToString();
            case "sbyte":
                return Memory.ReadMemory<sbyte>(address).ToString();
            default:
                return "";
        }
    }

    public void Write(ulong address, string value, int length)
    {
        switch (DataTypeName)
        {
            case "bool":
                if (char.TryParse(value, out char parsedValue))
                {
                    Memory.WriteMemory<bool>(address, parsedValue != 0); // Write true for any non-zero char value
                }
                break;
            case "float":
                if (float.TryParse(value, out float floatValue))
                {
                    Memory.WriteMemory<float>(address, floatValue);
                }
                break;
            case "string":
                // return Memory.ReadSring(address + sizeof(int)); // Read string data after the size
                break;
            case "ushort":
                if (ushort.TryParse(value, out ushort ushortValue))
                {
                    Memory.WriteMemory<ushort>(address, ushortValue);
                }
                break;
            case "ulonglong":
            case "long":
                if (long.TryParse(value, out long longValue))
                {
                    Memory.WriteMemory<long>(address, longValue);
                }
                break; 
            case "ulong":
                if (ulong.TryParse(value, out ulong ulongValue))
                {
                    Memory.WriteMemory<ulong>(address, ulongValue);
                }
                break;
            case "undefined":
            case "uchar":
                if (byte.TryParse(value, out byte byteValue))
                {
                    Memory.WriteMemory<byte>(address, byteValue);
                }
                break;
            case "dword":
                if (uint.TryParse(value, out uint uintValue))
                {
                    Memory.WriteMemory<uint>(address, uintValue);
                }
                break;
            case "longlong":
                if (long.TryParse(value, out longValue))
                {
                    Memory.WriteMemory<long>(address, longValue);
                }
                break;
            case "wchar_t":
                if (char.TryParse(value, out parsedValue))
                {
                    Memory.WriteMemory<char>(address, parsedValue);
                }
                break;
            case "void":
                // Assuming void does not have a value
                break;
            case "byte":
                if (byte.TryParse(value, out byteValue))
                {
                    Memory.WriteMemory<byte>(address, byteValue);
                }
                break;
            case "double":
                if (double.TryParse(value, out double doubleValue))
                {
                    Memory.WriteMemory<double>(address, doubleValue);
                }
                break;
            case "undefined4":
                if (int.TryParse(value, out int intValue))
                {
                    Memory.WriteMemory<int>(address, intValue);
                }
                break;
            case "uint":
                if (uint.TryParse(value, out uintValue))
                {
                    Memory.WriteMemory<uint>(address, uintValue);
                }
                break;
            case "int":
                if (int.TryParse(value, out intValue))
                {
                    Memory.WriteMemory<int>(address, intValue);
                }
                break;
            case "char":
                if (char.TryParse(value, out parsedValue))
                {
                    Memory.WriteMemory<char>(address, parsedValue);
                }
                break;
            case "short":
                if (ushort.TryParse(value, out ushort shortValue))
                {
                    Memory.WriteMemory<ushort>(address, shortValue);
                }
                break;
            case "sbyte":
                if (sbyte.TryParse(value, out sbyte sbyteValue))
                {
                    Memory.WriteMemory<sbyte>(address, sbyteValue);
                }
                break;
            default:
                break;
        }

    }
}
