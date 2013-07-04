using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Windows;

namespace BA1
{
    /// <summary>
    /// ViewModel for PanoPage.xaml
    /// </summary>
    public class PanoVM : INotifyPropertyChanged
    {
        public PanoVM()
        {
            this.RecentStops = new ObservableCollection<BusStop>();
            this.Suggestions = new ObservableCollection<string>();
            this.UpdateSuggestions();
        }

        /// <summary>
        /// Singleton
        /// </summary>
        public static PanoVM Instance
        {
            get
            {
                return _Instance ?? (_Instance = new PanoVM());
            }
        }

        /// <summary>
        /// Route suggestions for AutoCompleteBox
        /// </summary>
        public ObservableCollection<string> Suggestions { get; set; }
        public void UpdateSuggestions()
        {
            Suggestions.Clear();
            TransitInfo.RefreshRouteIDs();
            foreach (string k in TransitInfo.RouteIDs.Keys) Suggestions.Add(k);
        }

        #region Recent stops

        /// <summary>
        /// Recent stops observable list
        /// </summary>
        public ObservableCollection<BusStop> RecentStops { get; set; }

        /// <summary>
        /// Show or don't show the no recent stops message
        /// </summary>
        public Visibility RecentMsgVisibility
        {
            get { return this.RecentStops.Count > 0 ? Visibility.Collapsed : Visibility.Visible; }
        }

        #endregion

        private static PanoVM _Instance = null;

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

    /// <summary>
    /// Keeps track of recent stops
    /// </summary>
    public static class RecentStopsQueue
    {
        /// <summary>
        /// Maximum number of stops to keep in data structure
        /// </summary>
        public const int MAX_NUM_STOPS = 10;

        /// <summary>
        /// Populate RecentStops from AppSettings
        /// </summary>
        public static void Refresh()
        {
            PanoVM.Instance.RecentStops.Clear();
            foreach (string id in AppSettings.RecentIds.Value)
            {
                if (AppSettings.KnownStops.Value.ContainsKey(id))
                {
                    PanoVM.Instance.RecentStops.Add(AppSettings.KnownStops.Value[id]);
                    if (PanoVM.Instance.RecentStops.Count == 1)
                    {
                        PanoVM.Instance.NotifyPropertyChanged("RecentMsgVisibility");
                    }
                }
                //else AppSettings.RecentIds.Value.Remove(id);
            }
        }

        /// <summary>
        /// Add stop to recent stops
        /// </summary>
        /// <param name="stop"></param>
        public static void Push(BusStop stop)
        {
            if (stop == null) return;
            string id = stop.Id;
            if (AppSettings.RecentIds.Value.Contains(id))
            {
                AppSettings.RecentIds.Value.Remove(id);
            }
            if (AppSettings.RecentIds.Value.Count == MAX_NUM_STOPS)
            {
                AppSettings.RecentIds.Value.RemoveLast();
            }
            AppSettings.RecentIds.Value.AddFirst(id);
            AppSettings.RecentIds.Save();
            Refresh();
        }
    }
}
