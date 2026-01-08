using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<string> list = new List<string>(); // 문자열 목록 생성
        List<Person> list2 = new List<Person>(); // 문자열 목록 생성
        
        public MainWindow()
        {
            InitializeComponent();
            list.Add("aaa");
            list.Add("bbb");
            list.Add("ccc");
            list.Add("ddd");

            lst1.ItemsSource = list; // ListBox의 자원 설정
            tel_lst.ItemsSource = list2;
            lst3.ItemsSource = list2;
        }

        // 버튼 1을 누르면 버튼 1의 내용이 메세지 박스로 출력되는 로직
        private void bnt1_Click(object sender, RoutedEventArgs e)
        {
            Button b = (Button)sender;
            MessageBox.Show(b.Content+"");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string str = tb1.Text;
            // TextBox에 문자를 작성하여 설정 버튼을 누르면 Label, TextBlock에 작성한 문자열이 들어가는 로직
            /*
            tbl.Text = str;
            label1.Content = str;
            */
            // TextBox에 작성한 내용을 설정 버튼을 눌러 ListBox에 추가하는 로직
            list.Add((string)str);
            lst1.Items.Refresh(); // ListBox 항목 새로고침
            tb1.Text = "";
        }

        // RadioButton을 클릭하면 해당 content가 메세지 박스에 출력되는 로직
        private void ra1_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = (RadioButton)sender;
            string str = rb.Content + "";
            MessageBox.Show("당신의 기분은 " + str + "입니다.");

        }

        private void c1_Checked(object sender, RoutedEventArgs e)
        {

        }

        // Person클래스를 통해 이름 전화번호를 받아 ListBox에 넣어주는 로직 (저장 버튼)
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            string str_name = name.Text;
            string str_tel = tel.Text;
            list2.Add(new Person(str_name, str_tel));
            tel_lst.Items.Refresh(); // ListBox 항목 새로고침
            lst3.Items.Refresh(); // ListBox 항목 새로고침
            name.Text = "";
            tel.Text = "";
        }


        // sender는 이벤트 소스(이벤트가 발생한 뷰 위젯)
        // e는 이벤트 객체
        private void tel_lst_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listBox = (ListBox) sender;
            int idx = listBox.SelectedIndex;
            if (idx < 0)
            {
                return;
            }

            name.Text = list2[idx].Name;
            tel.Text = list2[idx].Tel;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            int idx = tel_lst.SelectedIndex;
            if (idx < 0)
            {
                return;
            }

            list2[idx].Name = name.Text;
            list2[idx].Tel = tel.Text;

            tel_lst.Items.Refresh();
            lst3.Items.Refresh(); // ListBox 항목 새로고침

            name.Text = "";
            tel.Text = "";
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            int idx = tel_lst.SelectedIndex;
            if (idx < 0)
            {
                return;
            }

            list2.RemoveAt(idx);
            tel_lst.Items.Refresh();
            lst3.Items.Refresh(); // ListBox 항목 새로고침

            name.Text = "";
            tel.Text = "";
        }

        private void bnt2_Click(object sender, RoutedEventArgs e)
        {
            Window1 window1 = new Window1();
            window1.Show();
        }
    }

    class Person
    {
        public string Name { get; set; }
        public string Tel { get; set; }

        public Person() { }
        public Person(string name, string tel)
        {
            Name = name;
            Tel = tel;
        }
        public override string ToString()
        {
            return Name + " / " + Tel;
        }
    }
}