using ServerLibrary.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleClient.Client
{
    class RemoteServerPeer : IPeer
    {
        public UserToken mToken { get; private set; }
        public RemoteServerPeer(UserToken token)
        {
            mToken = token;
            mToken.SetPeer(this);
        }
        int mRecieveCount = 0;

        void IPeer.OnMessage(PacketBase msg)
        {
            System.Threading.Interlocked.Increment(ref mRecieveCount);

            EProtocoleType protocolType = (EProtocoleType)msg.PopProtocolType();
            switch (protocolType)
            {
                case EProtocoleType.ChatMsgAck:
                    {
                        string text = msg.PopString();
                        Console.WriteLine(string.Format("text {0}", text));
                    }
                    break;
            }

        }
        void IPeer.OnRemoved()
        {
            Console.WriteLine("Server reemoved");
            Console.WriteLine("receive count " + mRecieveCount);
        }
        void IPeer.Send(PacketBase msg)
        {
            msg.RecordSize();
            mToken.Send(new ArraySegment<byte>(msg.Buffer, 0, msg.Position));
        }
        void IPeer.Disconnect()
        {
            mToken.Disconnect();
        }
    }
}
