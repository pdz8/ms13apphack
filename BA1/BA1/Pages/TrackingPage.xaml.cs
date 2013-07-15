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

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Update the current dispatcher
            Util.CurrentDispatcher = this.Dispatcher;

            string stop_id;
            if (NavigationContext.QueryString.TryGetValue("stop_id", out stop_id))
            {
                ViewModel = new TrackingVM(AppSettings.KnownStops.Value[stop_id]);
                this.DataContext = this.ViewModel;
                
                InitializeProgress();

                // Update recent stops
                RecentStopsQueue.Push(ViewModel.Context);
            }

            // Animate app bar
            this.AppBarEntrance.Begin();
            this.AppBarButtonsEntrance.Begin();
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

        private void TrackMap_Loaded(object sender, RoutedEventArgs e)
        {
            System.Threading.Thread.Sleep(500);
            if (LocationTracker.Location != null && this.ViewModel != null)
            {
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
                    this.ViewModel.BeginGeofence();
                    break;
                case TrackingVM.BtnStopTracking:
                    this.ViewModel.StopGeofence();
                    break;
            }
        }

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

    }
}