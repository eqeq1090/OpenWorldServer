using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace ServerLibrary.Network
{
    public class NetConnector
    {
        public delegate void ConnectHandler(UserToken token);
        public ConnectHandler ConnectCallback { get; set; }

        //원격지 서버와의 연결을 위한 소켓
        Socket mClient;

        NetworkService mNetworkService;


        public NetConnector(NetworkService networkService)
        {
            mNetworkService = networkService;
            ConnectCallback = null;
        }

        public void Connect(IPEndPoint remoteEndPoint)
        {
            mClient = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            mClient.NoDelay = true;

            //비동기 접속을 위한 event args
            SocketAsyncEventArgs eventArgs = new SocketAsyncEventArgs();
            eventArgs.Completed += OnConnectCompleted;
            eventArgs.RemoteEndPoint = remoteEndPoint;
            bool pending = mClient.ConnectAsync(eventArgs);
            if(!pending)
            {
                OnConnectCompleted(null, eventArgs);
            }
        }
        void OnConnectCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                Console.WriteLine("Connect Completed");
                UserToken token = new UserToken(this.mNetworkService.mLogicEntry);

                //데이터 수신 준비
                mNetworkService.OnConnectCompleted(mClient, token);

                if (ConnectCallback != null)
                {
                    ConnectCallback(token);
                }

            }
            else
            {
                Console.WriteLine(string.Format("Failed to connect. {0}", e.SocketError));
            }
        }
    }
}
