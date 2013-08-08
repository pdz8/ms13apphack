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
using Microsoft.Phone.Maps.Toolkit;
using System.Collections.ObjectModel;
using Microsoft.Phone.Maps.Controls;
using System.Device.Location;
using System.Windows.Threading;
using System.Threading.Tasks;

namespace BA1
{
    public partial class StopResultPage : PhoneApplicationPage
    {
        #region Properties

        private bool startup = true;

        /// <summary>
        /// Page's ViewModel
        /// </summary>
        public StopResultVM ViewModel { get; private set; }

        /// <summary>
        /// Not a singleton
        /// </summary>
        public static StopResultPage CurrentInstance { get; private set; }

        #endregion

        #region Initialization

        public StopResultPage()
        {
            InitializeComponent();

            CurrentInstance = this;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Update current dispatcher
            Util.CurrentDispatcher = this.Dispatcher;

            if (this.startup)
            {
                // Initialize progress bar
                this.InitializeProgress();
                
                // Setup ViewModel
                string route_id;
                string route_name;
                if (this.NavigationContext.QueryString.TryGetValue("route_id", out route_id))
                {
                    this.ViewModel = new StopResultVM(AppSettings.KnownRoutes.Value[route_id]);
                    await InitializeFromViewModel();
                }
                else if (this.NavigationContext.QueryString.TryGetValue(VoiceHelper.RouteNumPhraseList, out route_name))
                {
                    ProgressIndicatorHelper.Instance.Push(LoadingEnum.Routes);
                    BusRoute br = await TransitInfo.SearchForRoute(route_name);
                    ProgressIndicatorHelper.Instance.Remove(LoadingEnum.Routes);
                    if (LocationTracker.GetPermission())
                    {
                        LocationTracker.RetrieveLocation();
                    }
                    if (br != null)
                    {
                        this.ViewModel = new StopResultVM(br);
                        await InitializeFromViewModel();
                    }
                    else
                    {
                        MessageBox.Show(string.Format("Could not find route {0}.", route_name),
                            "No matches", MessageBoxButton.OK);
                    }
                }
            }
        }

        /// <summary>
        /// Finish initializations after setting the viewmodel
        /// </summary>
        /// <returns></returns>
        private async Task InitializeFromViewModel()
        {
            this.DataContext = this.ViewModel;

            ProgressIndicatorHelper.Instance.Push(LoadingEnum.Stops);
            //PIHelper.Push("Getting stops...");
            //var ls = await TransitInfo.GetStopsOfRoute(this.ViewModel.Context);
            if (!ViewModel.Context.Stop_Ids.Any(id => !AppSettings.KnownStops.Value.ContainsKey(id)) ||
                TransitLoader.InternetAvailable())
            {
                await ViewModel.GetStopsOfRoute();
            }
            ProgressIndicatorHelper.Instance.Remove(LoadingEnum.Stops);

            // Center the map
            if (this.ViewModel.Stops.Count > 0)
            {
                this.MapItems.ItemsSource = this.ViewModel.Stops;

                this.startup = false;

                this.ResultsMap.SetView(LocationRectangle.CreateBoundingRectangle(this.ViewModel.Stops.Select(s => s.Location)));
            }
            else this.startup = false;
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

        #endregion

        #region Map things

        /// <summary>
        /// MapItemsControl containing all items on the map
        /// </summary>
        private MapItemsControl MapItems
        {
            get
            {
                ObservableCollection<DependencyObject> children = MapExtensions.GetChildren(ResultsMap);
                var obj = children.FirstOrDefault(x => x.GetType() == typeof(MapItemsControl)) as MapItemsControl;
                return obj;
            }
        }

        /// <summary>
        /// Set view once map is loaded
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResultsMap_Loaded(object sender, RoutedEventArgs e)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = Util.MapApplicationID;
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = Util.MapAuthenticationToken;
            System.Threading.Thread.Sleep(500);
            if (!this.startup && this.ViewModel != null && this.ViewModel.Stops.Count > 0)
            {
                this.ResultsMap.SetView(LocationRectangle.CreateBoundingRectangle(this.ViewModel.Stops.Select(s => s.Location)));
            }
        }

        #endregion

        #region Animation

        private void ResultsMap_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (this.SpacingPanel.Height == 0)
            {
                this.ExpandSpacer.Begin();
            }
            //this.ContentPanel.Visibility = Visibility.Collapsed;
        }

        private void Grid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            if (this.SpacingPanel.Height == 480)
            {
                this.ContractSpacer.Begin();
            }
            //this.ContentPanel.Visibility = Visibility.Visible;
        }

        #endregion

        #region Tap events

        /// <summary>
        /// Display detailed pushpin content or open the stop
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Pushpin_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            Pushpin pp = sender as Pushpin;
            BusStop stop = pp.DataContext as BusStop;
            if (!stop.PinSelected)
            {
                foreach (var bs in this.ViewModel.Stops.Where(a => a.PinSelected)) 
                    bs.PinSelected = false;
                stop.PinSelected = true;
            }
            else
            {
                NavigationService.Navigate(new Uri(
                    string.Format("/pages/TrackingPage.xaml?stop_id={0}", stop.Id), UriKind.Relative));
            }
        }

        /// <summary>
        /// Tap on list item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StackPanel_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            StackPanel sp = sender as StackPanel;
            BusStop stop = sp.DataContext as BusStop;
            NavigationService.Navigate(new Uri(
                    string.Format("/pages/TrackingPage.xaml?stop_id={0}", stop.Id), UriKind.Relative));

        }

        /// <summary>
        /// Pin stop to start
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            MenuItem mi = sender as MenuItem;
            BusStop stop = mi.DataContext as BusStop;
            BusStop.PinToStart(stop);
        }

        #endregion

    }
}