using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using BA1.Resources;
using System.Windows.Media;
using System.Threading.Tasks;
using Windows.Phone.Speech.VoiceCommands;

namespace BA1
{
    public partial class PanoPage : PhoneApplicationPage
    {
        #region Constants


        #endregion

        #region Variables

        // FSM
        bool startup = true;

        // VM
        PanoVM ViewModel;

        /// <summary>
        /// Not a singleton
        /// </summary>
        public static PanoPage CurrentInstance { get; private set; }

        #endregion

        #region Initialization

        // Constructor
        public PanoPage()
        {
            InitializeComponent();

            this.ViewModel = PanoVM.Instance;
            this.DataContext = this.ViewModel;
            RecentStopsQueue.Refresh();

            CurrentInstance = this;

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        private async void PhoneApplicationPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Update current dispatcher
            Util.CurrentDispatcher = this.Dispatcher;

            if (startup)
            {
                startup = false;

                // Setup system tray
                SystemTray.SetProgressIndicator(this, ProgressIndicatorHelper.Instance.PI);
                SystemTray.Opacity = 0;
                SystemTray.IsVisible = true;
                SystemTray.ForegroundColor = Colors.Black;

                // Get Permissions
                GetPermissions();
                
                // Find location
                ProgressIndicatorHelper.Instance.Push(LoadingEnum.Location);
                LocationTracker.RetrieveLocation();

                // Setup speech recognition
                await VoiceHelper.InitializeSpeech();
            }
            
        }


        /// <summary>
        /// Assure that app has required user consent
        /// </summary>
        public void GetPermissions()
        {
            if (!AppSettings.LocationConsent.Value)
            {
                MessageBoxResult mbr = MessageBox.Show("This app requires your phone's location in order to operate. Is that ok?",
                    "Location", MessageBoxButton.OKCancel);
                if (mbr == MessageBoxResult.OK)
                {
                    AppSettings.LocationConsent.Value = true;
                    AppSettings.LocationConsent.Save();
                }
                else Application.Current.Terminate();
            }
        }

        #endregion

        #region Searching

        private const int spacerMaxHeight = 92;

        /// <summary>
        /// Is the search box focused
        /// </summary>
        private bool IsSearchFocused = false;

        private async void FindStopButton_Click(object sender, RoutedEventArgs e)
        {
            await SearchRoutes();
        }

        /// <summary>
        /// Search the text in the route box
        /// </summary>
        /// <returns></returns>
        private async Task SearchRoutes()
        {
            this.Focus();

            // Blank route
            if (this.RouteSearchBox.Text == EmptyTextBox || 
                string.IsNullOrWhiteSpace(this.RouteSearchBox.Text))
            {
                MessageBox.Show("Please enter the number of a bus route in your area.",
                    "No input", MessageBoxButton.OK);
                return;
            }

            ProgressIndicatorHelper.Instance.Push(LoadingEnum.Routes);
            var br = await TransitInfo.SearchForRoute(this.RouteSearchBox.Text);
            ProgressIndicatorHelper.Instance.Remove(LoadingEnum.Routes);
            if (br == null)
            {
                MessageBox.Show(string.Format("Could not find route {0}.", this.RouteSearchBox.Text),
                    "No matches", MessageBoxButton.OK);
                return;
            }

            if (this.SpacingPanel.Height == spacerMaxHeight)
            {
                this.SetSearchBarVisibility(Visibility.Collapsed);
            }
            NavigationService.Navigate(new Uri(
                string.Format("/Pages/StopResultPage.xaml?route_id={0}", br.Id),
                UriKind.Relative));
            //NavigationService.Navigate(new Uri("/Pages/StopResultPage.xaml", UriKind.Relative));
        }

        /// <summary>
        /// Hide the search bar if necessary
        /// </summary>
        /// <param name="e"></param>
        protected override void OnBackKeyPress(System.ComponentModel.CancelEventArgs e)
        {
            if (this.SpacingPanel.Height == spacerMaxHeight && !this.IsSearchFocused)
            {
                this.SetSearchBarVisibility(Visibility.Collapsed);
                e.Cancel = true;
            }
            base.OnBackKeyPress(e);
        }

        private const string EmptyTextBox = "search routes"; //"search via route #";
        private void RouteSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            this.IsSearchFocused = true;

            // Remove hint string
            if (this.RouteSearchBox.Text == EmptyTextBox)
            {
                this.RouteSearchBox.Text = "";
            }

            // Highlight all text
            // TODO
        }
        private void RouteSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            this.IsSearchFocused = false;
            if (string.IsNullOrWhiteSpace(this.RouteSearchBox.Text))
            {
                this.RouteSearchBox.Text = EmptyTextBox;
            }
        }

        private async void SearchButtonGrid_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            await this.SearchRoutes();
        }

        /// <summary>
        /// Expand and contract the search bar
        /// </summary>
        /// <param name="v"></param>
        private void SetSearchBarVisibility(Visibility v)
        {
            if (v == Visibility.Visible && this.SpacingPanel.Height == 0)
            {
                this.ExpandSpacer.Begin();
                var enterbutton = new ApplicationBarIconButton(new Uri("/Images/next.png", UriKind.Relative))
                {
                    Text = "enter"
                };
                enterbutton.Click += ApplicationBarIconButton_Click;
                ApplicationBar.Buttons.Clear();
                ApplicationBar.Buttons.Add(enterbutton);

            }
            else if (v == Visibility.Collapsed && this.SpacingPanel.Height == spacerMaxHeight)
            {
                this.Focus();
                this.ContractSpacer.Begin();
                var searchbutton = new ApplicationBarIconButton(new Uri("/Images/feature.search.png", UriKind.Relative))
                {
                    Text = "routes"
                };
                searchbutton.Click += ApplicationBarIconButton_Click;
                ApplicationBar.Buttons.Clear();
                ApplicationBar.Buttons.Add(searchbutton);
            }
        }

        #endregion

        #region Appbar

        /// <summary>
        /// AppBar Buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void ApplicationBarIconButton_Click(object sender, EventArgs e)
        {
            ApplicationBarIconButton abib = sender as ApplicationBarIconButton;
            switch (abib.Text)
            {
                case "routes":
                case "search":
                    if (this.SpacingPanel.Height == 0)
                    {
                        this.SetSearchBarVisibility(Visibility.Visible);
                        this.RouteSearchBox.Focus();
                    }
                    else if (this.SpacingPanel.Height == spacerMaxHeight)
                    {
                        // Search
                        //await this.SearchRoutes();

                        // Contract
                        this.SetSearchBarVisibility(Visibility.Collapsed);
                    }
                    break;
                case "enter":
                    await this.SearchRoutes();
                    break;
            }
        }

        /// <summary>
        /// AppBar Menu Items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ApplicationBarMenuItem_Click(object sender, EventArgs e)
        {
            ApplicationBarMenuItem abmi = sender as ApplicationBarMenuItem;
            switch (abmi.Text)
            {
                case "about":
                    NavigationService.Navigate(new Uri("/Pages/AboutPage.xaml", UriKind.Relative));
                    break;
                case "sync with skydrive":
                    NavigationService.Navigate(new Uri("/Pages/SkydrivePage.xaml", UriKind.Relative));
                    break;
                default:
                    break;
            }
        }

        #endregion

        #region Stop list

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

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
    }
}