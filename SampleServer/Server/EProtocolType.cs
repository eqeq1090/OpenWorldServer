using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleServer.Server
{
    public enum EProtocoleType : short
    {
        Begin,
        ChatMsgReq = 1,
        ChatMsgAck,
        ConnectReq,
        ConnectAck,
        NewClient,
        PlayerMove,
        CharacterMove,
        SetNicknameAck,
        SetNicknameReq,
        End
    }

    public enum EServerMessageType : short
    {
        Begin,
        Success,
        Error,
        End
    }

}
