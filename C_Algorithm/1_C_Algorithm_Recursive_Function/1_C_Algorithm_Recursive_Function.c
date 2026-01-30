// 재귀연습
#if 0

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
int cnt = 0;
void test() {
	printf("%d\n", ++cnt);
	test();
}
// 여기서 함수 호출은 스택 메모리를 사용하므로 일정 범위 내에서 반복을 멈추게 된다.
int main(void) {
	test();
	return 0;
}

#endif

#if 0

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
int cnt = 0;
void test(int sum) {
	int arr[10];
	printf("%d %d\n", sum, ++cnt);
	test(sum+cnt);
}

int main(void) {
	test(0);
	return 0;
}
// sum, arr라는 지역변수로 인해 재귀 호출 수가 더 줄어들게 됨.
// 함수 자체는 스택 메모리를 사용한다. -> 지역 변수를 사용하게 되면 메모리가 줄어든다.
#endif

#if 0

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>

int N = 5;
void test2(int L) {
	if (L > N) {
		return;
	}// 재귀 종료 조건
	printf("L = %d\n", L);
	test2(L + 1);
}
int main(void) {
	test2(0);
	return 0;
}
// 1, 2, 3, 4, 5
#endif

#if 0

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
int N = 5;
void test3(int L) {
	if (L > N) {
		return;
	}
	test3(L + 1);
	printf("L = %d\n", L);
}
int main(void) {
	test3(1);
	return 0;
}
//5, 4, 3, 2, 1
#endif

#if 0

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
int N = 5;
void test4(int L) {
	if (L > N) {
		return;
	}
	printf("%d ", L);
	test4(L + 1);
	printf("%d ", L);
}
int main(void) {
	test4(1);
	return 0;
}
// 1 2 3 4 5 5 4 3 2 1
#endif

#if 0

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>

int N = 5;
void test5(int L) {
	if (L > N) {
		return;
	}
	if (L % 2 == 1){
		printf("%d ", L); // 오름차순
	}
	test5(L + 1);
	if (L % 2 == 0) {
		printf("%d ", L); // 내림차순
	}
}
/* 다른 방식 풀이
void test5(int L) {
	if (L > N) {
		return;
	}
	
	printf("%d ", L); // 오름차순

	test5(L + 2);
	
	if (L > 0) printf("%d ", L); // 내림차순
	
}
*/

int main(void) {
	test5(1);
	return 0;
}
// 1 3 5 4 2
#endif

#if 0

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>

int N = 5;
void test6(int L) {
	if (L > N) {
		return;
	}
	printf("%d ", L);
	test6(L + 1);
	printf("%d ", N - L + 1);
}
int main(void) {
	test6(1);
	return 0;
}

#endif

#if 0

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>

int N = 5;
void test7(int L) {
	if (L > N) {
		return;
	}
	for (int i = 1; i <= L; ++i) {
		printf("%d ", i);
	}
	printf("\n");
	test7(L + 1);
	
}
int main(void) {
	test7(1);
	return 0;
}
/*
1
1 2
1 2 3
1 2 3 4
1 2 3 4 5
*/
#endif

// 재귀호출 연습 2
#if 0

#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
int N = 4;
void test(int L) {
	if (L > N) {
		return;
	}
	printf("%d ", L);
	test(L + 1);
	test(L + 1);
}
int main(void) {
	test(1);
	return 0;
}
// 1 2 3 4 4 3 4 4 2 3 4 4 3 4 4
#endif