using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WpfApp2.Model;
using WpfApp4;

namespace WpfApp2.ViewModel
{
    internal class ProdViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<Product> plist;
        public ObservableCollection<Product> Plist { get { return plist; } }

        public int SelectedIdx { get; set; }

        public Command CmdSave { get; set; } //항목추가
        public Command CmdEdit { get; set; }    //수정
        public Command CmdDel { get; set; }     //삭제
        public Command CmdSel { get; set; }     //항목선택

        private ProdModel pmodel;
        public ProdModel PModel
        {
            get { return pmodel; }
            set { pmodel = value; onPropertyChanged("PModel"); }
        }

        public ProdViewModel()
        {
            pmodel = new ProdModel();
            plist = new ObservableCollection<Product>();
            CmdSave = new Command(exe_save, canexe);
            CmdEdit = new Command(exe_edit, canexe);
            CmdDel = new Command(exe_del, canexe);
            CmdSel = new Command(exe_sel, canexe);
        }

        private void exe_save(object o)
        {
            Plist.Add(new Product(PModel.Name, PModel.Price, PModel.Amount));
        }
        private void exe_edit(object o)
        {
            Plist[SelectedIdx].Name = PModel.Name;
            Plist[SelectedIdx].Price = PModel.Price;
            Plist[SelectedIdx].Amount = PModel.Amount;
        }
        private void exe_del(object o)
        {
            Plist.RemoveAt(SelectedIdx);
        }
        private void exe_sel(object o)
        {
            if (SelectedIdx < 0)
            {
                return;
            }
            PModel.Id = Plist[SelectedIdx].Id;
            PModel.Name = Plist[SelectedIdx].Name;
            PModel.Price = Plist[SelectedIdx].Price;
            PModel.Amount = Plist[SelectedIdx].Amount;
        }
        private bool canexe(object o)
        {
            return true;
        }
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void onPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
