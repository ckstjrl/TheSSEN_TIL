// 재귀호출 3
#if 0
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>

int N = 3;
void test(int L) {
	if (L > N) {
		return;
	}
	printf("%d ", L);
	test(L + 1);
	test(L + 1);
	test(L + 1);
}
int main(void) {
	test(1);
	return 0;
}
// 1 2 3 3 3 2 3 3 3 2 3 3 3
#endif

// 중첩 for문을 재귀호출로 변경
#if 0
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
/*
N = 5
for (int i = 0; i < N-2, ++i){
	for (int j = i+1 ; j < N-1; ++i){
		for(int k = j+1; k < N; ++k) {
			printf("%d %d %d\n", i, j, k);
		}
	}
}
중첩 for문이 증가할 때 아래 있는 재귀함수의 경우 N의 크기와, choice 배열의 인덱스만 늘려주면 된다.
*/
#define ARR_SIZE 4
int N = ARR_SIZE - 1;
int choice[ARR_SIZE] = { 0 };

void print_arr(int L) {
	for (int i = 1; i < L; i++) {
		printf("%d ", choice[i]);
	}
}

void jump(int L, int start, int end) {
	if (L > N) {
		print_arr(L);
		printf("\n");
		return; 
	}
	for (int i = start; i < end; ++i) {
		//printf("%d ", i);
		choice[L] = i;
		jump(L+1, i + 1, end + 1);
	}
}

int main(void) {
	jump(1, 0, 3);
	return 0;
}

#endif

// 경우의 수 만들기
/*
1: 0 0 0
2: 0 0 1
3: 0 1 0
4: 1 0 0
5: 0 1 1
6: 1 0 1
7: 1 1 0
8: 1 1 1
*/
#if 0
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>

int N = 0;
int A[10] = { 0 };
int num = 1;
void print_arr(int L) {
	for (int i = 1; i < L; i++) {
		printf("%d ", A[i]);
	}
}

void test(int L) {
	if (L > N) {
		printf("%d: ", num);
		num++;
		print_arr(L);
		printf("\n");
		return;
	}
	for (int i = 0; i < 2; i++) {
		A[L] = i;
		test(L + 1);
	}
}

int main(void) {
	(void)scanf("%d", &N);
	test(1);
	return 0;
}

#endif
// 경우의 수 강사님 ver.
#if 0
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
int N = 0;
int A[10] = { 0 };
int num = 0;
void print_A(int L) {
	++num;
	printf("%2d : ", num);
	for (int i = 0; i < L; ++i) {
		printf("%d ", A[i]);
	}
	printf("\n");
}
void test(int L) {
	if (L >= N) {
		print_A(L);
		return;
	}
	A[L] = 0;
	test(L + 1);
	A[L] = 1;
	test(L + 1);
}
int main(void) {
	(void)scanf("%d", &N);
	test(0);
	return 0;
}

#endif

// 1의 개수가 두개 이상인 경우의 수를 세는 방법
#if 0
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>

int N = 0;
int A[10] = { 0 };
int C = 0;

void test(int L, int cnt) {
	if (L >= N) {
		if (cnt >= 2) {
			C++;
			for (int i = 0; i < L; ++i) {
				printf("%d ", A[i]);
			}
			printf("\n");
		}
		return;
	}
	A[L] = 0;
	test(L + 1, cnt);
	A[L] = 1;
	test(L + 1, cnt + 1);
}
int main(void) {
	(void)scanf("%d", &N);
	test(0, 0);
	printf("1이 두개 이상인 경우의 수 : %d", C);
	return 0;
}

#endif

// 1의 개수가 2개 이상인 경우의 수

// parameter 활용 - 가지치기
#if 0
#include <stdio.h>
int N = 5;
int a[10] = { 0 };
int sum = 0;
int loop = 0;
// n : 1의 개수
void print_a(int s) {
	for (int i = 0; i < s; i++) {
		printf("%d ", a[i]);
	}
	printf("\n");
}
void test(int s, int n) {
	++loop;
	if (n >= 2) {
		print_a(s);
		//printf("s=%d, N=%d\n", s, N);
		sum += (1 << (N - s));
		return;
	}
	if (s >= N) {
		//if (n >= 2) sum++;
		return;
	}

	a[s] = 0;
	test(s + 1, n);
	a[s] = 1;
	test(s + 1, n + 1);
}

int main() {
	test(0, 0);
	printf("1이 2개 이상인 조합의 개수: %d\n", sum);
	printf("loop = %d\n", loop);
	return 0;
}
#endif

// return 코드
#if 0
#include <stdio.h>

int A[10] = { 0 };

void print_A(int N) {
	static int no = 0;
	++no;
	printf("%2d : ", no);
	for (int i = 0; i < N; ++i) {
		printf("%d ", A[i]);
	}
	printf("\n");
}

int test(int L, int N) {
	int result = 0;
	if (L >= N) {
		print_A(L);
		int cnt1 = 0;
		for (int i = 0; i < N; ++i) {
			if (1 == A[i]) {
				++cnt1;
			}
		}
		return (cnt1 >= 2);
	}
	A[L] = 0;
	result += test(L + 1, N);
	A[L] = 1;
	result += test(L + 1, N);

	return result;
}

int main(void) {
	printf("%d\n", test(0, 3));
	return 0;
}
#endif

// N=3 (13)  N=4 (29) N=61
// static 변수의 사용 - 0의 개수 (++/-- 활용)
#if 0
#include <stdio.h>
int N = 3;
int A[10] = { 0 };
int sum = 0;

void print_A()
{
	static int no = 1;
	printf("%d : ", no);
	for (int i = 0; i < N; ++i)
	{
		printf("%d ", A[i]);
	}
	no++;
	printf("\n");
}
int loop = 0;
void test(int L)
{
	static int check = 0;
	++loop;
	if (check >= N - 1) {
		return;
	}
	if (L > N - 1) {
		sum++;
		print_A();
		return;
	}

	A[L] = 0;
	check++;
	test(L + 1);
	check--;
	A[L] = 1;
	test(L + 1);
}

int main()
{
	(void)scanf_s("%d", &N);
	test(0);
	printf("1이 2개 이상인 조합의 개수: %d\n", sum);
	printf("loop = %d\n", loop);
	return 0;
}
#endif

// 전역 변수 사용 ++/--를 활용, 가지치기 포함
#if 0
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
int N = 0;
int A[10];
int cnt = 0, count = 0;
int loop = 0;
void print_A() {
	static int no = 1;
	printf("%2d:", no);
	for (int i = 0; i < N; ++i) {
		printf(" %d", A[i]);
	}
	printf("\n");
	no++;
}

void test(int L) {
	++loop;
	if (cnt >= 2) {
		print_A();
		//printf("%d %d\n", L, N);
		count += 1 << (N - L);
		return;
	}
	if (L >= N) {
		return;
	}
	for (int i = 0; i < 2; ++i) {
		A[L] = i;
		if (i) cnt += 1;
		test(L + 1);
		if (i) cnt -= 1;
	}
}

int main(void) {
	scanf("%d", &N);
	test(0);
	printf("1이 2개 이상인 경우 개수 : %d\n", count);
	printf("loop = %d\n", loop);
	return 0;
}
#endif

/*
1 1 1
1 1 2
1 1 3
1 2 1
1 2 2
1 3 1
1 3 2
1 3 3
2 1 1
2 1 2
2 1 3
2 2 1
2 2 2
2 2 3
2 3 1
2 3 2
2 3 3
3 1 1
3 1 2
3 1 3
3 2 1
3 2 2
3 2 3
3 3 1
3 3 2
3 3 3

*/
#if 0
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>

int N = 3; // 반복하는 수
int n = 3; // 사용하는 숫자 수
int A[10] = { 0 };
int num = 0;
void print_arr(int L) {
	num++;
	printf("%2d : ", num);
	for (int i = 1; i < L; i++) {
		printf("%d ", A[i]);
	}
}

void test(int L) {
	if (L > N) {
		print_arr(L);
		printf("\n");
		return;
	}
	for (int i = 1; i <= n; ++i) {
		A[L] = i;
		test(L+1);
	}
}

int main(void) {
	test(1);
	return 0;
}

#endif

// 강사님 ver.
#if 0
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
int N = 3;
int A[10] = { 0 };
int num = 0;
void print_arr(int L) {
	num++;
	printf("%2d : ", num);
	for (int i = 1; i < L; i++) {
		printf("%d ", A[i]);
	}
}

void test(int L) {
	if (L > N) {
		print_arr(L);
		printf("\n");
		return;
	}
	A[L] = 1;
	test(L + 1);
	A[L] = 2;
	test(L + 1);
	A[L] = 3;
	test(L + 1);
}
int main(void) {
	test(1);
	return 0;
}

#endif

/*
1 2 3
1 3 2
2 1 3
2 3 1
3 1 2
3 2 1
*/
#if 0
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
int N = 0;
int A[10] = { 0 };
int V[10] = { 0 };

int num = 0;
void print_arr(int L) {
	num++;
	printf("%2d : ", num);
	for (int i = 1; i < L; i++) {
		printf("%d ", A[i]);
	}
}

void test(int L) {
	if (L > N) {
		print_arr(L);
		printf("\n");
		return;
	}
	for (int i = 1; i <= N; ++i) {
		if (V[i] != 1) {
			A[L] = i;
			V[i] = 1;
			test(L + 1);
			V[i] = 0;
		}
	}
}

int main(void) {
	(void)scanf("%d", &N);
	test(1);
	return 0;
}

#endif
