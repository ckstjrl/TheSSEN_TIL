// 큐 구현 -> 최단거리 구할 경우 유용
#if 0
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
#define ARR_SIZE (100)
int Queue[ARR_SIZE];
int front, rear;

// 삽입
void enqueue(int data) {
	// queue가 가득 찬 상태인지 확인 (FULL)
	if (rear >= ARR_SIZE) {
		printf("FULL!");
		return;
	}
	// queue 의 rear 위치에 삽입하고, rear 1증가
	Queue[rear++] = data;
}

// 맨 앞의 데이터를 삭제하며 그 값 리턴
int dequeue(void) {
	// queue에 데이터가 있는지 확인 (EMPTY)
	if (front == rear) {
		printf("EMPTY!\n");
		return -1;
	}
	// queue의 front의 데이터를 리턴하고, front 1 증가
	return Queue[front++];
}

void print_Queue(void) {
	for (int i = front; i < rear; ++i) {
		printf("%d ", Queue[i]);
	}
	printf("\n");
}

int main(void) {
	front = rear = 0;
	dequeue();
	enqueue(10);
	enqueue(20);
	enqueue(30);
	enqueue(40);
	printf("%d ", dequeue());
	printf("%d ", dequeue());
	enqueue(50);
	enqueue(60);
	printf("%d ", dequeue());
	printf("%d ", dequeue());
	printf("%d ", dequeue());
	
	return 0;
}


#endif

// 구조체로 구현
#if 0
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  // memset
#include <stdlib.h>
#define ARR_SIZE (100)

typedef struct _node {
	int r;
	int c;
	int cost;
}node_t;
node_t Queue[ARR_SIZE];
int front, rear;

void enqueue1(int r, int c, int cost) {
	// queue가 가득 찬 상태인지 확인 (FULL)
	if (rear >= ARR_SIZE) {
		printf("FULL!");
		return;
	}
	// queue 의 rear 위치에 삽입하고, rear 1증가
	Queue[rear].r = r;
	Queue[rear].c = c;
	Queue[rear++].cost = cost;
}

void enqueue2(node_t data) {
	// queue가 가득 찬 상태인지 확인 (FULL)
	if (rear >= ARR_SIZE) {
		printf("FULL!");
		return;
	}
	// queue 의 rear 위치에 삽입하고, rear 1증가
	Queue[rear++] = data;
}

void enqueue3(node_t *data) {
	// queue가 가득 찬 상태인지 확인 (FULL)
	if (rear >= ARR_SIZE) {
		printf("FULL!");
		return;
	}
	// queue 의 rear 위치에 삽입하고, rear 1증가
	Queue[rear++] = *data;
}


node_t dequeue1(void) {
	// queue에 데이터가 있는지 확인 (EMPTY)
	if (front == rear) {
		printf("EMPTY!\n");
		return (node_t) { 0, 0, 0 };
	}
	// queue의 front의 데이터를 리턴하고, front 1 증가
	return Queue[front++];
}

node_t *dequeue2(void) {
	// queue에 데이터가 있는지 확인 (EMPTY)
	if (front == rear) {
		printf("EMPTY!\n");
		return NULL;
	}
	// queue의 front의 데이터를 리턴하고, front 1 증가
	return Queue + front;
}

node_t* dequeue3(void) {
	// queue에 데이터가 있는지 확인 (EMPTY)
	if (front == rear) {
		printf("EMPTY!\n");
		return NULL;
	}
	// queue의 front의 데이터를 리턴하고, front 1 증가
	return &Queue[front++];
}

int main(void) {
	node_t data = { 2, 2, 200 };
	node_t* temp = NULL;
	front = rear = 0;
	enqueue1(1, 1, 100);
	enqueue2(data);
	enqueue2((node_t){3, 3, 300});
	enqueue3(&data);
	data = dequeue1();
	printf("%d %d %d\n", data.r, data.c, data.cost);
	// 문법상 사용하면 안되지만, data에 4개 바이트씩 12바이트를 가지고 있어서 가능한 것
	printf("%d %d %d\n", data);
	temp = dequeue2();
	printf("%d %d %d\n", temp->r, temp->c, temp->cost);
	return 0;
}


#endif