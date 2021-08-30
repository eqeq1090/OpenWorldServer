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

                        this.Position.X = data.X;
                        this.Position.Y = data.Y;
                        this.Position.Z = data.Z;

                        this.Rotation.Roll = data.Roll;
                        this.Rotation.Yaw = data.Yaw;
                        this.Rotation.Pitch = data.Pitch;

                        PacketBase update = PacketBase.Create((short)EProtocoleType.CharacterMove);
                        update.PushStruct(send);

                        //Connect 처리 되어 필드 정보를 갖고 있는 유저들만 보냄
                        List<GameUser> sendList = (from user in Program.UserList
                                                   where user.Connected = true && user.UserIndex != this.UserIndex
                                                   select user).ToList();
                        foreach(GameUser user in sendList)
                        {
                            user.Send(update);
                        }
                    }
                    break;
                case EProtocoleType.ConnectReq:
                    {

                        //구조체 json 직렬화
                        string json = msg.PopString();
                        PacketSetNicknameReq data = msg.DeserializeJsonToStruct<PacketSetNicknameReq>(json);//JsonSerializer.Deserialize<PacketSetNicknameReq>(readOnlySpan);//msg.DeserializeJsonToStruct<PacketSetNicknameReq>(json);
                        UserName = data.userName;

                        //접속 성공여부 및 닉네임, 필드정보 전송
                        PacketBase response = PacketBase.Create((short)EProtocoleType.ConnectAck);
                        PacketConnectAck ack = new PacketConnectAck();

                        List<GameUser> sendList = (from user in Program.UserList
                                                   where user.Connected = true && user.UserIndex != this.UserIndex
                                                   select user).ToList();

                        ack.MyName = UserName;
                        ack.ResultType = (short)EServerMessageType.Success;
                        ack.UserList = new List<UserData>();
                        foreach(GameUser user in sendList)
                        {
                            UserData userInfo = new UserData();
                            userInfo.Position = user.Position;
                            userInfo.Rotation = user.Rotation;
                            userInfo.UserIndex = user.UserIndex;
                            userInfo.UserName = user.UserName;

                            ack.UserList.Add(userInfo);
                        }
                        
                        string text = response.SerealizeStructToJson<PacketConnectAck>(ack);
                        response.Push(text);
                        Send(response);


                       
                        //새 유저가 접속했다고 알림
                        PacketBase response1 = PacketBase.Create((short)EProtocoleType.NewClient);
                        PacketNewClient newClient = new PacketNewClient();
                        newClient.UserIndex = this.UserIndex;
                        newClient.UserName = this.UserName;

                        foreach(GameUser user in sendList)
                        {
                            user.Send(response1);
                        }
                      
                    }
                    break;
                //case EProtocoleType.ConnectReq:
                //    {

                //    }
                //    break;
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
