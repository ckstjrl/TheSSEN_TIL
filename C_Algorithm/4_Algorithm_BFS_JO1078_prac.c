// JungOl 1078. 저글링 방사능 오염
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  
#include <stdlib.h>
#define ARR_SIZE (100 + 2)
int X, Y;                 
int map[ARR_SIZE][ARR_SIZE];
int sr, sc;               

void inputData(void) {
    char temp[ARR_SIZE];

    (void)scanf("%d %d", &Y, &X);

    for (int i = 1; i <= X; i++) {         // i: 행
        (void)scanf("%s", temp + 1);
        for (int j = 1; j <= Y; j++) {     // j: 열
            map[i][j] = temp[j] - '0';
        }
    }

    (void)scanf("%d %d", &sc, &sr);
}

typedef struct {
    int r, c;
} node_t;

int BFS(int* alive_cnt) {
    node_t q[ARR_SIZE * ARR_SIZE];
    int front = 0, rear = 0;

    int di[4] = { 1, -1, 0, 0 };
    int dj[4] = { 0, 0, 1, -1 };

    int last_time = 0;

    // 초기 감염은 3초부터 시작
    if (sr >= 1 && sr <= X && sc >= 1 && sc <= Y && map[sr][sc] == 1) {
        map[sr][sc] = 3;
        q[rear++] = (node_t){ sr, sc };
        last_time = 3;
    }

    while (front < rear) {
        node_t cur = q[front++];
        int cur_time = map[cur.r][cur.c];

        for (int d = 0; d < 4; d++) {
            int ni = cur.r + di[d];
            int nj = cur.c + dj[d];

            if (ni < 1 || ni > X || nj < 1 || nj > Y) continue;

            if (map[ni][nj] == 1) {
                map[ni][nj] = cur_time + 1;      // 감염(=죽는) 시간 기록
                q[rear++] = (node_t){ ni, nj };

                if (map[ni][nj] > last_time) last_time = map[ni][nj];
            }
        }
    }

    // 남은 1 count
    int alive = 0;
    for (int i = 1; i <= X; i++) {
        for (int j = 1; j <= Y; j++) {
            if (map[i][j] == 1) alive++;
        }
    }
    *alive_cnt = alive;

    // 다 죽는 시간은 last_time 자체가 정답(이미 3초부터 누적됨)
    return last_time;
}

int main(void) {
    inputData();

    int alive_cnt = 0;
    int dead_time = BFS(&alive_cnt);

    printf("%d\n%d\n", dead_time, alive_cnt);
    return 0;
}