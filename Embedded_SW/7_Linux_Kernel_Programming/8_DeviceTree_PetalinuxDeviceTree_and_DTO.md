# 임베디드 리눅스 커널 프로그래밍 day08

날짜: 2026년 3월 25일

# CHAP 21. continue

---

## 21.6 디바이스 트리와 드라이버 연결 원리

### `of_match_table` — 드라이버의 compatible 매칭 테이블

Linux 커널 드라이버는 `of_device_id` 구조체 배열로 자신이 지원하는 `compatible` 문자열 목록을 선언한다.

이 배열이 Device Tree 노드의 `compatible` 속성과 매칭되면 드라이버의 `probe()` 함수가 호출된다.

```c
/* drivers/tty/serial/stm32-usart.c 의 실제 코드 */
static const struct of_device_id stm32_match[] = {
    { .compatible = "st,stm32-uart",  .data = &stm32f4_info },
    { .compatible = "st,stm32f7-uart",.data = &stm32f7_info },
    { .compatible = "st,stm32h7-uart",.data = &stm32h7_info },
    {},                              /* 종료 마커 (빈 항목) */
};
MODULE_DEVICE_TABLE(of, stm32_match);

static struct platform_driver stm32_serial_driver = {
    .probe  = stm32_serial_probe,
    .remove = stm32_serial_remove,
    .driver = {
        .name          = DRIVER_NAME,
        .of_match_table = of_match_ptr(stm32_match), /* ← DT 매칭 테이블 */
    },
};
```

### 매칭 과정 흐름

```
① 커널 부팅 시 DTB 파싱
   └─ 루트 노드의 자식, simple-bus의 자식 → platform_device로 등록

② 드라이버 모듈 로드 (built-in 또는 insmod)
   └─ platform_driver_register(&stm32_serial_driver) 호출
   └─ 커널이 등록된 모든 platform_device를 순회

③ 매칭 시도
   └─ device의 compatible ↔ driver의 of_match_table 비교
   └─ 일치하는 항목 발견!

④ probe() 호출
   └─ stm32_serial_probe(pdev) 실행
   └─ DTS에서 reg, interrupts 등 리소스를 읽어 드라이버 초기화
```

### `probe()` 함수에서 DTS 속성 읽기

`probe()` 함수 내에서 DTS 노드의 속성을 읽는 주요 API:

```c
/* 정수 속성 읽기 */
u32 val;
of_property_read_u32(np, "my-property", &val);

/* 문자열 속성 읽기 */
const char *str;
of_property_read_string(np, "my-string", &str);

/* reg 속성에서 메모리 리소스 획득 */
struct resource *res;
res = platform_get_resource(pdev, IORESOURCE_MEM, 0);
void __iomem *base = devm_ioremap_resource(&pdev->dev, res);

/* interrupts 속성에서 IRQ 번호 획득 */
int irq = platform_get_irq(pdev, 0);
```

> 이 API들을 사용하면 드라이버 코드에서 하드코딩된 주소(`0xE000A000`)를 완전히 제거할 수 있다.
> 
> 
> PART 2~5에서 직접 `ioremap(0xE000A000, ...)`으로 작성했던 코드들을, 챕터 23에서 Platform Driver 방식으로 재구성할 때 이 API들이 핵심이 된다.
> 

### `simple-bus`와 자동 디바이스 등록

Linux 커널은 다음 조건의 DT 노드들을 **자동으로 `platform_device`로 등록**한다:

1. 루트 노드의 직접 자식 중 `compatible` 속성이 있는 노드
2. `compatible = "simple-bus"` 노드의 자식 노드

Zynq-7000의 경우, PS 주변장치는 `amba` 버스(simple-bus) 아래에 위치한다:

```
/ {
    amba: amba {
        compatible = "simple-bus";
        #address-cells = <1>;
        #size-cells = <1>;
        ranges;

        /* 이 아래의 노드들이 자동으로 platform_device로 등록됨 */
        gpio0: gpio@e000a000 { ... };
        uart1: serial@e0001000 { ... };
        gem0: ethernet@e000b000 { ... };
    };
};
```

## Zybo 보드의 Device Tree 구조

### Zynq-7000 Device Tree 파일 계층

PetaLinux로 생성된 Zybo 프로젝트의 Device Tree 파일 계층:

```
system-top.dts          ← 최종 DTS (자동 생성, 수정 금지)
  #include 모든 dtsi

  ├── zynq-7000.dtsi    (SoC 공통 정의, 수정 금지)
  ├── pcw.dtsi          (Vivado 자동생성, 수정 금지)
  └── system-user.dtsi  (사용자 수정 가능, 유일한 진입점)
```

| 파일 | 생성 주체 | 수정 | 내용 |
| --- | --- | --- | --- |
| `zynq-7000.dtsi` | PetaLinux (고정) | 금지 | Zynq-7000 SoC CPU, GIC, GPIO, UART, 이더넷 등 |
| `pcw.dtsi` | Vivado XSA 기반 자동 | 금지 | PS 핀 설정, 클럭 설정, 활성화된 PS 주변장치 |
| `pl.dtsi` | Vivado XSA 기반 자동 | 금지 | PL(FPGA) 쪽 IP 노드 (AXI GPIO 등) |
| `system-conf.dtsi` | PetaLinux 자동 | 금지 | 시스템 설정 (bootargs 등) |
| `system-top.dts` | PetaLinux 자동 | 금지 | 최종 DTS (모든 dtsi를 include) |
| `system-user.dtsi` | **사용자** | **허용** | 사용자 추가 노드 및 속성 override |

> **핵심 원칙:** `components/plnx_workspace/device-tree/device-tree/` 하위의 자동 생성 파일은 절대 직접 편집하지 않는다. 모든 커스터마이징은 `project-spec/meta-user/recipes-bsp/device-tree/files/system-user.dtsi`에서 수행한다.
> 

### Zybo Device Tree의 주요 노드 구조

```
/dts-v1/;

/ {
    model = "Zynq Zybo Z7 Development Board";
    compatible = "digilent,zynq-zybo-z7", "xlnx,zynq-7000";

    #address-cells = <1>;
    #size-cells = <1>;

    aliases {
        ethernet0 = &gem0;
        serial0 = &uart1;
    };

    chosen {
        bootargs = "console=ttyPS0,115200 root=/dev/nfs ...";
        stdout-path = "serial0:115200n8";
    };

    cpus {
        cpu@0 {
            compatible = "arm,cortex-a9";
            device_type = "cpu";
            reg = <0>;
            clocks = <&clkc 3>;
        };
    };

    memory@0 {
        device_type = "memory";
        reg = <0x00000000 0x20000000>;  /* 512MB DDR */
    };

    amba: amba {
        compatible = "simple-bus";
        interrupt-parent = <&intc>;
        ranges;

        intc: interrupt-controller@f8f01000 {
            compatible = "arm,cortex-a9-gic";
            #interrupt-cells = <3>;
            interrupt-controller;
            reg = <0xf8f01000 0x1000>, <0xf8f00100 0x100>;
        };

        gpio0: gpio@e000a000 {
            compatible = "xlnx,zynq-gpio-1.0";
            #gpio-cells = <2>;
            reg = <0xe000a000 0x1000>;
            interrupts = <0 20 4>;  /* SPI, IRQ#20, level-high */
            gpio-controller;
        };

        uart1: serial@e0001000 {
            compatible = "xlnx,xuartps", "cdns,uart-r1p8";
            reg = <0xe0001000 0x1000>;
            interrupts = <0 50 4>;
            status = "okay";
        };

        gem0: ethernet@e000b000 {
            compatible = "cdns,zynq-gem", "cdns,gem";
            reg = <0xe000b000 0x1000>;
            interrupts = <0 22 4>;
            status = "okay";
            phy-mode = "rgmii-id";
        };
    };
};
```

## 실습: DTS 작성 → DTB 컴파일 → 역컴파일 확인

### 실습 1: 간단한 커스텀 DTS 작성 및 컴파일

```bash
# dtc 설치 확인 (PetaLinux SDK 환경이 아닌 일반 Ubuntu에서)
sudo apt install -y device-tree-compiler

# DTS → DTB 컴파일
dtc -I dts -O dtb -o zybo_test.dtb zybo_test.dts

# DTB → DTS 역컴파일
dtc -I dtb -O dts -o zybo_test_decompiled.dts zybo_test.dtb
```

**확인 포인트:**

- 역컴파일 결과에서 `my_led:` 레이블이 사라졌는지 확인 (DTB에 저장되지 않음)
- `phandle` 속성이 자동으로 추가되었는지 확인
- 숫자 표현이 hex 형식으로 통일되었는지 확인

### 실습 2: 타겟 보드에서 현재 Device Tree 탐색

```bash
# /proc/device-tree/ 전체 구조 확인
ls /proc/device-tree/

# 보드 모델명 확인
cat /proc/device-tree/model
# 예상: Zynq Zybo Z7 Development Board

# compatible 문자열 확인 (null 구분자 → xxd로 확인)
xxd /proc/device-tree/compatible

# 메모리 크기 확인
hexdump -C /proc/device-tree/memory@0/reg
# 예상: 00000000 00000000 20000000 (시작 0, 크기 512MB)

# GPIO 컨트롤러 노드 탐색
ls /proc/device-tree/amba/gpio@e000a000/
cat /proc/device-tree/amba/gpio@e000a000/compatible
# 예상: xlnx,zynq-gpio-1.0

# 현재 DTB를 파일로 추출하여 역컴파일
cp /sys/firmware/fdt /tmp/current.dtb
dtc -I dtb -O dts /tmp/current.dtb > /tmp/current.dts
```

### 실습 3: DTS 문법 오류 진단

`dtc`는 **구문(syntax) 검사만** 수행한다. `compatible` 문자열이 실제 드라이버에 존재하는지, `reg` 주소가 유효한 하드웨어 영역인지 등의 **의미(semantic) 검증은 하지 않는다.**

의미 검증은 Linux 커널의 YAML binding 시스템(`make dtbs_check`)으로 별도 수행한다.

**자주 발생하는 오류 유형:**

| 오류 | 원인 | 메시지 |
| --- | --- | --- |
| 세미콜론 누락 | `compatible = "test"` (`;` 없음) | `syntax error` |
| `#size-cells` 불일치 | `#size-cells=1`인데 크기값 없음 | `reg entry size mismatch` |
| unit-address와 reg 불일치 | `node@2000`인데 `reg` 첫값은 `0x1000` | `unit_address_vs_reg mismatch` |

## 실습: `gpio-leds` 커널 드라이버를 DTS로 바인딩하기

> **대응 예제:** `42_led_dtb`
> 

### `gpio-leds` 드라이버란

Linux 커널의 `drivers/leds/leds-gpio.c`에 구현된 표준 LED 드라이버다.

DTS에 `compatible = "gpio-leds"` 노드가 있으면 자동으로 `probe()`된다.

이 드라이버를 사용하면:

- `/sys/class/leds/<label>/brightness` 파일에 `0`/`1` 쓰기로 LED 제어
- `/sys/class/leds/<label>/trigger` 파일로 `heartbeat`, `timer`, `mmc0` 등 내장 트리거 선택
- **별도 커스텀 드라이버 코드 없이 DTS 노드만으로 완성**

### 사전 조건 — GPIO 컨트롤러 레이블 확인

`gpio-leds` 드라이버는 DTS의 `gpios` 속성에서 GPIO 컨트롤러 phandle을 참조한다.

Zynq-7000에서는 `gpio@e000a000` 노드가 GPIO 컨트롤러이므로, 이 노드에 레이블(`gpio:`)이 붙어 있어야 한다.

```bash
# 현재 적용된 DTB에 레이블이 있는지 확인
dtc -I dtb -O dts /boot/system.dtb 2>/dev/null | grep -A3 "gpio@e000a000"
# 출력 예:
#   gpio: gpio@e000a000 {    ← 'gpio:' 레이블이 있으면 OK
#       compatible = "xlnx,zynq-gpio-1.0";
```

### `system_modify.dts` 수정 내용 분석

`42_led_dtb/system_modify.dts`는 원본 `system.dts`에서 다음 세 가지를 수정한 파일이다.

**수정 ① — `aliases` 노드 추가**

```
/ {
    aliases {
        gpio0 = &gpio;  /* gpio@e000a000 노드를 "gpio0" 이름으로 등록 */
    };
    ...
};
```

`aliases` 노드는 커널과 드라이버가 특정 노드를 이름으로 찾을 수 있게 해준다.

`gpio0 = &gpio`를 추가하면 `of_find_node_by_path("/aliases/gpio0")` 같은 API로 GPIO 컨트롤러를 찾을 수 있다.

**수정 ② — `gpio-leds` 노드 추가**

```
/ {
    gpio-leds {
        compatible = "gpio-leds";   /* leds-gpio.c 드라이버와 매칭 */

        led1 {
            label = "led1";         /* /sys/class/leds/led1/ 로 노출 */
            gpios = <&gpio 7 0>;    /* GPIO 컨트롤러(&gpio), 핀7, GPIO_ACTIVE_HIGH */
            default-state = "off";  /* 부팅 시 초기 상태: OFF */
        };
    };
};
```

**수정 ③ — `gpio@e000a000` 노드에 레이블 추가**

```
/* 수정 전 */
gpio@e000a000 {

/* 수정 후 */
gpio: gpio@e000a000 {   /* 'gpio:' 레이블 추가 */
```

이 레이블이 있어야 `gpios = <&gpio 7 0>`에서 `&gpio` phandle 참조가 가능하다.

### DTB 재컴파일 및 배포

```bash
# Ubuntu VM에서 수정된 DTS → DTB 컴파일
dtc -I dts -O dtb -o system_modify.dtb system_modify.dts

# U-Boot에서 새 DTB 파일명으로 부팅하려면 bootcmd 수정:
# Zynq> setenv fdtfile system_modify.dtb
# Zynq> saveenv
# Zynq> boot
```

### `/sys/class/leds/` 인터페이스 실습

```bash
# LED 노드가 생성됐는지 확인
ls /sys/class/leds/
# 출력: led1    ← DTS의 label = "led1" 값

# LED ON / OFF
echo 1 > /sys/class/leds/led1/brightness
echo 0 > /sys/class/leds/led1/brightness

# 내장 트리거 목록 확인 ([ ]가 현재 선택된 트리거)
cat /sys/class/leds/led1/trigger
# 출력: [none] rc-feedback kbd-scrolllock ... heartbeat timer ...

# heartbeat 트리거: 심장 박동 패턴으로 자동 점멸
echo heartbeat > /sys/class/leds/led1/trigger

# timer 트리거: 일정 주기로 점멸 (delay_on/off ms 단위)
echo timer > /sys/class/leds/led1/trigger
echo 100 > /sys/class/leds/led1/delay_on     # ON 100ms
echo 900 > /sys/class/leds/led1/delay_off    # OFF 900ms

# 트리거 해제 후 수동 제어로 복귀
echo none > /sys/class/leds/led1/trigger
```

### `gpios` 속성 세부 분석 — `<&gpio 7 0>`의 의미

```
gpios = <&gpio 7 0>;
/*        │    │  └─ 플래그: GPIO_ACTIVE_HIGH (0) 또는 GPIO_ACTIVE_LOW (1)
 *        │    └──── 핀 번호: GPIO 컨트롤러의 7번 핀 = MIO7 = Zybo LD0
 *        └───────── phandle: gpio: gpio@e000a000 노드 참조
 */
```

**GPIO 번호 규칙 (Zynq-7000):**

- MIO 핀 N → `#gpio-cells` 기준 N번 → Linux GPIO `base + N`
- 예: `gpio_base`가 906이면, MIO7 = Linux GPIO 번호 913

**플래그 값:**

| 값 | 의미 | LED 동작 |
| --- | --- | --- |
| `GPIO_ACTIVE_HIGH = 0` | `gpios = <&gpio 7 0>` | 1 쓰면 LED ON |
| `GPIO_ACTIVE_LOW = 1` | `gpios = <&gpio 7 1>` | 0 쓰면 LED ON |

### 자주 발생하는 오류

| 증상 | 원인 | 해결 |
| --- | --- | --- |
| `/sys/class/leds/` 디렉토리가 비어 있음 | DTB가 수정 전 것으로 로드됨 | `fdtfile` 환경 변수 확인, DTB 재배포 |
| `gpios` 속성 에러 (invalid phandle) | `gpio:` 레이블 누락 | DTS에서 `gpio: gpio@e000a000` 레이블 추가 확인 |
| DTC 컴파일 에러: `undefined symbol &gpio` | 위와 동일 | 동상 |
| LED 켜지지 않음 (`brightness=1` 썼음) | `GPIO_ACTIVE_LOW` 설정 필요 | `gpios = <&gpio 7 1>`로 수정 |

# CHAP 22. PetaLinux 디바이스 트리 파일 구조

## PetaLinux 디바이스 트리 파일의 이중 구조

PetaLinux에서 디바이스 트리를 다루는 핵심 원칙: **"자동 생성 파일은 절대 직접 편집하지 않고, 사용자 파일에서만 수정한다."**

```
<plnx-proj-root>/
│
├── components/plnx_workspace/device-tree/device-tree/  ← 🔒 자동 생성 영역 (수정 금지)
│   ├── skeleton.dtsi       (Zynq-7000 전용)
│   ├── zynq-7000.dtsi      (Zynq-7000 전용)
│   ├── pcw.dtsi            (PS 주변장치 설정)
│   ├── pl.dtsi             (PL IP 정의)
│   ├── system-conf.dtsi    (시스템 구성 설정)
│   ├── system-top.dts      (최상위 DTS, 모든 dtsi를 include)
│   └── <board>.dtsi        (BSP 보드 설정)
│
└── project-spec/meta-user/recipes-bsp/device-tree/files/  ← ✅ 사용자 수정 영역 (안전)
    ├── system-user.dtsi    (사용자 커스터마이징 진입점)
    └── pl-custom.dtsi      (PL 노드 커스터마이징)
```

## 자동 생성 파일 상세 분석 — 수정 금지 영역

`petalinux-config`를 실행하면 Vivado에서 export한 XSA(하드웨어 설명 파일)를 파싱하여 다음 파일들을 자동 생성한다. 이 파일들은 **매 설정 변경 시 재생성(덮어쓰기)** 되므로, 어떤 이유로든 직접 편집해서는 안 된다.

모든 자동 생성 파일 경로:

```
<plnx-proj-root>/components/plnx_workspace/device-tree/device-tree/
```

### `skeleton.dtsi` — DTS 뼈대 (Zynq-7000 전용)

디바이스 트리의 가장 기본적인 뼈대를 정의한다. 루트 노드(`/`), `#address-cells`, `#size-cells`, 기본 `cpus` 노드 등 최소한의 구조만 포함된다.

```
/* skeleton.dtsi – 자동 생성, 수정 금지 */
/ {
    #address-cells = <1>;
    #size-cells = <1>;
    chosen { };
    aliases { };
    memory { device_type = "memory"; reg = <0 0>; };
};
```

### `zynq-7000.dtsi` — Zynq-7000 SoC IP 정의 (Zynq-7000 전용)

Zynq-7000 PS(Processing System)의 모든 IP 블록을 정의한다. ARM 코어, GIC, UART, I2C, SPI, GPIO, Ethernet, USB 등 PS에 내장된 주변장치의 기본 레지스터 주소, 인터럽트 번호, 클럭 설정이 모두 여기에 담겨 있다.

**핵심 관찰 포인트:** 대부분의 주변장치 노드가 `status = "disabled"`로 선언되어 있다.

Vivado에서 활성화한 주변장치만 `pcw.dtsi`에서 `status = "okay"`로 override된다. 이것이 DTS의 override 메커니즘의 전형적인 패턴이다.

### `pcw.dtsi` — PS 주변장치 활성화 설정

PCW(Processing Configuration Wizard)가 생성하는 파일이다. Vivado Block Design에서 PS 주변장치를 활성화/비활성화한 설정이 이 파일에 반영된다.

```
/* pcw.dtsi – 자동 생성, 수정 금지 */
&uart1 {
    status = "okay";        /* PS UART1 활성화 (Zybo의 USB-UART) */
};

&gem0 {
    status = "okay";        /* PS Ethernet 0 활성화 */
    phy-mode = "rgmii-id";
};

&gpio0 {
    status = "okay";        /* PS GPIO 활성화 */
};

&usb0 {
    status = "okay";        /* PS USB 0 활성화 */
};

&qspi {
    status = "okay";        /* PS QSPI Flash 활성화 */
};

&sdhci0 {
    status = "okay";        /* PS SD 카드 인터페이스 활성화 */
};
```

`&uart1`처럼 `&` 접두사를 붙이면 기존에 선언된 노드를 **참조(reference)**하여 속성을 override할 수 있다.

### `pl.dtsi` — PL(Programmable Logic) IP 정의

Vivado Block Design에서 PL 영역에 추가한 AXI 주변장치(AXI GPIO, AXI UART, AXI Timer 등)의 노드가 이 파일에 자동 생성된다.

```
/* pl.dtsi – PL IP 노드 예시 (자동 생성, 수정 금지) */
/* AXI GPIO IP를 추가한 경우 */
/ {
    amba_pl: amba_pl {
        compatible = "simple-bus";
        ranges;

        axi_gpio_0: gpio@41200000 {
            #gpio-cells = <2>;
            compatible = "xlnx,xps-gpio-1.00.a";
            gpio-controller;
            reg = <0x41200000 0x10000>;
            xlnx,all-inputs = <0x0>;
            xlnx,all-outputs = <0x1>;
            xlnx,gpio-width = <0x4>;
        };
    };
};
```

> PL 노드를 커스터마이징하고 싶다면 `pl.dtsi`를 직접 편집하는 것이 아니라, `pl-custom.dtsi`에서 override해야 한다.
> 

### `system-conf.dtsi` — 시스템 구성 설정

`petalinux-config`에서 설정한 시스템 수준 옵션이 반영되는 파일이다. 메모리 크기, 콘솔 설정, 부팅 인자(bootargs) 등이 포함된다.

```
/* system-conf.dtsi – 자동 생성, 수정 금지 */
/ {
    memory {
        device_type = "memory";
        reg = <0x0 0x20000000>;     /* 512MB DDR (Zybo Z7-10 기준) */
    };

    chosen {
        bootargs = "console=ttyPS0,115200 earlyprintk";
        stdout-path = "serial0:115200n8";
    };

    aliases {
        serial0 = &uart1;
        ethernet0 = &gem0;
        spi0 = &qspi;
    };
};
```

> `chosen` 노드의 `bootargs`는 `petalinux-config → DTG Settings → Kernel Bootargs`에서 설정한 값이 반영된다. 이 값을 변경하고 싶다면 menuconfig에서 변경하거나, `system-user.dtsi`에서 `chosen` 노드의 `bootargs`를 직접 override하면 된다.
> 

### `system-top.dts` — 최상위 DTS 파일

모든 DTSI 파일을 include하는 진입점 파일이다.

```
/* system-top.dts – 자동 생성, 수정 금지 */
/dts-v1/;

/include/ "zynq-7000.dtsi"
/include/ "pcw.dtsi"
/include/ "pl.dtsi"
/include/ "system-conf.dtsi"
/include/ "system-user.dtsi"   /* ← 사용자 파일이 가장 마지막에 include됨 */
```

**include 순서가 매우 중요하다.** `system-user.dtsi`가 가장 마지막에 포함되기 때문에, 앞서 선언된 어떤 속성이든 `system-user.dtsi`에서 override할 수 있다. 이것이 PetaLinux 디바이스 트리 커스터마이징의 핵심 메커니즘이다.

### 자동 생성 파일 요약

| 파일명 | Zynq-7000 | 역할 | 재생성 시점 |
| --- | --- | --- | --- |
| `skeleton.dtsi` | ✅ 전용 | DTS 기본 뼈대 | `petalinux-config` |
| `zynq-7000.dtsi` | ✅ 전용 | SoC PS 전체 정의 | `petalinux-config` |
| `pcw.dtsi` | ✅ | Vivado PS 설정 반영 | `petalinux-config` |
| `pl.dtsi` | ✅ | PL AXI IP 노드 | `petalinux-config` |
| `system-conf.dtsi` | ✅ | bootargs, memory, aliases | `petalinux-config` |
| `system-top.dts` | ✅ | 최상위 include 진입점 | `petalinux-config` |
| `<board>.dtsi` | ✅ | BSP 보드별 설정 | `petalinux-config` |

> ⚠️ **핵심 경고:** 위 파일들은 `Auto Config Settings → Device tree autoconfig`가 활성화되어 있으면 `petalinux-config`를 실행할 때마다 재생성된다. 직접 편집한 내용은 모두 사라진다.
> 

## 사용자 수정 가능 파일 상세 — 안전한 커스터마이징 영역

사용자가 안전하게 편집할 수 있는 디바이스 트리 파일 경로:

```
<plnx-proj-root>/project-spec/meta-user/recipes-bsp/device-tree/files/
```

PetaLinux 도구는 이 디렉토리 내 파일을 **절대 자동으로 수정하지 않는다.** Git 등 VCS로 관리하기에도 안전하다.

### `system-user.dtsi` — 사용자 커스터마이징의 핵심 진입점

`system-user.dtsi`는 PetaLinux 프로젝트에서 디바이스 트리를 커스터마이징하는 **유일한 정규 진입점**이다.

**기본 생성 상태의 `system-user.dtsi`:**

```
/include/ "system-conf.dtsi"
/ {
};
```

이 최소 상태에서 출발하여 노드를 추가하거나 기존 노드를 override한다.

**override 기본 패턴 — 기존 노드 속성 변경:**

```
/include/ "system-conf.dtsi"
/ {
};

/* 기존 Ethernet PHY 정보 추가 (보드 레벨 정보) */
&gem0 {
    phy-handle = <&phy0>;
    ps7_ethernet_0_mdio: mdio {
        phy0: phy@7 {
            compatible = "marvell,88e1116r";
            device_type = "ethernet-phy";
            reg = <7>;
        };
    };
};
```

**새 노드 추가 패턴 — 루트 아래 새 디바이스 노드:**

```
/include/ "system-conf.dtsi"
/ {
    my_custom_device {
        compatible = "mycompany,custom-ctrl";
        reg = <0x43C00000 0x10000>;
        status = "okay";
    };
};
```

**노드 비활성화 패턴:**

```
/* 자동 생성된 PL IP 중 사용하지 않는 것을 비활성화 */
&axi_gpio_0 {
    status = "disabled";
};
```

**bootargs override 패턴:**

```
/ {
    chosen {
        /* NFS 부팅용 bootargs로 override */
        bootargs = "console=ttyPS0,115200 root=/dev/nfs rw nfsroot=10.0.0.2:/nfsroot,v3,tcp ip=10.0.0.3:10.0.0.2:10.0.0.2:255.255.255.0::eth0:off";
    };
};
```

### `pl-custom.dtsi` — PL 노드 전용 커스터마이징

`pl.dtsi`의 PL IP 노드를 수정하고 싶을 때 사용한다.

```
/* pl-custom.dtsi – PL IP 노드 커스터마이징 */

/* 자동 생성된 AXI GPIO의 속성을 override */
&axi_gpio_0 {
    xlnx,gpio-width = <0x8>;   /* GPIO 폭을 8비트로 변경 */
};
```

### 기타 사용자 수정 가능 DTSI 파일

| 파일명 | 용도 | 활성화 조건 |
| --- | --- | --- |
| `xen.dtsi` | Xen 하이퍼바이저 설정 | Xen 활성화 시 |
| `openamp.dtsi` | OpenAMP(AMP 코어 간 통신) 설정 | OpenAMP 활성화 시 |
| `xen-qemu.dtsi` | Xen QEMU 에뮬레이션 설정 | Xen + QEMU 사용 시 |

> Zybo 보드에서 기본 Linux 개발을 진행할 때는 `system-user.dtsi`만 편집하면 충분하다.
> 

## `system-user.dtsi`를 통한 커스터마이징

### Override 메커니즘의 원리

DTS 컴파일러(DTC)는 같은 노드가 여러 번 선언되면 **나중에 선언된 속성이 이전 속성을 덮어쓴다.**

`system-top.dts`에서 `system-user.dtsi`가 가장 마지막에 include되므로, `system-user.dtsi`의 선언이 최종 우선권을 가진다.

**시나리오: UART 보레이트 변경**

```
1단계 (zynq-7000.dtsi): uart1 노드 정의, status="disabled"
    ↓
2단계 (pcw.dtsi): &uart1 { status="okay"; }  → 활성화
    ↓
3단계 (system-user.dtsi): &uart1 { current-speed=<115200>; }  → 속성 추가
    ↓
최종 결과 (system.dtb):
    serial@e0001000 {
        status = "okay";           /* pcw.dtsi에서 override됨 */
        current-speed = <115200>;  /* system-user.dtsi에서 추가됨 */
        ...
    }
```

3개 파일에 분산된 정보가 하나의 완성된 노드로 합쳐졌다. 이것이 DTS override 체인의 핵심이다.

### 자식 노드 추가

```
&gpio0 {
    /* gpio0 노드 아래에 자식 노드 추가 */
    gpio-line-names =
        "MIO00", "MIO01", "MIO02", "MIO03",
        "MIO04", "MIO05", "MIO06", "MIO7_LED",
        "MIO08", "MIO09", "MIO10", "MIO11",
        "MIO12", "MIO13", "MIO14", "MIO15";
};
```

### 속성 삭제

DTS에서는 `/delete-property/` 지시자로 기존 속성을 제거할 수 있다:

```
&gem0 {
    /delete-property/ phy-handle;  /* phy-handle 속성 삭제 */
};
```

노드 자체를 제거하려면 `/delete-node/`를 사용한다:

```
/ {
    /delete-node/ my_unwanted_node;
};

/* 또는 부모 노드 내에서 삭제 */
&amba_pl {
    /delete-node/ gpio@41200000;
};
```

### Override 적용 확인 방법

**방법 1: DTB 역컴파일 (빌드 후)**

```bash
cd <plnx-proj-root>
dtc -I dtb -O dts images/linux/system.dtb -o /tmp/decompiled.dts
grep -A 10 "serial@e0001000" /tmp/decompiled.dts
```

**방법 2: 타겟 보드의 `/proc/device-tree/` 탐색 (부팅 후)**

```bash
# 부팅된 Zybo 보드에서 실행
ls /proc/device-tree/amba/serial@e0001000/
cat /proc/device-tree/amba/serial@e0001000/status
# 출력: okay

hexdump -C /proc/device-tree/amba/serial@e0001000/compatible
```

> `/proc/device-tree/`는 커널이 파싱한 DTB의 내용을 그대로 노출하므로, 실제로 커널에 전달된 디바이스 트리를 확인하는 **가장 신뢰할 수 있는 방법**이다.
> 

## 추가 DTSI 파일 분리 관리

프로젝트가 복잡해지면 `system-user.dtsi` 하나에 모든 커스터마이징을 넣는 것은 유지보수에 불리하다. 기능별로 DTSI 파일을 분리하고 `system-user.dtsi`에서 include하는 패턴이 훨씬 효과적이다.

### DTSI 파일 분리 예시

프로젝트에 LED 제어, 버튼 인터럽트, Ethernet PHY 설정이 모두 필요한 경우:

```
project-spec/meta-user/recipes-bsp/device-tree/files/
├── system-user.dtsi    ← 진입점 (include만 담당)
├── zybo-leds.dtsi      ← LED 관련 노드
├── zybo-buttons.dtsi   ← 버튼/인터럽트 관련 노드
└── zybo-ethernet.dtsi  ← Ethernet PHY 관련 노드
```

`system-user.dtsi`의 내용:

```
/include/ "system-conf.dtsi"
/include/ "zybo-leds.dtsi"
/include/ "zybo-buttons.dtsi"
/include/ "zybo-ethernet.dtsi"
/ {
};
```

각 기능별 DTSI는 독립적으로 관리할 수 있어서, 특정 기능을 비활성화하려면 해당 `/include/` 줄만 주석 처리하면 된다.

### `device-tree.bbappend` 수정 — 필수 단계

추가 DTSI 파일을 만들었다면 **반드시** BitBake 레시피 확장 파일에도 등록해야 한다. 이 단계를 빠뜨리면 빌드 시 추가 파일을 찾지 못해 에러가 발생한다.

**파일 위치:** `<plnx-proj-root>/project-spec/meta-user/recipes-bsp/device-tree/device-tree.bbappend`

```
FILESEXTRAPATHS:prepend := "${THISDIR}/files:"

SRC_URI += "file://system-user.dtsi"
SRC_URI += "file://zybo-leds.dtsi"
SRC_URI += "file://zybo-buttons.dtsi"
SRC_URI += "file://zybo-ethernet.dtsi"
```

| PetaLinux 버전 | 구문 |
| --- | --- |
| 2021.x 이전 | `FILESEXTRAPATHS_prepend` |
| 2022.x 이후 | `FILESEXTRAPATHS:prepend` |

### 파일 추가 전체 절차 요약

새 DTSI 파일을 추가하는 전체 절차 (3개 파일을 모두 수정해야 완전하다):

```bash
# 1. DTSI 파일 생성
nano project-spec/meta-user/recipes-bsp/device-tree/files/zybo-leds.dtsi

# 2. system-user.dtsi에 include 추가
nano project-spec/meta-user/recipes-bsp/device-tree/files/system-user.dtsi
# /include/ "zybo-leds.dtsi" 줄 추가

# 3. device-tree.bbappend에 SRC_URI 추가
nano project-spec/meta-user/recipes-bsp/device-tree/device-tree.bbappend
# SRC_URI += "file://zybo-leds.dtsi" 줄 추가

# 4. 빌드
petalinux-build

# 5. 확인
dtc -I dtb -O dts images/linux/system.dtb | grep -A 5 "leds"
```

> ⚠️ 3개 파일(DTSI 본문, `system-user.dtsi`, `bbappend`)을 모두 수정해야 완전하다. 하나라도 빠지면 동작하지 않는다.
> 

## Menuconfig DTG Settings 상세

`petalinux-config` 명령을 실행하면 나타나는 메뉴 중 **DTG Settings** 항목은 디바이스 트리 생성기(Device Tree Generator)의 동작을 제어한다.

```bash
cd <plnx-proj-root>
petalinux-config
# → DTG Settings 메뉴 진입
```

### Machine Name

보드 머신 이름을 설정한다. Xilinx 평가 보드(ZC702, ZCU102 등)를 사용하는 경우 BSP에 맞는 값이 자동으로 설정된다.

**커스텀 보드에서는 이 값을 변경하지 않는 것이 원칙이다.**

### 22.6.2 Extra dts/dtsi files

추가 DTS/DTSI 파일의 **절대 경로**를 공백으로 구분하여 지정한다. 지정된 파일은 디바이스 트리 빌드에 포함되며, 빌드 산출물이 `/boot/devicetree/` 디렉토리에 배포된다.

```
${PROOT}/project-spec/dts_dir/custom-board.dts ${PROOT}/project-spec/dts_dir/peripherals.dtsi
```

> **주의:** `Auto Config Settings → Device tree autoconfig`가 비활성화되어 있으면 이 설정은 무시된다.
> 

### Kernel Bootargs

커널 부팅 인자를 설정하는 하위 메뉴다.

**Auto generate 모드 (기본):** PetaLinux가 자동으로 bootargs를 생성한다.

```
console=ttyPS0,115200 earlyprintk
```

**User defined 모드:** 사용자가 직접 bootargs 문자열을 입력한다. NFS 부팅이나 특수한 커널 옵션이 필요한 경우에 사용한다.

```
console=ttyPS0,115200 root=/dev/nfs rw nfsroot=10.0.0.2:/nfsroot,v3,tcp ip=10.0.0.3:10.0.0.2:10.0.0.2:255.255.255.0::eth0:off earlyprintk
```

이 설정은 `system-conf.dtsi`의 `chosen` 노드 `bootargs` 속성에 반영된다.

단, `system-user.dtsi`에서 `chosen` 노드의 `bootargs`를 직접 override하는 것도 동일한 효과를 낸다.

### 22.6.4 Device Tree Overlay

이 옵션을 활성화하면 PL 관련 디바이스 트리(`pl.dtsi`)가 기본 DTB에 포함되지 않고, **별도의 overlay 파일(`pl.dtbo`)로 분리된다.**

**활성화 시 동작:**

- `pl.dtsi`가 `system.dtb`에서 분리됨
- `petalinux-build` 시 `images/linux/pl.dtbo` 파일이 생성됨
- 부팅 후 FPGA bitstream을 로드한 다음 `pl.dtbo`를 런타임으로 적용할 수 있음

이 기능은 챕터 24(디바이스 트리 오버레이)에서 상세히 다룬다.

> **참고:** FPGA Manager 옵션이 활성화되면 Device Tree Overlay 설정보다 우선한다.
> 

### Remove PL from Device Tree

PL IP를 전혀 사용하지 않거나, DTG가 PL IP 노드 생성 중 에러를 발생시키는 경우에 이 옵션을 활성화한다. 활성화하면 DTG가 PL 관련 노드를 일절 생성하지 않는다.

PS만 사용하는 프로젝트에서 빌드 시간을 단축하는 데도 유용하다.

### Auto Config Settings와의 관계

DTG Settings는 `Auto Config Settings → Device tree autoconfig`가 활성화되어 있어야 정상 동작한다.

```
petalinux-config
→ Auto Config Settings
   → [*] Device tree autoconfig    ← 이것이 활성화되어 있어야 함
```

이 옵션이 비활성화되면:

- `components/plnx_workspace/device-tree/device-tree/` 하위 파일이 재생성되지 않음
- DTG Settings의 `Extra dts/dtsi files`, `Kernel Bootargs` 설정이 무시됨
- 사용자가 디바이스 트리 소스를 완전히 수동 관리해야 함

일반적인 개발 환경에서는 이 옵션을 **활성화 상태로 유지**하는 것이 권장된다.

## 실습: `system-user.dtsi`에 Zybo LED/버튼 노드 추가

Linux 커널에 기본 탑재된 `gpio-leds`와 `gpio-keys` 드라이버를 활용하여, 코드 한 줄 작성 없이 디바이스 트리만으로 LED와 버튼을 제어하는 실습을 진행한다.

### `gpio-leds` 드라이버 바인딩

Linux 커널의 `gpio-leds` 드라이버는 디바이스 트리에서 `compatible = "gpio-leds"` 노드를 찾으면 자동으로 probe된다. 각 자식 노드가 하나의 LED를 나타낸다.

**필수/선택 속성:**

| 속성 | 타입 | 필수 | 설명 |
| --- | --- | --- | --- |
| `compatible` | string | ✅ | `"gpio-leds"` 고정 |
| `label` (자식) | string | 선택 | LED 이름 (`/sys/class/leds/<label>/`) |
| `gpios` (자식) | phandle + GPIO spec | ✅ | GPIO 컨트롤러 참조, 핀 번호, 활성 극성 |
| `linux,default-trigger` (자식) | string | 선택 | 기본 트리거 (`"heartbeat"`, `"timer"` 등) |
| `default-state` (자식) | string | 선택 | 초기 상태 (`"on"`, `"off"`, `"keep"`) |

### `gpio-keys` 드라이버 바인딩

`gpio-keys` 드라이버는 GPIO 핀에 연결된 버튼/스위치를 Linux input 이벤트로 변환한다.

**필수/선택 속성:**

| 속성 | 타입 | 필수 | 설명 |
| --- | --- | --- | --- |
| `compatible` | string | ✅ | `"gpio-keys"` 고정 |
| `label` (자식) | string | 선택 | 버튼 이름 |
| `gpios` (자식) | phandle + GPIO spec | ✅ | GPIO 컨트롤러 참조 |
| `linux,code` (자식) | u32 | ✅ | 입력 이벤트 코드 (`KEY_*` 매크로 값) |
| `debounce-interval` (자식) | u32 | 선택 | 디바운스 간격 (ms) |
| `wakeup-source` (자식) | bool | 선택 | 시스템 깨우기 가능 여부 |

### Zybo GPIO 핀 매핑 확인

Zybo Z7 보드의 LED와 버튼은 다음과 같이 PS GPIO(MIO)에 연결되어 있다:

| 보드 부품 | GPIO 컨트롤러 | 핀/채널 | 비고 |
| --- | --- | --- | --- |
| LD4 (PS LED) | `&gpio0` (PS MIO) | MIO 7 | PS에 직결 |
| BTN4 (PS 버튼) | `&gpio0` (PS MIO) | MIO 50 | PS에 직결 |
| BTN5 (PS 버튼) | `&gpio0` (PS MIO) | MIO 51 | PS에 직결 |
| LD0~LD3 | PL GPIO | — | AXI GPIO 필요 |
| BTN0~BTN3 | PL GPIO | — | AXI GPIO 필요 |
| SW0~SW3 | PL GPIO | — | AXI GPIO 필요 |

> PS GPIO에 직결된 LD4, BTN4, BTN5만으로 디바이스 트리 실습을 진행할 수 있다.
> 

### `system-user.dtsi` 작성

```
/include/ "system-conf.dtsi"
/ {
    /* ============================================= */
    /* gpio-leds: PS MIO에 직결된 LED 제어           */
    /* ============================================= */
    gpio-leds {
        compatible = "gpio-leds";

        /* Zybo PS LED (LD4) – MIO 7 */
        led_ld4: ld4 {
            label = "zybo:green:ld4";
            gpios = <&gpio0 7 0>;              /* &gpio0 = PS GPIO, 핀7, active-high */
            linux,default-trigger = "heartbeat";
            default-state = "off";
        };
    };

    /* ============================================= */
    /* gpio-keys: PS MIO에 직결된 버튼 이벤트 변환   */
    /* ============================================= */
    gpio-keys {
        compatible = "gpio-keys";

        /* Zybo PS 버튼 (BTN4) – MIO 50 */
        btn4 {
            label = "btn4";
            gpios = <&gpio0 50 0>;             /* &gpio0 = PS GPIO, 핀50, active-high */
            linux,code = <256>;                /* BTN_0 (input-event-codes.h) */
            debounce-interval = <50>;          /* 50ms 디바운스 */
        };

        /* Zybo PS 버튼 (BTN5) – MIO 51 */
        btn5 {
            label = "btn5";
            gpios = <&gpio0 51 0>;
            linux,code = <257>;                /* BTN_1 */
            debounce-interval = <50>;
        };
    };
};

/* Ethernet PHY 설정 (Zybo 보드 고유) */
&gem0 {
    phy-handle = <&phy0>;
    ps7_ethernet_0_mdio: mdio {
        phy0: phy@0 {
            compatible = "marvell,88e1510";   /* Zybo Z7 PHY 칩 */
            device_type = "ethernet-phy";
            reg = <0>;
        };
    };
};
```

**`gpios = <&gpio0 7 0>` 속성의 각 셀 의미:**

- `&gpio0`: PS GPIO 컨트롤러 노드에 대한 phandle 참조
- `7`: GPIO 핀 번호 (MIO 7)
- `0`: 플래그 (`0` = active-high, `1` = active-low)

`gpio0` 노드는 `zynq-7000.dtsi`에서 `#gpio-cells = <2>`로 정의되어 있으므로, phandle 뒤에 정확히 2개의 셀(핀 번호, 플래그)을 지정해야 한다.

**`linux,code` 값은 커널 헤더 `include/uapi/linux/input-event-codes.h`에 정의되어 있다:**

- `BTN_0 = 256 (0x100)`
- `BTN_1 = 257 (0x101)`
- `KEY_ENTER = 28`
- `KEY_POWER = 116`

### 빌드 및 적용

```bash
# PetaLinux 환경 활성화
source /opt/pkg/petalinux/settings.sh
cd <plnx-proj-root>

# 빌드
petalinux-build

# 생성된 DTB에서 노드 확인
dtc -I dtb -O dts images/linux/system.dtb | grep -A 10 "gpio-leds"
dtc -I dtb -O dts images/linux/system.dtb | grep -A 15 "gpio-keys"

# TFTP 서버 디렉토리에 DTB 배포
cp images/linux/system.dtb /tftpboot/

# Zybo 보드 재부팅
```

### 타겟 보드에서 동작 확인

**LED 확인:**

```bash
# gpio-leds 드라이버가 로드되었는지 확인
ls /sys/class/leds/
# 예상 출력: zybo:green:ld4

# heartbeat 트리거 동작 확인 (LED가 심장 박동처럼 깜빡인다)
cat /sys/class/leds/zybo:green:ld4/trigger
# 예상: ... [heartbeat] ...

# 수동 제어로 변경
echo none > /sys/class/leds/zybo:green:ld4/trigger
echo 1 > /sys/class/leds/zybo:green:ld4/brightness   # LED ON
echo 0 > /sys/class/leds/zybo:green:ld4/brightness   # LED OFF

# 다른 트리거 테스트 (주기적 점멸)
echo timer > /sys/class/leds/zybo:green:ld4/trigger
echo 500 > /sys/class/leds/zybo:green:ld4/delay_on   # ON 시간 500ms
echo 500 > /sys/class/leds/zybo:green:ld4/delay_off  # OFF 시간 500ms
```

**버튼 확인:**

```bash
# gpio-keys가 input 장치로 등록되었는지 확인
cat /proc/bus/input/devices
# 예상: Name="gpio-keys" 항목이 보여야 한다

# evtest로 이벤트 실시간 모니터링
evtest /dev/input/event0
# BTN4를 누르면:
# Event: type 1 (EV_KEY), code 256 (BTN_0), value 1   ← 누름
# Event: type 1 (EV_KEY), code 256 (BTN_0), value 0   ← 뗌

# evtest가 없는 경우 hexdump로 대체
hexdump /dev/input/event0
# BTN4를 누르면 바이너리 이벤트 데이터가 출력된다
```

**디바이스 트리 노드 확인:**

```bash
# /proc/device-tree에서 추가한 노드 확인
ls /proc/device-tree/gpio-leds/
# 예상: compatible  ld4/  name

ls /proc/device-tree/gpio-leds/ld4/
# 예상: compatible  default-state  gpios  label  linux,default-trigger  name

cat /proc/device-tree/gpio-leds/ld4/label
# 예상: zybo:green:ld4

cat /proc/device-tree/gpio-keys/btn4/label
# 예상: btn4
```

## `system-user.dtsi` 작성 시 자주 하는 실수와 해결법

### include 순서 오류

```
/* ✗ 잘못된 예: system-conf.dtsi include가 빠짐 */
/ {
    gpio-leds { /* ... */ };
};
```

`system-user.dtsi`는 반드시 `/include/ "system-conf.dtsi"`로 시작해야 한다. 이 include가 빠지면 `chosen` 노드의 `bootargs`나 `aliases` 설정이 누락될 수 있다.

### `#gpio-cells` 불일치

```
/* ✗ 잘못된 예: gpio0은 #gpio-cells = <2>인데 셀 3개를 지정 */
gpios = <&gpio0 7 0 1>;    /* 셀이 3개 – 에러 */

/* ✅ 올바른 예 */
gpios = <&gpio0 7 0>;      /* 셀이 2개 – 정상 */
```

GPIO specifier의 셀 개수는 해당 GPIO 컨트롤러의 `#gpio-cells` 값과 정확히 일치해야 한다. Zynq PS GPIO(`gpio0`)는 `#gpio-cells = <2>`이다.

### 노드 이름 충돌

```
/* ✗ 잘못된 예: 같은 이름의 노드를 루트에 두 번 선언 */
/ {
    gpio-leds { /* 첫 번째 */ };
    gpio-leds { /* 두 번째 – 첫 번째를 override해버림 */ };
};
```

같은 레벨에 같은 이름의 노드가 두 번 나타나면 나중 선언이 이전 선언을 덮어쓴다. 이도하지 않은 override를 방지하려면 노드 이름이 고유한지 확인해야 한다.

### bbappend 미등록

```
# 에러 메시지 예시 (petalinux-build 시)
ERROR: ... do_compile: zynq-leds.dtsi:1:1: fatal error: Could not open input file "zybo-leds.dtsi"
```

DTSI 파일을 만들고 `system-user.dtsi`에서 include했지만, `device-tree.bbappend`의 `SRC_URI`에 등록하지 않으면 빌드 시스템이 파일을 찾지 못한다. 앞서 설명한 3단계(파일 생성 → include 추가 → bbappend 등록)를 반드시 모두 완료해야 한다.

### 자동 생성 파일 직접 편집

```bash
# ✗ 절대 하지 말 것
nano components/plnx_workspace/device-tree/device-tree/pcw.dtsi
# 수정 후 petalinux-config를 실행하면 → 변경 내용 증발
```

어떤 급한 상황에서도 이 경로의 파일을 편집하면 안 된다. 실험적으로 수정해도 `petalinux-config`를 실행하는 순간 원래 상태로 되돌아간다.

### 문제 해결 요약표

| 증상 | 원인 | 해결 방법 |
| --- | --- | --- |
| 추가한 노드가 DTB에 없음 | `bbappend`에 `SRC_URI` 미등록 | `device-tree.bbappend`에 파일 추가 |
| 빌드 시 "file not found" | include 경로 오류 또는 bbappend 누락 | 파일명 철자, 경로 확인 |
| override가 적용 안 됨 | `autoconfig` 비활성화 | `petalinux-config → Auto Config → [*] Device tree autoconfig` |
| `petalinux-config` 후 수정 사라짐 | 자동 생성 파일을 직접 편집함 | `system-user.dtsi`에서 override |
| `gpio-leds` sysfs 미생성 | GPIO 핀 번호 오류 또는 커널 CONFIG 미활성화 | 핀 번호 확인, `CONFIG_LEDS_GPIO=y` 확인 |
| `gpio-keys` input 미등록 | 커널 CONFIG 미활성화 | `CONFIG_KEYBOARD_GPIO=y` 확인 |

## `/proc/device-tree/`를 이용한 런타임 DT 탐색

부팅 완료된 시스템에서 `/proc/device-tree/`(또는 `/sys/firmware/devicetree/base/`, 둘 다 같은 내용)를 통해 현재 로드된 디바이스 트리의 전체 내용을 탐색할 수 있다.

### 기본 탐색 명령

```bash
# 루트 노드의 자식 목록
ls /proc/device-tree/
# 예상: #address-cells  #size-cells  aliases  amba  chosen  cpus
#       gpio-keys  gpio-leds  memory  model  name ...

# 특정 노드의 속성 확인
ls /proc/device-tree/amba/serial@e0001000/
# 예상: compatible  interrupts  name  reg  status ...

# 문자열 속성 읽기
cat /proc/device-tree/amba/serial@e0001000/compatible
# 예상: xlnx,xuartps (NULL 문자가 포함된 바이트 배열)

# 정수 속성 읽기 (바이너리이므로 hexdump 사용)
hexdump -C /proc/device-tree/amba/serial@e0001000/reg
# 예상: 00000000  e0 00 10 00 00 00 10 00  → 주소 0xe0001000, 크기 0x1000
```

### `fdtdump` / `dtc` 역컴파일을 이용한 전체 조회

```bash
# 방법 1: fdtdump (간단한 출력)
fdtdump /sys/firmware/fdt 2>/dev/null | head -100

# 방법 2: dtc 역컴파일 (깔끔한 DTS 형식)
dtc -I fs /proc/device-tree/ 2>/dev/null | head -200

# 방법 3: 특정 노드만 역컴파일 (DTB 파일이 있는 경우)
dtc -I dtb -O dts /boot/system.dtb | grep -A 20 "gpio-leds"
```

### 실습: 현재 보드의 전체 GPIO 설정 확인

```bash
# PS GPIO 컨트롤러 노드 확인
ls /proc/device-tree/amba/gpio@e000a000/
cat /proc/device-tree/amba/gpio@e000a000/compatible
# 예상: xlnx,zynq-gpio-1.0

hexdump -C /proc/device-tree/amba/gpio@e000a000/\#gpio-cells
# 예상: 00000000  00 00 00 02  → 2셀

# gpio-leds 노드 확인 (system-user.dtsi에서 추가한 것)
ls /proc/device-tree/gpio-leds/
cat /proc/device-tree/gpio-leds/compatible
# 예상: gpio-leds

ls /proc/device-tree/gpio-leds/ld4/
cat /proc/device-tree/gpio-leds/ld4/label
# 예상: zybo:green:ld4
```

# CHAP 24. 디바이스 트리 오버레이 (DTO)

## Device Tree Overlay 개념

### 왜 오버레이가 필요한가

디바이스 트리(DTB)는 부팅 시점에 커널에 전달되며 하드웨어 구조를 인식하는 데 사용된다. 그런데 Zynq-7000과 같은 SoC 기반 시스템에서는 근본적인 문제가 존재한다: **FPGA의 Programmable Logic(PL)은 부팅 후에도 재구성(reconfiguration)이 가능하다는 점**이다.

기존 방식(정적 DTB)에서의 한계:

```
[부팅 시점]
┌──────────────────────────────────┐
│ Base DTB (PS + PL 노드 포함)     │
│ ├ PS: UART, GPIO, ETH ...        │
│ └ PL: AXI_GPIO, AXI_Timer        │ ← 부팅 시 PL 구성이 고정되어야 함
└──────────────────────────────────┘

문제: PL을 변경하면? → DTB를 다시 만들어 부팅해야 함
      → SD카드 교체 or TFTP 재전송 → 재부팅 필수
```

이 문제를 해결하는 메커니즘이 바로 **Device Tree Overlay(DTO)**다. DTO는 부팅된 이후, 즉 Linux가 이미 동작 중인 상태에서 DTB에 노드를 동적으로 추가하거나 제거하는 기능이다.

```
[부팅 시점]                          [런타임]
┌──────────────┐                    ┌──────────────┐
│ Base DTB     │                    │ Base DTB     │
│ (PS 노드만)  │ + overlay          │ (PS 노드)    │
│              │ ──────────→        │ + PL 노드 추가│
└──────────────┘                    └──────────────┘
                                      ↑ pl.dtbo 적용
```

### DTO의 핵심 원리

DTO는 **기존 DTB 위에 "패치"를 얹는 것**과 동일한 원리로 동작한다. Linux 커널 내부의 `of_overlay` 서브시스템이 이 기능을 담당한다.

**1단계: Base DTB 준비**

부팅 시 로드되는 DTB에는 PS(Processing System) 관련 노드만 포함한다. PL에 대한 노드는 의도적으로 제외한다. 대신 오버레이 지원을 위한 `__symbols__` 노드가 포함되어야 한다.

**2단계: Overlay DTB(DTBO) 생성**

PL에 배치된 IP 블록들의 정보를 담은 별도의 바이너리 파일(`.dtbo`)을 생성한다. 이 파일은 `fragment` 구조로 어떤 노드에 무엇을 추가할지를 기술한다.

**3단계: 런타임 적용**

Linux가 동작 중인 상태에서 DTBO 파일을 커널에 전달하면, `of_overlay` 서브시스템이 기존 DTB에 새 노드를 합산(merge)한다. 이 시점에 새로 추가된 노드의 `compatible` 문자열과 매칭되는 드라이버의 `probe()` 함수가 호출된다.

### Overlay DTS 문법

일반 DTS와 Overlay DTS는 문법이 약간 다르다. Overlay DTS는 `fragment` 구조를 사용하여 "어디에, 무엇을" 추가할지를 명시한다.

**일반 DTS (non-overlay) 방식:**

```
/* 일반 DTS — 전체 트리를 기술 */
/ {
    amba {
        my_gpio: gpio@41200000 {
            compatible = "xlnx,xps-gpio-1.00.a";
            reg = <0x41200000 0x10000>;
        };
    };
};
```

**Overlay DTS 방식:**

```
/* Overlay DTS — fragment 구조 사용 */
/dts-v1/;
/plugin/;       /* ← overlay임을 선언하는 핵심 키워드 */

/ {
    /* fragment@0: 기존 트리의 어떤 노드에 자식을 추가할지 지정 */
    fragment@0 {
        target = <&amba>;       /* Base DTB의 amba 노드를 타겟 지정 */
        __overlay__ {           /* 이 블록 안의 내용이 target에 합산됨 */
            my_gpio: gpio@41200000 {
                compatible = "xlnx,xps-gpio-1.00.a";
                reg = <0x41200000 0x10000>;
                #gpio-cells = <2>;
                gpio-controller;
            };
        };
    };
};
```

**핵심 키워드 정리:**

| 키워드 | 역할 |
| --- | --- |
| `/plugin/` | 이 DTS가 overlay(플러그인)임을 DTC 컴파일러에 알림 |
| `fragment@N` | N번째 오버레이 조각. 여러 fragment를 가질 수 있음 |
| `target = <&label>` | Base DTB에서 overlay를 적용할 대상 노드 지정 |
| `target-path = "/path"` | label 대신 경로로 타겟 지정 (label이 없을 때 사용) |
| `__overlay__ { }` | 타겟 노드에 합산될 실제 내용 |

### `__symbols__` 노드의 역할

Overlay가 `target = <&amba>` 처럼 label 참조를 사용하려면, Base DTB에 `__symbols__` 노드가 존재해야 한다. 이 노드는 DTS의 모든 label을 경로 문자열로 매핑하는 일종의 심볼 테이블이다.

```
/* Base DTB 내부의 __symbols__ 노드 (자동 생성) */
__symbols__ {
    amba  = "/amba";
    uart0 = "/amba/serial@e0001000";
    gpio0 = "/amba/gpio@e000a000";
};
```

DTC 컴파일 시 `-@` 옵션을 붙이면 `__symbols__` 노드가 자동 생성된다:

```bash
# Base DTB 컴파일 시 반드시 -@ 옵션 포함
dtc -@ -I dts -O dtb -o base.dtb base.dts

# Overlay DTBO 컴파일 시에도 -@ 옵션 포함
dtc -@ -I dts -O dtb -o overlay.dtbo overlay.dts
```

> PetaLinux에서는 Device tree overlay 옵션을 활성화하면 `-@` 옵션이 자동으로 적용된다.
> 

---

### Overlay 적용 시 내부 동작 흐름

```
[사용자 공간]
  DTBO 파일을 ConfigFS 또는 fpgautil 통해 커널에 전달
  │
  ▼
[커널 of_overlay 서브시스템]
  ① DTBO 바이너리 파싱 → fragment 구조체 추출
  ② fragment의 target(label or path) → Base DTB에서 대상 노드 탐색
  ③ __overlay__ 블록의 내용을 대상 노드에 합산(merge)
  ④ 새로 추가된 노드의 compatible 문자열 추출
  │
  ▼
[커널 Platform Bus]
  ⑤ 등록된 platform_driver 목록에서 compatible 매칭 검색
  ⑥ 매칭 성공 → 해당 드라이버의 probe() 함수 호출
  ⑦ probe()에서 리소스 획득(reg, interrupts 등) → 드라이버 초기화
  │
  ▼
[결과]
  /dev/gpio_device 같은 디바이스 노드가 생성되고 사용 가능
```

### Overlay 제거

오버레이는 적용뿐 아니라 제거도 가능하다. 제거 시에는 적용의 역순으로 동작한다:

1. 오버레이로 추가된 노드에 바인딩된 드라이버의 `remove()` 함수 호출
2. 드라이버가 점유한 리소스(IRQ, ioremap, cdev 등) 해제
3. Base DTB에서 합산되었던 노드를 분리(detach)

> **중요:** 여러 오버레이가 순차적으로 적용되었다면, 반드시 **LIFO(Last In, First Out)** 순서로 제거해야 한다. 가장 나중에 적용된 오버레이를 가장 먼저 제거해야 한다. 그렇지 않으면 의존성 충돌로 커널 경고 또는 오류가 발생할 수 있다.
> 

## PetaLinux에서 `pl.dtbo` 생성 절차

### 두 가지 경로: Device Tree Overlay vs FPGA Manager

| 항목 | Device Tree Overlay | FPGA Manager |
| --- | --- | --- |
| 활성화 위치 | DTG Settings → Device tree overlay | FPGA Manager → Fpga Manager |
| 산출물 | `pl.dtbo` (images/linux/) | `pl.dtbo` + `*.bit.bin` (/lib/firmware/xilinx/) |
| Bitstream 포함 | 별도 관리 (사용자가 직접 변환·배치) | 자동으로 `.bin` 변환 후 RootFS에 패킹 |
| 적용 방법 | ConfigFS 수동 적용 | `fpgautil` 명령으로 Bitstream + DTBO 동시 적용 |
| 우선순위 | FPGA Manager가 활성화되면 무시됨 | 최우선 |
| 주요 용도 | DTBO만 별도 관리할 때 | Bitstream + DTBO를 통합 관리할 때 **(권장)** |

> **핵심 규칙:** FPGA Manager가 활성화되면 Device tree overlay 설정은 무시된다.
> 

### 방법 1: Device Tree Overlay 단독 사용

학습 목적으로 DTO의 동작 원리를 이해하기에 적합한 방법이다.

```bash
# ① petalinux-config에서 Device tree overlay 활성화
petalinux-config
# → DTG Settings → [*] Device tree overlay → Save → Exit

# ② pl-custom.dtsi 수정 (PL 노드 커스터마이징)
nano project-spec/meta-user/recipes-bsp/device-tree/files/pl-custom.dtsi

# pl-custom.dtsi 예시: AXI GPIO 노드 속성 추가
&axi_gpio_0 {
    xlnx,gpio-width = <4>;
    xlnx,all-outputs = <1>;
};

# ③ 빌드 실행 → images/linux/pl.dtbo 생성
petalinux-build

# ④ DTBO 내용 역컴파일로 확인
dtc -I dtb -O dts images/linux/pl.dtbo
```

### 방법 2: FPGA Manager 사용 (권장)

실제 제품 개발에서 권장되는 표준 방법이다. Bitstream과 DTBO를 통합 관리한다.

```bash
# ① petalinux-config에서 FPGA Manager 활성화
petalinux-config
# → FPGA Manager → [*] Fpga Manager → Save → Exit
```

FPGA Manager 활성화 시 PetaLinux가 자동으로 수행하는 작업: `pl.dtsi` 노드를 overlay(DTBO) 형식으로 생성하고, Bitstream을 `.bit → .bin` 형식으로 자동 변환하며, DTBO와 `.bin`을 RootFS의 `/lib/firmware/xilinx/base/` 디렉토리에 패킹한다.

```bash
# ② 빌드
petalinux-build

# ③ 산출물 확인 — 부팅 후 타겟에서:
ls /lib/firmware/xilinx/base/
# design_1_wrapper.bit.bin ← 자동 변환된 Bitstream
# pl.dtbo                  ← PL 디바이스 트리 오버레이
```

### 추가 XSA를 FPGA Manager에 등록

여러 PL 구성(Bitstream)을 전환해야 하는 경우:

```bash
# 방법 A: petalinux-config에서 하드웨어 디렉토리 지정
petalinux-config
# → FPGA Manager → Specify hardware directory path → /path/to/extra_xsa_directory/

# 방법 B: petalinux-create 명령으로 앱 생성
petalinux-create -t apps \
    --template fpgamanager_dtg \
    -n can-interface \
    --srcuri /path/to/can_system.xsa \
    --enable
```

## FPGA Manager와 Bitstream 관리

### FPGA Manager 개요

FPGA Manager는 Linux 커널에서 FPGA의 PL을 재구성하기 위한 표준 인터페이스다. Zynq-7000에서는 PS 내부의 **DevC(Device Configuration)** 컨트롤러가 이 역할을 수행한다.

```
Zynq-7000의 PL 구성 경로:
Bitstream(.bin) → PS DevC 컨트롤러 → PCAP 인터페이스 → PL 구성
                        │
                        └── Linux FPGA Manager 드라이버가 제어
```

```bash
# FPGA Manager 상태 확인
cat /sys/class/fpga_manager/fpga0/name   # zynq_fpga_manager
cat /sys/class/fpga_manager/fpga0/state  # operating
```

### Bitstream `.bit` → `.bin` 변환

Linux FPGA Manager는 Xilinx의 기본 `.bit` 포맷을 직접 인식하지 못한다. `bootgen` 도구를 사용하여 변환한다.

```bash
# ① BIF(Boot Image Format) 파일 작성
cat > bitstream.bif << 'EOF'
all:
{
    [destination_device = pl] design_1_wrapper.bit
}
EOF

# ② bootgen으로 변환 실행 (Zynq-7000)
bootgen -image bitstream.bif -arch zynq -process_bitstream bin

# 생성 확인
ls -la design_1_wrapper.bit.bin
```

> **중요:** `.bin` 파일의 이름은 `pl.dtsi`에서 `firmware-name` 속성으로 지정된 이름과 정확히 일치해야 한다.
> 

### `fpgautil` 명령어

PetaLinux RootFS에 기본 포함되는 `fpgautil` 유틸리티는 Bitstream 로드와 DTO 적용을 한 번에 수행한다.

```bash
# Bitstream + DTBO 동시 로드
fpgautil -b /lib/firmware/xilinx/base/design_1_wrapper.bit.bin \
         -o /lib/firmware/xilinx/base/pl.dtbo
```

**`fpgautil` 주요 옵션:**

| 옵션 | 설명 |
| --- | --- |
| `-b <file>` | Bitstream `.bin` 파일 경로 |
| `-o <file>` | Overlay DTBO 파일 경로 |
| `-f Full` | Full Bitstream 로드 (기본값) |
| `-f Partial` | Partial Reconfiguration 용 |
| `-n <name>` | FPGA Region 이름 지정 |
| `-R` | 현재 적용된 Overlay 제거 |

### Zynq-7000 특이사항

1. **Full Reconfiguration만 지원:** Zynq-7000의 DevC 컨트롤러는 Partial Reconfiguration을 직접 지원하지 않는다.
2. **PL 구성과 PS 연결:** PL을 재구성하면 AXI 인터커넥트가 일시적으로 끊어진다. PL 재구성 전에 관련 드라이버를 모두 해제(remove)하는 것이 안전하다.
3. **DevC 드라이버:** Zynq-7000의 FPGA Manager는 커널의 `zynq-fpga.c` 드라이버가 담당한다. 디바이스 트리에서 `compatible = "xlnx,zynq-devcfg-1.0"` 노드로 표현된다.

## 런타임 DTO 적용 — ConfigFS

### ConfigFS 개요

ConfigFS(Configuration Filesystem)는 Linux 커널이 제공하는 사용자 공간 → 커널 설정 인터페이스다. sysfs가 "커널 → 사용자"로 정보를 노출하는 것과 반대로, ConfigFS는 "사용자 → 커널"로 설정을 주입하는 역할을 한다.

Device Tree Overlay에서 ConfigFS는 다음 경로에서 동작한다:

```
/sys/kernel/config/device-tree/overlays/
```

이 경로에 디렉토리를 생성하면 오버레이 슬롯이 만들어지고, 해당 디렉토리의 `dtbo` 파일에 DTBO 바이너리를 쓰면 오버레이가 적용된다.

### ConfigFS 마운트 확인

```bash
# ConfigFS 마운트 상태 확인
mount | grep configfs

# 마운트 안 되어 있으면 수동 마운트
sudo mount -t configfs configfs /sys/kernel/config

# Device tree overlay 지원 확인
ls /sys/kernel/config/device-tree/overlays/
```

경로가 존재하지 않는다면 커널에 DTO 지원이 비활성화된 것이다:

```bash
# PetaLinux 커널 설정
petalinux-config -c kernel
# Device Drivers → Device Tree and Open Firmware support
# → [*] Device Tree overlays
```

### ConfigFS를 이용한 DTO 수동 적용

```bash
# ① DTBO 파일을 타겟 보드에 배치
# (호스트) cp pl.dtbo /nfsroot/lib/firmware/

# ② 오버레이 슬롯 생성
mkdir /sys/kernel/config/device-tree/overlays/my_pl_overlay

# 슬롯 내부 파일 역할
# dtbo   → DTBO 바이너리를 쓰는 대상 (write)
# path   → DTBO 파일 경로를 문자열로 쓰는 방식 (대안)
# status → 현재 상태: unapplied / applied

# ③ DTBO 적용 (방법 A: 권장)
cat /lib/firmware/pl.dtbo > \
    /sys/kernel/config/device-tree/overlays/my_pl_overlay/dtbo

# ④ 적용 상태 확인
cat /sys/kernel/config/device-tree/overlays/my_pl_overlay/status
# applied

dmesg | tail -20
# [ xxx] OF: overlay: ... applied
# [ xxx] gpio-xilinx 41200000.gpio: ... ← 드라이버 probe() 호출됨
```

### ConfigFS를 이용한 DTO 제거

```bash
# 오버레이 슬롯 디렉토리를 삭제하면 제거됨
rmdir /sys/kernel/config/device-tree/overlays/my_pl_overlay

# dmesg에서 드라이버 remove() 호출 확인
dmesg | tail -10
# [ xxx] gpio-xilinx 41200000.gpio: ... removed
```

### ConfigFS DTO 적용·제거 자동화 스크립트

```bash
#!/bin/bash
# dto_manager.sh — DTO 적용/제거 관리 스크립트
# 사용법: ./dto_manager.sh apply|remove [overlay_name] [dtbo_path]

OVERLAY_BASE="/sys/kernel/config/device-tree/overlays"
ACTION=$1
NAME=${2:-"pl_overlay"}
DTBO_PATH=${3:-"/lib/firmware/pl.dtbo"}

case $ACTION in
    apply)
        if [ -d "${OVERLAY_BASE}/${NAME}" ]; then
            echo "오류: overlay '${NAME}' 이 이미 존재한다. 먼저 제거하라."
            exit 1
        fi
        mkdir "${OVERLAY_BASE}/${NAME}"
        cat "${DTBO_PATH}" > "${OVERLAY_BASE}/${NAME}/dtbo"
        STATUS=$(cat "${OVERLAY_BASE}/${NAME}/status")
        echo "Overlay '${NAME}' 상태: ${STATUS}"
        ;;
    remove)
        if [ ! -d "${OVERLAY_BASE}/${NAME}" ]; then
            echo "오류: overlay '${NAME}' 이 존재하지 않는다."
            exit 1
        fi
        rmdir "${OVERLAY_BASE}/${NAME}"
        echo "Overlay '${NAME}' 제거 완료."
        ;;
    status)
        if [ -d "${OVERLAY_BASE}/${NAME}" ]; then
            cat "${OVERLAY_BASE}/${NAME}/status"
        else
            echo "overlay '${NAME}' 미존재"
        fi
        ;;
    *)
        echo "사용법: $0 apply|remove|status [overlay_name] [dtbo_path]"
        exit 1
        ;;
esac
```

### ConfigFS vs `fpgautil` 비교

| 항목 | ConfigFS 수동 적용 | `fpgautil` |
| --- | --- | --- |
| Bitstream 로드 | 별도 수행 필요 | `-b` 옵션으로 자동 로드 |
| DTBO 적용 | `cat dtbo > ...` 수동 | `-o` 옵션으로 자동 적용 |
| 제거 | `rmdir` 수동 | `-R` 옵션 |
| 순서 제어 | 사용자가 직접 관리 | 도구가 자동 관리 |
| 유연성 | 높음 (세밀한 제어 가능) | 중간 (표준 흐름 자동화) |
| 적합한 용도 | 학습, 디버깅, 특수 시나리오 | 일반적인 PL 재구성 |

## 실습: AXI GPIO IP를 DTO로 런타임 로드

### 실습 개요

```
[목표]
  Zybo 보드의 4개 LED(LD0~LD3)를 AXI GPIO IP를 통해 제어.
  부팅 후 런타임에 PL을 로드하고, gpio-xilinx 드라이버가 자동 바인딩되는 것을 확인.

[전체 흐름]
  Vivado: AXI GPIO Block Design → Bitstream 생성 → XSA Export
      ↓
  PetaLinux: XSA Import → FPGA Manager 설정 → 빌드 → pl.dtbo + .bit.bin 생성
      ↓
  타겟 보드: 부팅(Base DTB만) → fpgautil로 Bitstream+DTBO 로드
      → /sys/class/gpio/ 에 AXI GPIO 노드 출현 → LED 제어
```

### Step 1: Vivado Block Design 생성

**① Vivado 프로젝트 생성:**

```
Vivado → Create Project → RTL Project
  Project Name: zybo_axi_gpio
  Default Part → Boards → Zybo Z7-10
```

**② Block Design 및 Zynq PS 추가:**

```
IP INTEGRATOR → Create Block Design → Design name: system
Diagram 창 우클릭 → Add IP → ZYNQ7 Processing System 추가
Run Block Automation → Apply Board Preset → OK
```

**③ AXI GPIO IP 추가 및 설정:**

```
Add IP → AXI GPIO 더블클릭:
  → GPIO Width: 4
  → All Outputs: 체크
```

**④ Connection Automation 실행:**

```
Run Connection Automation → /axi_gpio_0/S_AXI → OK
Run Connection Automation → /axi_gpio_0/GPIO → leds_4bits → OK
```

**⑤ 설계 검증 및 Bitstream 생성:**

```
Tools → Validate Design → 성공 확인
Sources → system 우클릭 → Create HDL Wrapper
Flow Navigator → Generate Bitstream
File → Export → Export Hardware (Include bitstream)
```

### Step 2: PetaLinux 프로젝트에 XSA 임포트 및 FPGA Manager 설정

```bash
cd /workspace/Zybo-Z7-10-Petalinux-2022-1
source /opt/pkg/petalinux/settings.sh

# XSA 임포트
petalinux-config --get-hw-description=~/vivado_projects/zybo_axi_gpio/

# FPGA Manager 활성화
petalinux-config
# → FPGA Manager → [*] Fpga Manager → Save → Exit

# 빌드
petalinux-build

# 산출물 확인
ls -la images/linux/pl.dtbo
```

**역컴파일 결과 예시 (PetaLinux 2022.1):**

```
/dts-v1/;
/plugin/;
/ {
    /* fragment@0: fpga-full 노드에 Bitstream 지정 */
    fragment@0 {
        target = <&fpga_full>;
        overlay0: __overlay__ {
            firmware-name = "design_1_wrapper.bit.bin";
        };
    };
    /* fragment@1: amba 버스에 PL 클럭 노드 추가 */
    fragment@1 {
        target = <&amba>;
        overlay1: __overlay__ {
            clocking0: clocking0 {
                compatible = "xlnx,fclk";
                /* ... */
            };
        };
    };
    /* fragment@2: amba 버스에 AXI GPIO 노드 추가 */
    fragment@2 {
        target = <&amba>;
        overlay2: __overlay__ {
            axi_gpio_0: gpio@41200000 {
                compatible = "xlnx,axi-gpio-2.0", "xlnx,xps-gpio-1.00.a";
                reg = <0x41200000 0x10000>;
                gpio-controller;
                xlnx,gpio-width = <0x4>;
            };
        };
    };
};
```

### PetaLinux 2022.1 `pl.dtbo` `#address-cells` 버그 수정

**버그 원인:**

PetaLinux 2022.1의 XSCT는 Zynq-7000(32-bit) 프로젝트임에도 `fragment@0`에 MPSoC(64-bit) 기준값을 잘못 적용한다.

```
pl.dtsi (버그 있는 상태):          system.dtb의 fpga-full 노드:
fragment@0.__overlay__ {           fpga-full {
    #address-cells = <2>; ← 오류      #address-cells = <0x01>; ← 올바름
    #size-cells = <2>;    ← 오류      #size-cells = <0x01>;
}                                  }
```

ConfigFS로 overlay 적용 시:

```
OF: overlay: ERROR: changing value of #address-cells is not allowed in /fpga-full
create_overlay: Failed to create overlay (err=-22)
```

**해결 방법: bbappend 후처리 패치**

`pl.dtsi`를 직접 수정하면 `petalinux-build` 시 XSCT가 덮어쓰기 때문에, Yocto bbappend를 통해 자동으로 패치를 적용한다.

**Step 1: bbappend 파일 생성:**

```bash
mkdir -p project-spec/meta-user/recipes-bsp/device-tree/
cat > project-spec/meta-user/recipes-bsp/device-tree/device-tree.bbappend << 'EOF'
do_configure:append() {
    PL_DTSI="${TOPDIR}/../components/plnx_workspace/device-tree/device-tree/pl.dtsi"
    if [ -f "${PL_DTSI}" ]; then
        sed -i 's/#address-cells = <2>;/#address-cells = <1>;/' ${PL_DTSI}
        sed -i 's/#size-cells = <2>;/#size-cells = <1>;/' ${PL_DTSI}
    fi
}
EOF
```

**Step 2: device-tree 재빌드:**

```bash
petalinux-build -c device-tree -x cleansstate
petalinux-build -c device-tree
```

**Step 3: 수정 결과 확인:**

```bash
grep "address-cells\|size-cells" \
    components/plnx_workspace/device-tree/device-tree/pl.dtsi
# #address-cells = <1>;
# #size-cells = <1>;

# fragment@0의 #address-cells가 <0x01>로 출력되면 정상
dtc -I dtb -O dts images/linux/pl.dtbo 2>/dev/null | grep -A5 "fragment@0"
```

> bbappend는 `petalinux-build` 전체 빌드 시에도 자동으로 실행되므로, 이후 XSA 변경 및 재빌드 시에도 별도 작업 없이 패치가 유지된다.
> 

### Step 3: 부팅 이미지 배포

```bash
# 커널 이미지와 Base DTB를 TFTP에 배포
cp images/linux/uImage     /tftpboot/
cp images/linux/system.dtb /tftpboot/

# RootFS를 NFS에 배포
sudo rm -rf /nfsroot/*
sudo tar -xzf images/linux/rootfs.tar.gz -C /nfsroot/

# DTBO를 NFS rootfs에 배치
sudo mkdir -p /nfsroot/lib/firmware/xilinx/base/
sudo cp images/linux/pl.dtbo /nfsroot/lib/firmware/xilinx/base/

# 산출물 확인 (Bitstream은 FPGA Manager 빌드 시 이미 rootfs.tar.gz 안에 포함)
ls /nfsroot/lib/firmware/xilinx/base/
# design_1_wrapper.bit.bin  ← FPGA Manager가 자동 변환하여 포함
# pl.dtbo                   ← 방금 복사한 파일

# 심볼릭 링크 생성 (필수!)
# 커널 firmware loader는 /lib/firmware/ 기준으로 파일을 탐색한다.
# firmware-name = "design_1_wrapper.bit.bin"이므로
# /lib/firmware/ 바로 아래에 심볼릭 링크가 없으면 err=-2 에러가 발생한다.
sudo ln -s /lib/firmware/xilinx/base/design_1_wrapper.bit.bin \
           /nfsroot/lib/firmware/design_1_wrapper.bit.bin
```

> **주의:** `rootfs.tar.gz`가 없으면 `petalinux-config → Image Packaging Configuration → Root filesystem type → [*] tar.gz` 활성화 후 재빌드한다. FPGA Manager 활성화 시 `bootgen`을 별도로 실행할 필요가 없다.
> 

### Step 4: 타겟 보드에서 런타임 DTO 적용

```bash
# ① 부팅 후 현재 상태 확인 (PL 관련 디바이스 없는 것을 확인)
ls /sys/class/gpio/
# gpiochip906 = PS GPIO (118핀). AXI GPIO는 아직 없음

# ② fpgautil로 Bitstream + DTBO 동시 로드
fpgautil -b /lib/firmware/xilinx/base/design_1_wrapper.bit.bin \
         -o /lib/firmware/xilinx/base/pl.dtbo

# ③ (권장) ConfigFS로 수동 적용
mkdir /sys/kernel/config/device-tree/overlays/pl
cat /lib/firmware/xilinx/base/pl.dtbo > \
    /sys/kernel/config/device-tree/overlays/pl/dtbo

# 적용 상태 확인
cat /sys/kernel/config/device-tree/overlays/pl/status
# applied
```

> **WARNING 메시지 설명:** `memory leak will occur if overlay removed` 경고는 Zynq Linux 커널의 알려진 동작이다. PL 로딩 및 overlay 적용 자체에는 영향을 주지 않으므로 정상 동작으로 간주하고 무시한다.
> 

### Step 5: AXI GPIO를 통한 LED 제어 실습

```bash
# 새 gpiochip 확인
ls /sys/class/gpio/
# gpiochip906: PS GPIO (ngpio=118, 기존)
# gpiochip902: AXI GPIO (ngpio=4, 새로 추가됨)

# base 번호 확인 (재부팅마다 달라질 수 있음)
BASE=$(cat /sys/class/gpio/gpiochip902/base)

# LED0 export 및 제어
echo $BASE > /sys/class/gpio/export
echo out > /sys/class/gpio/gpio${BASE}/direction
echo 1 > /sys/class/gpio/gpio${BASE}/value   # LED ON
echo 0 > /sys/class/gpio/gpio${BASE}/value   # LED OFF

# 전체 LED 순차 점멸
for i in 0 1 2 3; do
    PIN=$((BASE + i))
    echo $PIN > /sys/class/gpio/export 2>/dev/null
    echo out > /sys/class/gpio/gpio${PIN}/direction
done

for iter in $(seq 1 5); do
    for i in 0 1 2 3; do
        PIN=$((BASE + i))
        echo 1 > /sys/class/gpio/gpio${PIN}/value
        sleep 0.2
        echo 0 > /sys/class/gpio/gpio${PIN}/value
    done
done
```

**AXI GPIO base 번호 동적 탐색 스크립트:**

```bash
#!/bin/sh
AXI_GPIO_ADDR="41200000"
for chip in /sys/class/gpio/gpiochip*; do
    label=$(cat $chip/label 2>/dev/null)
    if echo "$label" | grep -q "$AXI_GPIO_ADDR"; then
        BASE=$(cat $chip/base)
        echo "AXI GPIO found: base=$BASE"
        echo $BASE > /sys/class/gpio/export
        echo out > /sys/class/gpio/gpio${BASE}/direction
        break
    fi
done
```

```bash
# DTO 제거 후 확인
fpgautil -R
# 또는: rmdir /sys/kernel/config/device-tree/overlays/pl

ls /sys/class/gpio/
# gpiochip906 만 존재 — AXI GPIO gpiochipN이 사라짐
```

### DTO 적용 시 발생 가능한 문제와 해결

| 증상 | 원인 | 해결 방법 |
| --- | --- | --- |
| `cat dtbo > ...` 시 `err=-2 (ENOENT)` | firmware loader가 `/lib/firmware/` 기준으로 Bitstream 파일을 찾지 못함 | 호스트에서 `/nfsroot/lib/firmware/`에 심볼릭 링크 생성 |
| `cat dtbo > ...` 시 `err=-22 (EINVAL)` + `#address-cells is not allowed` | PetaLinux 2022.1 XSCT 버그: `fragment@0`에 MPSoC(64-bit) 기준값 `<2>` 생성 | 24.5.3a절의 bbappend 적용 후 재빌드 |
| `cat dtbo > ...` 시 "Invalid argument" | DTBO 손상 또는 Base DTB와 호환 불가 | `dtc`로 DTBO 역컴파일 확인. `__symbols__` 노드 존재 확인 |
| `mkdir overlays/pl` 불가 | ConfigFS 미마운트 또는 커널 DTO 미지원 | `mount -t configfs configfs /sys/kernel/config` 실행. 커널 `CONFIG_OF_OVERLAY=y` 확인 |
| DTBO 적용됐지만 `/dev/` 노드 미생성 | 드라이버 미로드 또는 `compatible` 불일치 | `dmesg`에서 에러 확인. `modprobe gpio-xilinx` 시도 |
| Bitstream 로드 실패 | `.bin` 포맷 오류 또는 디바이스 불일치 | `bootgen`으로 재변환. 타겟 디바이스 확인 |
| DTO 제거 시 커널 WARNING | 드라이버가 리소스를 완전히 해제하지 못함 | GPIO를 먼저 `unexport`한 후 DTO 제거 |
| 여러 overlay 적용 후 제거 시 오류 | LIFO 순서 미준수 | 가장 나중에 적용한 overlay부터 먼저 제거 |
| `rootfs.tar.gz` 파일이 없음 | PetaLinux 빌드 설정에서 tar.gz 출력 비활성화 | `petalinux-config → Image Packaging → [*] tar.gz` 활성화 후 재빌드 |
| 재부팅 후 심볼릭 링크 사라짐 | 보드에서 임시 생성한 링크는 NFS rootfs에 반영되지 않음 | 반드시 호스트에서 `/nfsroot/lib/firmware/`에 링크를 생성 |

## 심화: 커스텀 Platform Driver + DTO 연동

### 챕터 23의 LED Platform Driver를 DTO로 전환

핵심 변경은 DTS 노드를 Base DTB가 아닌 Overlay DTBO에 넣는 것이다.

**기존 방식 (정적 DTB, 챕터 23):**

```
/* system-user.dtsi에 직접 노드 추가 */
/ {
    amba {
        led_ctrl: led-ctrl@e000a000 {
            compatible = "zybo,led-ctrl";
            reg = <0xe000a000 0x1000>;
            status = "okay";
        };
    };
};
```

**DTO 방식:**

```
/* led_overlay.dts — 별도 overlay 파일로 분리 */
/dts-v1/;
/plugin/;

/ {
    fragment@0 {
        target-path = "/amba";
        __overlay__ {
            led_ctrl: led-ctrl@e000a000 {
                compatible = "zybo,led-ctrl";
                reg = <0xe000a000 0x1000>;
                status = "okay";
            };
        };
    };
};
```

```bash
# 호스트에서 DTBO 컴파일
dtc -@ -I dts -O dtb -o led_overlay.dtbo led_overlay.dts

# NFS로 타겟에 배포
cp led_overlay.dtbo /nfsroot/lib/firmware/

# 타겟에서 적용
mkdir /sys/kernel/config/device-tree/overlays/led_ctrl
cat /lib/firmware/led_overlay.dtbo > \
    /sys/kernel/config/device-tree/overlays/led_ctrl/dtbo

# 드라이버 probe() 호출 확인
dmesg | grep led-ctrl
# [ xxx] led-ctrl e000a000.led-ctrl: probe() called
```

### DTO와 Platform Driver의 생명주기

```
[DTO 적용 시]
  ConfigFS에 DTBO 쓰기
  → of_overlay가 노드 추가
  → platform_bus가 compatible 매칭
  → 드라이버의 probe() 호출

[DTO 제거 시]
  ConfigFS 슬롯 디렉토리 삭제
  → of_overlay가 노드 제거 요청
  → 드라이버의 remove() 호출
  → of_overlay가 노드 분리
```

DTO 환경에서는 `remove()`가 런타임에 언제든 호출될 수 있으므로 `devm_*` API 사용이 특히 중요하다:

```c
static int led_ctrl_remove(struct platform_device *pdev)
{
    struct led_ctrl_data *data = platform_get_drvdata(pdev);

    /* ① 하드웨어 비활성화 */
    writel(0x00, data->base + GPIO_DATA_OFFSET);

    /* ② cdev 제거 및 디바이스 노드 삭제 */
    device_destroy(data->class, data->devno);
    class_destroy(data->class);
    cdev_del(&data->cdev);
    unregister_chrdev_region(data->devno, 1);

    /* ③ devm_ioremap_resource()로 매핑했다면 자동 해제 */
    pr_info("led-ctrl: remove() completed\n");
    return 0;
}
```

## Zynq-7000의 Partial Reconfiguration과 DTO

### Full Reconfiguration vs Partial Reconfiguration

| 항목 | Full Reconfiguration | Partial Reconfiguration |
| --- | --- | --- |
| 재구성 범위 | PL 전체 | PL 일부(Reconfigurable Partition) |
| 동작 중단 | PL 전체 중단 | 재구성 영역만 중단 |
| Zynq-7000 지원 | DevC 경유, FPGA Manager 직접 지원 | ICAP 경유, 별도 드라이버 필요 |
| Zynq UltraScale+ | PCAP 경유, FPGA Manager 직접 지원 | DFX 방식으로 지원 |
| DTO 연동 | 이 챕터에서 다룬 방법 그대로 적용 | 각 Partition별 별도 DTBO |

### PL 제거 옵션

PL IP를 전혀 사용하지 않는 프로젝트에서는 PL 노드를 아예 생성하지 않을 수 있다:

```bash
petalinux-config
# → DTG Settings → [*] Remove PL from device tree
```

주로 사용하는 상황: PS 기능만 사용하는 소프트웨어 개발 초기 단계, DTG가 특정 PL IP에서 에러를 발생시킬 때 임시 우회.

> **주의:** FPGA Manager가 활성화되면 이 옵션도 무시된다.
> 

## 실습: `gpio-leds` DTO로 LED 드라이버 런타임 활성화

> **대응 예제:** `43_led_overlay`
> 

24.5절의 AXI GPIO 실습과 달리, 이 절에서는 PS GPIO를 `gpio-leds` 드라이버와 연결하는 DTO를 만들고 런타임으로 적용·제거하는 방법을 다룬다. **커스텀 드라이버 코드가 전혀 필요 없다는 점이 핵심이다.**

### `led_overlay.dts` 전체 분석

```
/dts-v1/;
/plugin/;

/ {
    fragment@0 {
        target-path = "/";          /* 루트 노드 아래에 직접 노드 추가 */
        __overlay__ {
            gpio-leds {
                compatible = "gpio-leds";

                led1 {
                    label = "led1";
                    gpios = <&gpio 7 0>;    /* MIO7 = Zybo LD0 */
                    default-state = "off";
                };
                led2 {
                    label = "led2";
                    gpios = <&gpio 8 0>;    /* MIO8 = Zybo LD1 */
                    default-state = "off";
                };
            };
        };
    };
};
```

**`target = <&amba>` vs `target-path = "/"` 선택 이유:**

`gpio-leds` 노드는 메모리 맵 주소(`reg`)가 없는 노드이므로 amba(simple-bus) 아래에 넣을 필요가 없다. 루트 `/` 아래에 직접 추가하는 것이 올바른 위치다.

| 구분 | `target = <&label>` | `target-path = "/path"` |
| --- | --- | --- |
| 전제 조건 | Base DTB에 해당 label이 존재해야 함 | 해당 경로가 존재하면 됨 |
| `__symbols__` 의존 | 있음 | 없음 |
| 주요 용도 | 특정 버스 노드 하위에 추가 | 루트나 경로로 직접 지정 |

### DTO 컴파일 — `f`와 `@` 플래그

```bash
# Ubuntu VM에서
sudo apt install -y device-tree-compiler

# Overlay DTS 컴파일
# -f: 경고를 무시하고 강제 컴파일 (target-path 방식에서 일부 dtc 버전 경고 발생)
# -@: __symbols__ 노드 생성
dtc -f -@ -I dts -O dtb -o led_overlay.dtbo led_overlay.dts

# 결과 확인
dtc -I dtb -O dts led_overlay.dtbo   # 역컴파일로 내용 확인

# NFS rootfs에 배포
cp led_overlay.dtbo /nfsroot/lib/firmware/
```

### ConfigFS로 DTO 런타임 적용

```bash
# 현재 /sys/class/leds/ 비어있는 것 확인 (gpio-leds 드라이버 미활성화)
ls /sys/class/leds/

# DTO 적용
mkdir /sys/kernel/config/device-tree/overlays/gpio_leds
cat /lib/firmware/led_overlay.dtbo > \
    /sys/kernel/config/device-tree/overlays/gpio_leds/dtbo

# 상태 확인
cat /sys/kernel/config/device-tree/overlays/gpio_leds/status
# applied

# dmesg에서 leds-gpio 드라이버 probe() 확인
dmesg | tail -5
# [ xxx] leds-gpio gpio-leds: registered LED led1
# [ xxx] leds-gpio gpio-leds: registered LED led2

# /sys/class/leds/ 에 노드 생성 확인
ls /sys/class/leds/
# led1  led2

# LED 제어
echo 1 > /sys/class/leds/led1/brightness      # LD0 ON
echo 0 > /sys/class/leds/led1/brightness      # LD0 OFF
echo heartbeat > /sys/class/leds/led2/trigger  # LD1 심박 점멸
```

### DTO 제거 — 런타임 드라이버 언로드

```bash
# DTO 제거 전: trigger를 none으로 복원 (안전을 위해)
echo none > /sys/class/leds/led1/trigger
echo none > /sys/class/leds/led2/trigger

# DTO 제거
rmdir /sys/kernel/config/device-tree/overlays/gpio_leds

# /sys/class/leds/ 가 비워졌는지 확인
ls /sys/class/leds/
# (비어 있음 — gpio-leds 드라이버 remove() 호출됨)
```

### DTS 직접 수정 (21.9절) vs DTO (24.8절) 비교

| 항목 | 21.9절: DTS 직접 수정 | 24.8절: DTO |
| --- | --- | --- |
| 적용 시점 | 부팅 시 (DTB를 TFTP로 전송) | 런타임 (ConfigFS에 쓰기) |
| 재부팅 필요 | 필요 (새 DTB로 부팅해야 함) | 불필요 (동작 중 즉시 적용) |
| 파일 형태 | `system_modify.dtb` (전체 DTB) | `led_overlay.dtbo` (패치 파일) |
| Base DTB 수정 | 있음 (새 파일로 교체) | 없음 (원본 그대로 유지) |
| 되돌리기 | DTB 파일 교체 후 재부팅 | `rmdir` 즉시 원복 |
| 주요 용도 | 고정 하드웨어 구성, 최종 제품 | 개발 중 빠른 반복 테스트, PL 재구성 |

### U-Boot에서 DTO를 자동 로드하는 방법

부팅 시 자동으로 DTO를 적용해야 하는 경우, U-Boot의 `bootcmd`에서 DTBO 파일을 TFTP로 수신하고 `fdt apply` 명령으로 적용할 수 있다.

```bash
# U-Boot 프롬프트에서
Zynq> tftpboot 0x3000000 uImage
Zynq> tftpboot 0x2A00000 system.dtb
Zynq> tftpboot 0x2C00000 led_overlay.dtbo   # DTBO 수신

# Base DTB를 fdt 작업 공간으로 이동
Zynq> fdt addr 0x2A00000

# DTBO 적용
Zynq> fdt apply 0x2C00000

# 적용된 DTB로 부팅
Zynq> bootm 0x3000000 - 0x2A00000
```

> 이 방법은 Linux가 이미 합산된 DTB를 받으므로 ConfigFS 없이도 DTO 효과를 얻을 수 있다. 단, 이 경우 런타임에 DTO를 제거하는 것은 불가능하다.
> 

## 핵심 정리

> **정적 DTB vs DTO — 언제 무엇을 쓸 것인가**
> 

| 상황 | 권장 방식 |
| --- | --- |
| PL 구성이 고정된 최종 제품 | 정적 DTB (system-user.dtsi에 노드 추가) |
| PL을 런타임에 변경해야 하는 경우 | FPGA Manager + `fpgautil` |
| 드라이버 개발 중 빠른 테스트 | ConfigFS 수동 적용 |
| PS 드라이버만 런타임 활성화/비활성화 | ConfigFS + 커스텀 DTBO |
| 부팅 시 자동으로 특정 노드 추가 | U-Boot `fdt apply` |

| 원칙 | 설명 |
| --- | --- |
| **`__symbols__` 필수** | Base DTB에 `__symbols__` 노드가 없으면 label 참조 방식의 overlay 적용 불가 |
| **LIFO 제거 순서** | 여러 overlay 적용 시 반드시 적용의 역순으로 제거 |
| **FPGA Manager 우선순위** | FPGA Manager 활성화 시 Device tree overlay 및 Remove PL 옵션은 무시됨 |
| **`devm_*` API 필수** | DTO 환경에서는 `remove()`가 언제든 호출될 수 있으므로 자동 리소스 관리가 특히 중요 |
| **심볼릭 링크 위치** | firmware 파일은 반드시 호스트의 `/nfsroot/lib/firmware/`에 링크를 생성해야 재부팅 후에도 유지됨 |