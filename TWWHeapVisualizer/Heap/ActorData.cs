﻿using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json;
using System.Reflection;
using System.Text;
using TWWHeapVisualizer.Dolphin;
using TWWHeapVisualizer.Heap.DataStructTypes.GhidraParsing;

namespace TWWHeapVisualizer.Heap
{
    public class ProcNameEntry
    {
        public string ProcName { get; set; }
        public ushort ProcValue { get; set; }
        public string StructName { get; set; }
    }
    public class ActorDatabaseEntry
    {
        [JsonProperty("Actor Name")]
        public string ActorName { get; set; }
        [JsonProperty("English Name")]
        public string EnglishName { get; set; }
        [JsonProperty("ActorClassType")]
        public string ActorClassType { get; set; }

    }
    public class ObjectName
    {
        public const int TOTAL_ENTRIES = 0x339;
        public string actorName;
        public string englishName;
        public ushort procName;
        public byte actorSubTypeIndex;
        public byte gbaType;
        public string actorClassType { get; set; }
        public ObjectName()
        {

        }
        public ObjectName(UInt64 address)
        {
            this.actorName = Encoding.UTF8.GetString(Memory.ReadMemory((ulong)address, 8))?.TrimEnd('\0');
            this.procName = Memory.ReadMemory<ushort>((ulong)address + (ulong)8);
            this.actorSubTypeIndex = Memory.ReadMemory<byte>((ulong)address + (ulong)10);
            this.gbaType = Memory.ReadMemory<byte>((ulong)address + (ulong)11);
        }
        public override string ToString()
        {
            if (actorName == null)
            {
                return "N/A";
            }
            if (englishName == null)
            {
                return actorName;
            }
            return $"{actorName} ({englishName})";
        }
    }
    public class ActorData
    {
        public static UInt64 fopActQueueHead = 0x803654CC;  //can be overwritten in version selection menu     
        public static UInt64 zeldaHeapPtr = 0x803E9E00; //can be overwritten in version selection menu
        public static UInt64 objectNameTableAddress = 0x80365CB8; //can be overwritten in version selection menu
        public static UInt64 fpcCttg_Queue = 0x80365b30; //actors to create queue...TODO
        private static readonly Lazy<ActorData> lazy =
            new Lazy<ActorData>(() => new ActorData());

        public static ActorData Instance { get { return lazy.Value; } }
        public Dictionary<ushort, ProcNameEntry> ProcStructNames { get; set; }
        public Dictionary<string, int> Sizes;
        public Dictionary<string, IMemoryAccessor> DataTypes;
        //public List<ObjectName> ObjectNameTable;
        public Dictionary<ushort,ObjectName> ObjectNameTable;
        private string _binDirectoryPath;
        private const string _RESOURCE_FOLDER_NAME = "Resources";
        public void ResetData()
        {

        }
        public int ActorSize(string name)
        {
            if (name == null)
            {
                return -1;
            }
            name = name.ToLower();
            if (Instance.Sizes.ContainsKey(name))
            {
                return Instance.Sizes[name];
            }
            else
            {
                return -1;
            }
        }
        private ActorData()
        {
            this.InitializeData();
        }
        private Dictionary<string, int> ParseActorInstanceSizes()
        {
            Dictionary<string, int> parsedData = new Dictionary<string, int>();
            string filePath = Path.Combine(_binDirectoryPath, _RESOURCE_FOLDER_NAME, "ActorInstanceSizes.txt");
            string result = File.ReadAllText(filePath);
            string[] lines = result.Split('\n');
            foreach (string line in lines)
            {
                string[] values = line.Trim().Split(':');
                if (values.Length == 2)
                {
                    string actorName = values[0].Trim().ToLower();
                    string sizeString = values[1].Trim();
                    parsedData[actorName] = int.Parse(sizeString, System.Globalization.NumberStyles.HexNumber);
                }
            }
            return parsedData;
        }
        private List<ActorDatabaseEntry> ParseActorDatabaseJson()
        {
            List<ActorDatabaseEntry> dbEntries = new List<ActorDatabaseEntry>();
            string filePath = Path.Combine(_binDirectoryPath, _RESOURCE_FOLDER_NAME, "ActorDatabase.json");
            if (File.Exists(filePath))
            {
                string jsonText = File.ReadAllText(filePath);
                dbEntries = JsonConvert.DeserializeObject<List<ActorDatabaseEntry>>(jsonText);
                return dbEntries;
            }
            else
            {
                throw new Exception("File not found: " + filePath);
            }
        }
        public void InitializeData()
        {
            _binDirectoryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            Sizes = ParseActorInstanceSizes();
            ProcStructNames = ParseProcNamesCsv();

            List<ActorDatabaseEntry> dbEntries = ParseActorDatabaseJson();

            ObjectNameTable = new Dictionary<ushort, ObjectName>();
            UInt64 currAddress = objectNameTableAddress;
            for (int i = 0; i < ObjectName.TOTAL_ENTRIES; i++)
            {
                var obj = new ObjectName(currAddress);

                var dbEntry = dbEntries.FirstOrDefault(a => a.ActorName.ToLower() == obj.actorName.ToLower());
                string englishName = dbEntry?.EnglishName;

                if (englishName != null && obj.actorName.ToLower() != englishName.ToLower())
                {
                    obj.englishName = englishName;
                    obj.actorClassType = dbEntry?.ActorClassType;
                }
                ObjectNameTable[obj.procName] = obj;
                currAddress += 0xC;
            }
            foreach(var procValue in ProcStructNames.Keys)
            {
                if (!ObjectNameTable.ContainsKey(procValue))
                {
                    ObjectNameTable[procValue] = new ObjectName
                    {
                        actorClassType = "Unknown",
                        procName = procValue,
                        actorName = ProcStructNames[procValue].ProcName
                    };
                }
            }
            DataTypes = GhidraStructParser.LoadDataStructs();
        }

        public Dictionary<ushort, ProcNameEntry> ParseProcNamesCsv()
        {
            string filePath = Path.Combine(_binDirectoryPath, _RESOURCE_FOLDER_NAME, "proc_name_structs.csv");
            List <ProcNameEntry> entries = new List<ProcNameEntry>();
            Dictionary<ushort, ProcNameEntry> structNames = new Dictionary<ushort, ProcNameEntry>();
            try
            {
                using (TextFieldParser parser = new TextFieldParser(filePath))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");

                    // Skip header line
                    parser.ReadLine();

                    while (!parser.EndOfData)
                    {
                        string[] fields = parser.ReadFields();
                        if (fields.Length >= 3)
                        {
                            ProcNameEntry entry = new ProcNameEntry
                            {
                                ProcName = fields[0],
                                ProcValue = ushort.Parse(fields[1]),
                                StructName = fields[2]
                            };
                            entries.Add(entry);
                        }
                    }
                }
                foreach (var entry in entries)
                {
                    if (structNames.ContainsKey(entry.ProcValue))
                    {
                        throw new Exception($"More than one value {entry.ProcValue} in proc names struct");
                    }
                    structNames[entry.ProcValue] = entry;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error parsing CSV file: " + ex.Message);
            }

            return structNames;
        }
    }
}