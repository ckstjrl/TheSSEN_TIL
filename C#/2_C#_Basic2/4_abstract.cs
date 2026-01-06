using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace day02
{
    abstract class father
    {
        // 구현하지 않는 메서드를 추상 메서드라 한다.
        // 메서드 선언문 abstract 키워드를 붙여줘야 한다.
        // 현재 클래스도 abstract가 되어야 함
        // 추상 메서드를 하나라도 포함하면 클래스에 abstract를 붙여야함
        // 그래서 객체를 생성하면 에러가 남 -> 추상인데 어떻게 만들어??
        public abstract void f1(); 
        public abstract void f2();
        public void f5()
        {
            Console.WriteLine("구현 메세지"); 
        }
    }

    class son: father
    {

        // f1 하나만 구현하면 abstract를 하던, 객체를 사용하고 싶은 것은 다 상속받아서 구혐
        public override void f1()
        {
            Console.WriteLine("자식에서 구현한 f1()");
            throw new NotImplementedException();
        }
        public override void f2()
        {

            Console.WriteLine("자식에서 구현한 f1(w)");
            throw new NotImplementedException();
        }
    }
}
