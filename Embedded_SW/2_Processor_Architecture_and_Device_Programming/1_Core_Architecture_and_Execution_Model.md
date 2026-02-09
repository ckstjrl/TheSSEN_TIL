# 프로세서 구조 및 디바이스 프로그래밍 day01

날짜: 2026년 1월 28일

## 임베디드  수업의 주요 목표

상반부 - 보드를 활용한 펌웨어 개발, 임베디드 입문에 가까움

후반부 - 임베디드 리눅스 → 실제 프로젝트에서 필요한 기술

ZYNQ보드 환경에서 프로젝트를 진행하므로 리눅스 환경이 중요함

### 프로세서 구조 및 디바이스 프로그래밍

상반부 - 프로세서란 무엇인가

후반부 - 주변 장치(GPIO, 메모리, 버스, RCC 등) 실습을 위주로 학습

## CORTEX-M 일반

### ARM Cortex Process (v7)

- ARM Cortex-A family (v7-A)
    - 고사양
    - M보다 2배 이상 복잡함
    - MMU → OS (RTOS는 제외) 사용하기 위해 필수
    - AXI - 최신 버스 표준
    - VFP - 고정 소수점 연산기
    - NEON - 병렬 연산기
- ARM Cortex-R family (v7-R)
    - 자동차 겨냥한 제품
    - A에 더 가까움
    - MPU가 들어가 있음
    - AXI - 최신 버스 표준
- ARM Cortex-M family (v7-M)
    - 저가형
    - AVR 16비트 프로세서의 비슷한 기능이 많음
    - 가격도 AVR과 비슷, 32비트 사용, 전력 소모량 더 낮음
    - MPU (optional) → 저가형이라 선택 탑재
    - AHD Lite & APB - 과거 버스 표준

## CORTEX-M 프로세서의 특징

### CORTEX-M4 프로세서 특징

- 낮은 게이트 수, 낮은 인터럽트 대기 시간 및 디버깅 기능을 갖춘 저전력
- ARM 아키텍처 v7M 설계 기술 기반
- 기본 Thumb-2 명령어, 16비트 32비트로 구성 → 어셈블리 명령어
- 멀티미디어 및 신호처리 지원 강화 → SIMD (Single Instruction Multiple Data) → 병렬 연산
- 핸들러 및 스레드 모드
- 낮은 인터럽트 지연을 위해 다름 명령어 실행 중 인터럽트 가능 LDM / STM, PUSH/ POP
- 저 지연 인터럽트를 위한 프로세서 레지스터 자동 저장 및 복원
→ 원래는 인터럽트 발생 시 개발자가 수동 저장 및 복원 해야 했음
- ARM 아키텍처 v6 스타일 BE8 / LE 지원
- ARMv6 정렬 (struct 타입 정령)되지 않은 액세스
- NVIC (인터럽트 컨트롤러)는 프로세서 코어와 밀접하게 통합되어 대기 시간이 짧은 인터럽트 처리 구현
- 240개 내에서 칩 제조사가 추가 설정 가능한 외부 인터럽트
- 3 ~ 8비트 우선 순위비트
- 인터럽트 동적 우선 순위 지원
- 우선 순위 그룹화 → 선점 및 서브 인터럽트 레벨 선택 가능
- 인터럽트 응답 시간 개선을 위한 테일 체인 및 LATE ARRIVING 인터럽트 지원
- MPU → 메모리 보호 장치
- 고성능 버스(AHB-Lite) 기반 Icode, DCode 및 시스템 버스 인터페이스
- APB 및 PPB 인터페이스

## STM 32

CPU ⊂ 프로세서 ⊂ MCU

CORTEX = 프로세서

STM32 = MCU

![image.png](../Embedded_SW/img/STM_pic.png))

![image.png](../Embedded_SW/img/STM_struct.png)

## 레지스터

전부 32바이트

### 범용 레지스터 R0 ~ R12

- Thumb 명령어에서는 R0 ~ R7 사용
- Thumb2 명령어에서는 R0 ~ R12 사용

### SP (Stack Point)

- 스택 메모리의 목적
    1. 전달인자 (argument)
        
        함수의 전달 인자가 5개 이상일 경우 스택 메모리 활용
        
    2. 지역변수
    3. 컨텍스트
        
        ex. 변수 초기값을 다시 사용해야 할 때 초기값을 스택 메모리에 저장해 놓고 필요할 때 다시 사용
        
    4. 리턴주소
    5. 프레임 포인터 (R7 or R11)
        
        ```c
        f1()
        {
            f11();
        }
        
        f11()
        {
            f111();
        )
        ```
        
        함수가 꼬리를 물고 호출하는 궤적을 기억 → call trace 
        
- STACK PUSH
    
    `push{r7, lr}` 
    
- STACK POP
    
    `push{r7, lr}` 
    

## 동작 모드

![image.png](../Embedded_SW/img/activemode.png)

### 동작모드 전환

2 모드 - 스레드 / 핸들러

2 레벨 - 특권 / 비특권

총 3가지 - 특권 스레드, 비특권 스레드, 특권 핸들러 (인터럽트 핸들러) 존재

- 소프트웨어 제작 방법
    1. 특권 스레드 + 특권 핸들러 사용하여 제작
        
        특권 스레드 → 우리가 작성한 코드가 실행되는 모드
        
        특권 헨들러 → 인터럽트 핸들러
        
        평소에 특권 스레드에서 실행되다가 인터럽트가 발생하면 특권 핸들러에서 실행하고 다시 복귀
        
    2. 특권 스레드 + 비특권 스레드 + 특권 핸들러 사용하여 제작
        
        비특권 스레드의 경우 접근 권한이 제한되어 있음
        
        제작한 소프트웨어가 비특권 스레드에서 작동하게 할 수 있자만, 제한이 발생할 확률이 높음
        
        → 이 방법은 주로 사용하는 방법 X
        
        c.f ) 만약 OS가 존재하는 경우 특권 스레드에 OS 커널, 비특권 스레드에 애플리케이션 이런식으로 작동하게 할 수 있지만 Cortex-M에는 OS 탑재 불가….
        

### 동작 모드별 스택 메모리

- 특권 스레드, 특권 핸들러에서 소프트웨어가 실행되는 경우 → main stack 활용
- 비특권 스레드에서 소프트웨어 실행되는 경우 → process stack 활용

if )  비특권 스레드를 사용하지 않을 경우 → main stack 하나만 존재한다.

### 핸들러 모드와 스레드 모드

- control 레지스터의 설정값에 따라 스레드 모드에서 프로세서가 메인 스택, 프로세스 스택을 사용

### 비특권 스레드 권한 제한 실습

```c
printf("MSP=0x%08x\n", __get_MSP()); //
printf("PSP=0x%08x\n", __get_PSP()); //
printf("APSR=0x%08x\n", __get_APSR()); //
printf("IPSR=0x%08x\n", __get_IPSR()); //
printf("xPSR=0x%08x\n", __get_xPSR()); //
printf("PRIMASK=0x%08x\n", __get_PRIMASK()); //
printf("FAULTMASK=0x%08x\n", __get_FAULTMASK()); //
printf("BASEPRI=0x%08x\n", __get_BASEPRI()); //
printf("CONTROL=0x%08x\n", __get_CONTROL()); //

// User Thread 'WRITE' Test
printf("MSP=0x%08x\n", __get_MSP()); __set_MSP(__get_MSP()); //
printf("PSP=0x%08x\n", __get_PSP()); __set_PSP(__get_PSP()); //
printf("PRIMASK=0x%08x\n", __get_PRIMASK()); __set_PRIMASK(__get_PRIMASK()); //
printf("FAULTMASK=0x%08x\n", __get_FAULTMASK()); __set_FAULTMASK(__get_FAULTMASK()); //
printf("BASEPRI=0x%08x\n", __get_BASEPRI()); __set_BASEPRI(__get_BASEPRI()); //
printf("CONTROL=0x%08x\n", __get_CONTROL()); __set_CONTROL(1); //
printf("CONTROL=0x%08x\n", __get_CONTROL());

/*
MSP=0x00000000
PSP=0x00000000
APSR=0x20000000
IPSR=0x00000000
xPSR=0x20000000
PRIMASK=0x00000000
FAULTMASK=0x00000000
BASEPRI=0x00000000
CONTROL=0x00000003
MSP=0x00000000
PSP=0x00000000
PRIMASK=0x00000000
FAULTMASK=0x00000000
BASEPRI=0x00000000
CONTROL=0x00000003
CONTROL=0x00000003
*/
```

### 특권 스레드 실습

```c
/*
MSP=0x2002ff88
PSP=0x00000000
APSR=0x20000000
IPSR=0x00000000
xPSR=0x20000000
PRIMASK=0x00000000
FAULTMASK=0x00000000
BASEPRI=0x00000000
CONTROL=0x00000000
MSP=0x2002ff88
PSP=0x00000000
PRIMASK=0x00000000
FAULTMASK=0x00000000
BASEPRI=0x00000000
CONTROL=0x00000000
CONTROL=0x00000001
*/
```

위와 같은 코드 실행

### 특권 vs 비특권 권한

![image.png](../Embedded_SW/img/privileged.png)

### SP 레지스터

- 현재 수행되는 동작 모드에 따라 스택 포인터 레지스터(msp, psp)와 스택 메모리가 결정되는 구조
- 메인 스택 ⇒ msp, 프로세스 스택 ⇒ psp
- 결국 SP 레지스터는 현재 모드에서 사용하는 스택 메모리를  alias

## PC

### PC(Program Counter) 레지스터

- R15 또는 PC를 사용하여 명령어 FETCH
- 명령어가 들어가 있는 ROM의 주소 = PC 레지스터 값

![image.png](../Embedded_SW/img/PCR.png)

## THUMB2

### ARM 명령어 vs THUMB 명령어

- 32bit 메모리 사용 ARM > THUMB
- 16bit 메모리 사용 ARM < THUMB
    - 32비트 명령어를 사용할 경우 2번 읽어야 하므로 효율 저하
- 단일 명령어의 길이는 32나 16 비트
    - ARM 명령어는 32비트 길이
    - THUMB 명령어는 16비트 길이
    - THUMB2 명령어는 16 / 32 비트 길이

### THUMB 2 명령어

- 가변 길이 명령어
    - 16비트, 32비트 둘 다 가능
- ARM, THUMB보다 코드 밀도 26% 향상, 성능 25% 향상
- 명령어만 보고는 16비트인지 32비트인지 알 수 없다

### Narrow Bus Interface 에서 THUMB 을 사용하는 이유

- THUMB, THUMB 2 -16bit 둘 다 사용
- ROM이 16bit이므로

### Wide Bus Interface

- THUMB2 사용

## 파이프라인

### 개념도

![IMG_1622.jpeg](../Embedded_SW/img/pipeline.jpeg)

- 특정 명령어를 실행할 때 PC = 현재 FETCH 하는 명령어의 주소를 가지고 있다

### 플래시 메모리 인터페이스

![IMG_1623.jpeg](../Embedded_SW/img/flash.jpeg)

## LR

### Lr(Link register)

- R14 또는 LR를 사용하여 리턴 주소를 저장 ( vs 스택 메모리를 이용한 리턴 주소 저장)
- LR은 서부루틴 또는 함수 호출의 리턴 주소를 저장하는 데 사용
- PC는 기능이 완료된 후 LR에서 값을 로드한다
- 함수 호출
    - LR = 리턴 주소 + 1
    - PC = 함수 주소
- 함수 리턴
    - PC = LR

## AAPCS

### Procedure Call Standard for the ARM Architecture

- 기본 표준의 ARM 및 THUMB 명령어 세트에 공통적인 머신 수준의 코어 레지스터 전용 호출 표준을 정의
- R0 ~ R15 (r0 ~ r15)로 표시

### Core registers and AAPCS usage

![IMG_1624.jpeg](../Embedded_SW/img/core_registers&AAPCS.jpeg)

### Subroutine Calls

- ARM 및 THUMB 명령어 세트에는 링크 포함 분기 작업 수행하는 기본 서브 루틴 호출 명령어 BL이 포함되어 있음
- BL 실행의 효과 → PC의 다음 값인 리턴 주소를 LR로 보내고 목적지 주소를 PC로 전송
- 링크 레지스터의 비트 0은 BL 명령어가 THUMB 상태에서 실행된 경우 1로 설정
ARM 상태에서 실행된 경우 0으로 설정

### Core registers

- r0 ~ r3은 인수 값을 서브루틴으로 전달하고 함수에서 결과 값을 반환하는 데 사용
- 레지스터 r12 (IP)는 링커가 루틴과 이를 호출하는 서브루틴 간의 스크래치 레지스터로 사용 가능

### Result return

- 함수에서 결과가 반환되는 방식은 해당 결과의 유형에 따라 결정
- 기본 데이터 유형에서는 r0에서 반환
- 더블 유형에서는 r0 및 r1에서 반환
- 128비트 컨테이너화된 백터는 r0 ~ r3에서 반환

## xPSR

![IMG_1625.jpeg](../Embedded_SW/img/xPSR.jpeg)

### APSR

application Program Status Register

![image.png](../Embedded_SW/img/APSR.png)

##