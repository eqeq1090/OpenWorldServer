using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace SampleServer.Server
{

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public class GameDataBase<T> where T : class
    {
        public GameDataBase()
        {

        }

        public byte[] Serialize()
        {
            int size = Marshal.SizeOf(typeof(T));
            byte[] array = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, array, 0, size);
            Marshal.FreeHGlobal(ptr);
            return array;
        }

        public static T Deserialize(byte[] array)
        {
            int size = Marshal.SizeOf(typeof(T));
            IntPtr ptr = Marshal.AllocHGlobal(size);
            Marshal.Copy(array, 0, ptr, size);
            T s = (T)Marshal.PtrToStructure(ptr, typeof(T));
            Marshal.FreeHGlobal(ptr);
            return s;
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public struct TestData
    {
        public EProtocoleType TestType;
        public long InstanceID;
        public float Float;
        public bool Bool;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string Name;

        public TestData(long instanceID, string name, float floatData, bool isBool, EProtocoleType type)
        {
            InstanceID = instanceID;
            Name = name;
            Float = floatData;
            Bool = isBool;
            TestType = type;
        }
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public class TestPacketReq : GameDataBase<TestPacketReq>
    {
        public bool IsSuccess;
        public TestData TestValue;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string Message;

        public TestPacketReq()
        {

        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public class TestPacketRes : GameDataBase<TestPacketRes>
    {
        public bool IsSuccess;
        public int TestIntValue;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string Message;

        public TestPacketRes()
        {

        }
    }
}
