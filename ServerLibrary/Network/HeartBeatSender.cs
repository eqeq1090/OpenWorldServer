using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServerLibrary.Network
{
    class HeartBeatSender
    {
        UserToken mServer;
        Timer mTimer;
        uint mInterval;

        float mElapsedTime;

        public HeartBeatSender(UserToken server, uint interval)
        {
            mServer = server;
            mInterval = interval;
            mTimer = new Timer(OnTimer, null, Timeout.Infinite, mInterval * 1000);
        }
        void OnTimer(object state)
        {
            Send();
        }

        void Send()
        {
            PacketBase msg = PacketBase.Create((short)UserToken.SYS_UPDATE_HEARTBEAT);
            mServer.Send(msg);
        }

        public void Update(float time)
        {
            mElapsedTime += time;
            if(mElapsedTime < mInterval)
            {
                return;
            }
            mElapsedTime = 0.0f;
            Send();

        }

        public void Stop()
        {
            mElapsedTime = 0;
            mTimer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public void Play()
        {
            mElapsedTime = 0;
            mTimer.Change(0, mInterval * 1000);
        }
    }
}
