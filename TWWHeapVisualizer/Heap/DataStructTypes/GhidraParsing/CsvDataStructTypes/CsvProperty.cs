using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CsvProperty
{
    public string Name { get; set; } // Name of the property
    public string DataTypeName { get; set; } // Data type of the property
    public string DataStructType { get; set; }
    public int Offset { get; set; } // Offset in memory within the struct
    public int Length { get; set; } // Number of bytes the property occupies in memory

}