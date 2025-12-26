#ifndef __HEAP_LIB_H__
#define __HEAP_LIB_H__
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <stdlib.h>

typedef int ElementType2;
typedef struct _node {
	ElementType2 Data;
}HeapNode;

typedef struct _heap {
	HeapNode* Nodes;
	int Capacity;  // Nodes 배열의 크기
	int UsedSize;  // 사용된 요소의 수
}Heap;

Heap* HEAP_Create(int c);
void HEAP_Print(Heap* H);
void HEAP_Insert(Heap* H, ElementType2 data);
void HEAP_Insert2(Heap* H, ElementType2 data);
HeapNode* HEAP_Delete(Heap* H);
HeapNode* HEAP_Delete2(Heap* H);
#endif
