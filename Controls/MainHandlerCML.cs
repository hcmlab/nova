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

        private void databaseCMLTrain_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.TRAIN);
            showDialogClearWorkspace(dialog);
        }

        private void databaseCMLEvaluate_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.EVALUATE);
            showDialogClearWorkspace(dialog);
        }

        private void databaseCMLPredict_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.PREDICT);
            showDialogClearWorkspace(dialog);
        }

        private void databaseCMLCompleteStep()
        {
            saveSelectedAnno(true);
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.COMPLETE);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();
        }
        
        private void databaseCMLFusionStep()
        {
            saveSelectedAnno(true);
            DatabaseCMLBayesNetWindow dialog = new DatabaseCMLBayesNetWindow(this, DatabaseCMLBayesNetWindow.Mode.TRAIN);
            dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dialog.ShowDialog();
        }



        private void databaseCMLCompleteStep_Click(object sender, RoutedEventArgs e)
        {
            databaseCMLCompleteStep();
        }


        private void databaseCMLFusion_Click(object sender, RoutedEventArgs e)
        {
            databaseCMLFusionStep();
        }


      

        public string CMLExtractFeature(string chainPath, int nParallel, string fromPath, string toPath, string frameStep, string leftContext, string rightContext)
        {
            string result = "";
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\cml-extract.log";

            File.Delete(logPath);

            try
            {
                string arguments = "-list " +
                    " -parallel " + nParallel +
                    " -step " + frameStep +
                    " -left " + leftContext +
                    " -right " + rightContext +
                    " -log \"" + logPath + "\"" + " " +
                    chainPath + " " +
                    fromPath + " " +
                    toPath;

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\xmlchain.exe";
                startInfo.Arguments = arguments;
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

        public string CMLMergeFeature(string rootDir, string sessions, string roles, string inputStreams, string outputStream, bool force)
        {
            string result = "";
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\cml-merge.log";

            File.Delete(logPath);

            try
            {
                string arguments = "--merge -list " + sessions +                    
                    (force ? " -force " : "") +
                    " -log \"" + logPath + "\"" + " " +
                    rootDir + " " +
                    roles + " " +
                    inputStreams + " " +
                    outputStream;

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\cmltrain.exe";
                startInfo.Arguments = arguments;
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

        public string CMLTrainModel(string templatePath, string trainerPath, string datapath, string server, string username, string password, string database, string sessions, string scheme, string roles, string annotator, string stream, string leftContext, string rightContext, string balance, bool complete)
        {
            string result = "";

            string[] split = server.Split(':');
            string ip = split[0];
            string port = split[1];
            string logPath =  AppDomain.CurrentDomain.BaseDirectory + "\\cml-train.log";

            File.Delete(logPath);

            try
            {
                string options_no_pass = "-left " + leftContext +
                        " -right " + rightContext +
                        " -balance " + balance +
                        " -username " + username +                        
                        " -list " + sessions +
                        (complete ? " -cooperative" : "") +
                        " -log \"" + logPath + "\"";
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
                startInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\cmltrain.exe";
                startInfo.Arguments = "--train " + options + " " + arguments;
                result += "\n-------------------------------------------\r\n" + startInfo.FileName + " --train " + options_no_pass + " " + arguments + "\n-------------------------------------------\r\n";
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

        public string CMLEvaluateModel(string evalPath, string trainerPath, string datapath, string server, string username, string password, string database, string sessions, string scheme, string roles, string annotator, string stream)
        {
            string result = "";

            string[] split = server.Split(':');
            string ip = split[0];
            string port = split[1];
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\cml-eval.log";

            File.Delete(logPath);

            try
            {
                string options_no_pass = " -username " + username +
                        " -list " + sessions +
                        " -log \"" + logPath + "\"";
                string options = options_no_pass + " -password " + password;
                string arguments = "\"" + datapath + "\\" + database + "\" " +
                        ip + " " +
                        port + " " +
                        database + " " +
                        roles + " " +
                        scheme + " " +
                        annotator + " " +
                        "\"" + stream + "\" " +
                        "\"" + trainerPath + "\" " +
                        "\"" + evalPath + "\"";

                Process process = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.WindowStyle = ProcessWindowStyle.Normal;
                startInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\cmltrain.exe";
                startInfo.Arguments = "--eval " + options + " " + arguments;
                result += "\n-------------------------------------------\r\n" + startInfo.FileName + " --eval " + options_no_pass + " " + arguments + "\n-------------------------------------------\r\n";
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
            bool complete)
        {
            string result = "";

            string[] split = server.Split(':');
            string ip = split[0];
            string port = split[1];
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\cml-predict.log";

            File.Delete(logPath);

            try
            {
                string options_no_pass = "-left " + leftContext +
                        " -right " + rightContext +
                        " -confidence " + confidence +
                        " -mingap " + minGap +
                        " -mindur " + minDur +
                        " -username " + username +
                        " -list " + sessions +
                     //   " -finished" +
                        ( complete ? " -cooperative" : "" ) +
                        " -log \"" + logPath + "\"";
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
                startInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\cmltrain.exe";
                startInfo.Arguments = "--forward " + options + " " + arguments;
                result += "\n-------------------------------------------\r\n" + startInfo.FileName + " --forward " + options_no_pass + " " + arguments + "\n-------------------------------------------\r\n";
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



        public string CMLPredictFusion(string trainerPaths,
          string datapath,
          string server,
          string username,
          string password,
          string database,
          string sessions,
          string schemes,
          string roles,
          string annotators,
          string streams,
          string leftContext,
          string rightContext,
          double confidence,
          double minGap,
          double minDur,
          bool complete,
          string netpath,
          string outputprediction)
        {
            string result = "";

            string[] split = server.Split(':');
            string ip = split[0];
            string port = split[1];
            string logPath = AppDomain.CurrentDomain.BaseDirectory + "\\cml-fusion.log";

            File.Delete(logPath);


            try
            {
                string options_no_pass = " -username " + username +
                        " -list " + sessions +
                        " -log \"" + logPath + "\"" +
                        " -netpath " + "\"" + netpath + "\"";
                string options = options_no_pass + " -password " + password;
                string arguments = "\"" + datapath + "\\" + database + "\" " +
                      ip + " " +
                      port + " " +
                      database + " " +
                      roles + " " +
                      schemes + " " +
                      annotators + " " +
                      "\"" + streams + "\" " +
                      "\"" + trainerPaths + "\" " +
                      "\"" + outputprediction + "\"";

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
