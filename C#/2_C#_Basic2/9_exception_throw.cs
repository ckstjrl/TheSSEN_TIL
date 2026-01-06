using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace day02
{
    internal class MyNum
    {
        public void f1(int num)
        {
            if(num <= 0)
            {
                throw new Exception("num은 0보다 커야함"); 
            }
            Console.WriteLine("num: " + num);
        }
    }
}
