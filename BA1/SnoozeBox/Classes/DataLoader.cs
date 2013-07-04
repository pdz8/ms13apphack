using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.IO.IsolatedStorage;
using System.IO;
using System.Collections.ObjectModel;
using SnoozeBox.ViewModels;

namespace SnoozeBox
{
    //public static class DataLoader
    //{

    //    /// <summary>
    //    /// Convert a string to a stream
    //    /// http://stackoverflow.com/questions/1879395/how-to-generate-a-stream-from-a-string
    //    /// </summary>
    //    /// <param name="str"></param>
    //    /// <returns></returns>
    //    public static Stream StreamFromString(this string str)
    //    {
    //        MemoryStream stream = new MemoryStream();
    //        StreamWriter writer = new StreamWriter(stream);
    //        writer.Write(str);
    //        writer.Flush();
    //        stream.Position = 0;
    //        return stream;
    //    }

    //    public static BusStopVM ReadStop(this XmlReader reader, string end)
    //    {
    //        var retval = new BusStopVM();
    //        try
    //        {
    //            while (reader.Read())
    //            {
    //                if (reader.IsStartElement())
    //                {
    //                    switch (reader.Name)
    //                    {
    //                        case STOPID_TAG:
    //                            reader.Read();
    //                            retval.StopID = reader.ReadContentAsString();
    //                            break;
    //                        case AGENCYID_TAG:
    //                            reader.Read();
    //                            retval.AgencyID = reader.ReadContentAsString();
    //                            break;
    //                        case NAME_TAG:
    //                            reader.Read();
    //                            retval.Name = reader.ReadContentAsString();
    //                            break;
    //                        case LAT_TAG:
    //                            reader.Read();
    //                            //retval.Lat = reader.ReadContentAsDouble();
    //                            break;
    //                        case LON_TAG:
    //                            reader.Read();
    //                            //retval.Lon = reader.ReadContentAsDouble();
    //                            break;
    //                        case DISTANCEFROMCENTER_TAG:
    //                            reader.Read();
    //                            retval.DistanceFromCenter = reader.ReadContentAsDouble();
    //                            break;
    //                    }
    //                }
    //                else if (reader.Name == BUSSTOP_TAG && reader.NodeType == XmlNodeType.EndElement)
    //                {
    //                    return retval;
    //                }
    //            }
    //        }
    //        catch { }
    //        return retval;
    //    }

    //    public const string BUSSTOP_TAG = "m:properties";
    //    public const string STOPID_TAG = "d:StopId";
    //    public const string AGENCYID_TAG = "d:AgencyId";
    //    public const string NAME_TAG = "d:Name";
    //    public const string LAT_TAG = "d:Lat";
    //    public const string LON_TAG = "d:Lon";
    //    public const string DISTANCEFROMCENTER_TAG = "d:DistanceFromCenter";

    //    /// <summary>
    //    /// http://social.msdn.microsoft.com/Forums/en-US/devdocs/thread/446de581-129c-4327-b3eb-0d6a3e505728
    //    /// </summary>
    //    /// <param name="str"></param>
    //    /// <returns></returns>
    //    public static List<BusStopVM> GetStops(string str)
    //    {
    //        var retval = new List<BusStopVM>();
    //        var reader = XmlReader.Create(str.StreamFromString());
    //        while (reader.Read())
    //        {
    //            if (reader.IsStartElement())
    //            {
    //                switch (reader.Name)
    //                {
    //                    case BUSSTOP_TAG:
    //                        var stop = reader.ReadStop(BUSSTOP_TAG);
    //                        retval.Add(stop);
    //                        break;
    //                }
    //            }
    //        }
    //        return retval;
    //    }
        
    //}

    //public class BusStop
    //{
    //    public string StopID { get; set; }
    //    public string AgencyID { get; set; }
    //    public string Name { get; set; }
    //    public double Lat { get; set; }
    //    public double Lon { get; set; }
    //    public double DistanceFromCenter { get; set; }
    //}
}
