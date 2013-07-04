using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace BA1
{
    [DataContract]
    public class BusRoute
    {
        [DataMember]
        public string Category { get; set; }

        [DataMember]
        public string Agency { get; set; }

        [DataMember]
        /// <summary>
        /// The route number
        /// </summary>
        public string Name { get; set; }

        [DataMember]
        public string Agency_Url { get; set; }

        [DataMember]
        public List<string> Stop_Ids { get; set; }

        [DataMember]
        public string Id { get; set; }

        /// <summary>
        /// Name to display on StopResultPage
        /// </summary>
        public string Title { get { return "route " + Name; } }
    }

    public class TransitNetworkSearch
    {
        public Dictionary<string, BusRoute> Routes { get; set; }
        public Dictionary<string, BusStop> Stops { get; set; }
    }
}
