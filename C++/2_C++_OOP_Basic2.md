# C++ day02

날짜: 2025년 12월 30일

## 접근 제어자

- private  - 클래스 내에세만 보임. 은닉성 제공
    
    클래스 외부에서 다이렉트 접근 막음.
    
- `setter` - public 메서드로 private 멤버 변수의 값을 외부에서 변경하도록 제공
    
    ```cpp
    void setX(int x){
    	this->x = x;
    }
    ```
    
- `getter` - public 메서드로 private 멤버 변수의 값을 외부에 반환하는 함수
    
    ```cpp
    int getX() const{
    	return x;
    }
    ```
    
- `const` 변수를 상수화 하는 키워드
    
    ```cpp
    const int a = 10;
    //-------------------
    // 멤버 함수에도 붙일 수 있음
    // 멤버변수 변수 변경 불가
    // 읽기 전용 함수에 붙이는게 좋음.
    // const 멤버함수는 일반 멤버 함수 호출 불가
    // const는 오버로딩의 충족 요건이됨.
    // 호출 대상 객체가 const 객체이면 const함수 호출
    // const가 아니면 일반 함수 호출
    int getX() const{
    	return x;
    }
    ```
    
- `protected` 상속관계 클래스에만 보이고 다른 클래스에서는 안보임

## OOP 활용 전화번호부 만들기

```cpp
// Person.h
#pragma once
//vo(value object): 객체 1개의 값 저장
class Person
{
private:
	int num;
	char* name;
	char* tel;
public:
	Person();
	Person(int num, const char* name, const char* tel);
	Person(const Person& p); //복사 생성자
	~Person();
	void setTel(const char* tel);
	int getNum();
	void print();
};

//컨테이너 클래스. 객체에 대한 집합 처리 클리스
//배열이나 자료구조를 생성하여 여기에 객체를 추가, 검색, 수정, 삭제 작업
class PersonContainer {
private:
	Person* persons[100]; //Person객체 주소 100개 저장할 배열생성
	int cnt;  //추가될 배열의 위치(인덱스)
public:
	PersonContainer();
	~PersonContainer();
	void addPerson(Person* p);
	int getByNum(int num);//검색할 번호 받아서 방의 위치 반환. 없으면 -1반환
	Person* getPerson(int idx); //파람으로 방번호 받아서 객체 리턴
	void delPerson(int idx);     //삭제할 방번호 파람으로 받아서 배열에서 삭제
	void printList();
};

//service클래스: 비즈니스 로직 제공
class PersonService {
private:
	PersonContainer container;
public:
	void add();//추가
	void printAll();//전체목록
	void get();//검색
	void edit();//수정
	void del();//삭제
};

//Person.cpp
#include "Person.h"
#include<cstring>
#include<iostream>
using namespace std;

Person::Person()
{
}

Person::Person(int num, const char* name, const char* tel) :num(num)
{
	int nameLen = strlen(name) + 1;
	this->name = new char[nameLen];
	strcpy_s(this->name, nameLen, name);

	int telLen = strlen(tel) + 1;
	this->tel = new char[telLen];
	strcpy_s(this->tel, telLen, tel);

}

Person::Person(const Person& p) :num(p.num)
{
	int nameLen = strlen(p.name) + 1;
	this->name = new char[nameLen];
	strcpy_s(this->name, nameLen, p.name);

	int telLen = strlen(p.tel) + 1;
	this->tel = new char[telLen];
	strcpy_s(this->tel, telLen, p.tel);
}

Person::~Person()
{
	delete[]name;
	delete[]tel;
}

void Person::setTel(const char* tel)
{
	delete this->tel;
	int telLen = strlen(tel) + 1;
	this->tel = new char[telLen];
	strcpy_s(this->tel, telLen, tel);
}

int Person::getNum()
{
	return num;
}

void Person::print()
{
	cout << "num:" << num << " / name:" << name << " / tel:" << tel << endl;
}

PersonContainer::PersonContainer() :cnt(0)
{
}

PersonContainer::~PersonContainer()
{
	for (int i = 0; i < cnt; i++) {
		delete persons[i];
	}
}

void PersonContainer::addPerson(Person* p)
{
	if (cnt >= 100) {
		cout << "방이 찼다. 추가 취소" << endl;
		return;
	}
	persons[cnt++] = p;
}

int PersonContainer::getByNum(int num)
{
	int i;
	for (i = 0; i < cnt; i++) {
		if (num == (*persons[i]).getNum()) {
			return i;
		}
	}
	return -1;
}

Person* PersonContainer::getPerson(int idx)
{
	if (idx < 0)
		return NULL;
	return persons[idx];
}

void PersonContainer::delPerson(int idx)
{
	int i;
	delete persons[idx];
	for (i = idx; i < cnt - 1; i++) {
		persons[i] = persons[i + 1];
	}
	cnt--;
}

void PersonContainer::printList()
{
	int i;
	for (i = 0; i < cnt; i++) {
		(*persons[i]).print();
	}
}

void PersonService::add()
{
	int num, idx;
	char name[20];
	char tel[20];
	cout << "추가" << endl;
	cout << "num:";
	cin >> num;
	//번호 중복체크
	idx = container.getByNum(num); //번호 있으면 방번호(0이상의 정수). 없으면 -1반환
	if (idx >= 0) {
		cout << "중복된 번호. 추가 취소" << endl;
		return;
	}
	cout << "name:";
	cin >> name;
	cout << "tel:";
	cin >> tel;
	//new로 Person크기만큼 힙에 할당 받고 -> 생성자 호출. 멤버 초기화 
	//-> 대입 연산자로 new의 반환값이 할당받은 주소를 p에 할당
	Person* p = new Person(num, name, tel);
	container.addPerson(p);
}

void PersonService::printAll()
{
	cout << "전체목록" << endl;
	container.printList();
}
void PersonService::get() {
	int num;
	cout << "검색" << endl;
	cout << "num:";
	cin >> num;
	//번호 중복체크
	int idx = container.getByNum(num); //번호 있으면 방번호(0이상의 정수). 없으면 -1반환
	if (idx < 0) {
		cout << "없는 번호. 검색 취소" << endl;
		return;
	}
	container.getPerson(idx)->print();
}
void PersonService::edit() {
	int num;
	cout << "수정" << endl;
	cout << "num:";
	cin >> num;
	//번호 중복체크
	int idx = container.getByNum(num); //번호 있으면 방번호(0이상의 정수). 없으면 -1반환
	if (idx < 0) {
		cout << "없는 번호. 수정 취소" << endl;
		return;
	}

	Person* person = container.getPerson(idx);
	cout << "수정전 정보" << endl;
	person->print();

	cout << "new tel:";
	char tel[20];
	cin >> tel;
	person->setTel(tel); //검색된 객체의 전화번호 수정

	cout << "수정후 정보" << endl;
	person->print();
}
void PersonService::del() {
	int num;
	cout << "삭제" << endl;
	cout << "num:";
	cin >> num;
	//번호 중복체크
	int idx = container.getByNum(num); //번호 있으면 방번호(0이상의 정수). 없으면 -1반환
	if (idx < 0) {
		cout << "없는 번호. 삭제 취소" << endl;
		return;
	}

	container.delPerson(idx);
}

//main.cpp
#include <iostream>
#include "Person.h"
using namespace std;
int main()
{
    PersonService ps;
    bool flag = true;
    int m;
    while (flag) {
        cout << "1.추가 2.검색 3.수정 4.삭제 5.목록 6.종료" << endl;
        cin >> m;
        switch (m) {
        case 1:
            ps.add();
            break;
        case 2:
            ps.get();
            break;
        case 3:
            ps.edit();
            break;
        case 4:
            ps.del();
            break;
        case 5:
            ps.printAll();
            break;
        case 6:
            flag = false;
            break;
        }
    }
}

```

## const 함수 예제

```cpp

// Test.h
#pragma once
class Test
{
private:
	int x;
	int y;
public:
	Test();
	Test(int x, int y);
	void print()const;
	void print();
	void setX(int x);
	void setY(int y);
	int getX()const;
	int getY()const;
	//오버로딩
	void f1();
	void f1(int a);	
};
//const 멤버 함수는 멤버 변수 수정 불가

// Test.cpp
#include "Test.h"
#include<iostream>
using namespace std;
Test::Test():x(0),y(0)
{
}

Test::Test(int x, int y):x(x),y(y)
{
}

void Test::print() const
{
	//x = 10;	//error. const함수에서 멤버변수 값 쓰기 안됨
	//setX(3);  //const 함수에서 일반함수 호출 못함
	cout << "const print()" << endl;
	cout << "x:" << getX() << " / y:" << y << endl; //const에서 const함수 호출 가능
}

void Test::print()
{
	cout << "일반 print()" << endl;
	cout << "x:" << x << " / y:" << y << endl;
}

void Test::setX(int x)
{
	this->x = x;
}

void Test::setY(int y)
{
	this->y = y;
}

int Test::getX() const
{
	return x;
}

int Test::getY() const
{
	return y;
}

void Test::f1()
{
}

void Test::f1(int a)
{
}

// main.cpp
#include <iostream>
#include "Test.h"
using namespace std;
int main()
{
    Test t; //일반객체
    t.setX(1);  //일반 멤버 함수 호출 가능
    t.setY(2);
    t.print();  //const 멤버 함수 호출 가능
    t.f1();

    const Test t2(4,5); //const 객체는 const 멤버 함수만 호출 가능
    cout << "t2.x:" << t2.getX() << " / t2.y:" << t2.getY() << endl;
    t2.print();
   
}
```

## static 멤버 예제

```cpp
// Test.h
#pragma once

class Test
{
public:
	int x = 0;  //일반 멤버 변수
	static int y;  //static 멤버 변수
	void add();
	void print();
};
class Product {
private:
	int num;  //제품 시리얼 넘버. 자동할당
	char name[20];
	static int cnt; //생성된 객체를 카운팅. 이 값을 num에 할당
public:
	//생성자
	Product();
	Product(const char* name);
	void print();
};

// Test.cpp
#include "Test.h"
#include<iostream>
#include<cstring>
using namespace std;

int Test::y = 0;  //전역 선언으로 초기화 코드 작성
int Product::cnt = 0;

void Test::add()
{
	x++;
	y++;
}
void Test::print()
{
	cout << "x:" << x << ", y:" << y << endl;
}
Product::Product()
{
}

Product::Product(const char* name)
{
	num = ++cnt;
	strcpy_s(this->name, strlen(name)+1, name);
}
void Product::print() {
	cout << "num:" << num << " / name:" << name << endl;
}

// main.cpp
#include <iostream>
#include "Test.h"
using namespace std;
int main()
{
    cout << "객체 생성전 y:"<< Test::y << endl;
    Test t1, t2, t3;
    t1.add();
    t1.print();

    t2.add();
    t2.print();

    t3.add();
    t3.print();

    Product p1("aaa");
    Product p2("bbb");
    Product p3("ccc");

    p1.print();
    p2.print();
    p3.print();
}
```

## static 함수, static 멤버 함수

```cpp
// StaticTest.h
#pragma once
class StaticTest
{
public:
	int x;
	static int y;

	void f1();
	static void f2();
	void f3();
	static void f4();
};

// StaticTest.cpp
#include "StaticTest.h"
int StaticTest::y = 0;

// 일반함수, 일반 멤버 변수 ,static 멤버 변수 모두 사용 가능
void StaticTest::f1()
{
	x = 1;
	y = 2;
}
// static 함수는 static 멤버 변수만 사용 가능
// static 함수도 객체 생성 전에 호출 가능, 일반 멤버는 존재하지 않음 -> 사용 불가
void StaticTest::f2() {
	// x = 1; ERROR
	y = 3;
}

// 일반 멤버 함수는 일반 멤버 함수와 static 멤버 함수 모두 호출 가능
void StaticTest::f3() {
	f1();
	f2();
}

// static 함수는 일반 멤버 함수 호출 불가, static 멤버 함수는 호출 가능
void StaticTest::f4() {
	// f1(); ERROR
	f2();
}

// main.cpp
#include "StaticTest.h"
#include <iostream>
#include <cstring>

using namespace std;

int main() {
    StaticTest::f2();
    StaticTest::f4(); // 가능
    // StaticTest::f1(); 불가능

    StaticTest st;
    st.f1();
    st.f2();
    st.f3();
    st.f4();

}
```

## 상속

코드의 재사용성을 높임

다형성 구현

학사관리

학생 / 교수 / 교직원

학생정보: 학번, 이름, 학과, 수강과목

교수정보: 사번, 이름, 학과, 개설과목

교직원정보: 사번, 이름, 부서, 직무

```cpp
class Subject{
public:
   int subNum; //과목번호
   string name; //과목명
}
class Student{
public:
   int num; //학번
   string name; //이름
   string dept; //학과
   Subject subs[3]; //수강과목
}

class Prof{
public:
   int num; //교수번호
   string name; //이름
   string dept; //학과
   Subject subs[3]; //개설과목
}

class Staff{
public:
   int num; //사번
   string name; //이름
   string dept; //부서
   string job;
}
```

계속 반복되는 부분이 존재함.

이 요소를 뽑아서 상위 class로 만들고 상속 받는 형태로 제작

```cpp
class Person{
public:
   int num; //사번
   string name; //이름
   string dept; //부서

   void print(){
      cout<<"num:"<<num<<endl;
      cout<<"name:"<<name<<endl;
      cout<<"dept:"<<dept<<endl;
   }
}
```

- 상속의 문법
    
    자식 클래스 명 : 접근제어자 부모 클래스명
    
    부모 클래스의 private멤버와 생성자를 제외한 멤버들을 물려받음
    
    접근 제어자(private, protected, public)
    
    public 상속
    
    - 원본의 접근제어자를 그대로 물려받음
    
    protected 상속
    
    - 원본 protected 이상의 (protected/public)은 모두protected로 물려
    받음
    
    private 상속
    
    - 모두 private으로 물려받음

## 상속 예시 - 포켓몬

```cpp
#pragma once
#include <iostream>
using namespace std;
// 부모 클래스
class Pocketmon
{
protected:
	string name;
	int hp, exp, lv;

public:
	// 가상 함수 -> 호출시 동적 결합, runtime시 결정됨 / 객체에 함수가 선언되어 있다면 그 함수 실행, 만약 선언 X 부모의 함수 실행
	virtual void eat() { 
		cout << name << "밥 먹음" << endl;
	}
	virtual void sleep() {
		cout << name << "잠잠" << endl;
	}
	virtual bool play(){
		cout << name << "놀기" << endl;
		return true;
	}
	virtual bool exc() {
		cout << name << "운동하기" << endl;
		return true;
	}

	virtual void printState() {
		cout << name << "상태" << endl;
		cout << "hp : " << hp << ", exp : " << exp << ", lv : " << lv << endl;
	}

	virtual void lvCheck() {
		if (exp >= 20) {
			lv++;
			exp -= 20;
			cout << name << "의 레벨 1 증가됨" << endl;
			printState();
		}
	}
};

class Picachu : public Pocketmon {
public:
	Picachu() {
		name = "picachu";
		hp = 20;
		exp = 0;
		lv = 0;
		printState();
	}
	void eat() {
		Pocketmon::eat();
		hp += 5;
	}
	void sleep() {
		Pocketmon::sleep();
		hp += 10;
	}
	bool play() {
		hp -= 6;
		exp += 5;
		Pocketmon::lvCheck();
		Pocketmon::printState();
		cout << name << "놀기" << endl;
		return hp > 0;
	}
	bool exc() {
		hp -= 10;
		exp += 7;
		Pocketmon::lvCheck();
		Pocketmon::printState();
		cout << name << "운동하기" << endl;
		return hp > 0;
	}

	void lvCheck() {
		if (exp >= 20) {
			lv++;
			exp -= 20;
			Pocketmon::lvCheck();
			Pocketmon::printState();
		}
	}
};

class Squirtle : public Pocketmon {
public:
	Squirtle() {
		name = "squirtle";
		hp = 30;
		exp = 0;
		lv = 0;
		printState();
	}
	void eat() {
		Pocketmon::eat();
		hp += 5;
	}
	void sleep() {
		Pocketmon::sleep();
		hp += 10;
	}
	bool play() {
		hp -= 6;
		exp += 5;
		Pocketmon::lvCheck();
		Pocketmon::printState();
		cout << name << "놀기" << endl;
		return hp > 0;
	}
	bool exc() {
		hp -= 10;
		exp += 7;
		Pocketmon::lvCheck();
		Pocketmon::printState();
		cout << name << "운동하기" << endl;
		return hp > 0;
	}

	void lvCheck() {
		if (exp >= 20) {
			lv++;
			exp -= 20;
			Pocketmon::lvCheck();
			Pocketmon::printState();
		}
	}
};

class Bulbasaur : public Pocketmon {
public:
	Bulbasaur() {
		name = "Bulbasaur";
		hp = 20;
		exp = 0;
		lv = 0;
		printState();
	}
	void eat() {
		Pocketmon::eat();
		hp += 5;
	}
	void sleep() {
		Pocketmon::sleep();
		hp += 10;
	}
	bool play() {
		hp -= 6;
		exp += 5;
		Pocketmon::lvCheck();
		Pocketmon::printState();
		cout << name << "놀기" << endl;
		return hp > 0;
	}
	bool exc() {
		hp -= 10;
		exp += 7;
		Pocketmon::lvCheck();
		Pocketmon::printState();
		cout << name << "운동하기" << endl;
		return hp > 0;
	}

	void lvCheck() {
		if (exp >= 20) {
			lv++;
			exp -= 20;
			Pocketmon::lvCheck();
			Pocketmon::printState();
		}
	}
};

class Menu {
private:
	Pocketmon* p;  
	// 업캐스팅 포인터
	// 실제 객체는 Picachu / Squirtle / Bulbasaur 이지만
	// 부모 타입(Pocketmon*)으로 다루기 위해 선언
	// → 다형성(virtual 함수)을 사용하기 위한 핵심 포인트

public:
	void run() {
		cout << "캐릭터 선택\n1. 피카츄(default) 2. 꼬부기 3. 이상해씨" << endl;

		int ch;
		cin >> ch;

		// === 캐릭터 선택 ===
		switch (ch) {
		case 2:
			p = new Squirtle();
			// Squirtle 객체 생성 후
			// 부모 포인터(Pocketmon*)에 저장 → 업캐스팅 발생
			break;

		case 3:
			p = new Bulbasaur();
			// Bulbasaur 객체 생성 후 업캐스팅
			break;

		default:
			p = new Picachu();
			// 입력이 1이거나 잘못된 값이면 기본값 Picachu
			break;
		}

		bool flag = true; 
		// 캐릭터 생존 여부 및 게임 루프 제어용 플래그

		while (flag) {
			cout << "1. 밥 먹기, 2. 잠자기, 3. 놀기, 4. 운동하기, 5. 상태확인, 6. 종료" << endl;
			cin >> ch;

			switch (ch) {
			case 1:
				p->eat();
				// virtual 함수
				// → p의 타입(Pocketmon*)이 아니라
				//    실제 생성된 객체 타입(Picachu/Squirtle/Bulbasaur)의 eat() 호출
				break;

			case 2:
				p->sleep();
				// 가상 함수 → 동적 바인딩
				break;

			case 3:
				flag = p->play();
				// play()는 체력 감소 후
				// hp > 0 여부를 반환
				// false면 캐릭터 사망 처리
				if (!flag) {
					cout << "캐릭터 사망" << endl;
				}
				break;

			case 4:
				flag = p->exc();
				// 운동 역시 virtual 함수
				// 자식 클래스에 정의된 로직이 실행됨
				if (!flag) {
					cout << "캐릭터 사망" << endl;
				}
				break;

			case 5:
				p->printState();
				// 상태 출력
				// Bulbasaur는 printState를 오버라이딩했기 때문에
				// 해당 클래스의 함수가 호출됨
				break;

			case 6:
				flag = false;
				// 루프 종료
				break;
			}
		}

		// ⚠️ 현재 코드에는 delete p; 가 없음
		// 실제 프로그램에서는
		// delete p;
		// 를 추가하고, 부모 클래스에 virtual 소멸자를 두는 것이 안전함
	}
};

```