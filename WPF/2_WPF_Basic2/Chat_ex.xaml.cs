using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Chat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private TcpClient client;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;

        public MainWindow()
        {
            InitializeComponent();
            client = new TcpClient();
            client.Connect("localhost", 4567);  //서버 접속
            stream = client.GetStream();
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);
            new Thread(new ThreadStart(readMsg)).Start();
        }

        public void readMsg()
        {
            string msg;
            while (true)
            {
                msg = reader.ReadLine();
                if (msg.StartsWith("/exit"))
                {
                    break;
                }
                //텍스트블록에 메시지 출력
                Application.Current.Dispatcher.Invoke(() => {
                    body.Text += msg + "\n";
                });
            }
            client.Close();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            writer.WriteLine(input.Text + "\n");
            writer.Flush();
            input.Text = "";
        }


    }
}