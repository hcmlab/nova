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
            DatabaseCMLExtractFeaturesWindow dialog = new DatabaseCMLExtractFeaturesWindow(this, DatabaseCMLExtractFeaturesWindow.Mode.EXTRACT);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();
        }

        private void databaseCMLMergeFeatures_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLExtractFeaturesWindow dialog = new DatabaseCMLExtractFeaturesWindow(this, DatabaseCMLExtractFeaturesWindow.Mode.MERGE);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();
        }

        private void databaseCMLTrainAndPredict_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.TRAIN);
            showDialogClearWorkspace(dialog);
        }        

        private void databaseCMLCompleteStep()
        {                    
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.COMPLETE);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();
        }
        
        private void databaseCMLFusionStep()
        {
            saveSelectedAnno(true);
            DatabaseCMLBayesNetWindow dialog = new DatabaseCMLBayesNetWindow(this);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();
        }


        private void databaseCMLFusionPredictStep()
        {
            saveSelectedAnno(true);
            DatabaseCMLFusionPredictWindow dialog = new DatabaseCMLFusionPredictWindow(this);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();
        }

        private void databaseCMLCompleteStep_Click(object sender, RoutedEventArgs e)
        {
            saveSelectedAnno(true);
            databaseCMLCompleteStep();
        }

        private void databaseCMLFusion_Click(object sender, RoutedEventArgs e)
        {
            databaseCMLFusionStep();
        }
        private void databaseCMLFusionPredict_Click(object sender, RoutedEventArgs e)
        {
            databaseCMLFusionPredictStep();
        }


        

        private void runCMLProcess(string tool, string options)
        {
            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.WindowStyle = ProcessWindowStyle.Normal;
            startInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\" + tool + ".exe";
            startInfo.Arguments = options;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }
        
        private string runCMLTool(string tool, string mode, List<object> parameters, Dictionary<string,object> arguments, string logName)
        {
            string result = "";
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\" + logName + ".log";

            File.Delete(logPath);

            try
            {
                StringBuilder optionsBuilder = new StringBuilder();   
                
                if (mode != null || mode == "")
                {
                    optionsBuilder.Append("--" + mode + " ");
                }
                            
                foreach (KeyValuePair<string, object> arg in arguments)
                {
                    optionsBuilder.Append("-" + arg.Key + " ");
                    if (arg.Value != null)
                    {
                        optionsBuilder.Append(arg.Value.ToString() + " ");
                    }
                }

                optionsBuilder.Append("-log \"" + logPath + "\" ");

                foreach (object par in parameters)
                {
                    optionsBuilder.Append(par.ToString() + " ");
                }

                string options = optionsBuilder.ToString();
                string optionsNoPassword = null;
                if (arguments.ContainsKey("password"))
                {
                    optionsNoPassword = options.Replace(arguments["password"].ToString(), "*");
                }
                else
                {
                    optionsNoPassword = options;
                }

                string filename = AppDomain.CurrentDomain.BaseDirectory + "\\" + tool + ".exe";

                runCMLProcess(tool, options);

                result += "\n-------------------------------------------\r\n" + filename + " " + optionsNoPassword + "\n-------------------------------------------\r\n";
                result += File.ReadAllText(logPath);

                if (mode == "train" && arguments.ContainsKey("cooperative"))
                {
                    AnnoTierStatic.Selected.CMLCompleteTrainOptions = options;
                }
                if (mode == "forward" && arguments.ContainsKey("cooperative"))
                {
                    AnnoTierStatic.Selected.CMLCompletePredictOptions = options;
                }
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
            }

            return result;
        }

        public string CMLExtractFeature(string chainPath, int nParallel, string fromPath, string toPath, string frameStep, string leftContext, string rightContext)
        {
            List<object> parameters = new List<object>();
            parameters.Add("\"" + chainPath + "\"");
            parameters.Add("\"" + fromPath + "\"");
            parameters.Add("\"" + toPath + "\"");

            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments["list"] = null;
            arguments["parallel"] = nParallel;
            arguments["step"] = frameStep;
            arguments["left"] = leftContext;
            arguments["right"] = rightContext;

            return runCMLTool("xmlchain", null, parameters, arguments, "cml-extract");
        }

        public string CMLMergeFeature(string rootDir, string sessions, string roles, string inputStreams, string outputStream, bool force)
        {            
            List<object> parameters = new List<object>();
            parameters.Add("\"" + rootDir + "\"");
            parameters.Add("\"" + roles + "\"");
            parameters.Add("\"" + inputStreams + "\"");
            parameters.Add("\"" + outputStream + "\"");

            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments["list"] = sessions;
            if (force)  arguments["force"] = null;            

            return runCMLTool("cmltrain", "merge", parameters, arguments, "cml-merge");
        }

        public string CMLTrainModel(string templatePath, string trainerPath, string datapath, string server, string username, string password, string database, string sessions, string scheme, string roles, string annotator, string stream, string leftContext, string rightContext, string balance, bool complete, double cmlbegintime, string multisessionpath = null)
        {
            string[] split = server.Split(':');
            string ip = split[0];
            string port = split[1];

            List<object> parameters = new List<object>();
            parameters.Add("\"" + datapath +  "\"");
            parameters.Add(ip);
            parameters.Add(port);
            parameters.Add(database);
            parameters.Add(roles);
            parameters.Add(scheme);
            parameters.Add(annotator);
            parameters.Add("\"" + stream + "\"");
            parameters.Add("\"" + templatePath + "\"");
            parameters.Add("\"" + trainerPath + "\"");

            Dictionary <string, object> arguments = new Dictionary<string, object>();
            if (multisessionpath == null) arguments["list"] = sessions;
            arguments["left"] = leftContext;
            arguments["right"] = rightContext;
            arguments["balance"] = balance;
            arguments["username"] = username;
            arguments["password"] = password;
            arguments["cmlbegintime"] = cmlbegintime;
            if (multisessionpath != null) arguments["multisession"] = "\"" + multisessionpath + "\"";
            if (complete) arguments["cooperative"] = null;

            return runCMLTool("cmltrain", "train", parameters, arguments, "cml-train");            
        }

        public string CMLEvaluateModel(string evalPath, string trainerPath, string datapath, string server, string username, string password, string database, string sessions, string scheme, string roles, string annotator, string stream, bool loso)
        {
            string[] split = server.Split(':');
            string ip = split[0];
            string port = split[1];

            List<object> parameters = new List<object>();
            parameters.Add("\"" + datapath + "\\" + database + "\"");
            parameters.Add(ip);
            parameters.Add(port);
            parameters.Add(database);
            parameters.Add(roles);
            parameters.Add(scheme);
            parameters.Add(annotator);
            parameters.Add("\"" + stream + "\"");
            parameters.Add("\"" + trainerPath + "\"");
            parameters.Add("\"" + evalPath + "\"");

            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments["list"] = sessions;
            arguments["username"] = username;
            arguments["password"] = password;
            if (loso) arguments["loso"] = null;

            return runCMLTool("cmltrain", "eval", parameters, arguments, "cml-eval");
        }

        public string CMLPredictAnnos(string trainerPath, 
            string datapath, 
            string server, 
            string username, 
            string password, 
            string database, 
            string sessions, 
            string scheme, 
            string roles, 
            string annotator, 
            string stream, 
            string leftContext, 
            string rightContext,
            double confidence,
            double minGap,
            double minDur,
            bool complete,
            double cmlbegintime)
        {

            string[] split = server.Split(':');
            string ip = split[0];
            string port = split[1];

            List<object> parameters = new List<object>();
            parameters.Add("\"" + datapath + "\\" + database + "\"");
            parameters.Add(ip);
            parameters.Add(port);
            parameters.Add(database);
            parameters.Add(roles);
            parameters.Add(scheme);
            parameters.Add(annotator);
            parameters.Add("\"" + stream + "\"");
            parameters.Add("\"" + trainerPath + "\"");

            Dictionary<string, object> arguments = new Dictionary<string, object>();
            arguments["list"] = sessions;
            arguments["left"] = leftContext;
            arguments["right"] = rightContext;
            arguments["mingap"] = minGap;
            arguments["mindur"] = minDur;
            arguments["username"] = username;
            arguments["password"] = password;
            arguments["cmlbegintime"] = cmlbegintime;
            if (complete) arguments["cooperative"] = null;

            return runCMLTool("cmltrain", "forward", parameters, arguments, "cml-predict");            
        }

        public string CMLPredictBayesFusion(string roleout,
          string sessionpath,
          string schemespath,
          string server,
          string username,
          string password,
          string datapath,
          string database,
          string outputscheme,
          string roles,
          string outputannotator,
          bool tocontinuous,
          string netpath, float filter)
        {
            string result = "";

            string[] split = server.Split(':');
            string ip = split[0];
            string port = split[1];
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\" + "cml-fusion.log";

            File.Delete(logPath);

            string cont = (tocontinuous == true) ? "true" : "false";
            string filt = ((filter != -1.0f)  ?  "-filter " + filter : "");


            try
            {
                string options_no_pass = " -username " + username +
                        " -log \"" + logPath + "\"" +
                        " -role_out \"" + roleout + "\"" +
                        " -netpath " + "\"" + netpath + "\" " + filt;
                string options = options_no_pass + " -password " + password;
                string arguments = "\"" + sessionpath + "\" " +
                      "\"" + schemespath + "\" " +
                      "\"" + datapath + "\\" + database + "\" " +
                      ip + " " +
                      port + " " +
                      database + " " +
                      roles + " " +
                      outputannotator + " " +
                      outputscheme + " " +
                      cont;



                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\bayesfusion.exe";
                startInfo.Arguments = "--bayesnetfusion " + options + " " + arguments;
                result += "\n-------------------------------------------\r\n" + startInfo.FileName + " --bayesnetfusion " + options_no_pass + " " + arguments + "\n-------------------------------------------\r\n";
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                //result += File.ReadAllText(logPath);
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
            }

            return result;
        }

        public string CMLTrainBayesianNetwork(string netPath, string datasetpath, bool isdynamic)
        {
            string result = "";
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\cml-bayestrain.log";

            if(File.Exists(logPath)) File.Delete(logPath);

            try
            {
               string options =  (isdynamic ? "-dynamic " : "") + " -log \"" + logPath + "\"" ;
               string arguments = netPath + " " + datasetpath;


                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "bayesfusion.exe";
                startInfo.Arguments = "--bayesnettrain " + options + " " + arguments;
                result += "\n-------------------------------------------\r\n" + startInfo.FileName + " " + startInfo.Arguments + "\n-------------------------------------------\r\n";
                process.StartInfo = startInfo;
                process.Start();
                process.WaitForExit();
                result += File.ReadAllText(logPath);
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
            }

            return result;
        }

    }
}
