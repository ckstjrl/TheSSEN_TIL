// JungOl 1661. 미로탈출
/*
BFS 로직
1. 시작점을 queue에 넣음
2. queue에 내용이 있는 동안 반복
	2-1. queue에서 정점을 꺼냄.
	2-2. 정점의 인접을 찾아 queue에 넣음
	만약, 정점이 종료지점이라면 바로 종료할 수 있음.
*/
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  
#include <stdlib.h>
#define ARR_SIZE (100 + 2)
int X, Y;
int sX, sY, eX, eY;
int miro[ARR_SIZE][ARR_SIZE];
int visited[ARR_SIZE][ARR_SIZE];
typedef struct _node {
    int currX;
    int currY;
    int time;
}node_t;
node_t Queue[ARR_SIZE * ARR_SIZE];
int front, rear;
int dX[4] = { 0, 0, -1, 1 };
int dY[4] = { -1, 1, 0, 0 };  // 상하좌우

void printData(int (*ary)[ARR_SIZE]);
void inputData(void);
int miroBFS(int sX, int sY, int eX, int eY);

int main(void) {
    inputData();
    printf("%d\n", miroBFS(sX, sY, eX, eY));
    return 0;
}

int miroBFS(int sX, int sY, int eX, int eY) {
    node_t temp = { 0 };
    int nextX, nextY;

    front = rear;
    Queue[rear++] = (node_t){ sX, sY, 0 };
    visited[sY][sX] = 1;
    while (front != rear) {
        temp = Queue[front++];
        if (temp.currX == eX && temp.currY == eY) {
            return temp.time;
        }
        for (int i = 0; i < 4; ++i) {
            nextX = temp.currX + dX[i];
            nextY = temp.currY + dY[i];
            if (nextX < 1 || nextY < 1 || nextX > X || nextY > Y) continue;
            if (miro[nextY][nextX] && !visited[nextY][nextX]) {
                Queue[rear++] = (node_t){ nextX, nextY, temp.time + 1 };
                visited[nextY][nextX] = 1;
            }
        }
    }
    return -1;
}
void printData(int (*ary)[ARR_SIZE]) {
    for (int i = 1; i <= Y; ++i) {
        for (int j = 1; j <= X; ++j) {
            printf("%d ", ary[i][j]);
        }
        printf("\n");
    }
    printf("\n");
}

void inputData(void) {
    char temp[ARR_SIZE] = { 0 };
    (void)scanf("%d %d", &X, &Y);
    (void)scanf("%d %d %d %d", &sX, &sY, &eX, &eY);
    for (int i = 1; i <= Y; ++i) {
        (void)scanf("%s", temp + 1);
        for (int j = 1; j <= X; ++j) {
            miro[i][j] = (temp[j] - '0') ^ 1;
        }
    }
}
