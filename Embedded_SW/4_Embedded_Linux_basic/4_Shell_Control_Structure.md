# 임베디드 리눅스 개발자를 위한 리눅스 기본 day04

날짜: 2026년 2월 26일

## if 절 continue

### quote vs not quote

```bash
admin$ foo=[a-z]* name=lhunath
admin$ [[ $name = $foo ]] && echo "Name $name matches pattern $foo"
Name lhunath matches pattern [a-z]*
admin$ [[ $name = "$foo" ]] || echo "Name $name is not equal to the string $foo"
Name lhunath is not equal to the string [a-z]*
```

foo가 인용문구에 들어가면  정규식이 아니라 `[a-z]*` 그 자체로 문자로 인식됨

### if … elif … fi 구문

```bash
admin$ name=lhunath
admin$ if [[ $name = "George" ]]
> then echo "Bonjour, $name"
> elif [[ $name = "Hans" ]]
> then echo "Goeie dag, $name"
> elif [[ $name = "Jack" ]]
> then echo "Good day, $name"
> else
> echo "You're not George, Hans or Jack. Who the hell are you, $name?"
> fi
```

else 대신 elif 사용 가능

## 비교 연산자

### 비교 연산자

- 비교 연산자 `=`, `!=`, `>`, `<` 는 그들의 전달인자를 문자열로 다룸
- 피연산자를 숫자로 처리하려면 다른 연산자 집합을 사용.
    
     `-eq`, `-ne` (not equal), `-lt` (less than), `-gt`, `-le` (less than or equal to), `-ge`
    
- `<` 와 `>`는 bash에서 특별한 의미가 있습니다

### [ 을 이용한 test

glob에서 사용할 수 있는 메타 문자

| Name | Description |
| --- | --- |
| -e FILE | 파일이 있는 경우 True |
| -f FILE | 파일이 일반 파일인 경우 True |
| -d FILE | 파일이 디렉터리인 경우 True |
| -h FILE | 파일이 심볼 링크인 경우 True |
| -p PIPE | 파이프가 있는 경우 True |
| -r FILE | 사용자가 파일을 읽을 수 있는 경우 True |
| -s FILE | 파일이 존재하며 비어 있지 않은 경우 True |
| -t FD | 터미널에서 FD가 열려 있는 경우 True |
| -w FILE | 사용자가 파일을 쓸 수 있는 경우 True |
| -x FILE | 파일이 실행 가능한 경우 True |
| -O FILE | 파일이 사용자가 효과적으로 소유하는 경우 True |
| -G FILE | 파일이 그룹에 효과적으로 소유되는 경우 True |
| FILE -nt FILE | 앞 파일이 뒤 파일보다 최신인 경우 True |
| FILE -ot FILE | 앞 파일이 뒤 파일보다 오레된 경우 True |
| -z STRING | 문자열이 비어있으면 True |
| -n STRING | 문자열이 비어 있지 않은 경우 True |
| STRING = STRING | 앞 문자열이 뒤 문자열와 동일한 경우 True |
| STRING != STRING | 앞 문자열이 뒤 문자열과 동일하지 않은 경우 True |
| STRING < STRING | 앞 문장열이 뒤 문자열보다 먼저 정렬되는 경우 True |
| STRING > STRING | 앞 문자열이 뒤 문자열 뒤에 정렬되는 경우 True |
| EXPR -a EXPR | 두 식이 모두 true이면 true (logical AND) |
| EXPR -o EXPR | 두 식 중 하나가 true이면 true (logical OR) |
| ! EXPR | 표현식의 결과를 반전합니다. (logical NOT) |
| INT -eq INT | 두 정수가 동일한 경우 True |
| INT -ne INT | 정수가 동일하지 않은 경우 True |
| INT -lt INT | 첫 번째 정수가 두 번째 정수보다 작은 경우 True |
| INT -gt INT | 첫 번째 정수가 두 번째 정수보다 큰 경우 True |
| INT -le INT | 첫 번째 정수가 두 번째 정수보다 작거나 같으면 True |
| INT -ge INT | 첫 번째 정수가 두 번째 정수보다 크거나 같은 경우 True |
| STRING = (or ==) PATTERN | `[` 과 같은 문자열 비교는 아니지만 패턴 일치가 수행. 문자열이 글로브 패턴과 일치하는 경우 True |
| STRING != PATTERN | `[` 과 같은 문자열 비교는 아니지만 패턴 일치가 수행. 문자열이 글로브 패턴과 일치하지 않는 경우 True |
| STRING =~ REGEX | 문자열이 regex 패턴과 일치하는 경우 True |
| ( EXPR ) | 괄호를 사용하여 평가 우선 순위를 변경 가능 |
| EXPR && EXPR | 테스트의 `-a` 연산자와 매우 유사하지만 첫 번째 표현식이 이미 거짓으로 판명되면 두 번째 표현식을 평가 X |
| EXPR || EXPR | 테스트의 `-o` 연산자와 매우 유사하지만 첫 번째 표현식이 이미 사실인 경우 두 번째 표현식을 평가 X |

---

## 조건부 루프

### 조건부 루프

- **`while`** 명령
    
    명령이 성공적으로 실행되는 동안 반복합니다 (exit code is 0)
    
- **`until`** 명령
    
    명령이 실행되지 않는 한 반복합니다 (exit code is not 0)
    
- **`for`** *variable* **`in`** *words*
    
    각 단어에 대해 루프를 반복하고 변수를 각 단어에 차례로 설정
    
- **`for`** (( expression; expression; expression ))
    
    첫 번째 산술식을 평가하는 것으로 시작
    
    두 번째 산술 표현식이 성공하는한 루프를 반복 
    
    그리고 각 루프의 끝에서 세 번째 산술식을 평가
    
- `while` 과 `for` 루프

키워드 `do` 가 먼저 나오고, 본문에서 하나 혹은 그 이상의 명령이 나타나고, 마지막으로 키워드 `done`

```bash
admin$ while true
> do echo "Infinite loop"
> done
```

```bash
admin$ while ! ping -c 1 -W 1 1.1.1.1; do
> echo "still waiting for 1.1.1.1"
> sleep 1
> done
```

- `while`

```bash
#!/bin/bash
while true; do
 echo -n -e "\a"; 
 sleep 1; 
done
```

```bash
$ cat numbers.txt 
1
2
3
4
5
6
7
8
9
10
$ sum=0; while read num ; do sum=$(($sum + $num)); done < numbers.txt ; echo $sum
55
```

- `date`

```bash
# 4자리 연도 표시
admin$ date +"%Y-%m-%d"
2017-07-01

# 4자리연도+시간 표시
admin$ date +"%Y-%m-%d %r"
2017-07-01 04:58:21 PM

# 시간을 HH : MM 형식으로 표시 
admin$ date +"%Y-%m-%d %H:%M"
2017-07-01 17:00

# 요일 표시 
admin$ date +"%Y-%m-%d %H:%M %A"
2017-07-01 17:01 토요일

# 포맷형 표시 
admin$ date "+DATE: %Y-%m-%d%nTIME: %H:%M:%S"
DATE: 2017-07-01
TIME: 17:03:15
```

### `while` 과 `for` 루프

```bash
admin$ (( i=10 )); while (( i > 0 ))
> do echo "$i empty cans of beer."
> (( i-- ))
> done
admin$ for (( i=10; i > 0; i-- ))
> do echo "$i empty cans of beer."
> done
admin$ for i in {10..1} # for i in 10 9 8 7 6 5 4 3 2 1 by brace expansion
> do echo "$i empty cans of beer."
> done
```

```bash
admin$ typeset -i i END
admin$ 
admin$ END=10
admin$ 
admin$ for ((i=1;i<=END;++i)); do echo $i; done
1
2
3
4
5
6
7
8
9
10
admin$ 
```

***ERROR***

```bash
admin$ ls
The best song in the world.mp3
admin$ for file in $(ls *.mp3)
> do rm "$file"
> done
rm: cannot remove `The': No such file or directory
rm: cannot remove `best': No such file or directory
rm: cannot remove `song': No such file or directory
rm: cannot remove `in': No such file or directory
rm: cannot remove `the': No such file or directory
rm: cannot remove `world mp3': No such file or directory
```

***인용부호***

```bash
admin$ ls
The best song in the world.mp3 The worst song in the world.mp3
admin$ for file in "$(ls *.mp3)"
> do rm "$file"
> done
rm: cannot remove `The best song in the world.mp3 The worst song in the world.mp3':
No such file or directory
```

`"$(ls *.mp3)"` 이렇게 인용부호를 사용해주어야지 공백을 포함할 수 있다.

***사용 방법***

```bash
admin$ for file in *.mp3
> do rm "$file"
> done
```

### `until` 루프

- `until` 은 `while !` 와 완벽히 동일

```bash
admin$ # Wait for a host to come back online.
admin$ until ping -c 1 -W 1 "$host"
> do echo "$host is still unavailable."
> done; echo -e "$host is available again.\a"
```

### `seq`

```bash
admin$ for i in $( seq 1 10 ); do printf "%03d\t" "$i"; done
001     002     003     004     005     006     007     008     009     010
```

```bash
admin$ seq 5
1
2
3
4
5
```

```bash
admin$ seq 5 -1
5
4
3
2
1
0
-1
```

```bash
admin$ seq 0 2 10
0
2
4
6
8
10
```

```bash
admin$ seq -f "%e" 1 0.5 3
1.000000e+00
1.500000e+00
2.000000e+00
2.500000e+00
3.000000e+00
```

```bash
admin$ seq -s, 1 1 100
1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31
,32,33,34,35,36,37,38,39,40,41,42,43,44,45,46,47,48,49,50,51,52,53,54,55,56,57,58,
59,60,61,62,63,64,65,66,67,68,69,70,71,72,73,74,75,76,77,78,79,80,81,82,83,84,85,
86,87,88,89,90,91,92,93,94,95,96,97,98,99,100,admin$ 
```

## Choices (`case` 와 `select`)

### `case`

- 문자열이 이러한 패턴 중 하나와 일치할 경우 실행할 코드 블록
- 코드 블록의 끝을 나타내는 두 개의 세미콜론 (여러 줄에 써야 할 수도 있기 때문에)
- `case`는 어느 하나가 성공할 경우 패턴 매칭을 즉시 중지
- 다른 기타 선택 사항에 의해 포함되지 않은 사례와 일치시키기 위해 (wildcard) 패턴을 사용한 `case` 을 추가 할 수 있음

### `case` 와 `getopts`

```bash
# getopts.sh
while getopts ":a:" opt; do
  case $opt in
    a)
      echo "-a was triggered, Parameter: $OPTARG" >&2
      ;;
    \?)
      echo "Invalid option: -$OPTARG" >&2
      exit 1
      ;;
    :)
      echo "Option -$OPTARG requires an argument." >&2
      exit 1
      ;;
  esac
done

# Calling it without any arguments
admin$ ./go_test.sh
admin$ 
# Calling it with non-option arguments
admin$ ./go_test.sh /etc/passwd
admin$ 
# Calling it with option-arguments
admin$ ./go_test.sh -b
Invalid option: -b
admin$ 
# Valid option, but without the mandatory argument:
admin$ ./go_test.sh -a
Option -a requires an argument.
admin$ 
# Let's provide the argument:
admin$ ./go_test.sh -a /etc/passwd
-a was triggered, Parameter: /etc/passwd
admin$ 
```

```bash
echo; echo "아무키나 누른 다음 리턴을 치세요."
read Keypress

case "$Keypress" in
  [a-z]   ) echo "소문자";;
  [A-Z]   ) echo "대문자";;
  [0-9]   ) echo "숫자";;
  *       ) echo "구두점이나, 공백문자 등등";;
esac  # [대괄호]속 범위의 문자들을 받아 들입니다.

exit 0
```

### `select`

- `select`은 사용자가 선택할 수 있는 **menu of choices(선택메뉴)** 를 생성하기 위해 사용 가능한 루프와 같은 형태
- 사용자는 선택의 상황에 노출되며 자신의 선택에 따라 번호를 입력하도록 요청 받음, 그런 다음 사용자가 선택한 값으로 설정된 변수를 사용하여, 선택 블록의 코드가 실행
    
    → 사용자의 선택이 올바르지 않으면 변수가 비어 있게 됩니다
    

```bash
admin$ PS3="Which of these does not belong in the group (#)? "; \
> select choice in Apples Pears Crisps Lemons Kiwis; do
> if [[ $choice = Crisps ]]
> then echo "Correct! Crisps are not fruit."; break; fi
> echo "Errr... no. Try again."
> done
```

### 중첩 루프

조건부 구조는 중첩될 수 있음

```bash
#!/bin/bash
# A simple menu:
while true; do
  echo "Welcome to the Menu"
  echo " 1. Say hello"
  echo " 2. Say good-bye"
  read -p "-> " response
  case $response in
    1) echo 'Hello there!' ;;
    2) echo 'See you later!'; break ;;
    *) echo 'What was that?' ;;
  esac
done
# Alternative: use a variable to terminate the loop instead of an
# explicit break command.
quit=
while test -z "$quit"; do
  echo "...."
  read -p "-> " response
    case $response in
    #...
    2) echo 'See you later!'; quit=y ;;
    #...
  esac
done
```

# 배열

## 배열 작성

### 배열 작성

배열을 생성하거나 데이터로 채울 수 있는 몇 가지 방법 존재

→ 데이터의 출처와 데이터가 무엇인가에 따라 다름

- 데이터가 포함된 간단한 array 만드는 방법
    
    ```bash
    admin$ names=("Bob" "Peter" "$USER" "Big Bad John")
    ```
    
    → 많은 요소를 추가하는데 유연성 낮음
    
- 유연성 필요한 경우 명시적 인덱스 지정 가능
    
    ```bash
    admin$ names=([0]="Bob" [1]="Peter" [20]="$USER" [21]="Big Bad John")
    ```
    
    → 1과 20 인덱스 사이에 구멍 존재, 구멍이 있는 배열 ⇒ 스파스 배열
    
- 배열에 파일 이름을 채우는 경우
    
    ```bash
    admin$ photos=(~/"My Photos"/*.jpg)
    ```
    
    My Photos가 인용처리된 이유는 사이 공백 때문
    
- `IFS` 는 문자열을 끊어내는 데 사용
    
    ```bash
    admin$ IFS=. read -a ip_elements <<< "127.0.0.1"
    # .을 기준으로 끊음
    ```
    

## 확장 요소

### 확장 요소

- `for + "${array[@]}"` → 배열 순회
    
    ```bash
    admin$ names=("Bob" "Peter" "$USER" "Big Bad John")
    admin$ for name in "${names[@]}"; do echo "$name"; done
    ```
    
    → 배열을 순회하면서 이름을 출력함
    
    여기서 주의해야 하는 점은 `"${name[@]}"` 인용부호 존재
    
- `"${array[*]}"`
    
    ```bash
    admin$ names=("Bob" "Peter" "$USER" "Big Bad John")
    admin$ echo "Today's contestants are: ${names[*]}"
    Today's contestants are: Bob Peter lhunath Big Bad John
    ```
    
    → 배열 전체를 출력함
    
- `IFS` 를 `"${arrayname[*]}"` 와 결합
    
    ```bash
    admin$ names=("Bob" "Peter" "$USER" "Big Bad John")
    admin$ ( IFS=,; echo "Today's contestants are: ${names[*]}" )
    Today's contestants are: Bob,Peter,lhunath,Big Bad John
    ```
    
    요소 끝마다 , 를 찍어줌
    
- `${#array[@]}`
    
    → 요소의 개수
    

### 확장 지수

동시에 여러 요소를 참조하거나 여러 배열에서 같은 인덱스를 동시에 참조해야 하는 경우

```bash
admin$ first=(Jessica Sue Peter)
admin$ last=(Jones Storm Parker)
admin$ echo "${first[1]} ${last[1]}"
Sue Storm
```

```bash
admin$ for i in "${!first[@]}"; do
> echo "${first[i]} ${last[i]}"
> done
Jessica Jones
Sue Storm
Peter Parker
```

`"${!arrayname[@]}"`  → 배열의 인덱스 목록을 순차적으로 확장함

# 입력과 출력

## 환경

### 환경

### 파일 기술자

- 표준 입력(stdin) : fd 0
- 표준 출력(stdout) : fd1
- 표준 오류(stderr) : fd2

```bash
# 아래 경우 오류 발생시 출력이 나오지 않는다
admin$ mkdir DIR1 2> /dev/null
```

## 방향 재지정

### 방향 재지정

터미널 대신, 파일로 출력을 보내거나, 파일에서 응용 프로그램을 읽을 수 있음

```bash
# 이렇게 리다이렉션을 진행하면 터미널에 출력되지 않고 파일에 저장된다
admin$ echo "It was a dark and stromy night" > story
admin$ cat story
"It was a dark and stromy night"
```

### 파일 방향 재지정

```bash
admin$ for homedir in /home/*
> do rm "$homedir/secret"
> done 2 >> errors
```

# 파이프

## 파이프

### 파이프(pipes)

```bash
admin$ mkfifo pi
```

→ pi라는 파이프가 만들어짐

```bash
admin$ message=Test
admin$ echo 'Salut, le monde!' | read message
admin$ echo "The message is: $message"
The message is: Test
admin$ echo 'Salut, le monde!' | { read message; echo "The message is: $message"; }
The message is: Salut, le monde!
admin$ echo "The message is: $message"
The message is: Test
```

→ 파이프라인이 끝나면 파이프라인을 위해 만들어진 서브 쉘도 끝남.

그래서 `echo 'Salut, le monde!' | { read message; echo "The message is: $message"; }` 이렇게 작성할 때만 **The message is: Salut, le monde!** 이 출력 값을 얻을 수 있음

- 소트
    
    ```bash
    admin$ cat file1 file2 | sort > file4
    ```
    
- scale 은 화면에 출력할 디지트의 갯수를 제한
    
    ```bash
    admin$ var1=2
    admin$ var2=3
    admin$ var3=$(echo "scale=4;$var1/$var2" | bc)
    admin$ echo $var3
    .6666
    admin$
    ```
    
- 지정한 제목의 프로세스를 종료하는 법
    
    ```bash
    admin$ ps -ef | grep your_process_name | grep -v grep | \\
    awk '{print $2}' | xargs kill
    ```
    
- 파일 목록을 소트하여 10줄만큼 만 출력
    
    ```bash
    admin$ ls | head | sort -r >> file1
    ```
    
- **`tee`**
    
    ```bash
    admin$ echo hello > log
    # 이렇게 하면 log 파일에만 들어가고 터미널에서 보이지는않음
    admin$ echo hello | tee log
    # tee를 사용하면 log 파일에도 저장하고 터미널에서도 출력됨 
    ```
    

### `xargs`

xargs 유틸리티는 표준 입력에서 공백, 탭, 개행 및 파일 끝으로 구분된 문자열을 읽고 문자열을 인수로 사용하여 유틸리티를 실행합니다

- xargs 와 echo 활용

```bash
admin$ echo a a a a a a a a  | xargs -n3
a a a
a a a
a a

admin$ echo a a a a a a a a  | xargs -d\\n -n3
a a a a a a a a

admin$ echo a,a,a,a,a,a,a,a,a,a  | xargs -d\\n -n3
a,a,a,a,a,a,a,a,a,a

admin$ echo a,a,a,a,a,a,a,a,a,a  | xargs -d, -n3
a a a
a a a
a a a
a
```

```bash
admin$ echo -e aaa\nbbb | xargs -d\n
aaa bbb

admin$ echo -e "aaa\nbbb" | xargs -d\n
aaa
bbb

admin$ printf "aaa\nbbb" | xargs -d\n
aaa
bbb 
```

## 프로세스 대체

### 복합 명령

1. subshell
2. command grouping
3. arithmetic evaluation
4. functions
5. aliases

### 프로세스 대체 연산자

`<()` , `()>` 두 가지 형식으로 존재

- 두 개의 출력을 각각 파일에 넣고 차이점 찾기
    
    ```bash
    admin$ head -n 1 .dictionary > file1
    admin$ tail -n 1 .dictionary > file2
    admin$ diff -y file1 file2
    Aachen | zymurgy
    admin$ rm file1 file2
    ```
    
- 프로세스 대체 연산자를 사용하면 한 줄로 모든 작업을 수행할 수 있으므로 수동 정리가 필요 X
    
    ```bash
    admin$ diff -y <(head -n 1 .dictionary) <(tail -n 1 .dictionary)
    Aachen | zymurgy
    ```
    

## 서브쉘

자식프로세스 형태로 만들어져 실행되고 사라짐

```bash
# 명령 그룹
admin$ v1 = 1234; {echo $v1; ((v1++)); echo $v1;}; echo $v1
1234
1235
1235
# 서브쉘
admin$ v1 = 1234; (echo $v1; ((v1++)); echo $v1;); echo $v1
1234
1235
1234 # 서브쉘이 종료되면서 서브쉘에서 변경된 사항은 사라짐
```

→ 서브쉘에서 어떤 방법을 쓰더라도 부모 쉘에는 영향을 줄 수 없음

서브쉘은  SandBox라 생각해도 됨

# 함수와 엘리어스

## 함수

```bash
# 두가지 다 함수 정의
admin$ function sum() {
> v=$1
> y=$2
> echo $((v + y))
>}

admin$ sum() {
> v=$1
> y=$2
> echo $((v + y))
>}

# 함수 호출
sum 1 2

# 값을 어디 저장하고 싶은 경우
c=$(sum 1 2 )
```

## Aliases

함수와 별칭의 차이점

별칭은 명령어를 단축시킨 것 뿐

함수는 인수를 받아 연산처리 가능

편리성은 별칭 win

But 인수를 넘겨야하는 경우는 불가능하다는 단점 존재 

## sourcing

만약 환경변수, 별칭을 설정한 경우 `source ~./bashrc` 를 작성해서 반영해줘야 함

`source ~./bashrc`  == `. ~./bashrc`

# 작업 제어

## 작업 제어

### 작업 제어

`^C` : 강제 종료 (SIGINT)

`^Z` : 일시 정지 (SIGTSTP)

`^\` : 코어 덤프와 중단을 야기 (SIGQUIT)

`fg` : 백 그라운드에서 실행되는 것을 포 그라운드로 가져옴  

`dg` : 포 그라운드에서 실행되는 것을 백 그라운드로 가져옴

## 가독성

백 슬래시 이스케이프 자제

일관성 있게 소문자 위주로 작성 (환경변수는 대문자로 작성해야 하기 때문)

### 배쉬 테스트 [ vs [[

```bash
admin$ var=''
admin$ [ $var = '' ] && echo True
-bash: [: =: unary operator expected
admin$ [ "$var" = '' ] && echo True
True
admin$ [[ $var = '' ]] && echo True
True

admin$ var=
admin$ [ "$var" < a ] && echo True
-bash: a: No such file or directory
admin$ [ "$var" \< a ] && echo True
True
admin$ [[ $var < a ]] && echo True
True
```

가독성 측면에서는 test를 `[[`로  사용하는 것이 좋다

## 디버깅

### `set -x`

vim 편집기 안에 `set -x` 를 사용하면 실행되는 과정이 모두 출력됨.

### `cron`

```bash
crontab -e
no crontab for user - using an empty one

Select an editor.  To change later, run 'select-editor'.
  1. /bin/nano        <---- easiest
  2. /usr/bin/vim.basic
  3. /usr/bin/vim.tiny
  4. /bin/ed

Choose 1-4 [1]: 1
No modification made

#----------------------편집기
*****(정해진 시간마다 정해진 작업을 하도록 작성)
```