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
using Newtonsoft.Json;

namespace BA1
{
    [JsonObject(MemberSerialization.OptIn)]
    [DataContract]
    public class BusStop : INotifyPropertyChanged
    {
        #region Saved values

        [DataMember]
        [JsonProperty]
        public double Lat { get; set; }

        [DataMember]
        [JsonProperty]
        public double Lon { get; set; }

        [DataMember]
        [JsonProperty]
        public List<string> Route_Ids { get; set; }

        [DataMember]
        [JsonProperty]
        public string Id { get; set; }

        [DataMember]
        [JsonProperty]
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

        #region Distances

        /// <summary>
        /// Distance left to to destination in meters
        /// </summary>
        public double DistanceLeft
        {
            get 
            {
                return LocationTracker.Location != null ? 
                    this.Location.GetDistanceTo(LocationTracker.Location) : 
                    -1;
            }
        }
        public string DistanceString
        {
            get 
            { 
                return "Distance: " + DistanceLeft.MetersToMiles().ToMilesString(); 
            }
        }

        /// <summary>
        /// Notify distance left and distance strings have changed
        /// </summary>
        public void NotifyDistances()
        {
            this.NotifyPropertyChanged("DistanceLeft");
            this.NotifyPropertyChanged("DistanceString");
        }

        /// <summary>
        /// Notify the location has changed for all stops
        /// </summary>
        public static void NotifyAllDistances()
        {
            foreach (var stop in AppSettings.KnownStops.Value.Values)
            {
                stop.NotifyDistances();
            }
        }

        #endregion

        #region Pin to start

        public const string IdQueryKey = "stop_id";

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

            Uri tileUri = new Uri(string.Format("/Pages/TrackingPage.xaml?{1}={0}", stop.Id, BusStop.IdQueryKey), UriKind.Relative);
            StandardTileData tileData = new StandardTileData()
            {
                Title = stop.Name,
                BackgroundImage = new Uri("/Assets/Tiles/336x336.png", UriKind.Relative)
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

        /// <summary>
        /// Currently pinned stops
        /// </summary>
        public static IEnumerable<BusStop> PinnedStops
        {
            get
            {
                List<BusStop> retval = new List<BusStop>();
                foreach (ShellTile tile in ShellTile.ActiveTiles)
                {
                    string id;
                    if (tile.NavigationUri.TryGetValue(BusStop.IdQueryKey, out id))
                    {
                        if (AppSettings.KnownStops.Value.ContainsKey(id))
                        {
                            retval.Add(AppSettings.KnownStops.Value[id]);
                        }
                    }
                }
                return retval;
            }
        }

        #endregion

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
