using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleClient
{
    public enum EProtocoleType : short
    {
        Begin,
        ChatMsgReq = 1,
        ChatMsgAck,
        End
    }
}
