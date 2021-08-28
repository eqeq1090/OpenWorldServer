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

        public bool Connected;

        public FVector Position;
        public FRotator Rotation;

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
                    }
                    break;
                case EProtocoleType.PlayerMoveReq:
                    {
                        PacketPlayerMove data = msg.DeserializeStruct<PacketPlayerMove>();

                        PacketCharacterMove send;
                        send.X = data.X;
                        send.Y = data.Y;
                        send.Z = data.Z;
                        send.Roll = data.Roll;
                        send.Yaw = data.Yaw;
                        send.Pitch = data.Pitch;
                        send.UserIndex = this.UserIndex;

                        PacketBase update = PacketBase.Create((short)EProtocoleType.CharacterMove);
                        update.PushStruct(send);

                        //Connect 처리 되어 필드 정보를 갖고 있는 유저들만 보냄
                        List<GameUser> sendList = (from user in Program.UserList
                                                   where user.Connected = true
                                                   select user).ToList();
                        foreach(GameUser user in sendList)
                        {
                            user.Send(update);
                        }
                    }
                    break;
                case EProtocoleType.SetNicknameReq:
                    {

                        //구조체 json 직렬화
                        string json = msg.PopString();
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
                case EProtocoleType.ConnectReq:
                    {

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
