// 재귀로 풀이
#if 0
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
int N;
int E;
int adjMatrix[101][101];
int adjlist[101][101];
int visited[101] = { 0 };
int cnt = 0;

//void print_arr(int (*arr)[101]) {
//	for (int i = 1; i <= N; i++) {
//		for (int j = 1; j <= N; j++) {
//			printf("%d ", arr[i][j]);
//		}
//		printf("\n");
//	}
//}

void inputDataM() {
	int from, to;
	scanf("%d %d", &N, &E);
	for (int i = 0; i < E; i++) {
		scanf("%d %d", &from, &to);
		adjMatrix[from][to] = 1;
		adjMatrix[to][from] = 1;
	}
}
void test(int current) {
	for (int i = 1; i <= N; i++) {
		if (adjMatrix[current][i] == 1 && visited[i] == 0) {
			cnt++;
			visited[i] = 1;
			test(i);
		}
	}
}

int main(void) {
	inputDataM();
	//print_arr(adjMatrix);
	visited[1] = 1;
	test(1);
	printf("%d\n", cnt);
	return 0;
}

#endif

#if 01
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
int N;
int E;
int adjMatrix[101][101];
int adjlist[101][101];
int visited[101] = { 0 };
int cnt = 0;

//void print_arr(int (*arr)[101]) {
//	for (int i = 1; i <= N; i++) {
//		for (int j = 0; j <= N; j++) {
//			printf("%d ", arr[i][j]);
//		}
//		printf("\n");
//	}
//}

void inputDataL() {
	int from, to;
	(void)scanf("%d %d", &N, &E);
	for (int i = 0; i < E; i++) {
		(void)scanf("%d %d", &from, &to);
		adjlist[from][++adjlist[from][0]] = to;
		adjlist[to][++adjlist[to][0]] = from;
	}
}

void test(int current) {
	for (int i = 1; i <= adjlist[current][0]; i++) {
		if (visited[adjlist[current][i]] == 0) {
			cnt++;
			visited[adjlist[current][i]] = 1;
			test(adjlist[current][i]);
		}
	}
}

int main(void) {

	inputDataL();
	//print_arr(adjlist); // 입력값 확인
	visited[1] = 1;
	test(1);
	printf("%d\n", cnt);
	return 0;
}

#endif