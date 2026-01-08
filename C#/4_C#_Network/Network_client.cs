#if false // 1 명과만의 통신할 때 client 코드
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient();
            client.Connect("localhost", 1234); // 서버에 연결 요청
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);
            writer.WriteLine("hello");
            writer.Flush();

            string str = reader.ReadLine();
            Console.WriteLine("서버 메세지" + str);
            client.Close();
        }
    }
}
#endif

#if false // 동시에 여러개의 실행창을 활용하여 여러 클라이언트를 생성하고 통신, 클라이언트당 메시지 하나만 보냄
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    internal class Program
    {
        static void Main(string[] args)
        {
            TcpClient client = new TcpClient();
            client.Connect("localhost", 1234); // 서버에 연결 요청
            NetworkStream stream = client.GetStream();
            StreamReader reader = new StreamReader(stream);
            StreamWriter writer = new StreamWriter(stream);
            Console.WriteLine("이름 입력");
            string name = Console.ReadLine();
            Console.WriteLine("메시지 입력");
            string message = Console.ReadLine();
            
            writer.WriteLine(name + ": " + message);
            writer.Flush();

            string str = reader.ReadLine();
            Console.WriteLine("서버 메세지" + str);
            client.Close();
        }
    }
}
#endif

#if true // 동시에 여러개의 실행창을 활용하여 여러 클라이언트를 생성하고 통신, 클라이언트 한명이 여러 메세지를 보내는 경우
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Client
{
    //서버에 메시지 작성, 읽기 기능 구현
    class Client
    {
        private TcpClient client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;

        public Client(TcpClient c)
        {
            this.client = c;
            stream = c.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
        }

        //서버가 전송한 메시지 읽기 함수
        public void read()
        {
            string msg;
            while (true)
            {
                msg = reader.ReadLine();
                if (msg.StartsWith("/exit"))
                {
                    break;
                }
                else
                {
                    Console.WriteLine(msg);
                }

            }
            client.Close();
            Console.WriteLine("읽기 쓰레드 종료");
        }

        //서버에 메시지 전송 함수
        public void write()
        {
            string msg;
            Console.WriteLine("id:");
            while (true)
            {
                msg = Console.ReadLine();
                writer.WriteLine(msg);
                writer.Flush();
                if (msg.StartsWith("/exit"))
                {
                    break;
                }
            }
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            TcpClient c = new TcpClient();
            c.Connect("localhost", 4567);
            Client client = new Client(c);
            new Thread(new ThreadStart(client.read)).Start();
            new Thread(new ThreadStart(client.write)).Start();
        }
    }
}
#endif