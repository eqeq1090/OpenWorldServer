using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;


namespace ServerLibrary.Network
{
    public class PacketBase
    {
        public UserToken Owner { get; private set; }
        public byte[] Buffer { get; private set; }
        public int Position { get; private set; }
        public int Size { get; private set; }
        public Int16 ProtocolType { get; private set; }

        public static PacketBase Create(Int16 type)
        {
            PacketBase packet = new PacketBase();
            packet.SetProtocol(type);
            return packet;
        }
        public static void Destroy(PacketBase packet)
        { 
        }
        public PacketBase(ArraySegment<byte> buffer, UserToken owner)
        {
            //참조로만 보관하여 작업
            Buffer = buffer.Array;
            //헤더는 읽을필요 없으니 그 이후부터 시작
            Position = Defines.HEADERSIZE;
            Size = buffer.Count;
            //프로토콜 아이디만 확인할 경우도 있으므로 미리 뽑아놓는다.
            ProtocolType = PopProtocolType();

            Owner = owner;
        }

        public PacketBase(byte[] buffer, UserToken owner)
        {
            Buffer = buffer;

            Position = Defines.HEADERSIZE;

            Owner = owner;
        }

        public PacketBase()
        {
            Buffer = new byte[1024];
        }

        public Int16 PopProtocolType()
        {
            return PopInt16();
        }

        public void CopyTo(PacketBase target)
        {
            target.SetProtocol(ProtocolType);
            target.OverWrite(Buffer, Position);
        }

        public void OverWrite(byte[] source, int position)
        {
            Array.Copy(source, Buffer, source.Length);
            Position = position;
        }

        public byte PopByte()
        {
            byte data = Buffer[Position];
            Position += sizeof(byte);
            return data;
        }

        public Int16 PopInt16()
        {
            Int16 data = BitConverter.ToInt16(Buffer, Position);
            Position += sizeof(Int16);
            return data;
        }

        public Int32 PopInt32()
        {
            Int32 data = BitConverter.ToInt32(Buffer, Position);
            this.Position += sizeof(Int32);
            return data;
        }

        public string PopString()
        {
            //문자열 길이는 최대 2바이트 까지. 0 ~ 32767
            Int16 length = BitConverter.ToInt16(Buffer, Position);
            Position += sizeof(Int16);

            //인코딩은 utf8로 통일
            string data = System.Text.Encoding.UTF8.GetString(Buffer, Position, length);
            Position += length;

            return data;
        }

        public byte[] PopStringToBytes()
        {
            //문자열 길이는 최대 2바이트 까지. 0 ~ 32767
            Int16 length = BitConverter.ToInt16(Buffer, Position);
            Position += sizeof(Int16);

            //인코딩은 utf8로 통일
            byte[] array = new byte[length];
            Array.Copy(Buffer, Position, array, 0, length);
            Position += length;

            return array;
        }

        public float PopFloat()
        {
            float data = BitConverter.ToSingle(Buffer, Position);
            Position += sizeof(float);
            return data;
        }
        public T DeserializeStruct<T>()
        {
            var size = Marshal.SizeOf(typeof(T));
            if (size > Buffer.Length - Position)
                size = Buffer.Length - Position;
            byte[] array = new byte[size];
            Array.Copy(Buffer, Position, array, 0, size);

            var ptr = Marshal.AllocHGlobal(size); 
            Marshal.Copy(array, 0, ptr, size); 
            var result = (T)Marshal.PtrToStructure(ptr, typeof(T)); 
            Marshal.FreeHGlobal(ptr);
            Position += size;
            return result;

        }

        public string SerealizeStructToJson<T>(T data)
        {
            string json = JsonSerializer.Serialize(data);
            return json;
            //JsonConverter converter = new JsonConverter();
            //converter.
        }
        public T DeserializeJsonToStruct<T>(string json)
        {
            T data = JsonSerializer.Deserialize<T>(json);
            return data;
        }


        public void SetProtocol(Int16 type)
        {
            ProtocolType = type;

            //헤더는 나중에 넣을 것 이므로 데이터부터 넣을 수 있도록 위치를 점프
            Position = Defines.HEADERSIZE;

            PushInt16(type);
        }

        public void RecordSize()
        {
            //header + body를 합한 사이즈를 입력
            byte[] header = BitConverter.GetBytes(Position);
            header.CopyTo(Buffer, 0);
        }

        public void PushInt16(Int16 data)
        {
            byte[] tempBuffer = BitConverter.GetBytes(data);
            tempBuffer.CopyTo(Buffer, Position);
            Position += tempBuffer.Length;
        }
        public void Push(byte data)
        {
            byte[] tempBuffer = BitConverter.GetBytes(data);
            tempBuffer.CopyTo(Buffer, Position);
            Position += sizeof(byte);
        }

        public void Push(Int16 data)
        {
            byte[] tempBuffer = BitConverter.GetBytes(data);
            tempBuffer.CopyTo(Buffer, Position);
            Position += tempBuffer.Length;
        }

        public void Push(Int32 data)
        {
            byte[] tempBuffer = BitConverter.GetBytes(data);
            tempBuffer.CopyTo(Buffer, Position);
            Position += tempBuffer.Length;
        }

        public void Push(string data)
        {
            byte[] tempBuffer = Encoding.UTF8.GetBytes(data);

            Int16 length = (Int16)tempBuffer.Length;
            byte[] lengthBuffer = BitConverter.GetBytes(length);
            lengthBuffer.CopyTo(Buffer, Position);
            Position += sizeof(Int16);

            tempBuffer.CopyTo(Buffer, Position);
            Position += tempBuffer.Length;
        }
        public void Push(float data)
        {
            byte[] tempBuffer = BitConverter.GetBytes(data);
            tempBuffer.CopyTo(Buffer, Position);
            Position += tempBuffer.Length;
        }

        public void PushStruct<T>(T data)
        {
            var size = Marshal.SizeOf(typeof(T)); 
            var array = new byte[size];
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(this, ptr, true);
            Marshal.Copy(ptr, array, 0, size);
            Marshal.FreeHGlobal(ptr);

            Array.Copy(array, 0, Buffer, Position, size);
            Position += size;
        }
        //public byte[] Data
        //{
        //    get;set;
        //}
        //public void SetData(byte[] data, int length)
        //{
        //    Data = new byte[length];
        //    Array.Copy(data, Data, length);
        //}

        //public byte[] GetSendBytes()
        //{
        //    byte[] typeBytes = BitConverter.GetBytes(ProtocolType);
        //    int headerSize = (int)Data.Length;
        //    byte[] headerBytes = BitConverter.GetBytes(headerSize); 
        //    byte[] sendBytes = new byte[headerBytes.Length + typeBytes.Length + Data.Length];

        //    //헤더 복사, 헤더 == 데이터의 크기
        //    Array.Copy(headerBytes, 0, sendBytes, 0, headerBytes.Length);
        //    //타입 복사
        //    Array.Copy(typeBytes, 0, sendBytes, headerBytes.Length, typeBytes.Length);
        //    //데이터 복사
        //    Array.Copy(Data, 0, sendBytes, headerBytes.Length + typeBytes.Length, Data.Length);

        //    return sendBytes;
        //}
    }

    

}
