using System;
using System.Collections.Generic;
using System.IO;
using TWWHeapVisualizer.DataStructTypes;

public class ArrayTypeParser
{

    public ArrayTypeParser()
    {
    }

    public List<CsvArrayType> ParseArrayTypes(string filePath)
    {
        var arrayTypes = new List<CsvArrayType>();

        using (StreamReader reader = new StreamReader(filePath))
        {
            // Skip the header line
            reader.ReadLine();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(',');
                if (parts.Length >= 4)
                {
                    string dataTypeName = parts[0];
                    string componentDataTypeName = parts[1];
                    string componentDataStructType = parts[2];
                    int numberOfElements = int.Parse(parts[3]);

                    // Create a new FunctionType instance
                    var arrayType = new CsvArrayType
                    {
                        DataTypeName = dataTypeName,
                        ComponentDataType = componentDataTypeName,
                        ComponentDataStructType = componentDataStructType,
                        NumberOfElements = numberOfElements
                    };
                    // Add the function type to the list
                    arrayTypes.Add(arrayType);
                }
                else
                {
                    // Log invalid line
                    Console.WriteLine($"Invalid line: {line}");
                }
            }
        }

        return arrayTypes;
    }

}