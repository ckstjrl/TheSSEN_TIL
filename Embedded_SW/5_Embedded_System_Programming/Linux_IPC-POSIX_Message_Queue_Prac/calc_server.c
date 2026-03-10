/*
 * ================================================================
 *  STEP 2 : 양방향 메시지 큐 — 요청 / 응답
 *  파일   : calc_server.c
 * ================================================================
 *  컴파일: gcc -Wall -g -o calc_server calc_server.c -lrt
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

typedef struct {
    int  a;
    int  b;
    char op;
} Request;

typedef struct {
    int result;
    int ok;
} Response;

int main(void)
{
    /* --------------------------------------------------------
     * TODO 1 : 큐 속성 설정 및 두 큐 열기
     *
     *  요청 큐  : O_CREAT | O_RDONLY  (서버가 읽는 큐)
     *  응답 큐  : O_CREAT | O_WRONLY  (서버가 쓰는 큐)
     * -------------------------------------------------------- */

    mqd_t mq_req  = (mqd_t)-1;
    mqd_t mq_resp = (mqd_t)-1;

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

    mq_req = mq_open(MQ_REQUEST, O_CREAT | O_RDONLY, 0644, &req_attr);
    if(mq_req == -1)
    {
        perror("open req error");
        return EXIT_FAILURE;
    }

    mq_resp = mq_open(MQ_RESPONSE, O_CREAT | O_WRONLY, 0666, &resp_attr);
    if(mq_resp == -1)
    {
        perror("open resp error");
        return EXIT_FAILURE;
    }
    printf("[Server] 시작. 요청 대기 중...\n\n");

    /* --------------------------------------------------------
     * TODO 2 : 계산 루프
     *
     *  while (1):
     *    2-1. Request req;
     *         mq_receive(mq_req, &req, sizeof(req), NULL)
     *
     *    2-2. req.op == 'Q' 이면 break (종료 신호)
     *
     *    2-3. 요청 내용 출력
     *         printf("[Server] 요청: %d %c %d\n", req.a, req.op, req.b)
     *
     *    2-4. 계산 수행
     *         Response resp = {0, 1};   // ok = 1 로 초기화
     *         switch (req.op):
     *           '+' → resp.result = req.a + req.b
     *           '-' → resp.result = req.a - req.b
     *           '*' → resp.result = req.a * req.b
     *           '/' → req.b == 0 이면 resp.ok = 0
     *                 아니면 resp.result = req.a / req.b
     *
     *    2-5. mq_send(mq_resp, &resp, sizeof(resp), 0)
     *
     *    2-6. 결과 출력
     * -------------------------------------------------------- */
    while(1)
    {
        Request req;
        ssize_t n = mq_receive(mq_req, (char *)&req, sizeof(Request), NULL);
        if (n == -1) {
            perror("mq_receive");
            break;
        }

        if (req.op == 'Q') {
            break;
        }

        printf("[Server] 요청: %d %c %d\n", req.a, req.op, req.b);

        Response resp = {0, 1};

        switch (req.op) {
            case '+':
                resp.result = req.a + req.b;
                break;
            case '-':
                resp.result = req.a - req.b;
                break;
            case '*':
                resp.result = req.a * req.b;
                break;
            case '/':
                if (req.b == 0) {
                    resp.ok = 0;
                } else {
                    resp.result = req.a / req.b;
                }
                break;
            default:
                resp.ok = 0;
                break;
        }

        if (mq_send(mq_resp, (char *)&resp, sizeof(Response), 0) == -1) {
            perror("mq_send");
            break;
        }

        if (resp.ok) {
            printf("[Server] 결과: %d\n\n", resp.result);
        } else {
            printf("[Server] 결과: 오류\n\n");
        }
    }
    /* TODO */

    /* --------------------------------------------------------
     * TODO 3 : 정리
     *
     *  mq_close(mq_req)
     *  mq_close(mq_resp)
     *  mq_unlink(MQ_REQUEST)   ← 서버가 큐 삭제 담당
     *  mq_unlink(MQ_RESPONSE)
     * -------------------------------------------------------- */

    /* TODO */
    mq_close(mq_req);
    mq_close(mq_resp);
    mq_unlink(MQ_REQUEST);
    mq_unlink(MQ_RESPONSE);


    printf("[Server] 종료\n");
    return EXIT_SUCCESS;
}
