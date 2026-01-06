using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace day02
{
    interface ITest // 추상 메서드로만 구성됨. 완전 추상 클래스
    {
        void f1(); // 추상 메서드
        void f2();

    }
    // 아무것도 작성 안된 상태에는 추상 메서드 구현하라고 빨간 줄 생김
    class ITestImpl : ITest
    {
        public void f1()
        {
            Console.WriteLine("구현 클래스에서 추상 메서드 구현 f1()");
        }
        public void f2()
        {
            Console.WriteLine("구현 클래스에서 추상 메서드 구현 f2()");
        }
    }
}
