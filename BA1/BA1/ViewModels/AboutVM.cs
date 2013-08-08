using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BA1
{
    /// <summary>
    /// ViewModel for AboutPage
    /// </summary>
    public class AboutVM : INotifyPropertyChanged
    {
        #region Constructor and singleton

        private AboutVM()
        {
        }

        private static AboutVM _instance;

        /// <summary>
        /// Singleton
        /// </summary>
        public static AboutVM Instance
        {
            get { return AboutVM._instance ?? (AboutVM._instance = new AboutVM()); }
        }

        #endregion

        /// <summary>
        /// Do we have permission to use location
        /// </summary>
        public bool UseLocation
        {
            get
            {
                return AppSettings.LocationConsent.Value;
            }
            set
            {
                if (value == AppSettings.LocationConsent.Value) return;
                AppSettings.LocationConsent.Value = value;
                AppSettings.LocationConsent.Save();
                this.NotifyPropertyChanged("UseLocation");
            }
        }

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged(String propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (null != handler)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion
    }
}
