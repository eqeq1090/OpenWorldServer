using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerLibrary.Network;

namespace ServerLibrary.Network
{
    interface ILogicQueue
    {
        void Enqueue(PacketBase msg);
        Queue<PacketBase> GetAll();
    }
}
