using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
using System.Net;

namespace ServerLibrary.Network
{
    class Listener
    {
        //비동기 Accept를 위한 변수
        SocketAsyncEventArgs mAcceptArgs;
        //클라이언트 접속 처리 소켓
        Socket mListenSocket;
        //Accept 처리 순서를 제어하기 위한 이벤트 변수
        AutoResetEvent mFlowControlEvent;
        //새로운 클라이언트가 접속했을 때 호출되는 콜백
        public delegate void NewClientHandler(Socket clientSocket, object token);
        public NewClientHandler CallbackOnNewClient;

        public Listener()
        {
            CallbackOnNewClient = null;
        }

        public void Start(string host, int port, int backLog)
        {
            //소켓 생성
            mListenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            IPAddress address;
            if(host == "0.0.0.0")
            {
                address = IPAddress.Any;
            }
            else
            {
                address = IPAddress.Parse(host);
            }
            IPEndPoint endPoint = new IPEndPoint(address, port);

            try
            {
                //소켓에 host 정보를 바인딩 시킨 뒤 Listen 메소드를 호출하여 준비
                mListenSocket.Bind(endPoint);
                mListenSocket.Listen(backLog);

                mAcceptArgs = new SocketAsyncEventArgs();
                mAcceptArgs.Completed += new EventHandler<SocketAsyncEventArgs>(OnAcceptCompleted);

                //클라이언트가 들어오기를 대기
                //비동기 메서드임으로 블로킹 되지않고 바로 리턴됨
                //콜백 메서드를 통해서 접속통보 처리
                //mListenSocket.AcceptAsync(mAcceptArgs);
                Thread listenThread = new Thread(DoListen);
                listenThread.Start();

            }
            catch(Exception e)
            {
                Console.WriteLine(/*"클라이언트 접속오류" + ": " +*/e.Message);
            }
        }

        void DoListen()
        {
            //Accept 제어 처리를 위한 이벤트 객체 생성
            mFlowControlEvent = new AutoResetEvent(false);

            while(true)
            {
                //SocketAsyncEventArgs를 재사용 하기 위해서 null로 만들어 준다.
                mAcceptArgs.AcceptSocket = null;

                bool pending = true;

                try
                {
                    //비동기 Accept를 호출하여 클라이언트의 접속을 받아들임
                    //비동기 메서드지만 동기적으로 수행이 완료될 경우도 있으니 리턴값을 확인하여 분기
                    pending = mListenSocket.AcceptAsync(mAcceptArgs);
                }
                catch(Exception e)
                {
                    Console.WriteLine(e.Message);
                    continue;
                }
                //즉시 완료되면 이벤트가 발생하지 않으므로 리턴값이 false일 경우 콜백 메서드를 직접 호출해줍니다.
                //pending 상태라면 비동기 요청이 들어간 상태이므로 콜백 메서드를 기다리면 됩니다.
                if(!pending)
                {
                    OnAcceptCompleted(null, mAcceptArgs);
                }
                //클라이언트 접속 처리가 완료되면 이벤트 객체의 신호를 전달받아 다시 루프를 수행
                mFlowControlEvent.WaitOne();
            }
        }

        void OnAcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            if(e.SocketError == SocketError.Success)
            {
                //새로 생긴 소켓을 보관
                Socket clientSocket = e.AcceptSocket;
                clientSocket.NoDelay = true;

                //이 클래스에서는 Accept 까지의 역할만 수행하고 클라이언트의 접속 이후의 처리는
                //외부로 넘기기 위해서 콜백 메서드를 호출(로직 구현부와의 분리를 위해)
                if(CallbackOnNewClient != null)
                {
                    CallbackOnNewClient(clientSocket, e.UserToken);
                }
                //다음 연결을 받아들인다
                mFlowControlEvent.Set();
                return;
            }
            else
            {
                Console.WriteLine(e.SocketError.ToString());
            }

            mFlowControlEvent.Set();
        }
    }
}
