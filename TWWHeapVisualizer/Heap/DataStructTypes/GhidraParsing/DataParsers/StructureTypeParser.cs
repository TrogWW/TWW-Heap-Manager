using TWWHeapVisualizer.DataStructTypes;

public class StructureTypeParser
{

    public StructureTypeParser()
    {
    }

    public List<CsvStructureType> ParseStructureTypes(string filePath)
    {
        var structureTypes = new List<CsvStructureType>();

        using (StreamReader reader = new StreamReader(filePath))
        {
            // Skip the header line
            reader.ReadLine();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                string[] parts = line.Split(',');
                if (parts.Length == 6)
                {
                    string structureName = parts[0];
                    CsvStructureType existingStructure = structureTypes.FirstOrDefault(st => st.DataTypeName == structureName);

                    if (existingStructure == null)
                    {
                        // Create a new structure type if not found
                        existingStructure = new CsvStructureType()
                        {
                            DataTypeName = structureName
                        };
                        structureTypes.Add(existingStructure);
                    }

                    // Defer the resolution of data type and add property to the existing structure
                    string propertyName = parts[5];
                    int offset = int.Parse(parts[1]);
                    int length = int.Parse(parts[2]);
                    string dataTypeName = parts[3];
                    string dataStructType = parts[4];
                    existingStructure.AddProperty(propertyName, dataTypeName, dataStructType, offset, length);
                }
                else
                {
                    // Log invalid line
                    Console.WriteLine($"Invalid line: {line}");
                }
            }
        }
        return structureTypes;
    }
}