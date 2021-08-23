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
            service.Listen("222.107.110.135", 9000, 100);

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
    }
}
