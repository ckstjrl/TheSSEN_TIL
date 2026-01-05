# C++ day04

날짜: 2026년 1월 5일

## 클래스 템플릿

```cpp
// templateArr.cpp

#include <iostream>
using namespace std;

class Point {
public:
    int x;
    int y;
    Point() {}
    Point(int x, int y) :x(x), y(y) {}
    friend ostream& operator<<(ostream& os, const Point& s);
};
ostream& operator<<(ostream& os, const Point& s) {
    os << "(" << s.x << ", " << s.y << ")";
    return os;
}

template<typename T>
class MyArr {
    T* arr;  //임의의 타입 배열 주소
    int len; //배열의 길이

   // MyArr(const MyArr& m) {} //복사 생성자
  //  MyArr& operator=(const MyArr& m) {}//대입 연산자

public:
    MyArr() {
        arr = new T[6];
        len = 6;
    }
    MyArr(int len) {
        arr = new T[len];
        this->len = len;
    }
    //요소 접근 연산자
    const T& operator[](int idx) const {
        if (idx < 0 || idx >= len) {
            cout << "outofbounds exception" << endl;
            exit(-1);
        }
        return arr[idx];
    }

    //요소 접근 연산자
    T& operator[](int idx) {
        if (idx < 0 || idx >= len) {
            cout << "outofbounds exception" << endl;
            exit(-1);
        }
        return arr[idx];
    }

    int getLen() {
        return len;
    }

    ~MyArr() {
        delete[]arr;
    }
};

int main()
{
    MyArr<Point> a;
    cout << a.getLen() << endl;
    a[0] = Point(1, 2);
    a[1] = Point(3, 4);
    int i;
    for (i = 0; i < 2; i++) {
        cout << a[i] << endl;
    }

    MyArr<int> b(3);
    for (i = 0; i < b.getLen(); i++) {
        b[i] = i + 1;
    }

    for (i = 0; i < b.getLen(); i++) {
        cout << b[i] << endl;
    }
    return 0;
}
```

## 예외 처리

예외

- 프로그램  실행 중 발생하는 문제. 기본 동작은 중단.
- 0으로 나누기, 널포인터 예외. 해제한 메모리 또 해제...

예외처리

- 예외 발생 시 프로그램 중단을 막고 다음 코드로 진행할 수 있게 구성
- 문제발생 => 예외 던짐. => 예외 받아서 처리

예외 처리 구문

- try 블록: 예외 발생할 만한 코드를 작성
- throw: 예외 발생. 잘못된 코드가 감지 되면 예외 발생. 예외 던짐
- catch 블록: try 블록에서 던진 예외를 받아서 처리, 캐치 블록은 파람 타입과 동일한 예외 객체만 받음
- catch(...)블록: 모든 예외 받음

## STL 예제

```cpp
#include <iostream>
#include <algorithm>
#include <string>
#include <vector>
using namespace std;

class Member {
private:
	string name;
	string tel;
	string address;

public:
	Member() {
	}
	Member(string name, string tel, string address) {
		this->name = name;
		this->tel = tel;
		this->address = address;
	}
	void setName(string name) {
		this->name = name;
	}
	string getName() {
		return name;
	}
	void setTel(string tel) {
		this->tel = tel;
	}
	string getTel() {
		return tel;
	}
	void setAddress(string address) {
		this->address = address;
	}
	string getAddress() {
		return address;
	}
	void printMember() {
		cout << "name:" << name << ", tel:" << tel << ", address:" << address
				<< endl;
	}

	friend bool operator ==(Member m1, Member m2) {
		return (m1.name == m2.name) ? true : false;
	}

};

void printMenu() {
	cout << "menu" << endl;
	cout << "1. 추가" << endl;
	cout << "2. 검색" << endl;
	cout << "3. 수정" << endl;
	cout << "4. 삭제" << endl;
	cout << "5. 전체출력" << endl;
	cout << "6. 전체삭제" << endl;
	cout << "7. 종료" << endl;
}

Member input() {
	string name, tel, address;
	cout << "이름:";
	cin >> name;
	cout << "전화번호:";
	cin >> tel;
	cout << "주소:";
	cin >> address;
	Member m(name, tel, address);
	return m;
}

int main() {
	int i, menu;
	bool flag = true;
	string name, tel, address;
	vector<Member> data;
	vector<Member>::iterator it;

	while (flag) {
		printMenu();
		cin >> menu;
		switch (menu) {
		case 1:
			data.push_back(input());
			break;
		case 2:
			cout << "검색할 사람 이름:";
			cin >> name;
			it = find(data.begin(), data.end(), Member(name, "", ""));
			if (it != data.end()) {
				it->printMember();
			} else {
				cout << "찾는 사람 없음" << endl;
			}
			break;
		case 3:
			cout << "수정할 사람 이름:";
			cin >> name;
			it = find(data.begin(), data.end(), Member(name, "", ""));
			if (it != data.end()) {
				it->printMember();
				cout << "새 전화번호:";
				cin >> tel;
				cout << "새 주소:";
				cin >> address;
				it->setTel(tel);
				it->setAddress(address);
				cout << "변경되었음" << endl;
				it->printMember();
			} else {
				cout << "찾는 사람 없음" << endl;
			}
			break;
		case 4:
			cout << "삭제할 사람 이름:";
			cin >> name;
			it = remove(data.begin(), data.end(), Member(name, "", ""));
			data.erase(it, data.end());
			break;
		case 5:
			if (data.empty()) {
				cout << "출력할 데이터가 없다." << endl;
			} else {
				for (it = data.begin(); it != data.end(); it++) {
					it->printMember();
				}
			}
			break;
		case 6:
			data.clear();
			break;
		case 7:
			flag = false;
			break;
		default:
			cout << "다시입력하라" << endl;
		}
	}

	return 0;
}

```