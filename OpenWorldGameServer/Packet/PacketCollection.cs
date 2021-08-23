using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace OpenWorldGameServer.Packet
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector3
    {
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float X;
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Y;
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Z;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Rotation3
    {
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Pitch;
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Yaw;
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Roll;
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PacketPlayerMove
    {
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float X;
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Y;
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Z;

        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Pitch;
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Yaw;
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Roll;
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct PacketCharacterMove
    {
        public int UserIndex;

        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float X;
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Y;
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Z;

        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Pitch;
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Yaw;
        [MarshalAs(UnmanagedType.R8, SizeConst = 8)]
        public float Roll;
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public struct PacketSetNicknameAck
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Nickname;

        [MarshalAs(UnmanagedType.U2, SizeConst = 2)]
        public short ResultType;
    }
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
    public struct PacketSetNicknameReq
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
        public string Nickname;
    }

}
