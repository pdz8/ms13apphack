using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Phone.Scheduler;
using Microsoft.Phone.Shell;

namespace BA1
{
    /// <summary>
    /// Handles alarm creation and editing
    /// </summary>
    public class AlarmHandler
    {
        #region Singleton

        private static AlarmHandler _instance = null;
        /// <summary>
        /// Singleton
        /// </summary>
        public static AlarmHandler Instance
        {
            get { return _instance ?? (_instance = new AlarmHandler()); }
        }

        /// <summary>
        /// This class is effectively static
        /// </summary>
        private AlarmHandler()
        {
        }

        #endregion

        private ScheduledNotification Notif = null;

        /// <summary>
        /// Set an alarm/reminder for the given bus stop
        /// </summary>
        /// <param name="bs"></param>
        public void PrepareAlarm(BusStop bs)
        {
            this.ClearAlarms();

            if (bs == null) return;

            Alarm alarm = new Alarm(bs.Id)
            {
                BeginTime = DateTime.Now.AddHours(2),
                Content = "Your stop is nearing!",
                RecurrenceType = RecurrenceInterval.None,
                Sound = new Uri("/Sounds/clockring.mp3", UriKind.Relative)
            };

            ScheduledActionService.Add(alarm);
            this.Notif = alarm;
        }

        /// <summary>
        /// Change the alarm's BeginTime to now
        /// </summary>
        /// <param name="bs"></param>
        public void TriggerAlarm(BusStop bs)
        {
            if (bs == null) return;

            var alarm = ScheduledActionService.Find(bs.Id) as Alarm;
            if (alarm == null) return;

            alarm.BeginTime = DateTime.Now.AddSeconds(30);
            try
            {
                ScheduledActionService.Replace(alarm);
            }
            catch
            {
                ShellToast toast = new ShellToast()
                {
                    Content = "Your stop is nearing",
                    NavigationUri = new Uri("/Pages/PanoPage.xaml", UriKind.Relative),
                    Title = "Stop Alarm"
                };
                toast.Show();
            }

            this.Notif = null;
        }

        /// <summary>
        /// Remove all scheduled alarms.
        /// This can only be called in the foreground
        /// </summary>
        public void ClearAlarms()
        {
            var actions = ScheduledActionService.GetActions<ScheduledNotification>();
            foreach (var action in actions)
            {
                ScheduledActionService.Remove(action.Name);
            }
        }

    }
}
