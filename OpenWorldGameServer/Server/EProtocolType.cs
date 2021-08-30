using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenWorldGameServer.Server
{
    public enum EProtocoleType : short
    {
		Begin = 0,
		ChatMsgReq = 1,
		ChatMsgAck,
		ConnectReq,
		ConnectAck,
		NewClient,
		PlayerMoveReq,
		PlayerMoveAck,
		CharacterMove,
		SetNicknameReq,
		SetNicknameAck,
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
