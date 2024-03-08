//using CSVStructParserTest.DataParsers;
using System.Collections.Generic;
using System.Linq;

namespace TWWHeapVisualizer.DataStructTypes
{
    [Serializable()]
    public class StructureType : IMemoryAccessor
    {
        public string DataTypeName { get; set; }
        public int Size => Properties.Sum(p => p.DataType.Size);
        //public int Offset { get; set; }
        //public int Length { get; set; }
        public List<Property> Properties { get; set; } // Store resolved properties

        public StructureType()
        {
            Properties = new List<Property>();
        }

        public void AddProperty(string propertyName, IMemoryAccessor dataType, int offset, int length)
        {
            // Create a new Property instance with deferred resolution
            Properties.Add(new Property
            {
                Name = propertyName,
                DataType = dataType, // Store the data type name for deferred resolution
                Offset = offset,
                Length = length
            });
        }

        public List<IDataType> ListDependencies(List<IDataType> types, List<IDataType> nestedTypes)
        {
            throw new NotImplementedException();
        }

        public bool DependenciesResolved(List<IDataType> types, List<IDataType> resolvedTypes)
        {
            throw new NotImplementedException();
        }

        public string Read(ulong address, int length)
        {
            return "";
            //throw new NotImplementedException();
        }

        public void Write(ulong address, string value, int length)
        {
            return;
            //throw new NotImplementedException();
        }

        //public void ResolveProperties(DependencyTracker dependencyTracker)
        //{
        //    foreach (var propertyInfo in DeferredProperties)
        //    {
        //        // Resolve the data type for each property
        //        IDataType resolvedType = dependencyTracker.ResolveDataType(propertyInfo.DataTypeName);

        //        // Add the resolved property to the actual properties list
        //        Properties.Add(new Property
        //        {
        //            Name = propertyInfo.Name,
        //            DataType = resolvedType,
        //            Offset = propertyInfo.Offset,
        //            Length = propertyInfo.Length
        //        });
        //    }

        //    // Clear the deferred properties list after resolving
        //}
    }
}

//    public class DeferredPropertyInfo
//    {
//        public string Name { get; set; }
//        public string DataTypeName { get; set; }
//        public int Offset { get; set; }
//        public int Length { get; set; }
//    }
//}
