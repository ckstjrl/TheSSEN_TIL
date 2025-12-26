#if 1
#include "heap_lib.h"

int main(void) {
	Heap* h = NULL;
	h = HEAP_Create(20);
	printf("%d %d\n", h->Capacity, h->UsedSize);
	HEAP_Insert2(h, 10);
	HEAP_Insert2(h, 20);
	HEAP_Insert2(h, 8);
	HEAP_Insert2(h, -1);
	HEAP_Insert2(h, 3);
	HEAP_Insert2(h, 80);
	HEAP_Insert2(h, 15);
	HEAP_Insert2(h, 16);
	HEAP_Insert2(h, 17);
	HEAP_Insert2(h, 18);
	HEAP_Insert2(h, 28);
	HEAP_Print(h);
	for (int i = 0; i < 11; ++i) {
		printf("%d ", HEAP_Delete(h)->Data);
		HEAP_Print(h);
	}
	printf("\n");
	//printf("%d\n", HEAP_Delete(h)->Data);
	return 0;
}
#endif