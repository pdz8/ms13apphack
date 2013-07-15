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
                            StopResultVM.CurrentInstance.UpdateFromLocation();
                            BusStop.NotifyAllDistances();
                        });
                }
                if (TrackingPage.CurrentInstance != null && TrackingVM.CurrentInstance != null)
                {
                    TrackingPage.CurrentInstance.Dispatcher.BeginInvoke(() =>
                        {
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
            GPS.DesiredAccuracyInMeters = 100;
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
        #region Properties

        /// <summary>
        /// Stop location
        /// </summary>
        public GeoCoordinate Destination { get; private set; }

        /// <summary>
        /// Most recent position given to this
        /// </summary>
        private GeoCoordinate LastPosition = null;

        /// <summary>
        /// Time the most recent position was recorded at
        /// </summary>
        private DateTime LastTime;

        /// <summary>
        /// Total distance traveled since creation (meters)
        /// </summary>
        public double DistanceTraveled { get; private set; }

        /// <summary>
        /// Time at which first usable position comes it
        /// </summary>
        private DateTime StartTime;

        /// <summary>
        /// Has received more than 1 location
        /// </summary>
        public bool HasGottenUpdated { get; private set; }

        #endregion

        #region Getters

        /// <summary>
        /// Minutes since start
        /// </summary>
        public double TotalTimeElapsed
        {
            get { return this.HasGottenUpdated ? (DateTime.Now - this.StartTime).TotalMinutes : -1; }
        }

        /// <summary>
        /// Average speed in meters / min
        /// </summary>
        public double AverageSpeed
        {
            get { return this.DistanceTraveled > 0 && this.HasGottenUpdated ? this.DistanceTraveled / this.TotalTimeElapsed : -1; }
        }

        /// <summary>
        /// Distance left in meters
        /// </summary>
        private double DistanceLeft
        {
            get { return this.HasGottenUpdated ? Destination.GetDistanceTo(this.LastPosition) : -1; }
        }

        /// <summary>
        /// Estimate time left in minutes
        /// </summary>
        public double EstimatedTimeLeft
        {
            get { return this.HasGottenUpdated ? this.DistanceLeft / this.AverageSpeed : -1; }
        }

        #endregion

        #region Current speed

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

        #endregion

        /// <summary>
        /// Create new speed tracker
        /// </summary>
        /// <param name="dest">stop location</param>
        public SpeedTracker(GeoCoordinate dest)
        {
            this.DistanceTraveled = 0;
            this.Destination = dest;
            this.HasGottenUpdated = false;
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
                this.StartTime = DateTime.Now;
                return;
            }

            // Current speed
            double secondsSinceLast = (DateTime.Now - this.LastTime).TotalSeconds;
            if (secondsSinceLast > 0)
            {
                this.InstataneousSpeed = this.LastPosition.GetDistanceTo(pos) / secondsSinceLast;
            }

            // Average speed
            this.DistanceTraveled += this.LastPosition.GetDistanceTo(pos);
            if (!this.HasGottenUpdated && 
                (DateTime.Now - this.StartTime).TotalSeconds >= 1 &&
                this.DistanceTraveled > 0)
            {
                this.HasGottenUpdated = true;
            }

            // Update LastPosition
            this.LastPosition = pos;
            this.LastTime = DateTime.Now;
        }
    }

}
