using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace day02
{
    internal class car
    {
        protected string name;
        public car(string name)
        {
            this.name = name;
        }
        public virtual void horn()
        {
            Console.WriteLine(" 경적소리");
        }
    }

   class police : car // 이것만 쓰면 오류남 -> 생성자를 만들어야지 오류 없어짐
    {
        public police(string name) : base(name) { }

        public override void horn()
        {
            Console.WriteLine(name + " 삐뽀삐뽀");
            //base.horn();
        }
    }
    class em : car
    {
        public em (string name) : base(name) { }
        public override void horn()
        {
            Console.WriteLine(name + " 삐용~");
            //base.horn();
        }
    }
}
