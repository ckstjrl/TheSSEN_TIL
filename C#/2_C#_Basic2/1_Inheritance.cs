using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace day02
{
    internal class parent
    {
        private int a;
        protected int b;
        public int c;

        public parent()
        {
            Console.WriteLine("부모 생성자 매개변수 없음");
        }
        public parent(int a, int b, int c)
        {
            Console.WriteLine("부모 생성자 매개변수 있음");
            this.a = a;
            this.b = b;
            this.c = c;
        }
        public void print()
        {
            Console.WriteLine("a: " + a + " b: " + b + " c: " + c);
        }
    }
    class child : parent
    {
        private int d;
        public child()
        {
            Console.WriteLine("자식 생성자, 파람 없음");
        }
        public child(int a, int b, int c, int d) : base(a, b, c)
        {
            this.d = d;
            Console.WriteLine("자식 생성자, 파람 있음");
        }
        public void printChild()
        {
            // private 멤버는 자식도 접근할 수 없음
            //Console.WriteLine("a: " + a + " b: " + b + " c: " + c + " d: " + d);
            Console.WriteLine(" b: " + b + " c: " + c + " d: " + d);
        }

        //public void print() // 메서드 재정의?
        //{

        //}

        public new void print() // 정적 결합. 타입에 의해서 결정
        {
            //방법 1
            Console.WriteLine(" b: " + b + " c: " + c + " d: " + d);
            // 방법 2
            base.print(); // 재정의 된 부모 메서드 호출
            Console.WriteLine("d: " + d);
        }
    }
}
