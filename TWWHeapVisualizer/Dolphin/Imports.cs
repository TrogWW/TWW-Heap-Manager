
using System.Runtime.InteropServices;

namespace TWWHeapVisualizer.Dolphin
{
    public enum AllocationProtectEnum : uint
    {
        PAGE_EXECUTE = 0x00000010,
        PAGE_EXECUTE_READ = 0x00000020,
        PAGE_EXECUTE_READWRITE = 0x00000040,
        PAGE_EXECUTE_WRITECOPY = 0x00000080,
        PAGE_NOACCESS = 0x00000001,
        PAGE_READONLY = 0x00000002,
        PAGE_READWRITE = 0x00000004,
        PAGE_WRITECOPY = 0x00000008,
        PAGE_GUARD = 0x00000100,
        PAGE_NOCACHE = 0x00000200,
        PAGE_WRITECOMBINE = 0x00000400
    }

    public enum StateEnum : uint
    {
        MEM_COMMIT = 0x1000,
        MEM_FREE = 0x10000,
        MEM_RESERVE = 0x2000
    }

    public enum TypeEnum : uint
    {
        MEM_IMAGE = 0x1000000,
        MEM_MAPPED = 0x40000,
        MEM_PRIVATE = 0x20000
    }
    public struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public AllocationProtectEnum AllocationProtect;
        public IntPtr RegionSize;
        public StateEnum State;
        public AllocationProtectEnum Protect;
        public TypeEnum Type;
    }
    public struct _PSAPI_WORKING_SET_EX_BLOCK
    {
        private ulong Flags;

        // Define bit masks for each field
        private const ulong ValidMask = 0x1;
        private const ulong ShareCountMask = 0x7;
        private const ulong Win32ProtectionMask = 0x7FF;
        private const ulong SharedMask = 0x1;
        private const ulong NodeMask = 0x3F;
        private const ulong LockedMask = 0x1;
        private const ulong LargePageMask = 0x1;
        private const ulong ReservedMask = 0x7F;
        private const ulong BadMask = 0x1;
        private const ulong ReservedUlongMask = 0xFFFFFFFF;

        // Properties to access individual fields
        public bool Valid
        {
            get { return (Flags & ValidMask) != 0; }
            set { Flags = value ? (Flags | ValidMask) : (Flags & ~ValidMask); }
        }

        public ulong ShareCount
        {
            get { return (Flags >> 1) & ShareCountMask; }
            set { Flags = (Flags & ~(ShareCountMask << 1)) | ((value & ShareCountMask) << 1); }
        }

        public ulong Win32Protection
        {
            get { return (Flags >> 4) & Win32ProtectionMask; }
            set { Flags = (Flags & ~(Win32ProtectionMask << 4)) | ((value & Win32ProtectionMask) << 4); }
        }

        public bool Shared
        {
            get { return ((Flags >> 15) & SharedMask) != 0; }
            set { Flags = value ? (Flags | (SharedMask << 15)) : (Flags & ~(SharedMask << 15)); }
        }

        public ulong Node
        {
            get { return (Flags >> 16) & NodeMask; }
            set { Flags = (Flags & ~(NodeMask << 16)) | ((value & NodeMask) << 16); }
        }

        public bool Locked
        {
            get { return ((Flags >> 22) & LockedMask) != 0; }
            set { Flags = value ? (Flags | (LockedMask << 22)) : (Flags & ~(LockedMask << 22)); }
        }

        public bool LargePage
        {
            get { return ((Flags >> 23) & LargePageMask) != 0; }
            set { Flags = value ? (Flags | (LargePageMask << 23)) : (Flags & ~(LargePageMask << 23)); }
        }

        public ulong Reserved
        {
            get { return (Flags >> 24) & ReservedMask; }
            set { Flags = (Flags & ~(ReservedMask << 24)) | ((value & ReservedMask) << 24); }
        }

        public bool Bad
        {
            get { return ((Flags >> 31) & BadMask) != 0; }
            set { Flags = value ? (Flags | (BadMask << 31)) : (Flags & ~(BadMask << 31)); }
        }

        public ulong ReservedUlong
        {
            get { return Flags >> 32; }
            set { Flags = (Flags & ReservedUlongMask) | (value << 32); }
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct _PSAPI_WORKING_SET_EX_INFORMATION
    {
        public IntPtr VirtualAddress;

        public _PSAPI_WORKING_SET_EX_BLOCK VirtualAttributes;
    }

    //[StructLayout(LayoutKind.Sequential)]
    //public struct _PSAPI_WORKING_SET_EX_BLOCK
    //{
    //    public _PSAPI_WORKING_SET_EX_BLOCK_FLAG Flags;

    //    public ulong Invalid;
    //}
    //[StructLayout(LayoutKind.Explicit)]
    //public struct _PSAPI_WORKING_SET_EX_BLOCK_FLAG
    //{
    //    [FieldOffset(0)] public IntPtr Flags;
    //    [FieldOffset(0)] public bool Valid;
    //    //[FieldOffset(4)] public IntPtr Win32Protection;
    //    //[FieldOffset(15)] public IntPtr Shared;
    //}
    //struct {
    //  ULONG_PTR Valid : 1;
    //  ULONG_PTR ShareCount : 3;
    //  ULONG_PTR Win32Protection : 11;
    //  ULONG_PTR Shared : 1;
    //  ULONG_PTR Node : 6;
    //  ULONG_PTR Locked : 1;
    //  ULONG_PTR LargePage : 1;
    //  ULONG_PTR Reserved : 7;
    //  ULONG_PTR Bad : 1;
    //  ULONG_PTR ReservedUlong : 32;
    //};
    public static class Imports
    {
        [DllImport("kernel32.dll")]
        internal static extern IntPtr OpenProcess(int flags, bool b, int pid);
        [DllImport("kernel32.dll")]
        public static extern int ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwSize, out int lpNumberOfBytesRead);
        [DllImport("ntdll.dll")]
        internal static extern int NtWriteVirtualMemory(int m_iProcessHandle, UInt64 Adress, byte[] buffer, int bufferLength, out int m_iBytesWritten);
        [DllImport("ntdll.dll")]
        internal static extern int NtReadVirtualMemory(int m_iProcessHandle, UInt64 Adress, byte[] buffer, int bufferLength, out int m_iBytesWritten);
        [DllImport("kernel32.dll")]
        internal static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);
        [DllImport("psapi", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool QueryWorkingSetEx(IntPtr hProcess, [In, Out] _PSAPI_WORKING_SET_EX_INFORMATION[] pv, int cb);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);
        //[DllImport("ntdll.dll")]
        //internal static extern int NtQueryVirtualMemory(int m_iProcessHandle, UInt64 Address, MEMORY_BASIC_INFORMATION info, out MEMORY_BASIC_INFORMATION output, int size_t);
    }
}
