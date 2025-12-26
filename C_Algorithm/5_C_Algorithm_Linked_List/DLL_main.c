#if 0
#include "DLL_lib.h"

int main(void) {
    NodeDLL* head;
    NodeDLL* tail;
    NodeDLL* newnode;
    NodeDLL* temp;
    head = DLL_CreateNode(0);
    tail = DLL_CreateNode(0);
    head->next = tail;
    tail->prev = head;

    DLL_AppendNode(tail, DLL_CreateNode(10));
    DLL_AppendNode(tail, DLL_CreateNode(20));
    DLL_AppendNode(tail, DLL_CreateNode(30));
    DLL_PrintNode(head, tail);

    // 10찾아서 삭제
    temp = DLL_SerchNode(10, head, tail);
    if (temp != NULL) DLL_RemoveNode(temp);
    DLL_PrintNode(head, tail);

    // 40은 없으니까 삭제 X
    temp = DLL_SerchNode(40, head, tail);
    if (temp != NULL) DLL_RemoveNode(temp);
    DLL_PrintNode(head, tail);

    return 0;
}

#endif

#if 0
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <stdlib.h>
void CreateArray(int** arr) {
    *arr = (int*)malloc(sizeof(int) * 10);
    if (*arr == NULL) exit(0);
    (*arr)[0] = 100;
    printf("%d\n", (*arr)[0]);
}

int main(void) {
    int* p = NULL;
    CreateArray(&p);
    printf("%d\n", p[0]);
    return 0;
}
#endif


#if 0
#include <stdio.h>
#include <stdlib.h>
typedef struct node {
    int Data;
}Node;

typedef struct cq {
    Node* Nodes;
    int Capacity;
    int Front;
    int Rear;
}CircularQueue;

void CQ_CreateQueue(CircularQueue** Queue, int Capacity)
{
    CircularQueue* Q;
    // 큐를 자유 저장소에 생성
    (Q) = (CircularQueue*)malloc(sizeof(CircularQueue));
    // 입력된 Capacity+1만큼의 노드를 자유 저장소에 생성
    (Q)->Nodes = (Node*)malloc(sizeof(Node) * (Capacity + 1));

    (Q)->Capacity = Capacity;
    (Q)->Front = 0;
    (Q)->Rear = 0;

    *Queue = Q;
}

int main(void) {
    CircularQueue* Queue = NULL;
    CQ_CreateQueue(&Queue, 10);
    printf("%d\n", Queue->Capacity);
    return 0;
}
#endif