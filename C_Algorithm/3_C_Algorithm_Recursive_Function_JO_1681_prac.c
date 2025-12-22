// JungOl 1681. 해밀턴순환회로
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

int N;
int map[15][15];
int visited[15] = { 0 };
int best = INT_MAX;

void inputData(void) {
	(void)scanf("%d", &N);
	for (int i = 1; i <= N; i++) {
		for (int j = 1; j <= N; j++) {
			(void)scanf("%d", &map[i][j]);
		}
	}
}

void dfs(int L, int curr, int cost) {
    if (cost >= best) return;

    if (L == N) {
        if (map[curr][1] == 0) return;
        int total = cost + map[curr][1];
        if (total < best) best = total;
        return;
    }

    for (int next = 2; next <= N; next++) { 
        if (!visited[next] && map[curr][next] != 0) {
            visited[next] = 1;
            dfs(L + 1, next, cost + map[curr][next]);
            visited[next] = 0;
        }
    }
}

int main() {
    inputData();

    for (int i = 1; i <= N; i++) visited[i] = 0;
    visited[1] = 1;

    dfs(1, 1, 0);

    printf("%d\n", best);
    return 0;
}