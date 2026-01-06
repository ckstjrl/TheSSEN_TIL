using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace day02
{
    class product
    {
        protected int price;
        protected string name;
        protected int point;

        // 상속 관계한테만 저 변수들이 보여서 하나 더 만듦
        public int Price { set; get; }
        public string Name { set; get; }
        public int Point { set; get; }
    }
    class Tv: product
    {
        public Tv()
        {
            Name = "tv";
            Price = 100;
            Point = 20;
        }
    }

    class computer : product
    {
        public computer()
        {
            Name = "computer";
            Price = 80;
            Point = 10;
        }
    }
    class buyer
    {
        public int Money { get; set; }
        public int Point { get; set; }
        public buyer()
        {
            Money = 1000;
            Point = 0;
        }
        // 상속을 한다는 것은 반복이 되는 코드를 줄이는 것도 있지만
        // 다형성을 구현하는 것에서도 쓰인다.
        // 구현하는 방법은 업캐스팅, 다운캐스팅, 오버로딩, 라이딩.. 등 다양한 방법이 있다

        // 오버로딩으로 구현
        /*
        public void buy(Tv p)
        {
            if(Money >= p.Price)
            {
                Console.WriteLine(p.Name + " 구매");
                Money -= p.Price;
                Point += p.Point;
            }
            Console.WriteLine("현재 Money: " + Money + " Point: " + Point);
        }
        public void buy(computer p)
        {
            if (Money >= p.Price)
            {
                Console.WriteLine(p.Name + " 구매");
                Money -= p.Price;
                Point += p.Point;
            }
            Console.WriteLine("현재 Money: " + Money + " Point: " + Point);

        }
        */

        // 상속을 이용을 해서 업캐스팅을 하면
        // 더 간결해질 수 있음
        // 코드가 더 간결해질 수 있음
        public void buy(product p)
        {
            if (Money >= p.Price)
            {
                Console.WriteLine(p.Name + " 구매");
                Money -= p.Price;
                Point += p.Point;
            }
            Console.WriteLine("현재 Money: " + Money + " Point: " + Point);

        }
    }
}
