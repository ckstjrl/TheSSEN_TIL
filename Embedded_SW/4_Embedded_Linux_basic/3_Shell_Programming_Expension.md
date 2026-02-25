# 임베디드 리눅스 개발자를 위한 리

날짜: 2026년 2월 25일

# 확장

## 정규표현식

### 정규표현식 사용과 grep

- 정규 표현식
    - 다른 문자열을 검색하거나 치환할목적으로 고란된 특수 문자열
    - grep, sed, awk 등 강력한 유닉스 명령행 도구 중 일부는 정규 표현식을 사용
- 예시
    - golf ;; 기본 검색
    - [Gg]olf ;; 대괄호 사용하기

### 정규표현식

- globs에서 사용할 수 있는 메타 문자

| 기호 | 내용 |
| --- | --- |
| [abc] | a, b 또는 c의 단일 문자 |
| [a-z] | 범위의 모든 단일 문자 az |
| ^ | 라인의 시작 |
| . | 모든 단일문자  |
| \S | 비 공백 문자 |
| \D | 임의의 비 숫자 |
| \W | 비 단어 문자 |
| (…) | 동봉된 모든 것을 캡쳐하십시오 |
| a? | 0 또는 하나의 |
| a+ | 하나 이상의 |
| a{3,} | 3개 이상의 |
| [^abc] | a, b 또는 c를 제외 모든 문자 |
| [a-zA-Z] | az 또는 AZ범위의 모든 문자 |
| $ | 줄의 끝 |
| \s | 공백 문자 |
| \d | 임의의 숫자 |
| \w | 모든 단어 문자 (문자, 숫자, 밑줄) |
| \b | 모든 단어 경계 |
| (a\ | b) |
| a* | 0개 이상의 |
| a{3} | 정확히 3 |
| a{3,6} | 3 ~ 6개 사이 |

### 정규표현식 - langRegex

캡쳐 그룹을 사용하는 정규식 패턴은 나중의 검색을 위해 캡쳐된 문자열을 `BASH_REMATCH` 변수에 할당

- BASH에서 regex를 사용하는 방법

![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image.png)

→ 정규식 비교시 “” 인용 부호 사용 X

### regex

`vim ../../regex.sh`

![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%201.png)

![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%202.png)

### grep

파일이나 디렉토리 내부 대상으로 정규식 검색해주는 기능

- 정규식 검색, 검색 일치된 횟수
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%203.png)
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%204.png)
    
    → 찾고 싶은 패턴은 인용 부호 필수
    
    `grep "lines.*empty" demo_file`
    
    → 명령어 / 찾고 싶은 패턴 / 탐색할 위치
    
- 검색 일치된 텍스트 출력력
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%205.png)
    
    `-c` : count
    
    `-i` : ignore case → 대소문자 무시하고 검색하겠다
    
    `-r` : 디렉토리 내부 모든 걸 탐색해서 찾음
    
- 검색 일치된 텍스트만 출력, 검색 일치된 텍스트의 위치 출력
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%206.png)
    
    `-o` : only, 패턴 매칭이 된 것만 출력
    
    `-b` : 타겟이 몇 번째 gyte에서 나오는지 출력력
    
- OR 이용한 검색
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%207.png)
    
    `-e` , `egrep` , `-E` : 인용구문 내부의 이스케이프 문자 쓸 필요 없음
    
- AND를 이용한 검색
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%208.png)
    
- NOT을 이용한 검색
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%209.png)
    
    `-v` : 타겟 문자열이 없는 것만 출력력
    
- 공백 라인의 개수 계산
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2010.png)
    
    `"^$"` : 1번 라인에 아무것도 없는 것 = 공백 라인 
    
- 라인의 시작과 끝을 검색
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2011.png)
    
    `^`  : 이걸 사용하면 줄의 맨 앞에 있는 것만 Matching 
    
- `\+` 특수 문자의 앞글자가 1회 이상 반복되는 경우를 검색
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2012.png)
    
- `*` 특수 문자의 앞글자가 0회 이상 반복되는 경우를 검색
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2013.png)
    
- `\?` 특수 문자의 앞글자가 0 or 1회 되는 경우를 검색
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2014.png)
    
- `-B`
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2015.png)
    
    `-B 1` : 매칭된 문장의 한 개전 까지 출력
    
    `-B 2` : 매칭된 문장의 두 개전 까지 출력
    
    `-B 3` : 매칭된 문장의 세 개전 까지 출력 …
    
    `-A` : 매칭된 문장의 뒷 문장 출력
    
- 지정한 글자로 시작하지 않는 문자열 검색
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2016.png)
    
- ifconfig에서 IP만 반환하는 스크립트
    
    ```bash
    ifconfig | grep -A 1 ens33 | egrep -o 'inet [0-9]{1,}\.[0-9]{1,}\.[0-9]{1,}\.[0-9]{1,}'
    ```
    
- 스크립트 전달 인수 체크 (egrep.sh)
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2017.png)
    
    → `/dev/null/` : 복원시킬 수 없는 휴지통
    
    휴지통으로 집어넣기 때문에 화면에 나오지 않고 결과만 저장
    
    → `-q` 를 사용해도 화면에 나오지 않지만 이는 휴지통에 넣는 것 X
    
- `fgrep`
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2018.png)
    
    `fgrep` 을 활용하면 비 정규식 검색 즉, 평문 검색을 한다.
    
- 파일 목록만 얻고 싶을 때
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2019.png)
    
- 워드바운더리 `\b`
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2020.png)
    

### find

파일이나 디렉토리를 찾는 기능

- find를 사용해서 확장자가 m4v인 파일을 현재 디렉토리 내부에서 찾기
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2021.png)
    
- depth를 정해서 찾을 수 있다
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2022.png)
    
- usr 폴더 내부에 있는 모든 header 파일 찾기
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2023.png)
    
- usr 폴더 내부에 있는 모든 header 파일을 찾고 수정 일자, 용량 등 자세한 정보 출력
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2024.png)
    
- find 명령어는 여러가지 옵션이 AND 결합으로 된다.
    - -not -name
        
        ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2025.png)
        
        `find -not -name 'parameter_expansion.sh` 의 경우
        
        ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2026.png)
        
    - `-type d`  → 디렉토리만 검색
        
        ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2027.png)
        
    - `xdev`
        
        ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2028.png)
        
        root 디렉토리 type, 다른 디렉토리의 type들이 모두 다양하다
        
        /sys, /dev 등 대부분 허상이다. 실제로 존재하지 않음
        
        대부분의 경우 실제 우리가 필요로 하는 것은 ext2/ext3 타입이다.
        
        그러므로 `find / -name '타겟 이름' -xdev` 를 사용하면 root 디렉토리와 같은 타입의 디렉토리만 찾는다.
        
    - `time find / -name 'dsd.h'`
        
        → 실제 찾는 과정이 얼마나 걸렸는지 알려준다.
        
        → time은 다른 명령어에서도 사용할 수있다.
        

### sed

터미널 문서 편집기

- 한 줄 내의 단어 치환 Linux → Linux-Unix
    
    → 실제 파일 내부의 내용을 변경하는 것이 아니라 출력만 변경해주는 것
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2029.png)
    
    → 실제로 변한 건 없음
    
- 한 줄 내 모든 단어 치환 Linux → Linux-Unix
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2030.png)
    
- 한 줄 내의 2번째 단어 치환
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2031.png)
    
- 단어 치환 내용 (output)파일에 저장
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2032.png)
    
    `gpw` : glob + print + write
    
    `-n` 을 쓰면 변경한 것만 뜨게 할 수 있음
    
- `-` 기호 이후의 내용 삭제
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2033.png)
    
    `/\-` → 검색 패턴 → 이런 패턴을 먼저 찾아서 명령 실행
    
    `s/\-.*//g` - 뒤 모든 글자를 없앤다. 
    
- 모든 줄의 뒤 3글자씩 삭제
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2034.png)
    
- 모든 주석 삭제
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2035.png)
    
- 주석과 공백라인 모두 삭제
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2036.png)
    
- 암호파일에서 사용자 이름만 뽑아낸다.
    
    `sed -E 's/([^:]*).*/\1' /etc/passwd`
    
- 각 단어의 첫 번째 글자를 대문자로 변환
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2037.png)
    

## 중괄호 확장

### 동작 방식

![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2038.png)

![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2039.png)

### 디렉토리 계층 구조 만들기

![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2040.png)

### 라인 그리기 (eval)

![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2041.png)

### 배열 출력

![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2042.png)

→ 나중에 백업파일 만들때도 용이하게 사용

## 명령 대체

### 동작 방식

![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2043.png)

## 산술 확장

### 동작 원리

![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2044.png)

### true는 명령어가 아니라 오류 발생하는 case

![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2045.png)

→ 산술 확장은 중첩 가능

## 종료 상태

→ test and conditional : 테스트 결과가 참인지 거짓인지 반환

### $?

→ 종료 코드를 보여준다.

### 제어 연산자

- `&&`
    - `mkdir d && cd d`
        
        → 디렉토리 d를 만들고 성공하면 d로 이동
        
- `||`
    - `rm /etc/some_file.conf || echo "I coulnt remove the file"`
        
        → 파일 삭제가 실패하면 실패 문구 출력
        
- 활용 예시
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2046.png)
    
    ![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2047.png)
    

## 문장 그룹화

- 파일을 삭제하지 못했을 때를 대비하여 오류 메시지를 보여주고 싶음
    
    ```bash
    admin$ grep -q goodword "$file" && ! grep -q badword "$file" \
    && rm "$file" || echo "Couldn't delete $file"
    ```
    
    → 복잡해질수록 가독성 떨어지고, 잘못된 경우 발생 가능성 높음
    
    ```bash
    admin$ grep -q goodword "$file" && ! grep -q badword "$file" && \
    { rm "$file" || echo "Couldn't delete $file" >&2; }
    ```
    
    이렇게 명령어를 그룹화해주는 것이 좋음
    
    → 이때 중괄호 닫기 전에 세미콜론 ; 꼭 넣어야함
    

# 비교문

## 조건부 블록

![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2048.png)

### if 문

![image.png](/Embedded_SW/4_Embedded_Linux_basic/img_for_3/image%2049.png)

주의할 점이 많음.