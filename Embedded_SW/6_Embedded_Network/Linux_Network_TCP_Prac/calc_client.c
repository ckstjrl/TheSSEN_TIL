/*
 * ================================================================
 *  STEP 2 : TCP/IP 소켓 — 요청 / 응답 계산 클라이언트
 *  파일   : calc_client.c
 * ================================================================
 *
 *  [목표]
 *   클라이언트가 계산 요청을 TCP 소켓으로 전송하면
 *   서버가 계산해서 결과를 돌려준다.
 *
 *  [연결 구성]
 *
 *   client ──── connect(127.0.0.1:9000) ────► server
 *   client ──── send(Request)           ────► server
 *   client ◄─── recv(Response)          ──── server
 *
 *  [메시지 구조체]
 *
 *   typedef struct { int a; int b; char op; int _pad; } Request;
 *   typedef struct { int result; int ok; }               Response;
 *
 *  [동작 흐름]
 *
 *   client                      server
 *   ──────                      ──────
 *   req = {10, 3, '+'}
 *   send(req) ─────────────────► recv(req)
 *                                계산: 10 + 3 = 13
 *                                send(resp)
 *   recv(resp) ◄─────────────────
 *   결과 출력: 10 + 3 = 13
 *
 *  컴파일: gcc -Wall -g -o calc_client calc_client.c
 * ================================================================
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <unistd.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>

#define SERVER_IP  "127.0.0.1"
#define PORT       9000

/* ── 메시지 구조체 (서버와 동일해야 함) ── */
typedef struct {
    int  a;
    int  b;
    char op;    /* '+' '-' '*' '/' 'Q'(종료) */
    char _pad[3];
} Request;

typedef struct {
    int result;
    int ok;     /* 1=성공, 0=오류 */
} Response;

int main(void)
{
    /* --------------------------------------------------------
     * TODO 1 : TCP 소켓 생성 및 서버 연결
     *
     *  1-1. socket() 으로 TCP 소켓 생성
     *       domain   = AF_INET
     *       type     = SOCK_STREAM
     *       protocol = 0
     *       실패 시 perror() 후 EXIT_FAILURE
     *
     *  1-2. struct sockaddr_in 준비
     *       .sin_family      = AF_INET
     *       .sin_port        = htons(PORT)
     *       .sin_addr        = inet_addr(SERVER_IP)
     *         또는 inet_pton(AF_INET, SERVER_IP, &addr.sin_addr)
     *
     *  1-3. connect(sock_fd, (struct sockaddr *)&addr, sizeof(addr))
     *       실패 시 perror() 후 EXIT_FAILURE
     * -------------------------------------------------------- */
    int sock_fd = -1;

    /* TODO */
    // 1-1
    sock_fd = socket(AF_INET, SOCK_STREAM, 0);
    if (sock_fd < 0) {
        perror("Socket creation failed");
        return EXIT_FAILURE;
    }
    // 1-2
    struct sockaddr_in server_address;
    
    server_address.sin_family = AF_INET;
    server_address.sin_port = htons(PORT);

    // 서버 주소 변환
    if (inet_pton(AF_INET, SERVER_IP, &server_address.sin_addr) <= 0) {
        perror("Invalid address");
        return EXIT_FAILURE;
    }

    // 1-3
    // 서버에 연결
    if (connect(sock_fd, (struct sockaddr *)&server_address, sizeof(server_address)) < 0) {
        perror("Connection failed");
        return EXIT_FAILURE;
    }

    printf("[Client] 서버(%s:%d)에 연결됨\n\n", SERVER_IP, PORT);

    /* ── 테스트할 계산 목록 ── */
    Request tests[] = {
        { 10,  3, '+', {0} },   /* 10 + 3 = 13  */
        { 20,  4, '-', {0} },   /* 20 - 4 = 16  */
        {  6,  7, '*', {0} },   /*  6 * 7 = 42  */
        { 15,  3, '/', {0} },   /* 15 / 3 = 5   */
        {  9,  0, '/', {0} },   /*  9 / 0 = 오류 */
    };
    int n = sizeof(tests) / sizeof(tests[0]);

    /* --------------------------------------------------------
     * TODO 2 : 각 요청 전송 → 응답 수신 → 출력
     *
     *  for i = 0 .. n-1:
     *    2-1. send(sock_fd, &tests[i], sizeof(Request), 0)
     *         실패(반환값 < 0) 시 perror() 후 break
     *
     *    2-2. Response resp;
     *         recv(sock_fd, &resp, sizeof(Response), 0)
     *         실패(반환값 <= 0) 시 break
     *
     *    2-3. 결과 출력:
     *         if (resp.ok)
     *           printf("%d %c %d = %d\n", a, op, b, result)
     *         else
     *           printf("%d %c %d = 오류\n", a, op, b)
     * -------------------------------------------------------- */

    /* TODO */
    // 2-1
    for (int i = 0; i < n; i++) {
        if (send(sock_fd, &tests[i], sizeof(Request), 0) < 0) {
            perror("Send error");
            break;
        }

    // 2-2
        Response resp;
        ssize_t r = recv(sock_fd, &resp, sizeof(Response), 0);
        if (r <= 0) {
            break;
        }

    // 2-3
        if (resp.ok) {
            printf("[Client] %d %c %d = %d\n", tests[i].a, tests[i].op, tests[i].b, resp.result);
        }
        else {
            printf("[Client] %d %c %d = 오류\n", tests[i].a, tests[i].op, tests[i].b);
        }
    }
    /* --------------------------------------------------------
     * TODO 3 : 서버 종료 신호 전송
     *
     *  op = 'Q' 인 Request 를 전송하면 서버가 루프를 탈출하고 종료됨
     *
     *  Request quit = { 0, 0, 'Q', {0} };
     *  send(sock_fd, &quit, sizeof(quit), 0);
     * -------------------------------------------------------- */

    /* TODO */
    Request quit = { 0, 0, 'Q', {0} };
    send(sock_fd, &quit, sizeof(quit), 0);
    /* --------------------------------------------------------
     * TODO 4 : 정리
     *
     *  close(sock_fd)
     * -------------------------------------------------------- */

    /* TODO */
    close(sock_fd);

    printf("\n[Client] 완료\n");
    return EXIT_SUCCESS;
}
