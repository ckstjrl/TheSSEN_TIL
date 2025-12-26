#include "heap_lib.h"

Heap* HEAP_Create(int c) {
    // Heap 구조체 생성
    Heap* newheap = (Heap*)calloc(1, sizeof(Heap));
    if (newheap == NULL) exit(0);

    // 노드 배열 생성
    newheap->Nodes = (HeapNode*)calloc(1, sizeof(HeapNode));
    if (newheap->Nodes == NULL) {
        free(newheap);
        exit(0);
    }

    // 용량 및 사용 크기 초기화
    newheap->Capacity = c;
    newheap->UsedSize = 0;
    return newheap;
}

void HEAP_Print(Heap* H) {
    if (H == NULL) return;

    // 힙에 저장된 값 출력 (1번 인덱스부터 사용)
    HeapNode* nodes = H->Nodes;
    for (int i = 1; i <= H->UsedSize; ++i) {
        printf("%d ", nodes[i].Data);
    }
    printf("\n");
}

void HEAP_Insert(Heap* H, ElementType2 data) {
    // 마지막 위치에 삽입 후 위로 정렬(up-heap)
    int child = ++H->UsedSize;
    int parent = child / 2;

    H->Nodes[child].Data = data;

    while (parent > 0) {
        // 최소 힙 조건 만족 시 종료
        if (H->Nodes[child].Data >= H->Nodes[parent].Data) break;

        // 부모와 교환
        ElementType2 temp = H->Nodes[child].Data;
        H->Nodes[child].Data = H->Nodes[parent].Data;
        H->Nodes[parent].Data = temp;

        child = parent;
        parent = child / 2;
    }
}

void swap(HeapNode* a, HeapNode* b)
{
    // 두 노드 전체 교환
    HeapNode temp = *a;
    *a = *b;
    *b = temp;
}

int get_min_child(HeapNode* nodes, int UsedSize, int left_child, int right_child) {
    // 두 자식 중 더 작은 값을 가진 인덱스 반환
    int swap_child;
    if (right_child < UsedSize)
        swap_child = (nodes[left_child].Data > nodes[right_child].Data) ? right_child : left_child;
    else
        swap_child = left_child;
    return swap_child;
}

HeapNode* HEAP_Delete(Heap* H)
{
    // 힙이 비어있으면 삭제 불가
    if (H->UsedSize == 0) {
        printf("HEAP size is ZERO!\n");
        return NULL;
    }

    int parent = 1;
    int left_child = 2;
    int right_child = 3;
    int swap_child;

    HeapNode* nodes = H->Nodes;
    int UsedSize = H->UsedSize;

    // 루트와 마지막 노드 교환
    swap(nodes + 1, nodes + UsedSize);

    // 아래로 내려가며 힙 정렬(down-heap)
    while (left_child < UsedSize)
    {
        swap_child = get_min_child(nodes, UsedSize, left_child, right_child);

        if (nodes[parent].Data < nodes[swap_child].Data)
            break;

        swap(nodes + parent, nodes + swap_child);

        parent = swap_child;
        left_child = parent << 1;
        right_child = left_child + 1;
    }

    // 삭제된 노드 위치 반환
    return nodes + (H->UsedSize--);
}

HeapNode* HEAP_Delete2(Heap* H) {
    // 루트 삭제 방식 (삭제 값 별도 저장)
    int curr = 1;
    int child1, child2, comp;
    HeapNode tmp;
    static HeapNode ret;

    ret = H->Nodes[1];

    // 마지막 노드를 루트로 이동
    H->Nodes[1].Data = H->Nodes[H->UsedSize--].Data;

    // 아래로 정렬
    while (H->UsedSize / 2 >= curr) {
        child1 = curr << 1;
        child2 = child1 + 1;

        if (child2 < H->UsedSize && H->Nodes[child1].Data > H->Nodes[child2].Data)
            comp = child2;
        else
            comp = child1;

        if (H->Nodes[comp].Data >= H->Nodes[curr].Data) break;

        tmp = H->Nodes[curr];
        H->Nodes[curr] = H->Nodes[comp];
        H->Nodes[comp] = tmp;

        curr = comp;
    }
    return &ret;
}

int get_min_child2(Heap* heap, int left, int right) {
    // 힙 기준 최소 자식 인덱스 반환
    if (heap->UsedSize == left) return left;
    return (heap->Nodes[left].Data < heap->Nodes[right].Data) ? left : right;
}

HeapNode* HEAP_Delete3(Heap* heap) {
    // 데이터가 없으면 NULL 반환
    if (heap->UsedSize == 0) return NULL;

    HeapNode* nodes = heap->Nodes;
    static HeapNode data;

    // 삭제될 루트 값 저장
    data = nodes[1];

    // 마지막 노드를 루트로 이동
    nodes[1].Data = nodes[heap->UsedSize--].Data;

    int curr = 1;

    // 아래로 정렬
    while (curr * 2 < heap->UsedSize) {
        int min_child = get_min_child2(heap, curr * 2, curr * 2 + 1);
        if (nodes[curr].Data < nodes[min_child].Data) break;
        swap(nodes + min_child, nodes + curr);
        curr = min_child;
    }
    return &data;
}
