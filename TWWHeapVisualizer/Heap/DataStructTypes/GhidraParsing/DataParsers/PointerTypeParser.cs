using TWWHeapVisualizer.DataStructTypes;

public class PointerTypeParser
{

    public PointerTypeParser()
    {
    }

    public List<PointerType> ParsePointerTypes(string filePath)
    {
        var pointerTypes = new List<PointerType>();

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
                    var pointerType = new PointerType
                    {
                        DataTypeName = dataTypeName,
                        TargetDataTypeName = targetDataTypeName,
                        TargetDataStructType = targetDataStructType
                    };
                    if (!pointerTypes.Any(t => t.DataTypeName == dataTypeName))
                    {
                        // Add the function type to the list
                        pointerTypes.Add(pointerType);
                    }
                    else
                    {
                        var test = 5;
                    }

                }
                else
                {
                    // Log invalid line
                    Console.WriteLine($"Invalid line: {line}");
                }
            }
        }

        return pointerTypes;
    }

}