using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenWorldServer.Network
{
    public interface IMessageDispatcher
    {
        void OnMessage(UserToken user, ArraySegment<byte> buffer);
    }
}
