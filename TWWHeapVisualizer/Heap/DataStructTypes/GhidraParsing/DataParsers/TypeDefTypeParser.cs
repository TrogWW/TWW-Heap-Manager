using TWWHeapVisualizer.DataStructTypes;

public class TypeDefTypeParser
{

    public TypeDefTypeParser()
    {
    }

    public List<CsvTypeDefType> ParseTypeDefTypes(string filePath, List<IDataType> parsedTypes)
    {
        var typeDefTypes = new List<CsvTypeDefType>();

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
                    string dataTypeName = parts[0];
                    string targetDataTypeName = parts[1];
                    string targetDataStructType = parts[2];

                    // Create a new FunctionType instance
                    var typeDefType = new CsvTypeDefType
                    {
                        DataTypeName = dataTypeName,
                        BaseDataType = targetDataTypeName,
                        BaseDataStructType = targetDataStructType
                    };
                    if (!typeDefTypes.Any(t => t.DataTypeName == dataTypeName))
                    {
                        if (!parsedTypes.Any(t => t.DataTypeName == dataTypeName))
                        {
                            // Add the function type to the list
                            typeDefTypes.Add(typeDefType);
                        }
                    }
                }
                else
                {
                    // Log invalid line
                    Console.WriteLine($"Invalid line: {line}");
                }
            }
        }

        return typeDefTypes;
    }

}
