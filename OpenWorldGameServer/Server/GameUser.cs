using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using ServerLibrary.Network;
using OpenWorldGameServer.Packet;
using System.Text.Json;
using Newtonsoft.Json;

namespace OpenWorldGameServer.Server
{
    class GameUser : IPeer
    {
        UserToken mToken;

        public int UserIndex;

        public string UserName;

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
                //case EProtocoleType.PlayerMove:
                //    {
                //        PacketPlayerMove data = msg.DeserializeStruct<PacketPlayerMove>();
                //    }
                //    break;
                case EProtocoleType.SetNicknameReq:
                    {
                        //기존 구조체 직렬화
                        //PacketSetNicknameReq data = msg.DeserializeStruct<PacketSetNicknameReq>();
                        //Nickname = data.Nickname;

                        //PacketBase response = PacketBase.Create((short)EProtocoleType.SetNicknameAck);
                        //PacketSetNicknameAck ack;
                        //ack.Nickname = Nickname;
                        //ack.ResultType = (short)EServerMessageType.Success;
                        //response.PushStruct<PacketSetNicknameAck>(ack);

                        //구조체 json 직렬화
                        string json = msg.PopString();
                        //string json = raw.Replace("\r\n\t", "").Replace("\r\n", "");
                        //byte[] json = msg.PopStringToBytes();
                        //var readOnlySpan = new ReadOnlySpan<byte>(json);
                        PacketSetNicknameReq data = msg.DeserializeJsonToStruct<PacketSetNicknameReq>(json);//JsonSerializer.Deserialize<PacketSetNicknameReq>(readOnlySpan);//msg.DeserializeJsonToStruct<PacketSetNicknameReq>(json);
                        UserName = data.userName;
                        PacketBase response = PacketBase.Create((short)EProtocoleType.SetNicknameAck);
                        PacketSetNicknameAck ack = new PacketSetNicknameAck();

                        ack.userName = UserName;
                        ack.ResultType = (short)EServerMessageType.Success;
                        string text = response.SerealizeStructToJson<PacketSetNicknameAck>(ack);
                        response.Push(text);
                        Send(response);
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
