using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ssi
{
    public partial class MainHandler
    {
        private void databaseCMLExtractFeatures_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLExtractFeaturesWindow dialog = new DatabaseCMLExtractFeaturesWindow(this);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();

        }

        private void databaseCMLTrain_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.TRAIN);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();

        }

        private void databaseCMLPredict_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.PREDICT);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();

        }

        public string CMLExtractFeature(string chainPath, string fromPath, string toPath, string frameStep, string leftContext, string rightContext)
        {
            string result = "";

            try
            {
                string arguments = " -step " + frameStep +
                    " -left " + leftContext +
                    " -right " + rightContext +
                    " -debug cml.log " +
                    chainPath + " " +
                    fromPath + " " +
                    toPath;

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = "xmlchain.exe";
                startInfo.Arguments = arguments;
                result += startInfo.Arguments + "\n-------------------------------------------\r\n";
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                result += File.ReadAllText("cml.log");    
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
            }

            return result;
        }

        public string CMLTrainModel(string templatePath, string trainerPath, string datapath, string server, string username, string password, string database, string sessions, string scheme, string roles, string annotator, string stream, string leftContext, string rightContext)
        {
            string result = "";

            string[] split = server.Split(':');
            string ip = split[0];
            string port = split[1];

            try
            {
                string options_no_pass = "-left " + leftContext +
                        " -right " + rightContext +
                        " -username " + username +                        
                        " -list " + sessions +
                        " -log cml.log";
                string options = options_no_pass + " -password " + password;
                string arguments = "\"" + datapath + "\\" + database + "\" " +
                        ip + " " +
                        port + " " +
                        database + " " +
                        roles + " " +
                        scheme + " " +
                        annotator + " " +
                        "\"" + stream + "\" " +
                        "\"" + templatePath + "\" " +
                        "\"" + trainerPath + "\"";

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = "cmltrain.exe";
                startInfo.Arguments = "--train " + options + " " + arguments;
                result += "--train " + options_no_pass + " " + arguments + "\n-------------------------------------------\r\n";
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                result += File.ReadAllText("cml.log");
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
            }

            return result;
        }

        public string CMLPredictAnnos(string trainerPath, string datapath, string server, string username, string password, string database, string sessions, string scheme, string roles, string annotator, string stream, string leftContext, string rightContext)
        {
            string result = "";

            string[] split = server.Split(':');
            string ip = split[0];
            string port = split[1];

            try
            {
                string options_no_pass = "-left " + leftContext +
                        " -right " + rightContext +
                        " -username " + username +
                        " -list " + sessions +
                        " -finished" +
                        " -log cml.log";
                string options = options_no_pass + " -password " + password;
                string arguments = "\"" + datapath + "\\" + database + "\" " +
                        ip + " " +
                        port + " " +
                        database + " " +
                        roles + " " +
                        scheme + " " +
                        annotator + " " +
                        "\"" + stream + "\" " +
                        "\"" + trainerPath + "\"";

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = "cmltrain.exe";
                startInfo.Arguments = "--forward " + options + " " + arguments;
                result += "--forward " + options_no_pass + " " + arguments + "\n-------------------------------------------\r\n";
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                result += File.ReadAllText("cml.log");
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
            }

            return result;
        }

        public string CMLCompleteTier(int context, AnnoTier tier, string stream, double confidence = -1.0, double minGap = 0.0, double minDur = 0.0)
        {
            string result = "";
            Directory.CreateDirectory(Properties.Settings.Default.DatabaseDirectory + "\\" + Properties.Settings.Default.DatabaseName + "\\models");

            string username = Properties.Settings.Default.MongoDBUser;
            string password = Properties.Settings.Default.MongoDBPass;
            string session = Properties.Settings.Default.LastSessionId;
            string datapath = Properties.Settings.Default.DatabaseDirectory;
            string ipport = Properties.Settings.Default.DatabaseAddress;
            string[] split = ipport.Split(':');
            string ip = split[0];
            string port = split[1];
            string database = Properties.Settings.Default.DatabaseName;
            string role = tier.AnnoList.Meta.Role;
            string scheme = tier.AnnoList.Scheme.Name;
            string annotator = tier.AnnoList.Meta.Annotator;

            bool isTrained = false;
            bool isForward = false;

            try
            {
                {
                    string arguments = " -cooperative "
                    + "-context " + context +
                    " -username " + username +
                    " -password " + password +
                    " -filter " + session +
                    " -log cml.log " +
                    "\"" + datapath + "\\" + database + "\" " +
                    ip + " " +
                    port + " " +
                    database + " " +
                    role + " " +
                    scheme + " " +
                    annotator + " " +
                    "\"" + stream + "\"";

                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmltrain.exe";
                    startInfo.Arguments = "--train" + arguments;
                    result += startInfo.Arguments + "\n-------------------------------------------\r\n";
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                    result += File.ReadAllText("cml.log");

                    if (process.ExitCode == 0)
                    {
                        isTrained = true;
                    }
                    else
                    {
                        return result;
                    }
                }
                {
                    string arguments = " -cooperative "
                            + "-context " + context +
                            " -username " + username +
                            " -password " + password +
                            " -filter " + session +
                            " -confidence " + confidence +
                            " -mingap " + minGap +
                            " -mindur " + minDur +
                            " -log cml.log " +
                            "\"" + datapath + "\\" + database + "\" " +
                            ip + " " +
                            port + " " +
                            database + " " +
                            role + " " +
                            scheme + " " +
                            annotator + " " +
                            "\"" + stream + "\"";


                    Process process = new Process();
                    ProcessStartInfo startInfo = new ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmltrain.exe";
                    startInfo.Arguments = "--forward" + arguments;
                    result += startInfo.Arguments + "\n-------------------------------------------\r\n";
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                    result += File.ReadAllText("cml.log");

                    if (process.ExitCode == 0)
                    {
                        isForward = true;
                    }
                    else
                    {
                        return result;
                    }
                }

                if (isTrained && isForward)
                {
                    reloadAnnoTierFromDatabase(tier);
                }

            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
            }

            return result;
        }

    }
}
