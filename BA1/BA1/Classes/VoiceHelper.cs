using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Windows.Phone.Speech.VoiceCommands;

namespace BA1
{
    /// <summary>
    /// Static class to help with Voice Commands
    /// </summary>
    public static class VoiceHelper
    {
        #region Initializations

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

        #endregion

        #region Constants

        public const string VoiceCommandName = "voiceCommandName";
        public const string CommandSetName = "BusAlarmEnu";
        public const string RouteNumPhraseList = "route_num";
        public const string StopNamePhraseList = "stop_name";
        public const string StopSearchByRouteCommand = "StopSearchByRoute";
        public const string StopSearchByStopCommand = "StopSearchByStop";
        public const string AlarmSetCommand = "TrackStopByStop";

        #endregion

        #region Phraselist updates

        /// <summary>
        /// Update route_num
        /// </summary>
        public static async Task UpdateRoutePhraseList()
        {
            try
            {
                VoiceCommandSet vcs;
                if (VoiceCommandService.InstalledCommandSets.TryGetValue(VoiceHelper.CommandSetName, out vcs))
                {
                    await vcs.UpdatePhraseListAsync(VoiceHelper.RouteNumPhraseList, TransitInfo.RouteIDs.Keys);
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

        /// <summary>
        /// Update stop_name
        /// </summary>
        /// <returns></returns>
        public static async Task UpdateStopNamePhraseList()
        {
            // Get stop names
            HashSet<string> stopNames = new HashSet<string>();
            foreach (var id in AppSettings.RecentIds.Value)
            {
                if (AppSettings.KnownStops.Value.ContainsKey(id))
                {
                    stopNames.Add(AppSettings.KnownStops.Value[id].Name);
                }
            }
            foreach (BusStop stop in BusStop.PinnedStops)
            {
                stopNames.Add(stop.Name);
            }

            // Try updating the phrase list
            try
            {
                VoiceCommandSet vcs;
                if (VoiceCommandService.InstalledCommandSets.TryGetValue(VoiceHelper.CommandSetName, out vcs))
                {
                    await vcs.UpdatePhraseListAsync(VoiceHelper.StopNamePhraseList, stopNames);
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

        #endregion

    }
}
