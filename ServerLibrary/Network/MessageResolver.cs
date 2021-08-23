using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerLibrary.Network
{
    /// <summary>
    /// [header][body] 구조를 갖는 데이터를 파싱하는 클래스.
    /// - header : 데이터 사이즈. Defines.HEADERSIZE에 정의된 타입만큼의 크기를 갖는다.
    ///				2바이트일 경우 Int16, 4바이트는 Int32로 처리하면 된다.
    ///				본문의 크기가 Int16.Max값을 넘지 않는다면 2바이트로 처리하는것이 좋을것 같다.
    /// - body : 메시지 본문.
    /// </summary>
    public delegate void CompleteMessageCallback(ArraySegment<byte> buffer);
    class MessageResolver
    {
        //메시지 사이즈
        int mMessageSize;
        //진행중인 버퍼
        byte[] mMessageBuffer = new byte[1024];

        //현재 진행중인 버퍼의 인덱스를 가리키는 변수
        //패킷 하나를 완성한 뒤에는 0으로 초기화 시켜줘야 한다.
        int mCurrentPosition;

        //읽어와야 할 목표 위치
        int mPositionToRead;
        //남은 사이즈
        int mRemainBytes;

        public MessageResolver()
        {
            mMessageSize = 0;
            mCurrentPosition = 0;
            mPositionToRead = 0;
            mRemainBytes = 0;
        }

        bool ReadUntil(byte[] buffer, ref int srcPosition)
        {

            //읽어와야 할 바이트
            //데이터가 분리되어 올 경우 이전에 읽어놓은 값을 빼줘서 부족한 만큼 읽어올 수 있도록 계산
            int copySize = mPositionToRead - mCurrentPosition;

            //남은 데이터가 더 적다면 가능한 만큼만 복사한다
            if(mRemainBytes < copySize)
            {
                copySize = mRemainBytes;
            }

            //버퍼에 복사
            Array.Copy(buffer, srcPosition, mMessageBuffer, mCurrentPosition, copySize);

            //원본 버퍼 포지션 읻동
            srcPosition += copySize;

            //타겟 버퍼 포지션도 이동
            mCurrentPosition += copySize;

            //남은 바이트 수
            mRemainBytes -= copySize;

            //목표지점에 도달 못했으면 false
            if(mCurrentPosition < mPositionToRead)
            {
                return false;
            }
            return true;
        }

        public void OnReceive(byte[] buffer, int offset, int transffered, CompleteMessageCallback callback)
        {
            //이번 receive로 읽어오게 될 바이트 수
            mRemainBytes = transffered;
            //원본 버퍼의 포지션 값
            //패킷이 여러개 뭉ㅇ쳐 올 경우 원본 버퍼의 포지션은 계속 앞으로 가야 하는데 그 처리를 위한 변수이다
            int srcPosition = offset;
            //남은 데이터가 있다면 계속 반복한다.
           
            while (mRemainBytes > 0)
            {
                bool completed = false;
                //헤더만큼 못읽은 경우 헤더를 먼저 읽는다.
                if (mCurrentPosition < Defines.HEADERSIZE)
                {
                    //목표 지점 설정(헤더 위치까지 도달하도록 설정)
                    mPositionToRead = Defines.HEADERSIZE;

                    completed = ReadUntil(buffer, ref srcPosition);
                    if(!completed)
                    {
                        // 아직 다 못읽었으므로 다음 receive를 기다림
                        return;
                    }

                    //헤더 하나를 온전히 읽어왓으므로 메시지 사이즈를 구한다.
                    mMessageSize = GetTotalMeessageSize();

                    //메시지 사이즈가 0이하라면 잘못된 패킷으로 처리
                    if (mMessageSize <= 0)
                    {
                        ClearBuffer();
                        return;
                    }

                    //다음 목표 지점
                    mPositionToRead = mMessageSize;

                    if (mRemainBytes <= 0)
                    {
                        return;
                    }
                }
                //메시지를 읽는다.
                completed = ReadUntil(buffer, ref srcPosition);
                if (completed)
                {
                    //패킷 하나 완성
                    byte[] clone = new byte[mPositionToRead];
                    Array.Copy(mMessageBuffer, clone, mPositionToRead);
                    ClearBuffer();
                    callback(new ArraySegment<byte>(clone, 0, mPositionToRead));
                }


            }

            
        }

        int GetTotalMeessageSize()
        {
            //헤더에서 메시지 사이즈를 구한다
            //헤더타입은 int16, int32 두가지가 올 수 있으므로 각각을 구분하여 처리(실제로는 바뀔 일은 없음)
            if (Defines.HEADERSIZE == 2)
            {
                return BitConverter.ToInt16(mMessageBuffer, 0);
            }
            else if(Defines.HEADERSIZE == 4)
            {
                return BitConverter.ToInt32(mMessageBuffer, 0);
            }

            return 0;
        }
        public void ClearBuffer()
        {
            Array.Clear(mMessageBuffer, 0, mMessageBuffer.Length);

            mCurrentPosition = 0;
            mMessageSize = 0;
        }
    }
}
