# 임베디드 리눅스 개발자를 위한 리눅스 기본 day01

날짜: 2026년 2월 23일

# 쉘 프로그래밍의 개요

## 리눅스와 쉘 스크립트

### 터미널에서 자주 사용하는 단축키 모음

```yaml
up / down 화살표 : 이전 명령 검토
Tab : 명령 또는 파일 이름 자동 완성
Tab + Tab : 탭이 완료되지 않으면 가능한 일치 목록 표시
ctrl + Plus / Minus : 터미널 확대 축소
ctrl + L : clear / 화면 지우기
ctrl + Shift + N : 새로운 터미널 
ctrl + Shift + T : 새 탭찰 열기
ctrl + A : 커서 맨 앞으로
ctrl + E : 커서 맨 뒤로
Alt + Left/ Right 화실표 : 단어 단위로 건너 뛰기
ctrl + U : 커서 위치에서 앞쪽을 다 지움
ctrl + K : 커서 위치에서 뒤쪽을 다 지움
ctrl + W : 커서 위치의 앞쪽 단어 지우기
ctrl + Y : 방금 지운 텍스트 붙여 넣기
ctrl + P : 이전 명령 보기
ctrl + N : 다음 명령 표시
ctrl + R : 명령 기록 검색
ctrl + Shift + C : 복사
ctrl + Shift + V : 붙여넣기
```

### 파일 권한 : ALPHA 표기법

- `-rw-rw-r--`
    
    사용자 권한 : 읽기 + 쓰기
    
    그룹 권한 : 읽기 + 쓰기
    
    이외의 권한 : 읽기
    
- `chmod u+x filename`
    
    → `-rwx-rw-r--`
    
    사용자에게 파일 실행 권한 주기
    
- `chmod u-w filename`
    
    → `-rw-rw-r—`
    
    사용자에게 파일 실행 권한 뺏기
    

### 파일 권한 : ALPHA → OCTAL로

r = 4 / w = 2 / x = 1

- `chmod 764 filename`
    
    → `-rwxrw-r--`
    

## BASH란 무엇인가?

### BASH

- Bourne Again SHell
- Bourne 쉘 기반으로 하며 대부분의 기능 호환

### 명령과 인수

- 일반적으로 터미널이나 파일에서 명령을 읽음. 읽는 입력의 각 행은 명령으로 취급
- BASH는 각 행을 공백 문자로 구분되는 단어로 나눔

`ls -l ~/Desktop`

- 행의 첫 번째 단어는 실행될 명령의 이름
- 나머지 모든 단어는 해당 명령어에 대한 인수 (옵션, 파일 이름 등)

### echo

```bash
admin$ echo -n "Enter your id: " ### 에코시 화면에 줄바꿈하지 않도록 하려면
Enter your id: admin$ read id
ckstjrl
admin$ echo "Hello, $id"
Hello, ckstjrl

admin$ echo -e "\nThis\nis\nmy\nworld\n" ### 특수문자의 사용
This
is
my
world

admin$ echo -e "\vThis\vis\vmy\vworld\v"
This
		is
			my	
				world
admin$ echo -e "비프음\a이 들리시나요?"
admin$ echo $'1\n1\n3' > file ### $'의 사용
admin$ cat file
1
2
3
admin$ echo '1\n1\n3' > file
admin$ cat file
1\n2\n\3
```

`-n` : 줄바꿈을 하지 않고 출력 

`-e`, `$` : 인용문 안에 있는 \n과 같은 이스케이프 시퀀스를 인지할 수 있도록 함

`\v` : 수직 tab

`\a` : 비프음이 울리게 하는 이스케이프 시퀀스

`cat` : 파일 내용을 읽어오는 명령어

### 인용문

```bash
admin$ ls 
The voice in your head.mp3
admin$ rm The voice in your head.mp3 ### Executes rm with 5 arguments; not 1!
rm: cannot remove `The': No such file or directory
rm: cannot remove `voice': No such file or directory
rm: cannot remove `in': No such file or directory
rm: cannot remove `your': No such file or directory
rm: cannot remove `head.mp3': No such file or directory

### 에러가 발생하지 않게 하려면
admin$ rm "The voice in your head.mp3"
```

### test 명령어

```bash
admin$ [ -f file ]
```

file이라는 이름의 파일이 존재하는지 확인하는 구문

`[ ]` : 꼭 이렇게 닫아 줘야 사용 가능

그리고 괄호 사이 내용과 space 공백 필수

## 문자열이란

### 문자열

BASH 프르그래밍에서 거의 모든 것은 문자열

- 명령어 이름
- 인수
- 변수 이름
- 변수 내용
- 파일 이름
- 파일 내용

### tail

```bash
### 파일 후미의 10줄 화면에 출력
admin$ tail /var/log/syslog

### 파일 후미의 50줄을 표시
admin$ tail -n 50 var/log/syslog

### 파일의 101 ~ 110행 표시
admin$ cat /var/log/syslog | tail -n +101 | head -n 10
```

### 별칭