# 임베디드 SW 네트워크 프로그래밍 day02

날짜: 2026년 3월 13일

# 네트워크 (continue)

## DNS

### DNS 개요

DNS (Domain Name System)는 도메인 이름을 IP 주소로 변환하는 시스템

사용자가 웹사이트를 방문할 때 도메인 이름을 입력하면, DNS가 이를 IP 주소로 변환하여 실제 서버에 접속할 수 있게 도와줌

도메인 이름: 사람이 기억하기 쉬운 주소(예: [www.example.com](http://www.example.com/) )

IP 주소: 컴퓨터가 식별할 수 있는 주소(예: 192.168.1.1 )

### DNS의 동작 방식

1. 사용자가 도메인 이름을 입력: 예를 들어, [www.example.com](http://www.example.com/)
2. DNS 리졸버: 사용자의 요청을 처리하는 DNS 서버는 해당 도메인 이름에 대한 IP 주소를 찾습니다.
3. DNS 서버: 여러 DNS 서버가 계층적으로 동작하여 요청된 도메인의 IP 주소를 제공합니다.

### DNS 쿼리와 응답

- DNS 쿼리: 클라이언트가 DNS 서버에 도메인 이름에 대한 IP 주소를 요청하는 메시지입니다.
- DNS 응답: DNS 서버가 도메인 이름에 대한 IP 주소를 반환하는 메시지입니다.

### DNS 메시지 형식

- 헤더: 요청의 유형과 상태 등을 나타내는 정보
- 질문 섹션: 요청할 도메인 이름과 타입 (예: A 레코드)
- 답변 섹션: 도메인에 대한 IP 주소 등의 답변

### 예제

server

```c
/*
    소켓의 수신 버퍼(SO_RCVBUF)와 송신 버퍼(SO_SNDBUF) 크기를 적절히 설정하면, 
    대용량 데이터 송수신 시 성능을 최적화할 수 있다. 버퍼 크기가 너무 작으면 데이터 전송 속도가 느려지고, 
    너무 크면 메모리 낭비가 발생할 수 있다.
*/

#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <arpa/inet.h>

#define PORT 8081
#define BUFFER_SIZE 8192  // 수신 버퍼 크기 설정

int main() {
    int server_fd, new_socket, opt = 1;
    struct sockaddr_in address;
    int addrlen = sizeof(address);
    char buffer[BUFFER_SIZE] = {0};
    int bufsize = BUFFER_SIZE;

    // 소켓 생성
    if ((server_fd = socket(AF_INET, SOCK_STREAM, 0)) == 0) {
        perror("Socket failed");
        exit(EXIT_FAILURE);
    }

    // 버퍼 크기 설정
    if (setsockopt(server_fd, SOL_SOCKET, SO_RCVBUF, &bufsize, sizeof(bufsize)) < 0) {
        perror("버퍼 크기 설정 실패");
        exit(1);
    }

    // 소켓 옵션: 서버가 종료된 후 빠르게 재실행될 수 있도록 포트를 즉시 재사용하는 옵션
    if (setsockopt(server_fd, SOL_SOCKET, SO_REUSEADDR, &opt, sizeof(opt)) < 0) {
        perror("setsockopt failed");
        exit(EXIT_FAILURE);
    }
    
    address.sin_family = AF_INET;
    address.sin_addr.s_addr = INADDR_ANY;
    address.sin_port = htons(PORT);

    // 바인딩
    if (bind(server_fd, (struct sockaddr *)&address, sizeof(address)) < 0) {
        perror("Bind failed");
        exit(EXIT_FAILURE);
    }

    // 연결 대기
    if (listen(server_fd, 3) < 0) {
        perror("Listen failed");
        exit(EXIT_FAILURE);
    }

    printf("Waiting for connections...\n");

    // 클라이언트 연결 수락
    if ((new_socket = accept(server_fd, (struct sockaddr *)&address, (socklen_t*)&addrlen)) < 0) {
        perror("Accept failed");
        exit(EXIT_FAILURE);
    }

    // 클라이언트로부터 데이터 받기
    read(new_socket, buffer, BUFFER_SIZE);
    printf("Client message: %s\n", buffer);

    // 클라이언트로 데이터 보내기
    send(new_socket, "Hello from server", 17, 0);
    printf("Message sent to client\n");

    // 소켓 종료
    close(new_socket);
    close(server_fd);

    return 0;
}
```