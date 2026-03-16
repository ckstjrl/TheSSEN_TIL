# 임베디드 리눅스 커널 프로그래밍 day01

날짜: 2026년 3월 16일

# 환경 구성

## 실습 환경 구성

```bash
# bashrc 설정
echo 'KERNEL_SRC=/workspace/Zybo-Z7-10-Petalinux-2022-1/build/tmp/work-shared/zynq-generic/kernel-source' >> ~/.bashrc

# bashrc 설정 적용
source ~/.bashrc

# 설정파일 생성
make ARCH=arm CROSS_COMPILE=arm-linux-gnueabihf- xilinx_zynq_defconfig

# 설정 UI
make ARCH=arm CROSS_COMPILE=arm-linux-gnueabihf- menuconfig

# 커널 빌드
sudo apt update
sudo apt install u-boot-tools
make ARCH=arm CROSS_COMPILE=arm-linux-gnueabihf- -j4 uImage LOADADDR=0x2000000
# 커널 이미지 생성됨

# 컴파일 시간을 측정할 수 있음
time make ARCH=arm CROSS_COMPILE=arm-linux-gnueabihf- -j4 uImage LOADADDR=0x2000000
```

## 환경 설계

- 부트로더만 SD카드에 고정하고, 나머지는 전부 네트워크에서 가져온다
- SD카드에 저장하는 것 : `BOOT.BIN` 파일 딱 하나 → FSBL, U-Boot만 들어 있음
- TFTP (Trivial File Transfer Protocol) 에서 가져오는 것
    - Linux Kernel 이미지(`uImage`)와 Device Tree Blob(`system.dtb`) 를 네트워크를 통해 가져 옴
    - Uboot가 파일을 가져오는 행위를 하는 주체
- NFS 서버에서 마운트하는 것
    - Ubuntu 리눅스 경로 : `/nfsroot/home/petalinux/zynq_dd`
    - ZYNQ Petalinux 경로 : `/home/petalinux/zynq_dd`
- 부트로더의 환경 변수

```bash
Zynq> printenv bootcmd
bootcmd=setenv loadaddr 0x2000000;setenv fdt_addr 0x2A00000
setenv ramdisk_addr 0x3000000;
setenv verify no;setenv ipaddr 10.0.0.3;
setenv serverip 10.0.0.2;setenv netmask 255.255.255.0;
setenv gatewayip 10.0.0.1;
setenv bootfile uImage;
setenv fdtfile system.dtb;
setenv initrdfile rootfs.cpio.gz;run bootcmd_tftp
```

- U-Boot (= 부트 로더) 핵심 기능
    - OS를 메모리에 올려주는 것이 주 목적이다
    - 하드웨어 초기화
    - 사용자 인터페이스 ( 사용자가 개입할 수 있게 )
    - U-Boot는 임베디드에서 사용하는 가장 유명한 부트로더
    - Ubuntu의 부트로더 == **GNU GRUB**
    - Windows의 부트로더 == **NTLDR**
- Bootargs
    
    ```bash
    bootargs=console=ttyPS0,115200
    # 하드디스크 대신 네트워크 통신으로 가져와라
    root=/dev/nfs rw nfsroot=10.0.0.2:/nfs root,tcp,vers=3
    # IP를 알려주는 곳 
    ip=10.0.0.3:10.0.0.2:10.0.0.2:255.255.255.0
    ::eth0:off rootwait earlyprintk
    ```
    

## ZYNQ-7000 SoC 아키텍처 개요

### PS와 PL 이중 구조

- PS : 하드와이어된 ARM 프로세서와 주변장치 집합. 전원 인가시 즉시 동작, FPGA 프로그래밍 없이도 독립적 사용 가능
    - APU
    - 메모리 인터페이스
    - I/O 주변장치
    - 인터럽트 컨트롤러
    - DMA 컨트롤러
- PL : FPGA에 해당하는 재구성 가능 로직 영역
- PS와 PL사이 연결 통로
    - GP AXI : PS ↔ PL
    - HP AXI : PL → PS DDR
    - ACP : PL → PS Cache
    - EMIO : PS ↔ PL
    - 인터럽트 : PL → PS GIC

### MIO와 EMIO

- MIO : PS에 직접 연결된 54개의 전용핀, PL을 프로그래밍 하지 않아도 사용 가능
- EMIO : PL과 연결된 전용핀
- MIO[7] 활용 LED 실습 코드
    
    ```bash
    #!/bin/bash
    #
    # ledflash.sh
    #
    # 목적: sysfs GPIO 인터페이스를 사용하여 LED를 주기적으로 점멸한다.
    #
    # [수정] CTRL+C 시그널 핸들러 추가
    # 기존 코드: CTRL+C로 종료 시 GPIO가 export된 상태로 남아 다음 실행에서 오류 발생
    # 수정 후: CTRL+C 발생 시 cleanup() 함수를 실행하여 GPIO를 안전하게 해제
    #
    
    GPIO_PIN=${1:-912}   # 첫 번째 인자로 GPIO 번호 지정 (기본값: 912 = MIO7)
    INTERVAL=${2:-1}     # 두 번째 인자로 점멸 간격(초) 지정 (기본값: 1초)
    
    # [추가] 정리 함수: GPIO를 끄고 unexport
    cleanup() {
        echo ""
        echo "LED OFF (cleanup)"
        echo 0 > /sys/class/gpio/gpio${GPIO_PIN}/value 2>/dev/null
        echo "$GPIO_PIN" > /sys/class/gpio/unexport 2>/dev/null
        echo "GPIO $GPIO_PIN 해제 완료"
        exit 0
    }
    
    # [추가] SIGINT(CTRL+C)와 SIGTERM 시그널을 cleanup 함수로 처리
    trap cleanup SIGINT SIGTERM
    
    echo "GPIO ${GPIO_PIN} LED 점멸 시작 (간격: ${INTERVAL}초)"
    echo "종료하려면 CTRL+C를 누르세요"
    
    # GPIO export
    echo "$GPIO_PIN" > /sys/class/gpio/export 2>/dev/null
    
    if [ ! -d "/sys/class/gpio/gpio${GPIO_PIN}" ]; then
        echo "ERROR: GPIO ${GPIO_PIN}을 export할 수 없음"
        echo "  올바른 GPIO 번호인지 cat /sys/kernel/debug/gpio 로 확인하세요"
        exit 1
    fi
    
    # 출력 방향 설정
    echo "out" > /sys/class/gpio/gpio${GPIO_PIN}/direction
    
    # LED 점멸 루프
    while true; do
        echo 1 > /sys/class/gpio/gpio${GPIO_PIN}/value
        echo "LED ON"
        sleep "$INTERVAL"
    
        echo 0 > /sys/class/gpio/gpio${GPIO_PIN}/value
        echo "LED OFF"
        sleep "$INTERVAL"
    done
    
    ```
    
    - `sudo sh -c 'echo 912 > export'` 
    `sudo sh -c 'echo 905 > export’`
        
        ```bash
        Petalinux-2022:/sys/class/gpio$ sudo sh -c 'echo 912 > export'
        Password: 
        Petalinux-2022:/sys/class/gpio$ ls
        export        gpiochip1023  unexport
        gpio912       gpiochip905
        Petalinux-2022:/sys/class/gpio$ sudo sh -c 'echo 905 > export'       
        Petalinux-2022:/sys/class/gpio$ ls
        export        gpio912       gpiochip905
        gpio905       gpiochip1023  unexport
        ```
        
        905 → MIO 0번 제어
        
        9012 → MIO 7번 제어 → LD4 제어
        
        ```bash
        Petalinux-2022:/sys/class/gpio/gpio912$ ls
        active_low  direction   power       uevent
        device      edge        subsystem   value
        Petalinux-2022:/sys/class/gpio/gpio912$ sudo sh -c 'echo out > direct
        ion'
        Petalinux-2022:/sys/class/gpio/gpio912$ sudo sh -c 'echo 1 > value'
        # LED 켜짐
        Petalinux-2022:/sys/class/gpio/gpio912$ sudo sh -c 'echo 0 > value'
        # LED 꺼짐
        ```
        

# 크로스 툴 체인과 드라이버 빌드 기초

## 크로스 컴파일 개념

### 크로스 컴파일의 필요성

개발 PC는 x86_64 아키텍처 기반 CPU

ZYNQ의 경우 ARM Cortex-A9 듀얼 코어 프로세서

CPU의 명령 프로세스가 완전히 다름