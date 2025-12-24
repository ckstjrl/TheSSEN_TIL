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