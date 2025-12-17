# C언어 day08

날짜: 2025년 12월 17일

## 구조체 배열 + 동적할당

```c
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

struct student {
	int id;
	int* score; // int 배열 3개 짜리 주소
	char* name; // 문자열 주소
};

int main(void) {
	int people_num;
	int score_num;
	int name_length;

	(void)freopen("student2.txt", "r", stdin);
	(void)scanf("%d %d %d", &people_num, &score_num, &name_length);
	
	//동적할당
	int* scores_temp = (int*)calloc(score_num * people_num, sizeof(int));
	char* names_temp = (char*)calloc(20 * people_num, sizeof(char));
	struct student* stu = (struct student*)calloc(people_num, sizeof(struct student));
	//동적할당 실패시 종료 코드 필요

	
	//저장
	for (int i = 0; i < people_num; ++i) {
		(void)scanf("%d", &stu[i].id);
		
		stu[i].score = scores_temp;
		for (int j = 0; j < score_num; ++j) {
			(void)scanf("%d", &scores_temp[j]);
		}
		scores_temp += score_num;

		stu[i].name = names_temp;
		(void)scanf("%s", names_temp);
		names_temp += name_length;
	}
	//출력
	for (int i = 0; i < people_num; ++i) {
		printf("%d ", stu[i].id);
		for (int j = 0; j < score_num; ++j) {
			printf("%d ", stu[i].score[j]);
		}
		printf("%s\n", stu[i].name);
	}
	//해제
	free(stu[0].name);
	free(stu[0].score);
	free(stu);
	return 0;
}

```

## 구조체 멤버 정렬

- 성능 최적화와 데이터 접근의 효율성을 위한 것
- 구조체 메모리 할당 단위를 특정 배수 값에 위치한 메모리에 둠으로써 효율적으로 메모리를 접근하는 것
- 컴파일러와 시스템 아키텍처에 따라 다를 수 있음
- 구조체 멤버 정렬 규칙
    - 중첩 구조체를 포함한 멤버 중 가장 큰 자료형의 배수형으로 증가함
    - 멤버가 char, short, char[] 중 한가지로만 구성되어 있는 경우 필요한 바이트 만큼이 align 된다.
    - 메모리는 순서대로 배정받기 때문에, 멤버의 순서가 구조체 크기 결정에 영향을 준다.
- 멤버 중 가장 큰 자료형의 배수형으로 증가한다.
    - 메모리는 순서대로 배정받기 때문에 멤버의 순서가 구조체 크기 결정에 영향을 미친다.
    
    ```c
    struct mix1 {
    	double z;
    	int a;
    	char x;
    };// 16바이트 사용
    
    struct mix2 {
    	char x;
    	double y;
    	int a;
    }; // 24바이트 사용
    ```
    
- 중첩구조체를 포함한 멤버 중 가장 큰 자료형의 배수형으로 증가한다.
    
    ```c
    struct mix1 {
    	double z;
    	int a;
    	char x;
    };
    
    struct mix2{
    	char c, k, j;
    	struct mix1 m1;
    }; // 24 바이트 사용
    
    struct mix3 {
    	double z;
    	int a;
    	char x;
    	char c, k, j;
    }; // 16 바이트 사용
    ```
    
- 멤버가 char, short, char[] 중 한가지로만 구성된 경우
    
    ```c
    struct _c1{
    	char a[10];
    	char b[4];
    	char c[4];
    }c1; // 18
    
    struct c2 {
    	char a;
    	char b;
    	char c;
    	char d;
    	char e;
    }c2; // 5
    
    struct c3 {
    	char a;
    	char b;
    	char c;
    	char d;
    	char e;
    	int f;
    }c3; // 12
    
    struct c4{
    	short a;
    	short b;
    	short c;
    }c4; // 6
    
    struct mix1{
    	double z;
    	int a;
    	char x;
    }mix1; // 16
    
    struct mix2{
    	int a;
    	double z;
    	char x;
    }mix2; // 24
    
    struct mix3{
    	int a;
    	char x;
    	double z;
    }mix3; // 16
    
    struct mix4 {
    	int z;
    	int a;
    	char x;
    }mix4; //12
    ```
    
- #pragma pack(fm)에 의해 구조체 크기 증가의 최소단위를 설정
    
    ```c
    struct _c1{
    	char a[10];
    	char b[4];
    	char c[4];
    }c1; // 18
    
    struct c2 {
    	char a;
    	char b;
    	char c;
    	char d;
    	char e;
    }c2; // 5
    
    struct c3 {
    	char a;
    	char b;
    	char c;
    	char d;
    	char e;
    	int f;
    }c3; // 9
    
    struct c4{
    	short a;
    	short b;
    	short c;
    }c4; // 6
    
    struct mix1{
    	double z;
    	int a;
    	char x;
    }mix1; // 13
    
    struct mix2{
    	int a;
    	double z;
    	char x;
    }mix2; // 13
    
    struct mix3{
    	int a;
    	char x;
    	double z;
    }mix3; // 13
    
    struct mix4 {
    	int z;
    	int a;
    	char x;
    }mix4; // 9
    ```
    
- 비트 필드 구조체
    - 구조체를 이용한 비트 단위 설정
        - 정수 계열 형식보다 적은 저장소를 차지하는 멤버
        - 제한적 메모리를 가진 시스템이나 프로그래밍에서 바이트를 세분화 하여 구현할 때 사용
        - 비트 필드에는 주소연산자 X
        - 비트 필드 멤버 선언자에는 unsigned, signed, short, int 등이 올 수 있음
        
        ```c
        typedef struct {
        	unsigned int blue : 8;
        	unsigned int green : 8;
        	unsigned int red : 8;
        } color1;
        /*
        4바이트 구조체
        blue, green, red 라는 이름으로 24bit를 사용
        8bit는 사용하지 않음
        */
        
        typedef struct {
        	unsigned char blue;
        	unsigned char green;
        	unsigned char red;
        } color2;
        /*
        3바이트 구조체
        blue, green, red 라는 이름으로 각 1바이트(8bit)를 사용
        */
        ```
        

## 공용체 (UNION)

- 공용체
    - 구조체와 선언, 사용 방법이 동일
        
        But, 각 멤버들이 하나의 기억 공간을 공유함
        
    - 하나의 기억 공간에 저장된 데이터를 여러가지 형태로 사용해야 할 경우 사용
    - 구조체의 struct  대신 union 키워드 사용
    - 기억 공간의 크기는 크기가 가장 큰 멤버의 크기로 결정됨
    - 메모리를 절약할 수 잇는 장점이 있지만 부주의시 데이터 처리에 오류를 일으킬 수 있음
    - 멤버들이 공유하는 하나의 값만 초기화 가능
    
    ```c
    union student
    {
    	int num;
    	double grade;
    };
    ```