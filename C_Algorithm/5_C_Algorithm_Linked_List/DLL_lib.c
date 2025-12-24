#include "DLL_lib.h"

NodeDLL* DLL_CreateNode(ElementType data) {
    NodeDLL* newnode = NULL;
    newnode = (NodeDLL*)malloc(sizeof(NodeDLL));
    if (newnode == NULL) {
        printf("메모리 부족\n");
        return NULL;
    }
    newnode->Data = data;
    newnode->prev = NULL;
    newnode->next = NULL;
    return newnode;
}
//  prve -> newnode -> tail
void DLL_AppendNode(NodeDLL* tail, NodeDLL* newnode) {
    NodeDLL* prev = tail->prev;
    newnode->next = tail;
    newnode->prev = prev;
    tail->prev = newnode;
    prev->next = newnode;
}
// head -> ... -> tail
void DLL_PrintNode(NodeDLL* head, NodeDLL* tail) {
    for (NodeDLL* curr = head->next; curr != tail; curr = curr->next) {
        printf("%d ", curr->Data);
    }
    printf("\n");
}
// data를 사용하여 노드를 검색하여 같은 값을 갖는 노드 반환
// 찾지 못하는 경우 NULL을 반환
NodeDLL* DLL_SerchNode(ElementType data, NodeDLL *head, NodeDLL *tail) {
    for (NodeDLL* curr = head->next; curr != tail; curr = curr->next) {
        if (curr->Data == data) {
            return curr;
        }
    }
    return NULL;
}
//delnode 삭제
void DLL_RemoveNode(NodeDLL *delnode) {
    delnode->prev->next = delnode->next;
    delnode->next->prev = delnode->prev;
    free(delnode);
}
//삽입
void DLL_InsertAfter(NodeDLL* fnode, NodeDLL* newnode) {
    newnode->prev = fnode;
    newnode->next = fnode->next;
    newnode->prev->next = newnode;
    newnode->next->prev = newnode;
}
void DLL_InsertBefore(NodeDLL* fnode, NodeDLL* newnode) {
    newnode->prev = fnode->prev;
    newnode->next = fnode;
    fnode->prev = newnode;
    newnode->prev->next = newnode;
}

void DLL_Destroy(NodeDLL* head, NodeDLL* tail) {
    NodeDLL* temp = head->next;
    while (temp != tail)
    {
        temp = temp->next;
        free(temp->prev);
    }
    head->next = tail;
    tail->prev = head;

}