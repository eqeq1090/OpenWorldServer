using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;

namespace ServerLibrary.Network
{
    public class UserToken
    {
        enum EState
        {
            Idle,
            Connected,
            //종료가 예약됨
            //SendingList에 대기중인 상태에서 Disconnect를 호출한 경우,
            //남아있는 패킷을 모두 보낸 뒤 끊도록 하기 위한 상태값
            ReserveClosing,
            Closed,
        }
        //종료 요청. S -> C
        const short SYS_CLOSE_REQ = 0;
        //종료 응답. C -> S
        const short SYS_CLOSE_ACK = -1;
        //하트비트 시작. S -> C
        public const short SYS_START_HEARTBEAT = -2;
        //하트비트 갱신. C -> S
        public const short SYS_UPDATE_HEARTBEAT = -3;

        //close 중복 처리 방지를 위한 플래그
        //0 = 연결, 1 = 종료
        int mIsClosed;

        EState mCurrentState;
        
        //session 객체. 어플리케이션 단에서 구현하여 사용
        IPeer mPeer;
        public SocketAsyncEventArgs mSendEventArgs { get; private set; }
        public SocketAsyncEventArgs mReceiveEventArgs { get; private set; }
        MessageResolver mResolver;
        public Socket Socket { get; set; }
        //BufferList 적용을 위해 queue에서 list로 변경
        List<ArraySegment<byte>> mSendingList;
        //SendingList lock처링에 사용되는 객체
        private object CS_SendingQueue;

        IMessageDispatcher mDispatcher;

        public delegate void ClosedDelegate(UserToken token);
        public ClosedDelegate OnSessionClosed;

        public long LastestHeartbeatTime { get; private set; }
        HeartBeatSender mHeartbeatSender;
        bool mAutoHeartbeat;

        Queue<PacketBase> mSendingQueue;

        public UserToken(IMessageDispatcher dispatcher)
        {
            mDispatcher = dispatcher;
            CS_SendingQueue = new object();

            mResolver = new MessageResolver();
            mPeer = null;
            mSendingList = new List<ArraySegment<byte>>();
            LastestHeartbeatTime = DateTime.Now.Ticks;

            mCurrentState = EState.Idle;
        }

        public void OnConnected()
        {
            mCurrentState = EState.Connected;
            mIsClosed = 0;
            mAutoHeartbeat = true;
        }
        public void SetPeer(IPeer peer)
        {
            mPeer = peer;
        }

        public void SetEventArgs(SocketAsyncEventArgs receiveArgs, SocketAsyncEventArgs sendArgs)
        {
            mReceiveEventArgs = receiveArgs;
            mSendEventArgs = sendArgs;
        }

        public void OnReceive(byte[] buffer, int offset, int byteTransferred)
        {
            mResolver.OnReceive(buffer, offset, byteTransferred, OnMessegeCompleted);
        }

        void OnMessegeCompleted(ArraySegment<byte> buffer)
        {
            if(mPeer == null)
            {
                return;
            }    
            if(mDispatcher != null)
            {
                //로직 스레드의 큐를 타고 호출되도록 함
                mDispatcher.OnMessage(this, buffer);
            }
            else
            {
                //IO스레드에서 직접 호출
                PacketBase msg = new PacketBase(buffer, this);
                OnMessage(msg);
            }
        }
        public void OnMessage(PacketBase msg)
        {
            //active close를 위한 코딩
            //서버에서 종료하라고 연락이 왔는지 체크한다.
            //만약 종료신호가 맞다면 disconnect를 호춣하여 받은 쪽에서 먼저 종료 요청을 보낸다.
            switch (msg.ProtocolType)
            {
                case SYS_CLOSE_REQ:
                    Disconnect();
                    return;
                case SYS_START_HEARTBEAT:
                    {
                        //순서대로 파싱해야 하므로 프로토콜 아이디는 버린다.
                        msg.PopProtocolType();
                        //전송 인터벌
                        byte interval = msg.PopByte();
                        mHeartbeatSender = new HeartBeatSender(this, interval);
                        if(mAutoHeartbeat)
                        {
                            StartHearbeat();
                        }
                    }
                    return;
                case SYS_UPDATE_HEARTBEAT:
                    LastestHeartbeatTime = DateTime.Now.Ticks;
                    return;
            }
            if(mPeer != null)
            {
                try
                {
                    switch (msg.ProtocolType)
                    {
                        case SYS_CLOSE_ACK:
                            mPeer.OnRemoved();
                            break;
                        default:
                            mPeer.OnMessage(msg);
                            break;
                    }

                }
                catch(Exception e)
                {
                    Close();
                }
            }

            if(msg.ProtocolType == SYS_CLOSE_ACK)
            {
                if(OnSessionClosed != null)
                {
                    OnSessionClosed(this);
                }
            }

        }
        public void Close()
        {
            //중복 수행을 막는다
            if(Interlocked.CompareExchange(ref mIsClosed, 1, 0) == 1)
            {
                return;
            }
            if(mCurrentState == EState.Closed)
            {
                return;
            }

            mCurrentState = EState.Closed;
            Socket.Close();
            Socket = null;

            mSendEventArgs.UserToken = null;
            mReceiveEventArgs.UserToken = null;

            mSendingList.Clear();
            mResolver.ClearBuffer();

            if(mPeer != null)
            {
                PacketBase msg = PacketBase.Create((short)-1);
                if(mDispatcher != null)
                {
                    mDispatcher.OnMessage(this, new ArraySegment<byte>(msg.Buffer, 0, msg.Position));
                }
                else
                {
                    OnMessage(msg);
                }
            }
        }

        public void Send(ArraySegment<byte> data)
        {
            //큐가 비어 있다면 큐에 추가하고 바로 비동기 전송 메서드를 호출함
            lock(CS_SendingQueue)
            {
                mSendingList.Add(data);
                if (mSendingList.Count > 1)
                {
                    //큐에 무언가가 들어있다면 아직 이전 전송이 완료되지 않은 상태이므로 큐에 추가만 하고 리턴
                    //현재 수행중인 SendAsync가 완료된  이후에 큐를 검사하여 데이터가 있으면 SendAsync를 호출하여 전송해줄 것이다.
                    return;
                }
            }

            StartSend();
        }
        public void Send(PacketBase msg)
        {
            msg.RecordSize();
            Send(new ArraySegment<byte>(msg.Buffer, 0, msg.Position));
        }

        void StartSend()
        {
            try
            {
                //성능 향상을 위해 SetBuffer에서 BufferList를 사용하는 방식으로 변경함
                mSendEventArgs.BufferList = mSendingList;

                //비동기 전송 시작
                bool pending = Socket.SendAsync(mSendEventArgs);
                if(!pending)
                {
                    ProcessSend(mSendEventArgs);
                }
            }
            catch(Exception e)
            {
                if (Socket == null)
                {
                    Close();
                    return;
                }
                Console.WriteLine("Send error, Close Socket " + e.Message);
                throw new Exception(e.Message, e);
            }
        }
        static int mSendCount = 0;
        static object mCSCount = new object();


        public void ProcessSend(SocketAsyncEventArgs e)
        {
            if(e.BytesTransferred <= 0 || e.SocketError != SocketError.Success)
            {
                //연결이 끊겨서 이미 소켓이 종료된 경우일 것임
                return;
            }
            lock(CS_SendingQueue)
            {
                //리스트에 들어있는 데이터의 총 바이트 수
                var size = mSendingList.Sum(obj => obj.Count);

                //전송이 완료되기 전에 추가 전송 요청을 했다면 SendingList에 무언가 더 들어있을 것임
                if(e.BytesTransferred != size)
                {
                    //일단 close 시킴
                    if(e.BytesTransferred < mSendingList[0].Count)
                    {
                        string error = string.Format("Need to send more! transferred {0}, packet size {1}", e.BytesTransferred, size);
                        Console.WriteLine(error);
                        Close();
                        return;
                    }
                    //보낸 만큼 뺴고 나머지 대기중인 데이터들을 한방에 보내버린다.
                    int sendIndex = 0;
                    int sum = 0;
                    for(int i = 0; i < mSendingList.Count; i++)
                    {
                        sum += mSendingList[i].Count;
                        if(sum <= e.BytesTransferred)
                        {
                            //여기까지는 전송 완료된 데이터 인덱스
                            sendIndex = i;
                            continue;
                        }
                        break;
                    }
                    //전송 완료된 것은 리스트에서 삭제한다..
                    mSendingList.RemoveRange(0, sendIndex + 1);

                    //나머지 데이터들을 한방에 보낸다
                    StartSend();
                    return;
                }
                //더이상 보낼 것이 없다
                mSendingList.Clear();

                //종료가 예약된 경우, 보낼 건 다 보냈으니 진짜 종료처리
                if(mCurrentState == EState.ReserveClosing)
                {
                    Socket.Shutdown(SocketShutdown.Send);
                }
            }
        }
        public void Disconnect()
        {
            //close the socket associatted with client
            try
            {
                if(mSendingList.Count <= 0)
                {
                    Socket.Shutdown(SocketShutdown.Send);
                    return;
                }
                mCurrentState = EState.ReserveClosing;
            }
            catch(Exception)
            {
                Close();
            }
        }
        public void Ban()
        {
            try
            {
                ByeBye();
            }
            catch(Exception)
            {
                Close();
            }
        }
        void ByeBye()
        {
            PacketBase bye = PacketBase.Create(SYS_CLOSE_REQ);
            Send(bye);
        }
        public bool IsConnecteed()
        {
            return mCurrentState == EState.Connected;
        }

        public void StartHearbeat()
        {
            if(mHeartbeatSender != null)
            {
                mHeartbeatSender.Play();
            }
        }

        public void StopHeartbeat()
        {
            if(mHeartbeatSender != null)
            {
                mHeartbeatSender.Stop();
            }
        }

        public void UpdateHeartbeatManually(float time)
        {
            if(mHeartbeatSender != null)
            {
                mHeartbeatSender.Update(time);
            }
        }
    }
}
