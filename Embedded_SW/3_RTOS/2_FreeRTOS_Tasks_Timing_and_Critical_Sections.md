# 실시간 운영체제 구조 및 활용 day02

날짜: 2026년 2월 10일

## 태스크 (continue)

### IDLE 태스크의 특징 → 직접 생성하지 않아도 자동 생성되는 태스크

`static void prvIdleTask( void *pvParameters );`

- 우선 순위 0 (lowest)
- dead 시킬 수 없는 task
- 휴면하지 않음 (no pend)
- 아무 일도 하지 않음

→ 멀티 태스킹 시작하면 자동 생성

### 런타임 스택 검사 방법

1. `#define configCHECK_FOR_STACK_OVERFLOW        1`
    - 스택 포인터가 유효한 범위를 벗어난 것이 실시간으로 확인되면 사용자가 미리 정의한 hook 함수 호출
    - 속도 빠름
    - But, 문맥전환시에 스택 오버플로우 검사하는 방식이므로, 이외 시간에 발생하는 것은 확인 불가
2. `#define configCHECK_FOR_STACK_OVERFLOW        2`
    - 태스크가 생성되면 스택 공간을 알려진 0(zero) 패턴으로 덮어 채운다
    - 유효한 스택 내 패턴 정보 20 바이트를 테스트 진행
    - 20 바이트 중 하나라도 변경된 경우, 오버플로 hook 함수 호출
    - 1번 방법에 비해 느리지만, 보다 정확하게 스택 오버플로우 찾아냄

### Task 삭제

- 삭제된 태스크는 더 이상 스케줄링 되지 않음
- `void vTaskDelete( TaskHandle_t xTaskToDelete )`
- `xTaskToDelete` : 삭제할 태스크 핸들
- IDLE 태스크는 삭제 불가
- 자기 자신을 삭제하는 것도 가능, `xTaskToDelete`에 NULL을 전달

### Task 우선 순위 변경

- `vTaskPrioritySet()` 을 이용
- `void vTaskPrioritySet( TaskHandle_t xTask, UBaseType_t uxNewPriority )`
    - xTask : 변경하고자 하는 태스크 핸들
    - uxPriority : 새로운 우선 순위
- 자기 자신 (NULL) 혹은 다른 태스크 우선 순위를 변경
- IDLE 태스크의 우선순위 변경은 불가능

### Task 일시 중단

- 태스크의 실행을 일시 중단
- `void vTaskSuspend( TaskHandle_t xTaskToSuspend )`
    - xTaskToSuspend : 중지시킬 태스크 핸들
- 태스크를 다시 동작시키기 위해서 `vTaskResume()` 사용

### 중단된 Task의 실행 재개

- 중단된 태스크를 준비 상태로 재개
- `void vTaskResume( TaskHandle_t xTaskToResume )`
    - xTaskToResume : 준비 상태로 재개시킬 태스크 핸들

### Task 정보 얻어 오기

- 지정한 Task의 주요 정보를 얻어 온다
- `void vTaskGetTaskInfo( TaskHandle_t xTask, 
                       TaksStatus_t *pxTaskStatus, 
                       BaseType_t xGetFreeStackSpace, 
                       eTaskStats eState )`
    - xTask : 정보를 얻어 올 태스크 핸들
    - *pxTaskStatus : TaskStatus_t 유형의 변수를 가리켜야 함
- 디버깅시 활용 가능한 정보

## 시간관리 서비스

### FreeRTOS 변수의 이름 규칙

- 변수 프리픽스
    - type: '**c**' for char
    - type: '**s**' for int16_t (short)
    - type: ‘**l**’ for int32_t (long)
    - type: '**x**' for BaseType_t and any other non-standard types
        
        (structures, task handles, queue handles, etc.)
        
    - a variable is unsigned, it is also prefixed with a ‘**u**’
    - a variable is a pointer, it is also prefixed with a ‘**p**’

### FreeRTOS 함수의 이름 규칙

- 함수 프리픽스
    - **v**TaskPrioritySet() returns a void
    - **x**QueueReceive() returns a variable of type BaseType_t
    - **pv**TimerGetTimerID() returns a pointer to void
    - private functions are prefixed with '**prv**'

### FreeRTOS 매크로 이름 규칙

- 매크로 프리픽스
    - **port** (for example, **port**MAX_DELAY) portable.h or portmacro.h
    - **task** (for example, **task**ENTER_CRITICAL()) task.h
    - **pd** (for example, **pd**TRUE) projdefs.h
    - **config** (for example, **config**USE_PREEMPTION) FreeRTOSConfig.h
    - **err** (for example, **err**QUEUE_FULL) projdefs.h
    - **pdTRUE(pdPASS)** 1
    - **pdFALSE(pdFAIL)** 0

### 시간 관리 서비스

- FreeRTOSConfig.h 의 상수를 설정하여 사용
- FreeRTOS 시간관리 서비스
    - `vTaskDelay()`
    - `vTaskDelayUntil()`
    - `vTaskGetTickCount()`
- FreeRTOSConfig.h 설정 상수
    - INCLUDE_vTaskDelay
    - INCLUDE_vTaskDelayUntil

### vTaskDelay

- `void vTaskDelay( const TickType_t xTicksToDelay )`
    - xTicksToDelay : 지연시간 틱
- Tick 인터럽트는 1ms 주기 (but, 타이머 설정에 따라 달라짐)
- **문맥 전환 발생**
- 1 Tick 주기는 `configTICK_RATE_HZ` 설정에 따라 다르다
- 밀리초 틱으로 변환해주는 매크로 `pdMS_TO_TICKS()` 사용

### vTaskDelatUntil

- `void vTaskDelayUntil( TickType_t *pxPreviousWakeTime, TickType_t xTimeIncrement )`
- 절대 시간에 도달할 때 까지 `vTaskDelayUntil()` 을 호출하는 작업을 차단됨 상태로 만듦
- 주기적 태스크는 `vTaskDelayUntil()` 을 사용하여 실행 빈도를 일정하게 유지할 수 잇다
- `pxPreviousWakeTime` 이 시간은 작업이 다음에 차단됨 상태를 벗어나는 시간을 계산하기 위한 참조점으로 활용
- `xTimeIncrement`는 틱 단위로 지정됨, `pdMS_TO_TICKS()` 매크로는 밀리 초 틱으로 변환하는데 사용
- 사용 예시
    
    ```c
    /* Define a task that performs an action every 50 milliseconds. */
    void vCyclicTaskFunction( void * pvParameters )
    {
    	TickType_t xLastWakeTime;
    	const TickType_t xPeriod = pdMS_TO_TICKS( 50 );
    	xLastWakeTime = xTaskGetTickCount();
    	/* Enter the loop that defines the task behavior. */
    	for (;; )
    	{
    /* xLastWakeTime is automatically updated within vTaskDelayUntil() so is not explicitly updated by the task. */
    		TaskDelayUntil &xLastWakeTime, xPeriod);
    /* Perform the periodic actions here. */
    	}
    }
    ```
    

### vTaskDelay와 vTaskDelayUntil 의 차이

- vTaskDelay
    - 호출 태스크가 함수를 호출한 시간부터 지정된 틱 수 만큼 Blocked 상태로 들어가고 남아 있게 함
    - 이러한 이유로 호출한 태스크가 Blocked 상태를 종료할 시간은 호출한 시간에 관련
- vTaskDealyUntil
    - 호출 태스크를 입력한 다음 목표 절대 시간에 도달할 때까지 차단됨 상태 유지
    - 호출 순간이 아닌 특정 시간에 Blocked 상태를 정확히 종료

### 시스템 시간 얻기

- `TickType_t xTaskGetTickCount(void)`
    - 현재의 시스템 시간을 얻어 온다
    - 틱 수는 스케줄러가 시작된 이후 발생한 틱 인터럽트의 총 수
    - `xTaskGetTickCount` 는 현재 틱 계수 값을 반환
- `#define configUSE_16_BIT_TICKS        1` → 틱 계수는 16비트 변수에 보관
- `#define configUSE_16_BIT_TICKS        0` → 틱 계수는 32비트 변수에 보관

## 임계 영역

### 동시성 문제

2개 이상의 Task가 1개의 함수에 동시에 접근하는 경우 문제 발생

→ 비원자적 + 선점 → 이러한 이유로 문제 발생

### 임계 영역

- 공유 자원을 사용 중인 함수 내의 일부 혹은 전체 영역
- 코드 영역의 실행이 시작되면 다른 태스크가 이 영역을 선점하지 못하게 함
- 보호 장치
    - 인터럽트 중단
    - 스케줄링 중단
    - 세마포어 ( 상호 배제 커널 서비스 )

### 재진입

- 멀티태스킹 환경 그리고 다수의 태스크에서 호출하여 사용할 수 있기 위해 해당 함수는 재진입 가능하도록 작성되어져 있어야 함
- 멀티태스킹 환경이긴 하지만 **단일 태스크만이 독점하여 사용할 것으로 확신** 할 수 있는 경우에는 해당 함수는 재진입 아니어도 무방
- 함수를 재진입 가능하도록 하려면
    - 전역변수를 사용하지 않는다
    - 세마포어 같은 커널 리소스로 전역변수 보호
    - 전역변수 사용동안 인터럽트 작동 임시 중지

### c.f ) 문맥 전환이 발생하는 경우 2가지

1. vTaskDelay와 같이 커널 함수 내에 내장된 `portYIELD_WITHIN_API()` 에 의해 시작
    
    → 우선순위 높은 Task가 존재할 때만 문맥 전환 발생
    
2. systick 인터럽트 외 기타 하드웨어 인터럽트에 의해 시작
    
    → 우선순위 높은 Task가 존재할 때만 문맥 전환 발생
    

### 상호 배제 (Mutual Exclusion) (추천 순서대로)

1. 전역변수 → 지역변수 (가장 간단하고 가장 안전한 방법) == 상호 배제 사용하지 않는 방향
2. 세마포어류 - 뮤텍스 (안전(인터럽트 작동 잠금과 다르게 인터럽트 처리 가능)한 방법)
    - 즐겨 사용되는 방법
    - 사용이 지나치게 많을 경우 태스크의 블럭킹이 잦아지며 이로 인한 오버헤드 심함
3. 인터럽트 작동을 잠금
    - 공유 자원을 사용하는 동안 인터럽트 사용 금지
    - 임계 영역 코드의 실행 시간이 비교적 아주 짧은 경우 효과적
    - 이 때문에 타임 TICK 인터럽트의 주기를 놓치지 안도록 사용 해야 함
4. 스케줄러 잠금 (지원하지 않는 OS 많음)
    - 공유 자원을 사용하는 동안 스케줄링 금지시킴 → 문맥 전환 발생 X
    - FreeRTOS에서는 지원하지 않음
    - 임계 영역 코드의 실행 시간이 비교적 아주 짧은 경우 효과적
    - 이 실행이 빈번할 경우 높은 우선 순위 태스크 실행이 늦어지는 현상이 발생할 가능성 있음
- 상호 배제를 사용하지 않는 방법 → 가장 이상적임
    - 공유 자원을 되도록 사용하지 않는다
    - 공유 자원 (변수, I/O장치)을 사용하더라도 이 자원을 다수의 태스크가 공유하도록 하지 않는다.

### 크리티컬 섹션 (Critical Section)

- `taskENTER_CRITICAL()` , `taskEXIT_CRITICAL()`
    - FreeRTOS 에서의크리티컬 섹션 보호
- 인터럽트 비활성화 시간은 실시간 운영체제의 중요한 성능 지표
    - 리얼 타임 이벤트의 응답 시간 결정
- 응용 프로그램에서도 사용할 수 있음
    - 단, 인터럽트 비활성화 상태에서 사용하면 시스템이 멈출 가능성 존재
        
        → RTOS 종류마다 다르다. FreeRTOS의 경우 문제 발생
        
    - ex. vTaskDelay()
- **FreeRTOS 서비스를 호출할 때 인터럽트는 꼭 활성화 상태여야 한다.**

### FreeRTOS의 절대 우선 순위

taskENTER_CRITICAL >> 일반 인터럽트 (TICK, IRQ) >> 높은 우선 순위 >> 낮은 우선 순위(IDLE 태스크)