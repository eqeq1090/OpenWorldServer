using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerLibrary.Network
{
    class BufferManager
    {
        int mNumBytes;
        byte[] mBuffer;
        Stack<int> mFreeIndexPool;
        int mCurrentIndex;
        int mBufferSize;

        public BufferManager(int totalBytes, int bufferSize)
        {
            mNumBytes = totalBytes;
            mCurrentIndex = 0;
            mBufferSize = bufferSize;
            mFreeIndexPool = new Stack<int>();
        }

        public void InitBuffer()
        {
            mBuffer = new byte[mNumBytes];
        }

        public bool SetBuffer(SocketAsyncEventArgs args)
        {
            if(mFreeIndexPool.Count > 0)
            {
                args.SetBuffer(mBuffer, mFreeIndexPool.Pop(), mBufferSize);
            }
            else
            {
                if((mNumBytes - mBufferSize) < mCurrentIndex)
                {
                    return false;
                }
                args.SetBuffer(mBuffer, mCurrentIndex, mBufferSize);
                mCurrentIndex += mBufferSize;
            }
            return true;
        }

        public void FreeBuffer(SocketAsyncEventArgs args)
        {
            mFreeIndexPool.Push(args.Offset);
            args.SetBuffer(null, 0, 0);
        }
    }
}
