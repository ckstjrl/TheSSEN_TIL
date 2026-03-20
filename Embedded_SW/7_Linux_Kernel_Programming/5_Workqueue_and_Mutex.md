# 임베디드 리눅스 커널 프로그래밍 day05

날짜: 2026년 3월 20일

# CHAP 14. 인터럽트 Bottom Half — Workqueue

## ISR을 분리해야 하는 이유

### 28 예제의 구조적 문제

`28_buttonint` 드라이버에서 `read()` 핸들러는 다음과 같은 구조였다:

```c
static ssize_t dev_read(struct file *filep, char *buffer, size_t len, loff_t *offset)
{
    wait_event_interruptible(wait_queue, button_pressed == 1);
    button_pressed = 0;

    // ★ 문제: read() 컨텍스트에서 LED 제어 + 1초 sleep
    gpio_set_value(LED_PIN, 1);
    msleep(1000);
    gpio_set_value(LED_PIN, 0);

    return 0;
}
```

이 코드는 동작하지만 구조적으로 두 가지 문제가 있다.

### 문제 1 — `read()` 시스템 콜의 의미 왜곡

`read()`는 "데이터를 읽어오는" 동작이다. 그런데 이 코드에서 `read()`를 호출하면 LED가 1초간 켜졌다 꺼진다. 사용자 입장에서 `read()`를 호출했을 뿐인데 하드웨어 부수효과(side effect)가 발생하는 것은 UNIX의 "Everything is a file" 철학에 맞지 않는다.

### 문제 2 — 느린 하드웨어 동작이 호출자를 블록

`msleep(1000)`은 호출한 프로세스를 1초간 재울 뿐 아니라, 해당 시간 동안 다른 `read()` 요청도 처리할 수 없다. LED를 10초간 점멸해야 하는 상황이라면, 유저 애플리케이션은 10초간 아무것도 못 하고 멈춰 있게 된다.

> 이 문제의 근본 원인은 **"인터럽트 처리 결과에 따른 후속 동작"을 어디에서 수행할 것인가**를 제대로 설계하지 않은 데 있다. 해답은 **Bottom Half 메커니즘**이다.
> 

### ISR이 빨라야 하는 진짜 이유

ISR(Interrupt Service Routine)이 실행되는 동안 해당 인터럽트 라인은 비활성화(masked)되거나 같은 CPU에서 다른 인터럽트를 처리하지 못한다.

ISR 컨텍스트에서 **불가능한 작업**:

- 같은 IRQ 라인의 추가 인터럽트 수신
- `sleep` / `schedule`
- `mutex_lock`
- 프로세스 컨텍스트 전환

> **"ISR은 최대한 빠르게 끝내야 한다"** — ISR이 1ms 걸린다면 1ms 동안 시스템의 반응성이 저하되고, 100ms 걸린다면 동일 라인의 인터럽트를 100ms 동안 놓칠 수 있다.
> 

### Top Half / Bottom Half 분리 원칙

Linux 커널은 인터럽트 처리를 두 단계로 나눈다.

### Top Half (상반부) — ISR 자체

| 특성 | 내용 |
| --- | --- |
| 실행 컨텍스트 | 인터럽트 컨텍스트 |
| 하드웨어 상호작용 | 상태 레지스터 읽기, 인터럽트 acknowledge |
| 제약 | sleep 불가, mutex 불가, 긴 루프 금지 |
| 목표 | 가능한 한 빨리 끝내기 (수 μs 이내) |
| 반드시 수행 | 인터럽트 원인 확인, 플래그 설정, Bottom Half 스케줄링 |

### Bottom Half (하반부) — 지연 처리

| 특성 | 내용 |
| --- | --- |
| 실행 컨텍스트 | 프로세스 컨텍스트(Workqueue) 또는 소프트 인터럽트 컨텍스트(Tasklet/Softirq) |
| 역할 | ISR이 확인한 이벤트에 대한 실제 처리 수행 |
| Workqueue 특성 | sleep 가능, mutex 가능, 긴 처리 가능 |
| 목표 | ISR이 미루어 둔 나머지 작업을 안전하게 완료 |

> **핵심 원칙**: "ISR에서는 `queue_work()`만 호출하고, 실제 작업은 Workqueue 핸들러에서 수행한다."
> 

## Linux 커널의 Bottom Half 메커니즘 4종 비교

### Softirq

- 커널에 정적으로 등록되는 가장 낮은 수준의 Bottom Half 메커니즘
- 네트워크 스택(`NET_TX_SOFTIRQ`, `NET_RX_SOFTIRQ`)과 블록 I/O 처리 등 극도로 높은 성능이 필요한 커널 서브시스템에서만 사용
- 컴파일 시점에 등록 (동적 추가 불가)
- 여러 CPU에서 동시 실행 가능 → 동기화 부담 큼
- **드라이버 개발에서 직접 사용하는 경우는 거의 없다**

### Tasklet

- Softirq 위에 구축된 메커니즘으로, 드라이버에서 사용하기 더 쉬움
- 동적으로 생성할 수 있고, 같은 Tasklet은 하나의 CPU에서만 실행되도록 보장됨
- **sleep이 불가능** (소프트 인터럽트 컨텍스트에서 실행)
- `msleep()`, `mutex_lock()` 등을 호출하면 커널 패닉 발생
- 커널 커뮤니티에서는 Tasklet의 **폐기(deprecation)**를 논의 중 → 새로운 드라이버에서는 Workqueue 또는 Threaded IRQ 권장

```c
// Tasklet 선언 및 사용 패턴 (참고용)
DECLARE_TASKLET(my_tasklet, my_tasklet_handler);

// ISR에서 스케줄링
irqreturn_t my_isr(int irq, void *dev_id) {
    tasklet_schedule(&my_tasklet);
    return IRQ_HANDLED;
}
```

### Workqueue (중요)

- **커널 스레드** 컨텍스트에서 작업을 실행하는 메커니즘
- 프로세스 컨텍스트이므로 sleep 가능, mutex 가능, 긴 처리 허용

```c
// Workqueue의 핵심 특성
static void my_work_func(struct work_struct *work) {
    // ✓ msleep() 호출 가능
    // ✓ mutex_lock() 호출 가능
    // ✓ 파일 I/O 가능
    // ✓ DMA 전송 시작 가능
    // ✓ kmalloc(GFP_KERNEL) 가능
}
```

### Threaded IRQ

- `request_threaded_irq()` API로 등록, Top Half와 Bottom Half를 **하나의 API 호출**로 설정
- 커널이 자동으로 IRQ 스레드를 생성해 주므로 별도의 Workqueue 생성 불필요
- 최신 드라이버에서 가장 깔끔한 방식

```c
// Threaded IRQ 등록 패턴 (참고용)
request_threaded_irq(irq,
    my_hardirq_handler,    // Top Half (NULL이면 기본 핸들러 사용)
    my_thread_handler,     // Bottom Half (커널 스레드에서 실행)
    IRQF_TRIGGER_FALLING,
    "my_irq",
    dev);
```

### 4종 비교 정리

| 메커니즘 | 실행 컨텍스트 | sleep 가능 | 동적 생성 | 주요 사용처 |
| --- | --- | --- | --- | --- |
| Softirq | 소프트 인터럽트 | ✗ | ✗(정적) | 네트워크, 타이머 커널 서브시스템 |
| Tasklet | 소프트 인터럽트 | ✗ | ✓ | 단순 지연 처리 (폐기 논의 중) |
| **Workqueue** | **커널 스레드** | **✓** | **✓** | **드라이버 Bottom Half 범용** |
| Threaded IRQ | 커널 스레드 | ✓ | ✓ | 최신 드라이버 ISR 스레드화 |

### 드라이버 개발자를 위한 선택 기준

```
Bottom Half에서 msleep(), mutex_lock(), kmalloc(GFP_KERNEL) 등을 호출해야 하는가?
  ├── 예 → Workqueue 또는 Threaded IRQ
  │         ├── ISR과 Bottom Half를 하나의 API로 묶고 싶다 → Threaded IRQ
  │         └── 학습 목적 / 명시적 제어 원함 → Workqueue
  └── 아니오 → Tasklet (단, 새 코드에서는 비권장)
```

## Workqueue API 상세

### 핵심 자료 구조

### ① `struct workqueue_struct` — Workqueue 자체

커널 스레드 풀(thread pool)을 관리하는 구조체. 작업이 등록되면 이 풀에 속한 스레드 중 하나가 작업을 실행한다.

```c
#include <linux/workqueue.h>

static struct workqueue_struct *button_wq;
```

### ② `struct work_struct` — 개별 작업 단위

실행할 함수(콜백)와 연결된 작업 단위. Workqueue에 이 구조체를 등록하면, 해당 함수가 커널 스레드에서 호출된다.

```c
static struct work_struct button_work;
```

### Workqueue 생성 API

### 전역 Workqueue 사용 (별도 생성 불필요)

커널은 시스템 부팅 시 전역 Workqueue(`system_wq`)를 미리 생성해 둔다. 대부분의 드라이버는 이 전역 Workqueue만으로 충분하다.

```c
// 전역 Workqueue에 작업 등록 — 별도의 workqueue_struct 생성 불필요
schedule_work(&button_work);
```

### 커스텀 Workqueue 생성

특정 드라이버 전용의 독립적인 Workqueue가 필요한 경우에 사용. 다른 드라이버의 작업과 간섭 없이 실행을 보장한다.

```c
// 단일 스레드 커스텀 Workqueue 생성
button_wq = create_singlethread_workqueue("button_wq");
if (!button_wq) {
    pr_err("Failed to create workqueue\n");
    return -ENOMEM;
}
```

> `create_singlethread_workqueue()`는 해당 Workqueue 전용 커널 스레드를 1개 생성한다. 작업이 순차적으로 실행되므로 동기화가 단순해진다.
> 

### `alloc_workqueue()` — 더 세밀한 제어가 필요할 때

```c
// 최대 동시 실행 작업 수를 지정하는 고급 API
struct workqueue_struct *wq;
wq = alloc_workqueue("my_wq",
                     WQ_UNBOUND,  // CPU 고정 없이 실행
                     2);          // max_active: 동시 실행 최대 2개
```

| 플래그 | 의미 |
| --- | --- |
| `WQ_UNBOUND` | 특정 CPU에 고정하지 않고 실행 |
| `WQ_HIGHPRI` | 높은 우선순위 커널 스레드에서 실행 |
| `WQ_FREEZABLE` | 시스템 suspend 시 동결 가능 |
| `WQ_MEM_RECLAIM` | 메모리 회수 경로에서도 안전 |

### 작업 초기화 및 등록

### 작업 초기화 — `INIT_WORK()`

```c
// 작업 구조체와 콜백 함수를 연결
INIT_WORK(&button_work, button_work_func);
```

콜백 함수의 시그니처는 반드시 다음 형태여야 한다:

```c
static void button_work_func(struct work_struct *work)
{
    // 여기서 실제 처리 수행
}
```

인사로 전달되는 `work` 포인터를 통해 `container_of()` 매크로로 상위 구조체에 접근할 수 있다. 이 패턴은 고급 드라이버에서 중요하다:

```c
struct my_device {
    struct work_struct work;
    int led_pin;
    int duration_ms;
};

static void my_work_func(struct work_struct *work)
{
    // work 포인터로부터 상위 구조체(my_device) 포인터 획득
    struct my_device *dev = container_of(work, struct my_device, work);
    gpio_set_value(dev->led_pin, 1);
    msleep(dev->duration_ms);
    gpio_set_value(dev->led_pin, 0);
}
```

### 작업 등록 — `queue_work()` / `schedule_work()`

```c
// 커스텀 Workqueue에 등록
queue_work(button_wq, &button_work);

// 전역 Workqueue에 등록 (위와 동일하나 시스템 전역 큐 사용)
schedule_work(&button_work);
```

> **중요**: 이미 Workqueue에 등록되어 대기 중이거나 실행 중인 `work_struct`를 다시 `queue_work()` 하면, 두 번째 호출은 **무시**된다. 이는 설계상 의도된 동작으로, 같은 작업이 중복 실행되는 것을 방지한다. 빠른 연속 인터럽트에 대해 Workqueue가 자동으로 "병합(coalescing)" 역할을 한다.
> 

### Workqueue 정리 API

```c
// 대기 중인 모든 작업이 완료될 때까지 블록
flush_workqueue(button_wq);

// 특정 작업 하나의 완료를 대기
flush_work(&button_work);

// 작업 취소 (실행 중이면 완료 대기)
cancel_work_sync(&button_work);

// Workqueue 삭제 (커널 스레드 종료 및 자원 반환)
destroy_workqueue(button_wq);
```

### 모듈 종료 시 정리 순서

`module_exit`에서 Workqueue를 안전하게 정리하는 순서는 매우 중요하다. 잘못된 순서로 정리하면 **Use-After-Free** 또는 **NULL 포인터 역참조**가 발생할 수 있다.

```c
static void __exit my_exit(void)
{
    // ① 먼저 인터럽트 소스를 제거 — 더 이상 새 작업이 등록되지 않도록
    free_irq(button_irq, NULL);

    // ② GPIO 해제 — 하드웨어 자원 반환
    gpio_free(LED_PIN);
    gpio_free(BUTTON_PIN);

    // ③ 캐릭터 디바이스 등록 해제
    unregister_chrdev(dev_major, "blocking_io_device");

    // ④ Workqueue 정리 — 남은 작업 완료 후 삭제
    flush_workqueue(button_wq);    // 실행 중인 작업 완료 대기
    destroy_workqueue(button_wq);  // Workqueue 삭제
}
```

### 정리 순서의 핵심 원칙

1. **인터럽트 소스를 먼저 제거**한다 → 새로운 `queue_work()` 호출이 발생하지 않도록 차단
2. **`flush_workqueue()`로 진행 중인 작업을 완료**한다 → 작업이 GPIO를 사용하고 있을 수 있으므로
3. **`destroy_workqueue()`로 커널 스레드를 제거**한다

> ⚠️ **경고**: `destroy_workqueue()`를 먼저 호출하고 그 후에 `free_irq()`를 호출하면, ISR이 이미 삭제된 Workqueue에 `queue_work()`를 시도하여 **커널 패닉**이 발생할 수 있다.
> 

## Workqueue에서 허용되는 작업과 금지되는 작업

### 허용되는 작업

Workqueue 핸들러는 프로세스 컨텍스트(커널 스레드)에서 실행되므로, ISR이나 Tasklet에서 할 수 없는 많은 작업이 가능하다.

| 작업 | API 예시 | 설명 |
| --- | --- | --- |
| Sleep | `msleep(1000)`, `ssleep(1)` | 지정 시간만큼 프로세스를 재운다 |
| Mutex | `mutex_lock()`, `mutex_unlock()` | 공유 자원 보호 |
| 메모리 할당 | `kmalloc(size, GFP_KERNEL)` | sleep 허용 할당 |
| 파일 I/O | `vfs_read()`, `vfs_write()` | 파일 시스템 접근 |
| DMA 전송 | `dmaengine_submit()` | DMA 엔진 사용 |
| 네트워크 | `sock_sendmsg()` | 네트워크 전송 |
| 조건 대기 | `wait_event_interruptible()` | 조건 대기 |

### 주의해야 할 사항

### ① 무한 루프 금지

Workqueue 핸들러가 무한 루프에 빠지면 해당 커널 스레드가 영구적으로 점유된다. `create_singlethread_workqueue()`로 만든 경우, 해당 Workqueue의 다른 모든 작업이 영원히 실행되지 못한다.

```c
// ✗ 잘못된 패턴
static void bad_work_func(struct work_struct *work) {
    while (1) {          // 절대 금지!
        do_something();
        msleep(100);
    }
}

// ✓ 올바른 패턴 — 작업 완료 후 필요하면 재등록
static void good_work_func(struct work_struct *work) {
    do_something();
    // 반복이 필요하면 kthread를 사용하거나,
    // 다음 인터럽트가 발생할 때 다시 queue_work()
}
```

### ② `GFP_KERNEL` 플래그 사용

Workqueue 핸들러에서 메모리를 할당할 때는 `GFP_KERNEL`을 사용할 수 있다. ISR에서는 `GFP_ATOMIC`만 허용되지만, Workqueue에서는 메모리 부족 시 sleep하면서 기다릴 수 있는 `GFP_KERNEL`이 더 안정적이다.

```c
// ISR에서의 메모리 할당 (atomic만 가능)
void *buf_isr = kmalloc(size, GFP_ATOMIC);  // 실패 가능성 높음

// Workqueue에서의 메모리 할당 (kernel 플래그 사용 가능)
void *buf_wq = kmalloc(size, GFP_KERNEL);   // sleep하며 대기 → 성공 확률 높음
```

### ③ 실행 시점은 보장되지 않는다

`queue_work()`를 호출한 즉시 Workqueue 핸들러가 실행되는 것은 아니다. 커널 스케줄러가 해당 커널 스레드를 실행할 시점을 결정한다. 보통 수 ms 이내에 실행되지만, 시스템 부하에 따라 지연될 수 있다.

### 14.4.3 컨텍스트별 허용 작업 비교표

| 작업 | ISR (Top Half) | Tasklet | Workqueue | kthread |
| --- | --- | --- | --- | --- |
| `msleep()` / `ssleep()` | ✗ | ✗ | ✓ | ✓ |
| `mutex_lock()` | ✗ | ✗ | ✓ | ✓ |
| `kmalloc(GFP_KERNEL)` | ✗ | ✗ | ✓ | ✓ |
| `kmalloc(GFP_ATOMIC)` | ✓ | ✓ | ✓ | ✓ |
| `spin_lock()` | ✓ | ✓ | ✓ | ✓ |
| `schedule()` | ✗ | ✗ | ✓ | ✓ |
| `queue_work()` | ✓ | ✓ | ✓ | ✓ |
| `printk()` | ✓ | ✓ | ✓ | ✓ |

## `29_buttonbh` 예제 전체 분석

### 예제 파일 구조

```
29_buttonbh/
├── bh_io_driver.c      ← 커널 모듈 (드라이버 소스)
├── blocking_app.c      ← 유저 애플리케이션
├── Makefile            ← 크로스 빌드 Makefile
└── bh_io_driver.mod.c  ← 자동 생성 (빌드 시)
```

### `28_buttonint` vs `29_buttonbh` 핵심 변경 비교

| 항목 | `28_buttonint` (챕터 13) | `29_buttonbh` (이 챕터) |
| --- | --- | --- |
| LED 제어 위치 | `dev_read()` 내부 | `button_work_func()` (Workqueue) |
| `msleep()` 호출 컨텍스트 | `read()` 시스템 콜 컨텍스트 | 커널 스레드 컨텍스트 |
| `read()` 블록 시간 | 1초 이상 (LED 점멸 완료까지) | 즉시 반환 (버튼 누름 감지 즉시) |
| LED 동작과 앱 실행 | 동기적 (순차) | **비동기적 (병렬)** |

### Bottom Half 핸들러 — Workqueue 콜백 함수

```c
// ★ Bottom Half에서 실행될 함수 (프로세스 컨텍스트)
static void button_work_func(struct work_struct *work)
{
    gpio_set_value(LED_PIN, 1);       // LED ON
    msleep(1000);                      // 1초 대기 — sleep 가능!
    gpio_set_value(LED_PIN, 0);       // LED OFF
    printk(KERN_INFO "Button workqueue executed: LED toggled\n");
}
```

### ISR - Top Half

```c
static irqreturn_t button_isr(int irq, void *dev_id)
{
    button_pressed = 1;
    printk(KERN_INFO "Button pressed, scheduling workqueue\n");

    // ★ 핵심: Workqueue에 작업 등록
    queue_work(button_wq, &button_work);

    wake_up_interruptible(&wait_queue);
    return IRQ_HANDLED;
}
```

ISR이 수행하는 작업은 딱 세 가지다:

1. `button_pressed = 1` — 버튼 상태 플래그 설정
2. `queue_work()` — Bottom Half 작업을 Workqueue에 등록
3. `wake_up_interruptible()` — `read()`에서 대기 중인 프로세스 깨우기

LED를 직접 제어하거나 `msleep()`을 호출하는 코드가 ISR에서 완전히 제거되었다. ISR의 실행 시간은 수 μs로 극히 짧다.

### `read()` 핸들러

```c
static ssize_t dev_read(struct file *filep, char *buffer, size_t len, loff_t *offset)
{
    wait_event_interruptible(wait_queue, button_pressed == 1);
    button_pressed = 0;
    return 0;
}
```

`28_buttonint`의 `read()`에서 LED 제어 코드가 모두 제거되었다. 이제 `read()`는 순수하게 **"버튼이 눌렸는지 확인"**하는 역할만 수행한다. 버튼이 눌리면 즉시 반환하고, LED 점멸은 Workqueue가 별도로 처리한다.

### 모듈 초기화 함수 핵심 부분

```c
static int __init blocking_io_init(void)
{
    // ... GPIO 설정, 인터럽트 등록 ...

    // ★ Workqueue 초기화 — 28_buttonint에 없었던 부분
    button_wq = create_singlethread_workqueue("button_wq");
    INIT_WORK(&button_work, button_work_func);

    // ... 캐릭터 디바이스 등록 ...
}
```

초기화 순서: **GPIO 설정 → 인터럽트 등록 → Workqueue 생성 → 디바이스 등록**

### 실행 흐름 비교

```
[28_buttonint — 동기 방식]
버튼 누름 → read() 진입 → LED ON → 1초 대기 → LED OFF → read() 반환 → "Button pressed!" 출력

[29_buttonbh — 비동기 방식]
버튼 누름 → ISR → queue_work() + wake_up()
           → read() 즉시 반환 → "Button pressed!" 출력  (거의 동시에)
           → Workqueue: LED ON → 1초 대기 → LED OFF    (별도 스레드)
```

## Delayed Work — 지연 스케줄링

기본 `work_struct`는 `queue_work()` 호출 즉시(스케줄러 사정이 허락하는 한 빠르게) 실행된다. 반면 **Delayed Work**는 지정한 시간이 경과한 후에 실행을 시작한다.

### `delayed_work` API

```c
#include <linux/workqueue.h>

static struct delayed_work my_delayed_work;

// 초기화
INIT_DELAYED_WORK(&my_delayed_work, my_delayed_func);

// 500ms 후 실행 등록
queue_delayed_work(my_wq, &my_delayed_work, msecs_to_jiffies(500));

// 전역 Workqueue 사용 시
schedule_delayed_work(&my_delayed_work, msecs_to_jiffies(500));

// 취소 (대기 중이면 취소, 실행 중이면 완료 대기)
cancel_delayed_work_sync(&my_delayed_work);
```

### Delayed Work 활용 - 소프트웨어 디바운스

하드웨어 디바운스가 불충분한 경우, ISR에서 즉시 처리하지 않고 Delayed Work로 일정 시간 후에 버튼 상태를 재확인하는 패턴을 사용할 수 있다.

```c
// ISR
static irqreturn_t button_isr(int irq, void *dev_id)
{
    // 50ms 후에 버튼 상태를 재확인
    mod_delayed_work(button_wq, &debounce_work, msecs_to_jiffies(50));
    return IRQ_HANDLED;
}

// Delayed Work 핸들러
static void debounce_func(struct work_struct *work)
{
    // 50ms 후에도 여전히 눌려 있으면 진짜 버튼 이벤트
    if (gpio_get_value(BUTTON_PIN) == 0) {
        // 실제 버튼 처리
        queue_work(button_wq, &button_work);
    }
}
```

> `mod_delayed_work()`는 이미 등록된 Delayed Work의 타이머를 재설정한다. 50ms 이내에 인터럽트가 여러 번 발생하면 타이머가 계속 리셋되므로, 마지막 인터럽트로부터 50ms 후에 1회만 실행된다. 이것이 소프트웨어 디바운스의 핵심 원리다.
> 

## Threaded IRQ - 더 현대적인 대안 (참고)

### `request_threaded_irq()` API

```c
int request_threaded_irq(
    unsigned int irq,
    irq_handler_t handler,      // Top Half (hardirq) — NULL 가능
    irq_handler_t thread_fn,    // Bottom Half (스레드)
    unsigned long irqflags,
    const char *devname,
    void *dev_id
);
```

### `29_buttonbh`를 Threaded IRQ로 변환한 참고 코드

```c
// Top Half — 최소 처리만 수행
static irqreturn_t button_hardirq(int irq, void *dev_id)
{
    // IRQ_WAKE_THREAD를 반환하면 커널이 자동으로 thread_fn을 실행
    return IRQ_WAKE_THREAD;
}

// Bottom Half — 커널 스레드에서 실행 (sleep 가능)
static irqreturn_t button_thread_fn(int irq, void *dev_id)
{
    button_pressed = 1;
    wake_up_interruptible(&wait_queue);

    gpio_set_value(LED_PIN, 1);
    msleep(1000);
    gpio_set_value(LED_PIN, 0);

    return IRQ_HANDLED;
}

// 등록 — Workqueue 생성 불필요
ret = request_threaded_irq(button_irq,
                            button_hardirq,    // Top Half
                            button_thread_fn,  // Bottom Half
                            IRQF_TRIGGER_FALLING,
                            "button_irq", NULL);
```

Threaded IRQ 방식은 `create_singlethread_workqueue()`, `INIT_WORK()`, `queue_work()`, `flush_workqueue()`, `destroy_workqueue()` 호출이 모두 불필요하다. 커널이 IRQ 전용 스레드를 자동으로 관리한다. **코드가 훨씬 간결하므로, 실무에서는 Threaded IRQ가 선호된다.**

## 핵심 요약

| 개념 | 한 줄 정리 |
| --- | --- |
| Top Half (ISR) | 인터럽트 원인 확인 + 플래그 설정 + Bottom Half 스케줄링만 수행, 수 μs 이내 종료 |
| Bottom Half (Workqueue) | 커널 스레드에서 실행, sleep/mutex/긴 처리 모두 가능 |
| `INIT_WORK()` | work_struct와 콜백 함수를 연결 |
| `queue_work()` | ISR에서 호출, Bottom Half를 Workqueue에 등록 |
| `flush_workqueue()` | 모듈 종료 시 진행 중인 작업이 완료될 때까지 대기 |
| `destroy_workqueue()` | 커널 스레드 종료 및 자원 반환, 반드시 flush 후에 호출 |
| Delayed Work | 지정 시간 후 실행, 소프트웨어 디바운스에 활용 |
| Threaded IRQ | Workqueue 없이 하나의 API로 Top/Bottom Half 분리, 실무 권장 방식 |

# CHAP 15. Mutex와 동시성 제어

## Race Condition 발생 시나리오

### Race Condition이란

Race Condition은 두 개 이상의 실행 흐름이 **동시에** 공유 자원에 접근하면서, 접근 순서에 따라 결과가 달라지는 현상이다.

임베디드 시스템에서 특히 위험한 이유는 하드웨어 레지스터 하나의 값이 꼬이면 LED가 의도하지 않은 상태가 되거나, 최악의 경우 하드웨어 손상으로 이어질 수 있기 때문이다.

커널 드라이버에서 Race Condition이 발생하면 사용자 공간 프로그램과 달리 **시스템 전체가 불안정**해진다. Oops나 Panic이 발생할 수 있으며, 데이터 손실이나 파일시스템 손상까지 이어질 수 있다.

### 시나리오 1: 두 프로세스가 동시에 `/dev/led_device`에 쓰는 상황

프로세스 A는 LED를 켜려 하고(`'1'` 쓰기), 프로세스 B는 LED를 끄려 한다(`'0'` 쓰기). 두 프로세스가 거의 동시에 `write()` 시스템 콜을 호출하면:

```
시간 →

프로세스 A (LED ON)          프로세스 B (LED OFF)
─────────────────────        ─────────────────────
write(fd, "1", 1)            write(fd, "0", 1)
  ├ copy_from_user()
  │                            ├ copy_from_user()
  ├ gpio_set_value(pin, 1)
  │                            ├ gpio_set_value(pin, 0)
  ├ led_status = 1
  │                            ├ led_status = 0
  └ return                     └ return

결과: LED는 꺼져 있지만 led_status는?
→ 실행 순서에 따라 1이 될 수도, 0이 될 수도 있다!
```

`led_status` 변수의 최종값이 실제 LED 하드웨어 상태와 불일치하는 **상태 불일치(inconsistency)** 문제가 발생한다.

### 시나리오 2: 인터럽트 핸들러와 일반 코드의 변수 공유

ISR이 `button_pressed` 플래그를 설정하고, `read()` 핸들러가 이 플래그를 읽어 반환하는 구조에서:

```
시간 →

read() 핸들러 (프로세스 컨텍스트)    ISR (인터럽트 컨텍스트)
──────────────────────────────        ──────────────────────
if (button_pressed) {
  │  (여기서 preemption됨)            ← 인터럽트 발생!
  │                                   button_pressed = 1;
  │                                   wake_up_interruptible(...);
  │                                   return IRQ_HANDLED;
  │                                   → preemption 복귀
    button_pressed = 0;
    copy_to_user(buf, "1", 1);
}
```

타이밍에 따라 버튼 이벤트를 놓치거나, 동일 이벤트를 두 번 처리하는 문제가 발생할 수 있다.

> 인터럽트는 **언제든** 발생할 수 있으므로, 일반 코드와 ISR이 공유하는 변수는 반드시 보호해야 한다.
> 

### 시나리오 3: Read-Modify-Write 패턴의 위험

GPIO 레지스터를 직접 조작하는 경우, 특정 비트만 변경하려면 "읽고 → 수정하고 → 쓰는" 3단계가 필요하다.

```c
/* 위험한 코드: 보호 없는 Read-Modify-Write */
u32 val = readl(gpio_regs + DATA_OFFSET);  /* 읽기 */
val |= (1 << pin);                          /* 수정 */
writel(val, gpio_regs + DATA_OFFSET);       /* 쓰기 */
```

두 스레드가 서로 다른 핀을 동시에 제어하려 할 때, 읽기와 쓰기 사이에 다른 스레드가 끼어들면 **한쪽의 변경이 사라지는** "Lost Update" 문제가 발생한다.

```
스레드 A (pin 7 ON)              스레드 B (pin 8 ON)
──────────────────────           ──────────────────────
val = readl(DATA)  // 0x0000
val |= (1 << 7)    // 0x0080
                                 val = readl(DATA)  // 0x0000 (!)
                                 val |= (1 << 8)    // 0x0100
writel(val, DATA)  // 0x0080
                                 writel(val, DATA)  // 0x0100 ← pin 7 사라짐!

기대 결과: 0x0180 (pin 7, 8 모두 ON)
실제 결과: 0x0100 (pin 8만 ON, pin 7의 변경이 Lost!)
```

### Race Condition이 재현하기 어려운 이유

Race Condition의 가장 교활한 특성은 **대부분의 경우 정상 동작한다**는 점이다. 문제가 발생하려면 두 실행 흐름이 아주 좁은 시간 창(Critical Window) 안에서 겹쳐야 한다.

이런 특성 때문에 개발 중에는 발견하지 못하고, 제품 출시 후 부하가 높은 상황에서 간헐적으로 발생하여 디버깅이 극히 어렵다.

> **핵심 원칙: "지금 문제가 안 보인다"는 것이 "문제가 없다"를 의미하지 않는다. 공유 자원은 항상 보호하라.**
> 

## Mutex API

### Mutex 개요

Mutex(MUTual EXclusion)는 Linux 커널에서 가장 널리 사용되는 동기화 메커니즘이다. "상호 배제"라는 이름 그대로, 한 번에 하나의 실행 흐름만 Critical Section에 진입할 수 있도록 보장한다.

Mutex의 핵심 특성:

- **소유권(Ownership)**: Mutex를 잠근(lock) 주체만 잠금을 해제(unlock)할 수 있다. 다른 스레드가 해제하면 커널이 경고를 발생시킨다.
- **Sleep 가능**: Mutex를 획득하지 못한 스레드는 Sleep 상태로 전환되어 CPU를 양보한다. 이 특성 때문에 **인터럽트 컨텍스트에서는 Mutex를 사용할 수 없다.**
- **Priority Inheritance**: RT(Real-Time) 커널 설정에서 Priority Inversion 문제를 완화하기 위해 우선순위 상속을 지원한다.

### 정적 선언과 동적 초기화

### 방법 1: 정적 선언 (컴파일 타임 초기화)

```c
#include <linux/mutex.h>

/* 전역 변수로 선언과 동시에 초기화 */
static DEFINE_MUTEX(led_mutex);
```

`DEFINE_MUTEX` 매크로는 `struct mutex` 변수를 선언하면서 동시에 초기화한다. 모듈의 전역 Mutex에 적합하다. `device_create()` 이전에 Mutex가 이미 초기화되어 있으므로 타이밍 문제를 원천적으로 방지한다.

### 방법 2: 동적 초기화 (런타임 초기화)

```c
#include <linux/mutex.h>

static struct mutex led_mutex;

static int __init my_init(void) {
    mutex_init(&led_mutex);
    /* ... */
}
```

`mutex_init()`은 런타임에 Mutex를 초기화한다. 구조체 멤버로 Mutex를 포함하거나, 초기화 시점을 제어해야 할 때 사용한다.

> **주의**: `device_create()`가 완료되면 즉시 유저 공간에서 `open()` → `write()`가 가능해지는데, 이 시점에 Mutex가 아직 초기화되지 않았다면 미정의 동작이 발생한다. `DEFINE_MUTEX()` 정적 선언을 사용하면 이 문제가 자연스럽게 해결된다.
> 

### Lock / Unlock 기본 API

```c
/* 기본 잠금: 획득할 때까지 무한정 대기 (Sleep) */
void mutex_lock(struct mutex *lock);

/* 잠금 해제 */
void mutex_unlock(struct mutex *lock);
```

사용 패턴:

```c
mutex_lock(&led_mutex);
/* ---- Critical Section 시작 ---- */
gpio_set_value(GPIO_LED_PIN, 1);
led_status = 1;
/* ---- Critical Section 종료 ---- */
mutex_unlock(&led_mutex);
```

`mutex_lock()`은 Mutex를 획득할 수 없으면 **Sleep 상태로 전환**된다. 다른 스레드가 `mutex_unlock()`을 호출하면 대기 중인 스레드가 깨어나서 Mutex를 획득한다.

### `mutex_lock_interruptible()` — 시그널 대응 버전

```c
/* 시그널에 의해 중단 가능한 잠금
 * 반환값: 0 = 성공, -EINTR = 시그널에 의해 중단됨 */
int mutex_lock_interruptible(struct mutex *lock);
```

`mutex_lock()`은 Mutex를 획득할 때까지 **무조건 대기**한다. 사용자가 Ctrl+C를 눌러도 반응하지 않는다. 반면 `mutex_lock_interruptible()`은 시그널 수신 시 대기를 중단하고 `-EINTR`을 반환한다.

사용 패턴:

```c
if (mutex_lock_interruptible(&led_mutex))
    return -ERESTARTSYS;  /* 시그널 수신 → 시스템 콜 재시도 요청 */

/* Critical Section */
gpio_set_value(GPIO_LED_PIN, value);
led_status = value;

mutex_unlock(&led_mutex);
```

> `-ERESTARTSYS`를 반환하는 이유: 이 에러 코드를 받은 커널의 시그널 처리 로직은 "시그널 처리 후 시스템 콜을 자동으로 재시도"하거나 "유저 공간에 `-EINTR`로 반환"하는 결정을 내린다. 드라이버는 대부분의 경우 `-ERESTARTSYS`를 반환하는 것이 올바르다.
> 

### `mutex_trylock()` — 비블로킹 시도

```c
/* 비블로킹 잠금 시도
 * 반환값: 1 = 성공 (잠금 획득), 0 = 실패 (잠금 획득 못함) */
int mutex_trylock(struct mutex *lock);
```

`mutex_trylock()`은 Mutex를 즉시 획득할 수 있으면 1을 반환하고, 이미 다른 스레드가 잡고 있으면 **대기하지 않고 즉시 0을 반환**한다.

```c
if (!mutex_trylock(&led_mutex)) {
    pr_warn("LED 제어 중 — 나중에 다시 시도\n");
    return -EBUSY;
}

/* Critical Section */
gpio_set_value(GPIO_LED_PIN, value);
led_status = value;

mutex_unlock(&led_mutex);
```

이 패턴은 "잠금을 기다리느니 차라리 실패를 알려주는 것이 낫다"는 경우에 사용한다. 비블로킹 I/O(`O_NONBLOCK`)를 지원하는 드라이버에서 주로 활용된다.

### Mutex 사용 규칙 정리

| 규칙 | 설명 |
| --- | --- |
| 프로세스 컨텍스트에서만 사용 | ISR, Tasklet, Softirq에서 사용 금지 (sleep하므로) |
| Lock한 주체가 Unlock해야 한다 | 소유권 위반 시 커널 WARNING 발생 |
| 재귀 Lock 금지 | 같은 스레드가 두 번 `mutex_lock()` 호출 → Deadlock |
| Lock 상태에서 모듈 Unload 금지 | `rmmod` 전에 모든 Mutex가 해제되었는지 확인 |
| 중첩 Lock 순서 일관성 유지 | Mutex A → B 순서로 잠갔으면, 모든 코드 경로에서 동일 순서 유지 |

## Spinlock

### Spinlock 개요

Spinlock은 Mutex와 같은 목적의 동기화 메커니즘이지만, 잠금을 획득하지 못한 스레드가 **Sleep하지 않고 바쁜 대기(busy-wait, spinning)**하는 것이 핵심 차이다.

```
Mutex:    잠금 실패 → Sleep (CPU 양보) → Wake Up 후 재시도
Spinlock: 잠금 실패 → 계속 루프 돌며 재시도 (CPU 점유 유지)
```

"왜 CPU를 낭비하면서까지 바쁜 대기를 하는가?"라는 질문의 답은 **인터럽트 컨텍스트**에 있다. ISR, Tasklet, Softirq는 Sleep할 수 없다. 이들 컨텍스트에서 공유 자원을 보호하려면 Spinlock이 유일한 선택이다.

### 기본 Spinlock API

```c
#include <linux/spinlock.h>

/* 정적 선언 */
static DEFINE_SPINLOCK(my_lock);

/* 또는 동적 초기화 */
static spinlock_t my_lock;
spin_lock_init(&my_lock);

/* 기본 잠금/해제 */
spin_lock(&my_lock);
/* Critical Section */
spin_unlock(&my_lock);
```

### `spin_lock_irqsave()` / `spin_unlock_irqrestore()`

ISR과 프로세스 컨텍스트 코드가 같은 자원을 공유하는 경우, 단순 `spin_lock()`만으로는 부족하다. 프로세스 코드가 Spinlock을 잡은 상태에서 인터럽트가 발생하면, ISR이 같은 Spinlock을 시도하여 **Deadlock**에 빠진다 (ISR은 프로세스로 복귀해야 하는데, 프로세스는 ISR이 끝나길 기다리는 교착 상태).

해결: Spinlock 획득 시 **로컬 CPU의 인터럽트를 비활성화**한다.

```c
unsigned long flags;

spin_lock_irqsave(&my_lock, flags);     /* 인터럽트 비활성화 + 잠금 */
/* Critical Section — 인터럽트 발생 불가 */
spin_unlock_irqrestore(&my_lock, flags); /* 잠금 해제 + 인터럽트 복원 */
```

`flags` 변수는 Spinlock 획득 전의 인터럽트 플래그 상태를 저장한다. `spin_unlock_irqrestore()`는 이전 상태 그대로 복원하므로, 중첩 호출에도 안전하다.

> **ISR 내부에서는 `spin_lock()` / `spin_unlock()`을 사용한다.** ISR은 이미 인터럽트가 비활성화된 상태에서 실행되므로 `_irqsave` 버전이 불필요하다.
> 

```c
/* ISR 내부 */
irqreturn_t my_isr(int irq, void *dev_id) {
    spin_lock(&my_lock);
    /* shared_data 접근 */
    spin_unlock(&my_lock);
    return IRQ_HANDLED;
}

/* 프로세스 컨텍스트 */
ssize_t my_read(struct file *file, ...) {
    unsigned long flags;
    spin_lock_irqsave(&my_lock, flags);
    /* shared_data 접근 */
    spin_unlock_irqrestore(&my_lock, flags);
    return count;
}
```

### Spinlock 사용 규칙

| 규칙 | 이유 |
| --- | --- |
| Critical Section을 최소화하라 | Spinlock 보유 중 다른 CPU는 바쁜 대기 → CPU 시간 낭비 |
| Sleep 가능 함수 호출 금지 | `kmalloc(GFP_KERNEL)`, `mutex_lock()`, `copy_from_user()` 등 금지 |
| `copy_from_user()` / `copy_to_user()` 금지 | Page Fault 발생 가능 → Sleep 유발 |
| ISR과 공유 시 `_irqsave` 사용 | Deadlock 방지 |
| 단일 코어에서도 `_irqsave` 사용 | Preemption + 인터럽트 조합에 의한 Deadlock 방지 |

### Mutex vs Spinlock 선택 기준

| 기준 | Mutex | Spinlock |
| --- | --- | --- |
| 실행 컨텍스트 | 프로세스 컨텍스트만 | 모든 컨텍스트 (ISR 포함) |
| 대기 방식 | Sleep (CPU 양보) | Busy-wait (CPU 점유) |
| Critical Section 길이 | 길어도 됨 (Sleep 가능) | **반드시 짧아야 함** |
| `copy_from_user()` 사용 | 가능 | 불가 |
| `kmalloc(GFP_KERNEL)` 사용 | 가능 | 불가 (`GFP_ATOMIC`만) |
| Sleep 함수 (`msleep` 등) | 가능 | 불가 |
| Deadlock 위험 | 비교적 적음 | 인터럽트 미비활성화 시 높음 |
| 오버헤드 | Context Switch 비용 | CPU Spinning 비용 |

## `atomic_t` 원자 연산

### `atomic_t` 개요

간단한 정수 카운터 하나를 보호하기 위해 Mutex나 Spinlock을 사용하는 것은 과도한 오버헤드다. 이런 경우 `atomic_t` 타입과 원자 연산 함수를 사용한다.

`atomic_t`는 CPU의 하드웨어 수준 원자 명령(ARM의 `LDREX`/`STREX`, x86의 `LOCK` 접두사)을 활용하여 **Lock 없이 안전한 정수 연산을 보장**한다.

```c
#include <linux/atomic.h>

/* 선언 및 초기화 */
static atomic_t open_count = ATOMIC_INIT(0);
```

### 주요 원자 연산 함수

```c
/* 읽기 */
int atomic_read(const atomic_t *v);

/* 쓰기 */
void atomic_set(atomic_t *v, int i);

/* 증가 / 감소 */
void atomic_inc(atomic_t *v);   /* v++ */
void atomic_dec(atomic_t *v);   /* v-- */

/* 증가/감소 후 결과 반환 */
int atomic_inc_return(atomic_t *v);  /* return ++v */
int atomic_dec_return(atomic_t *v);  /* return --v */

/* 감소 후 0인지 테스트 (참조 카운트에 유용) */
bool atomic_dec_and_test(atomic_t *v);  /* --v == 0 ? true : false */

/* Compare-And-Swap (CAS) */
int atomic_cmpxchg(atomic_t *v, int old, int new);
/* v의 현재값이 old와 같으면 new로 교체하고 old값 반환
 * 다르면 교체하지 않고 현재값 반환 */
```

### `atomic_cmpxchg()` 상세

`atomic_cmpxchg()`는 가장 강력한 원자 연산이다. "현재 값이 expected와 같을 때만 new_value로 변경"하는 Compare-And-Swap(CAS) 패턴을 하드웨어 레벨에서 원자적으로 수행한다.

```c
int old_val = atomic_cmpxchg(&open_count, 0, 1);
/*
 * open_count가 0이면:
 *   → open_count를 1로 변경
 *   → 0 (이전값) 반환
 *
 * open_count가 0이 아니면:
 *   → open_count 변경 안 함
 *   → 현재값 반환 (0이 아닌 값)
 */
```

이 연산은 15.5절의 "단일 Open 보장 패턴"에서 핵심적으로 사용된다.

### `atomic_t` 사용 시 주의사항

`atomic_t`는 **단일 변수에 대한 단일 연산만** 보호한다. 다음과 같은 경우에는 `atomic_t`로 충분하지 않다:

```c
/* 위험: 두 원자 연산 사이의 틈 */
if (atomic_read(&counter) == 0) {    /* 읽기는 원자적 */
    /* ← 여기서 다른 스레드가 counter를 변경할 수 있다! */
    atomic_set(&counter, 1);         /* 쓰기도 원자적이지만... */
}
/* 읽기+조건판단+쓰기 전체가 원자적이지 않다! */

/* 올바른 방법: atomic_cmpxchg() 사용 */
if (atomic_cmpxchg(&counter, 0, 1) == 0) {
    /* 성공: 0→1 변경 완료 (원자적) */
}
```

또한 두 개 이상의 변수를 동시에 일관되게 변경해야 하는 경우(예: `led_status` + `gpio_set_value()`)에는 Mutex나 Spinlock을 사용해야 한다.

## 단일 Open 보장 패턴

### 단일 Open이 필요한 이유

LED 드라이버처럼 하드웨어를 직접 제어하는 디바이스에서는, 여러 프로세스가 동시에 `/dev/led_device`를 열어 제어하면 혼란이 발생한다. 프로세스 A가 LED를 켜는 동안 프로세스 B가 끄면, 두 프로세스 모두 의도한 대로 동작하지 않는다.

이를 방지하기 위해 "한 번에 하나의 프로세스만 디바이스를 열 수 있다"는 제약을 두는 것이 **단일 Open 보장 패턴**이다.

### `atomic_cmpxchg()` 기반 구현

```c
static atomic_t device_open_count = ATOMIC_INIT(0);

static int led_open(struct inode *inode, struct file *file) {
    /* 현재값이 0(닫힌 상태)일 때만 1(열린 상태)로 변경 시도 */
    if (atomic_cmpxchg(&device_open_count, 0, 1) != 0) {
        pr_warn("led_device: already opened by another process\n");
        return -EBUSY;  /* 이미 열려 있음 */
    }

    pr_info("LED Device Opened\n");
    return 0;
}

static int led_release(struct inode *inode, struct file *file) {
    atomic_set(&device_open_count, 0);  /* 닫힌 상태로 복원 */

    pr_info("LED Device Closed\n");
    return 0;
}
```

동작 흐름:

```
프로세스 A: open() → cmpxchg(0→1) 성공 → fd 획득
프로세스 B: open() → cmpxchg(0→1) 실패(현재값=1) → -EBUSY 반환
프로세스 A: close() → atomic_set(0) → 열림 상태 해제
프로세스 B: open() → cmpxchg(0→1) 성공 → fd 획득 (이제 가능)
```

### 단일 Open vs Mutex 보호의 차이

단일 Open과 Mutex 보호는 **다른 계층의 보호**다.

- **단일 Open**: 디바이스 파일을 한 프로세스만 열 수 있도록 제한 (`open()` 레벨)
- **Mutex 보호**: 이미 열린 디바이스에 대한 `read()` / `write()` 호출의 동시성 제어

`30_ledmutex` 예제에서는 단일 Open 제한 없이 Mutex만 사용한다. 여러 프로세스가 동시에 `open()` 가능하지만, `read()` / `write()` 내부의 GPIO 접근은 Mutex로 직렬화된다.

두 기법을 결합하면 가장 견고한 드라이버가 된다:

```c
static atomic_t device_open_count = ATOMIC_INIT(0);
static DEFINE_MUTEX(led_mutex);

static int led_open(struct inode *inode, struct file *file) {
    if (atomic_cmpxchg(&device_open_count, 0, 1) != 0)
        return -EBUSY;
    return 0;
}

static ssize_t led_write(struct file *file, const char __user *buf,
                          size_t len, loff_t *offset) {
    /* 단일 Open이 보장되더라도, 멀티스레드 프로세스는
     * 같은 fd를 여러 스레드에서 동시에 write() 가능하므로
     * Mutex 보호는 여전히 필요하다 */
    mutex_lock(&led_mutex);
    /* ... GPIO 접근 ... */
    mutex_unlock(&led_mutex);
    return len;
}
```

## `30_ledmutex` 예제 전체 분석

### 예제 파일 구조

```
30_ledmutex/
├── ledmutex.c          ← 커널 드라이버 (Mutex 적용 LED 제어)
├── ledmutex_driver.c   ← 유저 앱 (pthread 멀티스레드 테스트)
├── Makefile            ← 커널 모듈 빌드용
└── 99-led-device.rules ← udev 규칙 파일 (권한 설정)
```

### 커널 드라이버 핵심 구조: `ledmutex.c`

### 전역 변수 선언

```c
#include <linux/mutex.h>  /* ① Mutex 헤더 */

#define GPIO_LED_PIN  912        /* GPIO7 핀 (Zybo LED) */
#define DEVICE_NAME   "led_device"

static int led_status = 0;       /* ② 공유 상태 변수 */
static dev_t dev_num;
static struct cdev led_cdev;
static struct class *led_class;
static struct mutex led_mutex;   /* ③ Mutex 선언 */
```

- `led_status`가 바로 Race Condition의 대상이다. 여러 프로세스/스레드에서 동시에 읽고 쓸 수 있으므로 보호가 필요하다.
    - `struct mutex led_mutex`는 동적 초기화 방식을 사용한다. `DEFINE_MUTEX()` 정적 선언 방식과 비교하여 타이밍 문제에 주의해야 한다.
    
    ### `led_read()` 분석
    
    ```c
    static ssize_t led_read(struct file *file, char __user *buf,
                             size_t len, loff_t *offset) {
        char kbuf[2];
        int ret;
    
        printk(KERN_INFO "led_read begin\n");
    
        mutex_lock(&led_mutex);           /* ④ Mutex 잠금 */
        kbuf[0] = led_status + '0';       /* 정수 → 문자 변환 */
        kbuf[1] = '\n';
        mutex_unlock(&led_mutex);         /* ⑤ Mutex 해제 */
    
        if (*offset > 0)                  /* ⑥ 중복 읽기 방지 */
            return 0;
    
        ret = copy_to_user(buf, kbuf, 2);
        if (ret)
            return -EFAULT;
    
        *offset = 2;
        return 2;
    }
    ```
    
    `led_status` 값을 `kbuf`에 복사하는 동안 다른 스레드가 `led_status`를 변경하는 것을 방지한다. Mutex 보호 구간을 최소한으로 유지하는 점에 주목하라. **`copy_to_user()`는 Mutex 밖에서 호출한다.**
    
    > **왜 `copy_to_user()`를 Mutex 안에 넣지 않는가?** `copy_to_user()`는 유저 공간 메모리에 접근하므로 Page Fault가 발생할 수 있다. Page Fault 처리는 Sleep을 수반하며, Mutex는 프로세스 컨텍스트에서 Sleep 가능하므로 기술적으로는 넣을 수 있다. 그러나 **Critical Section을 최소화하는 것이 원칙**이다. 필요한 데이터만 커널 버퍼에 복사한 뒤 Mutex를 해제하고, 유저 복사는 그 이후에 수행한다.
    > 
    
    ### `led_write()` 분석
    
    ```c
    static ssize_t led_write(struct file *file, const char __user *buf,
                              size_t len, loff_t *offset) {
        char kbuf[10];
        int ret;
    
        if (len > sizeof(kbuf) - 1)       /* ⑦ 버퍼 오버플로 방지 */
            return -EINVAL;
    
        ret = copy_from_user(kbuf, buf, len);  /* ⑧ Mutex 밖에서 유저 복사 */
        if (ret)
            return -EFAULT;
    
        kbuf[len] = '\0';
    
        mutex_lock(&led_mutex);           /* ⑨ Mutex 잠금: Critical Section 시작 */
        if (kbuf[0] == '1') {
            gpio_set_value(GPIO_LED_PIN, 1);
            led_status = 1;               /* GPIO + status 변경이 원자적 블록 */
        } else if (kbuf[0] == '0') {
            gpio_set_value(GPIO_LED_PIN, 0);
            led_status = 0;
        } else {
            mutex_unlock(&led_mutex);     /* ⑩ 에러 경로에서도 반드시 해제! */
            return -EINVAL;
        }
        mutex_unlock(&led_mutex);         /* ⑪ 정상 경로 해제 */
    
        return len;
    }
    ```
    
    `gpio_set_value()`와 `led_status` 변경을 하나의 Mutex 보호 블록으로 묶었다. 이로써 "GPIO 하드웨어 상태"와 "소프트웨어 상태 변수"의 일관성이 보장된다.
    
    > **⑩ 에러 경로의 `mutex_unlock()`**: `else` 분기(잘못된 입력)에서 `-EINVAL`을 반환하기 전에 반드시 Mutex를 해제해야 한다. 만약 이를 빠뜨리면, 잘못된 입력이 들어올 때마다 Mutex가 잠긴 채 함수가 반환된다. 이후 모든 `read()`나 `write()` 호출은 `mutex_lock()`에서 영원히 Sleep하게 된다. **모든 코드 경로에서 `mutex_lock()`과 `mutex_unlock()`이 정확히 짝을 이루는지 확인하는 것이 Mutex 사용의 핵심이다.**
    > 
    
    ### `led_init()` 초기화 순서
    
    ```c
    static int __init led_init(void) {
        /* 1단계: GPIO 하드웨어 설정 */
        ret = gpio_request(GPIO_LED_PIN, "LED");
        gpio_direction_output(GPIO_LED_PIN, 0);
    
        /* 2단계: 캐릭터 디바이스 등록 */
        ret = alloc_chrdev_region(&dev_num, 0, 1, DEVICE_NAME);
        cdev_init(&led_cdev, &fops);
        cdev_add(&led_cdev, dev_num, 1);
    
        /* 3단계: sysfs 클래스 및 디바이스 노드 생성 */
        led_class = class_create(THIS_MODULE, DEVICE_NAME);
        device_create(led_class, NULL, dev_num, NULL, DEVICE_NAME);
    
        /* 4단계: Mutex 초기화 */
        mutex_init(&led_mutex);
    
        return 0;
    }
    ```
    
    초기화 순서와 에러 처리의 "역순 정리(unwind)" 패턴에 주목하라. 3단계에서 실패하면 2단계, 1단계에서 할당한 자원을 역순으로 정리한다.
    
    ### `led_exit()` 정리 순서
    
    ```c
    static void __exit led_exit(void) {
        gpio_set_value(GPIO_LED_PIN, 0);  /* LED 끄기 */
        gpio_free(GPIO_LED_PIN);
    
        device_destroy(led_class, dev_num);
        class_destroy(led_class);
        cdev_del(&led_cdev);
        unregister_chrdev_region(dev_num, 1);
    }
    ```
    
    정리 순서는 초기화의 역순이다: 디바이스 노드 제거 → 클래스 제거 → cdev 제거 → 번호 반환 → GPIO 해제. 이 순서를 지키지 않으면 이미 해제된 자원에 접근하는 Use-After-Free 버그가 발생할 수 있다.
    

### 유저 앱 분석: `ledmutex_driver.c`

이 유저 애플리케이션은 **Mutex가 제대로 동작하는지 검증**하기 위한 멀티스레드 테스트 프로그램이다.

```c
#include <pthread.h>

void *led_on_off(void *arg) {
    int fd;
    char *value = (char *)arg;

    fd = open(DEVICE_PATH, O_WRONLY);

    while(1) {
        write(fd, value, 1);
        sleep(1.5);  /* 주의: C 표준에서 sleep()은 unsigned int를 받으므로 1.5는 정수 1로 변환됨 */
    }
    close(fd);
    return NULL;
}

int main() {
    pthread_t tid1, tid2;

    /* 스레드 1: LED 켜기 ("1" 쓰기) */
    pthread_create(&tid1, NULL, led_on_off, "1");

    /* 스레드 2: LED 끄기 ("0" 쓰기) */
    pthread_create(&tid2, NULL, led_on_off, "0");

    pthread_join(tid1, NULL);
    pthread_join(tid2, NULL);
    return 0;
}
```

동작 원리: `main()`이 두 개의 POSIX 스레드를 생성한다. 스레드 1(`tid1`)은 무한 루프로 `"1"`을 쓰고(LED ON), 스레드 2(`tid2`)는 무한 루프로 `"0"`을 쓴다(LED OFF). 두 스레드가 동시에 `write()` 시스템 콜을 호출하여 드라이버에 도달하면, 드라이버 내부의 Mutex가 두 `write()` 호출을 직렬화한다.

> **`sleep(1.5)` 주의사항**: C 표준에서 `sleep()`은 `unsigned int`를 받으므로 `1.5`는 정수 `1`로 변환된다. 정확한 1.5초 대기가 필요하면 `usleep(1500000)` 또는 `nanosleep()`을 사용해야 한다.
> 

### udev 규칙 파일: `99-led-device.rules`

```
KERNEL=="led_device", SUBSYSTEM=="led_device", MODE="0666"
```

이 규칙은 `led_device` 노드의 권한을 `0666`(모든 사용자 읽기/쓰기)으로 설정한다. `device_create()`의 기본 권한은 `root:root 0600`이므로, 일반 사용자가 디바이스에 접근하려면 이 규칙 파일이 필요하다.

```bash
# 규칙 파일 설치
sudo cp 99-led-device.rules /etc/udev/rules.d/

# 규칙 적용
sudo udevadm control --reload-rules
sudo udevadm trigger
```

### 코드 개선 포인트

`30_ledmutex` 예제는 교육 목적으로 간결하게 작성되었다. 실무에서는 다음 사항을 개선하는 것이 좋다.

### 개선 1: `DEFINE_MUTEX()` 정적 선언 사용

```c
/* 변경 전 */
static struct mutex led_mutex;
/* led_init()에서 mutex_init(&led_mutex); */

/* 변경 후 */
static DEFINE_MUTEX(led_mutex);
/* led_init()에서 mutex_init() 호출 불필요 */
```

정적 선언을 사용하면 `device_create()` 이전에 Mutex가 이미 초기화되어 있으므로, 타이밍 문제를 원천적으로 방지한다.

### 개선 2: `mutex_lock_interruptible()` 사용

```c
/* 변경 전 */
mutex_lock(&led_mutex);

/* 변경 후 */
if (mutex_lock_interruptible(&led_mutex))
    return -ERESTARTSYS;
```

사용자가 Ctrl+C로 프로세스를 종료할 때, `mutex_lock()`은 시그널을 무시하고 대기를 계속한다. `mutex_lock_interruptible()`을 사용하면 시그널에 즉시 응답할 수 있다.

### 개선 3: 단일 Open 보장 추가

```c
static atomic_t device_open_count = ATOMIC_INIT(0);

static int led_open(struct inode *inode, struct file *file) {
    if (atomic_cmpxchg(&device_open_count, 0, 1) != 0)
        return -EBUSY;
    return 0;
}

static int led_release(struct inode *inode, struct file *file) {
    atomic_set(&device_open_count, 0);
    return 0;
}
```

## 동시성 제어 메커니즘 종합 비교

| 특성 | Mutex | Spinlock | `atomic_t` |
| --- | --- | --- | --- |
| 보호 대상 | 코드 블록 (Critical Section) | 코드 블록 (Critical Section) | 단일 정수 변수 |
| 대기 방식 | Sleep (CPU 양보) | Busy-wait (CPU 점유) | Lock 없음 (하드웨어 원자 명령) |
| ISR에서 사용 | 불가 | 가능 (`_irqsave` 필요) | 가능 |
| Sleep 함수 호출 | 가능 | 불가 | 해당 없음 |
| `copy_from/to_user()` | 가능 | 불가 | 해당 없음 |
| 오버헤드 | Context Switch 비용 | CPU Spinning 비용 | 최소 (1~2 CPU 명령) |
| 주요 사용처 | 일반 드라이버 동시성 | ISR-프로세스 간 보호 | 카운터, 플래그 |
| 구현 복잡도 | 낮음 | 중간 (irq 관리 필요) | 낮음 |

### 선택 플로우차트

```
공유 자원 접근 필요
  │
  ├── 단순 정수 카운터/플래그인가?
  │     ├── Yes → atomic_t
  │     └── No  ↓
  │
  ├── ISR에서 접근하는가?
  │     ├── Yes → Spinlock (spin_lock_irqsave)
  │     └── No  ↓
  │
  ├── Critical Section에서 Sleep이 필요한가?
  │     │   (copy_from_user, kmalloc(GFP_KERNEL), msleep 등)
  │     ├── Yes → Mutex
  │     └── No  → Mutex (기본) 또는 Spinlock (극히 짧은 경우)
  │
  └── 확신이 없으면 → Mutex (가장 안전)
```

## 핵심 요약

| 개념 | 한 줄 정리 |
| --- | --- |
| Race Condition | 두 실행 흐름이 동시에 공유 자원에 접근할 때 순서에 따라 결과가 달라지는 현상 |
| Mutex | 프로세스 컨텍스트 전용, Sleep 가능, Critical Section 보호의 기본 선택 |
| `DEFINE_MUTEX()` | 컴파일 타임 정적 초기화, 타이밍 문제 원천 방지 |
| `mutex_lock_interruptible()` | 시그널 수신 시 대기 중단, 사용자 응답성 보장 |
| Spinlock | 모든 컨텍스트(ISR 포함) 사용 가능, Critical Section은 반드시 짧게 |
| `spin_lock_irqsave()` | ISR과 프로세스 컨텍스트가 자원 공유 시 Deadlock 방지 |
| `atomic_t` | 단일 정수 변수 보호, Lock 없이 하드웨어 원자 명령 활용 |
| `atomic_cmpxchg()` | Compare-And-Swap, 단일 Open 보장 패턴의 핵심 |
| Critical Section 최소화 | `copy_from/to_user()`는 Mutex 밖에서, 꼭 필요한 코드만 보호 |
| 에러 경로 unlock | 모든 코드 경로에서 `mutex_lock()`과 `mutex_unlock()`이 반드시 짝을 이뤄야 함 |