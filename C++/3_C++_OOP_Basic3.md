# C++ day03

날짜: 2025년 12월 31일

## 상속 포켓몬 게임 예제

```cpp
// PocketMon.h
#pragma once
#include<iostream>
using namespace std;
//부모 클래스
class PocketMon  // 개념. 상속이 목적
{
protected:
	string name;
	int hp, exp, lv;
public:
	virtual void eat() = 0; //순수 가상 함수. 현재 이 클래스로 객체 생성할 일 없고 상속이 목적
	virtual void sleep() = 0;
	virtual bool play() = 0;
	virtual bool exc() = 0;
	virtual void lvCheck() = 0;

	void printState() {
		cout << name << " 상태" << endl;
		cout << "hp:" << hp << ", exp:" << exp << ", lv:" << lv << endl;
	}
};

class Picachu :public PocketMon {
public:
	Picachu() {
		name = "picachu";
		hp = 20;
		exp = 0;
		lv = 1;
		printState();
	}
	void eat() {
		//PocketMon::eat();
		hp += 5;
		printState();
	}
	void sleep() {
		//PocketMon::sleep();
		hp += 10;
		printState();
	}
	bool play() {
		//PocketMon::play();
		hp -= 6;
		exp += 5;
		lvCheck();
		printState();
		return hp>0;
	}
	bool exc() {
		//PocketMon::exc();
		hp -= 10;
		exp += 7;
		lvCheck();
		printState();
		return hp > 0;
	}

	void lvCheck() {
		//PocketMon::lvCheck();
		if (exp >= 20) {
			lv++;
			exp -= 20;
			cout << name << "의 레벨 1증가됨" << endl;
			printState();
		}
	}
	void elec() {
		cout << name << " 백만볼트 공격" << endl;
	}
};

class Gobook :public PocketMon {
public:
	Gobook() {
		name = "Gobook";
		hp = 30;
		exp = 0;
		lv = 1;
		printState();
	}
	void eat() {
		//PocketMon::eat();
		hp += 7;
		printState();
	}
	void sleep() {
		//PocketMon::sleep();
		hp += 15;
		printState();
	}
	bool play() {
		//PocketMon::play();
		hp -= 3;
		exp += 6;
		lvCheck();
		printState();
		return hp > 0;
	}
	bool exc() {
		//PocketMon::exc();
		hp -= 6;
		exp += 9;
		lvCheck();
		printState();
		return hp > 0;
	}

	void lvCheck() {
		//PocketMon::lvCheck();
		if (exp >= 30) {
			lv++;
			exp -= 30;
			cout << name << "의 레벨 1증가됨" << endl;
			printState();
		}
	}
	void water() {
		cout << name << " 물대포 공격" << endl;
	}
};

class Lee :public PocketMon {
public:
	Lee() {
		name = "Lee";
		hp = 10;
		exp = 0;
		lv = 1;
		printState();
	}
	void eat() {
		//PocketMon::eat();
		hp += 8;
		printState();
	}
	void sleep() {
		//PocketMon::sleep();
		hp += 20;
		printState();
	}
	bool play() {
		//PocketMon::play();
		hp -= 7;
		exp += 3;
		lvCheck();
		printState();
		return hp > 0;
	}
	bool exc() {
		//PocketMon::exc();
		hp -= 12;
		exp += 6;
		lvCheck();
		printState();
		return hp > 0;
	}

	void lvCheck() {
		//PocketMon::lvCheck();
		if (exp >= 15) {
			lv++;
			exp -= 15;
			cout << name << "의 레벨 1증가됨" << endl;
			printState();
		}
	}
	void nung() {
		cout << name << " 넝쿨공격" << endl;
	}
};

class Menu {
private:
	PocketMon* p;
	
public:
	void run() {
		cout << "캐릭터 선택\n1.피카추(기본) 2.꼬부기 3.이상해씨" << endl;
		int ch;
		cin >> ch;
		//각 캐릭터 객체가 업캐스팅되어 p에 저장
		//업캐스팅된 객체의 virtual 함수를 호출하면 재정의 된 함수가 호출됨
		switch (ch) {
		case 2:
			p = new Gobook();
			break;
		case 3:
			p = new Lee();
			break;
		default:
			p = new Picachu();
			break;
		}

		bool flag = true;
		while (flag) {
			cout << "1.밥먹기 2.잠자기 3.놀기 4.운동하기 5.상태확인 6.종료 7.특기공격" << endl;
			cin >> ch;
			switch (ch) {
			case 1:
				p->eat();
				break;
			case 2:
				p->sleep();
				break;
			case 3:
				flag = p->play();
				if (!flag)
					cout << "캐릭터 사망" << endl;
				break;
			case 4:
				flag = p->exc();
				if (!flag)
					cout << "캐릭터 사망" << endl;
				break;
			case 5:
				p->printState();
				break;
			case 6:
				flag = false;
				break;
			case 7:
				//피카추=>elec()
				//꼬부기=>water()
				if (typeid(*p) == typeid(Picachu)) {
					((Picachu*)p)->elec();
				}else if (typeid(*p) == typeid(Gobook)) {
					((Gobook*)p)->water();
				}
				else {
					((Lee*)p)->nung();
				}				
				break;
			}
		}
	}
};
```

## 순수 가상 함수

- 구현을 하지 않은 함수
- 함수 선언 뒤에 `=0` 을 붙임
- 상속을 목적으로 사용
- 하위 클래스에서 구현 필수

```cpp
class Base {
public:
    virtual void func() = 0;
};
```

- 행위의 규약 역할

## 추상 클래스

- 순수 가상 함수를 1개 이상 포함한 클래스
- 불완전 클래스
- 객체 생성 불가

```cpp
class Animal {
public:
    virtual void sound() = 0;
};

class Dog : public Animal {
public:
    void sound() override {
        cout << "멍멍" << endl;
    }
};
```

- 상속 받은 클래스에서 모든 순수 가상 함수를 구현해야 객체 생성 가능
- 하위 클래스에 설계 기준 제공

## 다중 상속

- 하나의 클래스가 여러 부모 클래스를 상속

```cpp
class Child : public Parent1, public Parent2 {
};
```

장점

- 다양한 기능을 동시에 상속 가능
- 코드 재사용성 증가

단점

- 다이아몬드 문제 발생

```css
    A
   / \
  B   C
   \ /
    D

```

- A의 멤버를 B, C를 통해 **중복 상속**
- 어떤 A의 멤버인지 **모호성 발생 → 컴파일 에러**
- `virtual inheritance` 로 해결 가능

## 주소록 프로그램 설계

- 한 명 정보: 이름(중복 X), 전화, 주소
- 기능
    - 추가
    - 이름 검색
    - 수정(전화, 주소)
    - 삭제
    - 전체 출력
    - 종료

```cpp
// Member.h
#pragma once
#include <string>
#include <iostream>
using namespace std;

class Member {
private:
    string name;
    string tel;
    string address;

public:
    Member() {}
    Member(string name, string tel, string address)
        : name(name), tel(tel), address(address) {}

    string getName() { return name; }
    void setTel(string tel) { this->tel = tel; }
    void setAddress(string address) { this->address = address; }

    void print() {
        cout << name << " / " << tel << " / " << address << endl;
    }
};

// MemberDao.cpp
#pragma once
#include "Member.h"

class MemberDao {
private:
    Member addr[30];
    int count = 0;

public:
    void insert(const Member& m);
    Member* findByName(string name);
    void update(string name, string tel, string address);
};

// MemberDao.cpp
#include "MemberDao.h"

void MemberDao::insert(const Member& m) {
    addr[count++] = m;
}

Member* MemberDao::findByName(string name) {
    for (int i = 0; i < count; i++) {
        if (addr[i].getName() == name)
            return &addr[i];
    }
    return nullptr;
}

void MemberDao::update(string name, string tel, string address) {
    Member* m = findByName(name);
    if (m) {
        m->setTel(tel);
        m->setAddress(address);
    }
}

// MemberService.h
#pragma once
#include "MemberDao.h"

class MemberService {
private:
    MemberDao dao;

public:
    void addMember();
};

// MemberService.cpp
#include "MemberService.h"
#include <iostream>
using namespace std;

void MemberService::addMember() {
    string name, tel, addr;
    cout << "이름: ";
    cin >> name;
    cout << "전화: ";
    cin >> tel;
    cout << "주소: ";
    cin >> addr;

    Member m(name, tel, addr);
    dao.insert(m);
}
```

## 연산자 오버로딩

- 예제 1

```cpp
// Point.h
#pragma once
#include <iostream>
using namespace std;

class Point {
private:
    int x, y;

public:
    Point() : x(0), y(0) {}
    Point(int x, int y) : x(x), y(y) {}

    void print() {
        cout << x << ", " << y << endl;
    }

    friend Point operator+(const Point& p1, const Point& p2);
    friend Point& operator++(Point& p);      // 전위
    friend Point operator++(Point& p, int);  // 후위
    friend ostream& operator<<(ostream& os, const Point& p);
};

// Point.cpp
#include "Point.h"

Point operator+(const Point& p1, const Point& p2) {
    return Point(p1.x + p2.x, p1.y + p2.y);
}

Point& operator++(Point& p) {
    p.x++;
    p.y++;
    return p;
}

Point operator++(Point& p, int) {
    Point temp(p.x, p.y);
    p.x++;
    p.y++;
    return temp;
}

ostream& operator<<(ostream& os, const Point& p) {
    os << "x:" << p.x << ", y:" << p.y;
    return os;
}

// main.cpp
#include <iostream>
#include "Point.h"

int main() {
    Point p1 = Point(1, 2) + Point(3, 4);
    p1.print();

    ++p1;
    p1.print();

    Point p = p1++;
    p.print();
    p1.print();

    cout << p1 << endl;
}
```

- 예제2

```cpp
// Person.h
#pragma once
#include<cstring>
#include<iostream>
using namespace std;
class Person
{
private:
	int num;
	char* name;
public:
	//생성자
	Person() :num(0), name(NULL) {}
	Person(int num, const char* name) :num(num) {
		int len = strlen(name) + 1;
		this->name = new char[len];
		strcpy_s(this->name, len, name);
	}
	//복사 생성자
	Person(const Person& p) :num(p.num) {
		int len = strlen(p.name) + 1;
		name = new char[len];
		strcpy_s(name, len, p.name);
	}

	//소멸자
	~Person() {
		if (name != NULL) {
			delete[]name;
		}
	}

	//= 연산자 오버로딩
	//p1 = p3;
	Person& operator=(const Person& p) {
		num = p.num;
		delete[]name;
		int len = strlen(p.name) + 1;
		name = new char[len];
		strcpy_s(name, len, p.name);
		return *this;
	}
	
	// << 연산자 오버로딩
	friend ostream& operator<<(ostream& os, const Person& p);
};

// Person.cpp
#include "Person.h"

ostream& operator<<(ostream& os, const Person& p)
{
    // TODO: 여기에 return 문을 삽입합니다.
    os << "num:" << p.num << " / name:" << p.name;
    return os;
}

//main.cpp
#include <iostream>
#include "Person.h"
using namespace std;
int main()
{
    Person p1(1, "aaa");
    cout << "p1:" << p1 << endl;

    Person p2 = p1; //Person p2(p1)
    cout << "p2:" << p2 << endl;

    Person p3(3, "ccc");
    cout << "p3:" << p3 << endl;
    p1 = p3; //=연산자가 사용됨
}
```

## 템플릿

```cpp
#include <iostream>
#include<cstring>
using namespace std;

class Point {
private:
    int x, y;
public:
    Point() :x(0), y(0) {}
    Point(int x, int y) :x(x), y(y) {}
    Point operator+(const Point& p) {
        return Point(x + p.x, y + p.y);
    }
    bool operator>(const Point& p) {
        return x > p.x;
    }
    friend ostream& operator<<(ostream& os, const Point& p);
};
ostream& operator<<(ostream& os, const Point& p) {
    os << "x:" << p.x << ", y:" << p.y << endl;
    return os;
}

template<typename T>
T add(T a, T b) {
    return a + b;
}

//함수 템플릿 특수화(const char*)
template<>
const char* add(const char* a, const char* b) {
    string s(a);
    string s2(b);
    string s3(s + s2);
    const char* p = s3.c_str();
    char* p2 = new char[strlen(p) + 1];
    strcpy_s(p2, strlen(p) + 1, p);
    return p2;
}

template<>
char* add(char* a, char* b) {
    strcat_s(a, strlen(a) + strlen(b) + 1, b);
    return a;
}

template<typename T>
T _max(T a, T b) {
    return a > b ? a:b;
}

template<>
const char* _max(const char* a, const char* b) {
    return strlen(a) > strlen(b) ? a : b;
}

int main()
{
    /*
    int a = add<int>(1, 2);
    float b = add<float>(1.2f, 3.4f);
    double c = add<double>(5.6, 7.8);
    */
    int a = add(1, 2);
    float b = add(1.2f, 3.4f);
    double c = add(5.6, 7.8);
    Point d = add(Point(1, 2), Point(2, 3));
    string e = add(string("aaa"), string("bbb"));
    char buf1[20] = "abc";
    char buf2[] = "def";
    
    const char* p = add("aaa", "bbb");
    char* p2 = add(buf1, buf2);
    cout << "add<int>:" << a << endl;
    cout << "add<float>:" << b << endl;
    cout << "add<double>:" << c << endl;
    cout << "add<Point>:" << d << endl;
    cout << "add<string>:" << e << endl;
    cout << "add<const char*>:" << p << endl;
    cout << "add<char*>:" << p2 << endl;
    delete p;

    int m1 = _max(1, 2);
    double m2 = _max(4.5, 2.3);
    Point m3 = _max(Point(23, 3), Point(1, 2));
    const char* m4 = _max("adsf", "qwereqrte");
    cout << "m1:" << m1 << endl;
    cout << "m2:" << m2 << endl;
    cout << "m3:" << m3 << endl;
    cout << "m4:" << m4 << endl;

    /*
    string s1 = "asdf";
    string s2 = string("adf") + string("sdfg");
    char str[] = "qqwer";
    string s3(str);
    string s4(s1);
    string s5 = s3 + s4;
    cout << "s1:" << s1 << endl;
    cout << "s21:" << s2 << endl;
    cout << "s3:" << s3 << endl;
    cout << "s4:" << s4 << endl;
    cout << "s5:" << s5 << endl;
    */
}
```