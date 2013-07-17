using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Devices.Geolocation;
using System.Device.Location;
using System.Windows.Threading;
using System.Windows.Controls;

namespace BA1
{
    /// <summary>
    /// Manages all location tracking
    /// </summary>
    public static class LocationTracker
    {
        #region Properties

        private static GeoCoordinate _Location;

        /// <summary>
        /// Most recently recorded position
        /// </summary>
        public static GeoCoordinate Location
        {
            get { return _Location; }
            set
            {
                if (value == null) return;
                _Location = value;

                if (PanoPage.CurrentInstance != null)
                {
                    PanoPage.CurrentInstance.Dispatcher.BeginInvoke(() =>
                        {
                            ProgressIndicatorHelper.Instance.Remove(LoadingEnum.Location);
                            BusStop.NotifyAllDistances();
                        });
                }
                if (StopResultPage.CurrentInstance != null && StopResultVM.CurrentInstance != null)
                {
                    StopResultPage.CurrentInstance.Dispatcher.BeginInvoke(() => 
                        {
                            ProgressIndicatorHelper.Instance.Remove(LoadingEnum.Location);
                            StopResultVM.CurrentInstance.UpdateFromLocation();
                            BusStop.NotifyAllDistances();
                        });
                }
                if (TrackingPage.CurrentInstance != null && TrackingVM.CurrentInstance != null)
                {
                    TrackingPage.CurrentInstance.Dispatcher.BeginInvoke(() =>
                        {
                            ProgressIndicatorHelper.Instance.Remove(LoadingEnum.Location);
                            TrackingVM.CurrentInstance.UpdateFromLocation();
                        });
                }
            }
        }
        public static bool HasLocation
        {
            get { return Location != null; }
        }

        /// <summary>
        /// Geolocator used to find position
        /// </summary>
        private static Geolocator GPS;

        /// <summary>
        /// Current usage of the GPS
        /// </summary>
        public static TrackingEnum TrackState { get; private set; }
        public static bool IsTracking
        { get { return TrackState == TrackingEnum.Tracking; } }

        #endregion

        static LocationTracker()
        {
            GPS = new Geolocator();
            GPS.DesiredAccuracy = PositionAccuracy.High;
            GPS.DesiredAccuracyInMeters = 50;
            //GPS.MovementThreshold = 100;
            GPS.ReportInterval = 1000;

            //IsTracking = false;
            TrackState = TrackingEnum.Off;
        }

        /// <summary>
        /// Convert Geoposition to GeoCoordinate
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public static GeoCoordinate ToGeoCoordinate(this Geoposition pos)
        {
            return new GeoCoordinate(pos.Coordinate.Latitude, pos.Coordinate.Longitude);
        }

        #region LocationTracker API

        /// <summary>
        /// Populate LocationTracker.Location with the current location
        /// </summary>
        /// <returns></returns>
        public static async Task<GeoCoordinate> RetrieveLocationAsync()
        {
            Geoposition pos = await GPS.GetGeopositionAsync();
            Location = pos.ToGeoCoordinate();
            return Location;
        }

        /// <summary>
        /// Start single use tracking
        /// </summary>
        public static void RetrieveLocation()
        {
            if (TrackState == TrackingEnum.Tracking) return;
            if (TrackState == TrackingEnum.SingleUse) return;

            TrackState = TrackingEnum.SingleUse;

            GPS.PositionChanged += GPS_PositionChanged;
            GPS.StatusChanged += GPS_StatusChanged;
        }

        /// <summary>
        /// Start active tracking
        /// </summary>
        public static void StartTracking()
        {
            TrackState = TrackingEnum.Tracking;

            GPS.PositionChanged += GPS_PositionChanged;
            GPS.StatusChanged += GPS_StatusChanged;
        }

        /// <summary>
        /// Stop all tracking
        /// </summary>
        public static void StopTracking()
        {
            GPS.PositionChanged -= GPS_PositionChanged;
            GPS.StatusChanged -= GPS_StatusChanged;
           
            TrackState = TrackingEnum.Off;
        }

        #endregion

        static void GPS_PositionChanged(Geolocator sender, PositionChangedEventArgs args)
        {
            Location = args.Position.ToGeoCoordinate();

            if (TrackState == TrackingEnum.SingleUse)
            {
                StopTracking();
            }
        }

        static void GPS_StatusChanged(Geolocator sender, StatusChangedEventArgs args)
        {
            switch (args.Status)
            {
                case PositionStatus.NotAvailable:

                    break;
                case PositionStatus.NoData:

                    break;
                case PositionStatus.Initializing:

                    break;
                case PositionStatus.Disabled:

                    break;
                case PositionStatus.Ready:

                    break;
                case PositionStatus.NotInitialized:

                    break;
            }
        }

    }

    /// <summary>
    /// Current usage of the GPS
    /// </summary>
    public enum TrackingEnum
    {
        Off,
        SingleUse,
        Tracking
    }

    /// <summary>
    /// Determines the average speed of a trip
    /// </summary>
    public class SpeedTracker
    {
        #region Current speed

        /// <summary>
        /// Most recent position given to this
        /// </summary>
        private GeoCoordinate LastPosition = null;

        /// <summary>
        /// Time the most recent position was recorded at
        /// </summary>
        private DateTime LastTime;

        /// <summary>
        /// How many minutes should the threshold be
        /// </summary>
        private const double THRESHOLD_FACTOR = 1.5;

        /// <summary>
        /// Current speed in m/s
        /// </summary>
        public double InstataneousSpeed { get; private set; }

        /// <summary>
        /// Recommended threshold in miles
        /// </summary>
        public double RecommendedThreshold
        {
            get
            {
                if (this.InstataneousSpeed <= 0) return 0;

                double retval = this.InstataneousSpeed * THRESHOLD_FACTOR * 60;
                retval = retval.MetersToMiles();

                return retval;
            }
        }

        /// <summary>
        /// Get threshold that corresponds to slider precision
        /// </summary>
        /// <param name="slider"></param>
        /// <returns></returns>
        public double GetSliderThreshold(Slider slider)
        {
            if (this.RecommendedThreshold >= slider.Maximum) return slider.Maximum;
            if (this.RecommendedThreshold <= slider.Minimum) return slider.Minimum;

            double smallChange = Math.Abs(slider.SmallChange);
            double retval = slider.Minimum;

            do
            {
                retval += smallChange;
            } while (this.RecommendedThreshold > retval && retval < slider.Maximum);

            return retval;
        }
        public double GetSliderThreshold()
        {
            return this.GetSliderThreshold(this.SliderControl);
        }

        #endregion

        /// <summary>
        /// The slider that is referenced to get limits
        /// </summary>
        public Slider SliderControl { get; set; }

        /// <summary>
        /// Create new speed tracker
        /// </summary>
        /// <param name="slider">Slider</param>
        public SpeedTracker(Slider slider)
        {
            this.SliderControl = slider;
        }

        /// <summary>
        /// Update the current position
        /// </summary>
        /// <param name="pos"></param>
        public void AddPosition(GeoCoordinate pos)
        {
            // Initialize last position
            if (this.LastPosition == null)
            {
                this.LastPosition = pos;
                return;
            }

            // Current speed
            double secondsSinceLast = (DateTime.Now - this.LastTime).TotalSeconds;
            if (secondsSinceLast > 0)
            {
                this.InstataneousSpeed = this.LastPosition.GetDistanceTo(pos) / secondsSinceLast;
            }

            // Update LastPosition
            this.LastPosition = pos;
            this.LastTime = DateTime.Now;
        }
    }

}
