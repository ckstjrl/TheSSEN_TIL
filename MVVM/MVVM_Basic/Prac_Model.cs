/*
 Visual Studio -> 도구 -> NuGet 패키지 관리자 -> 솔루션용 NuGet 패키지 관리
'CommunityToolkit.Mvvm'
'Microsoft.Xaml.Behaviors.Wpf'
설치 진행
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Mvvm
{
    class Model : INotifyPropertyChanged
    {
        private int num = 1;
        public int Num
        {
            get { return num; }
            set { num = value; onPropertyChanged("Num"); }
        }

        private string name = "aaa";
        public string Name
        {
            get { return name; }
            set { name = value; onPropertyChanged("Name"); }
        }

        private string res;
        public string Res
        {
            get { return res; }
            set { res = value; onPropertyChanged("Res"); }
        }

        // 이벤트 객체 : 특수 형태의 델리게이트 -> 이벤트 핸들러를 대신 호출해주는 델리게이트
        // 이벤트 핸들러 : void handlerfunc(object sender, EventArgs e);
        // delegate void xxx(int, int); -> 델리게이트 이름이 xxx
        // -> xxx f(대신 호출할 함수); -> f(1, 2) -> 등록한 함수를 f라는 이름으로 호출 가능
        // 여기서 대신 호출할 함수의 형식은 리턴이 void이고 parameter가 int 2개 형식이어야함.

        public event PropertyChangedEventHandler? PropertyChanged; // 여기서 PropertyChanged의 경우 델리게이트 이름일 뿐
        protected void onPropertyChanged(string propertyName)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    class ViewModel : INotifyPropertyChanged
    {
        private Model model = null;
        public Model Model
        {
            get { return model; }
            set { model = value; onPropertyChanged("Model"); }
        }

        public int idx { get;set; }

        private Person p;
        public Person P
        {
            get { return p; }
            set { p = value; onPropertyChanged("P"); }
        }

        private ObservableCollection<Person> plist = null;
        public ObservableCollection<Person> Plist { get { return plist; } }

        public Command Cmd { get; set; }
        public Command SaveCmd { get; set; }
        public Command CmdSelChanged { get; set; }

        public ViewModel()
        {
            model = new Model();
            Cmd = new Command(cmd_exe);
            SaveCmd = new Command(savecmd_exe);
            plist = new ObservableCollection<Person>();
            //  plist.Add(new Person(1, "aaa"));
            //  plist.Add(new Person(2, "bbb"));
            CmdSelChanged = new Command(sel_exe);
        }

        public void cmd_exe(object obj)
        {
            Model.Res = Model.Num + " / " + Model.Name;
        }
        public void savecmd_exe(object obj)
        {
            Plist.Add(new Person(Model.Num, Model.Name));
        }

        public void sel_exe(object obj)
        {
            /* SelectedIndex 활용
            Model.Num = Plist[idx].Num;
            Model.Name = Plist[idx].Name;
            */
            // SelectedItem 활용
            Model.Num = P.Num;
            Model.Name = P.Name;
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void onPropertyChanged(string propertyName)
        {
            PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // 이벤트를 명령어 화
    // 이벤트 발생시 실행할 함수에 대한 델리게이트를 정의 클래스
    class Command : ICommand
    {
        Action<object> action;

        public Command(Action<object> action)
        {
            this.action = action;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return true;
        }

        public void Execute(object? parameter)
        {
            action(parameter);
        }
    }
    class Person
    {

        public int Num { get; set; }

        public string Name { get; set; }

        public Person() { }
        public Person(int num, string name)
        {
            Num = num;
            Name = name;
        }
    }
}

