using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    internal class memo
    {
        private string path; // 메모 디렉토리 경로
        private DirectoryInfo info;

        public memo(string path)
        {
            this.path = path;
            info = new DirectoryInfo(path);
            if (!info.Exists)
            {
                info.Create();
            }
        }

        // 파일 목록 출력. 파일 선택 함수
        public string printFileList()
        {
            FileInfo[] files = info.GetFiles();
            int i;
            for (i = 0; i < files.Length; i++)
            {
                Console.WriteLine(i + " " + files[i].Name);
            }
            Console.WriteLine("파일의 번호를 선택하시오");
            int idx = Int32.Parse(Console.ReadLine());

            if (idx < 0 || idx > files.Length)
            {
                Console.WriteLine("잘못된 번호");
                return null;
            }
            return files[idx].Name;
        }

        public void read()
        {
            Console.WriteLine("===읽기===");
            string fname = printFileList();
            if (fname == null) { 
                Console.WriteLine("파일 잘못 선택, 읽기 종료");
                return;
            }
            Console.WriteLine(File.ReadAllText(path + "/" + fname));
        }

        public void write()
        {
            Console.WriteLine("==쓰기==");
            string fname = null;
            bool flag = true;
            int mode = 1; // mode: 1은 새로 쓰기, 2는 이어쓰기 
            while (flag)
            {
                Console.WriteLine("파일명: ");
                fname = Console.ReadLine();
                int m; // mode: 1은 새로 쓰기, 2는 이어쓰기 
                if (File.Exists(path + "/" + fname))
                {
                    Console.WriteLine("1. 파일명 다시 입력 2. 새로쓰기 3. 덮어쓰기");
                    m = Int32.Parse(Console.ReadLine());
                    switch (m)
                    {
                        case 2:
                            flag = false;
                            break;
                        case 3:
                            mode = 2;
                            flag = false;
                            break;
                    }
                }
                else
                {
                    flag = false;
                }
            }

            // 문자열을 저장하는 string 클래스와 같음
            // 문자열 조작을 빠르게 처리
            StringBuilder stringBuilder = new StringBuilder();
            flag = true;
            Console.WriteLine("파일 내용 입력. 멈추려면 /stop 입력");
            string ss;
            while (flag)
            {
                ss = Console.ReadLine();
                if (ss.StartsWith("/stop"))
                {
                    break;
                }
                stringBuilder.Append(ss + "\n");
            }
            
            // 파일에다가 씀
            if(mode == 2)
            {
                File.AppendAllText(path + "/" + fname, stringBuilder.ToString());
            }
            else
            {
                File.WriteAllText(path + "/" + fname, stringBuilder.ToString());
            }
        }

        public void delete()
        {
            Console.WriteLine("==삭제==");
            string fname = printFileList();
            if(fname == null)
            {
                Console.WriteLine("파일 잘못 선택, 삭제 종료");
                return;
            }
            File.Delete(path + "/" + fname);
        }
    }
}
