using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServerLibrary.Network;
using OpenWorldGameServer.Server;

namespace OpenWorldGameServer
{
    class Program
    {
        public static List<GameUser> UserList;

        public static int UserNum = 0;
        static void Main(string[] args)
        {
            UserList = new List<GameUser>();
            NetworkService service = new NetworkService(true);
            //콜백 메서드 설정
            service.mSessionCreatedCallback += OnSessionCreated;
            //초기화
            service.Initialize(10000, 1024);
            service.Listen("222.107.145.1", 9000, 100);

            service.DisableHeartbeat();

            Console.WriteLine("Server Started");
            while(true)
            {
                string input = Console.ReadLine();
                if(input.Equals("users"))
                {
                    Console.WriteLine(service.mUserManager.GetTotalCount());
                }
                System.Threading.Thread.Sleep(1000);
            }
        }

        static void OnSessionCreated(UserToken token)
        {
            GameUser user = new GameUser(token);
            lock(UserList)
            {
                UserList.Add(user);
            }
        }

        public static void RemoveUser(GameUser user)
        {
            lock(UserList)
            {
                UserList.Remove(user);
            }
        }
        /// <summary>
        /// index 의 유저를 제외한 모든 연결 유저를 반환
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static List<GameUser> GetConnectedUser(int index)
        {
            List<GameUser> sendList = (from user in Program.UserList
                                       where user.Connected = true && user.UserIndex != index
                                       select user).ToList();

            return sendList;
        }
        /// <summary>
        /// 연결된 모든 유저
        /// </summary>
        /// <returns></returns>
        public static List<GameUser> GetConnectedUser()
        {
            List<GameUser> sendList = (from user in Program.UserList
                                       where user.Connected = true
                                       select user).ToList();

            return sendList;
        }
    }
}
