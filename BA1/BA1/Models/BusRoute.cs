using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using System.IO;

namespace BA1
{
    [JsonObject(MemberSerialization.OptIn)]
    [DataContract]
    public class BusRoute
    {
        [DataMember]
        [JsonProperty]
        public string Category { get; set; }

        [DataMember]
        [JsonProperty]
        public string Agency { get; set; }

        [DataMember]
        [JsonProperty]
        /// <summary>
        /// The route number
        /// </summary>
        public string Name { get; set; }

        [DataMember]
        [JsonProperty]
        public string Agency_Url { get; set; }

        [DataMember]
        [JsonProperty]
        public List<string> Stop_Ids { get; set; }

        [DataMember]
        [JsonProperty]
        public string Id { get; set; }

        /// <summary>
        /// Name to display on StopResultPage
        /// </summary>
        public string Title { get { return "route " + Name; } }
    }

    /// <summary>
    /// Represents the stops and routes of a transit network
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class TransitNetworkSearch
    {
        [DataMember]
        [JsonProperty]
        public Dictionary<string, BusRoute> Routes { get; set; }

        [DataMember]
        [JsonProperty]
        public Dictionary<string, BusStop> Stops { get; set; }

        [DataMember]
        [JsonProperty]
        public Dictionary<string, double> Thresholds { get; set; }

        /// <summary>
        /// Name of agency associated with TNS if applicable
        /// </summary>
        public string Agency { get; private set; }

        #region Constructors

        /// <summary>
        /// Default constructor to allow Json deserializing
        /// </summary>
        public TransitNetworkSearch()
        {
            //this.Routes = new Dictionary<string, BusRoute>();
            //this.Stops = new Dictionary<string, BusStop>();
        }

        public TransitNetworkSearch(
            Dictionary<string, BusRoute> routes,
            Dictionary<string, BusStop> stops)
        {
            this.Routes = routes;
            this.Stops = stops;
        }

        public TransitNetworkSearch(
            Dictionary<string, BusRoute> routes,
            Dictionary<string, BusStop> stops,
            Dictionary<string, double> thresholds)
            : this(routes, stops)
        {
            this.Thresholds = thresholds;
        }

        #endregion

        /// <summary>
        /// Return a list of Transit Networks grouped by Agency name
        /// </summary>
        /// <param name="rootTNS">TNS supplying all routes and stops</param>
        /// <returns></returns>
        public static Dictionary<string, TransitNetworkSearch> GroupByAgency(TransitNetworkSearch rootTNS)
        {
            Dictionary<string, TransitNetworkSearch> retval = new Dictionary<string, TransitNetworkSearch>();
            if (rootTNS == null || 
                rootTNS.Routes == null || 
                rootTNS.Stops == null || 
                rootTNS.Thresholds == null) return retval;

            var routeGroups = rootTNS.Routes.Values.GroupBy(rt => rt.Agency);
            foreach (var rg in routeGroups)
            {
                TransitNetworkSearch tns = new TransitNetworkSearch()
                {
                    Routes = new Dictionary<string, BusRoute>(),
                    Stops = new Dictionary<string, BusStop>(),
                    Thresholds = new Dictionary<string, double>()
                };
                foreach (BusRoute route in rg)
                {
                    tns.Routes.Add(route.Id, route);
                    foreach (string stop_id in route.Stop_Ids)
                    {
                        if (rootTNS.Stops.ContainsKey(stop_id) && !tns.Stops.ContainsKey(stop_id))
                        {
                            tns.Stops.Add(stop_id, rootTNS.Stops[stop_id]);
                            if (rootTNS.Thresholds.ContainsKey(stop_id) && !tns.Thresholds.ContainsKey(stop_id))
                            {
                                tns.Thresholds.Add(stop_id, rootTNS.Thresholds[stop_id]);
                            }
                        }
                    }
                }
                retval.Add(rg.Key, tns);
            }

            return retval;
        }

        /// <summary>
        /// Save TNS to iso storage
        /// </summary>
        /// <param name="tns"></param>
        public static void SaveTNS(TransitNetworkSearch tns)
        {
            if (tns == null) return;

            if (tns.Routes != null)
            {
                foreach (string id in tns.Routes.Keys)
                {
                    AppSettings.KnownRoutes.Value[id] = tns.Routes[id];
                }
                AppSettings.KnownRoutes.Save();
            }
            if (tns.Stops != null)
            {
                foreach (string id in tns.Stops.Keys)
                {
                    AppSettings.KnownStops.Value[id] = tns.Stops[id];
                }
                AppSettings.KnownStops.Save();
            }
            if (tns.Thresholds != null)
            {
                foreach (string id in tns.Thresholds.Keys)
                {
                    AppSettings.AlarmThresholds.Value[id] = tns.Thresholds[id];
                }
                AppSettings.AlarmThresholds.Save();
            }
        }

    }
}
