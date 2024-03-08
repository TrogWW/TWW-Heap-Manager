using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWWHeapVisualizer.DataStructTypes
{
    [Serializable()]
    public class Property
    {
        public string Name { get; set; } // Name of the property
        public IMemoryAccessor DataType { get; set; } // Data type of the property
        public int Offset { get; set; } // Offset in memory within the struct
        public int Length { get; set; } // Number of bytes the property occupies in memory
                                        // Other properties specific to the property
    }
}
