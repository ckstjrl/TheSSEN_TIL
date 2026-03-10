/*
 * ================================================================
 *  STEP 2 : 양방향 메시지 큐 — 요청 / 응답
 *  파일   : calc_client.c
 * ================================================================
 *
 *  [목표]
 *   클라이언트가 계산 요청을 보내면 서버가 계산해서 결과를 돌려준다.
 *
 *  [큐 구성]
 *
 *   /calc_req   : 클라이언트 → 서버  (요청 큐)
 *   /calc_resp  : 서버 → 클라이언트  (응답 큐)
 *
 *  [메시지 구조체]
 *
 *   typedef struct {
 *       int  a;          // 첫 번째 숫자
 *       int  b;          // 두 번째 숫자
 *       char op;         // '+' '-' '*' '/'
 *   } Request;
 *
 *   typedef struct {
 *       int  result;     // 계산 결과
 *       int  ok;         // 1=성공, 0=실패 (예: 0 으로 나누기)
 *   } Response;
 *
 *  [동작 흐름]
 *
 *   client                      server
 *   ──────                      ──────
 *   req = {10, 3, '+'}
 *   mq_send(req_q, req) ──────> mq_receive(req_q)
 *                               계산: 10 + 3 = 13
 *                               mq_send(resp_q, resp)
 *   mq_receive(resp_q) <──────
 *   결과 출력: 10 + 3 = 13
 *
 *  컴파일: gcc -Wall -g -o calc_client calc_client.c -lrt
 * ================================================================
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <mqueue.h>
#include <fcntl.h>
#include <sys/stat.h>

#define MQ_REQUEST  "/calc_req"
#define MQ_RESPONSE "/calc_resp"
#define MAX_MSGS    8

/* ── 메시지 구조체 ── */
typedef struct {
    int  a;
    int  b;
    char op;   /* '+' '-' '*' '/' */
} Request;

typedef struct {
    int result;
    int ok;    /* 1=성공, 0=오류 */
} Response;

int main(void)
{
    /* --------------------------------------------------------
     * TODO 1 : 두 큐를 위한 mq_attr 준비
     *
     *  struct mq_attr attr;
     *  attr.mq_flags   = 0;
     *  attr.mq_maxmsg  = MAX_MSGS;
     *  attr.mq_msgsize = ???;   ← 어떤 구조체의 크기를 써야 할까?
     *  attr.mq_curmsgs = 0;
     *
     *  힌트: 요청 큐와 응답 큐의 msgsize 는 다를 수 있다.
     *        각 구조체 크기에 맞게 설정하면 된다.
     *        여기서는 둘 다 같은 attr 를 써도 무방하다.
     *        (sizeof 중 큰 값으로 통일하거나, 각각 따로 설정)
     * -------------------------------------------------------- */

    /* TODO */
    struct mq_attr resp_attr;
    resp_attr.mq_flags   = 0;
    resp_attr.mq_maxmsg  = MAX_MSGS;
    resp_attr.mq_msgsize = sizeof(Response);
    resp_attr.mq_curmsgs = 0;

    struct mq_attr req_attr;
    req_attr.mq_flags   = 0;
    req_attr.mq_maxmsg  = MAX_MSGS;
    req_attr.mq_msgsize = sizeof(Request);
    req_attr.mq_curmsgs = 0;
    /* --------------------------------------------------------
     * TODO 2 : 요청 큐 열기 (쓰기 전용)
     *          응답 큐 열기 (읽기 전용)
     *
     *  두 큐 모두 O_CREAT 사용
     *  실패 시 perror 후 EXIT_FAILURE
     * -------------------------------------------------------- */
    mqd_t mq_req  = (mqd_t)-1;
    mqd_t mq_resp = (mqd_t)-1;

    /* TODO */
    mq_req = mq_open(MQ_REQUEST, O_CREAT | O_WRONLY, 0644, &req_attr);
    if(mq_req == -1)
    {
        perror("open req error");
        return EXIT_FAILURE;
    }

    mq_resp = mq_open(MQ_RESPONSE, O_CREAT | O_RDONLY, 0644, &resp_attr);
    if(mq_resp == -1)
    {
        perror("open resp error");
        return EXIT_FAILURE;
    }

    printf("[Client] 서버에 연결됨\n\n");

    /* ── 테스트할 계산 목록 ── */
    Request tests[] = {
        { 10,  3, '+' },   /* 10 + 3 = 13  */
        { 20,  4, '-' },   /* 20 - 4 = 16  */
        {  6,  7, '*' },   /*  6 * 7 = 42  */
        { 15,  3, '/' },   /* 15 / 3 = 5   */
        {  9,  0, '/' },   /*  9 / 0 = 오류 */
    };
    int n = sizeof(tests) / sizeof(tests[0]);

    /* --------------------------------------------------------
     * TODO 3 : 각 요청 전송 → 응답 수신 → 출력
     *
     *  for i = 0 .. n-1:
     *    3-1. mq_send(mq_req, &tests[i], sizeof(Request), 0)
     *
     *    3-2. Response resp;
     *         mq_receive(mq_resp, &resp, sizeof(Response), NULL)
     *         (우선순위가 필요 없으면 NULL 전달 가능)
     *
     *    3-3. 결과 출력:
     *         if (resp.ok)
     *           printf("%d %c %d = %d\n", a, op, b, result)
     *         else
     *           printf("%d %c %d = 오류\n", a, op, b)
     * -------------------------------------------------------- */

    /* TODO */
    int i;
    for (i = 0; i < n; i++)
    {
        if (mq_send(mq_req, (const char *)&tests[i], sizeof(Request), 0) == -1) {
            perror("mq_send");
            return EXIT_FAILURE;
        }

        Response resp;
        if (mq_receive(mq_resp, (char *)&resp, sizeof(Response), NULL) == -1) {
            perror("mq_receive");
            return EXIT_FAILURE;
        }

        if (resp.ok)
            printf("%d %c %d = %d\n", tests[i].a, tests[i].op, tests[i].b, resp.result);
        else
            printf("%d %c %d = 오류\n", tests[i].a, tests[i].op, tests[i].b);
    }
    /* --------------------------------------------------------
     * TODO 4 : 서버 종료 신호 전송
     *
     *  op = 'Q' 인 Request 를 전송하면 서버가 종료됨
     * -------------------------------------------------------- */
    Request quit = { 0, 0, 'Q' };
    /* TODO: mq_send */
    if (mq_send(mq_req, (const char *)&quit, sizeof(Request), 0) == -1)
    {
        perror("mq_send quit");
    }
    /* --------------------------------------------------------
     * TODO 5 : 정리 — mq_close × 2
     * -------------------------------------------------------- */

    /* TODO */
    mq_close(mq_req);
    mq_close(mq_resp);
    
    printf("\n[Client] 완료\n");
    return EXIT_SUCCESS;
}
