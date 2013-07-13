using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Live;

namespace BA1
{
    /// <summary>
    /// ViewModel for SkydrivePage.xaml
    /// </summary>
    public class SkydriveVM : INotifyPropertyChanged
    {
        #region Constructor and singleton

        private SkydriveVM()
        {
        }

        private static SkydriveVM _instance;

        /// <summary>
        /// Singleton
        /// </summary>
        public static SkydriveVM Instance
        {
            get { return SkydriveVM._instance ?? (SkydriveVM._instance = new SkydriveVM()); }
        }

        #endregion

        #region Data stats

        /// <summary>
        /// Total number of routes
        /// </summary>
        public int NumRoutes
        {
            get { return AppSettings.KnownRoutes.Value.Count; }
        }
        public string NumRoutesString
        {
            get { return this.NumRoutes.ToString(); }
        }

        /// <summary>
        /// Total number of stops
        /// </summary>
        public int NumStops
        {
            get { return AppSettings.KnownStops.Value.Count; }
        }
        public string NumStopsString
        {
            get { return this.NumStops.ToString(); }
        }

        /// <summary>
        /// Update numbers
        /// </summary>
        public void NotifyNums()
        {
            this.NotifyPropertyChanged("NumRoutesString");
            this.NotifyPropertyChanged("NumStopsString");
        }

        #endregion

        /// <summary>
        /// Connection to Skydrive
        /// </summary>
        public LiveConnectClient Client
        {
            get { return this._client; }
            set
            {
                if (value == this._client) return;
                this._client = value;
                this.NotifyPropertyChanged("Client");
                this.NotifyPropertyChanged("LiveButtonEnabled");
            }
        }

        /// <summary>
        /// Enable the backup and restore buttons
        /// </summary>
        public bool LiveButtonEnabled
        {
            get { return this.Client != null; }
        }

        private LiveConnectClient _client;

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
