// JungOl 2613. 토마토(고)
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>
#include <stdlib.h>

#define ARR_SIZE (1000 + 2)           // N, M 최대 100 기준
#define Q_SIZE   (ARR_SIZE * ARR_SIZE)

int M, N;
int box[ARR_SIZE][ARR_SIZE];

typedef struct _node {
    int i;
    int j;
} node_t;

node_t Queue[Q_SIZE];
int front, rear;

int dI[4] = { 1, -1, 0, 0 };
int dJ[4] = { 0, 0, 1, -1 };

void inputData(void);
int tomatoBFS(void);

int main(void) {
    inputData();
    printf("%d\n", tomatoBFS());
    return 0;
}

void inputData(void) {
    (void)scanf("%d %d", &M, &N);

    // 입력
    for (int i = 0; i < N; i++) {
        for (int j = 0; j < M; j++) {
            (void)scanf("%d", &box[i][j]);
        }
    }
}

int tomatoBFS(void) {
    front = rear = 0;

    // 1) 초기 익은 토마토 전부 큐에 넣기, 0 존재 여부 체크
    int has_zero = 0;
    for (int i = 0; i < N; i++) {
        for (int j = 0; j < M; j++) {
            if (box[i][j] == 1) {
                Queue[rear++] = (node_t){ i, j };
            }
            else if (box[i][j] == 0) {
                has_zero = 1;
            }
        }
    }
    

    // 처음부터 다 익어있으면 0
    if (!has_zero) return 0;

    // 2) BFS 진행 (값을 "날짜"로 갱신: 1 -> 2 -> 3 ...)
    while (front != rear) {
        node_t cur = Queue[front++];

        for (int d = 0; d < 4; d++) {
            int ni = cur.i + dI[d];
            int nj = cur.j + dJ[d];

            if (ni < 0 || nj < 0) continue;
            if (ni >= N || nj >= M) continue;

            // 안익은 토마토(0)만 익히기
            if (box[ni][nj] == 0) {
                box[ni][nj] = box[cur.i][cur.j] + 1;
                Queue[rear++] = (node_t){ ni, nj };
            }
        }
    }

    // 3) 결과 확인: 0 남아있으면 -1, 아니면 최대값-1
    int ans = 0;
    
    for (int i = 0; i < N; i++) {
        for (int j = 0; j < M; j++) {
            if (box[i][j] == 0) return -1;
            if (ans < box[i][j]) ans = box[i][j];
        }
    }
    

    return ans - 1;
}
