using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Live;
using Newtonsoft.Json;
using System.Windows;

namespace BA1
{
    public static class CloudStorage
    {
        #region Constants

        private const string ROOT_FOLDER = "me/skydrive";
        private const string BA_FolderName = "Bus Alarm";
        private const string FILE_EXTENSION = ".busalarm";
        private const string defaultAgency = "routes";

        #endregion

        private static LiveConnectClient _client;

        /// <summary>
        /// Skydrive client
        /// </summary>
        public static LiveConnectClient Client
        {
            get { return CloudStorage._client; }
            set
            {
                CloudStorage._client = value;
                SkydriveVM.Instance.Client = CloudStorage._client;
            }
        }

        /// <summary>
        /// Download all Bus Alarm files to app
        /// </summary>
        /// <returns></returns>
        public static async Task RestoreFromSkydrive()
        {
            // Search for Bus Alarm folder
            List<FolderFileData> folders = await CloudStorage.GetFiles(CloudStorage.ROOT_FOLDER);
            FolderFileData baFolder = folders.FirstOrDefault(f => f.Name == CloudStorage.BA_FolderName);
            if (baFolder == null)
            {
                //MessageBox.Show("Cannot find 'Bus Alarm' folder in Skydrive root", "Error", MessageBoxButton.OK);
                throw new Exception("Cannot find 'Bus Alarm' folder in Skydrive root");
            }
            string folder_id = baFolder.Id;

            // Get files
            List<FolderFileData> files = await CloudStorage.GetFiles(folder_id);
            if (files == null)
            {
                //MessageBox.Show("Could not find files in 'Bus Alarm' folder", "Error", MessageBoxButton.OK);
                throw new Exception("Could not find files in 'Bus Alarm' folder");
            }
            var baFiles = files.Where(f => f.Name.EndsWith(CloudStorage.FILE_EXTENSION));

            // Downloadfiles
            foreach (FolderFileData ffd in baFiles)
            {
                await CloudStorage.DownloadTNS(ffd.Id);
            }
        }

        /// <summary>
        /// Backup all known routes and stops to skydrive
        /// </summary>
        /// <returns></returns>
        public static async Task BackupToSkydrive()
        {
            // Group by agency
            var knownTNS = new TransitNetworkSearch(AppSettings.KnownRoutes.Value, AppSettings.KnownStops.Value);
            Dictionary<string, TransitNetworkSearch> agencies = TransitNetworkSearch.GroupByAgency(knownTNS);
            //if (agencies.Count == 0) return;

            // Get folder
            List<FolderFileData> folders = await CloudStorage.GetFiles(CloudStorage.ROOT_FOLDER);
            string folder_id;
            FolderFileData ffd = folders.FirstOrDefault(f => f.Name == CloudStorage.BA_FolderName);
            if (ffd == null)
            {
                folder_id = await CreateFolder(CloudStorage.ROOT_FOLDER, CloudStorage.BA_FolderName);
            }
            else
            {
                folder_id = ffd.Id;
            }

            // Upload agencies
            foreach (var keyval in agencies)
            {
                await CloudStorage.UploadTNS(folder_id, keyval.Key, keyval.Value);
            }
        }

        /// <summary>
        /// Download TNS from path in Skydrive and update known routes/stops
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task DownloadTNS(string path)
        {
            try
            {
                LiveDownloadOperationResult ldor = await CloudStorage.Client.DownloadAsync(path + "/content");
                //TransitNetworkSearch tns = JsonConvert.DeserializeObject<TransitNetworkSearch>("hi");
                TransitNetworkSearch tns;
                using (StreamReader sr = new StreamReader(ldor.Stream))
                {
                    string content = sr.ReadToEnd();
                    tns = JsonConvert.DeserializeObject<TransitNetworkSearch>(content);
                }
                TransitNetworkSearch.SaveTNS(tns);
            }
            catch
            {

            }
        }

        /// <summary>
        /// Create a folder at the given location with the given name
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static async Task<string> CreateFolder(string path, string name)
        {
            try
            {
                var folderProperties = new Dictionary<string, object>();
                folderProperties.Add("name", name);
                LiveOperationResult opres = await CloudStorage.Client.PostAsync(path, folderProperties);
                dynamic result = opres.Result;
                return result.id;
            }
            catch (Exception e)
            {
                //Util.BeginInvoke(() => MessageBox.Show(e.Message, "Error finding folder", MessageBoxButton.OK));
                throw;
            }
        }

        /// <summary>
        /// Get all files at the folder
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static async Task<List<FolderFileData>> GetFiles(string path)
        {
            try
            {
                LiveOperationResult opres = await CloudStorage.Client.GetAsync(path + "/files");
                var wrapper = JsonConvert.DeserializeObject<FolderWrapper>(opres.RawResult);
                return wrapper.Data;
            }
            catch (Exception e)
            {
                //Util.BeginInvoke(() => MessageBox.Show(e.Message, "Error finding folder", MessageBoxButton.OK));
                throw;
            }
        }

        /// <summary>
        /// Upload Transit Network Search obj
        /// </summary>
        /// <param name="folder_id"></param>
        /// <param name="agency"></param>
        /// <param name="tns"></param>
        /// <returns></returns>
        public static async Task UploadTNS(
            string folder_id, 
            string agency, 
            TransitNetworkSearch tns)
        {
            agency = agency.Trim();
            agency = string.IsNullOrWhiteSpace(agency) ? CloudStorage.defaultAgency : agency;
            try
            {
                await CloudStorage.Client.UploadAsync(
                    folder_id,
                    agency + CloudStorage.FILE_EXTENSION,
                    JsonConvert.SerializeObject(tns).ToStream(),
                    OverwriteOption.Overwrite);
            }
            catch
            {
                throw;
            }
        }

    }
}
