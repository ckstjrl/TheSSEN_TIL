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

namespace Product_list
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<Product> products = new List<Product>();
        public MainWindow()
        {
            InitializeComponent();
            //listbox나 dataGrid는 여러행 출력 뷰이므로
            //이 뷰에서 출력할 배열이나 리스트 등을 연결하여 사용함
            //ItemsSource: listbox, dataGrid에 출력할 리스트를 연결해줌
            prodlist.ItemsSource = products;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            int id = Int32.Parse(idv.Text);
            string name = namev.Text;
            int price = Int32.Parse(pricev.Text);
            int amount = Int32.Parse(amountv.Text);
            bool isout = false;
            if (isoutv.IsChecked != null)
            {
                isout = (bool)isoutv.IsChecked;
            }
            products.Add(new Product(id, name, price, amount, isout));
            prodlist.Items.Refresh();//항목이 변경됐으므로 뷰 리프레시
            idv.Text = "";
            namev.Text = "";
            pricev.Text = "";
            amountv.Text = "";
            isoutv.IsChecked = false;

        }

        private void prodlist_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DataGrid dg = (DataGrid)sender;
            int idx = dg.SelectedIndex;//SelectedIndex:선택위치
            if (idx < 0)
            {
                return;
            }
            Product p = products[idx];
            idv.Text = p.Id + ""; //p.Id는 숫자이므로 숫자+문자열=>문자열
            namev.Text = p.Name;
            pricev.Text = p.Price + "";
            amountv.Text = p.Amount + "";
            isoutv.IsChecked = p.isOut;
            idv.IsReadOnly = true; //읽기전용

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            int idx = prodlist.SelectedIndex;

            int id = Int32.Parse(idv.Text);
            string name = namev.Text;
            int price = Int32.Parse(pricev.Text);
            int amount = Int32.Parse(amountv.Text);
            bool isout = false;
            if (isoutv.IsChecked != null)
            {
                isout = (bool)isoutv.IsChecked;
            }
            products[idx] = new Product(id, name, price, amount, isout);
            prodlist.Items.Refresh();//항목이 변경됐으므로 뷰 리프레시
            idv.Text = "";
            namev.Text = "";
            pricev.Text = "";
            amountv.Text = "";
            isoutv.IsChecked = false;
            idv.IsReadOnly = false;
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            int idx = prodlist.SelectedIndex;
            products.RemoveAt(idx);
            prodlist.Items.Refresh();
        }
    }
    class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Price { get; set; }
        public int Amount { get; set; }
        public bool isOut { get; set; }
        public Product() { }
        public Product(int id, string name, int price, int amount, bool isout)
        {
            Id = id;
            Name = name;
            Price = price;
            Amount = amount;
            isOut = isout;
        }
    }
}