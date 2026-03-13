/*
 * ================================================================
 *  STEP 2 : TCP/IP 소켓 — 요청 / 응답 계산 서버
 *  파일   : calc_server.c
 * ================================================================
 *
 *  [구조]
 *
 *   server                         client
 *   ──────                         ──────
 *   socket()
 *   bind()   ← PORT 9000
 *   listen()
 *   accept() ◄────── connect() ──── client
 *   recv(req) ◄───── send(req) ────
 *                    계산 수행
 *   send(resp) ────► recv(resp)
 *        ...
 *   (클라이언트가 op='Q' 보내면 종료)
 *   close(client_fd)
 *   close(server_fd)
 *
 *  [메시지 구조체]
 *
 *   typedef struct { int a; int b; char op; int _pad; } Request;
 *   typedef struct { int result; int ok; }               Response;
 *
 *  컴파일: gcc -Wall -g -o calc_server calc_server.c
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

#define PORT     9000
#define BACKLOG  5

/* ── 메시지 구조체 (클라이언트와 동일해야 함) ── */
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
     * TODO 1 : 서버 소켓 생성 및 바인드
     *
     *  1-1. socket() 으로 TCP 소켓 생성
     *       domain   = AF_INET
     *       type     = SOCK_STREAM
     *       protocol = 0
     *
     *  1-2. setsockopt() 으로 SO_REUSEADDR 설정
     *       (서버 재시작 시 "Address already in use" 방지)
     *
     *  1-3. struct sockaddr_in 준비
     *       .sin_family      = AF_INET
     *       .sin_port        = htons(PORT)
     *       .sin_addr.s_addr = INADDR_ANY
     *
     *  1-4. bind(server_fd, (struct sockaddr *)&addr, sizeof(addr))
     *
     *  1-5. listen(server_fd, BACKLOG)
     *
     *  각 단계 실패 시 perror() 후 EXIT_FAILURE
     * -------------------------------------------------------- */
    int server_fd = -1;

    /* TODO */
    // 1-1
    server_fd = socket(AF_INET, SOCK_STREAM, 0);
    if (server_fd < 0) {
        perror("Socket failed");
        return EXIT_FAILURE;
    }

    // 1-2
    int opt = 1;
    if (setsockopt(server_fd, SOL_SOCKET, SO_REUSEADDR, &opt, sizeof(opt)) < 0) {
        perror("setsockopt failed");
        return EXIT_FAILURE;
    }

    // 1-3
    struct sockaddr_in address;
    address.sin_family = AF_INET;
    address.sin_port = htons(PORT);
    address.sin_addr.s_addr = INADDR_ANY;

    // 1-4
    if (bind(server_fd, (struct sockaddr *)&address, sizeof(address)) < 0) {
        perror("Bind failed");
        return EXIT_FAILURE;
    }

    // 1-5
    if (listen(server_fd, 3) < 0) {
        perror("Listen failed");
        return EXIT_FAILURE;
    }

    printf("[Server] 시작. PORT %d 에서 대기 중...\n\n", PORT);

    /* --------------------------------------------------------
     * TODO 2 : 클라이언트 연결 수락
     *
     *  struct sockaddr_in client_addr;
     *  socklen_t client_len = sizeof(client_addr);
     *
     *  int client_fd = accept(server_fd,
     *                         (struct sockaddr *)&client_addr,
     *                         &client_len);
     *  실패 시 perror() 후 EXIT_FAILURE
     *
     *  inet_ntoa() 로 클라이언트 IP 출력
     *  printf("[Server] 클라이언트 연결: %s\n", inet_ntoa(client_addr.sin_addr));
     * -------------------------------------------------------- */
    int client_fd = -1;

    /* TODO */
    struct sockaddr_in client_addr;
    socklen_t client_len = sizeof(client_addr);

    client_fd = accept(server_fd, (struct sockaddr *)&client_addr, (socklen_t*)&client_len);
    if (client_fd < 0) {
        perror("Accept failed");
        return EXIT_FAILURE;
    }

    printf("[Server] 클라이언트 연결: %s\n", inet_ntoa(client_addr.sin_addr));

    /* --------------------------------------------------------
     * TODO 3 : 계산 루프
     *
     *  while (1):
     *    3-1. Request req;
     *         recv(client_fd, &req, sizeof(req), 0)
     *         반환값 <= 0 이면 break (연결 종료)
     *
     *    3-2. req.op == 'Q' 이면 break (종료 신호)
     *
     *    3-3. 요청 내용 출력
     *         printf("[Server] 요청: %d %c %d\n", req.a, req.op, req.b);
     *
     *    3-4. 계산 수행
     *         Response resp = {0, 1};   // ok = 1 로 초기화
     *         switch (req.op):
     *           '+' → resp.result = req.a + req.b
     *           '-' → resp.result = req.a - req.b
     *           '*' → resp.result = req.a * req.b
     *           '/' → req.b == 0 이면 resp.ok = 0
     *                 아니면 resp.result = req.a / req.b
     *
     *    3-5. send(client_fd, &resp, sizeof(resp), 0)
     *
     *    3-6. 결과 출력
     * -------------------------------------------------------- */

    /* TODO */
    // 3-1
    while (1) {
        Request req;
        ssize_t n = recv(client_fd, &req, sizeof(req), 0);
        if (n <= 0) {
            break;
        }

    // 3-2
        if (req.op == 'Q') {
            break;
        }
    // 3-3
        printf("[Server] 요청: %d %c %d\n", req.a, req.op, req.b);
    

    // 3-4
        Response resp = { .ok = 1 };
        switch (req.op) {
            case '+': resp.result = req.a + req.b; break;
            case '-': resp.result = req.a - req.b; break;
            case '*': resp.result = req.a * req.b; break;
            case '/':
                if (req.b == 0) { resp.ok = 0; }
                else             { resp.result = req.a / req.b; }
                break;
            default: resp.ok = 0;
        }

    // 3-5
        if (send(client_fd, &resp, sizeof(resp), 0) < 0) {
            perror("Send failed");
        }

    // 3-6
        if (resp.ok)
            printf("[Server] 결과: %d\n\n", resp.result);
        else
            printf("[Server] 결과: 오류\n\n");
    }
    /* --------------------------------------------------------
     * TODO 4 : 정리
     *
     *  close(client_fd)
     *  close(server_fd)
     * -------------------------------------------------------- */

    /* TODO */
    close(client_fd);
    close(server_fd);

    printf("[Server] 종료\n");
    return EXIT_SUCCESS;
}
