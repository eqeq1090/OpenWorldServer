using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenWorldServer.Network
{
    /// <summary>
    /// 수신된 패킷을 받아 로직 스레드에서 분배 담당
    /// </summary>
    public class LogicMessageEntry : IMessageDispatcher
    {
        NetworkService mService;
        ILogicQueue mMessageQueue;
        AutoResetEvent mLogicEvent;

        public LogicMessageEntry(NetworkService service)
        {
            mService = service;
            mMessageQueue = new DoubleBufferingQueue();
            mLogicEvent = new AutoResetEvent(false);
        }

        public void Start()
        {
            Thread logic = new Thread(DoLogic);
            logic.Start();
        }

        void IMessageDispatcher.OnMessage(UserToken user, ArraySegment<byte> buffer)
        {
            //여긴 IO스레드에서 호출된다.
            //완성된 패킷을 메시지 큐에 넣어준다.
            PacketBase msg = new PacketBase(buffer, user);
            mMessageQueue.Enqueue(msg); 

            //로직 스레드를 깨워 일을 시킨다.
            mLogicEvent.Set();
        }
        /// <summary>
        /// 로직 스레드
        /// </summary>
        void DoLogic()
        {
            while(true)
            {
                mLogicEvent.WaitOne();

                DispatchAll(mMessageQueue.GetAll());
            }
        }

        void DispatchAll(Queue<PacketBase> queue)
        {
            while(queue.Count > 0)
            {
                PacketBase msg = queue.Dequeue();
                if(!mService.mUserManager.IsExist(msg.Owner))
                {
                    continue;
                }
                msg.Owner.OnMessage(msg);
            }
        }
    }
}
