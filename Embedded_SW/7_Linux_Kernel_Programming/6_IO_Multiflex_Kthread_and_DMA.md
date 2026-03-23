# 임베디드 리눅스 커널 프로그래밍 day06

날짜: 2026년 3월 23일

# CHAP 16. I/O 다중화 — poll / select / epoll

## 왜 I/O 다중화가 필요한가

### Blocking I/O의 구조적 한계

단일 스레드에서 여러 디바이스를 동시에 감시하려면 어떻게 해야 할까?

Blocking I/O 방식에서는 `read(fd_button0, ...)` 호출 순간 프로세스가 잠들기 때문에, 다른 디바이스에서 이벤트가 발생해도 알 수 없다.

```c
/* 잘못된 접근: 순차적 Blocking I/O */
read(fd_button0, buf, len);  /* ← BTN0 이벤트까지 여기서 멈춤 */
read(fd_button1, buf, len);  /* BTN0 이벤트가 와야 여기 도달 */
read(fd_sensor,  buf, len);  /* BTN1 이벤트가 와야 여기 도달 */
```

이 코드는 BTN0 → BTN1 → Sensor 순서로만 이벤트를 처리할 수 있다.

### 멀티 스레드 방식의 비용

```c
/* 스레드 방식: 디바이스별 전담 스레드 */
void *thread_btn0(void *arg) { while(1) read(fd_btn0, ...); }
void *thread_btn1(void *arg) { while(1) read(fd_btn1, ...); }
void *thread_sensor(void *arg) { while(1) read(fd_sensor, ...); }
```

동작은 하지만 임베디드 환경에서 스레드를 무한정 생성하는 것은 **메모리와 컨텍스트 스위칭 비용** 측면에서 비효율적이다.

### I/O 다중화: 단일 스레드로 여러 파일 디스크립터 감시

- *I/O 다중화(I/O Multiplexing)**는 하나의 스레드가 여러 파일 디스크립터를 동시에 감시하고, 그 중 이벤트가 발생한 디스크립터만 처리하는 기법이다.

```c
/* I/O 다중화: 단일 스레드로 3개 디바이스 동시 감시 */
struct pollfd fds[3];
fds[0].fd = fd_btn0;   fds[0].events = POLLIN;
fds[1].fd = fd_btn1;   fds[1].events = POLLIN;
fds[2].fd = fd_sensor; fds[2].events = POLLIN;

while (1) {
    int ret = poll(fds, 3, 5000);   /* 3개 FD 동시 감시, 5초 타임아웃 */
    if (ret > 0) {
        if (fds[0].revents & POLLIN) handle_button0();
        if (fds[1].revents & POLLIN) handle_button1();
        if (fds[2].revents & POLLIN) handle_sensor();
    }
}
```

Linux 커널은 이를 위해 `select()`, `poll()`, `epoll()` 세 가지 시스템 콜을 제공한다.

## 세 가지 I/O 다중화 메커니즘

### `select()` — 가장 오래된 방식

BSD Unix에서 시작된 가장 오래된 I/O 다중화 메커니즘이다.

```c
#include <sys/select.h>

int select(int nfds,
           fd_set *readfds,    /* 읽기 감시 FD 집합 */
           fd_set *writefds,   /* 쓰기 감시 FD 집합 */
           fd_set *exceptfds,  /* 예외 감시 FD 집합 */
           struct timeval *timeout);
```

**핵심 특성:**

- `fd_set`은 비트맵(bitmask) 구조로, 각 비트가 하나의 파일 디스크립터에 대응
- `FD_SETSIZE` 상수(기본 1024)에 의해 감시 가능한 최대 FD 번호가 제한됨
- 호출할 때마다 커널에 `fd_set` 전체를 복사해야 하므로, FD 수가 많을수록 오버헤드 증가
- 반환 후 어떤 FD에 이벤트가 있는지 모든 비트를 순회(O(n))해야 함
- `select()`가 반환하면 `fd_set`이 변경되므로, **매번 재초기화**해야 함

**사용 패턴:**

```c
fd_set readfds;
struct timeval tv;

while (1) {
    FD_ZERO(&readfds);          /* 매번 재초기화 필수 */
    FD_SET(fd, &readfds);
    tv.tv_sec = 5; tv.tv_usec = 0;

    int ret = select(fd + 1, &readfds, NULL, NULL, &tv);
    if (ret > 0 && FD_ISSET(fd, &readfds)) {
        /* fd에 읽을 데이터가 있다 */
    }
}
```

### `poll()` — select의 개선

`poll()`은 `select()`의 FD 개수 제한과 `fd_set` 재초기화 문제를 해결하기 위해 System V에서 도입되었다.

```c
#include <poll.h>

int poll(struct pollfd *fds, nfds_t nfds, int timeout);

struct pollfd {
    int   fd;       /* 감시할 파일 디스크립터 */
    short events;   /* 감시할 이벤트 (입력) */
    short revents;  /* 발생한 이벤트 (출력) */
};
```

**핵심 특성:**

- `pollfd` 배열을 사용하므로 **FD 개수 제한이 없다** (커널 메모리 허용 범위 내)
- `events`(요청)와 `revents`(결과)가 분리되어 있어 **매번 재초기화할 필요가 없다**
- 여전히 커널이 매 호출마다 전체 `pollfd` 배열을 순회하므로 시간 복잡도는 O(n)
- `select()`보다 인터페이스가 깔끔하고 사용하기 쉽다

**주요 이벤트 플래그:**

| 플래그 | 방향 | 의미 |
| --- | --- | --- |
| `POLLIN` | events/revents | 읽기 데이터 있음 (일반 데이터) |
| `POLLRDNORM` | events/revents | 읽기 데이터 있음 (POLLIN과 동의어) |
| `POLLOUT` | events/revents | 쓰기 가능 |
| `POLLWRNORM` | events/revents | 쓰기 가능 (POLLOUT과 동의어) |
| `POLLPRI` | events/revents | 긴급(OOB) 데이터 있음 |
| `POLLERR` | revents만 | 에러 발생 (설정 불필요, 자동 감시) |
| `POLLHUP` | revents만 | 연결 끊김 (설정 불필요, 자동 감시) |
| `POLLNVAL` | revents만 | 유효하지 않은 FD |

**타임아웃 동작:**

| timeout 값 | 동작 |
| --- | --- |
| -1 | 이벤트 발생까지 무한 대기 |
| 0 | 즉시 반환 (non-blocking 체크) |
| 양수 n | n 밀리초까지 대기 후 타임아웃 |

### `epoll()` — 대규모 FD를 위한 이벤트 기반 설계

`epoll()`은 Linux 2.6에서 추가된 Linux 전용 메커니즘이다. 수천~수만 개의 FD를 감시할 때 `select()`/`poll()`과는 비교할 수 없는 성능을 보여준다.

```c
#include <sys/epoll.h>

int epoll_create1(int flags);                                              /* epoll 인스턴스 생성 */
int epoll_ctl(int epfd, int op, int fd, struct epoll_event *event);        /* FD 등록/수정/삭제 */
int epoll_wait(int epfd, struct epoll_event *events, int maxevents, int timeout); /* 이벤트 대기 */
```

**핵심 특성:**

- FD를 `epoll_ctl()`로 **한 번만 등록**하면, 이후 `epoll_wait()`는 이벤트가 발생한 FD만 반환
- 커널 내부에서 콜백 기반으로 이벤트를 추적하므로 시간 복잡도가 **O(1)**이다
- Level-Triggered(LT) 모드와 Edge-Triggered(ET) 모드를 지원

**사용 패턴:**

```c
int epfd = epoll_create1(0);
struct epoll_event ev, events[10];

ev.events = EPOLLIN;
ev.data.fd = fd;
epoll_ctl(epfd, EPOLL_CTL_ADD, fd, &ev);   /* 한 번만 등록 */

while (1) {
    int n = epoll_wait(epfd, events, 10, 5000);
    for (int i = 0; i < n; i++) {
        if (events[i].data.fd == fd) {
            /* 이벤트 처리 — 발생한 것만 순회 */
        }
    }
}
```

### 세 가지 메커니즘 종합 비교

| 항목 | `select()` | `poll()` | `epoll()` |
| --- | --- | --- | --- |
| 등장 시기 | BSD (1983) | System V | Linux 2.6 (2004) |
| FD 개수 제한 | `FD_SETSIZE` (1024) | 제한 없음 | 제한 없음 |
| FD 집합 구조 | bitmask (`fd_set`) | 배열 (`pollfd`) | 커널 내부 관리 |
| 매 호출 복사 | `fd_set` 전체 | `pollfd` 전체 | 없음 (등록 1회) |
| 이벤트 탐색 | 전체 순회 O(n) | 전체 순회 O(n) | 발생분만 O(1) |
| 재초기화 | 매번 필요 | 불필요 | 불필요 |
| 이식성 | POSIX (모든 Unix) | POSIX (대부분 Unix) | Linux 전용 |
| 적합 FD 수 | < 수십 | 수십 ~ 수백 | 수백 ~ 수만 |

### 임베디드 드라이버에서의 선택 기준

> **드라이버에서 `file_operations.poll` 핸들러를 구현하면 `select()`, `poll()`, `epoll()` 세 가지 모두를 자동으로 지원한다.**
> 

커널 내부에서 `select()`와 `epoll()`은 결국 드라이버의 `.poll` 핸들러를 호출하는 공통 경로를 거친다. 따라서 드라이버 개발자는 `.poll` 하나만 올바르게 구현하면 된다.

## 드라이버에서 `poll()` 핸들러 구현

### `file_operations.poll` 멤버

```c
struct file_operations {
    /* ... 기존 멤버들 ... */
    __poll_t (*poll)(struct file *filp, struct poll_table_struct *wait);
    /* ... */
};
```

드라이버는 이 핸들러에서 **두 가지 작업**을 수행해야 한다.

1. `poll_wait()`를 호출하여 wait queue를 poll table에 등록한다
2. **현재 이벤트 상태를 반환**한다 (예: 읽을 데이터가 있으면 `POLLIN | POLLRDNORM`)

### `poll_wait()` 함수

```c
#include <linux/poll.h>

void poll_wait(struct file *filp,
               wait_queue_head_t *queue,
               poll_table *wait);
```

> `poll_wait()`는 이름과 달리 **대기하지 않는다**. 이 함수의 실제 역할은 "이 wait queue에서 이벤트가 발생하면 나를 깨워 달라"고 커널에 등록하는 것이다.
> 

**동작 흐름:**

```
[유저 공간]                    [커널 공간]

poll(fds, 1, 5000) ────────→  sys_poll()
                                   │
                               ├── do_poll() 루프 시작
                               │       │
                               │   ├── 드라이버의 .poll() 호출
                               │   │       │
                               │   │   ├── poll_wait(file, &wq, wait)
                               │   │   │       → wq를 poll table에 등록
                               │   │   │
                               │   │   └── 이벤트 마스크 반환 (예: POLLIN)
                               │   │
                               │   ├── 반환값이 0이 아니면 → 즉시 반환
                               │   │
                               │   └── 반환값이 0이면 → schedule_timeout()
                               │           → 프로세스 sleep
                               │
                               ├── (이벤트 발생: ISR이 wake_up 호출)
                               │
                               ├── do_poll() 루프 재실행
                               │       └── .poll() 재호출 → 이번엔 POLLIN 반환
                               │
                               └── 유저 공간에 결과 반환
```

**핵심 포인트:**

- `poll_wait()`는 sleep하지 않는다. 단지 wait queue를 등록할 뿐이다
- `.poll` 핸들러가 0을 반환하면 커널이 sleep을 결정한다
- `.poll` 핸들러가 0이 아닌 값을 반환하면 즉시 유저 공간에 결과를 돌려준다
- ISR에서 `wake_up_interruptible()`이 호출되면 커널은 `.poll`을 다시 호출하여 상태를 재확인한다

### `.poll` 핸들러 구현 패턴

`.poll` 핸들러의 표준 구현 패턴은 다음과 같다.

```c
static unsigned int my_poll(struct file *file, poll_table *wait)
{
    unsigned int mask = 0;

    /* 1단계: wait queue 등록 */
    poll_wait(file, &my_wait_queue, wait);

    /* 2단계: 현재 이벤트 상태 확인 후 마스크 설정 */
    if (data_available) {
        mask |= POLLIN | POLLRDNORM;    /* 읽기 가능 */
    }
    if (write_space_available) {
        mask |= POLLOUT | POLLWRNORM;   /* 쓰기 가능 */
    }
    if (error_occurred) {
        mask |= POLLERR;                /* 에러 발생 */
    }

    return mask;
}
```

**반환값 규칙:**

| 반환 마스크 | 의미 | `poll()`에서 | `select()`에서 |
| --- | --- | --- | --- |
| `POLLIN | POLLRDNORM` | 일반 데이터 읽기 가능 | `revents`에 POLLIN 설정 | `readfds`에 FD 설정 |
| `POLLOUT | POLLWRNORM` | 쓰기 가능 | `revents`에 POLLOUT 설정 | `writefds`에 FD 설정 |
| `POLLPRI` | 긴급 데이터 있음 | `revents`에 POLLPRI 설정 | `exceptfds`에 FD 설정 |
| `POLLERR` | 에러 발생 | `revents`에 POLLERR 설정 | 모든 FD 집합에 설정 |
| `POLLHUP` | 장치 연결 끊김 | `revents`에 POLLHUP 설정 | 모든 FD 집합에 설정 |
| 0 | 이벤트 없음 | 대기 계속 | 대기 계속 |

### `poll_wait()`와 `wake_up_interruptible()`의 관계

드라이버에서 `.poll` 핸들러와 이벤트 발생 지점은 **반드시 같은 wait queue**를 사용해야 한다.

```c
/* 드라이버 전역 */
static DECLARE_WAIT_QUEUE_HEAD(wq);
static int flag = 0;

/* ISR: 이벤트 발생 시 */
static irqreturn_t button_isr(int irq, void *dev_id)
{
    flag = 1;                        /* 이벤트 플래그 설정 */
    wake_up_interruptible(&wq);      /* 같은 wq를 깨운다 */
    return IRQ_HANDLED;
}

/* .poll 핸들러: 커널이 호출 */
static unsigned int my_poll(struct file *file, poll_table *wait)
{
    poll_wait(file, &wq, wait);      /* 같은 wq에 등록한다 */

    if (flag) {
        flag = 0;                    /* 이벤트 소비 */
        return POLLIN | POLLRDNORM;
    }
    return 0;
}
```

> `poll_wait()`에서 등록한 wait queue와 `wake_up_interruptible()`에서 깨우는 wait queue가 일치하지 않으면, `poll()`은 영원히 반환되지 않는다. 이것은 가장 흔한 실수 중 하나다.
> 

### 주의사항: flag 변수의 동시성 문제

`flag` 변수는 ISR(인터럽트 컨텍스트)과 `.poll` 핸들러(프로세스 컨텍스트)가 동시에 접근할 수 있다. 엄밀한 구현에서는 `atomic_t`를 사용하거나 `spinlock`으로 보호하는 것이 안전하다.

```c
/* 개선된 버전: atomic_t 사용 */
static atomic_t event_flag = ATOMIC_INIT(0);

static irqreturn_t button_isr(int irq, void *dev_id)
{
    atomic_set(&event_flag, 1);
    wake_up_interruptible(&wq);
    return IRQ_HANDLED;
}

static unsigned int my_poll(struct file *file, poll_table *wait)
{
    poll_wait(file, &wq, wait);

    /* atomic_cmpxchg: "현재 값이 1이면 0으로 바꾸고 이전 값(1)을 반환" */
    if (atomic_cmpxchg(&event_flag, 1, 0) == 1) {
        return POLLIN | POLLRDNORM;
    }
    return 0;
}
```

> `atomic_cmpxchg(&event_flag, 1, 0)`는 이벤트 플래그의 확인과 소비가 하나의 원자 연산으로 이루어져 race condition이 방지된다.
> 

## 31_iomultiplex 예제 전체 분석

### 예제 구성 파일

```
31_iomultiplex/
├── led_poll_driver.c  ← 커널 드라이버 (poll 지원)
├── ledpoll.c          ← 유저 앱 (poll 사용)
└── Makefile           ← 크로스 빌드용
```

### 커널 드라이버 전역 변수

| 변수 | 용도 |
| --- | --- |
| `led_status` | 현재 LED 상태 (0=OFF, 1=ON) |
| `wq` | wait queue — `.poll`과 ISR이 공유 |
| `flag` | 이벤트 발생 플래그 — ISR이 설정, `.poll`이 확인 |
| `button_last_state` | 디바운스 용도 — 상태 변화가 있을 때만 이벤트 발생 |

### ISR 핵심 동작

```c
static irqreturn_t button_isr(int irq, void *dev_id)
{
    int button_state = gpio_get_value(GPIO_BUTTON_PIN);

    /* 실제 상태 변화가 있을 때만 이벤트 발생 */
    if (button_state != button_last_state) {
        button_last_state = button_state;
        flag = 1;                         /* ← 이벤트 플래그 설정 */
        wake_up_interruptible(&wq);        /* ← 대기 중인 poll()을 깨움 */
    }
    return IRQ_HANDLED;
}
```

ISR의 핵심 동작 두 줄:

1. `flag = 1` — `.poll` 핸들러가 확인할 이벤트 플래그를 설정한다
2. `wake_up_interruptible(&wq)` — `poll_wait()`에서 등록한 프로세스를 깨운다

`button_last_state`와의 비교는 **간이 디바운스** 역할을 한다. 같은 값이 반복 인터럽트로 들어오면 이벤트를 무시한다.

### write() 핸들러 — 이벤트도 함께 발생

```c
static ssize_t led_write(struct file *file, const char __user *buf,
                         size_t len, loff_t *offset)
{
    /* ... copy_from_user ... */

    /* 상태가 실제로 변할 때만 이벤트를 발생시킨다 */
    if (kbuf[0] == '1' && led_status != 1) {
        gpio_set_value(GPIO_LED_PIN, 1);
        led_status = 1;
        flag = 1;                        /* ← write로 인한 이벤트 */
        wake_up_interruptible(&wq);
    } else if (kbuf[0] == '0' && led_status != 0) {
        gpio_set_value(GPIO_LED_PIN, 0);
        led_status = 0;
        flag = 1;                        /* ← write로 인한 이벤트 */
        wake_up_interruptible(&wq);
    }
    return len;
}
```

> LED 상태가 **실제로 변경될 때만** `flag`를 설정하고 `wake_up_interruptible()`을 호출한다. 이미 ON 상태에서 다시 '1'을 쓰면 아무 이벤트도 발생하지 않는다. 이것은 **불필요한 이벤트 폭풍을 방지**하는 중요한 패턴이다.
> 

### `.poll` 핸들러 — 이 챕터의 핵심 함수

```c
static unsigned int led_poll(struct file *file, poll_table *wait)
{
    poll_wait(file, &wq, wait);   /* 1. wait queue 등록 — sleep하지 않는다 */

    if (flag) {
        flag = 0;                          /* 2. 이벤트 소비 (리셋) */
        printk(KERN_INFO "[led_poll]읽기 준비 완료\n");
        return POLLIN | POLLRDNORM;        /* 3. 읽기 가능 이벤트 반환 */
    }

    printk(KERN_INFO "[led_poll]0 반환\n");
    return 0;                              /* 4. 이벤트 없음 */
}
```

- **①** `poll_wait()` — sleep하지 않는다. "이 wq에서 이벤트가 오면 나를 깨워라"고 등록만 한다
- **②** `flag = 0` — 이벤트를 "소비"한다. 리셋하지 않으면 다음 `poll()`에서도 즉시 반환된다
- **③** `POLLIN | POLLRDNORM` — "일반 데이터를 읽을 수 있다"는 이벤트 마스크
- **④** `0` 반환 — "아직 이벤트 없다". 커널이 이 값을 보고 프로세스를 sleep시킨다

### `file_operations` 구조체 등록

```c
static struct file_operations fops = {
    .owner   = THIS_MODULE,
    .open    = led_open,
    .release = led_release,
    .read    = led_read,
    .write   = led_write,
    .poll    = led_poll,   /* ← poll 핸들러 등록 */
};
```

`.poll = led_poll`을 등록하는 것만으로 이 드라이버는 `poll()`, `select()`, `epoll()` **모두를 지원**한다.

## 유저 앱 분석

### ledpoll.c — poll() 유저 앱

```c
int main()
{
    struct pollfd fds[1];
    int fd, ret;

    fd = open(DEVICE_PATH, O_RDONLY);
    if (fd < 0) { perror("open"); return 1; }

    fds[0].fd = fd;
    fds[0].events = POLLIN;   /* 읽기 이벤트 감시 */

    while (1) {
        ret = poll(fds, 1, -1);   /* 무한 대기 */
        if (ret < 0) { perror("poll"); break; }

        /* 읽기 이벤트 확인 */
        if (fds[0].revents & POLLIN) {
            printf("LED or button state changed!\n");
        }

        /* 에러/연결 끊김 이벤트 확인 */
        if (fds[0].revents & (POLLERR | POLLHUP)) {
            printf("Error or hangup detected on device!\n");
            break;
        }
    }

    close(fd);
    return 0;
}
```

**유저 앱 동작 흐름:**

1. `/dev/led_device`를 `O_RDONLY`로 연다
2. `pollfd` 구조체에 FD와 감시할 이벤트(`POLLIN`)를 설정한다
3. `poll(fds, 1, -1)` 호출 — 이벤트 발생까지 무한 대기
4. 반환 후 `revents`를 확인하여 이벤트 종류를 판별한다
5. `POLLIN`이면 상태 변화를 출력하고 다시 `poll()`로 돌아간다
6. `POLLERR` 또는 `POLLHUP`이면 루프를 종료한다

### 빌드 및 배포 절차

```bash
# [Ubuntu VM] SDK 환경 활성화
source /opt/pkg/petalinux/zybo_project/images/linux/sdk/\
    environment-setup-cortexa9t2hf-neon-xilinx-linux-gnueabi

# 드라이버 빌드 및 배포
cd ~/zynq_dd/labs/31_iomultiplex/
make
make deploy

# 유저 앱 크로스 빌드
$(CC) -o ledpoll ledpoll.c
cp ledpoll /nfsroot/home/petalinux/zynq_dd/labs/

# [Zybo 보드] 드라이버 로드 및 앱 실행
insmod /home/petalinux/zynq_dd/labs/led_poll_driver.ko
/home/petalinux/zynq_dd/labs/ledpoll &
```

### 이벤트 발생 방법

**방법 1 — 버튼 누르기:**

```bash
# Zybo 보드의 BTN5 버튼을 손으로 누른다
# 콘솔 출력:
#   LED or button state changed!
```

**방법 2 — write()로 LED 상태 변경:**

```bash
echo 1 > /dev/led_device   # → LED ON + 콘솔 출력: LED or button state changed!
echo 0 > /dev/led_device   # → LED OFF + 콘솔 출력: LED or button state changed!
```

**dmesg 로그로 .poll 호출 패턴 관찰:**

```
# poll() 호출 직후:  .poll → flag=0 → "0 반환" → sleep
# 버튼 누름: ISR → flag=1 → wake_up → .poll 재호출 → "읽기 준비 완료" → POLLIN 반환
```

### strace로 시스템 콜 추적

```bash
strace -e poll /home/petalinux/zynq_dd/labs/ledpoll

# 예상 출력:
# poll([{fd=3, events=POLLIN}], 1, -1)  = 1 ([{fd=3, revents=POLLIN}])
# poll([{fd=3, events=POLLIN}], 1, -1)  ← 여기서 대기 중 (버튼 대기)
```

- `fd=3`: `/dev/led_device`의 파일 디스크립터 번호
- `events=POLLIN`: 읽기 이벤트 감시
- `= 1`: 이벤트가 발생한 FD 개수
- `revents=POLLIN`: 실제 발생한 이벤트

## select() 버전으로 변환 실습

같은 드라이버(`led_poll_driver.ko`)를 사용하면서 유저 앱만 `select()` 버전으로 변경한다. **드라이버의 `.poll` 핸들러는 수정하지 않는다.**

### ledselect.c — select() 유저 앱

```c
int main()
{
    int fd, ret;
    fd_set readfds;
    struct timeval tv;

    fd = open(DEVICE_PATH, O_RDONLY);
    if (fd < 0) { perror("open"); return 1; }

    while (1) {
        /* select()는 fd_set을 변경하므로 매번 재초기화 */
        FD_ZERO(&readfds);
        FD_SET(fd, &readfds);

        /* 타임아웃 설정: 5초 (매번 재설정 필요) */
        tv.tv_sec = 5;
        tv.tv_usec = 0;

        ret = select(fd + 1, &readfds, NULL, NULL, &tv);

        if (ret < 0) {
            perror("select");
            break;
        } else if (ret == 0) {
            printf("Timeout! No event in 5 seconds.\n");
            continue;
        }

        /* 이벤트 발생 */
        if (FD_ISSET(fd, &readfds)) {
            printf("LED or button state changed! (via select)\n");
        }
    }

    close(fd);
    return 0;
}
```

### poll() vs select() 코드 비교

| 항목 | `poll()` 버전 | `select()` 버전 |
| --- | --- | --- |
| FD 집합 구조 | `struct pollfd fds[1]` | `fd_set readfds` |
| 초기화 | 최초 1회 | `FD_ZERO` + `FD_SET` 매번 |
| 호출 | `poll(fds, 1, -1)` | `select(fd+1, &readfds, ...)` |
| 결과 확인 | `fds[0].revents & POLLIN` | `FD_ISSET(fd, &readfds)` |
| 타임아웃 단위 | 밀리초 (`int`) | `struct timeval` (초+마이크로초) |
| 에러 확인 | `revents & POLLERR` | `select()` 반환값 -1 확인 |

> `select()` 버전에는 5초 타임아웃을 추가했다. 이렇게 하면 "이벤트가 없어도 주기적으로 다른 작업을 수행할 수 있는" 패턴을 실습할 수 있다.
> 

## 다중 디바이스 감시 확장

### ledpoll_multi.c — 디바이스 + stdin 동시 감시

```c
int main()
{
    struct pollfd fds[2];
    int fd_dev, ret;
    char buf[64];

    fd_dev = open(DEVICE_PATH, O_RDWR);

    /* FD 0: 디바이스 — 버튼 이벤트 감시 */
    fds[0].fd = fd_dev;
    fds[0].events = POLLIN;

    /* FD 1: 표준 입력(stdin) — 키보드 입력 감시 */
    fds[1].fd = STDIN_FILENO;   /* 0 */
    fds[1].events = POLLIN;

    while (1) {
        ret = poll(fds, 2, 10000);   /* 2개 FD, 10초 타임아웃 */

        if (ret == 0) { printf("[timeout] No event in 10 seconds\n"); continue; }

        /* 디바이스 이벤트 확인 */
        if (fds[0].revents & POLLIN) {
            printf("[device] Button or LED state changed!\n");
        }

        /* 키보드 입력 확인 */
        if (fds[1].revents & POLLIN) {
            int n = read(STDIN_FILENO, buf, sizeof(buf) - 1);
            buf[n - 1] = '\0';   /* 개행 문자 제거 */

            if (strcmp(buf, "on") == 0) {
                write(fd_dev, "1", 1);
                printf("[keyboard] LED ON command sent\n");
            } else if (strcmp(buf, "off") == 0) {
                write(fd_dev, "0", 1);
                printf("[keyboard] LED OFF command sent\n");
            } else if (strcmp(buf, "quit") == 0) {
                break;
            }
        }
    }

    close(fd_dev);
    return 0;
}
```

이 예제의 핵심은 `poll(fds, 2, 10000)`에서 **2개의 FD를 동시에 감시**한다는 점이다. 버튼을 누르면 디바이스 이벤트가, 키보드를 입력하면 stdin 이벤트가 발생하고, 둘 다 하나의 `poll()` 호출로 감지된다.

## 자주 발생하는 오류와 디버깅

### `poll()` 즉시 반환 문제

**증상**: `poll()`이 대기하지 않고 즉시 반환을 반복한다.

**원인**: `.poll` 핸들러에서 `flag`를 리셋하지 않거나, 항상 `POLLIN`을 반환하는 경우.

```c
/* 잘못된 코드: flag 리셋 누락 */
if (flag) {
    /* flag = 0; ← 이 줄이 빠지면 무한 반환 */
    return POLLIN | POLLRDNORM;
}
```

**해결**: `flag = 0;`을 반드시 추가한다. 이벤트를 "소비"하지 않으면 `.poll`이 호출될 때마다 같은 이벤트가 반복 보고된다.

### `poll()` 영원히 반환되지 않는 문제

**증상**: 버튼을 눌러도 `poll()`이 깨어나지 않는다.

**원인 1**: `poll_wait()`와 `wake_up_interruptible()`이 서로 다른 wait queue를 사용한다.

```c
/* 잘못된 코드: wait queue 불일치 */
static DECLARE_WAIT_QUEUE_HEAD(wq_poll);    /* poll용 */
static DECLARE_WAIT_QUEUE_HEAD(wq_isr);     /* ISR용 */

static irqreturn_t button_isr(...) {
    wake_up_interruptible(&wq_isr);     /* ← wq_isr 사용 */
}

static unsigned int led_poll(...) {
    poll_wait(file, &wq_poll, wait);    /* ← wq_poll 사용 (불일치!) */
}
```

**해결**: 반드시 같은 wait queue를 공유한다.

**원인 2**: ISR이 `flag`를 설정하지 않거나, 인터럽트가 실제로 발생하지 않는다.

```bash
# /proc/interrupts에서 인터럽트 발생 횟수 확인
cat /proc/interrupts | grep button_irq
# 버튼을 누른 후 다시 확인하여 카운트가 증가하는지 비교
```

### `POLLERR` 반환 원인

`POLLERR`은 디바이스에서 에러가 발생했음을 의미한다. 주요 원인:

- 드라이버가 `.poll` 핸들러를 구현하지 않은 경우 (커널 기본 동작으로 `POLLERR` 반환)
- `.poll` 핸들러 내부에서 NULL 포인터 참조 등의 오류
- 디바이스 노드가 삭제되었거나 드라이버가 언로드된 경우

### 디버깅 체크리스트

| 확인 항목 | 확인 명령 | 정상 결과 |
| --- | --- | --- |
| 드라이버 로드 여부 | `lsmod | grep led_poll` | `led_poll_driver` 표시 |
| /dev 노드 존재 | `ls -la /dev/led_device` | crw 권한 파일 존재 |
| 인터럽트 등록 | `cat /proc/interrupts | grep button` | IRQ 번호와 카운트 표시 |
| wait queue 일치 | 소스 코드 검토 | `poll_wait`와 `wake_up` 같은 wq |
| flag 리셋 확인 | `dmesg -w` | "읽기 준비 완료" 1번 후 "0 반환" |
| GPIO 핀 상태 | `cat /sys/kernel/debug/gpio` | 핀 방향·값 표시 |

## 핵심 정리

### 개념 요약

| 개념 | 핵심 내용 |
| --- | --- |
| I/O 다중화 | 하나의 스레드로 여러 FD를 동시에 감시하는 기법 |
| `poll_wait()` | sleep하지 않는다. wait queue를 poll table에 등록만 한다 |
| `.poll` 반환값 | 0이면 커널이 sleep 결정, `POLLIN`이면 즉시 유저 공간 반환 |
| `.poll` 하나로 세 가지 지원 | `select()`, `poll()`, `epoll()` 모두 드라이버의 `.poll`을 호출 |
| 이벤트 플래그 리셋 | `.poll`에서 이벤트를 소비(flag=0)하지 않으면 무한 반환 |
| wait queue 일치 | `poll_wait()`와 `wake_up_interruptible()`은 같은 wq 사용 필수 |

### `.poll` 핸들러 구현 3단계 공식

```c
static unsigned int my_poll(struct file *file, poll_table *wait)
{
    ① poll_wait(file, &wq, wait);        /* wait queue 등록 */
    ② if (event_available) { ... }       /* 이벤트 확인 + 소비 */
    ③ return mask;                        /* 이벤트 마스크 반환 */
}
```

# CHAP 17. 커널 스레드 — kthread

## 커널 스레드(kthread) 개요

### 커널 스레드란 무엇인가

Linux 커널은 자체적으로 백그라운드 작업을 수행하기 위해 **커널 공간에서 실행되는 프로세스**를 생성할 수 있다. 이것이 **kernel thread(kthread)**다.

- `mm_struct`(메모리 디스크립터)를 가지지 않음
- 사용자 가상 주소 공간이 없고, **오직 커널 가상 주소 공간에서만 동작**
- 시스템 부팅 시 `ps aux | grep "\[" | head -20` 으로 확인 가능
- 대괄호 `[]`로 표시되는 프로세스가 kthread
- VSZ, RSS가 0 → 사용자 주소 공간 없음
- `[kthreadd]` (PID 2): 모든 kthread의 부모 프로세스

### kthread가 필요한 상황

| 상황 | 이유 |
| --- | --- |
| ① 장시간 반복 실행이 필요한 백그라운드 작업 | 내부 `while` 루프로 자체 반복 제어 가능 |
| ② 정밀한 생명주기 제어 | `kthread_stop()`, `kthread_should_stop()`으로 직접 제어 |
| ③ 이벤트 대기 + 처리를 하나의 루프에서 수행 | Wait queue와 결합하여 sleep→처리→sleep 패턴 |
| ④ 폴링(polling) 기반 하드웨어 모니터링 | 일정 간격으로 레지스터를 읽는 방식 |

### kthread vs User Space Thread vs Workqueue

| 비교 항목 | kthread | User Thread (pthread) | Workqueue |
| --- | --- | --- | --- |
| 실행 공간 | 커널 공간 | 사용자 공간 | 커널 공간 (worker thread) |
| `mm_struct` | 없음 | 있음 | 없음 |
| sleep 가능 | 가능 | 가능 | 가능 |
| 생명주기 | 드라이버가 직접 제어 | POSIX API로 제어 | 자동 (work 완료 시 종료) |
| 사용자 주소 접근 | 불가 | 가능 | 불가 |
| 반복 실행 | 내부 while 루프 | 내부 루프 | 매번 `schedule_work()` 필요 |
| 전형적 사용처 | 백그라운드 데몬, 폴링 | 일반 애플리케이션 | ISR 후 지연 처리 |
| 생성 API | `kthread_run()` | `pthread_create()` | `schedule_work()` |

### kthread의 실행 흐름 전체 그림

```
[insmod]                              [rmmod]
    ↓                                     ↓
module_init()                         module_exit()
    ├── kthread_run(thread_fn, data, "name")   ├── kthread_stop(thread_st)
    │       ↓                                  │       ↓
    │   kthread 실행 (별도 스케줄링 단위)       │   종료 신호 전송
    │   while (!kthread_should_stop()) {       │
    │       // 작업 수행                       │   thread_fn() return
    │       pr_info("실행 중\n");              │       ↓
    │       ssleep(5);                         │   kthread_stop() 반환
    │   }
    │   return 0;
    └── return 0; (init 성공)
```

> **핵심 포인트**: `kthread_run()` 호출 순간 새로운 스케줄링 단위(프로세스)가 생성되어 `module_init()`과는 **독립적으로 실행**된다.
> 

## kthread API 상세

### 헤더 파일

```c
#include <linux/kthread.h>   // kthread_run, kthread_stop, kthread_should_stop 등
#include <linux/delay.h>     // msleep, ssleep
#include <linux/sched.h>     // struct task_struct, set_current_state 등
```

### `kthread_create()` — 스레드 생성 (시작하지 않음)

```c
struct task_struct *kthread_create(int (*threadfn)(void *data),
                                   void *data,
                                   const char namefmt[], ...);
```

| 인자 | 타입 | 설명 |
| --- | --- | --- |
| `threadfn` | `int (*)(void *)` | 스레드가 실행할 함수. 반환값 `int`는 스레드 종료 코드 |
| `data` | `void *` | `threadfn`에 전달할 private data 포인터 |
| `namefmt` | `const char []` | 스레드 이름 (printf 형식 지원), `ps` 출력에 표시됨 |

| 반환 | 의미 |
| --- | --- |
| 유효한 `task_struct *` | 생성 성공. 아직 시작되지 않은 상태(`TASK_UNINTERRUPTIBLE`) |
| `ERR_PTR(-ENOMEM)` 등 | 생성 실패. `IS_ERR()` 매크로로 검사 |

> `kthread_create()`로 생성한 스레드는 **즉시 실행되지 않는다**. 별도로 `wake_up_process()`를 호출해야 실행이 시작된다.
> 

### `kthread_run()` — 생성 + 즉시 시작

```c
#define kthread_run(threadfn, data, namefmt, ...)
```

`kthread_run()`은 `kthread_create()` + `wake_up_process()`를 하나로 묶은 **매크로**다.

```c
thread_st = kthread_run(thread_function, NULL, "example_thread");
if (IS_ERR(thread_st)) {
    pr_err("커널 스레드 생성 실패\n");
    return PTR_ERR(thread_st);
}
```

**이름 규칙:**

- 스레드 이름은 `comm` 필드에 저장되며 최대 **15자**까지만 표시됨 (NULL 포함 16바이트)
- `ps`, `top`, `/proc/<pid>/comm`에서 확인 가능
- printf 형식 지원: `kthread_run(fn, data, "led_blink_%d", led_id)`

### `kthread_stop()` — 스레드 종료 요청 + 대기

```c
int kthread_stop(struct task_struct *k);
```

**동작 순서:**

1. 대상 kthread에 종료 플래그를 설정한다 (`kthread_should_stop()` → `true`)
2. 대상 kthread가 sleep 중이면 **wake up** 시킨다
3. 대상 kthread의 `threadfn()`이 **반환할 때까지 대기(block)**한다
4. `threadfn()`의 반환값을 리턴한다

> ⚠️ **주의**: `kthread_stop()` 호출 전에 스레드가 이미 종료(return)했다면 undefined behavior 발생.
> 
> 
> → 반드시 kthread 함수 내에서 `kthread_should_stop()`이 `true`가 될 때까지 반환하지 않아야 한다.
> 

### `kthread_should_stop()` — 종료 요청 확인

```c
bool kthread_should_stop(void);
```

- 현재 실행 중인 kthread에 대해 `kthread_stop()`이 호출되었는지 확인
- `true` 반환 시 스레드 함수는 즉시 루프를 탈출하고 `return` 해야 함
- **kthread 내부에서만 호출**해야 함

### `wake_up_process()` — sleep 중인 kthread 깨우기

```c
int wake_up_process(struct task_struct *p);
```

- `TASK_INTERRUPTIBLE` 또는 `TASK_UNINTERRUPTIBLE` 상태의 프로세스를 `TASK_RUNNING` 상태로 전환
- `kthread_create()`로 생성만 하고 아직 시작하지 않은 스레드를 시작할 때도 사용

| 반환값 | 의미 |
| --- | --- |
| 1 | 성공적으로 깨움 (이전 상태가 sleep이었음) |
| 0 | 이미 running 상태여서 깨울 필요 없었음 |

### API 호출 관계도

```
module_init()
    ├── kthread_create(fn, data, name)
    │       └── [커널 내부] kthreadd가 새 프로세스 fork
    │               ↓
    │           task_struct 생성 (TASK_UNINTERRUPTIBLE)
    │
    ├── wake_up_process(ts) ──────────→ fn(data) 실행 시작
    │                                       ↓
    │   (또는 kthread_run = create + wake_up)   while (!kthread_should_stop()) {
    │                                               /* 작업 */
    │                                               ssleep(N);
    │                                           }
    │                                           return 0;
    │                                               ▲
module_exit()                                       │
    └── kthread_stop(ts) ───────────────────────────┘
            │   종료 플래그 설정 + wake up
            └── fn() 반환값 수신
```

### 에러 처리 패턴: `IS_ERR()` / `PTR_ERR()`

Linux 커널은 포인터 반환 함수에서 에러를 표현할 때 **에러 코드를 포인터에 인코딩**하는 방식을 사용한다.

```c
struct task_struct *ts = kthread_run(fn, NULL, "worker");

/*
 * IS_ERR(ts): 포인터 값이 에러 범위(-4095 ~ -1)에 있는지 확인
 * PTR_ERR(ts): 포인터에서 에러 코드(음수 int)를 추출
 * ERR_PTR(err): 에러 코드를 포인터로 인코딩
 */
if (IS_ERR(ts)) {
    int err = PTR_ERR(ts);
    pr_err("kthread 생성 실패: error %d\n", err);
    return err;
}
```

> `IS_ERR(NULL)`은 `false`이므로 NULL 체크와 혼동하지 말 것.
> 

## kthread 내부 루프 패턴

### 패턴 A: 단순 주기 실행 (ssleep/msleep)

가장 기본적인 패턴. 일정 시간 간격으로 작업을 수행한다.

```c
static int periodic_thread(void *data)
{
    while (!kthread_should_stop()) {
        /* 주기적 작업 수행 */
        pr_info("주기적 작업 실행\n");

        /* 일정 시간 대기 */
        ssleep(5);   /* 5초 sleep */
    }

    pr_info("스레드 종료\n");
    return 0;
}
```

| 함수 | 단위 | 최소 해상도 | 사용 시점 |
| --- | --- | --- | --- |
| `ssleep(n)` | 초 | 1초 | 수 초 이상 대기 |
| `msleep(n)` | 밀리초 | ~1ms (HZ 의존) | 수십~수백 ms 대기 |
| `usleep_range(min, max)` | 마이크로초 | ~수 us | 짧은 대기, hrtimer 기반 |

> ⚠️ `ssleep()` / `msleep()`은 `TASK_UNINTERRUPTIBLE` 상태로 sleep한다.
> 
> 
> → `rmmod` 시 최대 sleep 시간만큼 블록될 수 있음.
> 
> 빠른 종료 응답이 필요하면 **패턴 B 또는 C** 사용 권장.
> 

### 패턴 B: 이벤트 대기 루프 (schedule + wait)

더 반응성 높은 패턴. 이벤트가 발생하거나 종료 요청이 올 때까지 효율적으로 대기한다.

```c
static int event_thread(void *data)
{
    while (!kthread_should_stop()) {
        /* 현재 상태를 TASK_INTERRUPTIBLE로 설정 */
        set_current_state(TASK_INTERRUPTIBLE);

        /* 종료 요청 확인 — schedule() 전에 한 번 더 체크 */
        if (kthread_should_stop()) {
            __set_current_state(TASK_RUNNING);
            break;
        }

        /* CPU 양보, 이벤트(wake_up) 또는 시그널이 올 때까지 sleep */
        schedule();

        /* 여기서 실제 작업 수행 */
        pr_info("이벤트 수신: 작업 처리\n");
    }

    pr_info("스레드 종료\n");
    return 0;
}
```

> **race condition 방지**: `set_current_state()` 이후 `schedule()` 전에 다른 CPU에서 `kthread_stop()`이 호출될 수 있으므로, `schedule()` 직전에 종료 조건을 한 번 더 확인한다.
> 

### 패턴 C: Wait Queue 결합 (가장 실용적)

패턴 B를 Wait Queue API로 감싼 형태로, 가장 실용적이고 가독성이 높다.

```c
static DECLARE_WAIT_QUEUE_HEAD(my_wq);
static int event_flag = 0;

/* ISR 또는 다른 곳에서 이벤트 발생 시 호출 */
static void trigger_event(void)
{
    event_flag = 1;
    wake_up_interruptible(&my_wq);
}

/* kthread 함수 */
static int wq_thread(void *data)
{
    while (!kthread_should_stop()) {
        /* 조건이 만족될 때까지 sleep */
        wait_event_interruptible(my_wq,
            event_flag || kthread_should_stop());

        if (kthread_should_stop())
            break;

        /* 이벤트 처리 */
        event_flag = 0;
        pr_info("이벤트 처리 완료\n");
    }

    return 0;
}
```

> 핵심: `wait_event_interruptible()`의 조건에 `kthread_should_stop()`을 OR로 포함하여 `kthread_stop()` 호출 시 즉시 깨어나 종료할 수 있다.
> 

### 세 패턴의 비교 정리

| 항목 | 패턴 A (ssleep) | 패턴 B (schedule) | 패턴 C (wait_event) |
| --- | --- | --- | --- |
| 코드 복잡도 | 낮음 | 높음 | 중간 |
| 종료 반응 속도 | 느림 (sleep 완료까지 대기) | 빠름 (즉시 wake up) | 빠름 (즉시 wake up) |
| 이벤트 대기 | 불가 (주기적 폴링만) | 가능 | 가능 (가장 깔끔) |
| race condition | 없음 | 직접 처리 필요 | API가 처리 |
| 전형적 사용처 | LED 점멸, 주기적 로그 | 저수준 이벤트 대기 | ISR + kthread 조합 |

## 32_kthread 예제 전체 분석

### 예제 파일 구성

```
32_kthread/
├── kthread.c       ← 커널 모듈 소스 (kthread 생성/실행/종료)
├── Makefile        ← 크로스 빌드 Makefile
└── kthread.mod.c   ← 자동 생성 모듈 정보 (빌드 시 생성)
```

### kthread.c 소스 코드 전체 분석

```c
#include <linux/kthread.h>   // kthread API
#include <linux/delay.h>     // msleep, ssleep
#include <linux/module.h>    // 필수 커널 모듈 헤더
#include <linux/kernel.h>    // 커널 관련 기본 함수 및 매크로

struct task_struct *thread_st;                          /* 1 */

int thread_function(void *data) {                       /* 2 */
    while (!kthread_should_stop()) {                    /* 3 */
        pr_info("커널 쓰레드 실행 중\n");
        ssleep(5);  // 5초간 대기                      /* 4 */
    }
    pr_info("커널 쓰레드 종료\n");                     /* 5 */
    return 0;
}

static int __init init_thread(void) {                  /* 6 */
    pr_info("커널 쓰레드를 생성합니다.\n");
    thread_st = kthread_run(thread_function, NULL, "example_thread"); /* 7 */
    if (IS_ERR(thread_st)) {                           /* 8 */
        pr_err("커널 쓰레드 생성 실패\n");
        return PTR_ERR(thread_st);
    }
    pr_info("커널 쓰레드가 성공적으로 생성되었습니다.\n");
    return 0;
}

static void __exit cleanup_thread(void) {              /* 9 */
    if (thread_st) {
        kthread_stop(thread_st);                       /* 10 */
        pr_info("커널 쓰레드를 종료했습니다.\n");
    }
}

module_init(init_thread);
module_exit(cleanup_thread);
MODULE_LICENSE("GPL");
```

**코드 번호별 해설:**

| 번호 | 항목 | 설명 |
| --- | --- | --- |
| ① | `struct task_struct *thread_st` | kthread 핸들. `kthread_stop()` 호출 시 전달하므로 전역 변수로 보관 |
| ② | `int thread_function(void *data)` | 시그니처가 `int (*)(void *)` 형태여야 함 |
| ③ | `while (!kthread_should_stop())` | kthread의 핵심 루프 조건 |
| ④ | `ssleep(5)` | TASK_UNINTERRUPTIBLE 상태로 5초 sleep |
| ⑤ | `pr_info("커널 쓰레드 종료\n")` | 정상 종료 경로를 밟았다는 증거 |
| ⑥ | `static int __init init_thread(void)` | `insmod` 시 호출되는 모듈 초기화 함수 |
| ⑦ | `kthread_run(...)` | kthread 생성 및 즉시 실행 시작 |
| ⑧ | `IS_ERR(thread_st)` | 에러 체크 |
| ⑨ | `static void __exit cleanup_thread(void)` | `rmmod` 시 호출되는 모듈 정리 함수 |
| ⑩ | `kthread_stop(thread_st)` | kthread에 종료를 요청하고 실제로 종료될 때까지 대기 |

### 빌드 → 배포 → 실행 전체 실습

**Step 1: SDK 환경 활성화 및 빌드**

```bash
source /opt/pkg/petalinux/images/linux/sdk/environment-setup-cortexa9t2hf-neon-xilinx-linux-gnueabi
cd ~/zynq_dd/labs/32_kthread/
make
```

**Step 2: NFS 배포 및 모듈 로드**

```bash
make deploy
insmod /home/petalinux/zynq_dd/labs/kthread.ko
dmesg | tail -5
```

**Step 3: kthread 동작 관찰**

```bash
ps aux | grep example_thread
# root  567  0.0  0.0  0  0 ?  S  12:34  0:00 [example_thread]

dmesg -w
# [ 1234.567892] 커널 쓰레드 실행 중
# [ 1239.568123] 커널 쓰레드 실행 중   ← 약 5초 후
```

**Step 4: 모듈 제거 및 종료 확인**

```bash
rmmod kthread
dmesg | tail -5
# [ 1252.123456] 커널 쓰레드 종료    ← kthread_should_stop() == true
# [ 1252.123457] 커널 쓰레드를 종료했습니다.  ← module_exit 메시지
```

> `rmmod` 실행 시 최대 5초(ssleep 대기 시간)까지 지연될 수 있다.
> 

### 확장 실습 1: LED 점멸 kthread

핵심 변경 포인트 — `msleep_interruptible()` 사용:

```c
/* kthread 함수: LED 점멸 루프 */
static int led_blink_fn(void *data)
{
    while (!kthread_should_stop()) {
        led_state = !led_state;
        gpio_set_value(GPIO_LED_PIN, led_state);
        msleep_interruptible(blink_ms);   /* ★ 핵심 변경 */
    }
    gpio_set_value(GPIO_LED_PIN, 0);
    return 0;
}
```

`msleep_interruptible()`은 `TASK_INTERRUPTIBLE` 상태로 sleep하므로:

- `kthread_stop()` 호출 시 **즉시 깨어남** (5초 대기 없음)
- `rmmod` 응답 속도가 크게 개선됨

### 확장 실습 2: kthread_stop() 전에 스레드가 종료된 경우

```c
/* 잘못된 예: kthread_should_stop() 미확인 */
static int bad_thread_fn(void *data)
{
    int count = 0;
    while (count < 3) {    /* ← kthread_should_stop() 미확인! */
        pr_info("실행 중: %d\n", count++);
        ssleep(1);
    }
    return 0;   /* 3초 후 자체 종료 → 위험! */
}
```

```c
/* 올바른 예: 작업 완료 후에도 종료 신호를 대기 */
static int correct_thread_fn(void *data)
{
    int count = 0;
    while (!kthread_should_stop() && count < 3) {
        pr_info("실행 중: %d\n", count++);
        ssleep(1);
    }
    /* 작업 완료 후에도 종료 신호가 올 때까지 대기 */
    while (!kthread_should_stop()) {
        set_current_state(TASK_INTERRUPTIBLE);
        schedule();
    }
    return 0;
}
```

> **핵심 규칙**: kthread 함수는 `kthread_should_stop()`이 `true`가 될 때까지 **절대 반환하면 안 된다**.
> 

## kthread와 Workqueue 선택 기준 정리

### 핵심 차이 요약

| 비교 항목 | kthread | Workqueue |
| --- | --- | --- |
| 생명주기 관리 | 드라이버가 직접 start / stop | 시스템이 worker thread 관리 |
| 반복 실행 | 내부 `while` 루프 | 매번 `schedule_work()` 재호출 |
| 상태 유지 | 함수 내 지역 변수로 자연스럽게 유지 | 별도 구조체에 저장 필요 |
| 생성 비용 | 무거움 (프로세스 생성) | 가벼움 (work 구조체만 초기화) |
| CPU 바인딩 | `kthread_create_on_cpu()` | `queue_work_on()` |
| 우선순위 조절 | `sched_setscheduler()` 가능 | `WQ_HIGHPRI` 플래그 |
| sleep 가능 | 가능 | 가능 |
| 전형적 사용처 | 데몬, 폴링, 상태 머신 | ISR 후처리, 비동기 I/O |
| 종료 | `kthread_stop()` — 명시적 | `flush_work()` / `cancel_work_sync()` |

### 선택 의사결정 플로차트

```
Q1: 작업이 반복적으로 실행되어야 하는가?
    ├── Yes → Q2: 작업 간에 상태를 유지해야 하는가?
    │               ├── Yes → kthread (지역 변수로 상태 유지)
    │               └── No  → Q3: 주기가 고정인가?
    │                             ├── Yes → 커널 타이머(챕터 12) 또는 kthread + msleep
    │                             └── No  → kthread + wait_event
    └── No  → Q4: 인터럽트 후 1회 지연 처리인가?
                    ├── Yes → Workqueue (schedule_work)
                    └── No  → Q5: Softirq 레벨 성능이 필요한가?
                                    ├── Yes → Tasklet (레거시) 또는 Softirq
                                    └── No  → Workqueue 또는 Threaded IRQ
```

### 실무 사례별 권장 선택

| 사례 | 권장 메커니즘 | 이유 |
| --- | --- | --- |
| LED 주기 점멸 (고정 주기) | 커널 타이머 or kthread | 단순 주기 반복 |
| 버튼 ISR 후 LED ON 1초 → OFF | Workqueue | ISR 후 1회 처리, sleep 필요 |
| 센서 데이터 주기적 폴링 | kthread | 반복 + 상태 유지 |
| DMA 전송 완료 후처리 | Workqueue or Threaded IRQ | 비동기 1회 처리 |
| 네트워크 패킷 수신 데몬 | kthread + wait_event | 이벤트 대기 + 반복 처리 |
| FPGA → PS 데이터 스트림 처리 | kthread + ring buffer | 연속 데이터 + 상태 유지 |

## 심화 주제

### kthread에 private data 전달하기

```c
struct my_driver_data {
    struct task_struct *thread;
    volatile uint32_t __iomem *gpio_regs;
    int led_pin;
    int blink_ms;
    atomic_t running;
};

static int blink_thread_fn(void *data)
{
    struct my_driver_data *drv = (struct my_driver_data *)data;

    while (!kthread_should_stop()) {
        gpio_toggle(drv->gpio_regs, drv->led_pin);
        msleep_interruptible(drv->blink_ms);
    }
    return 0;
}
```

> 전역 변수 대신 `data` 포인터를 사용하면 동일한 kthread 함수로 여러 인스턴스를 생성할 수 있다.
> 

### kthread 우선순위 설정

```c
static int realtime_thread(void *data)
{
    struct sched_param param = { .sched_priority = 50 };
    sched_setscheduler(current, SCHED_FIFO, &param);

    while (!kthread_should_stop()) {
        /* 실시간 작업 */
        usleep_range(100, 200);
    }
    return 0;
}
```

> ⚠️ 실시간 우선순위를 과도하게 높이면 시스템의 다른 프로세스가 기아 상태에 빠질 수 있다. 반드시 적절한 sleep을 포함하여 CPU를 양보해야 한다.
> 

### 다중 kthread 관리

```c
#define NUM_LEDS 4

static struct led_info leds[NUM_LEDS] = {
    { .pin = 7,  .delay_ms = 200,  .name = "led_blink_0" },
    { .pin = 8,  .delay_ms = 500,  .name = "led_blink_1" },
    { .pin = 9,  .delay_ms = 1000, .name = "led_blink_2" },
    { .pin = 10, .delay_ms = 2000, .name = "led_blink_3" },
};
```

## 자주 발생하는 실수와 디버깅

### 흔한 실수 목록

| 실수 | 증상 | 해결 방법 |
| --- | --- | --- |
| `kthread_should_stop()` 미확인 | `rmmod` 시 커널 패닉 또는 좀비 프로세스 | `while` 조건에 반드시 포함 |
| 스레드 자체 조기 종료 | `kthread_stop()` → dangling pointer | 작업 완료 후 idle 루프로 전환 |
| `IS_ERR()` 미검사 | `kthread_run()` 실패 시 잘못된 포인터 사용 | 반드시 에러 체크 |
| `ssleep()` 과도한 대기 | `rmmod` 장시간 블록 | `msleep_interruptible()` 사용 |
| 전역 변수 경쟁 | kthread와 ioctl 핸들러가 동시 접근 | mutex 또는 atomic 사용 |
| `module_exit`에서 `kthread_stop()` 누락 | 모듈 제거 후 좀비 kthread 잔류 | 반드시 stop 호출 |
| kthread 내에서 user space 접근 | `copy_to_user()` 실패 | kthread는 user mm이 없으므로 불가 |

### 디버깅 도구

```bash
# kthread 존재 확인
ps aux | grep "\[" | grep my_thread

# kthread PID 확인
cat /proc/$(pgrep -f example_thread)/status

# kthread 스택 트레이스 (커널 디버그 빌드 필요)
cat /proc/$(pgrep -f example_thread)/stack

# 모듈 로드 상태
lsmod | grep kthread

# 실시간 커널 로그
dmesg -w

# kthread CPU 사용률 확인
top -p $(pgrep -f example_thread)
```

### rmmod가 멈추는 경우 대처법

원인: kthread가 `TASK_UNINTERRUPTIBLE` sleep(`ssleep`/`msleep`)에 있는 경우

```bash
# kthread 상태 확인 (D = UNINTERRUPTIBLE, S = INTERRUPTIBLE)
ps aux | grep example_thread
# 상태가 D이면 sleep이 끝날 때까지 기다려야 함
```

**예방책**: kthread 내에서 `ssleep()` 대신 `msleep_interruptible()`을 사용하는 것을 습관화한다.

# CHAP 18. 파일 권한과 접근 제어

## /dev 노드의 권한 구조

### Linux 파일 권한 기초 복습

Linux에서 모든 파일(디바이스 노드 포함)은 세 가지 속성으로 접근 권한을 결정한다.

```
$ ls -la /dev/led_device
crw------- 1 root root 240, 0 Jan 15 10:30 /dev/led_device
```

**권한 비트 (3자리 8진수):**

| 8진수 | 이진수 | 의미 |
| --- | --- | --- |
| 0 | 000 | 접근 불가 |
| 4 | 100 | 읽기(r)만 허용 |
| 6 | 110 | 읽기+쓰기(rw) 허용 |
| 7 | 111 | 읽기+쓰기+실행(rwx) 허용 |

**디바이스 노드에서 흔히 사용하는 권한 조합:**

| 권한 | 의미 | 사용 시나리오 |
| --- | --- | --- |
| 0600 | root만 읽기/쓰기 | 보안이 중요한 디바이스 (기본값) |
| 0660 | root와 특정 그룹만 읽기/쓰기 | 그룹 기반 접근 제어 |
| 0666 | 모든 사용자 읽기/쓰기 | 개발 단계 편의용 (보안 취약) |
| 0440 | root와 그룹이 읽기만 | 센서 데이터 읽기 전용 |

### `device_create()` 기본 권한

커널 모듈이 `device_create()`를 호출하면 `/dev/` 아래에 디바이스 노드가 자동 생성된다.

이 시점에서의 기본 권한은 **root:root 0600**이다.

### 권한 결정의 두 단계

디바이스 파일 접근은 **두 단계의 보안 검사**를 거친다.

```
사용자 프로세스
    ↓
1단계: VFS 파일 권한 검사 (커널 자동 수행)
    → /dev/led_device의 rwx 비트 검사
    → 소유자/그룹/기타 권한 매칭
    → 실패 시 -EACCES 즉시 반환
    ↓ (통과)
2단계: 드라이버 내부 검사 (드라이버 코드에서 수행)
    → capable(), current_uid() 등
    → 드라이버 개발자가 직접 구현
    → 실패 시 -EPERM 반환
    ↓ (통과)
정상 동작 수행
```

> 1단계는 커널 VFS(Virtual File System) 계층이 자동으로 수행하므로 드라이버 코드에서 신경 쓸 필요가 없다.
> 
> 
> 2단계는 드라이버 개발자가 `file_operations` 핸들러 내부에서 직접 구현해야 한다.
> 

## udev 규칙 파일 작성

### udev의 역할

`udev`는 Linux의 디바이스 관리 데몬이다.

- 커널이 디바이스를 감지하면 `uevent`를 발생시킴
- `udev`가 이벤트를 수신하여 `/dev/` 노드의 속성을 조정함

```
커널: 디바이스 감지
    ↓
uevent 발생 → udev 데몬 수신
    ↓
/lib/udev/rules.d/  (시스템 기본 규칙)
/etc/udev/rules.d/  (사용자 커스텀 규칙)  ← 이 위치에 규칙 추가
    ↓
규칙 매칭 → /dev 노드 속성 변경
    (소유자, 그룹, 권한, 심볼릭 링크 등)
```

### 규칙 파일 위치와 이름 규칙

```
/etc/udev/rules.d/
    └── 99-led_device.rules
        │         │
        │         └── 확장자: .rules (필수)
        │    └────── 디바이스 식별 이름 (자유)
        └────────── 우선순위 번호 (00~99, 큰 수기 나중에 적용)
```

### 규칙 파일 문법

**매칭 키 (`==` 연산자):**

| 키 | 의미 | 예시 |
| --- | --- | --- |
| `KERNEL` | 커널이 부여한 디바이스 이름 | `KERNEL=="led_device"` |
| `SUBSYSTEM` | 디바이스가 속한 서브시스템 | `SUBSYSTEM=="led_device"` |
| `ATTR{filename}` | sysfs 속성 값 | `ATTR{vendor}=="0x1234"` |
| `DRIVER` | 바인딩된 드라이버 이름 | `DRIVER=="my_driver"` |

**할당 키 (`=` 연산자):**

| 키 | 의미 | 예시 |
| --- | --- | --- |
| `MODE` | 파일 권한 (8진수) | `MODE="0666"` |
| `OWNER` | 소유자 이름 | `OWNER="root"` |
| `GROUP` | 소유 그룹 이름 | `GROUP="gpio"` |
| `SYMLINK` | 심볼릭 링크 추가 생성 | `SYMLINK+="myled"` |

### 실제 규칙 파일 예시

**기본 규칙:**

```
# /etc/udev/rules.d/99-led_device.rules
KERNEL=="led_device", SUBSYSTEM=="led_device", MODE="0666"
```

**예시 1: 특정 그룹에만 접근 허용**

```
KERNEL=="led_device", MODE="0660", GROUP="gpio"
```

**예시 2: 심볼릭 링크 추가 생성**

```
KERNEL=="led_device", MODE="0660", GROUP="gpio", SYMLINK+="myled"
```

### 규칙 적용 명령

```bash
# 규칙 파일 재로드
sudo udevadm control --reload-rules

# 기존 디바이스에 새 규칙 즉시 적용
sudo udevadm trigger

# 특정 디바이스의 udev 이벤트 모니터링 (디버깅용)
sudo udevadm monitor --environment --udev

# 디바이스 속성 조회 (규칙 작성 시 참고)
udevadm info --query=all --name=/dev/led_device
```

### udev 규칙의 한계

| 요구사항 | udev로 가능? | 해결 방법 |
| --- | --- | --- |
| 특정 그룹에게 읽기/쓰기 허용 | 가능 | `GROUP="gpio"`, `MODE="0660"` |
| root만 쓰기, 일반 사용자는 읽기만 | 가능 | `MODE="0644"` |
| 특정 프로세스의 Capability 확인 | 불가 | 드라이버 내부 `capable()` 사용 |
| 비즈니스 로직에 따른 조건부 접근 | 불가 | 드라이버 내부 커스텀 검사 |
| 런타임 동적 권한 변경 | 불가 | 드라이버 내부 상태 기반 제어 |

## 드라이버 내부 권한 검사

### Linux Capability 시스템

전통적인 Unix에서는 프로세스 권한이 "root(UID 0)"와 "일반 사용자"로만 나뉘었다.

**Linux Capability 시스템은 root 권한을 세분화된 단위로 분리한다.**

**드라이버에서 주로 사용하는 Capability:**

| Capability | 의미 | 드라이버 사용 시나리오 |
| --- | --- | --- |
| `CAP_SYS_ADMIN` | 시스템 관리 권한 (가장 범용) | 하드웨어 제어, 디바이스 설정 변경 |
| `CAP_SYS_RAWIO` | Raw I/O 접근 | 물리 메모리, I/O 포트 직접 접근 |
| `CAP_NET_ADMIN` | 네트워크 관리 | 네트워크 디바이스 설정 변경 |
| `CAP_DAC_OVERRIDE` | 파일 권한 검사 우회 | 파일시스템 관련 디바이스 |
| `CAP_FOWNER` | 파일 소유자 검사 우회 | 다른 사용자 파일 접근 |

### `capable()` API

```c
#include <linux/capability.h>

// 현재 프로세스가 CAP_SYS_ADMIN을 보유하고 있는가?
if (!capable(CAP_SYS_ADMIN)) {
    printk(KERN_ALERT "Permission denied: not an admin\n");
    return -EPERM;   // Operation not permitted
}
```

**`capable()` 동작 규칙:**

| 호출 프로세스 | `capable(CAP_SYS_ADMIN)` 반환값 |
| --- | --- |
| root (UID 0) | `true` (1) — root는 기본적으로 모든 Capability 보유 |
| 일반 사용자 | `false` (0) — 명시적으로 Capability를 부여받지 않는 한 |
| Capability가 설정된 바이너리 | `true` (1) — `setcap`으로 부여된 경우 |

### `EPERM` vs `EACCES` 반환값 차이

| 에러 코드 | 의미 | 사용 시점 |
| --- | --- | --- |
| `-EPERM` | Operation not permitted | 프로세스의 Capability/권한이 부족할 때 |
| `-EACCES` | Permission denied | 파일 접근 모드(read/write)가 맞지 않을 때 |

### `file->f_mode` 접근 모드 확인

```c
#define FMODE_READ   0x1   // 읽기 모드로 열림
#define FMODE_WRITE  0x2   // 쓰기 모드로 열림

// 사용 예시: write() 핸들러에서 읽기 전용 오픈 거부
static ssize_t led_write(...) {
    if (!(file->f_mode & FMODE_WRITE)) {
        printk(KERN_ALERT "File not opened for writing\n");
        return -EACCES;
    }
    // ... 정상 처리 ...
}
```

| `open()` 플래그 | `file->f_mode` |
| --- | --- |
| `O_RDONLY` | `FMODE_READ` |
| `O_WRONLY` | `FMODE_WRITE` |
| `O_RDWR` | `FMODE_READ | FMODE_WRITE` |

### `current_uid()`: 프로세스 UID 확인

```c
#include <linux/cred.h>

kuid_t uid = current_uid();
if (!uid_eq(uid, GLOBAL_ROOT_UID)) {
    printk(KERN_ALERT "Only root (UID 0) can access this device\n");
    return -EPERM;
}
```

> **권장**: UID 직접 비교보다 `capable()`을 사용하는 것이 더 세밀하고 유연하다.
> 

## 33_perm 예제 전체 분석

### 예제 파일 구조

```
33_perm/
├── ledperm.c      ← 커널 모듈 (GPIO + capable() 권한 검사)
├── led.c          ← 유저 앱 (LED 제어 메뉴)
├── Makefile       ← 크로스 빌드 설정
└── ledperm.mod.c  ← 빌드 시 자동 생성 (무시)
```

### 27_ledon과의 차이 비교

`33_perm`이 `27_ledon`과 다른 점은 정확히 **두 가지**다.

```c
// 추가된 헤더
+#include <linux/capability.h>

// led_read() 함수 시작 부분
+    if (!capable(CAP_SYS_ADMIN)) {
+        printk(KERN_ALERT "Permission denied: not an admin\n");
+        return -EPERM;
+    }

// led_write() 함수 시작 부분
+    if (!capable(CAP_SYS_ADMIN)) {
+        printk(KERN_ALERT "Permission denied: not an admin\n");
+        return -EPERM;
+    }
```

> 헤더 하나와 검사 코드 두 블록만 추가하면 **Capability 기반 접근 제어**가 완성된다.
> 

## 실습: 권한 검사 동작 확인

### 시나리오별 동작 비교

| 시나리오 | 파일 권한 | `open()` 결과 | `read/write` 결과 | 차단 위치 |
| --- | --- | --- | --- | --- |
| A: 0600 + 일반 사용자 | `rw-------` | 실패 (`-EACCES`) | 호출 안 됨 | VFS (1단계) |
| B: 0666 + 일반 사용자 | `rw-rw-rw-` | 성공 | 실패 (`-EPERM`) | 드라이버 (2단계) |
| C: 0666 + root | `rw-rw-rw-` | 성공 | 성공 | 없음 (통과) |

### setcap으로 일반 사용자에게 Capability 부여

```bash
# root에서 실행: led 바이너리에 CAP_SYS_ADMIN Capability 부여
setcap cap_sys_admin+ep /home/petalinux/zynq_dd/labs/led

# 확인
getcap /home/petalinux/zynq_dd/labs/led
# /home/petalinux/zynq_dd/labs/led cap_sys_admin=ep

# 이제 일반 사용자도 led 앱을 통해 LED 제어 가능
su - petalinux
/home/petalinux/zynq_dd/labs/led
# 명령 1 입력 → LED가 켜졌습니다.  ← 성공!

# Capability 제거
setcap -r /home/petalinux/zynq_dd/labs/led
```

**setcap 플래그 의미:**

| 플래그 | 의미 |
| --- | --- |
| `e` (effective) | Capability가 실행 시 즉시 활성화됨 |
| `p` (permitted) | 프로세스가 이 Capability를 사용할 수 있음 |
| `i` (inheritable) | `exec()` 후에도 Capability 유지 |
| `ep` | effective + permitted (가장 일반적인 조합) |

## Sysfs 속성 파일 권한

### `DEVICE_ATTR` 매크로 계열

sysfs 속성 파일을 생성하면 `/sys/` 아래에 읽기/쓰기 가능한 파일이 만들어진다.

```c
// 읽기 전용 속성 (0444: 모든 사용자 읽기)
DEVICE_ATTR_RO(led_status);    // → /sys/.../led_status (0444)

// 쓰기 전용 속성 (0200: root만 쓰기)
DEVICE_ATTR_WO(led_control);   // → /sys/.../led_control (0200)

// 읽기+쓰기 속성 (0644: 모든 사용자 읽기, root만 쓰기)
DEVICE_ATTR_RW(led_brightness); // → /sys/.../led_brightness (0644)
```

**매크로별 기본 권한:**

| 매크로 | 콜백 | 기본 권한 | 의미 |
| --- | --- | --- | --- |
| `DEVICE_ATTR_RO(name)` | `name_show` | 0444 | 모든 사용자 읽기 |
| `DEVICE_ATTR_WO(name)` | `name_store` | 0200 | root만 쓰기 |
| `DEVICE_ATTR_RW(name)` | `name_show` + `name_store` | 0644 | 모든 사용자 읽기, root만 쓰기 |

### 커스텀 권한 설정

```c
// 커스텀 권한: root만 읽기/쓰기 (0600)
static DEVICE_ATTR(led_config, 0600, led_config_show, led_config_store);

// 모든 사용자 읽기, root만 쓰기 (0644) — DEVICE_ATTR_RW와 동일
static DEVICE_ATTR(led_value, 0644, led_value_show, led_value_store);
```

### 속성 파일 등록 및 해제

```c
// module_init()에서 등록
ret = device_create_file(led_dev, &dev_attr_led_status);

// module_exit()에서 해제
device_remove_file(led_dev, &dev_attr_led_status);
```

## 다층 접근 제어 설계 패턴

### 패턴 1: 읽기는 자유, 쓰기는 root만

```c
static ssize_t my_read(...) {
    // 읽기는 권한 검사 없이 허용 (파일 권한으로 1차 필터링됨)
    return do_read(buf, len, offset);
}

static ssize_t my_write(...) {
    // 쓰기는 CAP_SYS_ADMIN 필수
    if (!capable(CAP_SYS_ADMIN))
        return -EPERM;
    return do_write(buf, len, offset);
}
```

udev 규칙: `MODE="0644"` (소유자 rw, 나머지 r)

### 패턴 2: 그룹 기반 + Capability 조합

```c
static ssize_t my_write(...) {
    if (!capable(CAP_SYS_RAWIO)) {
        printk(KERN_WARNING "CAP_SYS_RAWIO required\n");
        return -EPERM;
    }
    return do_write(buf, len, offset);
}
```

udev 규칙: `MODE="0660"`, `GROUP="hw-access"`

**이 조합에서의 접근 결과:**

| 사용자 | 그룹 | `open()` | `write()` | 최종 결과 |
| --- | --- | --- | --- | --- |
| root | root | 성공 | 성공 | LED 제어 가능 |
| user1 | hw-access | 성공 | 실패 (`-EPERM`) | 파일 열기만 가능 |
| user2 | users | 실패 (`-EACCES`) | 호출 안 됨 | 완전 차단 |
| user1 + setcap | hw-access | 성공 | 성공 | LED 제어 가능 |

### 패턴 3: open() 수준의 단일 접근 제한

```c
static atomic_t open_count = ATOMIC_INIT(0);

static int my_open(struct inode *inode, struct file *file) {
    // Capability 검사
    if (!capable(CAP_SYS_ADMIN))
        return -EPERM;

    // 단일 접근 보장
    if (atomic_cmpxchg(&open_count, 0, 1) != 0)
        return -EBUSY;
    return 0;
}

static int my_release(struct inode *inode, struct file *file) {
    atomic_set(&open_count, 0);
    return 0;
}
```

> 이 패턴은 `open()` 단계에서 모든 검사를 수행하므로 `read()` / `write()` 내부에서는 별도의 권한 검사가 불필요하다.
> 

## 정리: 세 가지 다층 보안 패턴 비교

| 패턴 | udev 설정 | 보안 강도 | 주요 사용 사례 |
| --- | --- | --- | --- |
| 패턴 1: 읽기 자유, 쓰기 root 제한 | `MODE=0644` | 약 (Low) | 센서 데이터 읽기, 정보 노출 제약 필요, 설정 변경 시 |
| 패턴 2: 그룹 + Capability 조합 | `MODE=0660`, `GROUP=hw-access` | 중 (Medium) | 산업용 제어 장비, 특정 관리자 그룹 허가 환경 |
| 패턴 3: open() 단일 접근 제한 | `MODE=0600` | 강 (High) | 독점 하드웨어 제어, 의료기기/고신뢰 통신 장비 |

# CHAP 19. DMA 엔진 — 비동기 메모리 전송

> **대응 예제:** `34_dma`
> 

## 학습 목표

- DMA(Direct Memory Access)의 개념과 CPU Copy 대비 이점을 설명할 수 있다.
- `dma_alloc_coherent()`로 DMA 전용 메모리를 할당하고 해제할 수 있다.
- Linux DMA Engine 서브시스템의 채널 요청·전송·완료 흐름을 구현할 수 있다.
- `completion` 구조체로 DMA 전송 완료를 동기화할 수 있다.
- 인터럽트 + Workqueue + DMA를 연동하는 실전 드라이버를 작성할 수 있다.

---

## 선행 지식 체크

| 챕터 | 내용 |
| --- | --- |
| 챕터 13 | GPIO 인터럽트와 Blocking I/O (`request_irq`, `wait_queue`) |
| 챕터 14 | Workqueue를 이용한 Bottom Half 처리 (`schedule_work`, `INIT_WORK`) |
| 챕터 9 | `ioremap()`을 이용한 물리 주소 ↔ 가상 주소 매핑 개념 |
| 챕터 6 | 커널 모듈 생명주기 (`module_init`, `module_exit`) |

---

## 19.1 DMA 개념과 필요성

### 19.1.1 DMA란 무엇인가

DMA(Direct Memory Access)는 **CPU의 개입 없이** 메모리와 메모리 사이, 또는 주변장치와 메모리 사이에 데이터를 직접 전송하는 하드웨어 메커니즘이다. CPU가 일일이 바이트를 읽고 쓰는 대신, **DMA 컨트롤러(DMAC)**라는 전용 하드웨어가 데이터 이동을 대신 수행한다.

### CPU Copy (Programmed I/O) 방식

```
CPU가 직접 데이터를 한 바이트(또는 한 워드)씩 읽고 쓴다:
  1. CPU: src[0] 읽기  → 레지스터에 저장
  2. CPU: dst[0] 쓰기  → 레지스터에서 메모리로
  3. CPU: src[1] 읽기  → ...
  4. CPU: dst[1] 쓰기  → ...
  ... (N번 반복)

→ CPU는 전송이 끝날 때까지 다른 작업 불가
→ 대용량 전송 시 CPU 점유율 100%
```

### DMA Copy 방식

```
CPU는 DMAC에게 명령만 내린다:
  1. CPU: "src=0x1000, dst=0x2000, size=4096" → DMAC에 전달
  2. CPU: 다른 작업 (프로세스 스케줄링, 인터럽트 처리 등)
  3. DMAC: 하드웨어적으로 데이터를 고속 전송
  4. DMAC: 전송 완료 → 인터럽트 발생 → CPU에 알림

→ CPU는 전송 중 자유롭게 다른 작업 수행 가능
→ 시스템 전체 처리량(throughput) 향상
```

### CPU Copy vs. DMA Copy 핵심 비교

| 비교 항목 | CPU Copy (Programmed I/O) | DMA Copy (하드웨어 기반) |
| --- | --- | --- |
| 적합성 | 소용량 및 단순 제어 | 대용량 및 고속 데이터 전송 |
| CPU 점유율 | 100% (작업 중단) | ~0% (자유로운 병행 작업) |
| 처리량(Throughput) | 낮음 (< 25 MB/s) | 높음 (최대 1,200 MB/s 이상) |

### DMA가 필요한 실제 사례

- **고속 데이터 스트리밍:** 비디오 캡처 장치에서 프레임 데이터를 메모리로 전송할 때, 초당 수십 MB 이상의 데이터를 CPU가 직접 복사하면 다른 모든 작업이 멈춘다. DMA 컨트롤러가 프레임 버퍼를 자동으로 채우면 CPU는 렌더링이나 인코딩 작업에 집중할 수 있다.
- **네트워크 패킷 처리:** 기가비트 이더넷은 초당 100MB 이상의 패킷을 수신한다. 네트워크 카드의 DMA 엔진이 수신 버퍼에 패킷을 직접 기록하고, CPU는 프로토콜 처리만 담당한다.
- **FPGA ↔ PS 데이터 교환 (Zynq-7000 고유):** Zynq-7000의 PL에서 생성된 대량 데이터를 PS 메모리로 전송할 때, AXI DMA IP를 사용한다. 이 교재의 예제(`34_dma`)가 바로 이 시나리오의 단순화된 버전이다.
- **디스크 I/O:** 파일을 읽고 쓸 때 블록 디바이스 드라이버는 DMA를 사용하여 디스크 컨트롤러와 메모리 사이에 데이터를 전송한다.

### Zynq-7000의 DMA 구조

Zynq-7000 SoC는 여러 DMA 경로를 제공한다.

```
┌─────────────────────────────────────┐
│           Zynq-7000 SoC             │
│  ┌──── PS (Processing System) ────┐ │
│  │  ARM Cortex-A9 (Dual Core)     │ │
│  │          ↔  PS DMA Controller  │ │
│  │              (8 채널)           │ │
│  │  DDR Memory Controller         │ │
│  │          ↓                     │ │
│  │       DDR3 메모리               │ │
│  │    (Zybo: 1GB DDR3)            │ │
│  └────────────────────────────────┘ │
│         AXI Interconnect            │
│  ┌──── PL (Programmable Logic) ───┐ │
│  │  AXI DMA / AXI CDMA IP        │ │
│  │    (Vivado에서 인스턴스화)       │ │
│  │  Custom IP (데이터 소스/싱크)   │ │
│  └────────────────────────────────┘ │
└─────────────────────────────────────┘
```

- **PS DMA Controller (DMAC):** Zynq-7000 PS에 내장된 DMA 컨트롤러, 8개의 독립 채널 제공. ARM PrimeCell DMA Controller(PL330) 기반. 이 교재의 `34_dma` 예제는 이 PS DMAC의 MEMCPY 채널을 사용한다.
- **AXI DMA / AXI CDMA (PL):** Vivado에서 Block Design에 추가하는 DMA IP 코어. PL 내부의 커스텀 IP와 PS DDR 메모리 사이의 고속 전송에 사용한다.

### Linux DMA Engine 서브시스템 개요

Linux 커널은 다양한 DMA 컨트롤러를 통합 관리하기 위해 **DMA Engine** 프레임워크를 제공한다.

```
┌─────────────────────────────────────────────┐
│  Client Driver (우리가 작성하는 코드)          │ ← button_dma_driver.c
│    dma_request_channel()                    │
│    device_prep_dma_memcpy()                 │
│    dmaengine_submit()                       │
│    dma_async_issue_pending()                │
├─────────────────────────────────────────────┤
│              DMA Engine API                 │
├─────────────────────────────────────────────┤
│  DMA Engine Framework                       │ ← drivers/dma/dmaengine.c
│    채널 관리, 전송 큐, 콜백 디스패치           │
├─────────────────────────────────────────────┤
│           DMA Provider Interface            │
├─────────────────────────────────────────────┤
│  DMA Provider Driver (HW별 드라이버)          │ ← pl330.c (Zynq PS DMA)
│    실제 하드웨어 레지스터 제어                 │   xilinx_dma.c (AXI DMA)
├─────────────────────────────────────────────┤
│         DMA Controller Hardware             │
└─────────────────────────────────────────────┘
```

### DMA 전송 유형

| 타입 | 설명 |
| --- | --- |
| `DMA_MEMCPY` | 메모리 → 메모리 복사 (이 챕터의 예제) |
| `DMA_MEMSET` | 메모리를 특정 값으로 채우기 |
| `DMA_SG` | Scatter-Gather: 비연속 메모리 블록 전송 |
| `DMA_CYCLIC` | 순환 버퍼 전송 (오디오 스트림 등) |
| `DMA_SLAVE` | 주변장치 ↔ 메모리 전송 (SPI, I2C, UART DMA) |
| `DMA_INTERLEAVE` | 인터리브 패턴 전송 (이미지 처리) |

## DMA 메모리 할당

### 왜 일반 `kmalloc()` 메모리를 DMA에 직접 쓰면 안 되는가

DMA 컨트롤러는 **물리 주소(또는 버스 주소)**로 메모리에 접근한다. 반면 커널 코드는 **가상 주소**를 사용한다. 여기서 두 가지 문제가 발생한다.

**문제 1 — 주소 변환:** `kmalloc()`이 반환하는 것은 커널 가상 주소다. DMA 컨트롤러는 이 주소를 이해하지 못한다. 물리 주소(또는 DMA 주소)를 알아내야 하며, 이 주소가 DMA 컨트롤러의 주소 공간에서 유효해야 한다.

**문제 2 — 캐시 일관성(Cache Coherency):** ARM Cortex-A9은 L1/L2 캐시를 사용한다.

- CPU 쓰기 → DMA 읽기 시: CPU가 캐시에만 쓰고 DDR에 반영되지 않은 상태에서 DMA 전송이 시작되면, DMA 컨트롤러는 DDR의 **오래된 데이터**를 읽게 된다.
- DMA 쓰기 → CPU 읽기 시: DMA가 DDR에 기록한 데이터를 CPU가 읽을 때, 캐시에 이전 값이 남아있으면 **갱신되지 않은 데이터**를 읽게 된다.

이러한 문제를 자동으로 해결하는 것이 `dma_alloc_coherent()` 함수다.

### `dma_alloc_coherent()` API

```c
#include <linux/dma-mapping.h>

void *dma_alloc_coherent(
    struct device *dev,      // DMA를 수행하는 디바이스의 device 구조체
    size_t size,             // 할당할 바이트 수
    dma_addr_t *dma_handle,  // [출력] DMA 주소(물리/버스 주소) 저장
    gfp_t gfp               // 메모리 할당 플래그 (GFP_KERNEL, GFP_ATOMIC 등)
);
```

**반환값:**

- 성공: 커널 가상 주소 (CPU가 사용하는 포인터)
- 실패: NULL

**핵심 포인트 — 두 개의 주소를 동시에 얻는다:**

```c
dma_addr_t dma_src;   // DMA 컨트롤러가 사용할 물리/버스 주소
void *src_buf;         // CPU가 사용할 커널 가상 주소

src_buf = dma_alloc_coherent(dev, BUF_SIZE, &dma_src, GFP_KERNEL);

// src_buf → CPU가 데이터를 읽고 쓸 때 사용
// dma_src → DMA 컨트롤러에게 전달할 주소
```

### `gfp` 플래그 선택

| 플래그 | 사용 상황 |
| --- | --- |
| `GFP_KERNEL` | 프로세스 컨텍스트에서 호출 시 (sleep 가능). `module_init()`, `probe()`, `ioctl` 핸들러 등에서 사용 |
| `GFP_ATOMIC` | 인터럽트 컨텍스트에서 호출 시 (sleep 불가). ISR, tasklet, 타이머 콜백에서 사용. 즉시 사용 가능한 메모리만 할당 → 실패 확률 높음 |

> `34_dma` 예제에서는 `GFP_ATOMIC`을 사용하고 있다. 실습에서는 `module_init()`에서만 호출하므로 `GFP_KERNEL`로 변경해도 무방하다.
> 

### `dma_free_coherent()` — 해제

할당한 DMA 메모리는 반드시 `module_exit()`에서 해제해야 한다.

```c
void dma_free_coherent(
    struct device *dev,      // 할당 시 사용한 것과 동일한 device
    size_t size,             // 할당 시 지정한 것과 동일한 크기
    void *cpu_addr,          // dma_alloc_coherent()가 반환한 가상 주소
    dma_addr_t dma_handle    // dma_alloc_coherent()가 저장한 DMA 주소
);
```

> **중요:** `size`, `cpu_addr`, `dma_handle` 세 값 모두 할당 시와 **정확히 동일**해야 한다. 값이 다르면 커널 패닉이 발생할 수 있다.
> 

### `dma_addr_t` 타입의 의미

`dma_addr_t`는 DMA 컨트롤러가 사용하는 주소 타입이다. 플랫폼에 따라 32비트 또는 64비트이며, Zynq-7000은 32비트 시스템이므로 `dma_addr_t`는 32비트 정수다.

| 타입 | 의미 |
| --- | --- |
| `phys_addr_t` | CPU가 보는 물리 주소 |
| `dma_addr_t` | DMA 컨트롤러가 보는 버스 주소 |
| `void *` | CPU가 사용하는 커널 가상 주소 |

> 대부분의 시스템(Zynq-7000 포함)에서 `phys_addr_t`와 `dma_addr_t`는 같은 값이지만, IOMMU가 있는 시스템에서는 다를 수 있다. 따라서 항상 `dma_addr_t`를 사용하는 것이 이식성 면에서 올바르다.
> 

### DMA 메모리 할당 실전 코드

```c
#define BUF_SIZE 256   // 버퍼 크기

static struct dma_chan *dma_chan;
static dma_addr_t dma_src, dma_dst;   // DMA 주소 (컨트롤러용)
static void *src_buf, *dst_buf;        // 가상 주소 (CPU용)

static int dma_init(void)
{
    int i;

    /* ... (채널 요청은 19.3절에서 설명) ... */

    // 소스 버퍼 할당: CPU용 가상주소(src_buf)와 DMA용 주소(dma_src)를 동시에 획득
    src_buf = dma_alloc_coherent(
        dma_chan->device->dev,
        BUF_SIZE,
        &dma_src,
        GFP_ATOMIC
    );

    // 목적지 버퍼 할당
    dst_buf = dma_alloc_coherent(
        dma_chan->device->dev,
        BUF_SIZE,
        &dma_dst,
        GFP_ATOMIC
    );

    // NULL 체크 — 할당 실패 시 반드시 처리
    if (!src_buf || !dst_buf) {
        printk(KERN_ERR "Failed to allocate DMA buffers\n");
        return -ENOMEM;
    }

    // 소스 버퍼에 테스트 데이터 기록 (CPU가 가상주소로 접근)
    for (i = 0; i < BUF_SIZE; i++)
        ((char *)src_buf)[i] = (char)(i % 256);
    // → src_buf: 00 01 02 03 ... FE FF
    // → dst_buf: 00 00 00 00 ... (초기 상태)

    return 0;
}
```

## DMA 채널 및 전송 API

### DMA 채널 요청

```c
#include <linux/dmaengine.h>

// 1. 채널 능력(capability) 마스크 선언 및 설정
dma_cap_mask_t mask;
dma_cap_zero(mask);                  // 마스크 초기화 (모든 비트 0)
dma_cap_set(DMA_MEMCPY, mask);       // MEMCPY 능력 요구

// 2. 채널 요청
struct dma_chan *chan = dma_request_channel(mask, NULL, NULL);
if (!chan) {
    printk(KERN_ERR "Failed to request DMA channel\n");
    return -ENODEV;
}
```

**`dma_request_channel()` 시그니처:**

```c
struct dma_chan *dma_request_channel(
    const dma_cap_mask_t mask,   // 요구하는 채널 능력
    dma_filter_fn fn,            // 필터 함수 (특정 채널 선택, NULL=아무거나)
    void *fn_param               // 필터 함수 파라미터
);
```

**능력 마스크 예시:**

```c
dma_cap_set(DMA_MEMCPY, mask);   // 메모리 간 복사 가능한 채널
dma_cap_set(DMA_SLAVE, mask);    // 주변장치 ↔ 메모리 전송 가능한 채널
dma_cap_set(DMA_CYCLIC, mask);   // 순환 버퍼 전송 가능한 채널
```

### DMA 전송 준비 — `device_prep_dma_memcpy()`

채널을 확보했으면 전송 descriptor(전송 명세서)를 준비한다.

```c
struct dma_async_tx_descriptor *tx;
struct dma_device *dma_dev = dma_chan->device;

tx = dma_dev->device_prep_dma_memcpy(
    dma_chan,                          // DMA 채널
    dma_dst,                           // 목적지 DMA 주소 (dma_addr_t)
    dma_src,                           // 소스 DMA 주소 (dma_addr_t)
    BUF_SIZE,                          // 전송 바이트 수
    DMA_CTRL_ACK | DMA_PREP_INTERRUPT  // 플래그
);

if (!tx) {
    printk(KERN_ERR "DMA 전송 설정 실패\n");
    return;
}
```

> **주의:** 주소 인자에 `dma_addr_t` 타입을 전달해야 한다. 커널 가상 주소(`src_buf`, `dst_buf`)가 아니라 `dma_alloc_coherent()`에서 얻은 DMA 주소(`dma_src`, `dma_dst`)를 전달한다.
> 

**플래그 설명:**

| 플래그 | 설명 |
| --- | --- |
| `DMA_CTRL_ACK` | descriptor 재사용 허용 (프레임워크에 통보) |
| `DMA_PREP_INTERRUPT` | 전송 완료 시 인터럽트 발생 → 콜백 호출 트리거 |

### 콜백 설정

전송 완료 시 호출될 콜백 함수를 descriptor에 등록한다.

```c
// 콜백 함수 정의
static void dma_callback(void *completion)
{
    complete(completion);   // completion 시그널 발생
}

// descriptor에 콜백 등록
tx->callback = dma_callback;
tx->callback_param = &dma_complete;   // 콜백에 전달할 인자
```

> 콜백 함수는 **인터럽트 컨텍스트**(또는 softirq 컨텍스트)에서 실행되므로, sleep이 필요한 작업은 수행할 수 없다. `complete()`은 sleep하지 않으므로 안전하다.
> 

### 전송 제출과 실행 — `dmaengine_submit()` + `dma_async_issue_pending()`

```c
// 전송 큐에 제출 (아직 실행되지 않음)
dmaengine_submit(tx);

// 전송 실행 시작 (큐에 쌓인 모든 pending 전송을 실행)
dma_async_issue_pending(dma_chan);
```

> 이 두 단계가 분리되어 있는 이유는, 여러 전송을 먼저 큐에 쌓아 놓고(`submit`) 한꺼번에 실행(`issue_pending`)할 수 있도록 하기 위해서다.
> 

### 전체 전송 흐름 요약

```
├── dma_cap_zero(mask)
├── dma_cap_set(DMA_MEMCPY, mask)
├── dma_request_channel(mask, NULL, NULL)        ← 채널 확보
│
├── dma_alloc_coherent(dev, size, &dma_src, ...) ← src 버퍼 할당
├── dma_alloc_coherent(dev, size, &dma_dst, ...) ← dst 버퍼 할당
│
├── src_buf[i] = test_data                        ← CPU가 소스 데이터 기록
│
├── device_prep_dma_memcpy(chan, dst, src, size)  ← descriptor 준비
├── tx->callback = dma_callback                   ← 완료 콜백 설정
├── dmaengine_submit(tx)                          ← 큐에 제출
├── dma_async_issue_pending(chan)                 ← 실행 시작
│
├── wait_for_completion_timeout(&dma_complete, ...) ← CPU sleep (대기)
│         │
│         │    ... [DMA Controller가 하드웨어적으로 데이터 전송 중] ...
│         │
│         └── DMA 완료 인터럽트 → dma_callback() → complete()
│
├── (깨어남) dst_buf 내용 확인                     ← 전송 완료 확인
│
├── dma_free_coherent(dev, size, src_buf, dma_src) ← 정리
├── dma_free_coherent(dev, size, dst_buf, dma_dst)
└── dma_release_channel(chan)                     ← 채널 반환
```

### 채널 반환 — `dma_release_channel()`

모듈 종료 시 확보한 DMA 채널을 반드시 반환한다.

```c
dma_release_channel(dma_chan);
```

> 채널을 반환하지 않으면 해당 채널이 시스템에서 영구적으로 점유된 상태로 남아, 다른 드라이버가 DMA 채널을 사용할 수 없게 된다.
> 

## DMA 완료 동기화

### `completion` 구조체

DMA 전송은 비동기적으로 수행된다. Linux 커널은 이런 비동기 이벤트 대기를 위해 `completion` 구조체를 제공한다. Wait Queue와 유사하지만, **"1회성 이벤트 대기"**에 최적화되어 있다.

```c
#include <linux/completion.h>

// 방법 1: 정적 초기화
static DECLARE_COMPLETION(dma_complete);

// 방법 2: 동적 초기화
static struct completion dma_complete;
init_completion(&dma_complete);   // 반드시 사용 전에 호출
```

### `complete()` — 완료 신호 보내기

DMA 콜백 함수에서 전송 완료를 알린다.

```c
static void dma_callback(void *completion)
{
    complete(completion);
    // complete()는 sleep하지 않으므로 인터럽트 컨텍스트에서 안전
}
```

> `complete()`는 대기 중인 스레드 **하나**를 깨운다. 여러 스레드가 대기 중이면 `complete_all()`을 사용한다.
> 

### `wait_for_completion_timeout()` — 완료 대기

```c
unsigned long remaining;

remaining = wait_for_completion_timeout(
    &dma_complete,
    msecs_to_jiffies(1000)   // 타임아웃: 1000ms (1초)
);

if (remaining == 0) {
    printk(KERN_ERR "DMA 전송 타임아웃!\n");
    // 에러 처리: DMA 전송 취소, 리소스 정리 등
    return;
}
// remaining > 0: 타임아웃 전에 완료됨
// 이제 dst_buf의 데이터를 안전하게 사용할 수 있다
```

**반환값:**

| 값 | 의미 |
| --- | --- |
| `0` | 타임아웃 발생 (지정 시간 내에 `complete()`가 호출되지 않음) |
| `> 0` | 남은 jiffies 수 (타임아웃 전에 완료됨) |

> **타임아웃을 설정해야 하는 이유:** DMA 하드웨어가 오류를 일으키거나, 콜백이 호출되지 않는 상황이 발생할 수 있다. 타임아웃 없이 `wait_for_completion()`을 사용하면 시스템이 **영원히 멈출** 수 있다.
> 

### `completion` vs Wait Queue 비교

|  | `completion` | `wait_queue` |
| --- | --- | --- |
| 용도 | 1회성 이벤트 대기 | 반복적 조건 대기 |
| 신호 방식 | `complete()` / `complete_all()` | `wake_up_interruptible()` |
| 대기 방식 | `wait_for_completion*()` | `wait_event_interruptible()` |
| 재초기화 | `reinit_completion()` 필요 | 자동 (조건 기반) |
| 일반적 사용처 | DMA 완료, 펌웨어 로드 완료 | 인터럽트 이벤트, 데이터 수신 |

### `reinit_completion()` — 재사용 시 주의사항

`completion`은 1회성이다. 한 번 `complete()`가 호출되면 내부 카운터가 증가하여, 이후 `wait_for_completion()`이 즉시 반환된다. **DMA 전송을 반복 수행하려면 매번 `reinit_completion()`으로 초기화해야 한다.**

```c
// 올바른 반복 전송 패턴
while (need_transfer) {
    reinit_completion(&dma_complete);   // ← 반드시 재초기화

    tx = device_prep_dma_memcpy(...);
    tx->callback = dma_callback;
    tx->callback_param = &dma_complete;
    dmaengine_submit(tx);
    dma_async_issue_pending(dma_chan);

    wait_for_completion_timeout(&dma_complete, timeout);
}
```

## Workqueue와 DMA 연동

### ISR에서 직접 DMA를 시작할 수 없는 이유

ISR(Interrupt Service Routine)은 인터럽트 컨텍스트에서 실행되며 엄격한 제약이 있다.

```
ISR에서 금지되는 작업:
  ✗ sleep 가능한 함수 호출 (msleep, mutex_lock 등)
  ✗ 사용자 공간 메모리 접근 (copy_to_user 등)
  ✗ 긴 처리 (다른 인터럽트 지연 발생)
```

`wait_for_completion_timeout()`으로 완료를 대기하는 것은 sleep이므로 ISR에서 불가능하다. 따라서 **전체 DMA 처리를 Workqueue로 위임하는 것이 표준 패턴**이다.

### 버튼 → ISR → Workqueue → DMA 전체 흐름

```
[하드웨어]    [ISR]              [Workqueue]          [DMA HW]
BTN0 누름 → button_isr() → dma_work_handler() → DMA 전송
               │                    │
               ├── schedule_work()  ├── start_dma_transfer()
               └── wake_up()        ├── prep_memcpy()
                                    ├── submit()
                                    ├── issue_pending() ──→ HW 전송 시작
                                    │                       ... 전송 중 ...
                                    └── wait_for_completion() ←──────────
                                         dump_buffer()        (인터럽트)
```

### ISR 구현 분석

```c
static irqreturn_t button_isr(int irq, void *dev_id)
{
    button_pressed = 1;
    printk(KERN_INFO "Button pressed, queuing DMA transfer\n");

    // DMA 작업을 전역 workqueue에 등록
    schedule_work(&dma_work);

    // read()에서 대기 중인 프로세스를 깨움
    wake_up_interruptible(&wait_queue);

    return IRQ_HANDLED;
}
```

**ISR은 세 가지 작업만 수행한다:**

1. `button_pressed = 1`: 상태 플래그 설정
2. `schedule_work(&dma_work)`: DMA 처리를 Workqueue에 위임
3. `wake_up_interruptible()`: `read()` 차단 해제

ISR은 수 마이크로초 내에 반환된다. DMA 전송과 결과 출력은 모두 Workqueue에서 처리된다.

### Workqueue 핸들러 구현

```c
static void dma_work_handler(struct work_struct *work)
{
    start_dma_transfer();   // DMA 전송 실행 (내부에서 완료까지 대기)
}
```

Workqueue 핸들러는 프로세스 컨텍스트에서 실행되므로 `wait_for_completion_timeout()` (sleep 가능)을 호출할 수 있다.

### `schedule_work()` vs `queue_work()` 선택

```c
// 전역 workqueue 사용 (간단한 경우)
schedule_work(&dma_work);

// 커스텀 workqueue 사용 (격리가 필요한 경우)
static struct workqueue_struct *dma_wq;
dma_wq = create_singlethread_workqueue("dma_wq");
queue_work(dma_wq, &dma_work);
```

> 실무에서 DMA 전송이 빈번하거나 타이밍이 중요한 경우에는 `create_singlethread_workqueue()`로 전용 workqueue를 생성하는 것이 좋다.
> 

## `34_dma` 예제 전체 분석 및 실습

### 예제 파일 구조

```
34_dma/
├── button_dma_driver.c   ← 커널 모듈 소스 (DMA + GPIO + 인터럽트)
├── button.c              ← 유저 애플리케이션 (Blocking I/O로 버튼 이벤트 대기)
└── Makefile              ← 크로스 빌드 Makefile
```

### 전체 소스 코드 분석

### 헤더 파일과 전역 변수

```c
#include <linux/module.h>
#include <linux/kernel.h>
#include <linux/fs.h>
#include <linux/gpio.h>
#include <linux/interrupt.h>
#include <linux/uaccess.h>
#include <linux/wait.h>
#include <linux/delay.h>
#include <linux/dma-mapping.h>   // dma_alloc_coherent, dma_free_coherent
#include <linux/dmaengine.h>     // DMA Engine API
#include <linux/platform_device.h>
#include <linux/of_dma.h>
#include <linux/completion.h>    // completion 구조체
#include <linux/workqueue.h>     // workqueue API

#define LED_PIN        912   // GPIO7 핀에 LED 연결
#define BUTTON_PIN     956   // GPIO51(BTN5) 핀에 버튼 연결
#define BUF_SIZE       256   // DMA 버퍼 크기

// DMA 관련 전역 변수
static struct dma_chan *dma_chan;
static dma_addr_t dma_src, dma_dst;        // DMA 주소 (HW용)
static void *src_buf, *dst_buf;             // 가상 주소 (CPU용)
static struct completion dma_complete;      // DMA 완료 동기화
```

### `dma_init()` 함수

```c
static int dma_init(void)
{
    dma_cap_mask_t mask;
    int i;

    // ① completion 초기화
    init_completion(&dma_complete);

    // ② DMA 채널 능력 마스크 설정
    dma_cap_zero(mask);
    dma_cap_set(DMA_MEMCPY, mask);

    // ③ DMA 채널 요청
    dma_chan = dma_request_channel(mask, NULL, NULL);
    if (!dma_chan) {
        printk(KERN_ERR "Failed to request DMA channel\n");
        return -ENODEV;
    }

    // ④ DMA 버퍼 할당 (소스 + 목적지)
    src_buf = dma_alloc_coherent(dma_chan->device->dev,
                                  BUF_SIZE, &dma_src, GFP_ATOMIC);
    dst_buf = dma_alloc_coherent(dma_chan->device->dev,
                                  BUF_SIZE, &dma_dst, GFP_ATOMIC);

    if (!src_buf || !dst_buf) {
        printk(KERN_ERR "Failed to allocate DMA buffers\n");
        return -ENOMEM;
    }

    // ⑤ 소스 버퍼에 테스트 패턴 기록
    for (i = 0; i < BUF_SIZE; i++)
        ((char *)src_buf)[i] = (char)(i % 256);

    return 0;
}
```

| 단계 | 함수 | 역할 |
| --- | --- | --- |
| ① | `init_completion()` | DMA 완료 대기용 동기화 객체 초기화 |
| ② | `dma_cap_set()` | MEMCPY 능력을 가진 채널을 요청하겠다고 선언 |
| ③ | `dma_request_channel()` | 시스템에서 사용 가능한 DMA 채널 확보 |
| ④ | `dma_alloc_coherent() ×2` | 캐시 일관성 보장되는 DMA 버퍼 2개 할당 |
| ⑤ | 루프 초기화 | 소스 버퍼에 `0x00~0xFF` 패턴 기록 |

### `start_dma_transfer()` 함수

```c
static void start_dma_transfer(void)
{
    struct dma_async_tx_descriptor *tx;
    struct dma_device *dma_dev = dma_chan->device;

    // [단계 1] 전송 descriptor 준비
    tx = dma_dev->device_prep_dma_memcpy(
        dma_chan,
        dma_dst,                           // 목적지 DMA 주소
        dma_src,                           // 소스 DMA 주소
        BUF_SIZE,                          // 256 바이트
        DMA_CTRL_ACK | DMA_PREP_INTERRUPT  // 완료 시 인터럽트 발생
    );
    if (!tx) {
        printk(KERN_ERR "DMA 전송 설정 실패\n");
        return;
    }

    // [단계 2] 완료 콜백 설정
    tx->callback = dma_callback;
    tx->callback_param = &dma_complete;

    // [단계 3] 전송 큐에 제출
    dmaengine_submit(tx);

    // [단계 4] 전송 실행 시작
    dma_async_issue_pending(dma_chan);

    // 전송 완료 대기 (타임아웃 1초)
    if (!wait_for_completion_timeout(&dma_complete,
                                      msecs_to_jiffies(1000))) {
        printk(KERN_ERR "DMA 전송이 시간 내에 완료되지 않았습니다\n");
        return;
    }

    // 전송 결과 출력 — src와 dst의 내용이 동일해야 한다
    dump_buffer("src_buf", src_buf, BUF_SIZE);
    dump_buffer("dst_buf", dst_buf, BUF_SIZE);
}
```

### `dma_button_exit()` — 모듈 종료 함수

```c
static void __exit dma_button_exit(void)
{
    // 리소스 해제 순서: 초기화의 역순
    free_irq(button_irq, NULL);
    gpio_free(LED_PIN);
    gpio_free(BUTTON_PIN);
    unregister_chrdev(dev_major, "dma_button_device");

    // DMA 리소스 해제
    dma_free_coherent(dma_chan->device->dev, BUF_SIZE, src_buf, dma_src);
    dma_free_coherent(dma_chan->device->dev, BUF_SIZE, dst_buf, dma_dst);
    dma_release_channel(dma_chan);

    printk(KERN_INFO "DMA Button Driver unloaded\n");
}
```

**해제 순서:**

```
초기화 순서: DMA → GPIO → IRQ → Workqueue → chrdev
해제 순서:   IRQ → GPIO → chrdev → DMA_buf → DMA_chan (역순)
```

### 빌드·배포·실행 실습

### Step 1 — SDK 환경 활성화 및 빌드

```bash
# Ubuntu VM에서 실행
source /path/to/sdk/environment-setup-cortexa9t2hf-neon-xilinx-linux-gnueabi

cd 34_dma/
make
```

### Step 2 — NFS를 통한 배포

```bash
cp button_dma_driver.ko /nfsroot/home/petalinux/zynq_dd/labs/
cp button_app           /nfsroot/home/petalinux/zynq_dd/labs/
```

### Step 3 — Zybo 보드에서 모듈 로드

```bash
# Zybo 콘솔에서 실행
cd /home/petalinux/zynq_dd/labs/

insmod button_dma_driver.ko
dmesg | tail -5
# DMA Button Driver initialized with major number 245

# 디바이스 노드 수동 생성 (udev가 없는 경우)
mknod /dev/blocking_io_device c 245 0
```

### Step 4 — 유저 앱 실행 및 버튼 테스트

```bash
# 터미널 1: 유저 앱 실행
./button_app
# Waiting for button press...

# Zybo 보드의 BTN0 버튼을 누른다
# → LED가 1초간 켜졌다 꺼진다
```

### Step 5 — DMA 전송 결과 확인

```bash
dmesg | tail -40
```

```
Button pressed, queuing DMA transfer
src_buf - 버퍼 덤프 (길이: 256 바이트):
00000000: 00 01 02 03 04 05 06 07 08 09 0a 0b 0c 0d 0e 0f
00000010: 10 11 12 13 14 15 16 17 18 19 1a 1b 1c 1d 1e 1f
...
000000f0: f0 f1 f2 f3 f4 f5 f6 f7 f8 f9 fa fb fc fd fe ff

dst_buf - 버퍼 덤프 (길이: 256 바이트):
00000000: 00 01 02 03 04 05 06 07 08 09 0a 0b 0c 0d 0e 0f
...
000000f0: f0 f1 f2 f3 f4 f5 f6 f7 f8 f9 fa fb fc fd fe ff
```

두 덤프의 내용이 완전히 일치하면 DMA 전송이 정상적으로 완료된 것이다.

### Step 6 — 모듈 제거

```bash
rmmod button_dma_driver
dmesg | tail -3
# DMA Button Driver unloaded
```

### 코드 개선 포인트 분석

### 개선 1: `reinit_completion()` 누락

현재 코드는 `dma_init()`에서 `init_completion()`을 1회 호출한다. 버튼을 두 번째 이후로 누르면 `completion`이 이미 완료 상태이므로 `wait_for_completion_timeout()`이 즉시 반환되어 DMA 전송 결과를 확인하기 전에 `dump_buffer()`가 실행될 수 있다.

```c
// 수정: start_dma_transfer() 시작 부분에 추가
static void start_dma_transfer(void)
{
    reinit_completion(&dma_complete);   // ← 추가: 매 전송마다 재초기화
    // ...
}
```

### 개선 2: `button_pressed` 원자성 보장

`button_pressed` 변수가 ISR과 `read()` 핸들러에서 동시에 접근될 수 있다. `atomic_t`를 사용해야 안전하다.

```c
// 수정 전
static int button_pressed = 0;

// 수정 후
static atomic_t button_pressed = ATOMIC_INIT(0);

// ISR에서
atomic_set(&button_pressed, 1);

// read()에서
wait_event_interruptible(wait_queue, atomic_read(&button_pressed) == 1);
atomic_set(&button_pressed, 0);
```

### 개선 3: 에러 처리 보강 — `dma_init()` 실패 시 부분 해제

```c
// 수정: goto 에러 처리 패턴 적용
static int dma_init(void)
{
    src_buf = dma_alloc_coherent(dev, BUF_SIZE, &dma_src, GFP_KERNEL);
    if (!src_buf) {
        ret = -ENOMEM;
        goto err_release_chan;
    }

    dst_buf = dma_alloc_coherent(dev, BUF_SIZE, &dma_dst, GFP_KERNEL);
    if (!dst_buf) {
        ret = -ENOMEM;
        goto err_free_src;
    }

    return 0;

err_free_src:
    dma_free_coherent(dev, BUF_SIZE, src_buf, dma_src);
err_release_chan:
    dma_release_channel(dma_chan);
    return ret;
}
```

### 개선 4: `register_chrdev()` → `alloc_chrdev_region()` + `cdev`

현재 코드는 deprecated된 `register_chrdev()`를 사용한다. `alloc_chrdev_region()` + `cdev_init()` + `class_create()` 패턴으로 교체하면 `/dev` 노드가 자동 생성된다.

### 확장 실습 과제

### 과제 1: DMA 전송 시간 측정

`ktime_get()` API를 사용하여 DMA 전송 소요 시간을 측정하고, 동일 크기의 `memcpy()` (CPU Copy)와 비교하라.

```c
#include <linux/ktime.h>

ktime_t start, end;
s64 elapsed_ns;

// DMA 전송 시간 측정
start = ktime_get();
/* ... DMA 전송 + 완료 대기 ... */
end = ktime_get();
elapsed_ns = ktime_to_ns(ktime_sub(end, start));
printk(KERN_INFO "DMA transfer:  %lld ns\n", elapsed_ns);

// CPU memcpy 시간 측정
start = ktime_get();
memcpy(dst_buf, src_buf, BUF_SIZE);
end = ktime_get();
elapsed_ns = ktime_to_ns(ktime_sub(end, start));
printk(KERN_INFO "CPU memcpy:    %lld ns\n", elapsed_ns);
```

> 256바이트의 소규모 전송에서는 DMA 오버헤드(채널 설정, descriptor 준비) 때문에 CPU `memcpy()`가 더 빠를 수 있다. 버퍼 크기를 4KB, 64KB, 1MB로 늘려가며 교차 지점을 찾아보라.
> 

### 과제 2: DMA 전송 크기를 `module_param`으로 제어

```c
static unsigned int buf_size = 256;
module_param(buf_size, uint, S_IRUGO);
MODULE_PARM_DESC(buf_size, "DMA buffer size in bytes (default: 256)");
```

`insmod button_dma_driver.ko buf_size=4096`으로 로드하여 다양한 크기의 DMA 전송을 테스트하라.

### 과제 3: `reinit_completion()` 적용 후 반복 버튼 누름 테스트

개선 1을 적용한 후, 버튼을 연속으로 10회 누르면서 `dmesg`에 10회분의 정상적인 덤프가 출력되는지 확인하라.

## DMA API 요약 레퍼런스

### 메모리 할당/해제

```c
// Coherent DMA 버퍼 할당
void *dma_alloc_coherent(struct device *dev, size_t size,
                          dma_addr_t *dma_handle, gfp_t gfp);

// Coherent DMA 버퍼 해제
void dma_free_coherent(struct device *dev, size_t size,
                        void *cpu_addr, dma_addr_t dma_handle);
```

### 채널 관리

```c
// 채널 능력 마스크 조작
dma_cap_zero(mask);                 // 마스크 초기화
dma_cap_set(DMA_MEMCPY, mask);      // MEMCPY 능력 추가

// 채널 요청/해제
struct dma_chan *dma_request_channel(dma_cap_mask_t mask,
                                      dma_filter_fn fn, void *fn_param);
void dma_release_channel(struct dma_chan *chan);
```

### 전송 실행

```c
// MEMCPY 전송 descriptor 준비
struct dma_async_tx_descriptor *
device_prep_dma_memcpy(struct dma_chan *chan,
                        dma_addr_t dest, dma_addr_t src,
                        size_t len, unsigned long flags);

// 콜백 설정
tx->callback = my_callback;
tx->callback_param = my_param;

// 전송 큐 제출 + 실행
dmaengine_submit(tx);
dma_async_issue_pending(chan);
```

### 완료 동기화

```c
// 초기화
init_completion(&comp);
reinit_completion(&comp);           // 반복 사용 시

// 완료 신호
complete(&comp);                    // 대기 중인 스레드 1개 깨움
complete_all(&comp);                // 대기 중인 모든 스레드 깨움

// 완료 대기
wait_for_completion(&comp);                                    // 무한대기 (위험)
unsigned long wait_for_completion_timeout(&comp, timeout_jiffies);  // 타임아웃 대기 (권장)
```