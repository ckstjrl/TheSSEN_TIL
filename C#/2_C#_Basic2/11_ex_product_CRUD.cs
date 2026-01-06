using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace day02
{
    //상품번호(자동할당)/int, 상품명/string, 가격/int, 수량/int
    class Product
    {
        private static int cnt = 1;
       
        public int Num { set; get; }
        public string Name { set; get; }
        public int Price {  set; get; }
        public int Amount {  set; get; }

        public Product() { }
        public Product(string name, int price, int amount)
        {
            //상품 추가시 이 생성자로 해야함
            Num = cnt++;
            Name = name;
            Price = price;
            Amount = amount;
        }
        //object 클래스: c# 모든 클래스의 조상
        //object의 ToString()은 이 객체의 타입(클래스 fullname)을 반환
        //객체 출력시 이 메서드가 반환하는 값이 출력됨
        //override: 메서드 재정의
        public override string ToString()
        {
            //return base.ToString(); /this:현재 객체의 참조값 / base:부모 객체의 참조값
            return "num:" + Num + " / name:" + Name + " / price:" + Price 
                + " / amount:" + Amount;
        }

        public override bool Equals(object obj)
        {
            if (obj != null && obj is Product)
            {
                Product p = (Product)obj;
                if(p.Num == Num)
                {
                    return true;
                }
            }
            return false;
        }
    }
    class ProductContainer
    {
        private ArrayList list; // 타입, 크기 제한 없음, 기능 제공
        public ProductContainer() {
            list = new ArrayList();
        }
        public void insert(Product p)
        {
            list.Add(p);
        }
        public void printAll()
        {
            foreach (Product p in list)
            {
                Console.WriteLine(p);
            }
        }
        public Product selectByNum(int num)
        {
            Product getProd = new Product();
            getProd.Num = num;
            int idx = list.IndexOf(getProd);
            if(idx < 0)
            {
                return null;
            }
            else
            {
                return (Product)list[idx];
            }
        }
       
        public void delete(int num)
        {
            Product delProd = new Product();
            delProd.Num = num;
            list.Remove(delProd);
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            int i;
            string name;
            int num, price, amount;
            Product p = null;
            ProductContainer pc = new ProductContainer();
            for (i = 0; i < 3; i++)
            {
                Console.WriteLine("===상품추가===");
                Console.Write("상품명:");
                name = Console.ReadLine();
                Console.Write("상품가격:");
                price = Int32.Parse(Console.ReadLine());
                Console.Write("상품수량:");
                amount = Int32.Parse(Console.ReadLine());
                pc.insert(new Product(name, price, amount));
            }

            Console.WriteLine("===상품목록===");
            pc.printAll();

            for (i = 0; i < 2; i++)
            {
                Console.WriteLine("===상품검색===");
                Console.Write("상품번호:");
                num = Int32.Parse(Console.ReadLine());
                p = pc.selectByNum(num);
                if (p != null)
                {
                    Console.WriteLine(p);
                }
                else
                {
                    Console.WriteLine("없는 상품 번호");
                }
            }
            Console.WriteLine("===상품수정===");
            Console.Write("상품번호:");
            num = Int32.Parse(Console.ReadLine());
            p = pc.selectByNum(num);
            if (p != null)
            {
                Console.WriteLine(p);
                Console.Write("new 가격:");
                p.Price = Int32.Parse(Console.ReadLine());
                Console.Write("new 수량:");
                p.Amount = Int32.Parse(Console.ReadLine());
            }
            else
            {
                Console.WriteLine("없는 상품 번호. 수정취소");
            }
            pc.printAll();
            Console.WriteLine("===상품삭제===");
            Console.Write("상품번호:");
            num = Int32.Parse(Console.ReadLine());
            pc.delete(num);
            pc.printAll();
        }
    }
}