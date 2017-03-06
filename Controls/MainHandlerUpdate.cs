using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using System.Windows;
using System.Net;
using System.Text.RegularExpressions;

namespace ssi
{
    public partial class MainHandler
    {
        private async void checkForUpdates(bool silent = false)
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("nova"));
                var releases = await client.Repository.Release.GetAll("hcmlab", "nova");
                var latest = releases[0];
                string LatestGitVersion = latest.TagName;

                var result = compareVersion(LatestGitVersion, BuildVersion);

                if ((result == 0 || latest.Assets.Count == 0) && !silent)
                {
                    MessageBox.Show("You already have the latest version of NOVA. (Build: " + LatestGitVersion + ")");
                }
                else if (result > 0)
                {
                    MessageBoxResult mb = MessageBox.Show("Your build version is " + BuildVersion + ". The latest version is  " + LatestGitVersion + ". Do you want to update nova to the latest version? \n\n Release Notes:\n\n " + latest.Body, "Update available!", MessageBoxButton.YesNo);
                    if (mb == MessageBoxResult.Yes)
                    {
                        string url = "https://github.com/hcmlab/nova/blob/master/bin/updater.exe?raw=true";
                        WebClient Client = new WebClient();
                        Client.DownloadFile(url, AppDomain.CurrentDomain.BaseDirectory + "updater.exe");

                        System.Diagnostics.Process updateProcess = new System.Diagnostics.Process();
                        updateProcess.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "updater.exe";
                        updateProcess.StartInfo.Arguments = LatestGitVersion;
                        updateProcess.Start();

                        System.Environment.Exit(0);
                    }
                }
                else if (result < 0 && !silent)
                {
                    MessageBox.Show("The version you are running (" + BuildVersion + ") is newer than the latest offical release (" + LatestGitVersion + ")");
                }
            }
            catch
            {
            }
        }

        private void updateApplication_Click(object sender, RoutedEventArgs e)
        {
            checkForUpdates(true);
        }

        public int compareVersion(string Version1, string Version2)
        {
            Regex regex = new Regex(@"([\d]+)");
            MatchCollection m1 = regex.Matches(Version1);
            MatchCollection m2 = regex.Matches(Version2);
            int min = Math.Min(m1.Count, m2.Count);
            for (int i = 0; i < min; i++)
            {
                if (Convert.ToInt32(m1[i].Value) > Convert.ToInt32(m2[i].Value))
                {
                    return 1;
                }
                if (Convert.ToInt32(m1[i].Value) < Convert.ToInt32(m2[i].Value))
                {
                    return -1;
                }
            }
            return 0;
        }
    }
}
