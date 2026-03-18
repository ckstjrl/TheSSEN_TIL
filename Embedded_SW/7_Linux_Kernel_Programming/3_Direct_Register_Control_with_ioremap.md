# 임베디드 리눅스 커널 프로그래밍 day03

날짜: 2026년 3월 18일

# ioremap을 이용한 레지스터 직접 제어

## Linux 메모리 모델

### 세 가지 주소 공간 (32-bit ARM 기준)

| 주소 종류 | 범위 | 접근 주체 |
| --- | --- | --- |
| 물리 주소 (Physical Address) | 0x00000000 ~ 0xFFFFFFFF | 하드웨어 버스 |
| 커널 가상 주소 (Kernel Virtual Address) | 0xC0000000 ~ 0xFFFFFFFF | 커널 코드 |
| 사용자 가상 주소 (User Virtual Address) | 0x00000000 ~ 0xBFFFFFFF | 유저 프로세스 |

### 핵심 개념

- **MMIO (Memory-Mapped I/O)**: ARM은 모든 주변장치가 MMIO 방식 → 하드웨어 제어 = 메모리 주소 읽기/쓰기
- **물리 주소를 직접 사용할 수 없는 이유**: Linux가 MMU를 활성화한 이후 CPU가 발행하는 모든 주소는 가상 주소. 물리 주소를 직접 포인터로 사용하면 **page fault → Kernel Panic**
- **해결책**: `ioremap()`으로 물리 주소를 커널 가상 주소로 매핑

## ioremap() / iounmap() API

```c
#include <linux/io.h>

// 매핑
void __iomem *ioremap(phys_addr_t phys_addr, size_t size);

// 해제 (module_exit에서 반드시 호출)
void iounmap(volatile void __iomem *addr);

// 자동 해제 버전 (Platform Driver에서 사용)
void __iomem *devm_ioremap(struct device *dev, resource_size_t offset, resource_size_t size);
```

### 반환값 NULL 체크 — 필수 패턴

```c
gpio_regs = ioremap(GPIO_BASE, GPIO_REG_SIZE);
if (!gpio_regs) {
    pr_err("Failed to map GPIO registers\n");
    return -ENOMEM;
}
```

### readl() / writel() — 안전한 MMIO 접근 (실무 권장)

```c
u32 readl(const volatile void __iomem *addr);   // 32비트 읽기
void writel(u32 value, volatile void __iomem *addr); // 32비트 쓰기
```

- **메모리 배리어** 자동 삽입 → CPU out-of-order 실행 방지
- **volatile** 시맨틱 → 컴파일러 최적화로 접근이 생략되지 않음
- 학습 예제에서는 포인터 직접 접근을 쓰지만, **실무에서는 readl/writel 사용**

## Zynq-7000 GPIO 레지스터 맵

### GPIO Bank 구조

| Bank | 핀 범위 | 연결 |
| --- | --- | --- |
| Bank 0 | MIO[0:31] | PS MIO (이번 예제: MIO7 = LED LD4) |
| Bank 1 | MIO[32:53] | PS MIO |
| Bank 2 | EMIO[0:31] | PL EMIO (FPGA) |
| Bank 3 | EMIO[32:63] | PL EMIO (FPGA) |

### Bank 0 핵심 레지스터

| 레지스터 | 오프셋 (Hex) | uint32_t 인덱스 | 설명 |
| --- | --- | --- | --- |
| MASK_DATA_0_LSW | 0x000 | 0 | 마스크 기반 데이터 쓰기 (하위 16비트) |
| DATA_0 | 0x040 | 16 | 출력 데이터 (읽기/쓰기) |
| DIRM_0 | 0x204 | 129 | 방향 모드 (1=출력) |
| OEN_0 | 0x208 | 130 | 출력 활성화 (1=활성) |

### LED 출력 제어 순서 — **DIRM + OEN 둘 다 1이어야 동작**

```
1단계: DIRM_0[7] = 1  →  출력 방향 설정
2단계: OEN_0[7]  = 1  →  출력 드라이버 활성화
3단계: DATA_0[7] = 1  →  LED ON  /  = 0  →  LED OFF
```

### 비트 조작 패턴 (Read-Modify-Write)

```c
// 특정 비트 SET (다른 비트 유지)
register_value |= (1 << bit_number);

// 특정 비트 CLEAR (다른 비트 유지)
register_value &= ~(1 << bit_number);
```

> ⚠️ **Race Condition 주의**: 인터럽트 핸들러와 일반 코드가 같은 레지스터에 동시 Read-Modify-Write 시 문제 발생 가능 (챕터 15에서 다룸)
> 

## 캐릭터 디바이스 등록 구조

### 디바이스 번호: Major + Minor

```c
dev_t dev_num;
int major = MAJOR(dev_num);
int minor = MINOR(dev_num);
dev_num = MKDEV(major, minor);

// 동적 할당 (현대 드라이버 표준)
int ret = alloc_chrdev_region(&dev_num, 0, 1, "led_device");
```

### 등록/해제 5단계 — 반드시 역순으로 해제

| 단계 | 등록 (init) | 해제 (exit) |
| --- | --- | --- |
| ① | `ioremap()` | `iounmap()` |
| ② | `alloc_chrdev_region()` | `unregister_chrdev_region()` |
| ③ | `cdev_init()` + `cdev_add()` | `cdev_del()` |
| ④ | `class_create()` | `class_destroy()` |
| ⑤ | `device_create()` | `device_destroy()` |

### goto를 이용한 에러 처리 패턴 (커널 표준)

```c
gpio_regs = ioremap(GPIO_BASE, GPIO_REG_SIZE);
if (!gpio_regs) return -ENOMEM;

ret = alloc_chrdev_region(&dev_num, 0, 1, DEVICE_NAME);
if (ret < 0) goto err_ioremap;

// ... cdev, class, device ...

err_cdev:    cdev_del(&led_cdev);
err_chrdev:  unregister_chrdev_region(dev_num, 1);
err_ioremap: iounmap(gpio_regs);
             return ret;
```

c.f ) 27번 예제와 26번 예제의 차이

```bash
# 27번 LED ON, OFF
// LED 켜기 함수
 26 void led_on(int fd) {
 27     if (write(fd, "1", 1) < 0) {
 28         perror("LED 켜기 실패");
 29         exit(EXIT_FAILURE);
 30     }
 31     printf("LED가 켜졌습니다.\n");
 32 }
 33 
 34 // LED 끄기 함수
 35 void led_off(int fd) {
 36     if (write(fd, "0", 1) < 0) {
 37         perror("LED 끄기 실패");
 38         exit(EXIT_FAILURE);
 39     }
 40     printf("LED가 꺼졌습니다.\n");
 41 }

# 26번 LED ON, OFF
/* LED 켜기 */
 51     if (ioctl(fd, LED_ON_CMD) < 0) {
 52         perror("ioctl LED_ON_CMD");
 53         close(fd);
 54         return EXIT_FAILURE;
 55     }
 56     printf("LED ON 명령 전송\n");
 57     sleep(2);
 58 
 59     /* LED 끄기 */
 60     if (ioctl(fd, LED_OFF_CMD) < 0) {
 61         perror("ioctl LED_OFF_CMD");
 62         close(fd);
 63         return EXIT_FAILURE;
 64     }
 65     printf("LED OFF 명령 전송\n");
 66     sleep(1);
```

`ioctl` 을 사용해서 더 직관적이다.

## read() / write() 핸들러

### copy_from_user() / copy_to_user() — 커널-유저 데이터 복사

```c
#include <linux/uaccess.h>

// 유저 → 커널 (write 핸들러에서 사용)
unsigned long copy_from_user(void *to, const void __user *from, unsigned long n);

// 커널 → 유저 (read 핸들러에서 사용)
unsigned long copy_to_user(void __user *to, const void *from, unsigned long n);
// 반환값: 복사 실패한 바이트 수 (0이면 성공)
```

> ⚠️ 직접 포인터 역참조(`*(char __user *)buf = val`) 절대 금지 → 커널 메모리 훼손 위험
> 

### offset (f_pos) 처리 핵심

- `cat /dev/led_device`는 내부적으로 `read()`를 반복 호출
- 첫 번째 호출: 데이터 반환 후 `offset`을 데이터 크기만큼 전진
- 두 번째 호출: `offset > 0`이므로 0 반환 → EOF → cat 종료
- **`offset` 처리 누락 시 cat이 무한 출력**

## ioremap 매핑 크기 주의사항

| 크기 | 포함 레지스터 | 안전성 |
| --- | --- | --- |
| 0xB4 (180 bytes) | DATA, MASK_DATA만 | DIRM/OEN 접근 불가 → **Kernel Panic** |
| 0x2E8 (744 bytes) | DATA + DIRM + OEN + INT 관련 | 충분 |
| 0x300 (768 bytes) | — | 권장 |

> **핵심**: 매핑 크기는 반드시 `접근하려는 최대 오프셋 + 4바이트` 이상이어야 함
> 

## 자주 발생하는 오류

| 증상 | 원인 | 해결 방법 |
| --- | --- | --- |
| `insmod` 시 Invalid module format | 커널 버전 불일치 | `uname -r` 로 버전 확인 |
| ioremap 후 LED 무반응 | DIRM/OEN 미설정 | `gpio_set_output()` + `gpio_set_enable()` 호출 |
| ioremap 후 LED 무반응 | GPIO 클럭 미공급 | sysfs export 사전 실행 |
| `echo 1 > /dev/led_device` Permission denied | 권한 부족 | sudo 또는 udev 규칙 추가 |
| `cat /dev/led_device` 무한 출력 | `*offset` 처리 누락 | `read()` 핸들러에 `*offset > 0` 체크 추가 |
| Kernel Oops at address 0xXXXX | ioremap 크기 부족 | 매핑 크기를 0x300 이상으로 증가 |
| `/dev/led_device` 미생성 | 초기화 5단계 미완료 | `class_create` / `device_create` 누락 확인 |

# 캐릭터 디바이스 드라이버 완성 — file_operations 전체 구현

## 챕터 9 vs 챕터 10 핵심 차이

| 항목 | 챕터 9 (24_ledon_ioremap) | 챕터 10 (27_ledon) |
| --- | --- | --- |
| GPIO 제어 방식 | `ioremap()` + 레지스터 직접 조작 | **GPIO subsystem API** |
| 레지스터 주소 의존 | 0xE000A000 하드 코딩 | 커널이 내부적으로 처리 |
| 이식성 | Zynq-7000 전용 | GPIO 번호만 바꾸면 다른 SoC에서도 동작 |
| 유저 앱 | 없음 (echo/cat 사용) | 전용 유저 애플리케이션 포함 |

## file_operations 구조체

```c
// <linux/fs.h>
static struct file_operations fops = {
    .owner   = THIS_MODULE,   // 모듈 참조 카운트 자동 관리
    .open    = led_open,
    .release = led_release,
    .read    = led_read,
    .write   = led_write,
};
```

### 주요 멤버 역할

| 멤버 | 대응 시스템 콜 | 역할 |
| --- | --- | --- |
| `.owner` | — | 모듈 참조 카운트 관리 (`THIS_MODULE`) |
| `.open` | `open()` | 디바이스 파일 열기 |
| `.release` | `close()` | 디바이스 파일 닫기 (마지막 참조 시) |
| `.read` | `read()` | 디바이스 → 유저 데이터 전달 |
| `.write` | `write()` | 유저 → 디바이스 데이터 전달 |
| `.unlocked_ioctl` | `ioctl()` | 커스텀 제어 명령 (챕터 11) |
| `.poll` | `poll()/select()` | I/O 다중화 (챕터 16) |
| `.mmap` | `mmap()` | 메모리 매핑 (챕터 20) |

### .owner = THIS_MODULE의 역할

- 유저 앱이 `/dev/led_device`를 **열고 있는 동안** `rmmod` 시도 → 커널이 거부 (`Module is in use`)
- `.owner` 미설정 시 열린 파일에 대해 `read()/write()` 호출 시 이미 해제된 메모리 접근 → **Kernel Panic**

### C99 Designated Initializer 문법

- 명시적으로 초기화하지 않은 멤버는 자동으로 `NULL`로 설정
- 구조체 멤버 순서에 의존하지 않아 커널 버전 간 호환성이 좋음
- `NULL`인 함수 포인터에 대응하는 시스템 콜은 커널이 기본 동작 수행 또는 에러 반환

## open() 핸들러

```c
static int led_open(struct inode *inode, struct file *file) {
    printk(KERN_INFO "LED Device Opened\n");
    return 0;  // 성공
}
```

### inode vs file 구조체

| 구조체 | 대응 | 설명 |
| --- | --- | --- |
| `struct inode` | 파일 자체 | 디스크에 존재하는 파일 메타데이터. 하나의 파일에 하나의 inode |
| `struct file` | 열린 인스턴스 | 같은 파일을 여러 프로세스가 열면 각각 별도의 file 구조체 생성 |

### file->private_data 패턴 (다중 디바이스 지원)

```c
// open()에서 인스턴스별 데이터를 private_data에 저장
struct my_device *dev = container_of(inode->i_cdev, struct my_device, cdev);
file->private_data = dev;

// read()/write()에서 복원
struct my_device *dev = file->private_data;
```

## release() 핸들러

```c
static int led_release(struct inode *inode, struct file *file) {
    printk(KERN_INFO "LED Device Closed\n");
    return 0;
}
```

### release() vs close() 차이

- `release()`는 해당 inode에 대한 **마지막 `close()`** 시에만 호출됨
- `dup(fd)`, `fork()` 등으로 공유된 fd가 모두 닫혀야 `release()` 1회 호출
- 실제 프로덕션 드라이버에서 `release()`가 수행할 작업: `private_data` 메모리 해제, 잠금 해제, DMA 채널 반환, 하드웨어 안전 상태 복귀

## read() 핸들러

```c
static ssize_t led_read(struct file *file, char __user *buf,
                         size_t len, loff_t *offset) {
    char kbuf[2];
    kbuf[0] = led_status + '0';  // 0 또는 1 → '0' 또는 '1'
    kbuf[1] = '\n';

    if (*offset > 0) return 0;  // ① EOF 처리

    if (copy_to_user(buf, kbuf, 2)) return -EFAULT;  // ② 복사

    *offset = 2;  // ③ 오프셋 갱신
    return 2;     // ④ 읽은 바이트 수 반환
}
```

### read() 반환값

| 반환값 | 의미 |
| --- | --- |
| 양수 n | n 바이트 성공적으로 읽음 |
| 0 | EOF, 더 이상 읽을 데이터 없음 |
| `-EFAULT` | 유저 버퍼 주소 오류 |
| `-EINVAL` | 잘못된 인자 |

## write() 핸들러

```c
static ssize_t led_write(struct file *file, const char __user *buf,
                          size_t len, loff_t *offset) {
    char kbuf[10];
    if (len > sizeof(kbuf) - 1) return -EINVAL;  // ① 입력 크기 검증

    if (copy_from_user(kbuf, buf, len)) return -EFAULT;  // ② 유저→커널 복사

    kbuf[len] = '\0';  // ③ 널 종단

    if (kbuf[0] == '1') {          // ④ 입력 파싱
        gpio_set_value(GPIO_LED_PIN, 1);
        led_status = 1;
    } else if (kbuf[0] == '0') {
        gpio_set_value(GPIO_LED_PIN, 0);
        led_status = 0;
    } else {
        return -EINVAL;
    }

    return len;  // ⑤ 처리한 바이트 수 반환
}
```

> ⚠️ `write()`가 0을 반환하면 일부 유저 앱은 무한 재시도에 빠질 수 있음 → 에러 코드 반환 권장
> 

## GPIO Subsystem API (챕터 10 방식)

### Legacy API vs New API

| 항목 | Legacy (`gpio_*`) | New (`gpiod_*`) |
| --- | --- | --- |
| 헤더 | `<linux/gpio.h>` | `<linux/gpio/consumer.h>` |
| 핀 식별 | 정수 번호 (예: 912) | descriptor 구조체 |
| 상태 | Deprecated (여전히 동작) | **권장** |

### GPIO API 호출 순서

```c
#include <linux/gpio.h>

// ① 핀 소유권 요청
ret = gpio_request(GPIO_LED_PIN, "LED");

// ② 출력 방향 설정 + 초기값 지정
gpio_direction_output(GPIO_LED_PIN, 0);  // 출력, 초기값 LOW

// ③ 값 변경
gpio_set_value(GPIO_LED_PIN, 1);  // LED ON
gpio_set_value(GPIO_LED_PIN, 0);  // LED OFF

// ④ 핀 해제 (모듈 종료 시 반드시)
gpio_free(GPIO_LED_PIN);
```

### ioremap 방식 vs GPIO API 방식 내부 동작 비교

```
gpio_request(912, "LED")        → 핀 충돌 검사 + 소유권 등록
gpio_direction_output(912, 0)   → DIRM[7]=1, OEN[7]=1, DATA[7]=0
gpio_set_value(912, 1)          → DATA[7]=1
gpio_free(912)                  → 소유권 해제
```

결국 같은 하드웨어 레지스터를 조작하지만, GPIO API는 `zynq_gpio` 커널 드라이버를 통해 간접적으로 접근 → 드라이버 코드에서 물리 주소를 알 필요 없음

## 캐릭터 디바이스 등록 — 전체 흐름 (챕터 10 버전)

### 초기화 순서 (GPIO subsystem 사용)

```c
static int __init led_init(void) {
    // 1단계: 하드웨어 초기화 (GPIO 요청 및 설정)
    ret = gpio_request(GPIO_LED_PIN, "LED");
    gpio_direction_output(GPIO_LED_PIN, 0);

    // 2단계: 디바이스 번호 할당
    ret = alloc_chrdev_region(&dev_num, 0, 1, DEVICE_NAME);

    // 3단계: cdev 초기화 및 등록
    cdev_init(&led_cdev, &fops);
    ret = cdev_add(&led_cdev, dev_num, 1);

    // 4단계: 디바이스 클래스 생성
    led_class = class_create(THIS_MODULE, DEVICE_NAME);

    // 5단계: /dev 노드 생성
    device_create(led_class, NULL, dev_num, NULL, DEVICE_NAME);
    return 0;
}
```

### 해제 순서 (등록의 역순)

```c
static void __exit led_exit(void) {
    gpio_set_value(GPIO_LED_PIN, 0);   // 하드웨어 안전 상태
    gpio_free(GPIO_LED_PIN);           // ← 1단계의 역

    device_destroy(led_class, dev_num); // ← 5단계의 역
    class_destroy(led_class);           // ← 4단계의 역
    cdev_del(&led_cdev);               // ← 3단계의 역
    unregister_chrdev_region(dev_num, 1); // ← 2단계의 역
}
```

## 유저 앱 ↔ 드라이버 상호작용 흐름

```
[유저 앱]                    [커널/VFS]               [드라이버]
fd = open("/dev/led_device") → inode 검색, file 할당   → led_open()
write(fd, "1", 1)            → fops.write()            → led_write() → gpio_set_value(912, 1) → LED ON
read(fd, buf, 10)            → fops.read()             → led_read()  → copy_to_user("1\n", 2)
close(fd)                    → fops.release()           → led_release()
```

## echo/cat vs 유저 앱 동작 차이

| 항목 | echo/cat | 유저 앱 |
| --- | --- | --- |
| open/close 횟수 | 매 호출마다 open → read/write → close 반복 | 한 번 open 후 여러 번 read/write, 마지막에 close |
| 단일 open 제한 드라이버 | 동시성 테스트 어려움 | 적합 |
| ioctl, poll 등 고급 시스템 콜 | 사용 불가 | 사용 가능 |

## 핵심 개념 요약

```
file_operations 구조체
  - 캐릭터 디바이스 드라이버의 핵심 인터페이스
  - .owner = THIS_MODULE 로 모듈 참조 카운트 자동 관리
  - C99 Designated Initializer로 필요한 핸들러만 선택적 등록

read() / write() 공통 원칙
  - 유저 포인터를 절대 직접 역참조하지 않는다
  - copy_to_user() / copy_from_user() 를 반드시 사용
  - *offset 갱신으로 EOF를 올바르게 처리
  - 반환값은 처리한 바이트 수, 음수는 에러 코드

GPIO Subsystem API 순서
  gpio_request() → gpio_direction_output() → gpio_set_value() → gpio_free()

캐릭터 디바이스 등록 패턴
  alloc_chrdev_region() → cdev_init() → cdev_add() → class_create() → device_create()
  해제는 등록의 역순, goto 패턴으로 에러 시 이전 단계를 역순 정리
```