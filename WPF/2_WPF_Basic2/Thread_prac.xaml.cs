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

namespace Thread
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public int num;
        public MainWindow()
        {
            InitializeComponent();
            new Thread(new ThreadStart(counting)).Start(); // 스레드 생성 및 실행
        }

        public void counting()
        {
            for(int i = 0; i < 31; i++)
            {
                
                // ui 스레드의 dispatcher는 ui를 사용할 스레드들을 큐에 넣고 관리
                Application.Current.Dispatcher.Invoke(() => {
                    lnum.Content = i; // 파생 스레드에서 직접 ui에 접근 -> 불가능
                });
                Thread.Sleep(500);
            }
        }
    }
}