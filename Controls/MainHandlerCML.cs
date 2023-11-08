using CSJ2K.j2k.image;
using FFMediaToolkit.Graphics;
using MathNet.Numerics.Distributions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ssi.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media.Imaging;
using Tamir.SharpSsh.jsch;



namespace ssi
{
    public partial class MainHandler
    {

        private static readonly HttpClient client = new HttpClient();
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

        private void databaseCMLTrainAndPredictTrain_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.TRAIN);
            showDialogClearWorkspace(dialog);
        }
        private void databaseCMLTrainAndPredictPredict_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.PREDICT);
            showDialogClearWorkspace(dialog);
        }
        private void databaseCMLTrainAndPredictEval_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLTrainAndPredictWindow dialog = new DatabaseCMLTrainAndPredictWindow(this, DatabaseCMLTrainAndPredictWindow.Mode.EVALUATE);
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
            startInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "ssi\\" + tool + ".exe";
            startInfo.Arguments = options;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();
        }
        
        private string runCMLTool(string tool, string mode, List<object> parameters, Dictionary<string,object> arguments, string logName)
        {

            string result = "";
            string logPath = AppDomain.CurrentDomain.BaseDirectory + logName + ".log";

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

                string filename = AppDomain.CurrentDomain.BaseDirectory + "ssi\\" + tool + ".exe";


                if (mode == "train" && arguments.ContainsKey("cooperative"))
                {
                    AnnoTierStatic.Selected.CMLCompleteTrainOptions = options;
                }
                if (mode == "forward" && arguments.ContainsKey("cooperative"))
                {
                    AnnoTierStatic.Selected.CMLCompletePredictOptions = options;
                }

               // options =  options.Replace(arguments["list"].ToString(), "none");
                
                runCMLProcess(tool, options);

                result += "\n-------------------------------------------\r\n" + filename + " " + optionsNoPassword + "\n-------------------------------------------\r\n";
                result += File.ReadAllText(logPath);

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
            if(complete) parameters.Add("\"" + datapath + "\\" + database + "\"");
            else parameters.Add("\"" + datapath + "\"");
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
          
            arguments["list"] = sessions;
            arguments["left"] = leftContext;
            arguments["right"] = rightContext;
            arguments["balance"] = balance;
            arguments["username"] = username;
            arguments["password"] = password;
            arguments["cmlbegintime"] = cmlbegintime;
            if (multisessionpath != null && !complete) arguments["multisession"] = "\"" + multisessionpath + "\"";
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


        public async Task<Dictionary<string, string>> PythonBackEndPredict(string templatePath, string trainerScript, string trainerPath, string dataPath, string server, string username, string password, string database, string sessions, DatabaseScheme scheme, string roles, string annotator, DatabaseStream stream, string leftContext, string rightContext, string balance, bool complete, double cmlbegintime, string multisessionpath = null)
        {
            var trainerScriptPath = Directory.GetParent(templatePath) + "\\" + trainerScript;



            try
            {
                var content = new MultipartFormDataContent
                {
                    { new StringContent(templatePath), "templatePath" },
                    { new StringContent(trainerPath), "trainerPath" },
                    { new StringContent(dataPath), "dataPath" },
                    { new StringContent(server), "server" },
                    { new StringContent(username), "username" },
                    { new StringContent(password), "password" },
                    { new StringContent(database), "database" },
                    { new StringContent(sessions), "sessions" },
                    { new StringContent(scheme.Name), "scheme" },
                    { new StringContent(roles), "roles" },
                    { new StringContent(annotator), "annotator" },
                    { new StringContent(stream.Name), "stream" },
                    { new StringContent(leftContext), "leftContext" },
                    { new StringContent(rightContext), "rightContext" },
                    { new StringContent(balance), "balance" },
                    { new StringContent(complete.ToString()), "complete" },
                    { new StringContent(cmlbegintime.ToString()), "cmlbegintime" },
                    { new StringContent(multisessionpath), "multisessionpath" },
                    { new StringContent(trainerScriptPath), "trainerScript" }



                };


                string url = "http://localhost:5000/forward";
                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();
                var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                if (explanationDic["success"] == "failed")
                {
                    return null;
                }

               // CreateTrainerFile(trainerPath, dataPath, trainerScriptPath, database, sessions, roles, scheme, rightContext, leftContext, balance);

                return explanationDic;

            }
            catch (Exception e)
            {

                return null;
            }


        }


        public int CreateTrainerFile(string trainerPath, string dataPath, string trainerScriptPath, string database, string sessions, string roles, DatabaseScheme scheme, DatabaseStream stream, string rightContext, string leftContext, string balance)
        {

            string filename = trainerPath + ".trainer";

            StreamWriter swheader = new StreamWriter(filename, false, System.Text.Encoding.Default);
            swheader.WriteLine("<?xml version=\"1.0\" ?>");
            swheader.WriteLine("<trainer ssi-v=\"5\">");
            swheader.WriteLine("\t<info trained=\"true\"/>");
            swheader.WriteLine("\t<meta rightContext=\"" + rightContext + "\" leftContext=\"" + leftContext + "\" balance=\"" + balance + "\" backend=\"Python\" />");
            swheader.WriteLine("\t<register>");
            swheader.WriteLine("\t\t<item name=\"" + MainHandler.PYTHON_VERSION_FOLDER + "\" />");
            swheader.WriteLine("\t</register>");
            swheader.WriteLine("\t<streams>");

            string[] sessionssplit = sessions.Split(';');
            string[] rolessplit = roles.Split(';');
            //load exemplary data stream
            string path = dataPath + "\\" + database + "\\" + sessionssplit[0] + "\\" + rolessplit[0] + "." + stream.Name + ".stream";
            Signal signal = Signal.LoadStreamFile(path);
            signal.type = Signal.Type.FLOAT;
            swheader.WriteLine("\t\t<item byte=\"" + signal.bytes + "\" dim=\"" + signal.dim + "\" sr=\"" + signal.rate + "\" type=\"" + signal.type + "\" />");
            swheader.WriteLine("\t</streams>");
            swheader.WriteLine("\t<classes>");


            AnnoScheme annoscheme = DatabaseHandler.GetAnnotationScheme(scheme.Name);
            foreach (AnnoScheme.Label i in annoscheme.Labels)
            {
                swheader.WriteLine("\t\t<item name=\"" + i.Name + "\" />");
            }
            swheader.WriteLine("\t\t<item name=\"REST\" />");
            swheader.WriteLine("\t</classes>");

            swheader.WriteLine("\t<users>");



            foreach (string i in sessionssplit)
            {
                swheader.WriteLine("\t\t<item name=\"" + i + "\" />");
            }

            swheader.WriteLine("\t</users>");

            File.Copy(trainerScriptPath, trainerPath + "." + Path.GetFileName(trainerScriptPath), true);
            swheader.WriteLine("\t<model create=\"Model\" stream=\"0\" script=\"" + Path.GetFileName(trainerPath) + "." + Path.GetFileName(trainerScriptPath) + "\"/>");
            swheader.WriteLine("</trainer>");


            swheader.Close();

            swheader.Dispose();

            return 1;
        }

        public async Task<Dictionary<string, string>> PythonBackEndTraining(MultipartFormDataContent content)
        {
            try
            {
                string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
                string url = "http://" + tokens[0] + ":" + tokens[1] + "/train";
                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();
                var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                if (explanationDic["success"] == "failed")
                {
                    return null;
                }

                return explanationDic;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Dictionary<string, string>> pythonBackEndExtraction(MultipartFormDataContent content)
        {
            try
            {
                string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
                string url = "http://" + tokens[0] + ":" + tokens[1] + "/process";
                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();
                var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                if (explanationDic["success"] == "failed")
                {
                    return null;
                }

                return explanationDic;
            }
            catch (Exception)
            {
                return null;
            }
        }

        public async Task<Dictionary<string, string>> PythonBackEndPredict(MultipartFormDataContent content)
        {
            try
            {
                string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
                string url = "http://" + tokens[0] + ":" + tokens[1] + "/process";
                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();
                var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                if (explanationDic["success"] == "failed")
                {
                    return null;
                }

                return explanationDic;
            }
            catch (Exception)
            {
                return null;
            }
        }


        public async Task<Dictionary<string, string>> PythonBackEndPredictComplete(MultipartFormDataContent content)
        {
            bool done = false;
            HttpResponseMessage response;
            string responseString;
            Dictionary<string, string> explanationDic = null;

            do
            {
                try
                {
                    string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
                    string url = "http://" + tokens[0] + ":" + tokens[1] + "/predict";
                    response = await client.PostAsync(url, content);
                    responseString = await response.Content.ReadAsStringAsync();
                    explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
                    done = true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message);
                }
            } while (!done);

            if (explanationDic != null && explanationDic["success"] == "failed")
            {
                return null;
            }
            return explanationDic;
        }

        public async Task<Dictionary<string, string>> PythonBackEndComplete(MultipartFormDataContent content)
        {
            try
            {
                HttpClientHandler httpClientHandler = new HttpClientHandler();
                using (var client = new HttpClient(httpClientHandler, false))
                {

                    string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
                    string url = "http://" + tokens[0] + ":" + tokens[1] + "/complete";
                    var response = await client.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();
                    var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                    if (explanationDic["success"] == "failed")
                    {
                        return null;
                    }

                    return explanationDic;
                }
            }
            catch (Exception)
            {

                return null;
            }
        }


        public async void getResultFromServer(MultipartFormDataContent content)
        {
           

            string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
            string url = "http://" + tokens[0] + ":" + tokens[1] + "/fetch_result";


            var response = client.PostAsync(url, content).Result;
            var inputfilename = response.Content.Headers.ContentDisposition.FileName;
            var responseContent = await response.Content.ReadAsByteArrayAsync();
            if (response.Content.Headers.ContentType.MediaType == "video/mp4")
            {
                string filename = FileTools.SaveFileDialog(inputfilename, ".mp4", "Video (*.mp4)|*.mp4", "");
                if (filename == null) return;

                Stream t = new FileStream(filename, FileMode.Create);
                BinaryWriter b = new BinaryWriter(t);
                b.Write(responseContent);
                t.Close();
                MessageBox.Show("Stored video at " + filename);
            }

            else if (response.Content.Headers.ContentType.MediaType == "application/x-zip-compressed")
            {
                string filename = FileTools.SaveFileDialog(inputfilename, ".zip", "Archive (*.zip)|*.zip", "");
                if (filename == null) return;
                File.WriteAllBytes(filename, responseContent);
            }

            else if (response.Content.Headers.ContentType.MediaType == "text/plain")
            {
                string result = System.Text.Encoding.UTF8.GetString(responseContent);
                MessageBox.Show(result);
            }
            else if (response.Content.Headers.ContentType.MediaType == "application/octet-stream")
            {
                string result = System.Text.Encoding.UTF8.GetString(responseContent);
                MessageBox.Show(result);
            }
            else if (response.Content.Headers.ContentType.MediaType == "image/jpeg")
            {
                System.Drawing.Image image = byteArrayToImage(responseContent);
                ImageView view = new ImageView(image, inputfilename);
                view.Show();

            }
            else
            {
                MessageBox.Show("Result not found");
            }
           

        }

        public Image byteArrayToImage(byte[] byteArrayIn)
        {
            Image returnImage = null;
            try
            {
                MemoryStream ms = new MemoryStream(byteArrayIn, 0, byteArrayIn.Length);
                ms.Write(byteArrayIn, 0, byteArrayIn.Length);
                returnImage = Image.FromStream(ms, true);//Exception occurs here
            }
            catch { }
            return returnImage;
        }


        public Dictionary<string, string> getLogFromServer(MultipartFormDataContent content)
        {

            string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
            string url = "http://" + tokens[0] + ":" + tokens[1] + "/log";

            var response = client.PostAsync(url, content).Result;
            var responseContent = response.Content;
            string responseString = responseContent.ReadAsStringAsync().Result;
            var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

            return explanationDic;
        }

        public Dictionary<string, string> getStatusFromServer(MultipartFormDataContent content)
        {
            string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
            string url = "http://" + tokens[0] + ":" + tokens[1] + "/job_status";

            var response = client.PostAsync(url, content).Result;
            var responseContent = response.Content;
            string responseString = responseContent.ReadAsStringAsync().Result;
            var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

            return explanationDic;
        }



        public JObject get_info_from_server()
        {
            string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
            string url = "http://" + tokens[0] + ":" + tokens[1] + "/cml_info";

            var response = client.GetAsync(url).Result;
            var responseContent = response.Content;
            string responseString = responseContent.ReadAsStringAsync().Result;
            var result = JObject.Parse(responseString);


            return result;
        }

        public void cancleCurrentAction(MultipartFormDataContent content)
        {
            try
            {
                string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
                string url = "http://" + tokens[0] + ":" + tokens[1] + "/cancel";
                var response = client.PostAsync(url, content).Result;

                var responseContent = response.Content;
                string responseString = responseContent.ReadAsStringAsync().Result;
                var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
            }
            catch (Exception)
            {

            }
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
