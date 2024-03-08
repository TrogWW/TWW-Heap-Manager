using System;
using System.Collections.Generic;
using System.IO;
using TWWHeapVisualizer.DataStructTypes;

public class FunctionDefinitionParser
{

    public FunctionDefinitionParser()
    {
    }

    public List<FunctionType> ParseFunctionDefinitions(string filePath)
    {
        var functionTypes = new List<FunctionType>();

        using (StreamReader reader = new StreamReader(filePath))
        {
            // Skip the header line
            reader.ReadLine();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(',');
                if (parts.Length >= 3)
                {
                    string functionName = parts[0];
                    string returnType = parts[1];
                    string[] parameterTypes = GetParameterTypes(line, 2);

                    // Create a new FunctionType instance
                    var functionType = new FunctionType
                    {
                        DataTypeName = functionName,
                        ReturnType = returnType
                    };

                    // Add parameter types to the function type
                    foreach (string parameterType in parameterTypes)
                    {
                        functionType.AddParameter(parameterType);
                    }

                    // Add the function type to the list
                    functionTypes.Add(functionType);
                }
                else
                {
                    // Log invalid line
                    Console.WriteLine($"Invalid line: {line}");
                }
            }
        }

        return functionTypes;
    }

    // Helper method to extract parameter types from the CSV line
    private string[] GetParameterTypes(string line, int startIndex)
    {
        // Concatenate all items after the start index into a single string
        string parametersString = string.Join(",", line.Split(',', startIndex));

        // Split the concatenated string using commas
        return parametersString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
    }
}