# C언어 day07

날짜: 2025년 12월 16일

## 메모리 동적 할당

- **정적 할당**: 컴파일 타임에 크기 고정 (배열, 지역 변수 등)
- **동적 할당**: 실행 중(런타임)에 Heap 영역에서 메모리 할당
- 필요성
    - 입력 개수를 사전에 알 수 없는 경우
    - 대용량 데이터 처리
    - 유연한 자료구조 구현

## malloc / calloc / realloc / free

### malloc

```c
int *p = (int*)malloc(N * sizeof(int));

```

- 지정한 바이트 수만큼 메모리 할당
- **초기화되지 않음** (쓰레기값 존재)
- 반환형: `void*` → 형변환 필요
- 실패 시 `NULL` 반환
- 사용 후 반드시 `free()` 필요

```c
// malloc

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include "basic_op.h"
int main(void) {

	int* pi = NULL;
	int size = 10;

	pi = (int*)malloc(size * sizeof(int));
	if (pi != NULL) {
		scanf_ary(pi, size);
		print_ary(pi, size);
		free(pi);
		pi = NULL;
		//free(pi);
		printf("%p %d\n", pi, pi[3]);
	}
	return 0;
}


```

### calloc

```c
int *p = (int*)calloc(N, sizeof(int));

```

- `count * size` 만큼 할당
- **0으로 초기화됨**
- 배열 할당에 유리

```c
//calloc

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#include "basic_op.h"
int main(void) {

	int* pi = NULL;
	int size = 10;
	int sum = 0;
	pi = (int*)calloc(size, sizeof(int));
	if (pi != NULL) {
		scanf_ary(pi, size);
		print_ary(pi, size);
		sum = sum_ary(pi, size);
		printf("sum = %d, avg = %.2f\n", sum, (double)sum/10);
		free(pi);
		pi = NULL;
		//free(pi);  // pi=NULL 을 사용하면 오류 없음
		//printf("%p %d\n", pi, pi[3]); // pi=NULL을 사용하면 pi[3]에서 오류 발생
	}
	return 0;
}


```

### free

```c
free(p);
p = NULL;

```

- Heap 관리 테이블에서 **사용 목록만 삭제**
- 포인터 변수에 남아있는 주소는 여전히 접근 가능 → **Dangling Pointer 위험**
- 관례적으로 `free()` 후 `NULL` 대입

### realloc

```c
int *tmp = realloc(p, new_size);
if (tmp != NULL) {
    p = tmp;
}

```

- 기존 메모리 크기 변경
- 경우의 수
    1. 뒤에 여유 공간 존재 → 기존 주소 유지
    2. 공간 부족 → 새 공간 할당 + 데이터 복사 + 기존 공간 해제
    3. 실패 → `NULL` 반환 (기존 주소 유지)

⚠️ 바로 `p = realloc(p, ...)` 금지

```c
//realloc

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
#include "basic_op.h"
typedef int* intP;

int main(void) {
	intP pi=NULL, ptemp=NULL;
	int i, N;
	int num;
	int step = 3;

	//N = 3;
	//pi = (int*)calloc(N, sizeof(int));
	//if (pi == NULL) {
	//	printf("메모리가 부족합니다.\n");
	//	exit(1);
	//}
	N = 0;
	pi = NULL;
	for (i = 0; scanf("%d", &num) && num > 0; ++i) {
		if (i == N) {
			N += step;
			ptemp = (int*)realloc(pi, N * sizeof(int));
			if (ptemp == NULL) {
				printf("메모리가 부족합니다.\n");
				free(pi);
				exit(1);
			}
			pi = ptemp;
			memset(pi+i, 0, step*sizeof(int));
			print_ary(pi, N);
		}
		pi[i] = num;
	}
	print_ary(pi, i);
	free(pi);
	pi = NULL;

	return 0;
}


```

## realloc을 이용한 가변 배열 구현

```c
for (i = 0; scanf("%d", &num) && num > 0; ++i) {
    if (i == N) {
        N += step;
        ptemp = realloc(pi, N * sizeof(int));
        if (ptemp == NULL) {
            free(pi);
            exit(1);
        }
        pi = ptemp;
        memset(pi + i, 0, step * sizeof(int));
    }
    pi[i] = num;
}

```

- 입력 개수 미정 상황에서 **동적 배열 확장 패턴**
- 실무/코딩테스트에서 매우 자주 사용

## 문자열 배열 동적 할당 (char **)

### 구조

- `char *` : 문자열 하나
- `char **` : 문자열 포인터들의 배열

```c
str = (char**)calloc(N, sizeof(char*));
str[i] = (char*)calloc(strlen(temp)+1, sizeof(char));

```

- 2단계 할당 구조
    1. 문자열 포인터 배열
    2. 각 문자열 실제 저장 공간

```c
// 문자열 배열 - 동적할당

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
#include "basic_op.h"
void* my_calloc(int N, int size);
void get_strings(int N, char** str);
void print_string(int N, char** str);
void destroy(int N, char** str);
int main(void) {
	char** str = NULL;
	int N;

	(void)freopen("data.txt", "r", stdin);
	(void)scanf("%d", &N);
	(void)getchar();
	str = (char**)my_calloc(N, sizeof(char*));
	get_strings(N, str);
	print_string(N, str);
	destroy(N, str);
	str = NULL;
	return 0;
}

void* my_calloc(int N, int size) {
	void *str = calloc(N, size);
	if (str == NULL) {
		printf("메모리가 부족합니다.\n");
		exit(0);
	}
	return str;
}

void get_strings(int N, char **str) {
	int i;
	char temp[80] = { 0 };
	for (i = 0; i < N; ++i) {
		gets(temp);
		str[i] = (char*)calloc(strlen(temp) + 1, sizeof(char));
		if (str[i] == NULL) {
			printf("메모리가 부족합니다.\n");
			for (int j = 0; j < i; ++j) {
				free(str[j]);
			}
			free(str);
			exit(0);
		}
		strcpy(str[i], temp);
	}
}

void print_string(int N, char **str) {
	int i;
	for (i = 0; i < N; ++i) {
		printf("%s\n", str[i]);
	}
}

void destroy(int N, char**str) {
	int i;
	for (i = 0; i < N; ++i) {
		free(str[i]);
	}
	free(str);
}

```

```c
// char **str : char *의 배열
// char *temp : char의 배열

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
#include "basic_op.h"
unsigned int get_filesize(char* filename);
void print_string(int N, char** str);
void* my_calloc(int N, int size);

int main(void) {
	char* filename = "data.txt";
	char** str = NULL;
	char* temp = NULL;
	int N;
	int used = 0;

	(void)freopen(filename, "r", stdin);
	(void)scanf("%d", &N);
	(void)getchar();
	str = (char**)my_calloc(N, sizeof(char*));
	temp = (char*)my_calloc(get_filesize(filename), sizeof(char));
	for (int i = 0; i < N; ++i) {
		str[i] = temp + used;
		gets(str[i]);
		used += strlen(str[i]) + 1;
	}
	print_string(N, str);
	free(str[0]);
	free(str);
	str = NULL;
	return 0;
}

void* my_calloc(int N, int size) {
	void* str = calloc(N, size);
	if (str == NULL) {
		printf("메모리가 부족합니다.\n");
		exit(0);
	}
	return str;
}

unsigned int get_filesize(char* filename) {
	unsigned int size = 0;
	FILE* fp = fopen(filename, "r");
	fseek(fp, 0, SEEK_END);
	size = ftell(fp);
	fclose(fp);
	return size;
}

void print_string(int N, char** str) {
	int i;
	for (i = 0; i < N; ++i) {
		printf("%s\n", str[i]);
	}
}



// 고정길이 문자열  (char (*str)[80])

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
#include "basic_op.h"
typedef  char (*C80A)[80];
void* my_calloc(int N, int size);
void get_strings(int N, C80A str);
void print_string(int N, C80A str);

int main(void) {
	C80A str = NULL;
	int N;
	(void)freopen("data.txt", "r", stdin);
	(void)scanf("%d", &N);
	(void)getchar();
	str = (char (*)[80]) my_calloc(N, 80);
	get_strings(N, str);
	print_string(N, str);
	free(str);
	str = NULL;
	return 0;
}

void* my_calloc(int N, int size) {
	void* str = calloc(N, size);
	if (str == NULL) {
		printf("메모리가 부족합니다.\n");
		exit(0);
	}
	return str;
}

void get_strings(int N, C80A str) {
	int i;
	for (i = 0; i < N; ++i) {
		//fgets(str[i], 80, stdin);
		//temp[str[i] - 1] = '\0';
		gets(str[i]);
	}
}

void print_string(int N, C80A str) {
	int i;
	for (i = 0; i < N; ++i) {
		printf("%s\n", str[i]);
	}
}



```

### 해제 순서 중요

```c
for (i = 0; i < N; i++) {
    free(str[i]);
}
free(str);

```

- **안쪽 → 바깥쪽** 순서로 해제

## 구조체 (struct)

### 기본 구조체

```c
struct student {
    int num;
    double grade;
    char name[35];
};

```

- 서로 다른 자료형을 하나의 묶음으로 표현
- 배열과 달리 멤버별 자료형이 다를 수 있음

### 구조체와 포인터

```c
void print_student(struct student* p) {
    p->num = 3;
    p->grade = 4.1;
}

```

- 구조체는 크기가 크므로 **포인터 전달 권장**
- `.` vs `>`
    - 변수: `s.num`
    - 포인터: `p->num`

## 구조체 + 동적 할당

```c
struct student {
    int id;
    int* scores;
    char* name;
};

```

- 구조체 내부 멤버도 동적 할당 가능
- 관리 포인트
    - 구조체 배열 할당
    - 내부 포인터 멤버 할당
    - 해제 시 **역순 해제**

```c
// 구조체

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
#include "basic_op.h"
struct student {
	int num;
	double grade;
	char name[35];
};
void print_student1(struct student temp);
void print_student2(struct student* p);
int main(void) { 
	struct student stu = {1, 4.5, "Soyoung"};
	stu.num = 2;
	stu.grade = 4.0;
	strcpy(stu.name, "JulieYoon");
	printf("%d %.1f %s\n", stu.num, stu.grade, stu.name);
	printf("%d\n", sizeof(stu));
	//print_student1(stu);
	print_student2(&stu);
	printf("%d %.1f %s\n", stu.num, stu.grade, stu.name);
	return 0;
}
void print_student2(struct student* p) {
	printf("%d\n", sizeof(p));
	p->num = 3;
	(*p).grade = 4.1;
	strcpy(p->name, "Tom");
	printf("%d %.1f %s\n", p->num, p->grade, p->name);
}
void print_student1(struct student temp) {
	printf("%d\n", sizeof(temp));
	temp.num = 3;
	temp.grade = 4.1;
	strcpy(temp.name, "Tom");
	printf("%d %.1f %s\n", temp.num, temp.grade, temp.name);
}


// 구조체 - 2

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
#include "basic_op.h"
struct profile {
	int age;
	double height;
	char* name;
};
struct student {
	struct profile pf;
	int num;
	double grade;
};
struct student2 {
	struct profile * ppf;
	int num;
	double grade;
};
void print_student(struct student2* std);
void print_student2(struct student2** stu);
int main(void) {
	struct profile p = { 0 };
	struct profile temp;
	struct student2 B = { 0 };
	struct student2* C = &B;
	B.ppf = &p;
	B.ppf->age = 20;
	B.ppf->height = 175.5;
	B.ppf->name = "Soyoung";
	B.num = 3;
	B.grade = 4.4;
	printf("%d %.1f %s %d %.1f\n", 
		B.ppf->age, B.ppf->height, B.ppf->name, B.num, B.grade);
	temp = *B.ppf;
	printf("%d %.1f %s\n", temp.age, temp.height, temp.name);
	print_student(&B);
	print_student2(&C);
#if 0
	struct student A = { 0 };
	A.pf.age = 20;
	A.pf.height = 170.2;
	A.pf.name = "Soyoung";
	A.num = 1;
	A.grade = 4.3;
	printf("%d %.1f %s %d %.1f\n", A.pf.age, A.pf.height, A.pf.name, A.num, A.grade);
	struct profile* p = &A.pf;
	printf("%d %.1f %s\n", p->age, p->height, p->name);
#endif
	return 0;
}
void print_student2(struct student2 **stu) {
	printf("age = %d\n", (*stu)->ppf->age);
	printf("age = %d\n", (**stu).ppf->age);
	printf("age = %d\n", stu[0]->ppf->age);
	printf("age = %d\n", stu[0][0].ppf->age);
}

void print_student(struct student2* stu) {
	printf("%d %.1f %s %d %.1f\n",
		stu->ppf->age, stu->ppf->height, stu->ppf->name, stu->num, stu->grade);
}


// 구조체 배열 + 동적할당

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
#include "basic_op.h"
struct student {
	int id;
	int* scores;
	char* name;
};

int main(void) {
	struct student* stu = NULL;
	int N, M, S;
	int *s_temp;
	char* n_temp;
	(void)freopen("student2.txt", "r", stdin);
	(void)scanf("%d %d %d", &N, &M, &S);
	//init_student();

	stu = (struct student*)calloc(N, sizeof(struct student));
	if (stu == NULL) {
		exit(0);
	}
	s_temp = (int*)calloc(N * M, sizeof(int));
	if (s_temp == NULL) {
		free(stu);
		exit(0);
	}
	n_temp = (char*)calloc(N * S, sizeof(char));
	if (n_temp == NULL) {
		free(stu);
		free(s_temp);
		exit(0);
	}
	// stu 배열 각 요소의 scores, name 멤버를 초기화
	// 입력
	// 출력
	free(stu[0].scores);
	free(stu[0].name);
	free(stu);
	return 0;
}



```