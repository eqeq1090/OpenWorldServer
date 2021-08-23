using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenWorldServer.Network
{
    /// <summary>
    /// 두개의 큐를 교체해가며 활용
    /// IO스레드에서 입력큐에 막 쌓아놓고,
    /// 로직 스레드에서 큐를 뒤바꾼뒤(swap) 쌓아놓은 패킷을 가져가 처리한다.
    /// </summary>
    class DoubleBufferingQueue : ILogicQueue
    {
        //실제 데이터가 들어갈 큐
        Queue<PacketBase> mQueue1;
        Queue<PacketBase> mQueue2;

        //각각의 큐에 대한 참조
        Queue<PacketBase> mRefInput;
        Queue<PacketBase> mRefOutput;

        object CSWrite;

        public DoubleBufferingQueue()
        {
            //초기 세팅은 큐와 참조가 1:1로 매칭되게 설정한다.
            mQueue1 = new Queue<PacketBase>();
            mQueue2 = new Queue<PacketBase>();
            mRefInput = mQueue1;
            mRefOutput = mQueue2;

            CSWrite = new object();
        }
        /// <summary>
        /// IO스레드에서 전달된 패킷을 보관
        /// </summary>
        /// <param name="msg"></param>
        void ILogicQueue.Enqueue(PacketBase msg)
        {
            lock(CSWrite)
            {
                mRefInput.Enqueue(msg);
            }
        }

        Queue<PacketBase> ILogicQueue.GetAll()
        {
            Swap();
            return mRefOutput;
        }
        /// <summary>
        /// 입력큐와 출력큐를 뒤바꿈
        /// </summary>
        void Swap()
        {
            lock(CSWrite)
            {
                Queue<PacketBase> temp = mRefInput;
                mRefInput = mRefOutput;
                mRefOutput = temp;
            }
        }
    }
}
