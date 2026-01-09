using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2.Model
{
    public class ProdModel : INotifyPropertyChanged
    {
        private int _id;
        public int Id
        {
            get { return _id; }
            set { _id = value; onPropertyChanged("Id"); }
        }
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; onPropertyChanged("Name"); }
        }
        private int _price;
        public int Price
        {
            get { return _price; }
            set { _price = value; onPropertyChanged("Price"); }
        }
        private int _amount;
        public int Amount
        {
            get { return _amount; }
            set { _amount = value; onPropertyChanged("Amount"); }
        }


        public event PropertyChangedEventHandler? PropertyChanged;
        protected void onPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

    }
    public class Product : INotifyPropertyChanged
    {
        private int _id;
        public int Id
        {
            get { return _id; }
            set { _id = value; onPropertyChanged("Id"); }
        }
        private string _name;
        public string Name
        {
            get { return _name; }
            set { _name = value; onPropertyChanged("Name"); }
        }
        private int _price;
        public int Price
        {
            get { return _price; }
            set { _price = value; onPropertyChanged("Price"); }
        }
        private int _amount;
        public int Amount
        {
            get { return _amount; }
            set { _amount = value; onPropertyChanged("Amount"); }
        }

        private static int cnt;

        public Product(string name, int price, int amount)
        {
            Id = ++cnt;
            Name = name;
            Price = price;
            Amount = amount;
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void onPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}