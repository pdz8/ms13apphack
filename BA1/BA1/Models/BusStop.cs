using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Device.Location;
using System.ComponentModel;
using Microsoft.Phone.Shell;
using System.Windows;

namespace BA1
{
    [DataContract]
    public class BusStop : INotifyPropertyChanged
    {
        #region Saved values

        [DataMember]
        public double Lat { get; set; }

        [DataMember]
        public double Lon { get; set; }

        [DataMember]
        public List<string> Route_Ids { get; set; }

        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string Name
        {
            get { return this._Name; }
            set { this._Name = value.ConvertAddress(); }
        }

        #endregion

        /// <summary>
        /// Location calculated from Lat and Lon
        /// </summary>
        public GeoCoordinate Location
        { get { return new GeoCoordinate(Lat, Lon); } }
        
        #region StopResult mapping

        private int _number = 0;
        /// <summary>
        /// Map number
        /// </summary>
        public int Number
        {
            get { return _number; }
            set
            {
                _number = value;
                if (!PinSelected) 
                    PinDisplay = Number.ToString();
            }
        }

        private string _PinDisplay = "";
        public string PinDisplay
        {
            get { return _PinDisplay; }
            set
            {
                if (value == _PinDisplay) return;
                _PinDisplay = value;
                NotifyPropertyChanged("PinDisplay");
            }
        }

        private bool _PinSelected = false;
        public bool PinSelected
        {
            get { return _PinSelected; }
            set
            {
                _PinSelected = value;
                if (value) PinDisplay = Name;
                else PinDisplay = Number.ToString();
            }
        }

        #endregion

        /// <summary>
        /// Routes that go to this stop and are known by name to the app
        /// </summary>
        public List<BusRoute> KnownRoutes
        {
            get
            {
                return this.Route_Ids.Where(
                    id => AppSettings.KnownRoutes.Value.ContainsKey(id)).Select(
                    id => AppSettings.KnownRoutes.Value[id]).ToList();
            }
        }
        public string KnownRoutesString
        {
            get
            {
                Queue<BusRoute> rts = new Queue<BusRoute>(this.KnownRoutes);
                if (rts.Count == 0) return "No routes";
                if (rts.Count == 1) return "Route: " + rts.Peek().Name;
                StringBuilder sb = new StringBuilder("Routes: ");
                sb.Append(rts.Dequeue().Name);
                while (rts.Count > 0)
                {
                    sb.Append(", ");
                    sb.Append(rts.Dequeue().Name);
                }
                return sb.ToString();
            }
        }

        #region Pin to start

        /// <summary>
        /// Pin the stop to the start screen. TODO: make pretty
        /// </summary>
        /// <param name="stop"></param>
        public static void PinToStart(BusStop stop)
        {
            if (stop == null) return;
            if (stop.IsPinned)
            {
                MessageBox.Show("This stop is already pinned to start.", "Whoa there", MessageBoxButton.OK);
                return;
            }

            Uri tileUri = new Uri(string.Format("/Pages/TrackingPage.xaml?stop_id={0}", stop.Id), UriKind.Relative);
            StandardTileData tileData = new StandardTileData()
            {
                Title = stop.Name
            };

            ShellTile.Create(tileUri, tileData, false);
        }
        public void PinToStart()
        {
            PinToStart(this);
        }

        /// <summary>
        /// Is this stop pinned to start
        /// </summary>
        public bool IsPinned
        {
            get { return ShellTile.ActiveTiles.Any(s => s.NavigationUri.ToString().Contains(this.Id)); }
        }
        public bool IsNotPinned { get { return !IsPinned; } }

        #endregion

        ///// <summary>
        ///// Used to sort stops by distance on StopResultpage
        ///// </summary>
        ///// <param name="x"></param>
        ///// <param name="y"></param>
        ///// <returns></returns>
        //public static int CompareStopsByDistance(BusStop x, BusStop y)
        //{
        //    if (LocationTracker.Location == null) return 0;
        //    var distx = LocationTracker.Location.GetDistanceTo(x.Location);
        //    var disty = LocationTracker.Location.GetDistanceTo(y.Location);
        //    if (distx < disty) return -1;
        //    if (disty > distx) return 1;
        //    else return 0;
        //}

        private string _Name = "";

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
