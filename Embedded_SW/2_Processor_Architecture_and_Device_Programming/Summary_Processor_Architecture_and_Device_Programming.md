# 프로세서 구조 및 디바이스 프로그래밍 - 핵심 개념 정리

---

## 1. 코어 아키텍처 및 실행 모델

### 1-1. ARM Cortex-M 패밀리 개요

ARM Cortex 프로세서는 사용 목적에 따라 세 계열로 나뉜다.

| 계열 | 특징 | 주요 용도 |
|------|------|-----------|
| Cortex-A | 고사양, MMU 탑재, AXI 버스 | 스마트폰, 리눅스 OS |
| Cortex-R | MPU 탑재, 실시간 처리 | 자동차, 안전-critical 시스템 |
| Cortex-M | 저가, 저전력, MPU 선택, AHB Lite | 임베디드, MCU |

### 1-2. Cortex-M4 주요 특징

- **Thumb-2 명령어**: 16/32비트 혼합 → 코드 밀도 26% ↑, 성능 25% ↑
- **SIMD**: Single Instruction Multiple Data로 병렬 연산 지원
- **NVIC**: 최대 240개 외부 인터럽트, 최대 256 우선순위 레벨
- **자동 컨텍스트 저장/복원**: 인터럽트 진입/종료 시 하드웨어가 자동 처리
- **테일 체인 & Late Arriving**: 인터럽트 응답 시간 최적화

### 1-3. 레지스터 구성

- **R0~R12**: 범용 레지스터 (Thumb: R0~R7, Thumb2: R0~R12)
- **R13 (SP)**: 스택 포인터 (MSP: 메인 스택, PSP: 프로세스 스택)
- **R14 (LR)**: 링크 레지스터 - 함수 리턴 주소 저장
- **R15 (PC)**: 프로그램 카운터 - 현재 FETCH 중인 명령어 주소
- **xPSR**: 프로그램 상태 레지스터 (APSR/IPSR/EPSR 통합)

#### AAPCS 레지스터 사용 규약
- R0~R3: 함수 인자 전달 및 리턴값 반환
- R4~R11: 호출자가 보존해야 하는 레지스터
- R12 (IP): 링커 스크래치 레지스터

### 1-4. 동작 모드

```
[모드 구조]
├── 스레드 모드 (Thread Mode)
│   ├── 특권(Privileged) 스레드  ← 일반 코드 실행
│   └── 비특권(Unprivileged) 스레드  ← 제한된 접근
└── 핸들러 모드 (Handler Mode)
    └── 특권(Privileged) 핸들러  ← 인터럽트 처리
```

- **특권 모드**: 모든 시스템 레지스터 접근 가능
- **비특권 모드**: MSP, FAULTMASK 등 핵심 레지스터 접근 불가
- **스택 사용**: 특권 스레드/핸들러 → MSP, 비특권 스레드 → PSP

### 1-5. 파이프라인

- Fetch → Decode → Execute 3단계 파이프라인
- PC는 현재 Fetch 중인 명령어 주소를 가리킴 (실행 중인 명령어보다 2단계 앞)

### 1-6. Thumb / Thumb-2 명령어

- **ARM**: 32비트 고정 길이
- **Thumb**: 16비트 고정 길이 (메모리 절약)
- **Thumb-2**: 16/32비트 가변 길이 → 성능과 코드 밀도 모두 최적

---

## 2. GPIO 및 메모리/버스 아키텍처

### 2-1. GPIO (General Purpose I/O)

- 디지털 신호 입출력 범용 핀
- STM32 GPIO 레지스터는 32비트 워드 단위 접근
- 리셋 후 기본값: 입력 플로팅(floating) 모드

#### GPIO 출력 모드
- **Push-Pull**: 0/1 모두 능동 구동
- **Open-Drain**: 0만 능동 구동, 1은 외부 풀업 필요

#### Alternate Function (AF)
- 핀 하나를 GPIO 대신 UART/SPI/I2C 등 주변장치에 연결
- Pin MUX를 통해 하나의 핀으로 여러 기능 수행

### 2-2. 소프트웨어 계층 구조

| 계층 | 설명 | 이식성 |
|------|------|--------|
| CMSIS-CORE | ARM 표준 하드웨어 추상화 | 높음 |
| HAL | STM32 계열 간 이식성 최우선 | 높음 |
| LL (Low Layer) | 직접 레지스터 제어, 고성능 최적화 | 낮음 |

### 2-3. STM32 메모리 버스 구조

```
[버스 종류]
├── I-Bus: 명령어 Fetch (Flash ↔ Core)
├── D-Bus: 데이터 액세스 (Flash ↔ Core)
├── S-Bus: 주변장치/SRAM 데이터 읽기
└── PPB-Bus: 디버그 및 시스템 제어
```

- **SRAM**: 192KB, 대기상태 0 (CPU 속도와 동일)
- **Flash**: AHB I/D 코드 버스로 접근, 프리패치 및 캐시 라인 지원
- **부팅 모드**: BOOT[1:0] 핀으로 3가지 선택 (Flash/System/SRAM)

### 2-4. MMIO (Memory Mapped I/O)

- I/O 장치가 메모리와 동일한 주소 공간 사용
- LDR/STR 명령어로 주변장치 레지스터 직접 제어 가능

### 2-5. 비트 밴딩 (Bit Banding)

- 일반 방식: 1비트 수정 시 Read-Modify-Write 필요 → 비원자적
- 비트 밴딩: 1비트에 별칭(alias) 주소 할당 → 단일 쓰기로 원자적 수정
- **비원자적 문제 예**: 멀티태스킹에서 GPIO_ODR 동시 접근 시 데이터 손실
- **해결책**: BSRR 레지스터 사용 (하드웨어 원자적 접근)

---

## 3. 클럭 및 인터럽트 아키텍처

### 3-1. 클럭 시스템 (RCC)

#### 클럭 소스 3가지
- **HSE** (High Speed External): 외부 크리스탈 사용, 정확도 높음
- **HSI** (High Speed Internal): 내부 16MHz RC 발진기, 빠른 시작
- **PLL** (Phase Locked Loop): HSI/HSE 입력으로 고주파 클럭 생성

#### PLL 구성 요소
- 분할 인자 M, N, P, Q로 원하는 주파수 설정
- STM32CubeIDE에서 시각적으로 클럭 설정 가능

### 3-2. 인터럽트 개요

#### 폴링 vs 인터럽트
| 방식 | 폴링 | 인터럽트 |
|------|------|----------|
| CPU 효율 | 낮음 (계속 확인) | 높음 (이벤트 시만 처리) |
| 응답 지연 | 발생 가능 | 즉각 처리 |
| 구현 복잡도 | 단순 | 복잡 |

#### 인터럽트 초기화 순서
1. 전체 인터럽트 전역 활성화
2. 사용할 인터럽트 우선순위 설정
3. 개별 인터럽트 활성화

### 3-3. NVIC (Nested Vectored Interrupt Controller)

- 최대 240개 외부 인터럽트 + 16개 시스템 예외 = 256개
- 최대 256 우선순위 레벨 (3~8비트)
- **인터럽트 진입 시 하드웨어가 자동으로 컨텍스트 저장** (xPSR, PC, LR, R12, R3~R0)
- 인터럽트 종료 시 자동 복원
- **테일 체인 지원**: 연속 인터럽트 처리 시 스택 팝/푸시 생략

#### NVIC 레지스터
- `NVIC_ISER/ICER`: 인터럽트 활성화/비활성화
- `NVIC_ISPR/ICPR`: 인터럽트 펜딩 설정/클리어
- `NVIC_IPR`: 인터럽트 우선순위 설정

### 3-4. 인터럽트 우선순위

- **선점(Preempt) 우선순위**: 현재 핸들러를 중단하고 새 인터럽트 처리 여부 결정
- **서브(Sub) 우선순위**: 같은 선점 순위 내에서의 처리 순서
- **IRQ 번호**: 선점·서브 우선순위 모두 같을 때 낮은 번호 우선

우선순위 결정 순서: 선점 우선순위 → 서브 우선순위 → IRQ 번호

### 3-5. 예외처리 핸들러 진입/종료

#### 진입 (총 12 사이클 소요)
1. 현재 명령어 완료
2. 컨텍스트 스택에 푸시 (xPSR, PC, LR, R12, R3, R2, R1, R0)
3. 핸들러/특권 모드 전환, MSP 사용
4. 벡터 테이블에서 핸들러 주소 로드 → PC
5. EXC_RETURN → LR 로드

#### 종료
1. EXC_RETURN 트리거 명령어 실행
2. 스택에서 컨텍스트 복원
3. 중단 지점부터 실행 재개

### 3-6. 테일 체이닝 & 지연 도착

- **테일 체이닝**: 인터럽트 처리 완료 후 대기 중인 인터럽트가 있으면 스택 팝/푸시 없이 바로 처리 → 오버헤드 감소
- **지연 도착(Late Arriving)**: 스태킹 중 더 높은 우선순위 인터럽트 발생 시 해당 인터럽트 먼저 처리 → 인터럽트 지연 시간 단축

### 3-7. EXTI (External Interrupt Controller)

- 외부 핀 신호로 인터럽트 발생 (버튼 등)
- 에지 감지 방식 (상승/하강 에지 선택 가능)
- STM32 EXTI는 **펄스(에지) 인터럽트만 지원**
- 인터럽트 클리어: EXTI_PR 레지스터에 1 기록

### 3-8. 벡터 테이블 및 VTOR

- 벡터 테이블: 예외 핸들러 주소 저장 (명령어 아님)
- 초기 SP값 (0x0), 리셋 핸들러 주소 (0x4)
- **VTOR**: 벡터 테이블 위치 변경 → SRAM으로 재배치 시 런타임에 핸들러 수정 가능

---

## 4. 예외 처리 및 USART

### 4-1. 예외(Exception) 유형

| 예외 | 우선순위 | 설명 |
|------|----------|------|
| RESET | -3 | 콜드/웜 부트 시 발생 |
| NMI | -2 | Non-Maskable Interrupt, 항상 활성화 |
| HardFault | -1 | 예외 처리 중 오류 발생 |
| MemManage | 설정 가능 | 메모리 보호 위반 |
| BusFault | 설정 가능 | 메모리 버스 오류 |
| UsageFault | 설정 가능 | 정의되지 않은 명령어 등 |
| SVCall | 설정 가능 | SVC 명령어로 트리거 (OS 필수) |
| PendSV | 설정 가능 | 시스템 레벨 서비스 요청 |
| SysTick | 설정 가능 | 시스템 타이머 0 도달 시 발생 |

### 4-2. 예외 마스킹 레지스터

- **PRIMASK**: Reset/NMI/HardFault 제외 모든 예외 차단
- **FAULTMASK**: Reset/NMI 제외 모든 예외 차단 (Fault 포함)
- **BASEPRI**: 지정 우선순위 이하의 인터럽트만 차단

### 4-3. System Control Block (SCB)

| 레지스터 | 기능 |
|----------|------|
| SCB:VTOR | 벡터 테이블 기준 주소 오프셋 설정 |
| SCB:AIRCR | 우선순위 그룹화, 엔디안, 시스템 리셋 제어 |
| SCB:SHCSR | 시스템 핸들러 활성화 및 상태 확인 |
| SCB:CFSR | MemManage/BusFault/UsageFault 원인 |
| SCB:HFSR | HardFault 원인 정보 |
| SCB:MMFAR | MemManage 오류 발생 주소 |
| SCB:BFAR | BusFault 발생 주소 |

### 4-4. USART (Universal Sync/Async Receiver/Transmitter)

#### UART vs USART
- **UART**: 비동기 통신만 지원
- **USART**: 비동기 + 동기 통신 모두 지원 (CLK 핀 추가)

#### 시리얼 통신 프레임 구조
```
[IDLE] [Start(0)] [D0 D1 D2 D3 D4 D5 D6 D7] [Parity] [Stop(1)] [IDLE]
```

- **Start bit**: 항상 0 (통신 시작 알림)
- **Data bits**: 8 or 9비트
- **Parity bit**: 오류 검출 (홀수/짝수 패리티)
- **Stop bit**: 항상 1 (통신 종료)

#### Baud Rate
- 전송 속도 (bps), 송수신 양쪽이 동일해야 함
- USARTDIV 값으로 결정
- STM32 USART 최대 4.5Mbit/s

#### USART 수신 오류
- **Overrun Error**: 이전 데이터 읽기 전 새 데이터 수신
- **Frame Error**: Stop bit 오류
- **Break Character**: 데이터 라인 비정상 상태

---

## 5. Input Capture 타이머 및 DMA

### 5-1. Input Capture 타이머

- **목적**: 외부 신호의 펄스 폭, 주기 측정
- **원리**: 에지 발생 시 타이머 카운터 값을 캡처 레지스터(TIMx_CCRx)에 래치
- **주기 측정**: 같은 극성 두 에지 시간 차이 = 타이머 카운트
- **펄스 폭 측정**: 반대 극성 두 에지 시간 차이

#### 입력 캡처 설정 순서
1. 활성 입력 채널 선택 (CC1S 비트)
2. 입력 필터 설정
3. 트리거 에지 선택 (CC1P 비트)
4. 프리스케일러 설정 (IC1PS)
5. 캡처 활성화 (CC1E 비트)
6. 인터럽트/DMA 요청 활성화 (CC1IE/CC1DE)

### 5-2. DMA (Direct Memory Access)

#### DMA를 사용하는 이유
- CPU 개입 없이 데이터 전송 → CPU 리소스 확보
- 대용량 데이터 전송이나 반복 전송 시 효율적

#### DMA 동작 원리
1. CPU가 DMA 컨트롤러 설정 (주소, 전송 크기, 방향 등)
2. 주변장치가 DMA 요청 신호 발생 (DREQ)
3. DMA가 버스 요청 → CPU가 버스 해제 (Hi-Z 상태)
4. DMA가 마스터로서 데이터 전송 수행
5. 전송 완료 후 인터럽트 발생

#### DMA 전송 모드
| 모드 | 설명 |
|------|------|
| Peripheral to Memory | 주변장치 → 메모리 |
| Memory to Peripheral | 메모리 → 주변장치 |
| Memory to Memory | 메모리 간 복사 (소프트웨어 트리거) |
| Circular Mode | 순환 버퍼, ADC 스캔 모드 등에 활용 |

#### DMA 주소 모드
- **단일 주소(Single)**: 소스 주소는 암시적, 1 버스 사이클
- **이중 주소(Dual)**: 소스/대상 주소 모두 명시적, 2 버스 사이클, 내부 버퍼 사용

#### DMA 전송 크기 계산
```
전송 바이트 수 = 전송 단위 × BURST × BEAT
```

#### DMA 인터럽트
- Half-Transfer (HTIF): 절반 전송 완료
- Transfer Complete (TCIF): 전송 완료
- Transfer Error (TEIF): 전송 오류
- FIFO Error (FEIF): FIFO 오류
- Direct Mode Error: 직접 모드 오류

#### STM32 DMA 구성
- DMA 컨트롤러 2개, 각 8개 스트림, 스트림당 8개 채널 = 총 16 스트림
- 듀얼 AHB 마스터 버스 (메모리 전용 + 주변장치 전용)
- 스트림당 4워드 FIFO 지원

---

## 6. ADC, I2C 및 RTC

### 6-1. ADC (Analog-to-Digital Converter)

#### 기본 개념
- 아날로그 신호 → 샘플링 → 양자화 → 부호화 → 디지털 값
- 분해능: 12비트 (기본), 10/8/6비트 선택 가능
- 총 19채널: 16개 외부 + 2개 내부 + 1개 Vbat

#### ADC 변환 모드
| 모드 | 설명 |
|------|------|
| Single | 한 채널 1회 변환 후 중지 |
| Continuous | 한 채널 자동 반복 변환 |
| Scan | 여러 채널 순차 변환 |
| Discontinuous | 불연속 그룹 변환 |

#### ADC 변환 시간 계산
```
Tconv = 샘플링 시간 + 12 사이클
예) ADCCLK=30MHz, 샘플링=3 사이클 → Tconv=15 사이클=0.5μs
```

#### ADC 클럭
- **ADCCLK**: 아날로그 회로용, APB2 클럭을 프리스케일러로 분주
- **APB2**: 디지털 인터페이스용

#### Analog Watchdog
- 변환값이 상한/하한 임계값 벗어나면 AWD 플래그 설정 및 인터럽트 발생

### 6-2. I2C (Inter-Integrated Circuit)

#### I2C vs UART 비교
| 항목 | UART | I2C |
|------|------|-----|
| 동기 방식 | 비동기 | 동기 (CLK 존재) |
| 선 수 | 2선 (Tx, Rx) | 2선 (SDA, SCL) |
| 통신 방식 | 1:1 | 1:多 (다중 슬레이브) |
| 용도 | 장치 간 | 보드 내 칩 간 |

#### I2C 통신 프로토콜
```
[START] → [슬레이브 주소 7bit + R/W bit] → [ACK] → [데이터 바이트] → [ACK] → [STOP]
```

- **START**: SCL HIGH 상태에서 SDA를 HIGH→LOW
- **슬레이브 주소**: 7비트 (또는 10비트)
- **ACK**: 수신측이 SDA를 LOW로 당김 (9번째 비트)
- **STOP**: SCL HIGH 상태에서 SDA를 LOW→HIGH

#### I2C 특징
- 오픈 드레인 출력 → **외부 풀업 저항 필수**
- 통신 속도: 표준 100kHz, Fast 400kHz
- 멀티 마스터 지원 (중재 및 충돌 감지)
- 아날로그 노이즈 필터 내장

#### I2C 마스터 역할
- 클럭 신호 생성 (SCL)
- START/STOP 조건 생성

### 6-3. IWDG (Independent Watchdog)

- 소프트웨어 오류/무한 루프 감지하여 시스템 리셋
- **독립 RC 발진기(LSI)** 사용 → 메인 클럭 실패 시에도 동작
- 다운 카운터가 0 도달 시 리셋 발생
- 리셋 방지: 타임아웃 전 0x5555, 0xAAAA 순서로 특정 레지스터에 기록

### 6-4. RTC (Real-Time Clock)

#### 주요 기능
1. **시계 (Human Watch)**: 현재 시/분/초 제공
2. **캘린더 (Calendar)**: 날짜/요일 관리
3. **알람 (Alarm)**: 설정 시각 도달 시 인터럽트 발생
4. **웨이크업 (Wakeup)**: 저전력 모드에서 일정 시간 후 CPU 깨우기

#### RTC 클럭
- 32kHz 수정 발진자 구동
- 프리스케일러로 1Hz 클럭 생성

#### RTC와 저전력 모드
| 모드 | RTC 동작 |
|------|----------|
| Sleep | RTC 인터럽트로 절전 모드 종료 없음 |
| Stop | RTC 웨이크업으로 중지 모드 종료 |
| Standby | RTC 웨이크업으로 대기 모드 종료 |

---

## 핵심 용어 요약

| 용어 | 설명 |
|------|------|
| NVIC | Nested Vectored Interrupt Controller |
| EXTI | External Interrupt Controller |
| SCB | System Control Block |
| MMIO | Memory Mapped I/O |
| DMA | Direct Memory Access |
| ADC | Analog-to-Digital Converter |
| USART | Universal Sync/Async Receiver/Transmitter |
| RTC | Real-Time Clock |
| IWDG | Independent Watchdog |
| Tail-Chaining | 연속 인터럽트 처리 최적화 |
| Bit Banding | 1비트 원자적 접근 메커니즘 |
| VTOR | Vector Table Offset Register |
| PLL | Phase Locked Loop (클럭 배주) |
| EXC_RETURN | 예외 반환 코드 |
| AAPCS | ARM 아키텍처 프로시저 호출 표준 |