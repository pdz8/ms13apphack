using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using System.Windows.Media;
using Microsoft.Phone.Maps.Controls;

namespace BA1
{
    public partial class TrackingPage : PhoneApplicationPage
    {
        #region Properties

        /// <summary>
        /// This page's viewmodel
        /// </summary>
        public TrackingVM ViewModel { get; private set; }

        /// <summary>
        /// First startup
        /// </summary>
        private bool startup = true;

        /// <summary>
        /// Do we need to rezoom the map?
        /// </summary>
        public bool ReZoomMap = true;

        #endregion

        #region Initialization

        public TrackingPage()
        {
            InitializeComponent();

            CurrentInstance = this;
        }

        /// <summary>
        /// Not a singleton
        /// </summary>
        public static TrackingPage CurrentInstance { get; private set; }

        /// <summary>
        /// Execute upon arriving from navigation
        /// </summary>
        /// <param name="e"></param>
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Update the current dispatcher
            Util.CurrentDispatcher = this.Dispatcher;

            // Setup Context and ViewModel
            if (this.startup)
            {
                string stop_id;
                if (this.NavigationContext.QueryString.TryGetValue("stop_id", out stop_id))
                {
                    // Initialize ViewModel
                    this.ViewModel = new TrackingVM(AppSettings.KnownStops.Value[stop_id]);

                    // Finish initialization after ViewModel is set
                    this.InitializeFromViewModel();

                    // Update phrase list for stops
                    await VoiceHelper.UpdateStopNamePhraseList();
                }
                else if (this.NavigationContext.QueryString.ContainsKey(VoiceHelper.VoiceCommandName))
                {
                    string commandName = this.NavigationContext.QueryString[VoiceHelper.VoiceCommandName];
                    string stopName;
                    if (this.NavigationContext.QueryString.TryGetValue(VoiceHelper.StopNamePhraseList, out stopName))
                    {
                        var possibleStops = RecentStopsQueue.RecentBusStops.Concat(BusStop.PinnedStops);
                        BusStop stopContext = possibleStops.FirstOrDefault(s => s.Name == stopName);
                        if (stopContext != null)
                        {
                            // Initialize ViewModel
                            this.ViewModel = new TrackingVM(stopContext);

                            // Finish initialization after ViewModel is set
                            this.InitializeFromViewModel();

                            // Automatically start tracking
                            if (commandName == VoiceHelper.AlarmSetCommand)
                            {
                                this.ViewModel.BeginGeofence();
                            }
                        }
                    }
                }
            }

            // Animate app bar
            this.AppBarEntrance.Begin();
            this.AppBarButtonsEntrance.Begin();
        }

        /// <summary>
        /// Finish initialization after setting the viewmodel
        /// </summary>
        private void InitializeFromViewModel()
        {
            this.DataContext = this.ViewModel;

            // Initialize progress bar
            InitializeProgress();

            // Get location if necessary
            if (LocationTracker.Location == null && LocationTracker.GetPermission())
            {
                ProgressIndicatorHelper.Instance.Push(LoadingEnum.Location);
                LocationTracker.RetrieveLocation();
            }

            // Update recent stops
            RecentStopsQueue.Push(ViewModel.Context);

            // Preven further initialization
            this.startup = false;
        }

        /// <summary>
        /// Initialize the ProgressIndicator
        /// </summary>
        private void InitializeProgress()
        {
            SystemTray.SetProgressIndicator(this, ProgressIndicatorHelper.Instance.PI);
            SystemTray.Opacity = 0;
            SystemTray.IsVisible = true;
            SystemTray.ForegroundColor = Colors.Black;
        }

        /// <summary>
        /// Called when map loads
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = Util.MapApplicationID;
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = Util.MapAuthenticationToken;
            System.Threading.Thread.Sleep(500);
            this.CenterAndZoom();
        }

        /// <summary>
        /// Center and zoom the map on location and the destination
        /// </summary>
        public void CenterAndZoom()
        {
            if (LocationTracker.Location != null && this.ViewModel != null)
            {
                this.ReZoomMap = false;
                this.TrackMap.SetView(LocationRectangle.CreateBoundingRectangle(
                    LocationTracker.Location, this.ViewModel.Context.Location));
            }
            else if (this.ViewModel != null)
            {
                this.TrackMap.SetView(LocationRectangle.CreateBoundingRectangle(
                    this.ViewModel.Context.Location));
            }
        }

        #endregion

        #region Animation

        private void TrackMap_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (this.MapSpacer.Height == 0)
            {
                this.ExpandSpacer.Begin();
            }
            //if (TrackMap.Height == 160) expandtrackmap.Begin();
            //this.ContentPanel.Visibility = Visibility.Collapsed;
        }

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (this.MapSpacer.Height == 480)
            {
                this.ContractSpacer.Begin();
            }
            //if (TrackMap.Height == 640) contracttrackmap.Begin();
            //this.ContentPanel.Visibility = Visibility.Visible;
        }

        #endregion

        /// <summary>
        /// Respond to clicks on the awesome appbar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            switch (btn.Content.ToString())
            {
                case TrackingVM.BtnPinToStart:
                    BusStop.PinToStart(this.ViewModel.Context);
                    break;
                case TrackingVM.BtnStartTracking:
                    if (LocationTracker.GetPermission())
                    {
                        this.ViewModel.BeginGeofence();
                    }
                    break;
                case TrackingVM.BtnStopTracking:
                    this.ViewModel.StopGeofence();
                    break;
            }
        }

        /// <summary>
        /// Exit and stop tracking
        /// </summary>
        /// <param name="e"></param>
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (LocationTracker.IsTracking)
            {
                //LocationTracker.StopTracking();
                this.ViewModel.StopGeofence();
            }

            AppSettings.AlarmThresholds.Save();

            base.OnBackKeyPress(e);
        }

        /// <summary>
        /// Toggle manual and automatic threshold updating
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ManualToggleBlock_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            this.ViewModel.ManualEnabled = !this.ViewModel.ManualEnabled;
        }

    }
}