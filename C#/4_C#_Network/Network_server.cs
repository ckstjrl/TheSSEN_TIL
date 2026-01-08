#if false // 1 명과 단일 통신
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace dya04
{
    internal class Program
    {
        static void Main(string[] args)
        {
            IPAddress IP = new IPAddress(0);
            TcpListener listener = new TcpListener(IP, 1234);
            listener.Start(); // 서버 소켓 오픈, 클라이언트 요청 받을 준비 됨
            
            Console.WriteLine("서버 시작");

            // AccpetTcpClient(): 클라이언트 요청 대기
            // 요청이 수락되면 클라이언트와 1:1 통신할 소켓 TcpClient 객체를 반환
            TcpClient client = listener.AcceptTcpClient(); // 클라이언트 요청 대기
            
            // 소켓을 읽고 쓰기할 스트림 생성
            // NetworkStream: 네트워크 상에서 읽고 쓰기 할 원시 스트림
            NetworkStream stream = client.GetStream();

            // 원시 스트림을 가공하여 문자 단위로 읽고 쓰기 할 수 있는 2차 스트림 생성
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);

            string str = reader.ReadLine();
            Console.WriteLine("클라이언트 메세지" + str);
            writer.WriteLine(str);
            writer.Flush();
            
            client.Close();
            listener.Stop();
        }
    }
}

#endif

#if false // Thread를 활용한 여려 명과의 통신
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace dya04
{
    internal class Program
    {
        static void f1(object s)
        {
            TcpClient client = (TcpClient)s;
            // 소켓을 읽고 쓰기할 스트림 생성
            // NetworkStream: 네트워크 상에서 읽고 쓰기 할 원시 스트림
            NetworkStream stream = client.GetStream();

            // 원시 스트림을 가공하여 문자 단위로 읽고 쓰기 할 수 있는 2차 스트림 생성
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);

            string str = reader.ReadLine();
            Console.WriteLine("클라이언트 메세지" + str);
            writer.WriteLine(str);
            writer.Flush();

            client.Close();
        }
        static void Main(string[] args)
        {
            IPAddress IP = new IPAddress(0);
            TcpListener listener = new TcpListener(IPAddress.Any, 1234);
            listener.Start(); // 서버 소켓 오픈, 클라이언트 요청 받을 준비 됨

            Console.WriteLine("서버 시작");

            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                Thread th = new Thread(new ParameterizedThreadStart(f1));
                th.Start(client);
            }

            listener.Stop();
        }
    }
}
#endif

#if true // 다중 에코, 한 클라이언트가 여러 메세지 보내는 것.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace dya04
{
    //클라이언트 한명을 담당할 클래스
    class CServer
    {
        private TcpClient client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private string id; //클라이언트가 사용할 아이디

        public CServer(TcpClient client)
        {
            this.client = client;
            this.stream = client.GetStream();
            this.reader = new StreamReader(stream);
            this.writer = new StreamWriter(stream);
        }
        //클라이언트 담당 쓰레드에서 실행할 함수
        public void run()
        {
            id = reader.ReadLine();
            string msg = id + " 님이 입장하셨습니다.";

            lock (Program.clients) //동기화 처리
            {
                //환영 메시지 전체 전송
                foreach (CServer server in Program.clients)
                {
                    server.sendMsg(msg);
                }
            }

            bool flag = true;
            while (flag)
            {
                msg = reader.ReadLine();
                if (msg.StartsWith("/exit"))
                {
                    sendMsg(msg);
                    msg = id + " 님이 나가셨습니다.";
                    flag = false;
                }
                lock (Program.clients)
                {
                    //환영 메시지 전체 전송
                    foreach (CServer server in Program.clients)
                    {
                        if (!flag && server == this)
                            continue;
                        server.sendMsg(id + ": " + msg);
                    }
                }
            }
            client.Close();
            lock (Program.clients)
            {
                Program.clients.Remove(this);//객체를 찾아서 삭제
            }
        }

        //담당 클라이언트 한명에 메시지 전송
        public void sendMsg(string msg)
        {
            writer.WriteLine(msg);
            writer.Flush();
        }

    }
    internal class Program
    {
        //채팅방. 접속한 클라이언트 객체를 저장
        //쓰레드들의 공유자원
        public static List<CServer> clients = new List<CServer>();

        static void Main(string[] args)
        {
            IPAddress addr = new IPAddress(0);
            TcpListener listener = new TcpListener(addr, 4567);
            listener.Start();
            Console.WriteLine("채팅 서버 시작");
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                CServer server = new CServer(client);
                clients.Add(server);
                new Thread(new ThreadStart(server.run)).Start();
            }
            listener.Stop();
        }
    }
}
#endif
