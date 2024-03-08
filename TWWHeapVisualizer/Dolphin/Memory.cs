using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;


//This code is mostly just borrowed from DME translated into c#
//Windows only
//Theres probably some unecessary code in this and I had to make changes 
//https://github.com/aldelaro5/dolphin-memory-engine/blob/master/Source/DolphinProcess/Windows/WindowsDolphinProcess.cpp

namespace TWWHeapVisualizer.Dolphin
{
    internal class Memory
    {

        //public static Process m_iProcess;
        //private static IntPtr m_iProcessHandle;
        public static IntPtr m_hDolphin = IntPtr.Zero;

        // Dolphin maps 32 mb for the fakeVMem which is what ends up being the speedhack, but in reality
        // the ARAM is actually 16 mb. We need the fake size to do process address calculation
        const ulong MEM1_SIZE = 0x01800000U;
        const ulong MEM1_START = 0x80000000;
        const ulong MEM1_END = 0x81800000;

        const ulong ARAM_SIZE = 0x1000000;
        // Dolphin maps 32 mb for the fakeVMem which is what ends up being the speedhack, but in reality
        // the ARAM is actually 16 mb. We need the fake size to do process address calculation
        const ulong ARAM_FAKESIZE = 0x2000000;
        const ulong ARAM_START = 0x7E000000;
        const ulong ARAM_END = 0x7F000000;

        const ulong MEM2_SIZE = 0x04000000U;
        const ulong MEM2_START = 0x90000000;
        const ulong MEM2_END = 0x94000000;

        private static bool m_ARAMAccessible = false;
        private static int m_iBytesWritten;
        private static int m_iBytesRead;
        private static IntPtr m_iBtyesReadPtr;
        private static ulong m_emuRAMAddressStart;
        private static ulong m_emuARAMAdressStart;

        private static ulong m_MEM2AddressStart;

        public static bool Attach(Process m_iProcess)
        {
            try
            {
                m_hDolphin = Imports.OpenProcess(Flags.PROCESS_QUERY_INFORMATION | Flags.PROCESS_VM_OPERATION | Flags.PROCESS_VM_READ | Flags.PROCESS_VM_WRITE, false, m_iProcess.Id);
                MEMORY_BASIC_INFORMATION info;
                bool MEM1found = false;
                bool m_MEM2Present = false;
                uint MEMORY_BASIC_INFORMATION_SIZE = (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION));
                //ulong address = 0; ;
                uint PSAPI_WORKING_SET_EX_INFORMATION_SIZE = (uint)Marshal.SizeOf(typeof(_PSAPI_WORKING_SET_EX_INFORMATION));
                IntPtr p = IntPtr.Zero; // Start address
                                        //var virtualQueryResult = Imports.VirtualQueryEx((IntPtr)m_hDolphin, (IntPtr)address, out info, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
                                        //for (
                                        //    IntPtr p = IntPtr.Zero; 
                                        //    Imports.VirtualQueryEx((IntPtr)m_hDolphin, p, out info, MEMORY_BASIC_INFORMATION_SIZE) == MEMORY_BASIC_INFORMATION_SIZE; 
                                        //    p = IntPtr.Add(p, (int)info.RegionSize))

                while (Imports.VirtualQueryEx(m_hDolphin, p, out info, MEMORY_BASIC_INFORMATION_SIZE) == MEMORY_BASIC_INFORMATION_SIZE)
                {
                    if (!m_MEM2Present && (ulong)info.RegionSize == MEM2_SIZE)
                    {
                        long regionBaseAddress = (long)info.BaseAddress;
                        if (MEM1found && regionBaseAddress > (long)m_emuARAMAdressStart + 0x10000000)
                        {
                            // In some cases MEM2 could actually be before MEM1. Once we find MEM1, ignore regions of
                            // this size that are too far away. There apparently are other non-MEM2 regions of 64 MiB
                            break;
                        }
                        _PSAPI_WORKING_SET_EX_INFORMATION[] wsInfo = { new _PSAPI_WORKING_SET_EX_INFORMATION() };
                        wsInfo[0].VirtualAddress = info.BaseAddress;

                        if (Imports.QueryWorkingSetEx(m_hDolphin, wsInfo, (int)PSAPI_WORKING_SET_EX_INFORMATION_SIZE))
                        {
                            if (wsInfo[0].VirtualAttributes.Valid)
                            {
                                m_emuRAMAddressStart = (ulong)info.BaseAddress.ToInt64();
                                m_MEM2Present = true;
                            }
                        }
                    }
                    else if (info.RegionSize == (IntPtr)0x2000000 && info.Type == TypeEnum.MEM_MAPPED)
                    {
                        _PSAPI_WORKING_SET_EX_INFORMATION[] wsInfo = { new _PSAPI_WORKING_SET_EX_INFORMATION() };
                        wsInfo[0].VirtualAddress = info.BaseAddress;
                        if (Imports.QueryWorkingSetEx(m_hDolphin, wsInfo, (int)PSAPI_WORKING_SET_EX_INFORMATION_SIZE))
                        {
                            if (wsInfo[0].VirtualAttributes.Valid)
                            {
                                if (!MEM1found)
                                {
                                    m_emuRAMAddressStart = (ulong)info.BaseAddress;
                                    MEM1found = true;
                                }
                                else
                                {
                                    ulong aramCandidate = (ulong)info.BaseAddress;
                                    if (aramCandidate == m_emuRAMAddressStart + 0x2000000)
                                    {
                                        m_emuARAMAdressStart = aramCandidate;
                                        m_ARAMAccessible = true;
                                    }
                                }
                            }
                        }
                    }
                    //if (regionSizes.ContainsKey(info.RegionSize))
                    //{
                    //    regionSizes[info.RegionSize] += 1;
                    //}
                    //else
                    //{
                    //    regionSizes[info.RegionSize] = 1;
                    //}

                    p = (IntPtr)((long)p + (long)info.RegionSize);
                }

                if (m_MEM2Present)
                {
                    m_emuARAMAdressStart = 0;
                    m_ARAMAccessible = false;
                }
                if (m_emuRAMAddressStart == 0)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public static int rotateLeft(int input, int shift)
        {
            return (input << shift) | ((input >> (32 - shift)) & ~(-1 << shift));
        }
        public static int rlwinm(int input, int shift, int mask)
        {
            return rotateLeft(input, shift) & mask;
        }
        //uses jmath lookup table for cosine
        public static void WriteMemory<T>(ulong address, object Value)
        {
            ulong RAMAddress = 0;
            if (m_ARAMAccessible)
            {
                if (address >= ARAM_FAKESIZE)
                {
                    RAMAddress = m_emuRAMAddressStart + address - ARAM_FAKESIZE;
                }
                else
                {
                    RAMAddress = m_emuARAMAdressStart + address;
                }
            }
            else
            {
                RAMAddress = m_emuRAMAddressStart + address;
            }
            RAMAddress -= MEM1_START;
            var buffer = StructureToByteArray<T>(Value);

            Imports.NtWriteVirtualMemory((int)m_hDolphin, RAMAddress, buffer, buffer.Length, out m_iBytesWritten);
        }

        public static void WriteMemory<T>(ulong address, char[] Value)
        {
            ulong RAMAddress = 0;
            if (m_ARAMAccessible)
            {
                if (address >= ARAM_FAKESIZE)
                {
                    RAMAddress = m_emuRAMAddressStart + address - ARAM_FAKESIZE;
                }
                else
                {
                    RAMAddress = m_emuARAMAdressStart + address;
                }
            }
            else
            {
                RAMAddress = m_emuRAMAddressStart + address;
            }
            RAMAddress -= MEM1_START;
            var buffer = Encoding.UTF8.GetBytes(Value);

            Imports.NtWriteVirtualMemory((int)m_hDolphin, RAMAddress, buffer, buffer.Length, out m_iBytesWritten);
        }
        public static void WriteMemory(ulong address, byte[] Value)
        {
            ulong RAMAddress = 0;
            if (m_ARAMAccessible)
            {
                if (address >= ARAM_FAKESIZE)
                {
                    RAMAddress = m_emuRAMAddressStart + address - ARAM_FAKESIZE;
                }
                else
                {
                    RAMAddress = m_emuARAMAdressStart + address;
                }
            }
            else
            {
                RAMAddress = m_emuRAMAddressStart + address;
            }
            RAMAddress -= MEM1_START;
            //var buffer = Encoding.UTF8.GetBytes(Value);

            Imports.NtWriteVirtualMemory((int)m_hDolphin, RAMAddress, Value, Value.Length, out m_iBytesWritten);
        }
        public static T ReadMemory<T>(ulong address) where T : struct
        {
            ulong RAMAddress = 0;
            if (m_ARAMAccessible)
            {
                if (address >= ARAM_FAKESIZE)
                {
                    RAMAddress = m_emuRAMAddressStart + address - ARAM_FAKESIZE;
                    //RAMAddress = m_emuARAMAdressStart + address;

                }
                else
                {
                    RAMAddress = m_emuARAMAdressStart + address;
                }
            }
            else
            {
                RAMAddress = m_emuRAMAddressStart + address;
            }
            RAMAddress -= MEM1_START;
            var ByteSize = Marshal.SizeOf(typeof(T));

            var buffer = new byte[ByteSize];
            // virtualQueryResult = Imports.VirtualQueryEx((IntPtr)m_iProcessHandle, (IntPtr)address, out m, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
            Imports.ReadProcessMemory((IntPtr)m_hDolphin, (IntPtr)RAMAddress, buffer, buffer.Length, out m_iBtyesReadPtr);
            // Imports.NtReadVirtualMemory((int)m_iProcessHandle, RAMAddress, buffer, buffer.Length, out m_iBytesRead);
            //m_ARAMAccessible = accessible;
            return ByteArrayToStructure<T>(buffer);
        }
        public static string ReadMemoryString(ulong address)
        {
            List<byte> bytes = new List<byte>();
            int offset = 0;
            while (true)
            {
                var b = ReadMemory<byte>(address + (ulong)offset);
                if(b == 0)
                {
                    break;
                }
                bytes.Add(b);
                offset += 1;
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }
        public static byte[] ReadMemory(ulong address, int size)
        {
            ulong RAMAddress = 0;
            if (m_ARAMAccessible)
            {
                if (address >= ARAM_FAKESIZE)
                {
                    RAMAddress = m_emuRAMAddressStart + address - ARAM_FAKESIZE;
                }
                else
                {
                    RAMAddress = m_emuARAMAdressStart + address;
                }
            }
            else
            {
                RAMAddress = m_emuRAMAddressStart + address;
            }
            RAMAddress -= MEM1_START;
            var buffer = new byte[size];

            Imports.NtReadVirtualMemory((int)m_hDolphin, RAMAddress, buffer, size, out m_iBytesRead);

            return buffer;
        }

        public static float[] ReadMatrix<T>(ulong address, int MatrixSize) where T : struct
        {
            ulong RAMAddress = 0;
            if (m_ARAMAccessible)
            {
                if (address >= ARAM_FAKESIZE)
                {
                    RAMAddress = m_emuRAMAddressStart + address - ARAM_FAKESIZE;
                }
                else
                {
                    RAMAddress = m_emuARAMAdressStart + address;
                }
            }
            else
            {
                RAMAddress = m_emuRAMAddressStart + address;
            }
            RAMAddress -= MEM1_START;
            var ByteSize = Marshal.SizeOf(typeof(T));
            var buffer = new byte[ByteSize * MatrixSize];
            Imports.NtReadVirtualMemory((int)m_hDolphin, RAMAddress, buffer, buffer.Length, out m_iBytesRead);

            return ConvertToFloatArray(buffer);
        }

        //public static int GetModuleAddress(string Name)
        //{
        //    try
        //    {
        //        foreach (ProcessModule ProcMod in m_iProcess.Modules)
        //            if (Name == ProcMod.ModuleName)
        //                return (int)ProcMod.BaseAddress;
        //    }
        //    catch (Exception ex)
        //    {

        //    }

        //    Console.ForegroundColor = ConsoleColor.Red;
        //    Console.WriteLine("ERROR: Cannot find - " + Name + " | Check file extension.");
        //    Console.ResetColor();

        //    return -1;
        //}

        #region Other

        /// <summary>
        /// Flags found here
        /// https://www.pinvoke.net/default.aspx/kernel32.openprocess
        /// </summary>
        internal struct Flags
        {
            public const int PROCESS_VM_OPERATION = 0x0008;
            public const int PROCESS_VM_READ = 0x0010;
            public const int PROCESS_VM_WRITE = 0x0020;
            public const int PROCESS_QUERY_INFORMATION = 0x00000400;
        }

        #endregion

        #region Conversion

        public static float[] ConvertToFloatArray(byte[] bytes)
        {
            if (bytes.Length % 4 != 0)
                throw new ArgumentException();

            var floats = new float[bytes.Length / 4];

            for (var i = 0; i < floats.Length; i++)
                floats[i] = BitConverter.ToSingle(bytes, i * 4);

            return floats;
        }

        private static T ByteArrayToStructure<T>(byte[] bytes) where T : struct
        {
            if (typeof(T) == typeof(float))
            {
                bytes = ReadSingleBigEndian(bytes, 0);
            }
            else if (typeof(T) == typeof(int) || typeof(T) == typeof(uint) || typeof(T) == typeof(ushort))
            {
                Array.Reverse(bytes);
            }
            var handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        private static byte[] StructureToByteArray(object obj)
        {
            var length = Marshal.SizeOf(obj);

            var array = new byte[length];

            var pointer = Marshal.AllocHGlobal(length);

            Marshal.StructureToPtr(obj, pointer, true);
            Marshal.Copy(pointer, array, 0, length);
            Marshal.FreeHGlobal(pointer);

            if (obj is float)
            {
                array = ReadSingleBigEndian(array, 0);
            }
            return array;
        }
        private static byte[] StructureToByteArray<T>(object obj)
        {
            var length = Marshal.SizeOf(obj);

            var array = new byte[length];

            var pointer = Marshal.AllocHGlobal(length);

            Marshal.StructureToPtr(obj, pointer, true);
            Marshal.Copy(pointer, array, 0, length);
            Marshal.FreeHGlobal(pointer);
            if (typeof(T) == typeof(float))
            {
                array = ReadSingleBigEndian(array, 0);
            }
            else if (typeof(T) == typeof(int) || typeof(T) == typeof(uint) || typeof(T) == typeof(ushort))
            {
                Array.Reverse(array);
            }
            //if (obj is float)
            //{
            //    array = ReadSingleBigEndian(array, 0);
            //}
            return array;
        }
        #endregion

        public static byte[] ReadSingleBigEndian(byte[] data, int offset)
        {
            return ReadSingle(data, offset, false);
        }
        public static byte[] ReadSingleLittleEndian(byte[] data, int offset)
        {
            return ReadSingle(data, offset, true);
        }
        private static byte[] ReadSingle(byte[] data, int offset, bool littleEndian)
        {
            if (BitConverter.IsLittleEndian != littleEndian)
            {   // other-endian; reverse this portion of the data (4 bytes)
                byte tmp = data[offset];
                data[offset] = data[offset + 3];
                data[offset + 3] = tmp;
                tmp = data[offset + 1];
                data[offset + 1] = data[offset + 2];
                data[offset + 2] = tmp;
            }
            return data;
        }
    }
}
