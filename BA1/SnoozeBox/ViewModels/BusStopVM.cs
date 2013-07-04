using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Device.Location;
using Newtonsoft.Json;

namespace SnoozeBox.ViewModels
{
    public class BusStopVM : INotifyPropertyChanged
    {
        public string Name
        {
            get { return _Name; }
            set
            {
                if (value == _Name) return;
                _Name = value;
                NotifyPropertyChanged("Name");
            }
        }

        public double Lat
        {
            get { return _Lat; }
            set
            {
                if (value == _Lat) return;
                _Lat = value;
                NotifyPropertyChanged("Lat");
            }
        }

        public double Lon
        {
            get { return _Lon; }
            set
            {
                if (value == _Lon) return;
                _Lon = value;
                NotifyPropertyChanged("Lon");
            }
        }

        public string Summary_Text
        {
            get { return _Summary_Text; }
            set
            {
                if (_Summary_Text == value) return;
                _Summary_Text = value;
                NotifyPropertyChanged("Summary_Text");
            }
        }

        public string Summary_Html
        {
            get { return _Summary_Html; }
            set
            {
                if (_Summary_Html == value) return;
                _Summary_Html = value;
                NotifyPropertyChanged("Summary_Html");
            }
        }

        public List<BusRouteVM> Route_Summary
        {
            get { return _Route_Summary; }
            set
            {
                if (_Route_Summary == value) return;
                _Route_Summary = value;
                NotifyPropertyChanged("Route_Summary");
            }
        }

        public string Id
        {
            get { return _Id; }
            set
            {
                if (_Id == value) return;
                _Id = value;
                NotifyPropertyChanged("Id");
            }
        }

        public GeoCoordinate Location
        { get { return new GeoCoordinate(Lat, Lon); } }
        
        private string _Name;
        private double _Lat;
        private double _Lon;
        private string _Summary_Text;
        private string _Summary_Html;
        private List<BusRouteVM> _Route_Summary;
        private string _Id;

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
