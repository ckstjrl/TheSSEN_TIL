using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    // 객체 단위로 읽고 쓰면 더 편함 
    // 바이너리 형태로 읽고 쓰는 형식으로 바꾸자
    // 직렬화가 되어 있어야 함
    // 직렬화: java에서는 객체를 넘길 때, (c에서는 참조변수, 주소값을 선택해서 보낼 수 있음) 객체를 넘김
    // 참조값은 이 프로그램에서만 의미가 있지, 다른 프로그램과 함께 쓸 때에는 참조값이 없음
    // 직렬화는 그 메모리에 있는 객체를 꺼내서 다 보내줘야 함
    // person 객체를 생성해서 멤버에 값을 넣고, 직렬화를 해야 함
    [Serializable] // 직렬화

    class person
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public person() { }
        public person(string name, int age)
        {
            Name = name;
            Age = age;
        }
        public override string ToString()
        {
            return "name: " + Name + " Age: " + Age;
        }
    }
}
