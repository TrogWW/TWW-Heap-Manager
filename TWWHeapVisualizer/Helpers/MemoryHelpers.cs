using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TWWHeapVisualizer.Helpers
{
    public static class MemoryHelpers
    {
        public static bool isValidAddress(uint address)
        {
            return address > 2147483648 && address < 2172649471;
        }
    }
}
