using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace ServerLibrary.Network
{
    /// <summary>
    /// 서버와 클라이언트에서 공통으로 사용하는 세션객체
    /// 서버 : 하나의 클라이언트 객체를 나타냄
    ///         이 인터페이스를 구현한 객체를 NetworkService 클래스의 SessionCreatedCallback 호출 시 생성하여 리턴
    ///         객체를 풀링할지 여부는 사용자 재량
    /// 클라이언트 : 
    ///             접속한 서버객체를 나타냄
    /// </summary>
    public interface IPeer
    {
        /// <summary>
        /// NetworkService.Initialize에서 UseLogicThread를 true로 설정할 경우
        /// -> IO스레드에서 직접 호출됨
        /// false로 설정할 경우
        /// -> 로직 스레드에서 호출됨. 로직 스레드는 싱글 스레드로 돌아감.
        /// </summary>
        /// <param name="msg"></param>
        void OnMessage(PacketBase msg);
        /// <summary>
        /// 원격 연결이 끊겼을 때 호출
        /// 이 메서드가 호출된 이후부터는 데이터 전송 불가
        /// </summary>
        void OnRemoved();

        void Disconnect();
        void Send(PacketBase msg);

        //void ProcessUserOperation(PacketBase msg);
    }
}
