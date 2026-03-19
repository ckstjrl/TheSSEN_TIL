# 임베디드 리눅스 커널 프로그래밍 day04

날짜: 2026년 3월 19일

# 리눅스 가상 메모리 (동적 메모리 할당)

## 커널의 힙과 동적 메모리

### 커널의 개념

리눅스 커널은 사용자 공간의 `malloc()` 과 유사한 동적 메모리 할당 기능을 제공

but, 사용자 공간과는 다르게 페이지 테이블을 관리해야 하며, 성능을 고려해야 함

커널은 동적 메모리를 할당 방식

- `kmalloc()` : 가상 메모리 공간에서 연속되어 있다면, 물리적으로 연속된 메모리 할당
- `vmalloc()` : 가상 메모리 공간에서 연속된 메모리 할당 (물리적으로 불연속 가능)
- **slab/slub allocator** : 작은 크기의 메모리 할당 최적화

커널과 사용자 공간의 메모리 할당 차이

| 메모리 할당 방식 | 사용자 공간 | 커널 공간 |
| --- | --- | --- |
| 할당 함수 | malloc() | kmalloc(), vmalloc(), slab_alloc() |
| 메모리 배치 | 가상 메모리, 스왑 가능 | 물리 메모리, 스왑 불가 |
| 페이지 연속성 | 가상 주소만 연속됨 | kmalloc()은 물리적으로 연속됨 |
| 보호 기법 | 프로세스마다 독립적 | 전체 커널이 공유 |

### `kmalloc()`과 `vmalloc()`의 차이

`kmalloc()`

- 물리적으로 연속된 메모리 블록을 할당한다.
- 빠른 속도를 제공하지만, 큰 메모리 할당이 어렵다.
- 일반적으로 4KB(한 페이지) 이하의 작은 메모리 할당에 적합하다.

`vmalloc()`

- 가상 메모리에서는 연속된 주소를 제공하지만, 물리적으로는 불연속적일 수 있다.
- 큰 메모리를 할당할 수 있지만, 속도가 느리다.
- 페이지 테이블을 수정해야 하므로 오버헤드가 크다.

### **Slab/Slub Allocator**란?

커널에서는 자주 사용되는 작은 크기의 구조체를 효율적으로 관리하기 위해 Slab Allocator를 사용한다.

Slab Allocator의 특징

- 자주 사용하는 데이터 구조체를 미리 메모리 풀(Pool)로 할당.
- 할당/해제 성능이 `kmalloc()` 보다 더 빠름.
- 메모리 단편화를 방지.

### 메모리 할당 최적화 전략

작은 객체는 **slab allocator** 사용

- `kmalloc()` 대신 `kmem_cache_alloc()` 을 사용하면 더 빠른 할당 가능.

큰 메모리는 `vmalloc()` 사용

- 1MB 이상의 큰 메모리를 할당해야 하면 `vmalloc()` 사용.

GFP 플래그 활용

- GFP_ATOMIC : 인터럽트 컨텍스트에서 메모리 할당 가능.
- GFP_KERNEL : 일반적인 커널 메모리 할당.
- GFP_DMA : DMA 버퍼를 위한 메모리 할당.

# ioctl 인터페이스 설계와 구현

## 예제 26

```c
/*
 * 26_ledon_ioctl/led_app.c
 *
 * 목적: ioctl 드라이버(led_on.c)를 테스트하는 사용자 공간 애플리케이션.
 *
 * 컴파일: gcc -o led_app led_app.c
 * 실행:   sudo ./led_app /dev/led_ioctl
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Copyright (c) 2024 HONG YUNG KI (guileschool@gmail.com)
 */

#include <stdio.h>
#include <stdlib.h>
#include <fcntl.h>
#include <unistd.h>
#include <sys/ioctl.h>

/* 드라이버와 동일한 매직 넘버 및 명령 정의 */
#define LED_MAGIC   'L'
#define LED_ON_CMD  _IO(LED_MAGIC, 0)
#define LED_OFF_CMD _IO(LED_MAGIC, 1)
#define LED_GET_CMD _IOR(LED_MAGIC, 2, int)

int main(int argc, char *argv[])
{
    int fd;
    int led_state;
    const char *device = (argc > 1) ? argv[1] : "/dev/led_ioctl";

    /* O_RDWR: 드라이버가 O_RDWR 모드 확인을 수행하므로 필수 */
    fd = open(device, O_RDWR);
    if (fd < 0) {
        perror("open");
        return EXIT_FAILURE;
    }

    /* LED 현재 상태 확인 */
    if (ioctl(fd, LED_GET_CMD, &led_state) < 0) {
        perror("ioctl LED_GET_CMD");
        close(fd);
        return EXIT_FAILURE;
    }
    printf("초기 LED 상태: %d (%s)\n", led_state, led_state ? "ON" : "OFF");

    /* LED 켜기 */
    if (ioctl(fd, LED_ON_CMD) < 0) {
        perror("ioctl LED_ON_CMD");
        close(fd);
        return EXIT_FAILURE;
    }
    printf("LED ON 명령 전송\n");
    sleep(2);

    /* LED 끄기 */
    if (ioctl(fd, LED_OFF_CMD) < 0) {
        perror("ioctl LED_OFF_CMD");
        close(fd);
        return EXIT_FAILURE;
    }
    printf("LED OFF 명령 전송\n");
    sleep(1);

    /* 최종 상태 확인 */
    if (ioctl(fd, LED_GET_CMD, &led_state) < 0) {
        perror("ioctl LED_GET_CMD");
        close(fd);
        return EXIT_FAILURE;
    }
    printf("최종 LED 상태: %d (%s)\n", led_state, led_state ? "ON" : "OFF");

    close(fd);
    return EXIT_SUCCESS;
}

```

# CHAP 11. ioctl 인터페이스

## ioctl이 필요한 이유

`read()` / `write()`는 **바이트 스트림** 기반이므로, 아래 같은 명령 전달에는 적합하지 않다:

- LED 점멸 주기 설정
- 하드웨어 레지스터 상태를 구조체로 읽기
- 드라이버 동작 모드 전환 (수동/자동/타이머)
- 펌웨어 버전 조회

이런 경우는 **명령(command)을 전달하고 응답(response)을 받는 제어 채널(control channel)** 개념이 필요하다.
→ 이것이 `ioctl`의 역할.

```c
#include <sys/ioctl.h>
int ioctl(int fd, unsigned long request, ...);
```

| 파라미터 | 설명 |
| --- | --- |
| `fd` | `open()`으로 열은 디바이스 파일 디스크립터 |
| `request` | 드라이버가 정의한 명령 코드 |
| `...` | 명령에 따른 추가 데이터 (값 또는 포인터) |

## ioctl vs sysfs attribute

| 비교 항목 | ioctl | sysfs attribute |
| --- | --- | --- |
| 인터페이스 형태 | 프로그래밍 API | 파일 I/O (`echo` / `cat`) |
| 데이터 전달 | 구조체, 정수, 포인터 등 | 문자열 기반 |
| 원자성 | 단일 시스템 콜 | `open → write → close` 다단계 |
| 디버깅 | `strace`로 추적 | `echo / cat`으로 즉시 테스트 |
| 멀티 명령 | 하나의 fd에 다수 명령 가능 | 명령별 별도 파일 필요 |

> **실무 패턴**: 런타임에 빈번히 호출되는 제어 명령은 `ioctl`로, 초기 설정/디버깅 파라미터는 `sysfs`로.
> 

## ioctl 명령 코드의 32비트 구조 ⭐

Linux 커널은 32비트 명령 코드 안에 **4개 필드**를 인코딩한다:

```
| 방향(dir) | 크기(size) | 매직(magic) | 번호(nr) |
|  2 bit   |   14 bit  |    8 bit   |  8 bit  |
| [31:30]  |  [29:16]  |   [15:8]   |  [7:0]  |
```

| 필드 | 비트 | 설명 |
| --- | --- | --- |
| `nr` (번호) | [7:0] | 같은 드라이버 내에서 명령을 구분하는 순번 |
| `type` (매직 넘버) | [15:8] | 드라이버를 식별하는 고유 문자 (예: `'L'`, `'G'`) |
| `size` | [29:16] | 전달 데이터 크기 (`sizeof(type)` 결과) |
| `dir` (방향) | [31:30] | 데이터 전달 방향 (없음/읽기/쓰기/양방향) |

## 명령 코드 생성 매크로

```c
#include <linux/ioctl.h>

// 데이터 전달 없는 명령 (dir=0, size=0)
#define _IO(type, nr)

// 유저 → 커널 방향 쓰기 (Write)
#define _IOW(type, nr, datatype)

// 커널 → 유저 방향 읽기 (Read)
#define _IOR(type, nr, datatype)

// 양방향 (Read + Write)
#define _IOWR(type, nr, datatype)
```

> ⚠️ **방향의 기준은 유저 공간(애플리케이션) 관점이다.**
> 
> - `_IOW`: 유저가 Write (유저 → 커널로 데이터 전달)
> - `_IOR`: 유저가 Read (커널 → 유저로 데이터 전달)

### **예제 명령 코드 정의:**

```c
#define LED_ON     _IOW('L', 0, int)   // 유저 → 커널: LED 켜기
#define LED_OFF    _IOW('L', 1, int)   // 유저 → 커널: LED 끄기
#define LED_STATUS _IOR('L', 2, int)   // 커널 → 유저: LED 상태 읽기
```

### **실제 32비트 값 계산 예시:**

```
LED_ON = _IOW('L', 0, int)
  dir  = 01 (Write)
  size = 4 (sizeof(int))
  type = 0x4C ('L'의 ASCII)
  nr   = 0
→ 0x40044C00
```

## 매직 넘버 충돌 방지

매직 넘버(`type` 필드)는 서로 다른 드라이버의 ioctl 명령이 우연히 같은 코드를 갖지 않도록 구분한다.

| 매직 | 용도 | 출처 |
| --- | --- | --- |
| `'T'` (0x54) | 터미널 (tty) | POSIX 표준 |
| `'V'` (0x56) | Video4Linux | V4L2 서브시스템 |
| `'S'` (0x53) | SCSI | SCSI 서브시스템 |
| `'L'` (0x4C) | LED 제어 (예제용) | 이 교재 예제 |

> 커널 소스 트리의 `Documentation/userspace-api/ioctl/ioctl-number.rst`에서 이미 사용 중인 매직 넘버 목록을 확인할 수 있다.
> 

## unlocked_ioctl 핸들러 구현

### file_operations 등록

```c
static struct file_operations fops = {
    .owner          = THIS_MODULE,
    .open           = led_open,
    .release        = led_release,
    .unlocked_ioctl = led_ioctl,   // ← ioctl 핸들러 등록
};
```

> **역사적 배경**: 과거의 `.ioctl`은 Big Kernel Lock(BKL)을 자동 획득했다. 성능 병목이었던 BKL은 Linux 2.6.36 이후 제거되었고, 현재는 `.unlocked_ioctl`만 사용한다.
> 

### 핸들러 함수 시그니처

```c
static long led_ioctl(struct file *file, unsigned int cmd, unsigned long arg);
```

| 파라미터 | 타입 | 의미 |
| --- | --- | --- |
| `file` | `struct file *` | 열린 파일 인스턴스 |
| `cmd` | `unsigned int` | ioctl 명령 코드 |
| `arg` | `unsigned long` | 추가 인자 (값 또는 유저 공간 포인터) |
| 반환값 | `long` | 0(성공) 또는 음수 에러 코드 |

### `arg` 해석 방법

| 명령 종류 | arg 처리 |
| --- | --- |
| `_IO` 명령 | arg 무시 |
| `_IOW` 명령 | arg는 유저 공간 포인터 → `copy_from_user()`로 읽기 |
| `_IOR` 명령 | arg는 유저 공간 포인터 → `copy_to_user()`로 쓰기 |

## switch-case 명령 분기 패턴

```c
static long led_ioctl(struct file *file, unsigned int cmd, unsigned long arg)
{
    int state, ret;

    // 파일 접근 모드 검증
    if (file->f_flags != 2)  // O_RDWR 확인
        return -EINVAL;

    switch (cmd) {
        case LED_ON:
            gpio_set_value(GPIO_LED_PIN, 1);
            led_status = 1;
            break;

        case LED_OFF:
            gpio_set_value(GPIO_LED_PIN, 0);
            led_status = 0;
            break;

        case LED_STATUS:
            state = gpio_get_value(GPIO_LED_PIN) & (1 << GPIO_LED_PIN) ? 1 : 0;
            ret = copy_to_user((int *)arg, &state, sizeof(state));
            if (ret)
                return -EFAULT;
            break;

        default:
            return -EINVAL;  // 알 수 없는 명령 → 반드시 -EINVAL 반환
    }
    return 0;
}
```

## copy_to_user / copy_from_user 선택 기준

```c
// _IOR 명령: 커널 → 유저 복사
ret = copy_to_user((void __user *)arg, &kernel_data, sizeof(kernel_data));

// _IOW 명령: 유저 → 커널 복사
ret = copy_from_user(&kernel_data, (const void __user *)arg, sizeof(kernel_data));
```

| 함수 | 방향 | ioctl 매크로 | 반환값 |
| --- | --- | --- | --- |
| `copy_to_user()` | 커널 → 유저 | `_IOR` | 복사 못 한 바이트 수 (0=성공) |
| `copy_from_user()` | 유저 → 커널 | `_IOW` | 복사 못 한 바이트 수 (0=성공) |

> ⚠️ **왜 직접 포인터 접근(`*(int *)arg = state`)이 위험한가?**
> 
> - `arg`는 유저 공간 주소. 커널이 직접 역참조하면 **page fault** 발생 가능
> - 악의적인 유저가 커널 주소를 전달하면 커널 메모리가 덮어써질 수 있음
> - `copy_to_user()` / `copy_from_user()`는 내부적으로 주소 범위 검증을 수행하므로 안전

## 파일 접근 모드 검증

### 올바른 방법: `O_ACCMODE` 마스크 사용

```c
int accmode = file->f_flags & O_ACCMODE;

switch (accmode) {
    case O_RDONLY: /* 읽기 전용 (값: 0) */ break;
    case O_WRONLY: /* 쓰기 전용 (값: 1) */ break;
    case O_RDWR:   /* 읽기+쓰기 (값: 2) */ break;
}
```

> `file->f_flags != 2` 비교는 `O_NONBLOCK` 같은 추가 플래그가 OR 결합되면 오동작한다.
`O_ACCMODE`는 하위 2비트 마스크(값: 3)로 접근 모드만 정확히 추출한다.
> 

## 공유 헤더 파일 패턴 ⭐

ioctl 명령 코드는 **커널 드라이버와 유저 애플리케이션 양쪽에서 동일한 값을 사용**해야 한다.

```c
/* led_ioctl.h — 커널/유저 공유 헤더 */
#ifndef _LED_IOCTL_H_
#define _LED_IOCTL_H_

#ifdef __KERNEL__
    #include <linux/ioctl.h>   // 커널 모드
#else
    #include <sys/ioctl.h>     // 유저 모드
    #include <stdint.h>
#endif

#define LED_IOCTL_MAGIC  'L'

#define LED_ON     _IOW(LED_IOCTL_MAGIC, 0, int)
#define LED_OFF    _IOW(LED_IOCTL_MAGIC, 1, int)
#define LED_STATUS _IOR(LED_IOCTL_MAGIC, 2, int)

// 확장 가능한 데이터 구조체
struct led_config {
    uint32_t pin_number;
    uint32_t blink_period_ms;
    uint32_t brightness;
};

#endif /* _LED_IOCTL_H_ */
```

### **핵심 설계 원칙:**

- `#ifdef __KERNEL__`로 커널/유저 환경별 include 분기
- 매직 넘버를 매크로(`LED_IOCTL_MAGIC`)로 분리하여 한 곳에서 관리
- `#ifndef ... #define ... #endif` include guard로 중복 포함 방지

### **프로젝트 디렉토리 구조:**

```
프로젝트/
├── include/
│   └── led_ioctl.h       ← 공유 헤더 (1개만 존재)
├── driver/
│   └── led_on.c          ← #include "../include/led_ioctl.h"
└── app/
    └── led_app.c         ← #include "../include/led_ioctl.h"
```

## strace로 ioctl 시스템 콜 추적

```bash
strace ./led_app 2>&1 | grep -E "open|ioctl|close"
```

### **예상 출력:**

```
openat(AT_FDCWD, "/dev/led_device", O_RDWR) = 3
ioctl(3, _IOC(_IOC_WRITE, 0x4c, 0, 0x4), 0)    = 0  # LED_ON
ioctl(3, _IOC(_IOC_READ,  0x4c, 2, 0x4), 0xbe...) = 0  # LED_STATUS
ioctl(3, _IOC(_IOC_WRITE, 0x4c, 1, 0x4), 0)    = 0  # LED_OFF
close(3) = 0
```

> `strace -e ioctl ./led_app` 으로 ioctl 호출만 필터링 가능.
> 

# CHAP 12. 커널 타이머를 이용한 LED 주기 점멸

## Linux 커널 시간 관리: jiffies

Linux 커널은 부팅 이후 경과한 시간을 **`jiffies`** 전역 카운터로 관리한다.

```c
#include <linux/jiffies.h>
extern unsigned long volatile jiffies;
```

`jiffies`는 시스템 타이머 인터럽트가 발생할 때마다 1씩 증가한다. 1초에 몇 번 증가하는지는 커널 빌드 시 결정되는 **`HZ` 상수**로 정의된다.

| HZ 값 | 타이머 인터럽트 주기 | 타이밍 정밀도 |
| --- | --- | --- |
| 100 | 10 ms | 10 ms |
| 250 | 4 ms | 4 ms |
| 1000 | 1 ms | 1 ms |

> Zynq-7000 PetaLinux 기본값: `HZ = 100` (10ms 단위)
"1초 후"를 표현하려면 `jiffies + HZ`, "500ms 후"는 `jiffies + HZ/2`
> 

### **현재 HZ 값 확인:**

```bash
zcat /proc/config.gz | grep CONFIG_HZ
```

## 시간 변환 매크로

```c
#include <linux/jiffies.h>

// 밀리초 → jiffies
unsigned long msecs_to_jiffies(const unsigned int m);

// 마이크로초 → jiffies
unsigned long usecs_to_jiffies(const unsigned int u);

// jiffies → 밀리초
unsigned int jiffies_to_msecs(const unsigned long j);

// jiffies → 마이크로초
unsigned int jiffies_to_usecs(const unsigned long j);
```

```c
// 500ms를 jiffies로 변환
unsigned long delay = msecs_to_jiffies(500);
// HZ=100일 때: 50, HZ=1000일 때: 500
```

> `HZ/2`처럼 직접 계산하는 것보다 `msecs_to_jiffies()`를 사용하는 것이 이식성 면에서 우수하다.
> 

## jiffies 오버플로와 안전한 비교

`jiffies`는 `unsigned long` 타입이므로, 32비트 시스템에서는 2^32-1 다음에 0으로 되돌아간다(wrap-around). HZ=100 기준으로 **약 497일 후** 오버플로가 발생한다.

### **잘못된 비교 (오버플로 시 오동작):**

```c
if (jiffies > timeout) { ... }  // ❌
```

### **올바른 비교 (오버플로에도 안전):**

```c
if (time_after(jiffies, timeout)) { ... }   // ✅ a가 b 이후인지
if (time_before(a, b)) { ... }              // a가 b 이전인지
if (time_after_eq(a, b)) { ... }            // a >= b
if (time_before_eq(a, b)) { ... }           // a <= b
```

> 이 매크로들은 내부적으로 부호 있는 뺄셈 `(long)(a) - (long)(b)`을 수행하여 wrap-around를 올바르게 처리한다.
> 

## timer_list API

Linux 커널의 소프트웨어 타이머는 `timer_list` 구조체로 관리된다.

```c
#include <linux/timer.h>

struct timer_list {
    struct hlist_node entry;
    unsigned long     expires;           // 만료 시점 (jiffies 값)
    void (*function)(struct timer_list *);  // 콜백 함수
    u32               flags;
};
```

### 주요 API 함수

```c
// 타이머 초기화 (커널 4.15 이상 표준)
void timer_setup(struct timer_list *timer,
                 void (*callback)(struct timer_list *),
                 unsigned int flags);

// 타이머 시작 또는 재설정
int mod_timer(struct timer_list *timer, unsigned long expires);

// 타이머 제거 (비동기 - 콜백 완료 미보장)
int del_timer(struct timer_list *timer);

// 타이머 제거 (동기 - 콜백 완전 종료 보장) ⭐
int del_timer_sync(struct timer_list *timer);
```

> ⚠️ **`module_exit()`에서는 반드시 `del_timer_sync()`를 사용해야 한다.**`del_timer()`만 호출하면 콜백이 아직 실행 중인 상태에서 모듈 메모리가 해제되어 **Kernel Panic**이 발생할 수 있다.
> 

## Self-Reloading Timer 패턴 ⭐

커널 타이머는 기본적으로 **단발(one-shot)** 타이머다. 주기적으로 반복하려면 콜백 함수 내에서 `mod_timer()`를 다시 호출하여 타이머를 재등록해야 한다.

```c
static struct timer_list blink_timer;
static int led_status = 0;

// 타이머 콜백 — softirq 컨텍스트에서 실행
static void blink_timer_callback(struct timer_list *timer)
{
    // 1) LED 상태 토글
    led_status = !led_status;
    gpio_set_value(GPIO_LED_PIN, led_status);

    // 2) 타이머 재등록 → 다음 주기에 다시 이 콜백이 호출됨
    mod_timer(&blink_timer, jiffies + BLINK_DELAY);
}

// 초기화 시
timer_setup(&blink_timer, blink_timer_callback, 0);
mod_timer(&blink_timer, jiffies + BLINK_DELAY);
```

### **실행 흐름:**

```
t=0        t=1초      t=2초      t=3초
├─mod_timer()
          ├─callback() → LED 토글 → mod_timer()
                    ├─callback() → LED 토글 → mod_timer()
                              ├─callback() → LED 토글 → mod_timer()
```

## 타이머 콜백의 실행 컨텍스트 제약 ⭐

타이머 콜백은 **softirq(소프트 인터럽트) 컨텍스트**에서 실행된다. 일반 프로세스 컨텍스트와 다른 환경이며, 엄격한 제약이 따른다.

### 허용되는 작업

| 작업 | 비고 |
| --- | --- |
| `readl()` / `writel()` (MMIO 접근) | GPIO 레지스터 읽기/쓰기 가능 |
| `mod_timer()` | Self-Reloading 패턴의 핵심 |
| `spin_lock()` / `spin_unlock()` | 바쁜 대기(busy-wait)이므로 가능 |
| `atomic_*` 연산 | 원자적 카운터 조작 가능 |
| `printk()` | 디버깅용 커널 로그 출력 |
| `wake_up_interruptible()` | Wait Queue 깨우기 가능 |
| `schedule_work()` | Workqueue에 작업 위임 가능 |

### 금지되는 작업 ❌

| 작업 | 금지 이유 |
| --- | --- |
| `msleep()`, `ssleep()`, `usleep_range()` | sleep 계열 함수는 프로세스를 스케줄링 아웃시키는데, softirq에는 프로세스 컨텍스트가 없다 |
| `mutex_lock()` | Mutex는 내부적으로 sleep을 사용한다 |
| `copy_from_user()` / `copy_to_user()` | 유저 공간 접근은 page fault를 유발할 수 있으며, 이는 sleep을 필요로 한다 |
| `kmalloc(..., GFP_KERNEL)` | GFP_KERNEL 플래그는 메모리 부족 시 sleep을 허용한다 (`GFP_ATOMIC` 사용 가능) |
| `schedule()` | 직접 스케줄러 호출은 softirq에서 금지 |

> **핵심 원칙: softirq에서는 절대 sleep하지 않는다.**
콜백 내에서 `in_softirq()`를 호출하면 `true`가 반환된다. 이 상태에서 sleep하면 커널은 `BUG: scheduling while atomic` 경고를 출력한다.
> 

### 긴 처리가 필요할 때: Workqueue로 위임

```c
static struct work_struct my_work;

static void work_handler(struct work_struct *work)
{
    // 프로세스 컨텍스트 — sleep 가능
    msleep(100);  // ✅ 허용
}

static void timer_callback(struct timer_list *timer)
{
    // softirq 컨텍스트 — sleep 불가
    schedule_work(&my_work);    // Workqueue에 위임
    mod_timer(timer, jiffies + HZ);
}
```

## del_timer vs del_timer_sync 비교

| 함수 | 콜백 실행 중일 때 동작 | 사용 시점 |
| --- | --- | --- |
| `del_timer()` | 타이머 등록만 해제. 콜백이 다른 CPU에서 실행 중일 수 있음 | ISR 등 sleep 불가 컨텍스트 |
| `del_timer_sync()` | 콜백 완료까지 대기 후 해제. 콜백이 완전히 종료된 것을 보장 | `module_exit()` 등 프로세스 컨텍스트 |

### **module_exit에서의 올바른 패턴:**

```c
static void __exit led_exit(void)
{
    del_timer_sync(&blink_timer);  // ✅ 콜백 완전 종료 보장
    gpio_set_value(GPIO_LED_PIN, 0);
    iounmap(gpio_regs);
    // ... 이후 cdev 제거 등 안전하게 진행
}
```

> ⚠️ **정리 순서도 중요하다.** `del_timer_sync()`를 가장 먼저 호출해야 한다. 타이머가 살아있는 상태에서 `iounmap()`을 먼저 호출하면, 타이머 콜백이 해제된 주소에 접근하여 Kernel Panic이 발생한다.
> 

## sysfs를 통한 런타임 점멸 주기 변경

`BLINK_DELAY`를 컴파일 시간 상수 대신 `module_param`과 sysfs로 노출하면 모듈 로드 후에도 변경할 수 있다.

```c
static int blink_period_ms = 1000;  // 기본값 1000ms
module_param(blink_period_ms, int, S_IRUGO | S_IWUSR);
MODULE_PARM_DESC(blink_period_ms, "LED blink period in milliseconds (default: 1000)");

static void blink_timer_callback(struct timer_list *timer)
{
    unsigned long delay;
    toggle_led();

    // 입력 유효성 검사
    if (blink_period_ms < 100)   blink_period_ms = 100;
    if (blink_period_ms > 10000) blink_period_ms = 10000;

    delay = msecs_to_jiffies(blink_period_ms);
    mod_timer(&blink_timer, jiffies + delay);
}
```

### **사용 방법:**

```bash
# 로드 시 파라미터 전달
insmod led_on.ko blink_period_ms=200

# 런타임 변경 (root 권한)
echo 2000 > /sys/module/led_on/parameters/blink_period_ms

# 현재 설정값 확인
cat /sys/module/led_on/parameters/blink_period_ms
```

> 변경된 값은 다음 타이머 콜백 실행 시점부터 반영된다.
> 

## Shell Script vs 커널 타이머 성능 비교

| 항목 | Shell Script (`20_ledflash.sh`) | 커널 타이머 (`25_ledflash_timer`) |
| --- | --- | --- |
| 구현 난이도 | 매우 쉬움 (bash 3줄) | 중간 (커널 모듈 작성 필요) |
| 타이밍 정밀도 | 약 ±5ms 오차 | HZ 단위 (10ms @HZ=100) |
| CPU 점유율 | 약 2~3% (sysfs 경유) | 거의 0% (직접 레지스터 접근) |
| 유저 공간 프로세스 | 필요 (bash 프로세스 상주) | 불필요 |
| 시스템 콜 횟수 | 토글당 2~3회 (open, write, close) | 0회 (커널 내부 동작) |
| 적합한 용도 | 프로토타이핑, 단순 테스트 | 제품 드라이버, 정밀 타이밍 |

## 전체 드라이버 초기화/종료 흐름 요약

```
insmod led_on.ko
    └─ led_init()
        ├─ ioremap(0xE000A000)       → gpio_regs 획득
        ├─ gpio_set_output(7)        → MIO 출력 모드
        ├─ gpio_set_value(7, 1)      → LED ON (초기 상태)
        ├─ alloc_chrdev_region()     → /dev/led_device 생성
        ├─ timer_setup()             → 콜백 등록
        └─ mod_timer(jiffies + HZ)   → 1초 후 첫 번째 콜백 예약

    [1초 후] blink_timer_callback()
        ├─ toggle_led()              → LED 상태 반전
        └─ mod_timer(jiffies + HZ)   → 다시 1초 후 콜백 예약
    (무한 반복)

rmmod led_on
    └─ led_exit()
        ├─ del_timer_sync()          → 타이머 해제 (콜백 완료 보장)
        ├─ gpio_set_value(7, 0)      → LED OFF
        ├─ iounmap(gpio_regs)        → 메모리 매핑 해제
        └─ 캐릭터 디바이스 정리
```

# CHAP 13. GPIO 인터럽트와 Blocking I/O

## 인터럽트란 무엇인가

CPU가 현재 실행 중인 코드를 일시 중단하고, 외부 하드웨어 또는 내부 이벤트에 즉시 응답하도록 하는 메커니즘이다.

**Polling vs 인터럽트 비교:**

```
[Polling 방식]
CPU: —검사—검사—검사—(이벤트!)—처리—검사—
     CPU 100% 점유, 대부분 낭비

[인터럽트 방식]
CPU: —다른작업—다른작업— | ISR | —다른작업—
                         ↑
              하드웨어 이벤트 발생 시만 CPU 점유
```

> Zynq-7000은 듀얼 코어 ARM Cortex-A9이므로, Polling으로 한 코어를 점유하면 전체 성능의 50%가 사라진다.
> 

## Zynq-7000 인터럽트 하드웨어 경로

버튼을 누르는 물리적 동작이 소프트웨어 ISR 호출까지 이어지는 경로:

```
BTN5 버튼 (MIO 51) — 전기 신호 (Rising/Falling Edge)
        ↓
GPIO 컨트롤러 (0xE000A000)
  └ INT_STAT 레지스터: 핀 변화 감지 → 인터럽트 요청 생성
        ↓
GIC (Generic Interrupt Controller)
  ├ Distributor: 인터럽트 소스 식별, 우선순위 판단, 대상 CPU 결정
  └ CPU Interface: 해당 CPU 코어에 IRQ 신호 전달
        ↓
ARM Cortex-A9 CPU 코어
  └ IRQ 예외 벡터 → Linux 커널 IRQ 핸들러 진입
        ↓
Linux 커널 인터럽트 프레임워크
  └ irq_desc[N] → 등록된 ISR(button_isr) 호출
```

### **GIC(Generic Interrupt Controller)의 3가지 역할:**

- 인터럽트 소스 식별 (고유 IRQ ID 부여)
- 우선순위 판단 (동시 발생 시 순서 결정)
- 대상 CPU 선택 (Linux 커널은 기본적으로 모든 외부 인터럽트를 CPU0에 배정)

## 인터럽트 타입 분류 (Zynq-7000 GIC)

| 타입 | GIC IRQ ID | 소유 | 주요 소스 |
| --- | --- | --- | --- |
| SGI (Software Generated Interrupt) | 0~15 | CPU별 | 소프트웨어 IPI (CPU 간 통신) |
| PPI (Private Peripheral Interrupt) | 16~31 | CPU별 | Generic Timer, Watchdog |
| SPI (Shared Peripheral Interrupt) | 32~95 | 공유 | GPIO(52), UART(59,82), ETH(54), USB(53) |

> 이 챕터의 GPIO 인터럽트(IRQ ID 52)는 **SPI**에 속한다.
Linux 커널에서 GIC IRQ ID를 직접 쓸 필요는 없다. `gpio_to_irq()`가 GPIO 핀 번호를 Linux 내부 IRQ 번호로 자동 변환해주기 때문이다.
> 

## Top Half / Bottom Half 아키텍처

Linux 커널의 인터럽트 처리는 **2단계(Two-Half)** 구조로 설계되어 있다. ISR이 실행되는 동안 **같은 인터럽트 라인이 비활성화**되며, ISR이 오래 걸리면 다른 인터럽트까지 지연되어 시스템 반응성이 떨어지기 때문이다.

### Top Half (하드 인터럽트 컨텍스트)

`request_irq()`로 등록하는 ISR 함수 자체. 하드웨어 인터럽트가 발생하면 즉시 호출된다.

**엄격히 지켜야 하는 제약:**

- `sleep()`, `msleep()`, `ssleep()` 등 슬립 함수 **호출 금지**
- `mutex_lock()` **호출 금지** (sleep 가능한 잠금이므로)
- `copy_to_user()`, `copy_from_user()` **호출 금지** (page fault 발생 가능)
- 메모리 할당 시 `GFP_ATOMIC` 플래그만 사용 가능 (`GFP_KERNEL` 금지)
- 가능한 한 **최소한의 작업만** 수행: 하드웨어 상태 확인, 플래그 설정, Bottom Half 스케줄링

### Bottom Half (지연 처리)

Top Half에서 처리하지 못한 시간이 오래 걸리는 작업을 수행한다.

| 구분 | 실행 컨텍스트 | sleep 가능 | 실행 시점 | 전형적 작업 |
| --- | --- | --- | --- | --- |
| Top Half (ISR) | 인터럽트 컨텍스트 | 불가 | 인터럽트 즉시 | 플래그 설정, wake_up |
| Bottom Half (Workqueue) | 프로세스 컨텍스트 | 가능 | 스케줄링 후 | 데이터 처리, I/O, sleep |
| Bottom Half (Tasklet) | Softirq 컨텍스트 | 불가 | Softirq 처리 시 | 경량 지연 처리 |
| Threaded IRQ | 전용 커널 스레드 | 가능 | 스케줄링 후 | 드라이버 친화적 IRQ 처리 |

> 이 챕터의 `28_buttonint` 예제는 **Top Half에서 모든 처리를 완료**하는 단순한 구조다. ISR에서 플래그를 설정하고 `wake_up_interruptible()`을 호출하는 것이 전부이므로, Top Half만으로 충분하다. LED 점등(`msleep`)과 같은 시간이 걸리는 작업은 `read()` 핸들러(프로세스 컨텍스트)에서 수행한다.
> 

## 인터럽트 컨텍스트에서 금지되는 작업 — 원인 상세

### `msleep()` / `schedule()` 호출 시 커널 패닉 발생 원리

`msleep()`은 현재 프로세스를 `TASK_INTERRUPTIBLE` 상태로 전환하고 스케줄러를 호출한다. 그런데 인터럽트 컨텍스트에는 "현재 프로세스"라는 개념이 존재하지 않는다. 인터럽트는 어떤 프로세스가 실행 중이든 관계없이 CPU를 가로채므로, sleep을 시도하면 커널이 `BUG: scheduling while atomic` 메시지와 함께 패닉을 일으킨다.

### `mutex_lock()` 호출 시 데드락 발생 원리

Mutex는 내부적으로 sleep을 사용한다. 이미 다른 곳에서 잠겨 있으면 잠금이 풀릴 때까지 기다리는데, 이때 sleep이 호출된다. 인터럽트 컨텍스트에서 mutex를 잡으려 하면 위와 같은 이유로 패닉이 발생한다.

> **인터럽트 컨텍스트에서 동기화가 필요하면 `spin_lock_irqsave()`를 사용해야 한다.**
> 

### `copy_to_user()` / `copy_from_user()` 호출 시 문제

이 함수들은 유저 공간 주소에 접근하므로 page fault가 발생할 수 있다. Page fault 처리에는 sleep이 필요하며, 인터럽트 컨텍스트에서는 불가능하다.

**인터럽트 컨텍스트에서 안전한 작업 목록:**

| 작업 | 허용 여부 |
| --- | --- |
| `readl()` / `writel()` (레지스터 접근) | ✅ OK |
| `gpio_set_value()` | ✅ OK |
| `atomic_set()` / `atomic_inc()` | ✅ OK |
| `wake_up_interruptible()` | ✅ OK |
| `queue_work()` / `schedule_work()` | ✅ OK |
| `printk()` (과도한 출력은 성능 저하) | ✅ OK |
| `spin_lock()` / `spin_unlock()` | ✅ OK |
| `ktime_get()` | ✅ OK |
| `msleep()`, `ssleep()` | ❌ 금지 |
| `mutex_lock()` | ❌ 금지 |
| `copy_to_user()` / `copy_from_user()` | ❌ 금지 |
| `kmalloc(..., GFP_KERNEL)` | ❌ 금지 (`GFP_ATOMIC` 사용) |

## GPIO 인터럽트 설정 API

### GPIO 핀 번호 계산

```
Linux GPIO 번호 = gpiochip base + MIO 핀 번호
LED (MIO 7):   905 + 7  = 912
BTN5 (MIO 51): 905 + 51 = 956
```

```bash
# 확인 방법
cat /sys/class/gpio/gpiochip905/base   # 905
cat /sys/class/gpio/gpiochip905/ngpio  # 118
```

### 설정 순서 (전체 흐름도)

```
[module_init]
├─ gpio_is_valid(LED_PIN)          — 실패 → return -ENODEV
├─ gpio_request(LED_PIN, "LED")    — 실패 → return ret
├─ gpio_direction_output(LED_PIN, 0)
├─ gpio_request(BUTTON_PIN, "Button") — 실패 → gpio_free(LED_PIN)
├─ gpio_direction_input(BUTTON_PIN)
├─ gpio_to_desc(BUTTON_PIN)        — 실패 → gpio_free(both)
├─ gpiod_set_debounce(desc, 200)   — 200μs 디바운스
├─ gpio_to_irq(BUTTON_PIN)         → button_irq
├─ request_irq(button_irq, ...)    — 실패 → gpio_free(both)
└─ register_chrdev(...)

[module_exit]
├─ free_irq(button_irq, NULL)
├─ gpio_free(LED_PIN)
├─ gpio_free(BUTTON_PIN)
└─ unregister_chrdev(dev_major, ...)
```

### 1. `gpio_to_irq()` — GPIO → IRQ 번호 변환

```c
static int button_irq;
button_irq = gpio_to_irq(BUTTON_PIN);
```

`gpio_to_irq()`는 내부적으로 GPIO 컨트롤러 드라이버(`zynq_gpio`)에게 해당 핀의 IRQ 번호를 질의한다. 반환되는 IRQ 번호는 GIC의 하드웨어 IRQ ID가 아니라 **Linux 커널의 가상 IRQ 번호**다.

### 2. `request_irq()` — 인터럽트 핸들러 등록 ⭐

```c
#include <linux/interrupt.h>

int request_irq(unsigned int irq,
                irq_handler_t handler,
                unsigned long flags,
                const char *name,
                void *dev_id);
```

| 인자 | 타입 | 설명 |
| --- | --- | --- |
| `irq` | `unsigned int` | `gpio_to_irq()`가 반환한 IRQ 번호 |
| `handler` | `irq_handler_t` | ISR 함수 포인터 |
| `flags` | `unsigned long` | 트리거 방식, 공유 여부 등 플래그 조합 |
| `name` | `const char *` | `/proc/interrupts`에 표시되는 이름 |
| `dev_id` | `void *` | ISR에 전달되는 드라이버 고유 데이터 (비공유 IRQ에서는 NULL 가능) |

**트리거 플래그:**

| 플래그 | 의미 | 사용 사례 |
| --- | --- | --- |
| `IRQF_TRIGGER_RISING` | 신호가 LOW→HIGH로 변할 때 | 버튼 누름 감지 (Active-High) |
| `IRQF_TRIGGER_FALLING` | 신호가 HIGH→LOW로 변할 때 | 버튼 누름 감지 (Active-Low) |
| `IRQF_TRIGGER_BOTH` | 양쪽 에지 모두 | 상태 변화 감지 |
| `IRQF_TRIGGER_HIGH` | HIGH 레벨 유지 시 | 레벨 트리거 주변장치 |
| `IRQF_TRIGGER_LOW` | LOW 레벨 유지 시 | 레벨 트리거 주변장치 |
| `IRQF_SHARED` | 여러 드라이버가 같은 IRQ 공유 | PCI 장치 등 |

```c
// 28_buttonint 예제에서의 실제 호출
ret = request_irq(button_irq,
                  button_isr,          // ISR 함수 포인터
                  IRQF_TRIGGER_RISING, // 버튼 누름 = Rising Edge
                  "button_irq",        // /proc/interrupts 표시명
                  NULL);               // 비공유이므로 NULL
```

> **왜 `IRQF_TRIGGER_RISING`을 선택하는가?**
Zybo 보드의 BTN5는 기본적으로 풀다운 상태에서 버튼을 누르면 HIGH로 전환되는 구조다. 따라서 "버튼이 눌리는 순간"을 감지하려면 Rising Edge를 사용한다. `IRQF_TRIGGER_FALLING`을 사용하면 "버튼에서 손을 떼는 순간"을 감지하게 된다.
> 

### 3. `free_irq()` — 인터럽트 해제

```c
free_irq(button_irq, NULL);  // request_irq()의 dev_id와 동일한 값 전달
```

> 모듈이 제거될 때(`module_exit`) 반드시 해제해야 한다. 해제하지 않으면 모듈은 언로드되지만 ISR 함수의 메모리는 해제되어 있으므로, 다음 인터럽트 발생 시 커널 패닉이 발생한다.
> 

### 4. 디바운스(Debounce) 설정

물리적 기계식 버튼은 접점이 닫히거나 열릴 때 수 밀리초 동안 신호가 불안정하게 요동친다(Bouncing). 한 번의 버튼 누름이 수십 번의 인터럽트를 발생시킬 수 있다.

```c
#include <linux/gpio/consumer.h>

struct gpio_desc *desc;
desc = gpio_to_desc(BUTTON_PIN);
if (desc) {
    gpiod_set_debounce(desc, 200);  // 200μs 디바운스 설정
}
```

> `gpiod_set_debounce()`의 두 번째 인자는 **마이크로초(μs)** 단위다. 200μs는 대부분의 택트 스위치에 적합하지만, 노이즈가 심한 환경에서는 1000~5000μs까지 늘릴 수 있다.
> 

## irqreturn_t 반환값

ISR 함수는 반드시 `irqreturn_t` 타입을 반환해야 한다.

```c
enum irqreturn {
    IRQ_NONE         = (0 << 0),  // 이 핸들러가 처리하지 않은 인터럽트
    IRQ_HANDLED      = (1 << 0),  // 이 핸들러가 처리한 인터럽트
    IRQ_WAKE_THREAD  = (1 << 1),  // Threaded IRQ의 스레드 핸들러 깨우기
};
```

- **`IRQ_HANDLED`** — 가장 일반적인 반환값. "이 인터럽트는 내 드라이버가 담당하는 것이 맞고, 정상적으로 처리했다"는 의미. `28_buttonint` 예제에서는 항상 이 값을 반환한다.
- **`IRQ_NONE`** — "이 인터럽트는 내 드라이버가 처리할 것이 아니다"라는 의미. 주로 **공유 인터럽트(`IRQF_SHARED`)** 환경에서 사용한다. 하나의 IRQ 라인에 여러 드라이버가 핸들러를 등록한 경우, 커널은 모든 핸들러를 순차 호출하며, 각 핸들러는 자기 장치의 상태 레지스터를 확인하여 자기 장치가 발생시킨 인터럽트인지 판별한 뒤 아니면 `IRQ_NONE`을 반환한다.

## Blocking I/O: Wait Queue

### Blocking I/O의 개념

"버튼이 눌릴 때까지 대기"하는 시나리오에서 두 가지 선택이 있다:

**선택 A — Non-blocking (즉시 반환):**

```c
// 유저 앱 — Polling 방식 (CPU 낭비)
while (1) {
    ret = read(fd, buf, 1);
    if (ret > 0) break;   // 데이터 도착
    usleep(10000);        // 10ms 대기 후 재시도
}
```

**선택 B — Blocking (대기 후 반환):**

```c
// 유저 앱 — Blocking 방식 (CPU 0% 사용)
ret = read(fd, buf, 1);   // 버튼이 눌릴 때까지 여기서 멈춤
// 여기에 도달하면 버튼이 눌린 것
```

Blocking I/O가 훨씬 효율적이다. 프로세스는 슬립 상태에서 CPU를 전혀 소비하지 않으며, 커널 스케줄러가 다른 프로세스에게 CPU를 배정한다. 이 메커니즘을 구현하는 것이 **Wait Queue**다.

### Wait Queue 선언

```c
#include <linux/wait.h>

// 정적 선언 (전역 변수로 선언할 때) — 권장
static DECLARE_WAIT_QUEUE_HEAD(wait_queue);

// 동적 선언 (구조체 멤버로 사용할 때)
wait_queue_head_t my_queue;
init_waitqueue_head(&my_queue);
```

### `wait_event_interruptible()` — 조건 대기 ⭐

`read()` 핸들러 내부에서 프로세스를 슬립시키는 API다.

```c
int wait_event_interruptible(wait_queue_head_t wq, int condition);
```

| 인자 | 설명 |
| --- | --- |
| `wq` | Wait Queue 변수 (포인터가 아니라 변수 자체를 전달) |
| `condition` | C 표현식 — 이 값이 **참(non-zero)이면 즉시 반환**, 거짓이면 슬립 |

**반환값:**

- `0` — 조건이 만족되어 정상 반환
- `ERESTARTSYS` — 시그널에 의해 인터럽트됨 (유저가 Ctrl+C 누름 등)

**동작 원리 단계별:**

```
유저 앱: read(fd, buf, 1) 호출
        ↓
커널: dev_read() 진입
        ↓
wait_event_interruptible(wait_queue, button_pressed == 1)
        ↓
button_pressed == 1 ? YES → 즉시 반환 (0)
                      NO  → 현재 프로세스를 TASK_INTERRUPTIBLE 상태로 전환
                            프로세스를 wait_queue에 등록
                            schedule() 호출 → CPU 양보 (슬립)
                            [다른 프로세스 실행 중...]
                            [버튼 누름!] → ISR 실행:
                                button_pressed = 1
                                wake_up_interruptible(&wait_queue)
                            프로세스 깨어남 → 조건 재확인
                            button_pressed == 1 ? YES → 반환 (0)
        ↓
button_pressed = 0;   // 플래그 리셋 (다음 대기를 위해)
        ↓
LED 켜기 → 1초 대기 → LED 끄기
        ↓
return 0;   // read() 반환 → 유저 앱에 알림
```

### `_interruptible` 접미사의 의미

`wait_event_interruptible()`에서 슬립 중인 프로세스는 시그널(signal)에 의해 깨어날 수 있다. 예를 들어 유저가 Ctrl+C를 누르면 SIGINT 신호가 발생하고, 슬립 중인 프로세스가 깨어나며 함수는 `-ERESTARTSYS`를 반환한다.

```c
// 시그널 처리 패턴 (권장)
if (wait_event_interruptible(wait_queue, button_pressed == 1))
    return -ERESTARTSYS;  // 시그널 수신 → 시스템 콜 재시작
```

> 시그널을 무시하고 조건이 만족될 때까지 절대 반환하지 않는 `wait_event()` 버전도 존재하지만, 유저 경험이 나빠지므로(Ctrl+C가 안 먹힘) 거의 사용하지 않는다.
> 

### `wake_up_interruptible()` — 대기 프로세스 깨우기

ISR에서 호출하여 Wait Queue에서 슬립 중인 프로세스를 깨운다.

```c
void wake_up_interruptible(wait_queue_head_t *wq);
```

이 함수가 호출되면 다음이 순서대로 발생한다:

1. Wait Queue에 등록된 프로세스 중 `TASK_INTERRUPTIBLE` 상태인 것을 찾는다
2. 해당 프로세스의 상태를 `TASK_RUNNING`으로 변경한다
3. 스케줄러의 실행 큐(runqueue)에 추가한다
4. 프로세스가 스케줄링되면 `wait_event_interruptible()`이 조건을 다시 확인한다
5. 조건이 참이면 함수가 반환되고, 거짓이면 다시 슬립한다

> `wake_up_interruptible()` 자체는 ISR 내에서 즉시 반환된다. 대기 중인 프로세스가 실제로 실행되는 것은 스케줄러가 선택한 이후다. 따라서 ISR에서 `wake_up` 호출은 매우 빠르게 완료되며, Top Half의 시간 제약을 위반하지 않는다.
> 

### `wake_up()` 변형 비교

| 함수 | 깨우는 대상 | 사용 시나리오 |
| --- | --- | --- |
| `wake_up()` | `TASK_INTERRUPTIBLE` + `TASK_UNINTERRUPTIBLE` 모두 | 조건 변수 스타일 |
| `wake_up_interruptible()` | `TASK_INTERRUPTIBLE`만 | 가장 일반적, 시그널 대응 가능 |
| `wake_up_all()` | 대기 중인 모든 프로세스 (INTERRUPTIBLE + UNINTERRUPTIBLE) | 브로드캐스트 |
| `wake_up_interruptible_all()` | 대기 중인 모든 INTERRUPTIBLE 프로세스 | 여러 reader 깨우기 |

### Spurious Wakeup과 조건 재확인

`wait_event_interruptible()` 매크로는 내부 구현상 조건을 **두 번** 확인한다 — 슬립 전 한 번, 깨어난 후 한 번. 이것은 **Spurious Wakeup(허위 깨어남)** 문제를 방지하기 위한 것이다.

Spurious Wakeup이 발생하는 상황:

- 멀티코어 시스템에서 race condition으로 인해 조건이 잠시 참이었다가 바로 거짓으로 돌아감
- 시그널 수신으로 깨어남 (`ERESTARTSYS` 처리)
- Linux 커널 내부의 최적화로 인한 조건 없는 wakeup

`wait_event_interruptible()` 매크로는 내부적으로 `while` 루프로 구현되어 있어, 깨어난 후 조건을 다시 확인하고 거짓이면 다시 슬립한다.

## 28_buttonint 예제 전체 코드 구조

### 코드를 7개의 블록으로 분석

**1. 핀 번호 정의**

```c
#define LED_PIN    912  // MIO 7 → Zybo LD4
#define BUTTON_PIN 956  // MIO 51 → Zybo BTN5
```

gpiochip base(905) + MIO 핀 번호로 계산. 보드와 PetaLinux 버전에 따라 다를 수 있으므로, 실습 전에 반드시 `cat /sys/class/gpio/gpiochip*/base`로 확인해야 한다.

**2. 전역 변수**

```c
static int dev_major;
static int button_irq;
static int button_pressed = 0;  // ISR ↔ read() 공유 플래그
```

`button_pressed`가 ISR과 `read()` 사이에서 공유되는 플래그 변수다. 단일 CPU, 단일 reader 환경에서는 일반 `int`로도 안전하지만, 멀티코어 환경이나 복수 접근 시에는 `atomic_t`가 더 안전하다.

**3. Wait Queue 선언**

```c
static DECLARE_WAIT_QUEUE_HEAD(wait_queue);
```

`read()` 핸들러에서 대기하고, ISR에서 깨운다.

**4. ISR (Interrupt Service Routine) — Top Half**

```c
static irqreturn_t button_isr(int irq, void *dev_id)
{
    button_pressed = 1;                        // 플래그 설정
    printk(KERN_INFO "Button pressed!\n");     // 디버깅 로그
    wake_up_interruptible(&wait_queue);        // 대기 중인 프로세스 깨우기

    return IRQ_HANDLED;
}
```

3줄로 구성된 최소 ISR. 모든 작업이 인터럽트 컨텍스트에서 허용되는 것들뿐이다.

**5. file_operations 핸들러**

```c
static ssize_t dev_read(struct file *filep, char *buffer,
                        size_t len, loff_t *offset)
{
    // 버튼이 눌릴 때까지 Blocking
    wait_event_interruptible(wait_queue, button_pressed == 1);

    // 플래그 리셋 (다음 대기를 위해)
    button_pressed = 0;

    // LED 1초 점등 — 프로세스 컨텍스트이므로 msleep 허용
    gpio_set_value(LED_PIN, 1);
    msleep(1000);
    gpio_set_value(LED_PIN, 0);

    return 0;
}
```

`dev_read()`가 핵심이다. `wait_event_interruptible()`로 Blocking하고, 깨어난 후 LED를 제어한다. **`msleep(1000)`은 ISR이 아니라 `read()` 시스템 콜의 컨텍스트에서 실행된다는 점에 주의하라.**

**6. module_init**

리소스 확보 순서: GPIO → 디바운스 → IRQ → chrdev. 에러 발생 시 확보 역순으로 해제한다.

**7. module_exit**

모든 리소스를 해제한다. `free_irq()`를 가장 먼저 호출하여 ISR이 더 이상 호출되지 않게 한 후, GPIO와 chrdev를 해제한다.

### register_chrdev() vs alloc_chrdev_region() + cdev_add()

| 항목 | `register_chrdev()` | `alloc_chrdev_region()` + `cdev_add()` |
| --- | --- | --- |
| /dev 노드 자동 생성 | 안 됨 (mknod 수동 필요) | `class_create` + `device_create`로 자동 |
| Minor 번호 제어 | 0~255 전체 점유 | 필요한 수만 할당 |
| 코드량 | 1줄 | 5~6줄 |
| 적합 용도 | 간단한 학습/프로토타입 | 실제 드라이버 |

이 예제에서 `register_chrdev()`를 사용하는 이유는 인터럽트와 Blocking I/O 학습에 집중하기 위해서다. `/dev` 노드를 수동으로 생성해야 한다:

```bash
# Major 번호 확인
dmesg | grep "major number"
# 예: Blocking IO Driver initialized with major number 243

# /dev 노드 수동 생성
sudo mknod /dev/blocking_io_device c 243 0
sudo chmod 666 /dev/blocking_io_device
```

## Zybo 보드 BTN4/BTN5 풀업 저항 이슈

`28_buttonint` 예제를 실행하기 전에 반드시 알아야 할 Zybo 보드 특이 사항이다. BTN4와 BTN5는 PS GPIO(MIO) 핀에 연결되어 있는데, Vivado 기본 설정에서 **내부 풀업 저항이 활성화**되어 있다. 이 상태에서는 버튼을 눌러도 GPIO 입력 값이 항상 HIGH로 고정되어 신호 변화가 감지되지 않는다.

**해결 방법: `devmem` 으로 풀업 비활성화**

풀업 저항 제어 레지스터는 Zynq-7000의 SLCR(System Level Control Registers) 영역에 있다:

| MIO 핀 | SLCR 레지스터 주소 | 풀업 비트 |
| --- | --- | --- |
| MIO 50 (BTN4) | `0xF80007C8` | bit[12:11] |
| MIO 51 (BTN5) | `0xF80007CC` | bit[12:11] |

`bit[12:11] = 0b11`이면 풀업 활성화, `0b00`이면 비활성화.

```bash
# 풀업 비트(bit 12:11) 클리어
sudo devmem 0xF80007C8 w $(($(sudo devmem 0xF80007C8) & ~0x3000))
sudo devmem 0xF80007CC w $(($(sudo devmem 0xF80007CC) & ~0x3000))

# 변경 확인 (0x00000200으로 바뀌면 정상)
sudo devmem 0xF80007C8
sudo devmem 0xF80007CC
```

> ⚠️ **이 설정은 재부팅하면 초기화된다.** 영구적으로 해결하려면 Vivado에서 MIO 핀 설정을 수정하고 새로 bitstream을 생성해야 한다.
> 

**GPIO 상태 확인:**

```bash
sudo cat /sys/kernel/debug/gpio
# 정상 출력 예:
# gpio-956 ( |Button) in hi IRQ
# gpio-912 ( |LED   ) out lo
```

`gpio-956 (BTN5)`이 `in` 방향이고 `IRQ` 표시가 있으면 인터럽트가 정상 등록된 것이다.

## /proc/interrupts로 인터럽트 통계 확인

```bash
cat /proc/interrupts | grep button
#  XX:   3   0   zynq_gpio   XX Edge   button_irq
#         ↑   ↑               ↑         ↑
#  CPU0처리횟수 CPU1처리횟수  컨트롤러  등록한 이름
```

| 필드 | 의미 |
| --- | --- |
| `XX:` | Linux IRQ 번호 |
| `3` | CPU0에서 처리한 인터럽트 횟수 |
| `0` | CPU1에서 처리한 횟수 (0이면 CPU0만 처리) |
| `zynq_gpio` | 인터럽트 컨트롤러 이름 |
| `Edge` | 에지 트리거 방식 |
| `button_irq` | `request_irq()`에서 등록한 이름 |

버튼을 누를 때마다 발생 횟수가 증가하는지 확인하라. 디바운스가 제대로 동작하면 한 번 누름에 1회만 증가한다.

## CPU 사용률 비교: Polling vs Blocking

```bash
# Polling 방식 (CPU ~99%)
while true; do cat /sys/class/gpio/gpio956/value; done &
top  # CPU 사용률 ~99%

# Blocking 방식 (CPU 0%)
./blocking_app &
top  # CPU 사용률 0.0%
```

Blocking I/O가 임베디드 시스템에서 필수적인 이유를 수치로 확인할 수 있다.

## 심화 개선 포인트

### 1. 시그널 처리 개선

```c
// 현재 코드 (시그널 무시)
wait_event_interruptible(wait_queue, button_pressed == 1);

// 개선 버전 (시그널 처리)
if (wait_event_interruptible(wait_queue, button_pressed == 1))
    return -ERESTARTSYS;  // Ctrl+C 등 시그널 수신 → 즉시 에러 반환
```

### 2. `button_pressed` Race Condition 방지

```c
// 현재 코드 (SMP 환경에서 race condition 가능)
static int button_pressed = 0;

// 안전한 버전 (atomic 연산 사용)
static atomic_t button_pressed = ATOMIC_INIT(0);

// ISR에서
atomic_set(&button_pressed, 1);
wake_up_interruptible(&wait_queue);

// read()에서
wait_event_interruptible(wait_queue, atomic_read(&button_pressed) == 1);
atomic_set(&button_pressed, 0);
```

### 3. read()에서 LED 제어를 분리하는 설계 개선

현재 `dev_read()` 안에 `msleep(1000)`이 있어 `read()` 호출이 최소 1초 이상 걸린다. 더 좋은 설계는 `read()`에서는 이벤트 발생만 알리고, LED 제어는 별도의 `write()` 핸들러 또는 유저 앱에서 수행하는 것이다.

```c
// 개선된 read() — 이벤트 알림만
static ssize_t dev_read(struct file *filep, char *buffer,
                        size_t len, loff_t *offset)
{
    char status = '1';

    if (wait_event_interruptible(wait_queue, button_pressed == 1))
        return -ERESTARTSYS;

    button_pressed = 0;

    // 유저에게 이벤트 발생을 1바이트로 알림
    if (copy_to_user(buffer, &status, 1))
        return -EFAULT;

    return 1;
}
```