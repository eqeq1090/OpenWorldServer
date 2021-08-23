using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using OpenWorldServer.Network;
using SampleServer.Packet;

namespace SampleServer.Server
{
    class GameUser : IPeer
    {
        UserToken mToken;

        public int UserIndex;

        public string Nickname;

        public GameUser(UserToken token)
        {
            mToken = token;
            mToken.SetPeer(this);
            UserIndex = Program.UserNum;
            Program.UserNum++;
        }
        void IPeer.OnRemoved()
        {
            Program.RemoveUser(this);
        }
        public void Send(PacketBase packet)
        {
            mToken.Send(packet);
        }
        void IPeer.OnMessage(PacketBase msg)
        {
            EProtocoleType protocol = (EProtocoleType)msg.ProtocolType;//(EProtocoleType)msg.PopProtocolType();
            switch (protocol)
            {
                case EProtocoleType.ChatMsgReq:
                    {
                        string text = msg.PopString();
                        Console.WriteLine(string.Format("text {0}", text));

                        PacketBase response = PacketBase.Create((short)EProtocoleType.ChatMsgAck);
                        response.Push(text);
                        foreach(GameUser user in Program.UserList)
                        {
                            user.Send(response);
                        }
                        
                        //Send(response);

                        if (text.Equals("exit"))
                        {
                            //대량의 메시지를 한꺼번에 보낸 후 종료하는 시나리오 테스트
                            for (int i = 0; i < 1000; ++i)
                            {
                                PacketBase dummy = PacketBase.Create((short)EProtocoleType.ChatMsgAck);
                                dummy.Push(i.ToString());
                                Send(dummy);
                            }

                            mToken.Ban();
                        }
                        //종료
                    }
                    break;
                case EProtocoleType.PlayerMove:
                    {
                        PacketPlayerMove data = msg.DeserializeStruct<PacketPlayerMove>();
                    }
                    break;
                case EProtocoleType.SetNicknameAck:
                    {
                        PacketSetNicknameReq data = msg.DeserializeStruct<PacketSetNicknameReq>();
                        Nickname = data.Nickname;


                    }
                    break;
            }

        }
        void IPeer.Disconnect()
        {
            //종료
            mToken.Ban();
        }

        //void IPeer.ProcessUserOperation(PacketBase msg)
        //{

        //}
    }
}
