using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TWWHeapVisualizer.DataStructTypes;


public interface IMemoryAccessor : IDataType
{
    public string Read(ulong address, int length);
    public void Write(ulong address, string value, int length);
}

