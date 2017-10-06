using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Octokit;
using System.Windows;
using System.Net;
using System.Text.RegularExpressions;
using System.ComponentModel;
using System.IO;

namespace ssi
{
    public partial class MainHandler
    {
        WebClient webClient;
   

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
                //MessageBox.Show("An Error occured. API limit reached, or no Internet Connection!");
            }
        }

        private async void checkForCMLUpdates(bool silent = false)
        {

            try
            {
            var client = new GitHubClient(new ProductHeaderValue("ssi"));
            var commits = await client.Repository.Commit.GetAll("hcmlab", "ssi");
            var first = commits.First();
            var last = commits.Last();
            var result = await client.Repository.Commit.Compare("hcmlab", "ssi", last.Sha, first.Sha);


            var files = result.Files;
            bool IsCMLTrainUptodate = false;
            foreach (var file in files)
            {
                if (file.Filename == "bin/x64/vc140/cmltrain.exe")
                {
                    if (Properties.Settings.Default.CMLTrainexeGitSha == file.Sha)
                    {
                        IsCMLTrainUptodate = true;
                    }
                    else
                    {
                        Properties.Settings.Default.CMLTrainexeGitSha = file.Sha;
                        Properties.Settings.Default.Save();

                    } 

                    break;
                }
            }


            string cmltrainexe = "cmltrain.exe";
            string cmltrainexePath = AppDomain.CurrentDomain.BaseDirectory + cmltrainexe;
            string SSIbinaryGitPath = "https://github.com/hcmlab/ssi/raw/master/bin/x64/vc140/";

            if (!IsCMLTrainUptodate || !(File.Exists(cmltrainexePath)))
            {


                    MessageBoxResult mb = MessageBox.Show("A new version of the Cooperative Machine Learning tools is available, update now?", "Update available", MessageBoxButton.YesNo);

                if (mb == MessageBoxResult.No)
                {
                    return;
                }

                string[] fileEntries = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory);

                foreach (string file in fileEntries)
                {
                    if (File.Exists(file) && Path.GetFileName(file).StartsWith("ssi") && file.EndsWith("dll"))
                    {
                        File.Delete(file);
                    }
                }


                    /*
                    * CMLTrain and XMLchain executables are downloaded from the official SSI git repository.
                     * */

                try
                {
                    DownloadFile(SSIbinaryGitPath + cmltrainexe, cmltrainexePath);
                }
                catch
                {
                    MessageBox.Show("Can't update tools, check your internet conenction!");
                    return;
                }





                //Download xmlchain, if not present yet.
                string xmlchainexe = "xmlchain.exe";
                string xmlchainexePath = AppDomain.CurrentDomain.BaseDirectory + xmlchainexe;



                DownloadFile(SSIbinaryGitPath + xmlchainexe, xmlchainexePath);




                    //Download libmongoc-1.0.dll, if not present yet.
                    string libmongocdll = "libmongoc-1.0.dll";
                    string libmongocdllPath = AppDomain.CurrentDomain.BaseDirectory + libmongocdll;

                    DownloadFile(SSIbinaryGitPath + libmongocdll, libmongocdllPath);


                    //Download libbson-1.0.dll, if not present yet.
                    string libsondll = "libbson-1.0.dll";
                    string libbsondllPath = AppDomain.CurrentDomain.BaseDirectory + libsondll;

                    DownloadFile(SSIbinaryGitPath + libsondll, libbsondllPath);

                    //Download ssiframe.dll, if not present yet (cml tools will do this automatically, here we force to overwrite it).
                    string ssiframedll = "ssiframe.dll";
                    string ssiframedllPath = AppDomain.CurrentDomain.BaseDirectory + ssiframedll;

                    DownloadFile(SSIbinaryGitPath + ssiframedll, ssiframedllPath);

                    if (File.Exists(xmlchainexePath) && File.Exists(cmltrainexePath))
                {

                    long sizexmlchain = new System.IO.FileInfo(xmlchainexePath).Length;
                    long sizecmltrain = new System.IO.FileInfo(cmltrainexePath).Length;

                    if (sizexmlchain == 0 || sizecmltrain == 0)
                    {
                        if (File.Exists(xmlchainexePath)) File.Delete(xmlchainexePath);
                        if (File.Exists(cmltrainexePath)) File.Delete(cmltrainexePath);
                      
                        MessageBox.Show("Could not update Cooperative Machine Learning Tools");
                    }
                    else MessageBox.Show("Successfully updated Cooperative Machine Learning Tools");
                }
            }

            else if (!silent) MessageBox.Show("Cooperative Machine Learning Tools are already up to date");

            }
            catch
            {
                //MessageBox.Show("An Error occured. API limit reached, or no Internet Connection!");
            }
        }


        private void updateApplication_Click(object sender, RoutedEventArgs e)
        {
            checkForUpdates(false);
        }

        private void updateCML_Click(object sender, RoutedEventArgs e)
        {
            checkForCMLUpdates(false);
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


        public void DownloadFile(string urlAddress, string location)
        {
            using (webClient = new WebClient())
            {

                Uri URL = new Uri(urlAddress);
                // Start the stopwatch which we will be using to calculate the download speed

                try
                {
                    // Start downloading the file
                    webClient.DownloadFile(URL, location);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }



    }
}
