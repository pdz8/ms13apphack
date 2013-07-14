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
using Microsoft.Live;
using Newtonsoft.Json;

namespace BA1.Pages
{
    /// <summary>
    /// Handles all syncing with Skydrive
    /// </summary>
    public partial class SkydrivePage : PhoneApplicationPage
    {
        #region Constants and variables

        private bool startup = true;

        public SkydriveVM ViewModel { get; private set; }

        #endregion

        #region Initialization

        public SkydrivePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // Update current dispatcher
            Util.CurrentDispatcher = this.Dispatcher;

            // Initial startup actions
            if (this.startup)
            {
                this.ViewModel = SkydriveVM.Instance;
                this.DataContext = this.ViewModel;

                // Initialize progress bar
                this.InitializeProgress();

                this.startup = false;
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

        #endregion

        #region Skydrive handling

        /// <summary>
        /// Fired upon successful login (hopefully)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SignInButton_SessionChanged(object sender, Microsoft.Live.Controls.LiveConnectSessionChangedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                CloudStorage.Client = new LiveConnectClient(e.Session);
            }
            else
            {
                CloudStorage.Client = null;
            }
        }

        /// <summary>
        /// Upload all route and stop data to Skydrive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BackupBtn_Click(object sender, RoutedEventArgs e)
        {
            ProgressIndicatorHelper.Instance.Push(LoadingEnum.Uploading);
            try
            {
                await CloudStorage.BackupToSkydrive();

                // Display success
                ProgressIndicatorHelper.Instance.Remove(LoadingEnum.Uploading);
                MessageBox.Show("All known stops and routes were uploaded to Skydrive", "Success", MessageBoxButton.OK);
            }
            catch (Exception exc)
            {
                ProgressIndicatorHelper.Instance.Remove(LoadingEnum.Uploading);
                MessageBox.Show(exc.Message, "An error occured", MessageBoxButton.OK);
            }
            finally
            {
            }
        }

        /// <summary>
        /// Restore files from skydrive
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void RestoreBtn_Click(object sender, RoutedEventArgs e)
        {
            ProgressIndicatorHelper.Instance.Push(LoadingEnum.Downloading);
            try
            {
                await CloudStorage.RestoreFromSkydrive();
                ProgressIndicatorHelper.Instance.Remove(LoadingEnum.Downloading);
                this.ViewModel.NotifyNums();
                RecentStopsQueue.Refresh();
                await TransitInfo.RefreshRouteIDsAsync();
                PanoVM.Instance.UpdateSuggestions();
                MessageBox.Show("Downloaded all stops and routes from Skydrive to local storage", "Success", MessageBoxButton.OK);
            }
            catch (Exception exc)
            {
                ProgressIndicatorHelper.Instance.Remove(LoadingEnum.Downloading);
                MessageBox.Show(exc.Message, "An error occured", MessageBoxButton.OK);
            }
            finally
            {
            }
        }

        #endregion

        /// <summary>
        /// Clear all routes, stops, and recents
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ResetBtn_Click(object sender, RoutedEventArgs e)
        {
            // Assure the user didn't misclick
            MessageBoxResult mbr = MessageBox.Show("This will remove all of this app's local transit data", "Are you sure?",
                MessageBoxButton.OKCancel);
            if (mbr != MessageBoxResult.OK) return;

            // Clear everything
            AppSettings.KnownStops.Value = new Dictionary<string, BusStop>();
            AppSettings.KnownRoutes.Value = new Dictionary<string, BusRoute>();
            AppSettings.RecentIds.Value = new LinkedList<string>();

            // Save changes
            AppSettings.KnownRoutes.Save();
            AppSettings.KnownStops.Save();
            AppSettings.RecentIds.Save();

            // Change numbers
            this.ViewModel.NotifyNums();
            PanoVM.Instance.UpdateSuggestions();
            RecentStopsQueue.Refresh();
        }
    }
}