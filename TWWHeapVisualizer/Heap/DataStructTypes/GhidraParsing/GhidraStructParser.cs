using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using TWWHeapVisualizer.DataStructTypes;

namespace TWWHeapVisualizer.Heap.DataStructTypes.GhidraParsing
{
    public static class GhidraStructParser
    {
        //private static readonly string directoryPath = @"C:\Output\";
        private static readonly string bin_file_name = @"ghidra_datatypes.dat";
        public static Dictionary<string, IMemoryAccessor> LoadDataStructs()
        {
            string directoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string binFileFullPath = Path.Combine(directoryPath, "Resources", bin_file_name);
            if (File.Exists(binFileFullPath))
            {
                return (Dictionary<string, IMemoryAccessor>)Deserialize(binFileFullPath);
            }
            else
            {
                throw new Exception($"Unable to locate data types at: {directoryPath}");
            }
        }
        public static Dictionary<string, IMemoryAccessor> ParseDataTypes(string directoryPath)
        {
            // Define the directory path for CSV files

            // Define the file paths for CSV files
            string enumCsvPath = Path.Combine(directoryPath, "enum_types.csv");
            string pointerCsvPath = Path.Combine(directoryPath, "pointer_types.csv");
            string structureCsvPath = Path.Combine(directoryPath, "structure_types.csv");
            string functionDefinitionsCsvPath = Path.Combine(directoryPath, "function_types.csv");
            string typeDefCsvPath = Path.Combine(directoryPath, "typedef_types.csv");
            string arrayCsvPath = Path.Combine(directoryPath, "array_types.csv");
            // Parse CSV files
            List<EnumType> enumTypes = new EnumTypeParser().ParseEnumTypes(enumCsvPath);
            List<FunctionType> functionTypes = new FunctionDefinitionParser().ParseFunctionDefinitions(functionDefinitionsCsvPath);
            List<CsvStructureType> structureTypes = new StructureTypeParser().ParseStructureTypes(structureCsvPath);
            List<PointerType> pointerTypes = new PointerTypeParser().ParsePointerTypes(pointerCsvPath);
            List<CsvArrayType> arrayTypes = new ArrayTypeParser().ParseArrayTypes(arrayCsvPath);
            List<IDataType> parsedTypes = new List<IDataType>();
            parsedTypes.AddRange(enumTypes);
            parsedTypes.AddRange(functionTypes);
            parsedTypes.AddRange(structureTypes);
            parsedTypes.AddRange(pointerTypes);

            parsedTypes.AddRange(arrayTypes);
            List<CsvTypeDefType> typeDefTypes = new TypeDefTypeParser().ParseTypeDefTypes(typeDefCsvPath, parsedTypes);
            parsedTypes.AddRange(typeDefTypes);

            foreach (string typeName in UnknownType.TypeSizes.Keys)
            {
                parsedTypes.Add(new UnknownType(typeName));
            }

            DataTypeResolver resolver = new DataTypeResolver();
            resolver.ResolveData(parsedTypes);
            var finalResolvedTypes = resolver.ResolvedTypes;

            string binDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Serialize(Path.Combine(binDirectoryPath, "Resources", bin_file_name), finalResolvedTypes);
            return finalResolvedTypes;
        }
        public static object Deserialize(Stream stream)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            return formatter.Deserialize(stream);
        }

        public static object Deserialize(string fullFilePath)
        {
            FileStream stream = null;
            try
            {
                stream = new FileStream(fullFilePath, FileMode.Open, FileAccess.Read);
                return Deserialize(stream);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                }

            }
        }
        public static void Serialize(Stream stream, object value)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, value);
        }

        public static void Serialize(string fullFilePath, object value)
        {
            FileStream stream = null;
            try
            {
                stream = File.Create(fullFilePath);
                Serialize(stream, value);
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                    stream.Dispose();
                }
            }
        }
    }
}