using TWWHeapVisualizer.DataStructTypes;
using System;
using System.Collections.Generic;
using TWWHeapVisualizer.Dolphin;

[Serializable()]
public class EnumType : IMemoryAccessor
{
    public string DataTypeName { get; set; } // Enum name
    public Dictionary<string, long> EnumValues { get; set; } // Enum values (name-value pairs)

    // Constructor
    public EnumType(string enumName)
    {
        DataTypeName = enumName;
        EnumValues = new Dictionary<string, long>();
    }

    // Add an enum value
    public void AddEnumValue(string entryName, long number)
    {
        EnumValues.Add(entryName, number);
    }
    // Method to get names associated with a specific value
    public List<string> GetNamesForValue(long value)
    {
        List<string> names = new List<string>();
        foreach (var pair in EnumValues)
        {
            if (pair.Value == value)
            {
                names.Add(pair.Key);
            }
        }
        return names;
    }

    public List<IDataType> ListDependencies(List<IDataType> types, List<IDataType> nestedTypes)
    {
        return new List<IDataType>();
    }
    public bool DependenciesResolved(List<IDataType> types, List<IDataType> reslovedTypes)
    {
        return true;
    }

    public string Read(ulong address, int length)
    {
        long value = 0;
        switch (length)
        {
            case 1:
                value = Memory.ReadMemory<byte>(address);
                break;
            case 2:
                value = Memory.ReadMemory<ushort>(address);
                break;
            case 4:
                value = Memory.ReadMemory<int>(address);
                break;
            case 8:
                value = Memory.ReadMemory<long>(address);
                break;
            default:
                break;
        }
        if (!EnumValues.Values.Contains(value))
        {
            return value.ToString();
        }
        else
        {
            var enumValue = EnumValues.First(kvp => kvp.Value == value);
            return enumValue.Key;
        }
    }

    public void Write(ulong address, string key, int length)
    {
        if (!EnumValues.ContainsKey(key))
        {
            return;
        }
        long value = EnumValues[key];
        switch (length)
        {
            case 1:
                Memory.WriteMemory<byte>(address, (byte)value);
                break;
            case 2:
                Memory.WriteMemory<short>(address, (short)value);
                break;
            case 4:
                Memory.WriteMemory<int>(address, (int)value);
                break;
            case 8:
                Memory.WriteMemory<long>(address, value);
                break;
            default:
                break;
        }
        //throw new NotImplementedException();
    }

    // Calculate the size of the enum based on the range of values
    public int Size
    {
        get
        {
            // Determine the range of values
            long minValue = long.MaxValue;
            long maxValue = long.MinValue;

            foreach (long value in EnumValues.Values)
            {
                minValue = Math.Min(minValue, value);
                maxValue = Math.Max(maxValue, value);
            }

            // Determine the smallest possible primitive type that accommodates the range of values
            if (minValue >= sbyte.MinValue && maxValue <= sbyte.MaxValue)
            {
                return sizeof(sbyte);
            }
            else if (minValue >= short.MinValue && maxValue <= short.MaxValue)
            {
                return sizeof(short);
            }
            else if (minValue >= int.MinValue && maxValue <= int.MaxValue)
            {
                return sizeof(int);
            }
            else
            {
                return sizeof(long);
            }
        }
    }

}
