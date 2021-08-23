using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;


namespace OpenWorldServer.Network
{
    public class NetworkService
    {
        //클라이언트 접속을 받아들이는 객체
        //Listener mListener;
        //수신용 풀
        SocketAsyncEventArgsPool mReceiveEventArgsPool;
        //전송용 풀
        SocketAsyncEventArgsPool mSendEventArgsPool;
        //송,수신 버퍼 풀링
        //BufferManager mBufferManager;

        //클라이언트의 접속이 이루어졌을 때 호출되는 델리게이트
        public delegate void SessionHandler(UserToken token);
        public SessionHandler mSessionCreatedCallback { get; set; }

        public LogicMessageEntry mLogicEntry { get; private set; }
        public ServerUserManager mUserManager { get; private set; }

        public const int MAX_CONNECTIONS = 100;
        public const int BUFFER_SIZE = 1024;
        public const int PRE_ALLOC_COUNT = 2;


        /// <summary>
        /// 로직 스레드를 사용하려면 useLogicThread를 true로 설저한다.
        /// -> 하나의 로직 스레드 생성
        /// -> 메시지는 큐잉되어 싱글 스레드에서 처리된다.
        /// 
        /// 사용하지 않으려면 false
        /// -> 별도의 로직 스레드는 생성하지 않는다.
        /// -> IO 스레드에서 직접 메시지 처리
        /// </summary>
        /// <param name="useLogicThread"></param>
        public NetworkService(bool useLogicThread = false)
        {
            mSessionCreatedCallback = null;
            mUserManager = new ServerUserManager();
            if(useLogicThread)
            {
                mLogicEntry = new LogicMessageEntry(this);
                mLogicEntry.Start();
            }
        }
        public void Initialize()
        {
            int maxConnections = 10000;
            int bufferSize = 1024;
            Initialize(maxConnections, bufferSize);
        }
        public void Initialize(int maxConnections, int bufferSize)
        {
            //receive 버퍼만 할당
            //send 벞퍼는 보낼때마다 할당하든 풀에서 얻어오든 하기 때문에
            int preAllocCount = 1;
            BufferManager bufferManager = new BufferManager(maxConnections * bufferSize * preAllocCount, bufferSize);
            mReceiveEventArgsPool = new SocketAsyncEventArgsPool(maxConnections);
            mSendEventArgsPool = new SocketAsyncEventArgsPool(maxConnections);

            bufferManager.InitBuffer();

            SocketAsyncEventArgs arg;
            for(int i = 0; i < maxConnections; i++)
            {
                //일단 OnNewClient에서 그때 그때 생성하도록 하고,
                //소켓이 종료되면 null로 세팅하여 오류 발생ㅇ시 확실히 드러날 수 있게
                arg = new SocketAsyncEventArgs();
                arg.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
                arg.UserToken = null;

                bufferManager.SetBuffer(arg);
                mReceiveEventArgsPool.Push(arg);



                arg = new SocketAsyncEventArgs();
                arg.Completed += new EventHandler<SocketAsyncEventArgs>(SendCompleted);
                arg.UserToken = null;

                //send 버퍼는 보낼 때 설정
                arg.SetBuffer(null, 0, 0);
                mSendEventArgsPool.Push(arg);
            }
        }

        public void Listen(string host, int port, int backLog)
        {
            Listener listener = new Listener();
            listener.CallbackOnNewClient += OnNewClient;
            listener.Start(host, port, backLog);

            //heartbeat
            byte checkInterval = 10;
            mUserManager.StartHeartbeatChecking(checkInterval, checkInterval);
        }
        public void DisableHeartbeat()
        {
            mUserManager.StopChecking();
        }
        /// <summary>
        /// 원격서버에 접속 성공 했을 때 호출
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="token"></param>
        public void OnConnectCompleted(Socket socket, UserToken token)
        {
            token.OnSessionClosed += OnSessionClosed;

            mUserManager.AddUser(token);
            //SocketAsyncEventArgsPool에서 빼오지 않고 그때 그때 할당
            //풀은 서버에서 클라이언트와의 통신용으로만 쓰려고 만든 것이기 떄문
            //클라 입장에서 서버와 통신을 할 때는 접속한 서버당 두개의 EventArgs만 있으면 되기 때문에 그냥 new해서 쓴다.
            //서버간 연결에서도 마찬가지
            //풀링처리를 하려면 C -> S로 가는 별도의 풀을 만들어서 써야함
            SocketAsyncEventArgs receiveArgs = new SocketAsyncEventArgs();
            receiveArgs.Completed += new EventHandler<SocketAsyncEventArgs>(ReceiveCompleted);
            receiveArgs.UserToken = token;
            receiveArgs.SetBuffer(new byte[1024], 0, 1024);

            SocketAsyncEventArgs sendArgs = new SocketAsyncEventArgs();
            sendArgs.Completed += new EventHandler<SocketAsyncEventArgs>(SendCompleted);
            sendArgs.UserToken = token;
            sendArgs.SetBuffer(null, 0, 0);

            BeginReceive(socket, receiveArgs, sendArgs);
        }
        /// <summary>
        /// 새로운 클라이언트가 접속 성공했을 때 호출
        /// AcceptAsync의 콜백 메서드에서 호출되며 여러 스레드에서 동시에 호출될 수 있기 때문에 공유자원 접근 시 주의 필요
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="token"></param>
        void OnNewClient(Socket clientSocket, object token)
        {
            //풀에서 하나 꺼내와 사용
            SocketAsyncEventArgs receiveArgs = mReceiveEventArgsPool.Pop();
            SocketAsyncEventArgs sendArgs = mSendEventArgsPool.Pop();

            // UserToken은 매번 새로 생성하여 깨끗한 인스턴스로 넣어줌
            UserToken userToken = new UserToken(mLogicEntry);
            userToken.OnSessionClosed += OnSessionClosed;
            receiveArgs.UserToken = userToken;
            sendArgs.UserToken = userToken;

            mUserManager.AddUser(userToken);

            userToken.OnConnected();
            

            if(mSessionCreatedCallback != null)
            { 
                mSessionCreatedCallback(userToken);
            }
            BeginReceive(clientSocket, receiveArgs, sendArgs);

            PacketBase msg = PacketBase.Create((short)UserToken.SYS_START_HEARTBEAT);
            byte sendInterval = 5;
            msg.Push(sendInterval);
            userToken.Send(msg);
        }

        void BeginReceive(Socket socket, SocketAsyncEventArgs receiveArgs, SocketAsyncEventArgs sendArgs)
        {
            //receiveArgs, sendArgs 아무곳에나 꺼내와도 됨. 둘 다 동일한 UserToken을 참조
            UserToken userToken = receiveArgs.UserToken as UserToken;
            userToken.SetEventArgs(receiveArgs, sendArgs);


            //생성된 클라이언트 소켓을 보관해 놓고 통신할 때 사용한다.
            userToken.Socket = socket;

            //데이터를 받을 수 있도록 소켓 메서드를 호출해준다.
            //비동기로 수신할 경우 워커 스레드에서 대기중으로 있다가 Complete에 설정해놓은 메서드가 호출된다.
            //동기로 완료될 경우에는 직접 완료 메서드를 호출해줘야 한다.
            bool pending = socket.ReceiveAsync(receiveArgs);
            if(!pending)
            {
                ProcessReceive(receiveArgs);
            }
        }

        void ReceiveCompleted(object sender, SocketAsyncEventArgs e)
        {
            if(e.LastOperation == SocketAsyncOperation.Receive)
            {
                ProcessReceive(e);
                return;
            }
            throw new ArgumentException("The last opertaion completed on the socket was not a receive");
        }
    

        void SendCompleted(object sender, SocketAsyncEventArgs e)
        {
            try
            {
                UserToken token = e.UserToken as UserToken;
                token.ProcessSend(e);
            }
            catch(Exception)
            { 

            }

            //mSendEventArgsPool.Push(e);
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            UserToken token = e.UserToken as UserToken;
            if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
            {
                //이후의 작업은 UserToken에 맡긴다.
                token.OnReceive(e.Buffer, e.Offset, e.BytesTransferred);

                //다음 메시지 수신을 위해서 다시 ReceiveAsync 메서드를 호출한다.
                bool pending = token.Socket.ReceiveAsync(e);
                if (!pending)
                {
                    ProcessReceive(e);
                }
            }
            else
            {
                try
                {
                    token.Close();
                }
                catch (Exception)
                {
                    Console.Write("Already closed this socket");
                }
            }
        }
        void OnSessionClosed(UserToken token)
        {
            mUserManager.Remove(token);

            //버퍼는 반환할 필요가 없다. SocketAsyncEventArgs 가 버퍼를 들고있기 때문에
            //이것을 재사용 할 때 물고 있는 버퍼를 그대로 사용
            if(mReceiveEventArgsPool != null)
            {
                mReceiveEventArgsPool.Push(token.mReceiveEventArgs);
            }
            if(mSendEventArgsPool != null)
            {
                mSendEventArgsPool.Push(token.mSendEventArgs);
            }
            token.SetEventArgs(null, null);
        }
    }
}
