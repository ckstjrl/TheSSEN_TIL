// JungOl 2606. 토마토(초)
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

#define ARR_SIZE (100 + 2)           // N, M 최대 100 기준
#define H_SIZE   (100 + 2)           // H 최대 100 기준
#define Q_SIZE   (H_SIZE * ARR_SIZE * ARR_SIZE)

int M, N, H;
int box[H_SIZE][ARR_SIZE][ARR_SIZE];

typedef struct _node {
    int h;
    int i;
    int j;
} node_t;

node_t Queue[Q_SIZE];
int front, rear;

int dH[6] = { 1, -1, 0, 0, 0, 0 };
int dI[6] = { 0, 0, 1, -1, 0, 0 };
int dJ[6] = { 0, 0, 0, 0, 1, -1 };

void inputData(void);
int tomatoBFS(void);

int main(void) {
    inputData();
    printf("%d\n", tomatoBFS());
    return 0;
}

void inputData(void) {
    (void)scanf("%d %d %d", &M, &N, &H);

    // 입력: h층마다 N줄, 각 줄 M개
    for (int h = 0; h < H; h++) {
        for (int i = 0; i < N; i++) {
            for (int j = 0; j < M; j++) {
                (void)scanf("%d", &box[h][i][j]);
            }
        }
    }
}

int tomatoBFS(void) {
    front = rear = 0;

    // 1) 초기 익은 토마토 전부 큐에 넣기, 0 존재 여부 체크
    int has_zero = 0;
    for (int h = 0; h < H; h++) {
        for (int i = 0; i < N; i++) {
            for (int j = 0; j < M; j++) {
                if (box[h][i][j] == 1) {
                    Queue[rear++] = (node_t){ h, i, j };
                }
                else if (box[h][i][j] == 0) {
                    has_zero = 1;
                }
            }
        }
    }

    // 처음부터 다 익어있으면 0
    if (!has_zero) return 0;

    // 2) BFS 진행 (값을 "날짜"로 갱신: 1 -> 2 -> 3 ...)
    while (front != rear) {
        node_t cur = Queue[front++];

        for (int d = 0; d < 6; d++) {
            int nh = cur.h + dH[d];
            int ni = cur.i + dI[d];
            int nj = cur.j + dJ[d];

            if (nh < 0 || ni < 0 || nj < 0) continue;
            if (nh >= H || ni >= N || nj >= M) continue;

            // 안익은 토마토(0)만 익히기
            if (box[nh][ni][nj] == 0) {
                box[nh][ni][nj] = box[cur.h][cur.i][cur.j] + 1;
                Queue[rear++] = (node_t){ nh, ni, nj };
            }
        }
    }

    // 3) 결과 확인: 0 남아있으면 -1, 아니면 최대값-1
    int ans = 0;
    for (int h = 0; h < H; h++) {
        for (int i = 0; i < N; i++) {
            for (int j = 0; j < M; j++) {
                if (box[h][i][j] == 0) return -1;
                if (ans < box[h][i][j]) ans = box[h][i][j];
            }
        }
    }

    return ans - 1;
}
