using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.IsolatedStorage;

namespace BA1
{
    /// <summary>
    /// Class that contains all isolated storage for app - not just settings
    /// </summary>
    public static class AppSettings
    {
        public static IsolatedStorageSettings settings { get; private set; }

        static AppSettings()
        {
            settings = IsolatedStorageSettings.ApplicationSettings;
            KnownRoutes = new SavedSetting<Dictionary<string, BusRoute>>("rout");
            KnownStops = new SavedSetting<Dictionary<string, BusStop>>("stop");
            RecentIds = new SavedSetting<LinkedList<string>>("recs");

            LocationConsent = new SavedSetting<bool>("cons", false);

            AlarmThreshold = new SavedSetting<double>("radi", 0.5);
        }

        public static SavedSetting<Dictionary<string, BusRoute>> KnownRoutes { get; private set; }
        public static SavedSetting<Dictionary<string, BusStop>> KnownStops { get; private set; }
        public static SavedSetting<LinkedList<string>> RecentIds { get; private set; }

        /// <summary>
        /// Do we have permission to use location
        /// </summary>
        public static SavedSetting<Boolean> LocationConsent { get; private set; }

        /// <summary>
        /// Geofence radius
        /// </summary>
        public static SavedSetting<double> AlarmThreshold { get; private set; }
    }

    /// <summary>
    /// Class allowing settings to only be loaded and saved when necessary
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class SavedSetting<T> where T : new()
    {
        private static IsolatedStorageSettings IsoSettings = IsolatedStorageSettings.ApplicationSettings;

        public string Key { get; private set; }
        public T Value { get; set; }

        public SavedSetting(string key, T defval)
        {
            Key = key;
            this.Load(defval);
        }
        public SavedSetting(string key)
        {
            Key = key;
            this.Load();
        }

        /// <summary>
        /// Load setting from Isolated Storage
        /// </summary>
        /// <returns>Success</returns>
        public T Load(T defval)
        {
            if (IsoSettings.Contains(this.Key))
            {
                this.Value = (T)IsoSettings[this.Key];
            }
            else
            {
                this.Value = defval;
            }
            return Value;
        }
        public T Load()
        {
            return this.Load(new T());
        }

        /// <summary>
        /// Update the save setting to Value
        /// </summary>
        /// <returns></returns>
        public bool Save()
        {
            if (IsoSettings.Contains(this.Key))
            {
                if (((T)IsoSettings[this.Key]).Equals(this.Value))
                {
                    IsoSettings[Key] = this.Value;
                    IsoSettings.Save();
                    return true;
                }
                return false;
            }
            else
            {
                IsoSettings.Add(Key, this.Value);
                IsoSettings.Save();
                return true;
            }
        }

        /// <summary>
        /// Update to nu_val and save
        /// </summary>
        /// <param name="nu_val"></param>
        /// <returns></returns>
        public bool UpdateSave(T nu_val)
        {
            this.Value = nu_val;
            return this.Save();
        }

    }

}
