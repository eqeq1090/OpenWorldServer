using OpenWorldServer.Network;
using SampleClient.Client;
using System;
using System.Collections.Generic;
using System.Net;

namespace SampleClient
{
    class Program
    {
        static List<IPeer> mGameServers = new List<IPeer>();
        static void Main(string[] args)
        {
            PacketBufferManager.Initialize(2000);

            NetworkService service = new NetworkService(true);

            //endpoint 정보를 갖고있는 connector 생성, 만들어둔 networkservice 객체를 넣어줌
            NetConnector connector = new NetConnector(service);
            //접속 성공시 호출될 콜백 메서드 지정
            connector.ConnectCallback += OnConnectedGameServer;
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse("222.107.110.135"), 9000);
            connector.Connect(endPoint);

            service.DisableHeartbeat();
            while(true)
            {
                Console.Write("> ");
                string line = Console.ReadLine();
                if(line == "q")
                {
                    break;
                }

                PacketBase msg = PacketBase.Create((short)EProtocoleType.ChatMsgReq);
                msg.Push(line);
                mGameServers[0].Send(msg);
            }

            ((RemoteServerPeer)mGameServers[0]).mToken.Disconnect();

            Console.ReadKey();
        }
        static void OnConnectedGameServer(UserToken serverToken)
        {
            lock(mGameServers)
            {
                IPeer server = new RemoteServerPeer(serverToken);
                serverToken.OnConnected();
                mGameServers.Add(server);
                Console.WriteLine("Connected!");
            }
        }
    }
}
