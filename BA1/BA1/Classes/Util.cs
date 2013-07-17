using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Windows.Phone.Speech.VoiceCommands;

namespace BA1
{
    /// <summary>
    /// Miscellaneous utilities
    /// </summary>
    public static class Util
    {
        public const string MapApplicationID = "09e85e7f-9128-4dca-8979-dd893b9ac3e7";
        public const string MapAuthenticationToken = "G7attB_FwgHPovVn7d-Usw";

        #region Lowercase uppercase

        private static readonly string[] UpperWords = new string[] { "NE", "SW", "SE", "NW" };
        private static readonly string[] LowerWords = new string[] { "of", "at", "in", "the", "and", "or" };

        /// <summary>
        /// Reduce the stop address form being all caps
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ConvertAddress(this string str)
        {
            str.Trim();
            var words = str.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                word.Trim();
                if (UpperWords.Contains(word.ToUpper()))
                {
                    words[i] = word.ToUpper();
                    continue;
                }
                if (LowerWords.Contains(word.ToLower()) && i > 0)
                {
                    words[i] = word.ToLower();
                    continue;
                }
                words[i] = word.ToCapitalizeFirst();
            }
            return string.Join(" ", words);
        }

        /// <summary>
        /// Return a new string with the first letter capitalized
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string ToCapitalizeFirst(this string str)
        {
            return str[0].ToString().ToUpper() + str.Substring(1).ToLower();
        }

        #endregion

        #region Distance strings

        public const double METERS_PER_MILE = 1609.344;
        public const double FEET_PER_MILE = 5280;
        public const double METERS_PER_KM = 1000;

        /// <summary>
        /// Convert meters to miles
        /// </summary>
        /// <param name="meters"></param>
        /// <returns></returns>
        public static double MetersToMiles(this double meters)
        {
            return meters / METERS_PER_MILE;
        }

        /// <summary>
        /// Get string in miles
        /// </summary>
        /// <param name="meters"></param>
        /// <returns></returns>
        public static string ToMilesString(this double miles)
        {
            string dist_str;
            if (miles >= 0)
            {
                miles = Math.Round(miles, 1);
                dist_str = miles.ToString();
            }
            else
            {
                dist_str = "--";
            }
            return dist_str + " mile" + (dist_str != "1" ? "s" : "");
        }

        #endregion

        #region Time strings

        public static double MinutesToHours(this double minutes)
        {
            return minutes / 60;
        }

        /// <summary>
        /// Return string representing timespan
        /// </summary>
        /// <param name="minutes">Time left in minute</param>
        /// <returns></returns>
        public static string ToTimeString(this double minutes)
        {
            if (minutes < 0)
            {
                return "-- min";
            }
            else if (minutes >= 60)
            {
                double hours = minutes.MinutesToHours();
                hours = Math.Round(hours, 1);
                return hours.ToString() + " hour" + (hours == 1 ? "" : "s");
            }
            else if (minutes < 1)
            {
                return "< 1 min";
            }
            else
            {
                minutes = Math.Round(minutes);
                return minutes.ToString() + " min";
            }
        }

        public static string ToMPHString(this double mph)
        {
            if (mph < 0) return "-- mph";
            else
            {
                mph = Math.Round(mph, 1);
                return mph.ToString() + " mph";
            }
        }

        #endregion 

        #region Dispatch

        public static Dispatcher CurrentDispatcher { private get; set; }

        /// <summary>
        /// Use the current Dispatcher
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        public static bool BeginInvoke(Action a)
        {
            if (CurrentDispatcher == null)
            {
                return false;
            }
            else
            {
                CurrentDispatcher.BeginInvoke(a);
                return true;
            }
        }

        #endregion

        #region Stream

        /// <summary>
        /// http://stackoverflow.com/questions/1879395/how-to-generate-a-stream-from-a-string
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static Stream ToStream(this string s)
        {
            MemoryStream stream = new MemoryStream();
            StreamWriter writer = new StreamWriter(stream);
            writer.Write(s);
            writer.Flush();
            stream.Position = 0;
            return stream;
        }

        #endregion

    }

    public static class VoiceHelper
    {
        /// <summary>
        /// Load VDH file to initialize voice capabilities
        /// </summary>
        public static async Task InitializeSpeech()
        {
            try
            {
                await VoiceCommandService.InstallCommandSetsFromFileAsync(
                    new Uri("ms-appx:///Resources/BusStopVHD.xml"));
            }
            catch
            {
                Util.BeginInvoke(() =>
                {
                    MessageBox.Show("Unable to initialize voice capabilities",
                        "Error", MessageBoxButton.OK);
                });
            }
        }

        public const string CommandSetName = "BusAlarmEnu";
        public const string RouteNumPhraseList = "route_num";
        public const string SearchCommandName = "StopSearchByRoute";

        /// <summary>
        /// Update route_num
        /// </summary>
        public static async Task UpdateRoutePhraseList()
        {
            try
            {
                VoiceCommandSet vcs;
                if (VoiceCommandService.InstalledCommandSets.TryGetValue(CommandSetName, out vcs))
                {
                    await vcs.UpdatePhraseListAsync(RouteNumPhraseList, TransitInfo.RouteIDs.Keys);
                }
            }
            catch
            {
                Util.BeginInvoke(() =>
                {
                    MessageBox.Show("Unable to update voice phraseology",
                        "Error", MessageBoxButton.OK);
                });
            }
        }
    }

    /// <summary>
    /// Maintains the progress indicator for a page
    /// </summary>
    public class ProgressIndicatorHelper
    {
        #region Properties

        public ProgressIndicator PI { get; private set; }
        private LinkedList<string> Messages;
        private static ProgressIndicatorHelper _Instance;
        
        /// <summary>
        /// Singleton
        /// </summary>
        public static ProgressIndicatorHelper Instance
        {
            get { return _Instance ?? (_Instance = new ProgressIndicatorHelper()); }
        }

        #endregion

        public ProgressIndicatorHelper()
        {
            PI = new ProgressIndicator()
            {
                IsVisible = false,
                IsIndeterminate = false
            };
            Messages = new LinkedList<string>();
        }

        #region Stack actions

        /// <summary>
        /// Add new loading state to indicator
        /// </summary>
        /// <param name="le"></param>
        public void Push(LoadingEnum le)
        {
            string msg = GetLoadingMessage(le);
            Messages.AddFirst(msg);
            PI.Text = msg;
            PI.IsIndeterminate = true;
            PI.IsVisible = true;
        }

        /// <summary>
        /// Pop current message out of indicator
        /// </summary>
        public void Pop()
        {
            if (Messages.Count > 1)
            {
                Messages.RemoveFirst();
                PI.Text = Messages.First.Value;
            }
            else this.Clear();
        }

        /// <summary>
        /// Remove the loading status from the 'stack'
        /// </summary>
        /// <param name="le"></param>
        public void Remove(LoadingEnum le)
        {
            if (Messages.Count == 0) return;
            string msg = GetLoadingMessage(le);
            Messages.Remove(msg);
            if (Messages.Count > 0)
            {
                PI.Text = Messages.First.Value;
            }
            else
            {
                this.Clear();
            }
        }

        /// <summary>
        /// Stop the indicator
        /// </summary>
        public void Clear()
        {
            PI.IsIndeterminate = false;
            PI.IsVisible = false;
            Messages.Clear();
        }

        #endregion

        /// <summary>
        /// Get loading message for loading state
        /// </summary>
        /// <param name="le"></param>
        /// <returns></returns>
        public static string GetLoadingMessage(LoadingEnum le)
        {
            switch (le)
            {
                case LoadingEnum.Location: return "Getting location...";
                case LoadingEnum.Stops: return "Getting stops...";
                case LoadingEnum.Loading: return "Loading...";
                case LoadingEnum.Routes: return "Finding route...";
                case LoadingEnum.Uploading: return "Uploading...";
                case LoadingEnum.Downloading: return "Downloading...";
                default: return "";
            }
        }
    }

    /// <summary>
    /// Used by ProgressIndicatorHelper to get strings
    /// </summary>
    public enum LoadingEnum
    {
        Location,
        Stops,
        Loading,
        Routes,
        Uploading,
        Downloading,
        None
    }

}
