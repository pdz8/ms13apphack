using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Device.Location;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Devices.Geolocation;

namespace BA1
{
    /// <summary>
    /// ViewModel for TrackingPage.xaml
    /// </summary>
    public class TrackingVM : INotifyPropertyChanged
    {
        /// <summary>
        /// BusStop that we set the geofence around
        /// </summary>
        public BusStop Context { get; private set; }

        public TrackingVM(BusStop bs)
        {
            this.Context = bs;
            //this.HeaderTitle = "Bus alarm";
            //this.HeaderTitle = "bus alarm - " + this.Context.Name;
            this.HeaderTitle = this.Context.Name;
            //this.Title = this.Context.Name;
            this.Title = "stop alarm";
            this.StopName = this.Context.Name;

            TrackingVM.CurrentInstance = this;

            // Start speed tracker
            this.SpeedTracker = new SpeedTracker(this.Context.Location);
            if (LocationTracker.HasLocation)
            {
                this.SpeedTracker.AddPosition(LocationTracker.Location);
            }
        }

        /// <summary>
        /// Not a singleton
        /// </summary>
        public static TrackingVM CurrentInstance { get; private set; }

        #region Mapping properties & stats

        /// <summary>
        /// Location at launch of view
        /// </summary>
        public GeoCoordinate MyLocation
        {
            get { return LocationTracker.Location ?? new GeoCoordinate(); }
        }

        /// <summary>
        /// Destination location
        /// </summary>
        public GeoCoordinate StopLocation
        {
            get { return this.Context.Location; }
        }

        /// <summary>
        /// Distance left to to destination in meters
        /// </summary>
        public double DistanceLeft
        {
            get { return StopLocation.GetDistanceTo(MyLocation); }
        }
        public string DistanceString
        {
            get { return DistanceLeft.MetersToMiles().ToMilesString(); }
        }

        /// <summary>
        /// Threshold that triggers the alarm (miles)
        /// </summary>
        public double Threshold
        {
            get { return AppSettings.AlarmThreshold.Value; }
            set
            {
                if (value == AppSettings.AlarmThreshold.Value) return;
                AppSettings.AlarmThreshold.UpdateSave(value);
                NotifyPropertyChanged("Threshold");
                NotifyPropertyChanged("ThresholdString");
            }
        }
        public string ThresholdString
        {
            get { return Threshold.ToMilesString(); }
        }

        #endregion

        /// <summary>
        /// Update fields of VM from the location
        /// </summary>
        /// <param name="location"></param>
        public void UpdateFromLocation()
        {
            // Update speed tracker
            this.SpeedTracker.AddPosition(LocationTracker.Location);

            // Notify properties
            this.NotifyPropertyChanged("MyLocation");
            this.NotifyPropertyChanged("DistanceLeft");
            this.NotifyPropertyChanged("DistanceString");
            this.NotifyPropertyChanged("EstimatedTimeString");
            this.NotifyPropertyChanged("AverageSpeedString");

            if (this.DistanceLeft.MetersToMiles() <= this.Threshold)
            {
                LocationTracker.StopTracking();
                //MessageBox.Show("Your stop is coming up soon!", "Non-final alarm", MessageBoxButton.OK);
                AlarmHandler.Instance.TriggerAlarm(this.Context);
            }

            this.NotifyPropertyChanged("LeftButtonText");
        }

        #region Track control

        /// <summary>
        /// Initialize all controls for geofencing.
        /// This should only be called in foreground.
        /// </summary>
        public void BeginGeofence()
        {
            LocationTracker.StartTracking();
            this.NotifyLeftButton();
            AlarmHandler.Instance.PrepareAlarm(this.Context);
        }

        /// <summary>
        /// Stop the geofencing.
        /// This should only be called in foreground.
        /// </summary>
        public void StopGeofence()
        {
            LocationTracker.StopTracking();
            this.NotifyLeftButton();
            AlarmHandler.Instance.ClearAlarms();
        }

        #endregion

        #region Speed tracking

        /// <summary>
        /// Helps determine estimated time left
        /// </summary>
        public SpeedTracker SpeedTracker { get; private set; }

        public string EstimatedTimeString
        {
            get { return SpeedTracker.EstimatedTimeLeft.ToTimeString(); }
        }

        public string AverageSpeedString
        {
            get { return (SpeedTracker.AverageSpeed.MetersToMiles() * 60).ToMPHString(); }
        }

        #endregion

        #region Button properties

        public const string BtnStartTracking = "start tracking";
        public const string BtnStopTracking = "stop tracking";
        public const string BtnPinToStart = "pin to start";

        public void NotifyLeftButton()
        {
            this.NotifyPropertyChanged("LeftButtonText");
        }

        public string LeftButtonText
        {
            get { return LocationTracker.TrackState == TrackingEnum.Off ? BtnStartTracking : BtnStopTracking; }
        }

        public string RightButtonText
        {
            get { return BtnPinToStart; }
        }

        public bool RightButtonEnabled
        {
            get
            {
                return !this.Context.IsPinned;
            }
        }

        #endregion

        #region Page titling

        /// <summary>
        /// Name of stop
        /// </summary>
        public string StopName
        {
            get { return this._StopName; }
            set
            {
                if (value == this._StopName) return;
                this._StopName = value;
                NotifyPropertyChanged("StopName");
            }
        }

        /// <summary>
        /// Large lowercase title
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

        private string _StopName = "";
        private string _HeaderTitle = "";
        private string _Title = "";

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
