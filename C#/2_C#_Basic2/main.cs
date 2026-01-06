#if false
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace day02
{
    internal class Program
    {
        static void Main(string[] args)
        {
            parent p = new parent(1, 2, 3);
            p.print();
            
            child c = new child(5, 6, 7, 8);
            c.print();
            //c.printChild();
            parent p2 = new child(6, 7, 8, 9); // 업캐스팅
            p2.print();

            ((child)p2).print(); // 다운캐스팅
        }
    }
}
#endif

#if false
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
            // 조각과 조각을 이어 붙였다가 뗌 -> 
            car c1 = new car("자동차");
            c1.horn();
            car c2 = new police("경찰차"); // 다운 캐스팅
            c2.horn(); 
            car c3 = new em("엠뷸");
            c3.horn();
        }
    }
}
#endif


#if false
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
            // 업캐스팅 적용 x -> 줄줄이 나열
            //buyer b = new buyer();
            //b.buy(new Tv());
            //b.buy(new computer());

            // 업캐스팅 적용
            product[] prods = { new Tv(), new computer(),
                new Tv(), new computer() };

            buyer b2 = new buyer();
            for(int i=0; i< prods.Length; i++)
            {
                // 타입을 비교하는 연산자 is
                // 두 타입이 같으면 true
                // 다르면 false
                if (prods[i] is Tv)
                {
                    Console.WriteLine("tv");
                }
                else if (prods[i] is computer)
                {
                    Console.WriteLine("computer");
                }
                b2.buy(prods[i]);
            }
        }
    }
}
#endif

#if false // class 3
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
            // 업캐스팅 적용 x -> 줄줄이 나열
            //buyer b = new buyer();
            //b.buy(new Tv());
            //b.buy(new computer());

            // 업캐스팅 적용
            product[] prods = { new Tv(), new computer(),
                new Tv(), new computer() };

            buyer b2 = new buyer();
            for (int i = 0; i < prods.Length; i++)
            {
                // 타입을 비교하는 연산자 is
                // 두 타입이 같으면 true
                // 다르면 false
                if (prods[i] is Tv)
                {
                    Console.WriteLine("tv");
                }
                else if (prods[i] is computer)
                {
                    Console.WriteLine("computer");
                }
                b2.buy(prods[i]);
            }
        }
    }
}
#endif


#if false // class 4
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
            //father f = new father(); // 추상 클래스는 객체 생성을 못한다.
            father f = new son(); // 추상 클래스는 객체 생성을 못한다.

            f.f1();
            f.f2();
        }
    }
}
#endif



#if false // class 5
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
            ITestImpl impl = new ITestImpl();
            impl.f1();
            impl.f2();
            
        }
    }
}
#endif

#if false // class 6
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
            // 코드 결합도가 높으면 -> 의존도가 높은 것임
            // 한 부분을 수정을 하고 싶지만 걔만 뜯어내서 수정할 수가 없음
            // 유지보수에 좋지가 않음
            // 그럼 얘를 어떻게 변신을 하냐? -> new OracleDao() 이 부분
            // DB를 바꾸고 싶다면 Main 함수에서 new 하는 부분만 다른 클래스로 바꿔 끼우면 된다.
            Service service = new Service(new OracleDao());
            service.add();
            service.get();
            service.edit();
            service.del();
        }
    }
}
#endif

#if false // class 7
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
            c tmC = new c();
            tmC.f1();
            tmC.f2();
        }
    }
}
#endif


#if false // class 7
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
            Member m = new Member("aaa", 12); // Member m; -> 4바이트임 // 그러나 new .. (" ", 12)를 하면 
            // string 참조값, int 참조값 -> 8바이트를 할당해줌, 실제 값이 들어가는 게 아니라 -> m자체가 포인터임
            
            Member m2 = new Member("aaa", 12);
            Member m3 = m;

            Console.WriteLine("m의 참조값: " + m.GetHashCode());
            Console.WriteLine("m2의 참조값: " + m2.GetHashCode());
            Console.WriteLine("m3의 참조값: " + m3.GetHashCode());

            Console.WriteLine("m==m2: " + (m == m2));
            Console.WriteLine("m==m3: " + (m == m3));

            Console.WriteLine("m.Equals(m2): " + m.Equals(m2));
            Console.WriteLine("m.Equals(m3): " + m.Equals(m3)); // true -> object 클래스에서도 단순히 주소를 비교하도록 했기 때문에

        }
    }
}
#endif

#if false // class 8 메모리 주소 비교
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
            Member m = new Member("aaa", 12); // Member m; -> 4바이트임 // 그러나 new .. (" ", 12)를 하면 
                                              // string 참조값, int 참조값 -> 8바이트를 할당해줌, 실제 값이 들어가는 게 아니라 -> m자체가 포인터임

            Member m2 = new Member("aaa", 12);
            Member m3 = m;

            Console.WriteLine("m의 참조값: " + m.GetHashCode());
            Console.WriteLine("m2의 참조값: " + m2.GetHashCode());
            Console.WriteLine("m3의 참조값: " + m3.GetHashCode());

            Console.WriteLine("m==m2: " + (m == m2));
            Console.WriteLine("m==m3: " + (m == m3));

            // 위 예제에서는 기본 equals라서 주소값을 비교를 했다.
            // 이때 equals를 재정의 해서 안의 값이 true로 나오는 것을 볼 수가 있음
            Console.WriteLine("m.Equals(m2): " + m.Equals(m2));
            Console.WriteLine("m.Equals(m3): " + m.Equals(m3)); // true -> object 클래스에서도 단순히 주소를 비교하도록 했기 때문에

            Console.WriteLine(m);
            Console.WriteLine(m2);
            Console.WriteLine(m3);

            string s1 = "aaa";
            //string s2 = new string(new char[] {}); // 이게 뭔 문법이냐
            string s2 = (string)s1.Clone(); // 객체를 복사함 -> 속도를 빠르게 하기 위해 눈속임으로 
            // 둘 다 한 공간의 메모리를 가리키는데 한쪽이 값을 바꾸면, 그때 분리가 일어남
            string s3 = "aaa";
            Console.WriteLine(s1);
            Console.WriteLine(s2);
            Console.WriteLine(s3);

            Console.WriteLine("s1.GetHashCode(): " + s1.GetHashCode());
            Console.WriteLine("s2.GetHashCode(): " + s2.GetHashCode());
            Console.WriteLine("s3.GetHashCode(): " + s3.GetHashCode());

            s2 += "bbb"; // 이때 이사를 감
            Console.WriteLine(s2);
            Console.WriteLine("s2.GetHashCode(): " + s2.GetHashCode());

            Console.WriteLine("s1==s2: " + (s1 == s2));
            Console.WriteLine("s1==s2: " + (s1 == s2));

            Console.WriteLine("s1.Equals(s2): " + s1.Equals(s2));
            Console.WriteLine("s1.Equals(s2): " + s1.Equals(s3));

        }
    }
}
#endif

#if false // class 9 exception
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
            Console.WriteLine("프로그램 시작");
            int x = 3, y = 0;
            int[] arr = { x, y };
            string s = null;
            try
            {
                Console.WriteLine("try 블록 안 예외 발생 전");
                //Console.WriteLine("x/y: " + (x / y));
                for(int i=0; i<arr.Length+1; i++)
                {
                    Console.WriteLine(arr[i]);
                }
                s.ToString(); // 객체가 null인데 toString이 되겠냐~~~
                Console.WriteLine("try 블록 안 예외 발생 후");
            }
            catch (IndexOutOfRangeException e) // 동일한 타입이 있어야 함 Exception -> 모든 예외
            // IndexOutOfRangeException -> 안됨
            {
                Console.WriteLine("배열 인덱스 넘어감");
                Console.WriteLine(e.ToString());

            }
            catch (DivideByZeroException e)
            {
                Console.WriteLine("0으로 나눔");
                Console.WriteLine(e.ToString());

            }
            catch (NullReferenceException e)
            {
                Console.WriteLine("널 객체 사용");
                Console.WriteLine(e.ToString());

            }
            finally // 자원 해제 같은 거
            // DB 작업을 하다가 갑자기 종료되면 DB 연결을 끊은 것
            {
                Console.WriteLine("종료하기 전에 무조건 실행되는 블록");
            }
            Console.WriteLine("프로그램 종료");
        }
    }
}
#endif

#if false // class 10 throw
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
            MyNum myNum = new MyNum();
            // myNum.f1(-1); // 프로그램이 강제종료되고 있음 //-> 수정해보자
            try
            {
                myNum.f1(-1); 
            }
            catch(Exception e)
            {
                Console.WriteLine("음수 값을 받음");
                Console.WriteLine(e);
            }
            Console.WriteLine("프로그램 종료");
        }
    }
}
#endif

#if true // class 10 collection
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
            object[] arr = { 1,"aaa", new Point(4,5)}; // int -> object, string -> object, class -> object 업캐스팅 되어 들어감 
            // foreach
            foreach(object obj in arr)
            {
                Console.WriteLine(obj.ToString());
            }
            // 1은 클래스 타입이 아닌데(객체 타입은 아님) 그러나 객체지향언어는 객체로 취급하는 라이브러리를 제공해줌(자동으로 처리)
            // auto boxing, unboxing이라 함
            for(int i=0; i<arr.Length; i++)
            {
                Console.WriteLine(arr[i]);
            }
            // object 멤버에 x,y가 없으므로 오류가 남 -> 다운 캐스팅을 해야 함
            ((Point)arr[2]).x = 1;
            ((Point)arr[2]).y = 2;
            Console.WriteLine(arr[2]);
            // 다운 캐스팅 2
            Console.WriteLine("문자열 길이: " + ((string)arr[1]).Length);
        }
    }
}
#endif
