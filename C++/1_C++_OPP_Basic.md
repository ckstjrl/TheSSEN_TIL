# C++ day01

날짜: 2025년 12월 29일

## C++ 개요

```cpp
// 기본 출력
#include <iostream>

int main() // 전역 함수
{
    std::cout << "Hello World!\n";
}
```

### C++은 객체지향 프로그래밍(OOP)의 대표 언어

- C++은 절차지향(C)과 객체지향(OOP)을 모두 지원하는 언어
- 클래스, 객체, 상속, 다형성 같은 객체지향 개념을 직접 구현할 수 있음
- 성능이 중요한 시스템, 임베디드, 게임 개발에서 많이 사용됨

### 메모리 안정성과 성능 측면의 특징

- C++은 메모리를 개발자가 직접 관리해야 함
- 장점
    - 불필요한 자동 처리 없이 높은 성능을 낼 수 있음
- 단점
    - 메모리 관리 실수 시 치명적인 오류 발생 가능
    - 디버깅 난이도가 높음

### 메모리 해제 관리의 중요성

- 동적 메모리 할당 후 반드시 해제가 필요함
- 잘못 관리할 경우 발생하는 문제
    - 메모리 누수 (Memory Leak)
    - 스택 오버플로우
        - 과도한 재귀 호출
        - 지역 변수의 과다 사용
    - 힙 오버플로우
        - 대규모 동적 할당 반복
        - 해제 누락
- C++ 학습 시 메모리 구조 이해는 필수

---

## 2. 객체(Object)의 개념

### 객체란 무엇인가

- 객체는 실생활의 개념을 프로그램으로 표현한 모델
- 데이터(속성)와 기능(동작)을 하나로 묶은 단위

예시 개념

- 사람: 이름, 나이 / 말하다, 걷다
- 자동차: 속도, 연료 / 가속하다, 멈추다

객체는 현실 세계를 코드로 옮기기 위한 기본 단위이다.

---

## 3. 객체 사이의 관계

### 포함 관계 (Has-a 관계)

- A 객체가 B 객체를 포함하는 관계
- 예: 자동차는 엔진을 가진다
- 특징
    - 구성, 조립 관계
    - 현실 세계 구조를 표현하기에 적합

### 상속 관계 (Is-a 관계)

- A 객체가 B 객체의 한 종류인 관계
- 예: 전기차는 자동차이다
- 특징
    - 기존 코드 재사용
    - 공통 기능을 상위 클래스로 묶음

---

## 4. 객체지향 프로그래밍의 핵심 개념

### 1. 캡슐화 (Encapsulation)

- 객체 내부 구현을 외부에서 직접 접근하지 못하도록 숨김
- 필요한 기능만 공개
- 장점
    - 데이터 보호
    - 코드 유지보수 용이
    - 오류 발생 가능성 감소

### 2. 오버로딩과 오버라이딩

### 오버로딩 (Overloading)

- 함수 이름은 같지만 매개변수의 개수나 타입이 다른 경우
- 동일한 개념을 다양한 형태로 표현 가능

### 오버라이딩 (Overriding)

- 상속 관계에서 부모 클래스의 함수를 자식 클래스가 재정의
- 다형성을 구현하는 핵심 요소

### 3. 상속과 다형성

- 상속
    - 공통된 속성과 기능을 부모 클래스에 정의
    - 코드 재사용성과 구조적 설계에 유리
- 다형성
    - 같은 함수 호출
    - 객체 타입에 따라 서로 다른 동작 수행
- 하나의 인터페이스로 여러 동작을 구현할 수 있음

---

## 5. C++ 컴파일 및 실행 과정

### 컴파일 과정

1. 소스 코드 작성
2. 컴파일
    - 오브젝트 파일 생성 (.o, ELF 형식)
3. 링킹
    - 여러 오브젝트 파일 결합
4. 실행 파일 생성 (.exe)

---

## 6. 프로그램 메모리 구조

### 실행 파일의 메모리 영역 구성

- text 영역
    - 실행 코드와 상수 저장
    - 읽기 전용
- data 영역
    - 초기화된 전역 변수
    - 초기화된 static 변수
- bss 영역 (ZI 영역)
    - 초기화되지 않은 전역 변수
    - 초기화되지 않은 static 변수
    - 실행 시 자동으로 0으로 초기화됨
- heap 영역
    - 동적 메모리 할당 공간
    - new, malloc으로 할당
    - 런타임 중 크기 변경 가능
- stack 영역
    - 함수 호출 정보
    - 매개변수, 지역 변수 저장
    - 함수 종료 시 자동 해제

### 실행 중 메모리 동작 특징

- heap은 낮은 주소에서 높은 주소 방향으로 확장
- stack은 높은 주소에서 낮은 주소 방향으로 확장
- heap과 stack이 충돌하면 프로그램이 비정상 종료됨
- 시스템, 임베디드 개발에서 특히 중요함

---

## C와의 차이점

### 1. `scnaf()` / `printf()` → `cin` / `cout`

---

### 2. `::` 스코프 연산자

      → 네임스페이스를 사용하여 이름 영역 구분

---

### 3. 지역 변수 선언 위치 :

      C++은 사용하기 전에 선언만 하면 됨, 위치 상관 X

힙 메모리 할당

`malloc(크기)` / `free()` → `new 타입` / `delete` 

```cpp
int* p = new int;
*p = 10;
cout << "*p = " << *p << endl;
delete p;

// output : *p = 10
```

---

### 4. C++은 함수 오버로딩 가능

- 함수 오버로딩 : 동일한 이름의 함수를 여러 개 정의할 수 있음
    
    파라메터 개수나 타입을 다르게 해야함.
    

ex)

```cpp
// Test.h
#pragma once
class Test {
};
add(int, int);
add(int, int, int);
add(float, float);

//Test.cpp
#include "Test.h"

int add(int a, int b) {
	return a + b;
}

int add(int a, int b, int c) {
	return a + b + c;
}

float add(float a, float b) {
	return a + b;
}

// main.cpp
#include <iostream>
#include "Test.h"

using namespace std;

int main() {
    int res1 = add(1, 2);
    int res2 = add(1, 2, 3);
    float res3 = add(1.2f, 2.3f);

    cout << "res1 : " << res1 << "\n"
		    << "res2 : " << res2 << "\n"
		    << "res3 : " << res3 << endl;
}

/*
output
res1 : 3
res2 : 6
res3 : 3.5
*/
```

---

### 5. 함수 단점

- 재귀호출은 스택 오버플로우 문제 발생 위험
    
    함수 호출 → 함수로 분리 → 파이프라인 삭제 다시 구축 → 오버헤더 발생
    
- 짧은 함수는 매크로나 인라인 함수로 대체

---

### 6. 참조 변수

- 변수의 별칭, 선언시 꼭 초기화 필요
    
    `int a = 10;` → 변수 선언, stack, static 메모리 할당
    
    `int &b = a;` → b는 참조 변수
    
    → 컴파일러도 원본 구분 못함. a, b 동일
    
- call by value : 변수 값 복사해서 전달
- call by reference : 참조 변수를 통한 전달

```cpp
#include <iostream>

using namespace std;

int main() {
    int x = 10;
    int& y = x;
    cout << "x = " << x << "\n"
        << "y = " << y << "\n" << endl;
    
    y = 20;
    cout << "x = " << x << "\n"
        << "y = " << y << "\n" << endl;
}

/*
output
x = 10
y = 10

x = 20
y = 20

컴파일러는 x와 y를 구분하지 못한다.
*/
```

```cpp
// Test.h
void swap(int, int);
void swap(int*, int*);
void swapref(int&, int&);

//Test.cpp

// call by value (값 복사)
void swap(int a, int b) {
	int temp;
	temp = a;
	a = b;
	b = temp;
}

// call by value (주소값 복사)
void swap(int * a, int * b) {
	int temp;
	temp = *a;
	*a = *b;
	*b = temp;
}

// call by reference
void swapref(int& a, int& b) {
	int temp;
	temp = a;
	a = b;
	b = temp;
}

// main.cpp

#include <iostream>
#include "Test.h"

using namespace std;

int main() {
    int w = 10, q = 20;
    swap(w, q);
    cout << "call by value w = " << w << "\n"
        << "call by value q = " << q << "\n" << endl;

    w = 10;
    q = 20;
    swap(&w, &q);
    cout << "call by address value w = " << w << "\n"
        << "call by address value q = " << q << "\n" << endl;

    w = 10;
    q = 20;
    swapref(w, q);
    cout << "call by reference w = " << w << "\n"
        << "call by reference q = " << q << "\n" << endl;
}

/*
output
call by value w = 10
call by value q = 20

call by address value w = 20
call by address value q = 10

call by reference w = 20
call by reference q = 10
-> 메모리 값 제일 적게 듦
*/

```

---

## 객체지향 프로그래밍

### 객체 중심 프로그래밍

→ 모델링할 구분을 구성하는 객체를 도출

→ 객체를 정의, 관계를 명시

→ 다형성 구현

---

### ATM 프로그래밍 예시

- <요구분석>: 이 프로그램이 외부에 제공할 기능 정의
    1. 액터 선정: 이 시스템을 사용할 사람/시스템
    2. 각 액터 별로 제공할 기능 정의

ATM

<기능분석. 명세> 

→ 기능 분석을 상세히 할수록 객체(설계시 객체, sample)를 정할 수 있음.

1. 출금
카드삽입 → 출금메뉴 선택 → 비밀번호입력 (Y) → 금액 입력 → 
    
                                                                                (N) → 오류 출력 후  메뉴로 리턴
    
    (잔액있음)-> 출금
    
    (없음)->오류 출력 후 메뉴로 리턴
    
    객체(sample) 도출 → 제작할 class 도출.
    
    카드 (카드 번호, 회사 명, 비밀번호, 유효 기간..., 연결 계좌)
    
    계좌 (은행 명, 계좌번호, 예금주, 잔액...)
    
2. 입금
    - <시퀀스 다이어그램>
        
        시간 흐름에 따른 객체 변화
        
        카드 생성 --<CREATE>---> 계좌 (포함 관계)
        
    - <클래스 다이어그램>
        
        객체 별 관계 명시
        
        클래스 명
        
        멤버 변수(타입 변수 명)
        
        멤버 함수(반환 타입 이름(파라메터 리스트))
        
    - 클래스 작성
        
        ```cpp
        class 클래스명{
        		접근제어자:
        			 타입 변수명; //멤버변수
        			 함수타입 함수명(파라메터); //멤버함수
        };
        ```
        

예시 코드

```cpp
// Person.h
class Person{
public:
   string name;
   string tel;
   int num;
   void inputInfo(string n, string t, int no);
   void outputInfo();
};

// Person.cpp
void Person::inputInfo(string n, string t, int no){
	name = n;
	tel = t;
  num = no;
}
void Person::outputInfo(){
	cout<<name<<endl;
	cout<<tel<<endl;
	cout<<num<<endl;
}

// main.cpp
#include "Person.h"
int main(){
   Person p1; //스택에 객체 생성
   p1.inputInfo("aaa", "111", 1);
   p1.outputInfo();
   p1.name = "bbb";
   p1.tel = "222";
   p1.num = 2;
   p1.outputInfo();

   Person *p2 = new Person; //힙에 객체 생성
   p2->inputInfo("ccc", "333", 3);
   p2->outputInfo();
}
```

---

### 구조체도 함수를 포함할 수 있다.

```cpp
// Test.h
struct Mystr {
	int a;
	int b;
	void input(int x, int y);
	void print();
};

// Test.cpp
void Mystr::input(int x, int y) {
	a = x;
	b = y;
}
void Mystr::print() {
	cout << "a : " << a << "\n"
		<< "b : " << b << "\n" << endl;
}

// main.cpp
#include <iostream>
#include "Test.h"

using namespace std;

int main() {
    Mystr ms;
    ms.input(1, 2);
    ms.print();
}
/*
output
a : 1
b : 2
*/
```

---

### class 예시

```cpp
// Test.h
#pragma once
// 멤버 변수, 멤버 함수(메서드)를 포함
// C++ 클래스 멤버의 default 접근 제어자는 private -> 클래스 안에서만 보이고 밖에서는 안보임
// 접근제어자 : visible
// public :  클래스 외부에서 보임
class Test {
public: //  클래스 외부에서 보임
	int a;
	int b;
	void input(int, int);
	void print();
};

// Test.cpp
void Test::input(int x, int y) {
	a = x;
	b = y;
}

void Test::print() {
	cout << "t.a : " << a << "\n"
		<< "t.b : " << b << "\n" << endl;
}

// main.cpp
#include <iostream>
#include "Test.h"

using namespace std;

int main() {
    Test t1;
    
    t1.a = 1;
    t1.b = 2;
    // 접근 제어자 때문에 class의 경우 이렇게 사용하면 오류 발생
    // class에 public 접근제어자 작성하면 오류 발생 X

    cout << "t1.a = " << t1.a << "\n"
        << "t1.b = " << t1.b << "\n" << endl;
    
    Test* p = new Test; // 힙에 객체 생성
		p->a = 3;
		p->b = 4;
		cout << "p.a = " << p->a << "\n"
		    << "p.b = " << p->b << "\n" << endl;
		delete p; // 해제
		
		Test t;
		
		t->input(5, 6);
		t->print();
}

/*
output
t1.a = 1
t1.b = 2

p.a = 3
p.b = 4

t.a : 5
t.b : 6
*/
```

### class 변수에 string 포함

```cpp
// Test.h
class Person {
private: // 멤버변수는 통상적으로 private (은닉성)
	int num;
	char name[20];
	
public: // 메서드는 public
	void input(int, const char*);
	void print();
};

// Test.cpp
#include <cstring>
void Person::input(int n, const char* nm) {
	num = n;
	// strcpy_s(대상, 크기, 원본)
	strcpy_s(name, 20, nm);
}

void Person::print() {
	cout << "num : " << num << "\n"
		<< "name : " << name << "\n" << endl;
}

//main.cpp
#include <iostream>
#include <cstring>
#include "Test.h"

using namespace std;

int main() {
    Person p1; // 스택에 객체 생성

    p1.input(1, "ckstjrl");
    p1.print();
    
    Person* p2 = new Person; // 힙에 객체 생성
    p2->input(2, "chan");
    p2->print();
    delete p2;
}
/*
output
num : 1
name : ckstjrl

num : 2
name : chan
*/
```

### 접근 제어자: visible

- private :
    
    클래스 내에서만 보임, 은닉성 제공.
    
    클래스 외부에서 다이렉트 접근 막음 → 보호. 멤버변수에 지정
    
- protected : 상속 관계 클래스에만 보이고 다른 클래스에서는 안보임
- public: 모두에게 보임

---

### 오버로딩

- 동일한 이름의 함수를 여러 개 정의하는 기법
- 기능은 같으나 입력, 출력 값에 따라서 이름을 다르게 지어야 하는 번거로움을 제거
- 다형성에도 사용될 수 있음
- 함수 호출 시 컴파일러가 구분할 수 있도록 파라메터의 타입이나 개수를 다르게 해야함

---

### 생성자

- 클래스와 이름이 같은 멤버 함수. 반환 타입이 없음
- 맘대로 호출 못함. 객체 생성 시에만 호출됨
- 객체 초기화, 멤버 변수 값 할당
- 생성자 작성을 안하면 컴파일러가 자동으로 생성해줌 → 아무 일도 안함
- 한 개라도 작성하면 자동 생성 없음
- 오버로딩으로 여러 개 만들 수 있음

```cpp
// Test.h
class Test {
private:
	int a;
	int b;
	
public:
	Test();
	Test(int, int); // 생성자 오버로딩
	void input(int, int);
	void print();
};

// Test.cpp
void Test::input(int x, int y) {
	a = x;
	b = y;
}

void Test::print() {
	cout << "t.a : " << a << "\n"
		<< "t.b : " << b << "\n" << endl;
}

Test::Test() {
	cout << "디폴트 생성자\n" << endl;
}

Test::Test(int x, int y) {
	cout << "파라메터가 있는 생성자\n" << endl;
	a = x;
	b = y;
}

// main.cpp
#include <iostream>
#include "Test.h"

using namespace std;

int main() {
    Test t; // 파라메터 없는 생성자
    t.input(1, 2);
    t.print();

    Test t1(3, 4); // 파라메터 있는 생성자
    t1.print();
	
		Test* t2 = new Test();
		t2->input(5, 6);
		t2->print();
		delete t2;
		
		Test* t3 = new Test(7, 8);
		t3->print();
		delete t3;
}

/*
output
디폴트 생성자

t.a : 1
t.b : 2

파라메터가 있는 생성자

t.a : 3
t.b : 4

디폴트 생성자

t.a : 5
t.b : 6

파라메터가 있는 생성자

t.a : 7
t.b : 8
*/
```

---

### `this->`

- 객체 자신의 주소를 갖는 포인터 변수
- 클래스 안에서만 사용 (밖에서는 객체 이름으로 접근)

```cpp
Test::Test(int a, int b) {
	cout << "파라메터가 있는 생성자\n" << endl;
	this->a = a;
	this->b = b;
}
```

---

### 이니셜라이져

`int a = 2;` → C에서의 초기화

`int a(2);` → C++에서의 초기화

### 생성자 초기화 코드

`Point::Point():x(0), y(0){}` 

```cpp
Point::Point(){
	x = 0;
	y = 0;
}
```

두 코드는 동일한 의미를 갖음

### 이니셜라이져를 활용한 코드

```cpp
// Point.h
class Point
{
private:
	int x;
	int y;
	
public:
	Point();
	Point(int, int);
	void print();
};

// Point.cpp
#include "Point.h"
#include <iostream>

using namespace std;

Point::Point():x(0), y(0){}

Point::Point(int x, int y):x(x), y(y){}

void Point::print() {
	cout << "x : " << x << "\n"
		<< "y : " << y << "\n" 
		<< "(" << x << ", " << y << ")" << "\n" << endl;
}

// main.cpp
#include <iostream>
#include "Point.h"

using namespace std;

int main() {
    Point p;
    p.print();

    Point p1(1, 2);
    p1.print();

    Point* p2 = new Point();
    p2->print();

    Point* p3 = new Point(3, 4);
    p3->print();
    
    Point p4 = p1; // 복사 생성자에 의해서 모든 멤버가 복사됨 == Point p3(p2)
    // 스택 메모리는 다르지만 같은 값을 갖는다.
		p4.print();
}

/*
output
x : 0
y : 0
(0, 0)

x : 1
y : 2
(1, 2)

x : 0
y : 0
(0, 0)

x : 3
y : 4
(3, 4)

x : 1
y : 2
(1, 2)
*/
```

---

### 소멸자

- 객체가 소멸 직전 호출됨
- 보통 객체 자원 해제. 특히 힙 메모리 반환할 때
- `~생성자명()`

### 복사 생성자

```cpp
Person p1;
// 아래의 경우 복사 생성자가 실행됨
Person p2 = p1;
Person p3(p1);
```

- 복사 생성자는 컴파일러가 기본 제공 → 새 객체를 생성해서 원본 객체의 모든 멤버 변수 값을 복사
- 생성자에서 힙의 메모리를 할당 받는 경우 + 얕은 복사 (주소 값만 복사)를 하는 경우 복사 생성자 생
- 이런 경우 깊은 복사를 하는 사용자 정의 복사 생성자를 만들어줘야 함.

→ 주로 함수의 파라메터로 객체를 전달하거나, 함수의 리턴 값으로 객체를 던질 경우 문제 발생

```cpp
// Point.h
class Person
{
private:
	int num;
	char* name;
	
public:
	Person();
	Person(int, const char*);

	// 복사 생성자, 이걸 통해 복사하면 Error 발생을 해결
	Persona(const Persona& p); 
	//------------------------------------------------

	void print();
	~Person(); // 소멸자
};

// Point.cpp
Person::Person():num(0){
	name = new char[10];
	strcpy_s(name, 10, "이름 없음");
}

Person::Person(int num, const char* name) :num(num) {
	this->name = new char[strlen(name) + 1];
	strcpy_s(this->name, strlen(name) + 1, name);
}

//----------------- 복사 생성자 detail--------------------------
Person::Person(const Person& p) :num(p.num) {
	this->name = new char[strlen(p.name) + 1];
	strcpy_s(this->name, strlen(p.name) + 1, p.name);
}
//--------------------------------------------------------------

void Person::print() {
	cout << "num : " << num << "\n"
		<< "name : " << name << "\n" << endl;
}

Person::~Person() {
	cout << "소멸자 호출\n" << endl;
	delete[]name;
}

// main.cpp
#include <iostream>
#include "Point.h"

using namespace std;

int main() {
		Person p0;
    p0.print();
    
    Person p1(1, "ckstjrl");
    p1.print();
    
    // Error 발생
    // name에는 heap에 저장되어 있는 문자열의 주소값이 담겨 있음
    // 소멸자에 의해 heap 주소가 해제되고
    // p2 과정에서 한번더 heap 주소를 해제하려고 하면 Error 발생하는 것
    // 요약하자면 이미 해제된 heap 주소를 다시 해제하려고 해서 문제
    //------- 복사 생성자 없는 경우 -----------------------
    Person p2(p1); // Error
		//-----------------------------------------------------
		
		//---------- 복사 생성자 있는 경우 -----------------
		// 정상 출력
		Person p3(p1);
		p3.print();  
}

/*
output
num : 0
name : 이름 없음

num : 1
name : ckstjrl

num : 1
name : ckstjrl

소멸자 호출

소멸자 호출

소멸자 호출
*/
```

### class 배열

```cpp
// Point.h
class Point
{
private:
	int x;
	int y;
	
public:
	Point();
	Point(int, int);
	void print();
};

// Point.cpp
#include "Point.h"
#include <iostream>

using namespace std;

Point::Point():x(0), y(0){}

Point::Point(int x, int y):x(x), y(y){}

void Point::print() {
	cout << "x : " << x << "\n"
		<< "y : " << y << "\n" 
		<< "(" << x << ", " << y << ")" << "\n" << endl;
}

// main.cpp
#include <iostream>
#include "Point.h"

using namespace std;

int main() {
    Point arr1[3]; // 디폴트 생성자로 생성된 Point 객체 3개를 갖는 배열, 요소 3개
    int i;
    for (i = 0; i < 3; i++) {
        arr1[i].print();
    }
    
    Point arr2[] = { {1, 2}, {3, 4} }; // int 2개 짜리 생성자로 생성된 객체, 요소 2개
    for (i = 0; i < 2; i++) {
        arr2[i].print();
    }
    
    Point* arr3[3]; // Point 주소를 요소로 갖는 3개짜리 배열
    int x, y;
    for (i = 0; i < 3; i++) {
        cout << "x : ";
        cin >> x;
        cout << "y : ";
        cin >> y;
        arr3[i] = new Point(x, y);
        arr3[i]->print();
    }
    
    for (i = 0; i < 3; i++) {
        delete arr3[i]; // 해제
    }
    
}

/*
x : 0
y : 0
(0, 0)

x : 0
y : 0
(0, 0)

x : 0
y : 0
(0, 0)

x : 1
y : 2
(1, 2)

x : 3
y : 4
(3, 4)

x : 5
y : 6
x : 5
y : 6
(5, 6)

x : 7
y : 8
x : 7
y : 8
(7, 8)

x : 9
y : 10
x : 9
y : 10
(9, 10)
*/
```

### class 배열 활용 주소록

- 기능
1. 추가 (번호 중복 X)
2. 번호로 검색
3. 수정 (전화번호만)
4. 삭제
5. 전체 목록

```cpp
// Phone.h
#pragma once
class Phone
{
private:
	int num;
	char* name;
	char* tel;

public:
	Phone();
	Phone(int, const char*, const char*);
	Phone(const Phone& p);
	~Phone();
	void setTel(const char*); // 전화번호 수정용, 새 전화번호만 받음
	int getNum(); // num을 외부에서 읽어옴, num을 받음
	void print();
};

class PhoneContainer {
private:
	Phone* phones[100]; // Phone 객체 주소 100개 저장할 배열 생성
	int cnt; // 추가될 배열의 위치(index)

public:
	PhoneContainer();
	~PhoneContainer();
	void addPhone(Phone* p);
	int getByNum(int num); // 검색할 num을 파라메터로 받아서 index의 위치 반환, 없으면 -1 반환
	void printList();
};

class PhoneService { // 기능을 제공하는 클래스
private:
	PhoneContainer container;
public:
	void add();
	void printAll();
};

// Phone.cpp
#include "Phone.h"
#include <iostream>
#include <cstring>

using namespace std;

Phone::Phone()
{

}

Phone::Phone(int num, const char* name, const char* tel):num(num)
{
	int nameLen = strlen(name) + 1;
	this->name = new char[nameLen];
	strcpy_s(this->name, nameLen, name);

	int telLen = strlen(tel) + 1;
	this->tel = new char[telLen];
	strcpy_s(this->tel, telLen, tel);
}

Phone::Phone(const Phone& p):num(p.num)
{
	int nameLen = strlen(p.name) + 1;
	this->name = new char[nameLen];
	strcpy_s(this->name, nameLen, p.name);

	int telLen = strlen(p.tel) + 1;
	this->tel = new char[telLen];
	strcpy_s(this->tel, telLen, p.tel);
}

Phone::~Phone() {
	//cout << "소멸자 호출\n" << endl;
	delete[]name;
	delete[]tel;
}

void Phone::setTel(const char* tel)
{
	delete[]this->tel;

	int telLen = strlen(tel) + 1;
	this->tel = new char[telLen];
	strcpy_s(this->tel, telLen, tel);
}

int Phone::getNum() // 현재의 num은 변경할 수 없는 private 값이므로 이런식으로 불러도 상관 없음
{
	return num;
}

void Phone::print()
{
	cout << "num : " << num << "\n"
		<< "name : " << name << "\n"
		<< "tel : " << tel << endl;
}

PhoneContainer::PhoneContainer():cnt(0)
{
}

PhoneContainer::~PhoneContainer()
{
	for (int i = 0; i < cnt; i++) {
		delete phones[i];
	}
}

void PhoneContainer::addPhone(Phone* p)
{
	if (cnt >= 100) {
		cout << "공간이 없습니다" << endl;
		return;
	}
	phones[cnt++] = p;
}

int PhoneContainer::getByNum(int num)
{
	int i;
	for (i = 0; i < cnt; i++) {
		if (num == (*phones[i]).getNum()) {
			return i;
		}
	}
	return -1;
}

void PhoneContainer::printList()
{
	int i;
	for (i = 0; i < cnt; i++) {
		(*phones[i]).print();
	}
}

void PhoneService::add()
{
	int num, idx;
	char name[20];
	char tel[20];

	cout << "num : " << endl;
	cin >> num;

	idx = container.getByNum(num);
	if (idx >= 0) {
		cout << "이미 등록되어 있습니다." << endl;
		return;
	}

	cout << "name : " << endl;
	cin >> name;

	cout << "tel : " << endl;
	cin >> tel;

	Phone* p = new Phone(num, name, tel);
	container.addPhone(p);
}

void PhoneService::printAll()
{
	container.printList();
}

//main.cpp
#include <iostream>
#include "Phone.h"

using namespace std;

int main() {
    PhoneService ps;
    for (int i = 0; i < 3; i++) {
        ps.add();
    }
    ps.printAll();
}

// continue
```

---