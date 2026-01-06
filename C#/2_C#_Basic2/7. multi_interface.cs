using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace day02
{
    class A
    {
        public int a;
    }
    class B { public int b; }
    //class C: A, B
    //{

    //}
    interface IA
    {
        void f1();
    }
    interface IB
    {
        void f1();
    }
    class c: IA, IB
    {
        public void f1()
        {
            Console.WriteLine("f1구현");
        }
        public void f2()
        {
            Console.WriteLine("f2구현");
        }
    }
}
