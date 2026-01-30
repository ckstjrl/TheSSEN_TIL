// BOJ 2667. 단지번호붙이기
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>
#include <stdlib.h>
#define ARR_SIZE (25 + 2)

int N;
int map[ARR_SIZE][ARR_SIZE];
int visited[ARR_SIZE][ARR_SIZE] = { 0 };
int cnt;
int cnt_house[25 * 25 / 2] = { 0 };
int di[4] = { 1, -1, 0, 0 };
int dj[4] = { 0, 0, 1, -1 };

void inputData(void) {
	int i;
	char temp[27] = { 0 };
	(void)scanf("%d", &N);
	for (i = 1; i <= N; ++i) {
		(void)scanf("%s", temp);
		for (int j = 1; j <= N; ++j) {
			map[i][j] = temp[j-1] - '0';
		}
	}
}

int comp(void* a, void* b) {
	int x = *(const int*)a;
	int y = *(const int*)b;
	if (x > y) return 1;
	else if (x == y) return 0;
	else return -1;
}


void test(int r, int c) {
	if (map[r][c] == 0 || visited[r][c] != 0) {
		return;
	}
	visited[r][c] = 1;
	cnt += 1;
	for (int i = 0; i < 4; i++) {
		int nr = r + di[i];
		int nc = c + dj[i];
		if (map[nr][nc] == 1 && visited[nr][nc] == 0) {
			test(nr, nc);
		}
	}
}

int main(void) {
	inputData();
	int danji_cnt = 0;
	for (int i = 1; i <= N; i++) {
		for (int j = 1; j <= N; j++) {
			if (map[i][j] == 1 && visited[i][j] == 0) {
			cnt = 0;
			test(i, j);
			cnt_house[danji_cnt] = cnt;
			danji_cnt++;
			}
		}
	}
	qsort(cnt_house, danji_cnt, sizeof(cnt_house[0]), comp);
	printf("%d\n", danji_cnt);
	for (int a = 0; a < danji_cnt ; a++) {
		printf("%d\n", cnt_house[a]);
	}

	return 0;
}