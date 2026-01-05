# C# day01

날짜: 2026년 1월 5일

## 기본 개념

### 1. 데이터 타입

- 기본 타입
    - 정수 타입
        - `sbyte`(1)
        - `byte`(1)
        - `short`(2)
        - `ushort`(2)
        - `int`(4)
        - `uint`(4)
        - `long`(8)
        - `ulong`(8)
        - `char`(2)
    - 실수형
        - `float`(4)
        - `double`(8)
    - 논리형
        - `bool`(1bit)
    - 참조형
        - object - C## 모든 클래스의 조상 클래스
        - string - 문자열

### 2. 출력 / 입력

- 출력
    - `Consloe.WriteLine("Hello World");`
    - `Consloe.WriteLine(3 + 3);`
- 입력
    - `string userName = Console.ReadLine();`
    - `int age = Convert.ToInt32(Console.ReadLine());`

### 3. 변수 / 정수

```csharp
int a = 10;
string b = “asdf”;
const int num = 10;
```

### 4. 캐스팅

- 암묵적 형변환 (자동) - 더 작은 유형을 더 큰 유형 크기로 변환
    
    char-> int-> long-> float->double
    
- 명시적 캐스팅 (수동) - 더 큰 유형을 더 작은 크기의 유형으로 변환
double-> float-> long-> int->char
    
    ```csharp
    int myInt = 10;
    double myDouble = 5.25;
    bool myBool = true;
    
    ```
    
- 형변환 메서드
    
    ```csharp
    Console.WriteLine(Convert.ToString(myInt)); // convert int to string
    Console.WriteLine(Convert.ToDouble(myInt)); // convert int to double
    Console.WriteLine(Convert.ToInt32(myDouble)); // convert double to int
    Console.WriteLine(Convert.ToString(myBool)); // convert bool to string
    ```
    

### 5. 클래스와 객체

- 객체는 항상 new로 생성
- 객체 메모리는 자동 해제됨 → 가비지 컬렉터

```csharp
class Car
{
	public string model;
	public string color;
	public int year;
	public void fullThrottle()
 {
		Console.WriteLine("The car is going as fast as it can!");
 }
}

class Program
{
	static void Main(string[] args)
	{
		Car Ford = new Car();
		Ford.model = "Mustang";
		Ford.color = "red";
		Ford.year = 1969;
		Car Opel = new Car();
		Opel.model = "Astra";
		Opel.color = "white";
		Opel.year = 2005;
		Console.WriteLine(Ford.model);
		Console.WriteLine(Opel.model);
	}
}
```

- 예제

```csharp
namespace day01
{
    class Car
    {
        public string model;
        public string color;
        public int year;
        public Car() { }
        public Car(string model, string color, int year)
        {
            this.model = model;
            this.color = color;
            this.year = year;
        }

        public void print()
        {
            Console.WriteLine("model:" + model);
            Console.WriteLine("color:" + color);
            Console.WriteLine("year:" + year);
        }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            // System.NullReferenceException: 개체 참조가 개체의 인스턴스로 설정되지 않았습니다.
            // 객체가 없다는 에러
            /*
            Car car = null;
            car.model = Console.ReadLine();
            */
            Car car = new Car("model1", "color1", 2026);
            car.print();
            /*
            output
            model: model1
            color:color1
            year:2026
            */
            Console.Write("model:");
            car.model = Console.ReadLine();
            Console.Write("color:");
            car.color = Console.ReadLine();
            Console.Write("year:");
            car.year = Convert.ToInt32(Console.ReadLine());
            car.print();
            /*
            input
            model:Tesla model S
            color:red
            year:2025
            
            output
            model:Tesla model S
            color:red
            year:2025
            */
        }
    }
}
```

### 6. 생성자

```csharp
class Car
{
	public string model;
	public string color;
	public int year;
	// 오버로딩 가능
	public Car(string modelName, string modelColor, int modelYear)
	{
		model = modelName;
		color = modelColor;
		year = modelYear;
	}
	static void Main(string[] args)
	{
		Car Ford = new Car("Mustang", "Red", 1969);
		Console.WriteLine(Ford.color + " " + Ford.year + " " + Ford.model);
	}
}
```

### 7. 접근 제어자

- 클래스나 멤버에 작성
- public : 모든 클래스에서 접근
- private : 클래스 내에서만 접근. 디폴트 값
- protected : 상속관계에서는 public, 아닌 클래스엔 private
- internal : 현재 어셈블리에서만 사용가능. 다른 어셈블리에서 사용 불가능

### 8. 속성

- 캡슐화: "민감한" 데이터가 사용자에게 숨겨지도록 숨김.
- 필드/변수를 private 으로 선언
- 속성을 통해 필드 값에 액세스하고 업데이트하기 위한 public setter / getter 제공
- 속성 코드
    
    ```csharp
    class Person
    {
    	private string name; // field
    	public string Name // property
    	{
    		get { return name; } // get method
    		set { name = value; } // set method
    	}
    }
    
    class Program
    {
    	static void Main(string[] args)
    	{
    		Person myObj = new Person();
    		myObj.Name = "Liam";
    		Console.WriteLine(myObj.Name);
    	}
    }
    ```
    
- 자동 속성
    
    ```csharp
    class Person
    {
    	public string Name // property
    	{ get; set; }
    }
    
    class Program
    {
    	static void Main(string[] args)
    	{
    		Person myObj = new Person();
    		myObj.Name = "Liam";
    		Console.WriteLine(myObj.Name);
    	}
    }
    ```
    
- 예제
    
    ```csharp
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    
    namespace day01
    {
        class Person
        {
            private string name;
            public string Name { get; set; }
            private string tel;
            public string Tel { get; set; }
            private string address;
            public string Address { get; set; }
    
            public Person() { }
            public Person(string name, string tel, string address)
            {
                this.name = name;
                this.tel = tel;
                this.address = address;
            }
    
            public void print()
            {
                Console.WriteLine("name:" + Name);
                Console.WriteLine("tel:" + Tel);
                Console.WriteLine("address:" + Address);
            }
        }
        internal class Program
        {
            static void Main(string[] args)
            {
                Person person = new Person();
                person.Name = "ckstjrl";
                person.Tel = "010-0000-0000";
                person.Address = "Seoul";
                person.print();
                Console.WriteLine(person);
            }
        }
    }
    
    /*
    name:ckstjrl
    tel:010-0000-0000
    address:Seoul
    day01.Person
    */
    ```
    
- C# 배열 생성
    
    ```csharp
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    
    namespace day01
    {
        
        internal class Program
        {
            static void Main(string[] args)
            {
                int[] arr = { 1, 2, 3, };
    						int[] arr2 = new int[2];
    						int i;
    						for(i=0; i < arr.Length; i++)
    						{
    						    Console.Write(arr[i] + "\t");
    						}
    						Console.WriteLine();
    						for (i = 0; i < arr2.Length; i++)
    						{
    						    Console.Write(arr2[i] + "\t");
    						}
    						Console.WriteLine();
    				}
        }
    }
    
    /*
    1       2       3
    0       0
    */
    ```
    
- 객체 배열
    
    ```csharp
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    
    namespace day01
    {
        class Person
        {
            private string name;
            public string Name { get; set; }
            private string tel;
            public string Tel { get; set; }
            private string address;
            public string Address { get; set; }
    
            public Person() { }
            public Person(string name, string tel, string address)
            {
                Name = name;
                Tel = tel;
                Address = address;
            }
    
            public void print()
            {
                Console.WriteLine("name:" + Name);
                Console.WriteLine("tel:" + Tel);
                Console.WriteLine("address:" + Address);
            }
        }
        internal class Program
        {
            static void Main(string[] args)
            {
                Person[] arr = { new Person("ckstjrl", "010-0000-0000", "서울"), 
                    new Person("박찬", "010-1111-1111", "판교"), 
                    new Person("찬석", "010-2222-2222", "사천") };
                int i;
                for(i=0; i < arr.Length; i++)
                {
                    arr[i].print(); 
                }
                Console.WriteLine();
    
                Person[] arr1 = new Person[2];
                for(i=0; i < arr1.Length; i++)
                {
                    arr1[i] = new Person();
                    Console.Write("name:");
                    arr1[i].Name = Console.ReadLine();
                    Console.Write("tel:");
                    arr1[i].Tel = Console.ReadLine();
                    Console.Write("address:");
                    arr1[i].Address = Console.ReadLine();
                }
                for (i=0; i < arr1.Length; i++)
                {
                    arr1[i].print();
                }
            }
        }
    }
    /*
    //arr output
    name:ckstjrl
    tel:010-0000-0000
    address:서울
    name:박찬
    tel:010-1111-1111
    address:판교
    name:찬석
    tel:010-2222-2222
    address:사천
    
    //arr1 input
    name:ckstjrl
    tel:010-0000-0000
    address:서울
    name:박찬
    tel:010-1111-1111
    address:판교
    //arr1 output
    name:ckstjrl
    tel:010-0000-0000
    address:서울
    name:박찬
    tel:010-1111-1111
    address:판교
    */
    ```
    

### 9. 메서드 재정의

- 동일한 이름이나 메서드 여러 개 정의
- 파라메터 개수나 타입 다르게 지정

```csharp
using System;
namespace MyApplication
{
	class Program
	{
		static int PlusMethodInt(int x, int y)
		{
			return x + y;
		}
		static double PlusMethodDouble(double x, double y)
		{
			return x + y;
		}
		static void Main(string[] args)
		{
			int myNum1 = PlusMethodInt(8, 5);
			double myNum2 = PlusMethodDouble(4.3, 6.26);
			Console.WriteLine("Int: " + myNum1);
			Console.WriteLine("Double: " + myNum2);
		}
	}
}
```

```csharp
using System;
namespace MyApplication
{
	class Program
	{
		static int PlusMethod(int x, int y)
		{
			return x + y;
		}
		static double PlusMethod(double x, double y)
		{
			return x + y;
		}
		static void Main(string[] args)
		{
			int myNum1 = PlusMethod(8, 5);
			double myNum2 = PlusMethod(4.3, 6.26);
			Console.WriteLine("Int: " + myNum1);
			Console.WriteLine("Double: " + myNum2);
		}
	}
}
```

- 예제
    
    ```csharp
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    
    namespace day01
    {
        class Test1
        {
            public int a = 1;
            public static int b = 1; // 객체들이 공유, 초기화는 한번만, 객체 생성 유무 상관없음
    
            public void add()
            {
                a++;
                b++;
            }
    
            public void print()
            {
                Console.WriteLine("a: " + a);
                Console.WriteLine("b: " + b);
            }
        }
        internal class Program
        {
            static void Main(string[] args)
            {
                Test1 t1 = new Test1();
                t1.add();
                t1.print();
                Console.WriteLine();
    
                Test1 t2 = new Test1();
                t2.add();
                t2.print();
                Console.WriteLine();
                
                Test1 t3 = new Test1();
                t3.add();
                t3.print();
                Console.WriteLine();
            }
        }
    }
    
    /*
    a: 2
    b: 2
    
    a: 2
    b: 3
    
    a: 2
    b: 4
    */
    // a는 객체 호출할 때마다 1로 초기화, b는 초기화 처음 딱 1번
    ```
    
- static 멤버 함수
    
    ```csharp
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    
    namespace day01
    {
        class Test1
        {
            public int a = 1;
            public static int b = 1; // 객체들이 공유, 초기화는 한번만, 객체 생성 유무 상관없음
    
            public void add()
            {
                a++;
                b++;
            }
    
            // 일반 멤버함수는 일반 멤버, static 멤버 모두 사용 가능
            public void print()
            {
                Console.WriteLine("a: " + a);
                Console.WriteLine("b: " + b);
            }
    
            // 일반 멤버 사용 불가, 객체 생성 전에 호출하면 일반 멤버는 존재하지 않기때문
            public static void f1()
            {
                Console.WriteLine("b: " + b);
            }
    
            // 일반 멤버 함수는 일반 멤버 함수, static 멤버 함수 모두 호출 가능
            public void f2()
            {
                print(); // 일반 멤버 함수
                f1(); // static 멤버 함수
            }
    
            // static 멤버 함수는 static 멤버 함수만 호출 가능
            public static void f3()
            {
                f1();
            }
        }
        internal class Program
        {
            static void Main(string[] args)
            {
                Test1.f1();
                Test1.f3();
                Console.WriteLine();
    
                Test1 t1 = new Test1();
                t1.add();
                t1.print();
                Console.WriteLine();
    
                Test1 t2 = new Test1();
                t2.add();
                t2.print();
                Console.WriteLine();
    
                Test1 t3 = new Test1();
                t3.add();
                t3.print();
                Console.WriteLine();
            }
        }
    }
    
    /*
    b: 1
    b: 1
    
    a: 2
    b: 2
    
    a: 2
    b: 3
    
    a: 2
    b: 4
    */
    ```
    
- 상품 추가 예제
    
    ```csharp
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    
    namespace day01
    {
        // 상품 번호(자동 할당), 상품 명, 가격, 수량
        class Product
        {
            private static int cnt = 1;
            public int Num { get; set; }
            private string name;
            public string Name { get; set; }
            private int price;
            public int Price { get; set; }
            private int ea;
            public int Ea { get; set; }
    
            public Product() { }
            public Product(string name, int price, int ea)
            {
                // 상품 추가시 이 생성자 사용해야 자동으로 상품 번호 증가
                Num = cnt++;
                Name = name;
                Price = price;
                Ea = ea;
            }
    
            // object 클래스 : C# 모든 클래스의 조상
            // object의 ToString()은 이 객체의 타입(클래스 fullname)을 반환
            // 객체 출력시 이 메서드가 반환하는 값이 출력된다.
            // override : 메서드 재정의
            public override string ToString()
            {
                // this: 현재 객체의 참조 값 / base: 부모 객체의  참조값
                // return base.ToString();
                return "num: " + Num + " / name: " + Name + " / price: " + Price + " / ea: " + Ea;
            }
    
        }
        internal class Program
        {
            static void Main(string[] args)
            {
                Product p1 = new Product("샴푸", 1111, 11);
                Product p2 = new Product("린스", 2222, 22);
                Product p3 = new Product("바디워시", 3333, 33);
                Console.WriteLine(p1);
                Console.WriteLine(p2);
                Console.WriteLine(p3);
            }
        }
    }
    
    /*
    num: 1 / name: 샴푸 / price: 1111 / ea: 11
    num: 2 / name: 린스 / price: 2222 / ea: 22
    num: 3 / name: 바디워시 / price: 3333 / ea: 33
    */
    ```
    

## 상품 추가, 검색, 삭제 기능 구현

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp4
{
    //상품번호(자동할당)/int, 상품명/string, 가격/int, 수량/int
    class Product
    {
        private static int cnt = 1;

        public int Num { set; get; }
        public string Name { set; get; }
        public int Price { set; get; }
        public int Amount { set; get; }

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
    }
    class ProductContainer
    {
        private Product[] products;
        private int cnt;
        public ProductContainer()
        {
            products = new Product[30];
        }
        public void insert(Product p)
        {
            if (cnt >= 30)
            {
                Console.WriteLine("배열 참");
                return;
            }
            products[cnt++] = p;
        }
        public void printAll()
        {
            for (int i = 0; i < cnt; i++)
            {
                Console.WriteLine(products[i]);
            }
        }
        public int selectByNum(int num)
        {
            int i;
            for (i = 0; i < cnt; i++)
            {
                if (num == products[i].Num)
                {
                    return i;
                }
            }
            return -1;
        }
        public Product selectByIdx(int idx)
        {
            if (idx < 0 || idx >= cnt)
            {
                return null;
            }
            else
            {
                return products[idx];
            }
        }
        public void delete(int idx)
        {
            int i;
            for (i = idx; i < cnt - 1; i++)
            {
                products[i] = products[i + 1];
            }
            cnt--;
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
                p = pc.selectByIdx(pc.selectByNum(num));
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
            p = pc.selectByIdx(pc.selectByNum(num));
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
            int idx = pc.selectByNum(num);
            if (idx < 0)
            {
                Console.WriteLine("없는 상품 번호. 삭제 취소");
            }
            else
            {
                pc.delete(idx);
            }
            pc.printAll();
        }
    }
}

```