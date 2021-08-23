using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenWorldServer.Network
{
    public class PacketBufferManager
    {
        static object mCSBuffer = new object();
        static Stack<PacketBase> mPool;
        static int mPoolCapacity;

        public static void Initialize(int capacity)
        {
            mPool = new Stack<PacketBase>();
            mPoolCapacity = capacity;
            Allocate();
        }

        static void Allocate()
        {
            for(int i = 0; i < mPoolCapacity; ++i)
            {
                mPool.Push(new PacketBase());
            }
        }

        public static PacketBase Pop()
        {
            lock(mCSBuffer)
            {
                if(mPool.Count <= 0)
                {
                    Console.WriteLine("reallocate");
                    Allocate();
                }
                return mPool.Pop();
            }
        }

        public static void Push(PacketBase packet)
        {
            lock(mCSBuffer)
            {
                mPool.Push(packet);
            }
        }
    }
}
