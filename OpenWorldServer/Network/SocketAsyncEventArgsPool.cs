using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;

namespace OpenWorldServer.Network
{
    class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> mPool;

        public SocketAsyncEventArgsPool(int capacity)
        {
            mPool = new Stack<SocketAsyncEventArgs>(capacity);
        }

        public void Push(SocketAsyncEventArgs item)
        {
            if(item == null)
            {
                throw new ArgumentNullException("Item is null");
            }
            lock(mPool)
            {
                if(mPool.Contains(item))
                {
                    throw new Exception("Aready exist item");
                }
                mPool.Push(item);
            }
        }

        public SocketAsyncEventArgs Pop()
        {
            lock(mPool)
            {
                return mPool.Pop();
            }
        }

        public int GetCount
        {
            get { return mPool.Count; }
        }
    }
}
