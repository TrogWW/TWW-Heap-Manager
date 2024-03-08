using TWWHeapVisualizer.DataStructTypes;
using System;
using System.Collections.Generic;
using System.IO;

public class EnumTypeParser
{
    public EnumTypeParser()
    {
    }

    public List<EnumType> ParseEnumTypes(string filePath)
    {
        List<EnumType> enumTypes = new List<EnumType>();

        using (StreamReader reader = new StreamReader(filePath))
        {
            // Skip the header line
            reader.ReadLine();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(',');
                if (parts.Length == 3)
                {
                    string enumName = parts[0];
                    string entryName = parts[2];
                    long value = long.Parse(parts[1]);

                    Console.WriteLine($"Parsing enum: {enumName}");

                    EnumType enumType = enumTypes.FirstOrDefault(t => t.DataTypeName == enumName);
                    // Check if the enum type with the same name already exists

                    if (enumType == null)
                    {
                        Console.WriteLine($"Resolving enum: {enumName}");
                        // If not, create a new EnumType and register it
                        enumType = new EnumType(enumName);
                        enumTypes.Add(enumType);
                    }

                    // Add enum value to the existing or newly created enum type
                    enumType.AddEnumValue(entryName, value);
                }
                else
                {
                    Console.WriteLine($"Invalid line: {line}");
                }
            }
        }

        return enumTypes;
    }




}