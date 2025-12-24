#ifndef __DLL_LIB_H__
#define __DLL_LIB_H__
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <stdlib.h>

typedef int ElementType;
typedef struct _nodeDLL {
    ElementType Data;
    struct _nodeDLL* prev;
    struct _nodeDLL* next;
}NodeDLL;

NodeDLL* DLL_CreateNode(ElementType data);
void DLL_AppendNode(NodeDLL* tail, NodeDLL* newnode);
void DLL_PrintNode(NodeDLL* head, NodeDLL* tail);
NodeDLL* DLL_SerchNode(ElementType data, NodeDLL* head, NodeDLL* tail);
void DLL_RemoveNode(NodeDLL* delnode);
void DLL_InsertAfter(NodeDLL* fnode, NodeDLL* newnode);
void DLL_InsertBefore(NodeDLL* fnode, NodeDLL* newnode);
#endif
