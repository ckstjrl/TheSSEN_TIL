# 임베디드 리눅스 커널 프로그래밍 day07

날짜: 2026년 3월 24일

사용하고 싶은 기능이 있다면

1. rootfs에서 찾아보기
2. 없다면 yocto에서 레시피 찾아보기
3. 여기도 없다면 source 파일 받아서 cross-complie 진행

# CHAP 20. mmap — 커널-유저 메모리 직접 매핑

> **대응 예제:** `35_mmap`
> 

## `mmap()` 시스템 콜 개요

### 왜 mmap이 필요한가

기존 `read()` / `write()` 방식은 데이터가 **두 번 복사**된다는 근본적인 한계가 있다.

```
[write() 흐름]
유저 앱 → copy_from_user(kernel_buf, user_buf) ← ① 유저→커널 복사
        → writel(kernel_buf, hw_register)        ← ② 커널→하드웨어

[read() 흐름]
하드웨어 → val = readl(hw_register)              ← ① 하드웨어→커널
         → copy_to_user(user_buf, kernel_buf)    ← ② 커널→유저 복사
```

**대용량 데이터에서 이 오버헤드가 심각해지는 경우:**

| 상황 | 규모 |
| --- | --- |
| 비디오 프레임버퍼 | 1920×1080, 32bpp → 초당 30프레임 시 매초 240MB 복사 |
| 대용량 센서 데이터 | 고속 ADC 연속 수집 수 MB 단위 |
| DMA 전송 버퍼 | 유저 앱에서 직접 읽어야 하는 경우 |
| 하드웨어 레지스터 직접 접근 | 성능 카운터, 디버그 레지스터 폴링 |

`mmap()`은 커널 메모리(또는 디바이스의 물리 주소 공간)를 유저 프로세스의 가상 주소 공간에 **직접 매핑**하여, **복사 없이 포인터 접근만으로** 데이터를 공유한다 → **Zero Copy**.

### mmap의 동작 원리 — 페이지 테이블 매핑

`mmap()`의 핵심은 **페이지 테이블(Page Table) 조작**이다.

ARM Cortex-A9(Zybo)의 MMU는 가상 주소를 물리 주소로 변환하며, 이 변환 정보가 페이지 테이블에 저장된다.

**`mmap()` 시스템 콜 단계별 흐름:**

1. **유저 프로세스가 `mmap()` 호출** → 커널에 "이 파일(디바이스)의 내용을 내 주소 공간에 매핑해 달라"고 요청
2. **커널이 VMA(Virtual Memory Area) 생성** → `vm_area_struct` 구조체 생성
3. **드라이버의 `.mmap` 핸들러 호출** → `file_operations.mmap` 호출
4. **`remap_pfn_range()` 실행** → 물리 페이지 프레임 번호(PFN)를 유저 가상 주소에 매핑하도록 페이지 테이블 엔트리 설정
5. **유저 프로세스로 복귀** → `mmap()`이 매핑된 유저 가상 주소를 반환

이후 유저 프로세스가 반환된 주소에 접근하면, MMU가 페이지 테이블을 참조하여 해당 물리 메모리에 직접 접근한다. **커널 코드를 거치지 않으므로 시스템 콜 오버헤드도 없다.**

### mmap의 대표적 활용 사례

| 활용 분야 | 설명 | 커널 드라이버 예 |
| --- | --- | --- |
| 비디오 프레임버퍼 | 화면 출력 버퍼를 유저 앱이 직접 쓰기 | `fbdev`, DRM/KMS |
| V4L2 카메라 버퍼 | 카메라 캡처 프레임을 복사 없이 접근 | `videobuf2` |
| DMA 결과 버퍼 | DMA 전송 완료 데이터를 유저 앱에서 읽기 | ALSA `snd_pcm_mmap` |
| 하드웨어 레지스터 접근 | 성능 카운터, 디버그 레지스터 폴링 | UIO(`/dev/uioN`) |
| IPC 공유 메모리 | 프로세스 간 대용량 데이터 공유 | `shm_open` + `mmap` |
| 파일 I/O 최적화 | 대용량 파일 읽기/쓰기 가속 | 일반 파일의 `mmap` |

> **임베디드 시스템에서 특히 중요한 것은 UIO(Userspace I/O) 패턴이다.**
> 
> 
> Zynq-7000의 PL에 구현한 커스텀 IP의 레지스터를 유저 공간에서 직접 제어하려면, 해당 물리 주소를 `mmap()`으로 매핑하는 것이 가장 효율적이다.
> 

## `vm_area_struct (vma)` 구조체

드라이버의 `.mmap` 핸들러가 호출될 때, 커널은 이미 생성된 `vm_area_struct` 구조체의 포인터를 전달한다.

이 구조체는 **유저 프로세스의 가상 메모리 영역 하나**를 기술한다.

### 주요 필드 분석

```c
struct vm_area_struct {
    unsigned long vm_start;     /* 매핑 시작 가상 주소 (유저 공간) */
    unsigned long vm_end;       /* 매핑 끝 가상 주소 (배타적: 실제 범위는 vm_start ~ vm_end-1) */
    unsigned long vm_pgoff;     /* 파일/디바이스 내 오프셋 (PAGE_SIZE 단위) */
    pgprot_t vm_page_prot;      /* 페이지 보호 속성 (읽기/쓰기/실행 권한) */
    unsigned long vm_flags;     /* VM 플래그: VM_READ, VM_WRITE, VM_SHARED 등 */
    struct file *vm_file;       /* 매핑된 파일 구조체 포인터 */
    /* ... 기타 필드 생략 ... */
};
```

**유저 앱의 `mmap()` 호출과 대응:**

```c
map = mmap(NULL,               /* addr:   커널이 vm_start를 결정 */
           4096,               /* length: vm_end - vm_start = 4096 */
           PROT_READ|PROT_WRITE, /* prot: vm_page_prot에 반영 */
           MAP_SHARED,         /* flags:  vm_flags에 VM_SHARED 설정 */
           fd,                 /* fd:     vm_file에 연결 */
           0);                 /* offset: vm_pgoff = 0 / PAGE_SIZE = 0 */
```

### 매핑 크기 계산과 검증

드라이버에서 **반드시** 수행해야 하는 첫 번째 작업은 요청된 매핑 크기가 드라이버가 제공할 수 있는 범위를 초과하지 않는지 검증하는 것이다.

```c
static int device_mmap(struct file *filp, struct vm_area_struct *vma)
{
    unsigned long size = vma->vm_end - vma->vm_start;

    /* 드라이버가 할당한 버퍼보다 큰 매핑을 요청하면 거부 */
    if (size > MMAP_SIZE) {
        printk(KERN_ERR "mmap: requested size %lu exceeds buffer size %lu\n",
               size, (unsigned long)MMAP_SIZE);
        return -EINVAL;
    }
    /* ... 매핑 진행 ... */
}
```

> ⚠️ **이 검증을 생략하면, 유저 프로세스가 드라이버 버퍼 뒤에 있는 관련 없는 커널 메모리까지 접근할 수 있다. 이는 심각한 보안 취약점이자 커널 크래시의 원인이 된다.**
> 

### `vm_pgoff`와 오프셋 처리

`vm_pgoff`는 유저가 `mmap()`의 마지막 인자 `offset`으로 전달한 값을 `PAGE_SIZE`로 나눈 것이다.

이 값은 "디바이스 메모리의 어느 지점부터 매핑할 것인지"를 지정한다.

> **주의:** `offset`은 반드시 페이지 크기의 정수배여야 한다.
> 
> 
> 올바른 값: `0` 또는 `PAGE_SIZE`(4096)의 배수
> 

### `vm_page_prot` — 페이지 보호 속성

`vm_page_prot`는 페이지 테이블 엔트리에 설정할 하드웨어 보호 비트를 담고 있다.

**디바이스 레지스터를 매핑할 때는 캐시를 비활성화해야 한다:**

```c
/* 디바이스 레지스터 매핑 시 캐시 비활성화 */
vma->vm_page_prot = pgprot_noncached(vma->vm_page_prot);
```

> RAM 버퍼 매핑 시에는 기본 `vm_page_prot`를 그대로 사용하면 된다. CPU 캐시가 정상 동작하므로 성능이 좋다.
> 

## `remap_pfn_range()` 상세

`remap_pfn_range()`는 드라이버의 `.mmap` 핸들러에서 호출하는 핵심 함수로, **물리 메모리 페이지를 유저 가상 주소 공간에 매핑하는 페이지 테이블 엔트리를 생성한다.**

### 함수 시그니처와 인자

```c
int remap_pfn_range(struct vm_area_struct *vma,
                    unsigned long addr,   /* 매핑 시작 유저 가상 주소 */
                    unsigned long pfn,    /* 물리 페이지 프레임 번호 */
                    unsigned long size,   /* 매핑 크기 (바이트) */
                    pgprot_t prot);       /* 페이지 보호 속성 */
```

| 인자 | 설명 | 전형적인 값 |
| --- | --- | --- |
| `vma` | 커널이 전달한 VMA 구조체 | 그대로 전달 |
| `addr` | 매핑할 유저 가상 주소 시작점 | `vma->vm_start` |
| `pfn` | 물리 페이지 프레임 번호 | `virt_to_phys(buf) >> PAGE_SHIFT` |
| `size` | 매핑할 바이트 수 | `vma->vm_end - vma->vm_start` |
| `prot` | 페이지 보호 속성 | `vma->vm_page_prot` |

**반환값:** 성공 시 `0`, 실패 시 음수 에러 코드

### PFN(Page Frame Number) 변환

PFN은 물리 주소를 `PAGE_SIZE`(4096바이트, 즉 12비트)로 나눈 값이다.

```c
/* 커널 가상 주소 → 물리 주소 → PFN 변환 */
void *kernel_buf = kmalloc(PAGE_SIZE, GFP_KERNEL);

unsigned long phys_addr = virt_to_phys(kernel_buf);
/* 예: phys_addr = 0x0F800000 */

unsigned long pfn = phys_addr >> PAGE_SHIFT;
/* PAGE_SHIFT = 12 (ARM 기준) */
/* pfn = 0x0F800000 >> 12 = 0x0F800 */
```

> ⚠️ **`virt_to_phys()`는 `kmalloc()`, `kzalloc()`, `get_zeroed_page()` 등으로 할당한 물리적 연속 메모리에서만 정확한 결과를 보장한다. `vmalloc()`으로 할당한 메모리는 물리적으로 비연속이므로 `virt_to_phys()`가 올바른 결과를 반환하지 않는다.**
> 

### 물리 주소를 직접 사용하는 경우

디바이스 레지스터처럼 이미 물리 주소를 알고 있는 경우에는 `virt_to_phys()` 변환이 필요 없다:

```c
/* Zynq-7000 GPIO 레지스터를 유저 공간에 직접 매핑하는 예 */
#define GPIO_BASE_PHYS  0xE000A000
#define GPIO_MAP_SIZE   PAGE_SIZE

static int gpio_mmap(struct file *filp, struct vm_area_struct *vma)
{
    unsigned long pfn = GPIO_BASE_PHYS >> PAGE_SHIFT;

    /* I/O 레지스터는 캐시 비활성화 필수 */
    vma->vm_page_prot = pgprot_noncached(vma->vm_page_prot);

    if (remap_pfn_range(vma, vma->vm_start, pfn,
                        vma->vm_end - vma->vm_start,
                        vma->vm_page_prot)) {
        return -EAGAIN;
    }
    return 0;
}
```

> 이 패턴은 UIO(Userspace I/O) 드라이버의 핵심이며, Zynq PL의 AXI 주변장치를 유저 공간에서 직접 제어할 때 자주 사용한다.
> 

### `remap_pfn_range()` 실패 원인

| 실패 상황 | 에러 코드 | 원인 |
| --- | --- | --- |
| 잘못된 PFN | `-EINVAL` | 물리 주소 범위를 벗어남 |
| 매핑 크기 불일치 | `-EINVAL` | `size`가 페이지 정렬되지 않음 |
| 이미 매핑된 영역 | `-EAGAIN` | 페이지 테이블 충돌 |
| 메모리 부족 | `-ENOMEM` | 페이지 테이블 할당 실패 |

## mmap에 적합한 메모리 할당 방식

드라이버에서 유저 공간에 매핑할 버퍼를 할당할 때, 할당 방식에 따라 `remap_pfn_range()` 사용 가능 여부가 달라진다.

### 할당 방식별 비교

| 할당 함수 | 물리 연속성 | 페이지 정렬 | `remap_pfn_range()` | 주요 용도 |
| --- | --- | --- | --- | --- |
| `kmalloc(PAGE_SIZE, GFP_KERNEL)` | 연속 | 보장 안 됨* | 사용 가능 (주의) | 소규모 버퍼 |
| `kzalloc(PAGE_SIZE, GFP_KERNEL)` | 연속 | 보장 안 됨* | 사용 가능 (주의) | 소규모 버퍼 (0 초기화) |
| `get_zeroed_page(GFP_KERNEL)` | 연속 | **보장** | **권장** | 단일 페이지 버퍼 |
| `__get_free_pages(GFP_KERNEL, order)` | 연속 | **보장** | **권장** | 다중 페이지 버퍼 |
| `vmalloc()` | **비연속** | 페이지별 연속 | **사용 불가** | 대용량 버퍼 (직접 매핑 부적합) |
| `dma_alloc_coherent()` | 연속 | 보장 | **최적** | DMA + mmap 조합 |

> *`kmalloc()` / `kzalloc()`은 내부적으로 slab allocator를 사용하므로 반환된 주소가 페이지 경계에 정렬되지 않을 수 있다. `PAGE_SIZE` 이상을 요청하면 buddy allocator가 사용되어 페이지 정렬이 보장된다.
> 

### `vmalloc()`이 부적합한 이유

`vmalloc()`은 물리적으로 비연속인 페이지들을 커널 가상 주소 공간에서 연속으로 보이게 매핑한다.

`remap_pfn_range()`는 **연속된 물리 페이지**를 한 번에 매핑하므로, `vmalloc()` 메모리에 적용하면 **첫 페이지만 올바르게 매핑되고 나머지는 엉뚱한 물리 메모리를 가리킨다.**

### `dma_alloc_coherent()`와 mmap 조합

챕터 19의 DMA 버퍼를 유저 공간에 매핑하는 것은 매우 일반적인 패턴이다:

```c
/* DMA 버퍼 할당 (물리 연속 + 캐시 일관성 보장) */
void *dma_buf;
dma_addr_t dma_handle;
dma_buf = dma_alloc_coherent(dev, BUF_SIZE, &dma_handle, GFP_KERNEL);

/* mmap 핸들러에서 DMA 버퍼 매핑 */
static int my_mmap(struct file *filp, struct vm_area_struct *vma)
{
    unsigned long pfn = dma_handle >> PAGE_SHIFT;

    /* DMA coherent 메모리는 이미 캐시 비활성화 상태 */
    vma->vm_page_prot = pgprot_noncached(vma->vm_page_prot);

    return remap_pfn_range(vma, vma->vm_start, pfn,
                           vma->vm_end - vma->vm_start,
                           vma->vm_page_prot);
}
```

> **참고:** 최신 커널에서는 `dma_mmap_coherent()` 헬퍼 함수를 제공하여 DMA 버퍼 매핑을 더 간결하게 처리할 수 있다.
> 

## 유저 애플리케이션에서 mmap 사용

### `mmap()` 시스템 콜 시그니처

```c
#include <sys/mman.h>

void *mmap(void *addr,       /* 희망 매핑 주소 (NULL이면 커널이 결정) */
           size_t length,    /* 매핑 크기 (바이트) */
           int prot,         /* 접근 권한 */
           int flags,        /* 매핑 플래그 */
           int fd,           /* 디바이스 파일 디스크립터 */
           off_t offset);    /* 디바이스 내 오프셋 (PAGE_SIZE 배수 필수) */
```

**반환값:** 성공 시 매핑된 주소 포인터, 실패 시 `MAP_FAILED` (`(void *)-1`)

### `prot` 인자 — 접근 권한

| 플래그 | 의미 |
| --- | --- |
| `PROT_READ` | 읽기 허용 |
| `PROT_WRITE` | 쓰기 허용 |
| `PROT_EXEC` | 실행 허용 (드라이버 mmap에서는 거의 사용 안 함) |
| `PROT_NONE` | 접근 불가 (가드 페이지용) |

> 디바이스 드라이버 mmap에서는 일반적으로 `PROT_READ | PROT_WRITE`를 사용한다.
> 

### `flags` 인자 — `MAP_SHARED` vs `MAP_PRIVATE`

| 플래그 | 동작 | 드라이버 mmap에서 |
| --- | --- | --- |
| `MAP_SHARED` | 쓰기 내용이 원본(커널 버퍼)에 **즉시 반영** | **거의 항상 이것을 사용** |
| `MAP_PRIVATE` | 쓰기 시 Copy-on-Write 발생, 원본에 반영 안 됨 | 드라이버 mmap에서 거의 사용 안 함 |

### `offset` 인자 — 반드시 페이지 정렬

`offset` 값은 `PAGE_SIZE`(4096)의 정수배여야 한다. 그렇지 않으면 `mmap()`은 `EINVAL`로 실패한다.

```c
/* 올바른 호출 */
mmap(NULL, 4096, PROT_READ|PROT_WRITE, MAP_SHARED, fd, 0);     /* offset=0  ✓ */
mmap(NULL, 4096, PROT_READ|PROT_WRITE, MAP_SHARED, fd, 4096);  /* offset=4096 ✓ */

/* 잘못된 호출 */
mmap(NULL, 4096, PROT_READ|PROT_WRITE, MAP_SHARED, fd, 100);   /* offset=100 ✗ */
```

### `munmap()` — 매핑 해제

```c
int munmap(void *addr, size_t length);
```

`mmap()`으로 얻은 주소와 동일한 크기로 호출해야 한다.

`munmap()` 호출 후 해당 주소에 접근하면 `SIGSEGV`(Segmentation Fault)가 발생한다.

### `msync()` — 쓰기 동기화

`MAP_SHARED`로 매핑한 경우, CPU 캐시와 실제 메모리 사이의 동기화를 보장하려면 `msync()`를 사용한다.

```c
int msync(void *addr, size_t length, int flags);
/* flags: MS_SYNC (동기, 완료까지 블록) 또는 MS_ASYNC (비동기) */
```

> RAM 버퍼 mmap에서는 CPU 캐시 일관성이 자동 보장되므로 `msync()`가 필수는 아니다.
> 
> 
> 하드웨어 레지스터나 DMA 버퍼 매핑에서는 명시적 동기화가 필요할 수 있다.
> 

## `35_mmap` 예제 전체 분석

### 드라이버 소스 (`mmap_driver.c`)

```c
#define DEVICE_NAME "mmap_device"
#define MMAP_SIZE PAGE_SIZE          /* 4096바이트 = 1페이지 */

static int major;
static char *mmap_buffer;            /* mmap으로 공유할 커널 버퍼 */

/* mmap 핸들러 (핵심) */
static int device_mmap(struct file *filp, struct vm_area_struct *vma)
{
    /* ① 커널 가상 주소 → 물리 주소 → PFN 변환 */
    unsigned long pfn = virt_to_phys(mmap_buffer) >> PAGE_SHIFT;

    /* ② 요청된 매핑 크기 계산 */
    unsigned long size = vma->vm_end - vma->vm_start;

    /* ③ 크기 검증: 드라이버 버퍼를 초과하면 거부 */
    if (size > MMAP_SIZE) {
        printk(KERN_ERR "mmap_device: Requested size is too large\n");
        return -EINVAL;
    }

    /* ④ 페이지 테이블에 매핑 설정 */
    if (remap_pfn_range(vma, vma->vm_start, pfn, size, vma->vm_page_prot)) {
        printk(KERN_ERR "mmap_device: remap_pfn_range failed\n");
        return -EAGAIN;
    }

    printk(KERN_INFO "mmap_device: Memory mapped\n");
    return 0;
}

static struct file_operations fops = {
    .open    = device_open,
    .release = device_release,
    .mmap    = device_mmap,         /* .mmap 핸들러 등록 */
};
```

### 드라이버 실행 흐름

```
[insmod mmap_driver.ko]
  └─ mmap_device_init()
     ├─ register_chrdev() → major=243 (예시)
     └─ kmalloc(4096) → mmap_buffer = 0xC8001000 (예시)
           └─ 물리 주소: virt_to_phys(0xC8001000) = 0x08001000

[유저 앱: mmap(NULL, 4096, PROT_READ|PROT_WRITE, MAP_SHARED, fd, 0)]
  ├─ 커널: VMA 생성 (vm_start=0x40040000, vm_end=0x40041000)
  ├─ device_mmap(filp, vma) 호출
  │  ├─ pfn = 0x08001000 >> 12 = 0x08001
  │  ├─ size = 0x40041000 - 0x40040000 = 4096 ✓
  │  └─ remap_pfn_range(vma, 0x40040000, 0x08001, 4096, prot)
  │        └─ 페이지 테이블: 0x40040000 → 0x08001000 매핑 설정
  └─ mmap() 반환: map = 0x40040000

[유저 앱: map[0] = 'A']
  ├─ CPU: 가상 0x40040000 → MMU → 물리 0x08001000 에 'A' 기록
  └─ 커널의 mmap_buffer[0]도 'A'가 됨 (같은 물리 페이지)
```

### 유저 앱 소스 (`mmap_app.c`)

```c
#define DEVICE "/dev/mmap_device"
#define MMAP_SIZE 4096

int main()
{
    int fd;
    char *map;

    /* ① 디바이스 파일 열기 */
    fd = open(DEVICE, O_RDWR);

    /* ② 커널 버퍼를 유저 주소 공간에 매핑 */
    map = mmap(NULL, MMAP_SIZE, PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0);

    /* ③ 매핑된 메모리에 값 쓰기 – 시스템 콜 없이 직접 접근 */
    map[0] = 'A';

    /* ④ 매핑 해제 */
    munmap(map, MMAP_SIZE);

    /* ⑤ 디바이스 닫기 – 드라이버의 release에서 mmap_buffer[0] 출력 */
    close(fd);

    return EXIT_SUCCESS;
}
```

### 원본 예제의 버그 분석 및 수정

원본 코드의 `mmap()` 호출에서 `offset` 인자로 `100`을 전달하고 있다.

```c
/* 원본 코드 (버그) */
map = mmap(NULL, MMAP_SIZE, PROT_READ | PROT_WRITE, MAP_SHARED, fd, 100);
/* ↑ 문제: PAGE_SIZE(4096)의 배수가 아님 → EINVAL 반환 */

/* 수정된 코드 */
map = mmap(NULL, MMAP_SIZE, PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0);
/* ↑ PAGE_SIZE의 배수 (0은 항상 유효) */
```

## 크로스 빌드 및 Zybo 보드 실습

### Zybo 보드에서 실행 순서

```bash
# — 1단계: 모듈 로드 —
insmod mmap_driver.ko
dmesg | tail -3
# mmap_device: Registered with major number 243

# — 2단계: 디바이스 노드 생성 —
mknod /dev/mmap_device c 243 0

# — 3단계: 유저 앱 실행 —
./mmap_app
# Memory mapped at address 0x40040000
# Wrote 'A' to mapped memory

# — 4단계: 커널 로그 확인 —
dmesg | tail -5
# mmap_device: Device opened
# mmap_device: Memory mapped
# mmap_buffer[0]=A    ← 유저가 쓴 'A'가 커널에 반영됨!
# mmap_device: Device closed

# — 5단계: 모듈 제거 —
rmmod mmap_driver
```

### 핵심 확인 포인트

`dmesg`에서 `mmap_buffer[0]=A` 메시지가 출력되었다는 것은 다음을 증명한다:

1. 유저 앱이 `map[0] = 'A'`로 쓴 값이 **시스템 콜 없이** 커널 버퍼에 반영되었다
2. `remap_pfn_range()`로 설정한 페이지 테이블 매핑이 정상 동작한다
3. `MAP_SHARED` 플래그 덕분에 유저 쓰기가 원본 물리 페이지에 직접 반영되었다

## 개선된 mmap 드라이버 설계

### `/dev` 노드 자동 생성

`class_create()` / `device_create()`를 사용하면 `insmod` 시 `/dev/mmap_device`가 자동 생성된다.

### `read()` 핸들러 추가

mmap과 `read()`를 모두 지원하면, 디버깅이나 단순 접근 시 `read()`를 사용하고 성능이 필요한 경우 `mmap`을 사용하는 유연한 설계가 가능하다.

### 초기 데이터 패턴으로 매핑 검증

모듈 초기화 시 버퍼에 알려진 패턴을 기록하면, 유저 앱에서 mmap 후 패턴을 읽어 매핑 정확성을 검증할 수 있다.

### 양방향 데이터 교환 실습

mmap의 진정한 가치는 **양방향 제로 카피 통신**이다:

1. 커널이 `mmap_buffer`에 `"KERNEL_HELLO"` 기록 (초기화 시)
2. 유저가 mmap으로 `"KERNEL_HELLO"` 읽기 → 화면 출력
3. 유저가 `"USER_REPLY"` 기록
4. `close()` 시 커널이 `mmap_buffer`에서 `"USER_REPLY"` 읽기 → dmesg 출력

이 전체 과정에서 `copy_to_user()`, `copy_from_user()` 호출은 **한 번도 발생하지 않는다.**

## 하드웨어 레지스터를 mmap으로 직접 접근하는 확장 실습

Zynq-7000 PS GPIO 레지스터를 유저 공간에서 직접 제어하는 UIO 패턴:

```c
#define GPIO_BASE_PHYS  0xE000A000
#define DATA_OFFSET     0x040   /* Data Output Register */
#define DIRM_OFFSET     0x204   /* Direction Mode (1=output) */
#define OEN_OFFSET      0x208   /* Output Enable */
#define MIO7_BIT        (1 << 7)

/* 유저 앱에서 LED 제어 */
volatile uint32_t *gpio = mmap(NULL, GPIO_MAP_SIZE,
                                PROT_READ | PROT_WRITE, MAP_SHARED, fd, 0);

gpio[DIRM_OFFSET / 4] |= MIO7_BIT;   /* MIO7를 출력으로 설정 */
gpio[OEN_OFFSET / 4]  |= MIO7_BIT;   /* MIO7 출력 활성화 */

/* LED 점멸 (시스템 콜 없이 직접 레지스터 접근) */
gpio[DATA_OFFSET / 4] |= MIO7_BIT;   /* LED ON */
usleep(500000);
gpio[DATA_OFFSET / 4] &= ~MIO7_BIT;  /* LED OFF */
```

> ⚠️ **보안 경고:** 하드웨어 레지스터를 유저 공간에 직접 노출하면, 유저 앱의 버그가 시스템 전체를 크래시시킬 수 있다. 프로덕션 환경에서는 반드시 접근 권한을 제한하고, 매핑 범위를 최소화해야 한다.
> 

## mmap 디버깅 기법

### `/proc/PID/maps`로 매핑 영역 확인

```bash
cat /proc/$(pidof mmap_app)/maps
# 40040000-40041000 rw-s 00000000 00:06 54321  /dev/mmap_device  ← mmap 영역
```

### `/proc/iomem`으로 물리 메모리 매핑 확인

```bash
cat /proc/iomem | grep -i gpio
# e000a000-e000afff : e000a000.gpio   ← GPIO 레지스터 물리 주소 확인
```

### `strace`로 mmap 시스템 콜 추적

```bash
strace -e mmap,munmap,open,close ./mmap_app
# mmap(NULL, 4096, PROT_READ|PROT_WRITE, MAP_SHARED, 3, 0) = 0x40040000
# munmap(0x40040000, 4096) = 0
```

### 자주 발생하는 오류와 해결법

| 증상 | 원인 | 해결 방법 |
| --- | --- | --- |
| `mmap: Invalid argument` | `offset`이 `PAGE_SIZE` 배수가 아님 | `offset`을 0 또는 4096의 배수로 변경 |
| `mmap: Permission denied` | `/dev` 노드 권한 부족 | `chmod 666 /dev/mmap_device` 또는 udev 규칙 |
| Segfault 발생 (유저 앱) | `munmap()` 후 포인터 접근 | 포인터를 NULL로 초기화 |
| 커널 Oops/Panic | 매핑 크기가 할당 버퍼 초과 | `device_mmap()`에서 크기 검증 강화 |
| 쓰기 값이 커널에 미반영 | `MAP_PRIVATE` 사용 | `MAP_SHARED`로 변경 |
| I/O 레지스터 읽기값 불일치 | 캐시 미비활성화 | `pgprot_noncached()` 적용 |
| `remap_pfn_range` 실패 | `vmalloc()` 메모리 사용 | `kmalloc()` 또는 `get_zeroed_page()` 사용 |

## 성능 비교: `read()/write()` vs `mmap()`

| 방식 | 시스템 콜 횟수 | 데이터 복사 횟수 | 컨텍스트 스위치 |
| --- | --- | --- | --- |
| `read()/write()` | 40,000회 | 40,000회 | 40,000회 |
| `mmap()` | **2회** (mmap+munmap) | **0회** | **2회** |

> Zybo 보드(ARM Cortex-A9 @ 667MHz)에서 시스템 콜 한 번의 오버헤드는 약 1~5μs이다.
> 
> 
> 4만 회 반복하면 40~200ms의 순수 오버헤드가 발생한다. mmap 방식에서는 이 오버헤드가 거의 0이다.
> 

# CHAP 21. 디바이스 트리 기초와 문법

## 디바이스 트리의 탄생 배경

### ARM Linux의 board file 시대

2011년 이전의 ARM Linux 커널에는 심각한 구조적 문제가 있었다.

각 보드의 하드웨어 정보 — UART 베이스 주소, 인터럽트 번호, GPIO 핀 배치, 메모리 맵 — 가 커널 소스코드 내부에 **C 코드로 하드코딩**되어 있었다. 이를 **board file** 방식이라 한다.

**board file 방식의 문제점:**

1. **커널 소스 오염** - 새 보드가 출시될 때마다 `arch/arm/mach-*/` 디렉토리에 보드별 C 파일이 추가됨. 수천 개의 board file이 커널 트리에 쌓였다.
2. **동일 커널 바이너리의 재사용 불가** - 같은 SoC를 사용하더라도 보드마다 핀 배치와 외부 주변장치가 다르므로, 보드 A의 커널 이미지를 보드 B에서 부팅할 수 없었다.
3. **유지보수 비용** - 보드 하드웨어가 변경되면 커널 소스를 수정하고 전체를 다시 빌드해야 했다.

### 해결책: 하드웨어 정보를 커널에서 분리

ARM 커뮤니티는 PowerPC와 SPARC 아키텍처가 이미 사용하던 **Device Tree**라는 데이터 구조를 도입하기로 결정했다.

> 하드웨어를 "설명"하는 데이터를 커널 바이너리에서 완전히 분리하여,
> 
> 
> **별도의 바이너리 파일(DTB)**로 부트로더가 커널에 전달한다.
> 

**이로써 가능해진 것:**

- **동일 커널 바이너리를 여러 보드에서 재사용** - DTB 파일만 보드에 맞게 교체하면 된다.
- **커널 소스 수정 없이 하드웨어 변경 반영** - DTS 파일을 수정하고 DTB를 재컴파일하면 끝이다.
- **부팅 시 동적 하드웨어 구성** - 부트로더(U-Boot)가 DTB를 메모리에 로드하고 커널에 그 주소를 알려준다.

### 부팅 과정에서 Device Tree의 역할 (Zybo 기준)

```
① FSBL (First Stage Boot Loader)
   └─ SDcard의 BOOT.BIN에서 로드
   └─ PS 초기화 (DDR, 클럭, MIO 핀)

② U-Boot
   └─ TFTP로 uImage(커널)와 system.dtb(DTB)를 메모리에 수신
   └─ DTB를 메모리 주소 0x2A00000에 로드
   └─ 커널에게 DTB 메모리 주소를 전달하며 부팅 시작

③ Linux 커널
   └─ DTB를 파싱하여 트리 구조를 메모리에 구축
   └─ 각 노드의 compatible 속성과 등록된 드라이버를 매칭
   └─ 매칭된 드라이버의 probe() 함수 호출
   └─ /proc/device-tree/에 전체 트리를 가상 파일로 노출
```

### Device Tree의 설계 원칙

- **원칙 1 — 하드웨어의 "상태"를 기술하라, "설정"이 아니다.** Device Tree는 "이 보드에 UART가 0x101F1000에 존재한다"를 기술하는 것이지, "UART를 115200 baud로 설정하라"를 지시하는 것이 아니다.
- **원칙 2 — OS에 독립적이다.** 동일한 Device Tree가 Linux, U-Boot, FreeBSD 어디에서든 사용 가능해야 한다.
- **원칙 3 — 하드웨어의 "통합(integration)"을 기술하라, "내부 동작"이 아니다.** UART 컨트롤러 내부의 레지스터 동작 방식은 드라이버 코드가 처리한다. Device Tree는 그 UART가 시스템 내에서 어떻게 연결되어 있는지 — IRQ 라인 번호, 클럭 소스, DMA 채널 — 를 기술한다.

## DTS / DTSI / DTB 형식과 컴파일

### 세 가지 파일 형식

| 파일 형식 | 설명 |
| --- | --- |
| **DTS** (Device Tree Source) | 사람이 읽고 편집하는 텍스트 형식. C 스타일 주석 지원. 파일 확장자 `.dts`. 최종 보드별 Device Tree를 정의한다. |
| **DTSI** (Device Tree Source Include) | DTS에 포함(include)되는 공통 정의 파일. 파일 확장자 `.dtsi`. SoC 레벨의 공통 노드 정의를 담는다. |
| **DTB** (Device Tree Blob) | 커널이 읽는 바이너리 형식. DTS를 `dtc`(Device Tree Compiler)로 컴파일한 결과물. U-Boot가 메모리에 로드하여 커널에 전달하는 것이 바로 이 DTB 파일이다. |

**컴파일 관계:**

```
zynq-7000.dtsi  (SoC 공통: CPU, GIC, GPIO)
      ↑ include
board-zybo.dts  (보드별: LED, 버튼, PHY)
      | dtc 컴파일
      ↓
board-zybo.dtb  (바이너리, ~30KB)
      | U-Boot이 메모리에 로드
      ↓
Linux 커널 파싱 → /proc/device-tree/ 노출
```

### DTS ↔ DTB 컴파일과 역컴파일

`dtc`(Device Tree Compiler)는 DTS와 DTB 사이의 양방향 변환을 수행한다.

```bash
# DTS → DTB 컴파일
dtc -I dts -O dtb -o board.dtb board.dts

# DTB → DTS 역컴파일 (decompile)
dtc -I dtb -O dts -o board_decompiled.dts board.dtb
```

### DTSI 파일의 상속 구조

DTSI 파일은 C의 헤더 파일처럼 `#include` 또는 `/include/`로 포함된다.

**나중에 선언된 속성이 먼저 선언된 속성을 덮어쓴다**는 규칙이다. 이를 **overlay(상속) 메커니즘**이라 한다.

**PetaLinux에서의 핵심 패턴:**

| 파일 | 역할 |
| --- | --- |
| `zynq-7000.dtsi` | Zynq-7000 SoC 공통 **(수정 금지)** |
| `pcw.dtsi` | Vivado에서 자동 생성 **(수정 금지)** |
| `system-user.dtsi` | **사용자가 수정 가능한 유일한 파일** |

### Linux 커널의 DTS 파일 위치

- ARM 32비트 (Linux 6.5 이전): `arch/arm/boot/dts/`
- ARM 32비트 (Linux 6.5 이후): `arch/arm/boot/dts/<vendor>/`
- ARM 64비트: `arch/arm64/boot/dts/<vendor>/`

## DTS 핵심 문법

### 트리 구조: 노드와 속성

Device Tree는 이름 그대로 **트리 데이터 구조**다. 모든 DTS 파일은 `/dts-v1/;` 선언으로 시작하며, 루트 노드 `/`를 최상위에 둔다.

```
/dts-v1/;

/ {                                     /* 루트 노드 */
    node1 {                             /* 자식 노드 */
        a-string-property = "A string";
        a-byte-data-property = [01 23 34 56];

        child-node1 {                   /* 손자 노드 */
            first-child-property;       /* 빈 속성 (값 없음) */
            second-child-property = <1>;
        };
    };

    node2 {
        an-empty-property;
        a-cell-property = <1 2 3 4>;    /* 각 숫자는 uint32 셀 */
    };
};
```

- **루트 노드 `/`**: 트리의 최상위. 모든 노드는 이 아래에 위치한다.
- **자식 노드**: `node1`, `node2`는 루트의 직접 자식이다.
- **속성**: 노드 내부의 이름-값 쌍이다.

### 노드 이름 규칙

```
[label:] node-name[@unit-address]
```

- **`node-name`** — 디바이스 종류를 나타내는 ASCII 문자열. 1~31자 길이. 관례적으로 디바이스 송류를 이름에 반영한다: `serial`, `gpio`, `ethernet`, `spi`, `i2c` 등.
- **`@unit-address`** — 디바이스의 기본 주소. 해당 노드의 `reg` 속성 첫 번째 값과 일치해야 한다.
- **`label:`** — 다른 노드에서 이 노드를 참조할 때 사용하는 레이블. DTS 소스에서만 유효하며, DTB 바이너리에는 저장되지 않는다.

### 속성 데이터 타입

| 타입 | 표현 | 예시 |
| --- | --- | --- |
| **빈 속성** | 값 없음 | `interrupt-controller;` |
| **문자열** | `"..."` | `compatible = "arm,cortex-a9";` |
| **문자열 리스트** | `"...", "..."` | `compatible = "st,stm32mp1-dwmac", "snps,dwmac-4.20a";` |
| **셀 배열** | `<...>` | `reg = <0x101f0000 0x1000>;` |
| **바이트 배열** | `[...]` | `local-mac-address = [00 11 22 33 44 55];` |
| **혼합** | `,`로 연결 | `mixed-property = "a string", [01 23 45 67], <0x12345678>;` |

### phandle — 노드 간 참조

`phandle`은 Device Tree 안에서 **한 노드가 다른 노드를 참조할 때** 사용하는 메커니즘이다. DTS 소스에서는 `&label` 문법으로 표현하며, DTB 바이너리에서는 고유한 32비트 정수값(`phandle`)으로 변환된다.

```
/* 인터럽트 컨트롤러 노드에 레이블 정의 */
intc: interrupt-controller@a0021000 {
    compatible = "arm,cortex-a7-gic";
    #interrupt-cells = <3>;
    interrupt-controller;
    reg = <0xa0021000 0x1000>, <0xa0022000 0x2000>;
};

/* 다른 노드에서 &intc로 참조 */
soc {
    interrupt-parent = <&intc>;   /* &intc → intc 노드의 phandle 값 */

    serial@5c000000 {
        interrupts = <GIC_SPI 51 IRQ_TYPE_LEVEL_HIGH>;
    };
};
```

## 핵심 표준 속성 상세

### `compatible` — 가장 중요한 속성

`compatible`은 Device Tree에서 **가장 핵심적인 속성**이다. 이 속성이 커널 드라이버와 디바이스 노드를 연결하는 열쇠 역할을 한다.

```
compatible = "fsl,mpc8641", "ns16550";
```

**형식:** 문자열 리스트이며, **가장 구체적인 것에서 가장 일반적인 것** 순서로 나열한다.

**드라이버 매칭 원리:**

1. Linux 커널은 `compatible` 리스트를 앞에서부터 순서대로 매칭을 시도한다.
2. `"fsl,mpc8641"`을 지원하는 드라이버가 있으면 그것을 사용한다.
3. 없으면 다음 문자열 `"ns16550"`을 지원하는 드라이버를 찾는다.
4. 이를 통해 **하위 호환성**을 구현한다.

### `reg` — 주소와 크기

`reg` 속성은 디바이스의 **주소 영역**을 정의한다.

```
reg = <주소 크기 [주소 크기 ...]>;
```

`reg` 값의 해석은 **부모 노드의 `#address-cells`와 `#size-cells`에 의해 결정된다.**

### `#address-cells`와 `#size-cells`

이 두 속성은 **자식 노드의 `reg` 속성을 어떻게 해석할지** 지정한다. 부모 노드에 설정하며, 자식 노드에 적용된다.

| 버스 유형 | `reg`의 의미 | 예시 |
| --- | --- | --- |
| Memory-mapped | 물리 주소 + 크기 | `reg = <0xe000a000 0x1000>;` |
| I2C | 슬레이브 주소 | `reg = <0x4a>;` |
| SPI | Chip Select 번호 | `reg = <0>;` |

### `status` — 디바이스 활성화 상태

| 값 | 의미 | 설명 |
| --- | --- | --- |
| `"okay"` | 동작 중 | 디바이스가 사용 가능하며 드라이버가 probe된다 |
| `"disabled"` | 비활성화 | 현재 사용하지 않지만 향후 활성화 가능 |
| `"reserved"` | 예약됨 | 다른 소프트웨어(예: firmware)가 제어 중 |
| `"fail"` | 오류 | 하드웨어 오류 감지, 수리 불가 상태 |

> `status` 속성이 없으면 `"okay"`로 간주한다.
> 

**DTSI/DTS 패턴에서의 핵심 역할:**

SoC DTSI 파일에서는 외부 세계와 인터페이스하는 디바이스를 기본적으로 `"disabled"`로 설정한다. 보드별 DTS 파일에서 해당 보드가 실제로 사용하는 디바이스만 `"okay"`로 활성화한다.

### `interrupts`와 인터럽트 관련 속성

인터럽트 연결을 기술하는 네 가지 속성:

| 속성 | 설명 |
| --- | --- |
| `interrupt-controller` | 빈 속성. 이 노드가 인터럽트 컨트롤러임을 선언한다. |
| `#interrupt-cells` | 인터럽트 컨트롤러 노드에 설정. 인터럽트 하나를 기술하는 데 필요한 셀 수를 지정한다. |
| `interrupt-parent` | 디바이스 노드에 설정. 이 디바이스의 인터럽트가 연결된 인터럽트 컨트롤러를 phandle로 가리킨다. |
| `interrupts` | 디바이스 노드에 설정. 인터럽트 사양(specifier)의 목록이다. |

**GIC(Generic Interrupt Controller)의 3셀 인터럽트 사양:**

| 셀 | 의미 | 값 |
| --- | --- | --- |
| 첫째 | 인터럽트 타입 | `GIC_SPI` (0): Shared Peripheral, `GIC_PPI` (1): Private Peripheral |
| 둘째 | 인터럽트 번호 | SPI: 0~987, PPI: 0~15 |
| 셋째 | 플래그 | `IRQ_TYPE_LEVEL_HIGH` (4), `IRQ_TYPE_EDGE_RISING` (1) 등 |

### `ranges` — 주소 변환

`ranges` 속성은 **부모 주소 공간과 자식 주소 공간 간의 주소 변환 규칙**을 정의한다. 버스 브리지 노드에서 사용된다.

> `ranges`가 **빈 속성**이면 부모-자식 주소 공간이 1:1 매핑이라는 뜻이다.
> 

## 디바이스 트리의 특수 노드

### 루트 노드 `/`

모든 Device Tree의 최상위 노드. 필수 속성:

```
/ {
    #address-cells = <1>;               /* 필수 */
    #size-cells = <1>;                  /* 필수 */
    model = "Digilent Zybo Z7";         /* 필수: 보드 모델명 */
    compatible = "digilent,zynq-zybo", "xlnx,zynq-7000";  /* 필수 */
};
```

### `/cpus` 노드

모든 Device Tree에 필수로 존재해야 하는 노드. 시스템의 CPU를 기술한다.

```
cpus {
    #address-cells = <1>;
    #size-cells = <0>;          /* CPU에는 크기가 없으므로 0 */

    cpu@0 {
        compatible = "arm,cortex-a9";
        device_type = "cpu";
        reg = <0>;              /* CPU 번호 */
        clock-frequency = <666666687>;
    };
};
```

### `/memory` 노드

시스템의 물리 메모리를 기술한다. 최소 하나 이상 존재해야 한다.

```
memory@0 {
    device_type = "memory";
    reg = <0x00000000 0x20000000>;  /* 시작 0, 크기 512MB */
};
```

### `/chosen` 노드

실제 하드웨어를 나타내지 않는 특수 노드. **펌웨어(부트로더)와 OS 사이에서 데이터를 전달하는 용도**로 사용한다.

```
chosen {
    bootargs = "console=ttyPS0,115200 root=/dev/nfs rw nfsroot=10.0.0.2:/nfsroot";
    stdout-path = "serial0:115200n8";
};
```

> 이 노드의 내용은 보통 DTS에는 비워 두고, U-Boot가 부팅 시 동적으로 채운다.
> 

### `/aliases` 노드

긴 노드 경로에 짧은 별칭을 부여한다.

```c
aliases {
		ethernet0 = &gem0; // /amba/ethernet@e000b000 의 별칭
		serial0 = &uart1;  // /amba/serial@e0001000 의 별칭
};
```