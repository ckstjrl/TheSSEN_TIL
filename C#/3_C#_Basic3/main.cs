#if false // 1
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            List<string> list = new List<string>();
            list.Add("aaa");
            list.Add("bbb");
            list.Add("ccc");
            //list.Add('dd'); -> string 자료형에 char가 들어갈 수 없음
            foreach(string item in list)
            {
                Console.WriteLine(item);
            }
            if (list.Contains("ccc")) // 파라미터로 넣은 요소가 있는지 판단
            {
                Console.WriteLine("ccc 있음");
                Console.WriteLine(list.IndexOf("ccc") + " 번째에 있음");
            }
            else
            {
                Console.WriteLine("ccc 없음");
            }
            Console.WriteLine("요소 개수: " + list.Count);
            list.Remove("ccc"); // ccc 값을 찾아서 삭제
            list.RemoveAt(0); // 0 번째 값을 삭제
            Console.WriteLine("삭제 후 요소 개수: " + list.Count);
            foreach(string item in list)
            {
                Console.WriteLine(item);
            }
        }
    }
}

#endif

#if false // 2 dictionary
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 키(string), 값(string) 딕셔너리 생성
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("name", "aaa");
            dic["tel"] = "111";

            // 출력 1. key, val 따로 출력 
            foreach( string key in dic.Keys)
            {
                Console.WriteLine("key: " + key);
                Console.WriteLine("val: " + dic[key]);
            }

            // 출력 2. val값 출력
            foreach(string val in dic.Values)
            {
                Console.WriteLine(val);
            }
            // 출력 3. 쌍으로 출력
            foreach(KeyValuePair<string,string> pair in dic)
            {
                Console.WriteLine(pair);
            }

            // 수정
            dic["tel"] = "1234";
            Console.WriteLine("tel: " + dic["tel"]);

            // 삭제 
            dic.Remove("name"); 
            foreach (KeyValuePair<string, string> pair in dic)
            {
                Console.WriteLine(pair);
            }

        }
    }
}

#endif

#if false // 3 파일 입출력
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {

            // bin 폴더에 a.txt 파일이 있다.
            StreamWriter writer = new StreamWriter("a.txt");
            writer.WriteLine("가나다라");
            writer.WriteLine("abcdef");
            writer.WriteLine("123456");
            writer.Close();

            StreamReader reader = new StreamReader("a.txt");
            string s;
            while((s=reader.ReadLine()) != null)
            {
                Console.WriteLine(s);
            }
            reader.Close();
        }
    }
}

#endif

#if false // 4 파일 출력
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 한 번에 다 읽어오기 (반복문 안 쓰고)
            string content = File.ReadAllText("a.txt");
            Console.WriteLine(content);
        }
    }
}

#endif

#if false // 5 (class 1) 직렬화
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 객체 단위로 파일에 읽고 쓰기
            List<person> list = new List<person>();
            list.Add(new person("aaa", 12));
            list.Add(new person("bbb", 13));
            list.Add(new person("ccc", 14));
            // 나열하는 것보다 object(상위 객체)를 만들어서 객체 생성을 하자

            // 파일을 쓰기 모드로 오픈.
             Stream s= File.OpenWrite("persons.dat"); // .dat => 바이너리 파일
            BinaryFormatter bf = new BinaryFormatter();
        
            foreach(person p in list)
            {
                bf.Serialize(s, p);
            }
            s.Close(); // 항상 닫는 거 잊지말자

            // 파일을 읽기 모드로 오픈
            s = File.OpenRead("persons.dat");
            person p2 = null;
            try
            {
                // 역직렬화를 해서 하나씩 읽음
                // 객체 형태가 어떤 형태인지 모르기 때문에 상위 객체 object로 읽어드림
                // 그러면 다운 캐스팅을 하면 됨
                // 파일의 마지막을 가리키면 -1을 리턴을 함. 이때 catch문으로 예외가 발생했다라고 뜸.
                    // [구문 분석을 완료하기 전에 스트림 끝에 도달했습니다.]
                // 정상적으로 읽음. 프로그램 동작에는 문제가 없다고 하심.
                //
                // [입력 스트림이 올바른 바이러리 형식이 아닙니다] 예외
                // persons.dat 파일 더러워서 예외 구문이 안 나오는 거였음
                // 근데 persons 파일 만들어 놓은 게 없었는데 다른 예외가 뜰 수가 있네

                while ((p2 = (person)bf.Deserialize(s)) != null)
                {
                    Console.WriteLine(p2); 
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            s.Close();
        }
    }
}

#endif

#if false // 6 디렉토리 제어
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            // 디렉토리 제어
            DirectoryInfo di = new DirectoryInfo("./"); // 현재 디렉토리 정보
            FileInfo[] fi = di.GetFiles("*.*"); // 파일 한개에 대한 정보( 전체 파일)
            foreach(FileInfo f2 in fi)
            {
                Console.WriteLine(f2.Name); // 파일명만 출력
            }

            // 없는 경로로 DirectoryInfo 객체 생성 가능
            DirectoryInfo di2 = new DirectoryInfo("./meno");
            if (di2.Exists)
            {
                Console.WriteLine("memo 디렉토리가 이미 있다.");
            }
            else
            {
                Console.WriteLine("memo 디렉토리 생성");
                di2.Create();
            }
        }
    }
}

#endif


#if false // 7 실습. (class 2)
// 메모장 현재 디렉토리에 memo 디렉토리 있는지 확인하여 없으면 생성, 있으면 그것을 사용
// 1. 읽기
// memo 디렉토리의 메모 파일 목록을 출력 -> 파일 선택 -> 파일 내용 화면 출력
// 2. 쓰기
// 파일명 입력 -> 중복되면(이름을 다시 입력, 이어쓰기, 덮어쓰기) -> 파일 내용 일력(여러 줄) -> 저장
// 3. 삭제
// memo 디렉토리의 메모파일 목록을 출력 -> 파일 선택 -> 삭제
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void Main(string[] args)
        {
            memo m = new memo("./memo");
            m.delete();
            m.write();
            m.read();
        }
    }
}

#endif


#if false // 8 delegate 교재 3 시작
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        //대리자
        //함수 포인터 비슷한 얘임
        //C#에는 포인터가 없기 때문에 그 역할을 포인터처럼 함수의 주솟값을 받아옴
        public delegate void mydelegate(string s);
        static void test1(string s)
        {
            Console.WriteLine("test1: " + s);
        }

        static void test2(string s)
        {
            Console.WriteLine("test2: " + s);
        }
        static void Main(string[] args)
        {
            mydelegate md1 = new mydelegate(test1);
            md1("msg");

            mydelegate md2 = new mydelegate(test2);
            md2("msg");

            mydelegate md3 = md1 + md2;
            md3("asd"); // test1 + test2 => 두 개 함수를 호출, 매개변수는 asd
            // 이벤트 핸들러 등록같이
            // 저게 언제 발생할지 모르지만 이 이벤트가 발생하면 이 함수를 호출하도록 하겠따.
            // 호출되도록 설정을 해줘야 함
            // 이러한 delegate는 합성이 가능함 (test1 + test2)
        }
    }
}

#endif

#if false // 9 delegate 교재 3, p.3
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class client
    {
        private int id;
        public delegate void clientService(object sender, EventArgs args);
        public clientService service;

        public client(int id)
        {
            this.id = id;
        }
        public int ID
        {
            get { return id; }
        }
        public void fireEvent()
        {
            if(service != null)
            {
                EventArgs args = new EventArgs();
                service(this, args);
            }
        }
    }
    internal class Program
    {
        public static void OnEvent(object sender, EventArgs args)
        {
            client c = (client)sender;
            Console.WriteLine("이벤트 당첨번호:{0}", c.ID);
        }
        static void Main(string[] args)
        {
            client a = new client(100);
            a.service = new client.clientService(OnEvent);
            a.fireEvent();

            client b = new client(200);
            b.service = new client.clientService(OnEvent);
            b.fireEvent();
        }
    }
}

#endif


#if false // 10 스레드
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        public static void test1()
        {
            for(int i=0; i<30; i++)
            {
                // Thread.CurrentThread 현재 실행중인 스레드
                // 스레드는 이름을 안 지어주면 이름이 없음
                Console.WriteLine(Thread.CurrentThread.Name + " " + i);
                Thread.Sleep(500);
            }
        }
        static void test2(object x)
        {
            int num = (int)x;
            for(int i=1; i<=num; i++)
            {
                Console.WriteLine(Thread.CurrentThread.Name + " " + i);
                Thread.Sleep(500);
            }
        }
        static void Main(string[] args)
        {
            // RTOS: 커널이 선점형으로 만들어야 함
            // 더 높은 우선순위의 일이 발생하면 현재 하고 있는 일을 멈춤
            // 이벤트가 발생하면 바로 처리할 수 있는 OS
            // 우주 항공, 자동화 시스템

            // window: 비선점형
            ////////////////////////
            ///
            // 쓰레드 상태
            //ready
            //run
            //wait(sleep, wait)
            //terminate

            // 스레드가 교체될 때, 남은 정보만 교체
            // 교체되는 오버헤드 줄어듦..
            // C# -> 멀티태스킹 기반
            // wait-> 현재 상황이 특정 작업을 하는데 실행될 상황이 아니다.. 라고 판단하면 대기 상태로 빠짐
            // 다른 스레드들이 얘 깨워야할 것 같은데라고 하면 깨움

            // 스레드 2개 교체되면서(번갈아가면서는 아님) 작업을 수행
            // 동시성을 제공해주는 코드
            // ThreadStart: 쓰레드에서 실행할 함수를 대신 호출해줄 delegate
            Thread t1 = new Thread(new ThreadStart(test1));
            t1.Name = "th1"; // 스레드 이름 설정
            t1.Start();

            Thread t2 = new Thread(new ThreadStart(test1));
            t2.Name = "th2"; // 스레드 이름 설정
            t2.Start();


            // main 스레드에 이름 설정
            Thread.CurrentThread.Name = "main";
            for(char i = 'a'; i<='z'; i++)
            {
                Console.WriteLine(Thread.CurrentThread.Name + ": " + i);
                Thread.Sleep(500);
            }

            // 스레드 루틴을 익명함수로 정의
            Thread t3 = new Thread(() =>
            {
                for (char i = 'A'; i <= 'Z'; i++)
                {
                    Console.WriteLine(Thread.CurrentThread.Name + ": " + i);
                    Thread.Sleep(500);
                }
            });
            t3.Name = "th3"; // 스레드 이름 설정
            t3.Start(); // 늦게 실행이 됨 -> 스레드가 없나

            // ParameterizedThreadStart는 object 방식으로 보낸다..?
            // Start(10) -> 10을 object 방식으로 보냄
            Thread t4 = new Thread(new ParameterizedThreadStart(test2));
            t4.Name = "th4"; // 스레드 이름 설정
            t4.Start(10);

        }
    }
}

#endif


#if false // 11 스레드 실습
// 키보드 입력받아서 화면에 바로 출력
// 파일에서 한줄씩 읽어서 출력(1초에 한 번)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class Program
    {
        static void input()
        {
            string ch = Console.ReadLine();
            Console.WriteLine(ch);
            Thread.Sleep(500);
        }
        static void readFile(object o)
        {
            string path = (string)o;
            StreamReader reader = new StreamReader(path);
            string line;
            while((line =reader.ReadLine() ) != null)
            {
                Console.WriteLine(line);
                Thread.Sleep(500);
            }            

        }
        static void Main(string[] args)
        {
            //Thread t1 = new Thread(input);
            //t1.Start();

            string path = "a.txt";
            Thread t2 = new Thread(new ParameterizedThreadStart(readFile));
            t2.Start(path);

            string str;
            while (true)
            {
                str = Console.ReadLine();
                if (str.StartsWith("/stop")){
                    break;
                }
                Console.WriteLine();
            }
        }
    }
}

#endif

#if false // 12 스레드 기본 사용
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Num
    {
        public int num;
    }
    internal class Program
    {
        static void test(object n)
        {
            for(int i=0; i<30; i++)
            {
                ((Num)n).num = i;
                Thread.Sleep(500);
                Console.WriteLine(Thread.CurrentThread.Name + ": " + ((Num)n).num);
            }
        }
        static void Main(string[] args)
        {
            Num m = new Num();
            Thread th = new Thread(new ParameterizedThreadStart(test));
            th.Name = "th";
            th.Start(m);

            Thread.CurrentThread.Name = "main";
            for(int i=0; i<30; i++)
            {
                m.num = i;
                Thread.Sleep(500);
                Console.WriteLine(Thread.CurrentThread.Name + ": " + m.num);
            }
        }
    }
}

#endif


#if false // 13 스레드 락 + 동기화
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Num
    {
        public int num;
    }
    internal class Program
        // 속도가 느려짐 이제 서로 번갈아가면서 태스크 수행
        // 동기화
    {
        static void test(object n)
        {
            for (int i = 0; i < 30; i++)
            {
                lock (n)
                {
                    ((Num)n).num = i;
                    Thread.Sleep(500);
                    Console.WriteLine(Thread.CurrentThread.Name + ": " + ((Num)n).num);
                }
            }
        }
        static void Main(string[] args)
        {
            Num m = new Num();
            Thread th = new Thread(new ParameterizedThreadStart(test));
            th.Name = "th";
            th.Start(m);

            Thread.CurrentThread.Name = "main";
            for (int i = 0; i < 30; i++)
            {
                lock (m)
                {
                    m.num = i;
                    Thread.Sleep(500);
                    Console.WriteLine(Thread.CurrentThread.Name + ": " + m.num);
                }
            }
        }
    }
}

#endif



#if false // 14 스레드 wait
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Num
    {
        public int num;
    }
    internal class Program
    {
        static void add(object n)
        {
            Num num = (Num)n;
            while (true)
            {
                lock (num)
                {
                    if(num.num >= 40) // 증가를 할 상황이 아니면 현재 스레드는 wait으로 들어간다.
                    {
                        //Monitor.PulseAll(n); -> 다른 스레드를 깨우고 (대기 큐에 있는 애들을)
                        //Monitor.Wait(n); -> 본인은 wait 상태에 들어간다.
                        // lock(화장실 문을 잠구고) -> wait을 하면 자동으로 lock을 풀어줌(다른 스레드들 화장실 이용을 위해)

                        Monitor.PulseAll(num); // 공유자원에 있는 애들을 다 깨움
                        Monitor.Wait(num); // 자기자신은 wait 상태로 감
                    }
                    num.num++;
                    Console.WriteLine(Thread.CurrentThread.Name + ' ' + num.num);
                    Thread.Sleep(200);
                }
            }
        }

        static void sub(object n)
        {
            Num num = (Num)n;
            while (true)
            {
                lock (num)
                {
                    if (num.num <= 20) // 증가를 할 상황이 아니면 현재 스레드는 wait으로 들어간다.
                    {
                        Monitor.PulseAll(num); // 공유자원에 있는 애들을 다 깨움
                        Monitor.Wait(num); // 자기자신은 wait 상태로 감
                    }
                    num.num--;
                    Console.WriteLine(Thread.CurrentThread.Name + ' ' + num.num);
                    Thread.Sleep(200);
                }
            }
        }
        static void Main(string[] args)
        {
            Num n = new Num();
            Thread th = new Thread(new ParameterizedThreadStart(add));
            th.Name = "add_th";
            th.Start(n);

            Thread th2 = new Thread(new ParameterizedThreadStart(sub));
            th2.Name = "sub_th";
            th2.Start(n);
        }
    }
}

#endif

#if true // 15 스레드 실습 생산자외 소비자
// int[] arr = new int[10];
// 생산자 th = 1조에 숫자 한 개 생성
// 배열이 차면 wait -> 3개 소비되면 깨어남

// 소비자 스레드 2개 (th1, th2)
// th1: 2초마다 한 개 소비
// th2: 엔터치면 소비 -> 엔터 빨리치면 배열이 비겠지 -> 그러면 소비자 중단(wait) -> 생산 3개되면 깨어남

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    // 생산자, 소비자 쓰레드들의 공유자원
    class product
    {
        public int[] arr = new int[10];
        public int pIdx = 0; // 생산자 인덱스
        public int cIdx = 0; // 소비자 인덱스
        public int cnt = 0; // 배열에 담긴 데이터 수 (10개 넘으면 안됨)
        public int num = 1; // 시리얼 넘버(데이터)
        public bool flag = false; // 소비자 상태 false(소비자 활동) / true(소비자멈춤)
    }

   //생산자 스레드, 소비자 스레드에서 사용할 함수 정의 클래스
    class Work
    {
        public void productor(object o)
        {
            product p = (product)o;
            while (true)
            {
                lock (p)
                {
                    // 소비자쪽 상태 확인
                    if(p.cnt >= 3) // 소비자가 깨어날 조건
                    {
                        p.flag = false; // 소비자 활동 상태로 설정
                        Console.WriteLine("소비자 깨어남");
                        Monitor.PulseAll(p); // 소비자를 깨워줌
                        //Monitor.Wait(p);
                    }
                    // 생산자쪽 상태 확인
                    if(p.cnt >= 10)
                    {
                        Console.WriteLine("생산자 대기로 들어감");
                        Monitor.Wait(p); // 현재 스레드 대기 상태로 들어감
                    }
                    else // 생산할 수 있는 조건 -> 생산 가능
                    {
                        p.arr[p.pIdx] = p.num++;
                        Console.WriteLine("생산자 현재 들어간 값 확인: " + p.arr[p.pIdx]);
                        p.pIdx++;
                        p.cnt++;
                        p.pIdx %= 10; // 원형큐처럼 맨 끝까지 가면 앞에서 다시 채움
                    }
                }
                Thread.Sleep(1000);
            }
        }
        public void customer1(object o) // 2초에 하나 소비
        {
            product p = (product)o;
            while (true)
            {
                lock (p)
                {
                    // 생산자 깨울 조건 체크
                    if(p.cnt <= 7)
                    {
                        Console.WriteLine("생산자 깨어남");
                        Monitor.Pulse(p);

                    }
                    // 소비자 활동 상황 체크
                    if(p.cnt == 0|| p.flag)
                    {
                        Console.WriteLine("배열이 빔. 소비자 대기 상태 들어감");
                        if(!p.flag) p.flag = true;
                        Monitor.Wait(p);
                    }
                    else
                    {
                        Console.WriteLine("소비자 값: " + p.arr[p.cIdx++]);
                        p.cnt--;
                        p.cIdx %= 10;
                    }
                }
                Thread.Sleep(2000);
            }
        }
        public void customer2(object o)
        {
            product p = (product)o;
            while (true)
            {
                Console.ReadLine();
                lock (p)
                {
                    // 생산자 깨울 조건 체크
                    if (p.cnt <= 7)
                    {
                        Console.WriteLine("생산자2 깨어남");
                        Monitor.Pulse(p);

                    }

                    // 소비자 활동 상황 체크
                    if (p.cnt == 0 || p.flag)
                    {
                        Console.WriteLine("배열이 빔. 소비자 대기 상태 들어감");
                        if (!p.flag) p.flag = true;
                        Monitor.Wait(p);
                    }
                    else
                    {
                        Console.WriteLine("소비자2 값: " + p.arr[p.cIdx++]);
                        p.cnt--;
                        p.cIdx %= 10;
                    }
                }
            }
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            product p = new product();
            Work w = new Work();
            Thread th1 = new Thread(new ParameterizedThreadStart(w.productor));
            th1.Start(p);
            Thread th2 = new Thread(new ParameterizedThreadStart(w.customer1));
            th2.Start(p);
            Thread th3 = new Thread(new ParameterizedThreadStart(w.customer2));
            th3.Start(p);
        }
    }
}

#endif