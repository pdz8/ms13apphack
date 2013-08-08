using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace BA1
{
    public class StopResultVM : INotifyPropertyChanged
    {
        #region Initialization

        public BusRoute Context { get; private set; }

        public StopResultVM(BusRoute br)
        {
            Context = br;
            Title = Context.Title;
            //this.Title = "stop results";
            //HeaderTitle = "bus alarm - " + Context.Title;
            HeaderTitle = "stop results";
            //this.HeaderTitle = "bus alarm";
            Stops = new ObservableCollection<BusStop>();

            StopResultVM.CurrentInstance = this;
        }

        /// <summary>
        /// Not a singleton
        /// </summary>
        public static StopResultVM CurrentInstance { get; private set; }

        #endregion

        #region Location

        /// <summary>
        /// Location at launch of view
        /// </summary>
        public GeoCoordinate MyLocation
        { 
            get { return LocationTracker.Location ?? new GeoCoordinate(); } 
        }

        /// <summary>
        /// Update from location in LocationTracker
        /// </summary>
        public void UpdateFromLocation()
        {
            this.NotifyPropertyChanged("MyLocation");
        }

        #endregion

        #region Stop loading

        /// <summary>
        /// Get all stops that the route visits
        /// </summary>
        /// <returns></returns>
        public async Task GetStopsOfRoute()
        {
            List<BusStop> stopList = new List<BusStop>();
            if (this.Context == null) return;
            if (this.Context.Stop_Ids == null) return;

            NumStopsVisibility = Visibility.Visible;
            NumStops = 0;
            foreach (string id in this.Context.Stop_Ids)
            {
                BusStop stop;
                if (AppSettings.KnownStops.Value.ContainsKey(id))
                {
                    stop = AppSettings.KnownStops.Value[id];
                }
                else
                {

                    stop = await TransitLoader.GetStop(id);
                    if (stop == null)
                    {
                        break;
                    }
                    await TransitLoader.GetTransitNetwork(stop.Location);
                }
                stopList.Add(stop);
                NumStops++;
            }
            NumStopsVisibility = Visibility.Collapsed;

            // Update Stops Observable Collection
            if (LocationTracker.Location != null)
            {
                stopList = stopList.OrderBy(s => LocationTracker.Location.GetDistanceTo(s.Location)).ToList();
            }
            int numbering = 0;
            foreach (BusStop bs in stopList)
            {
                numbering++;
                bs.Number = numbering;
                this.Stops.Add(bs);
            }
        }

        /// <summary>
        /// Stops to display in ResultsList
        /// </summary>
        public ObservableCollection<BusStop> Stops { get; set; }

        /// <summary>
        /// Current number of loaded stops
        /// </summary>
        public int NumStops
        {
            get { return _NumStops; }
            set
            {
                if (value == _NumStops || value < 0) return;
                _NumStops = value;
                NotifyPropertyChanged("NumStopsString");
            }
        }
        public string NumStopsString
        {
            get 
            { 
                //return NumStops.ToString() + " bus stops found and cached!"; 
                //return "Found " + NumStops.ToString() + " bus stops...";
                return string.Format("Found {0}/{1} bus stops...", this.NumStops, this.Context.Stop_Ids.Count);
            }
        }
        public Visibility NumStopsVisibility
        {
            get { return _NumStopsVisibility; }
            set
            {
                if (value == _NumStopsVisibility) return;
                _NumStopsVisibility = value;
                NotifyPropertyChanged("NumStopsVisibility");
            }
        }

        #endregion

        #region Text properties

        /// <summary>
        /// Large page title
        /// </summary>
        public string Title
        {
            get { return _Title; }
            set
            {
                value = value.Trim().ToLower();
                if (value == _Title) return;
                _Title = value;
                NotifyPropertyChanged("Title");
            }
        }

        /// <summary>
        /// Small uppercase title
        /// </summary>
        public string HeaderTitle
        {
            get { return _HeaderTitle; }
            set
            {
                value = value.Trim().ToUpper();
                if (value == _HeaderTitle) return;
                _HeaderTitle = value;
                NotifyPropertyChanged("HeaderTitle");
            }
        }

        #endregion

        private string _Title = "default title";
        private int _NumStops = 0;
        private Visibility _NumStopsVisibility = Visibility.Visible;
        private string _HeaderTitle;

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
