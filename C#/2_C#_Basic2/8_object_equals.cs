using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace day02
{
    class Member
    {
        public string Name { get; set; }
        public int Age { get; set; }
        public Member() { }
        public Member(string name, int age)
        {
            Name = name;
            Age = age;
        }
        // Equals(): 객체 비교 메서드, object 클래스에 정의(참조값 비교)
        // 각 클래스에 적합하게 비교하는 구문으로 재정의 해야함
        // 모든 경우는 아님 -> 객체를 비교하는 게 필요하다라고 하면 쓰자
        public override bool Equals(object obj) // 현재 객체와 비교할 대상
        {
            if(obj != null && obj is Member)
            {

                // 업캐스팅 된 상태로는 비교할 수 없음. 그래서 다운 캐스팅을 해
                Member m = (Member)obj;
                if (Name.Equals(m.Name) && Age == m.Age) return true;
            }
            return false;
        }

        // ToString(): 객체 설명 메서드, 문자열 반환, object 클래스는 클래스 fullname 반환
        public override string ToString()
        {
            return "name:" + Name + " / age: " + Age;
        }
    }
}
