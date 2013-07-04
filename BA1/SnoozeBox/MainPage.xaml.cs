using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using SnoozeBox.Resources;
using Windows.Devices.Geolocation;
using System.Device.Location;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Maps.Toolkit;
using Newtonsoft.Json;
using System.Threading.Tasks;
using SnoozeBox.ViewModels;
using System.Collections.ObjectModel;
using System.Text;
using System.Diagnostics;

namespace SnoozeBox
{
    public partial class MainPage : PhoneApplicationPage
    {
        System.Device.Location.GeoCoordinate pos2;

        public const string sample_REQUEST = "http://proxy.transitiq.apiphany.com/data/GeoAreas('38.8955|-77.071956|0.3')/Stops?key=";
        public const string sample_WALKSCORE = "http://transit.walkscore.com/transit/search/stops/?lat=47.6101359&lon=-122.3420567&wsapikey=955cda715c2b212a5d4f1682ca67f590";
        public const string bingmaps_KEY = "Ap316w9oByJzoSZIe_9uX1_Qzsh4_32pA1a1T9ILnhFOx0WPxj0GnPwWgeXxKi8g";
        public const string commuterapi_KEY = "0589367A-5EF5-4FD8-9C42-05FF65BF004F";
        public const string walkscore_KEY = "955cda715c2b212a5d4f1682ca67f590";
        public static string GetRequest(GeoCoordinate gc)
        {
            //if (gc == null) return sample_REQUEST + commuterapi_KEY;
            //return String.Format("http://proxy.transitiq.apiphany.com/data/GeoAreas('{0}|{1}|1')/Stops?key={2}",
            //    gc.Latitude.ToString(), gc.Longitude.ToString(), commuterapi_KEY);
            if (gc == null) return sample_WALKSCORE;
            var url = String.Format("http://transit.walkscore.com/transit/search/stops/?lat={0}&lon={1}&wsapikey=955cda715c2b212a5d4f1682ca67f590",
                gc.Latitude.ToString(), gc.Longitude.ToString());
            return url;
        }

        // Constructor
        public MainPage()
        {
            InitializeComponent();
            
            // Set the data context of the listbox control to the sample data
            //DataContext = App.ViewModel;
            //DataContext = PivotVM.Instance;
            this.DataContext = PivotVM.Instance;

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        // Load data for the ViewModel Items
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!App.ViewModel.IsDataLoaded)
            {
                App.ViewModel.LoadData();
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            switch (btn.Tag.ToString())
            {
                case "1":
                    Geolocator geo = new Geolocator();
                    aboveMap.Text = "Getting location";
                    Geoposition pos = await geo.GetGeopositionAsync();
                    aboveMap.Text = "Got location";
                    pos2 = new GeoCoordinate(pos.Coordinate.Latitude, pos.Coordinate.Longitude);
                    theMap.SetView(pos2, 16);
                    break;
                case "2":
                    if (pos2 == null) return;
                    var ml = new MapLayer();

                    Microsoft.Phone.Maps.Toolkit.Pushpin pp = new Pushpin();
                    pp.GeoCoordinate = pos2;

                    MapOverlay mo = new MapOverlay();
                    //mo.GeoCoordinate = pos2;
                    mo.Content = pp;
                    ml.Add(mo);
                    theMap.Layers.Add(ml);
                    break;
                case "3":
                    ObservableCollection<BusStopVM> obco;
                    List<BusStopVM> ls;
                    Uri apilink = new Uri(GetRequest(pos2));
                    Debugger.Log(0, "", GetRequest(pos2) + "\n"); 
                    Abovelist.Text = "loading";
                    var response = await DownloadString(apilink);

                    //var ls = DataLoader.GetStops(response);
                    //var obco = new ObservableCollection<BusStopVM>(DataLoader.GetStops(response));
                    ls = JsonConvert.DeserializeObject<List<BusStopVM>>(response);
                    obco = new ObservableCollection<BusStopVM>(ls);

                    Abovelist.Text = "response: " + response.Length + ", obco: " + obco.Count;
                    if (obco.Count > 0) Abovelist.Text += ", first: " + obco[0].Name;

                    //foreach (var bs in obco) PivotVM.Instance.BusStops.Add(bs);
                    PivotVM.Instance.BusStops = obco;
                    //App.ViewModel.BusStops = obco;
                    //stoplist.ItemsSource = obco;
                    //stoplist.ItemsSource = ls;
                    
                    
                    //mapitems = new MapItemsControl();
                    GetMPC().ItemsSource = obco;
                    Abovelist.Text = (GetMPC().ItemsSource as ObservableCollection<BusStopVM>).Count.ToString();
                    break;
            }
        }

        private MapItemsControl GetMPC()
        {
            ObservableCollection<DependencyObject> children = MapExtensions.GetChildren(theMap);
            var obj = children.FirstOrDefault(x => x.GetType() == typeof(MapItemsControl)) as MapItemsControl;
            return obj;
        }

        /// <summary>
        /// http://www.verious.com/tutorial/using-async-await-with-web-client-in-windows-phone-8-or-task-completion-source-saves-the-day/
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public Task<string> DownloadString(Uri uri)
        {
            var tcs = new TaskCompletionSource<string>();
            var wc = new WebClient();
            wc.DownloadStringCompleted += (s, e) =>
            {
                if (e.Error == null)
                {
                    tcs.SetResult(e.Result);
                }
                else
                {
                    tcs.SetException(e.Error);
                }
            };
            wc.DownloadStringAsync(uri);
            return tcs.Task;
        }

        private void theMap_ZoomLevelChanged(object sender, Microsoft.Phone.Maps.Controls.MapZoomLevelChangedEventArgs e)
        {
            //Map map = (Map)sender;
            //aboveMap.Text = "zoom = " + map.ZoomLevel.ToString();
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var sld = sender as Slider;
            if (theMap != null) theMap.ZoomLevel = sld.Value;
        }

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