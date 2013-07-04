using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SnoozeBox.ViewModels
{
    public class PivotVM : INotifyPropertyChanged
    {
        public PivotVM()
        {
            BusStops = new ObservableCollection<BusStopVM>();
        }

        private static PivotVM _instance;
        public static PivotVM Instance
        {
            get
            {
                if (_instance == null) _instance = new PivotVM();
                return _instance;
            }
        }

        public ObservableCollection<BusStopVM> _BusStops;
        public ObservableCollection<BusStopVM> BusStops
        {
            get { return _BusStops; }
            set
            {
                if (_BusStops == value) return;
                _BusStops = value;
                NotifyPropertyChanged("BusStops");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
