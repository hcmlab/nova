using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssi
{
    public partial class MainHandler
    {

        public string CompleteTier(int context, AnnoTier tier, string stream, double confidence = -1.0, double minGap = 0.0, double minDur = 0.0)
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
                    databaseReload(tier);
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
