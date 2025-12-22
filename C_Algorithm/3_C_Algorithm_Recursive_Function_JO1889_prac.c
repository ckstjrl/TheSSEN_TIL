// JungOl 1889. N Queen
#define _CRT_SECURE_NO_WARNINGS
#include <stdio.h>
#include <string.h>  
#include <stdlib.h>
#define N_MAX (13+1)
int N;
int c_visit[N_MAX];
int d1_visit[N_MAX * 2];
int d2_visit[N_MAX * 2];
int result = 0;
int t = 0;
void NQueen(int R) {
    ++t;
    if (R > N) {
        ++result;
        return;
    }
    for (int C = 1; C <= N; ++C) {
        int D1 = R - C + N;
        int D2 = R + C;
        if (!c_visit[C] && !d1_visit[D1] && !d2_visit[D2]) {
            c_visit[C] = 1;
            d1_visit[D1] = 1;
            d2_visit[D2] = 1;
            NQueen(R + 1);
            c_visit[C] = 0;
            d1_visit[D1] = 0;
            d2_visit[D2] = 0;
        }
    }
}

int main(void) {
    (void)scanf("%d", &N);
    result = 0;
    NQueen(1);
    printf("%d\n", result);
    printf("t = %d\n", t);
    return 0;
}