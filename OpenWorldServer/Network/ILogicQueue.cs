using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenWorldServer.Network;

namespace OpenWorldServer.Network
{
    interface ILogicQueue
    {
        void Enqueue(PacketBase msg);
        Queue<PacketBase> GetAll();
    }
}
