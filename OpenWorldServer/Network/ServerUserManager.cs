using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OpenWorldServer.Network
{
    /// <summary>
    /// 현재 접속중인 전체 유저를 관리하는 클래스
    /// </summary>
    public class ServerUserManager
    {
        object mCSUser;
        List<UserToken> mUsers;

        Timer mTimer;
        long mHeartbeatDuration;

        public ServerUserManager()
        {
            mCSUser = new object();
            mUsers = new List<UserToken>();
        }

        public void StartHeartbeatChecking(uint checkIntervalSec, uint allowDurationSec)
        {
            mHeartbeatDuration = allowDurationSec * 10000000;
            mTimer = new Timer(CheckHeartbeat, null, 1000 * checkIntervalSec, 1000 * checkIntervalSec);
        }

        public void StopChecking()
        {
            mTimer.Dispose();
        }

        public void AddUser(UserToken user)
        {
            lock(mCSUser)
            {
                mUsers.Add(user);
            }
        }

        public void Remove(UserToken user)
        {
            lock(mCSUser)
            {
                mUsers.Remove(user);
            }
        }

        public bool IsExist(UserToken user)
        {
            lock(mCSUser)
            {
                return mUsers.Exists(obj => obj == user);
            }
        }

        public int GetTotalCount()
        {
            return mUsers.Count;
        }

        public void CheckHeartbeat(object state)
        {
            long allowTime = DateTime.Now.Ticks - mHeartbeatDuration;

            lock(mCSUser)
            {
                for(int i = 0; i < mUsers.Count; ++i)
                {
                    long heartbeatTime = mUsers[i].LastestHeartbeatTime;
                    if(heartbeatTime >= allowTime)
                    {
                        continue;
                    }
                    mUsers[i].Disconnect();
                }
            }
        }
    }
}
