using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Windows;
using System.Collections.ObjectModel;

namespace BA1
{
    public static class TransitInfo
    {
        public static GeoCoordinate MyLocation
        {
            get
            {
                return LocationTracker.Location;
            }
        }
        
        /// <summary>
        /// Maps route names to IDs
        /// </summary>
        public static Dictionary<string, List<string>> RouteIDs { get; private set; }

        /// <summary>
        /// RePopulate RouteIDs - This makes no effort to be efficient
        /// </summary>
        public static void RefreshRouteIDs()
        {
            (RouteIDs ?? (RouteIDs = new Dictionary<string,List<string>>())).Clear();
            foreach (BusRoute br in AppSettings.KnownRoutes.Value.Values)
            {
                if (!RouteIDs.ContainsKey(br.Name))
                {
                    RouteIDs.Add(br.Name, new List<string>());
                }
                RouteIDs[br.Name].Add(br.Id);
            }
        }

        /// <summary>
        /// RePopulate RouteIDs and also update speech inputs
        /// </summary>
        public static async Task RefreshRouteIDsAsync()
        {
            RefreshRouteIDs();
            await VoiceHelper.UpdateRoutePhraseList();
        }

        /// <summary>
        /// Search for route within known routes.
        /// If not found, load up nearby transit info and search again.
        /// TODO: If there is a known far away stop, this will fail.
        /// </summary>
        /// <param name="routeName"></param>
        /// <returns></returns>
        public static async Task<BusRoute> SearchForRoute(string routeName)
        {
            BusRoute result = null;
            result = await GetNearestMatchingRoute(routeName);
            if (result != null) 
                return result;

            if (MyLocation != null)
            {
                await TransitLoader.GetTransitNetwork(MyLocation);
            }

            //RefreshRouteIDs();
            await RefreshRouteIDsAsync();

            result = await GetNearestMatchingRoute(routeName);
            return result;
        }

        /// <summary>
        /// Finds the nearest matching route
        /// </summary>
        /// <param name="routeName">Name of route</param>
        /// <returns></returns>
        private static async Task<BusRoute> GetNearestMatchingRoute(string routeName)
        {
            BusRoute retRoute = null;
            if (RouteIDs.ContainsKey(routeName))
            {
                if (RouteIDs[routeName].Count == 0) return null;
                if (RouteIDs[routeName].Count == 1)
                {
                    if (!AppSettings.KnownRoutes.Value.ContainsKey(RouteIDs[routeName][0])) return null;
                    retRoute = await TransitLoader.GetRoute(RouteIDs[routeName][0]);
                    return retRoute;
                }

                double minDist = double.PositiveInfinity;
                foreach (string id in RouteIDs[routeName])
                {
                    if (!AppSettings.KnownRoutes.Value.ContainsKey(id)) return retRoute;
                    var br = AppSettings.KnownRoutes.Value[id];
                    if (br.Stop_Ids == null || br.Stop_Ids.Count == 0) return retRoute;

                    BusStop firststop = await TransitLoader.GetStop(br.Stop_Ids[0]);
                    var dist = MyLocation.GetDistanceTo(firststop.Location);
                    if (minDist > dist)
                    {
                        retRoute = br;
                        minDist = dist;
                    }
                }
                return retRoute;
            }
            return null;
        }

        static TransitInfo()
        {
            RefreshRouteIDs();
        }
    }

    /// <summary>
    /// Helper class that handles all downloads
    /// </summary>
    public static class TransitLoader
    {
        #region Request formation

        private const string walkscore_KEY = "955cda715c2b212a5d4f1682ca67f590";

        private static string rNetworkSearch(double lat, double lon)
        {
            return string.Format("http://transit.walkscore.com/transit/search/network/?lat={0}&lon={1}&wsapikey={2}",
                lat.ToString(), lon.ToString(), walkscore_KEY);
        }
        private static string rNetworkSearch(GeoCoordinate pos)
        {
            return rNetworkSearch(pos.Latitude, pos.Longitude);
        }

        private static string rStop(string id)
        {
            return string.Format("http://transit.walkscore.com/transit/stop/{0}/?wsapikey={1}", 
                id, walkscore_KEY);
        }

        private static string rRoute(string id)
        {
            return string.Format("http://transit.walkscore.com/transit/route/{0}/?wsapikey={1}",
                id, walkscore_KEY);
        }

        /// <summary>
        /// Get Url for the string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        private static Uri ToUri(this string str)
        {
            return new Uri(str);
        }

        #endregion

        #region Downloaders

        /// <summary>
        /// Download stop if necessary
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static async Task<BusStop> GetStop(string id)
        {
            if (AppSettings.KnownStops.Value.ContainsKey(id))
            {
                return AppSettings.KnownStops.Value[id];
            }
            else
            {
                var response = await DownloadString(rStop(id).ToUri());
                BusStop retval = JsonConvert.DeserializeObject<BusStop>(response);
                AppSettings.KnownStops.Value[id] = retval;
                AppSettings.KnownStops.Save();
                return retval;
            }
        }

        /// <summary>
        /// Download route if necessary
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static async Task<BusRoute> GetRoute(string id)
        {
            if (AppSettings.KnownRoutes.Value.ContainsKey(id))
            {
                return AppSettings.KnownRoutes.Value[id];
            }
            else
            {
                var response = await DownloadString(rStop(id).ToUri());
                BusRoute retval = JsonConvert.DeserializeObject<BusRoute>(response);
                AppSettings.KnownRoutes.Value[id] = retval;
                AppSettings.KnownRoutes.Save();
                return retval;
            }
        }

        /// <summary>
        /// Download transit network
        /// </summary>
        /// <param name="lat"></param>
        /// <param name="lon"></param>
        /// <returns></returns>
        public static async Task<TransitNetworkSearch> GetTransitNetwork(double lat, double lon)
        {
            var response = await DownloadString(rNetworkSearch(lat, lon).ToUri());
            TransitNetworkSearch tns = JsonConvert.DeserializeObject<TransitNetworkSearch>(response);
            TransitNetworkSearch.SaveTNS(tns);
            return tns;
        }
        public static async Task<TransitNetworkSearch> GetTransitNetwork(GeoCoordinate geo)
        {
            return await GetTransitNetwork(geo.Latitude, geo.Longitude);
        }

        /// <summary>
        /// http://www.verious.com/tutorial/using-async-await-with-web-client-in-windows-phone-8-or-task-completion-source-saves-the-day/
        /// </summary>
        /// <param name="u"></param>
        /// <returns></returns>
        public static Task<string> DownloadString(Uri uri)
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

        #endregion

    }
}
