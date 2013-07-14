using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using Microsoft.Phone.Tasks;
using System.Reflection;

namespace BA1.Pages
{
    public partial class AboutPage : PhoneApplicationPage
    {
        //bool startup = true;

        public AboutPage()
        {
            InitializeComponent();

            this.VersionBlock.Text = "Version: " + Assembly.GetExecutingAssembly().FullName.Split('=')[1].Split(',')[0];
        }

        /// <summary>
        /// React to user feedback buttons
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            switch (btn.Content.ToString())
            {
                case "Rate and Review":
                    MarketplaceReviewTask mrt = new MarketplaceReviewTask();
                    mrt.Show();
                    break;
                case "Suggest Changes":
                    EmailComposeTask ect = new EmailComposeTask();
                    ect.To = "zieskeapps@yahoo.com";
                    ect.Subject = String.Format("Unit Converter");
                    ect.Show();
                    break;
                default:
                    break;
            }
        }
    }
}