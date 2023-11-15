using NovA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Drawing;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using System.Windows.Media.Effects;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Xml;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ssi.Controls.Other.NovaServerUtility;
using static ssi.DatabaseNovaServerWindow;
using System.Threading;

namespace ssi
{
    /// <summary>
    /// Interaction logic for ExplanationWindow.xaml
    /// </summary>
    public partial class ExplanationWindow : Window
    {
        public string modelPath;
        private string mlBackend;
        private string trainerPath;
        public bool getNewExplanation;
        public List<Processor> availableTrainers;
        public byte[] img;
        public BitmapImage explainedImg;

        public int topLablesV;
        public int numSamplesV;
        public int numFeaturesV;
        public string hideRestV;
        public string hideColorV;
        public string positiveOnlyV;
        public int frame;

        private string role;
        private string videoname;
        public Dictionary<int, string> idToClassName;
        private string basePath;
        private IntPtr lk;
        private static Action EmptyDelegate = delegate () { };

        private static readonly HttpClient client = new HttpClient();
        

        public ExplanationWindow()
        {

            InitializeComponent();
            explanationButton.Click += getExplanation;

            topLabels.Text = "2";
            numFeatures.Text = "15";
            numSamples.Text = "800";

            getNewExplanation = false;

            string schemeType = AnnoTier.Selected.AnnoList.Scheme.Type.ToString().ToLower();
            string scheme = AnnoTier.Selected.AnnoList.Scheme.Name;

            videoname = Path.GetFileNameWithoutExtension(MediaBoxStatic.Selected.Media.GetFilepath()).Split('.')[1];
            role = Path.GetFileNameWithoutExtension(MediaBoxStatic.Selected.Media.GetFilepath()).Split('.')[0];
            string streamName = "stream:video";
            string schemeName = "annotation:" + schemeType + ":" + scheme;
            availableTrainers = new List<Processor>();
            Thread thread = new Thread(() => getAvailableTrainers(streamName, schemeName));
            thread.Start();
                            
        }

        private void loadTrainers()
        {
            if (availableTrainers != null)
            {
                foreach (Processor trainer in availableTrainers)
                {
                    modelsBox.Items.Add(trainer.Name);
                }
                modelsBox.SelectedIndex = 0;
            }

        }

        private async void getAvailableTrainers(string inputFilter, string outputFilter)
        {
            this.Dispatcher.Invoke(() =>
            {
                _busyIndicator.BusyContent = "Loading trainers...";
                _busyIndicator.IsBusy = true;
            });

            JArray inputFilterList = new JArray();
            inputFilterList.Add(inputFilter);

            string jsonInput = inputFilterList.ToString(Newtonsoft.Json.Formatting.None);

            JArray outputFilterList = new JArray();
            outputFilterList.Add(outputFilter);

            string jsonOutput = outputFilterList.ToString(Newtonsoft.Json.Formatting.None);

            MultipartFormDataContent trainer_content = new MultipartFormDataContent
            {
                { new StringContent(jsonInput), "input_filter"},
                { new StringContent(jsonOutput), "output_filter"}
            };

            string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
            string url = "http://" + tokens[0] + ":" + tokens[1] + "/cml_info";
            var response = await client.PostAsync(url, trainer_content);
            var responseString = await response.Content.ReadAsStringAsync();
            var result = JObject.Parse(responseString);
            JObject trainer = JObject.FromObject(result["trainer_ok"]);

            foreach (var t in trainer)
            {
                Processor processor = NovaServerUtility.parseTrainerFile(t);
                if (processor != null)
                {
                    availableTrainers.Add(processor);
                }

            }

            this.Dispatcher.Invoke(() =>
            {
                loadTrainers();
                _busyIndicator.IsBusy = false;
            });

        }

        //private async Task<List<Tuple<int, double, BitmapImage>>> sendExplanationRequestForm()
        //{

        //    string sessionsstr = "[\"" + DatabaseHandler.SessionName + "\"]";

        //    topLablesV = Int32.Parse(topLabels.Text);
        //    numSamplesV = Int32.Parse(numSamples.Text);
        //    numFeaturesV = Int32.Parse(numFeatures.Text);

        //    hideRestV = hideRest.IsChecked.Value.ToString();
        //    hideColorV = hideColor.IsChecked.Value.ToString();
        //    positiveOnlyV = positiveOnly.IsChecked.Value.ToString();

        //    JObject ob = new JObject
        //    {
        //      {"id", "explanation_stream"},
        //      {"type", "input" },
        //      {"src", "db:stream" },
        //      {"name", videoname},
        //      {"role", role},
        //      {"active", "True" }
        //    };
        //    JArray data = new JArray();
        //    data.Add(ob);
        //    string json = data.ToString(Newtonsoft.Json.Formatting.None);

        //    MultipartFormDataContent content = new MultipartFormDataContent
        //    {
        //        { new StringContent(modelPath), "modelPath" },
        //        { new StringContent(Properties.Settings.Default.DatabaseAddress.Split(':')[0]), "dbHost" },
        //        { new StringContent(Properties.Settings.Default.DatabaseAddress.Split(':')[1]), "dbPort" },
        //        { new StringContent(Properties.Settings.Default.MongoDBUser), "dbUser" },
        //        { new StringContent(MainHandler.Decode(Properties.Settings.Default.MongoDBPass)), "dbPassword" },
        //        { new StringContent(DatabaseHandler.DatabaseName), "dataset" },
        //        { new StringContent(sessionsstr), "sessions" },
        //        { new StringContent(getIdHash()), "jobID" },
        //        { new StringContent(json), "data"  },
        //        { new StringContent("LIME_IMAGE"), "explainer"},
        //        { new StringContent(JsonConvert.SerializeObject(this.frame)), "frame" },
        //        { new StringContent(numFeaturesV.ToString()), "numFeatures" },
        //        { new StringContent(topLablesV.ToString()), "topLabels" },
        //        { new StringContent(numSamplesV.ToString()), "numSamples" },
        //        { new StringContent(hideRestV.ToString()), "hideRest" },
        //        { new StringContent(hideColorV.ToString()), "hideColor" },
        //        { new StringContent(positiveOnlyV.ToString()), "positiveOnly" }
        //    };

        //    try
        //    {
        //        string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
        //        string url = "http://" + tokens[0] + ":" + tokens[1] + "/explain";
        //        var response = await client.PostAsync(url, content);

        //        var responseString = await response.Content.ReadAsStringAsync();

        //        var explanationDic = (JObject)JsonConvert.DeserializeObject(responseString);

        //        //var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, float>>(responseString);

        //        if (explanationDic["success"].ToString() == "failed")
        //        {
        //            return null;
        //        }

        //        JArray explanations = JArray.Parse(explanationDic["explanations"].Value<object>().ToString());

        //        List<Tuple<int, double, BitmapImage>> explanationData = new List<Tuple<int, double, BitmapImage>>();

        //        foreach (var item in explanations)
        //        {
        //            var temp = item.ToArray();
        //            int cl = (int)temp[0];
        //            double cl_score = (double)temp[1];
        //            string explanation_img64 = (string)temp[2];
        //            byte[] explanation = System.Convert.FromBase64String(explanation_img64);

        //            BitmapImage explanation_bitmap = new BitmapImage();
        //            explanation_bitmap.BeginInit();
        //            explanation_bitmap.StreamSource = new System.IO.MemoryStream(explanation);
        //            explanation_bitmap.EndInit();
        //            explanation_bitmap.Freeze();

        //            Tuple<int, double, BitmapImage> tuple = new Tuple<int, double, BitmapImage>(cl, cl_score, explanation_bitmap);
        //            explanationData.Add(tuple);
        //        }

        //        return explanationData;
        //    }
        //    catch (Exception e)
        //    {
        //        return null;
        //    }


        //}

        public async void explanationStatus(MultipartFormDataContent content)
        {
            try
            {
                string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
                string url = "http://" + tokens[0] + ":" + tokens[1] + "/explain";
                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();

                url = "http://" + tokens[0] + ":" + tokens[1] + "/job_status";

                bool killme = true;

                while (killme)
                {

                    Dictionary<string, string> statusresponse = await NovaServerUtility.getInfoFromServer(getIdHash(), url, client);
                    var status = statusresponse["status"];

                    if (status == "2")
                    {
                        killme = false;
                    }
                    else if (status == "3")
                    {
                        //TODO display message in gui that generating explanation failed
                        killme = false;
                        return;
                    }
                    Thread.Sleep(1000);
                }

                url = "http://" + tokens[0] + ":" + tokens[1] + "/fetch_result";

                MultipartFormDataContent contentFetch = new MultipartFormDataContent
                {
                    { new StringContent(getIdHash()), "jobID"  }
                };

                response = client.PostAsync(url, contentFetch).Result;
                var inputfilename = response.Content.Headers.ContentDisposition.FileName;
                var responseContent = await response.Content.ReadAsByteArrayAsync();

                string result = System.Text.Encoding.UTF8.GetString(responseContent);

                var explanationDic = (JObject)JsonConvert.DeserializeObject(result);

                JArray explanations = JArray.Parse(explanationDic["explanations"].Value<object>().ToString());

                List<Tuple<int, double, BitmapImage>> explanationData = new List<Tuple<int, double, BitmapImage>>();

                foreach (var item in explanations)
                {
                    var temp = item.ToArray();
                    int cl = (int)temp[0];
                    double cl_score = (double)temp[1];
                    string explanation_img64 = (string)temp[2];
                    byte[] explanation = System.Convert.FromBase64String(explanation_img64);

                    BitmapImage explanation_bitmap = new BitmapImage();
                    explanation_bitmap.BeginInit();
                    explanation_bitmap.StreamSource = new System.IO.MemoryStream(explanation);
                    explanation_bitmap.EndInit();
                    explanation_bitmap.Freeze();

                    Tuple<int, double, BitmapImage> tuple = new Tuple<int, double, BitmapImage>(cl, cl_score, explanation_bitmap);
                    explanationData.Add(tuple);
                }

                this.Dispatcher.Invoke(() => { 
                
                    if (explanationData == null)
                    {

                        this.explanationButton.IsEnabled = true;

                        return;
                    }

                    for (int i = 0; i < explanationData.Count; i++)
                    {

                        System.Windows.Controls.StackPanel wrapper = new System.Windows.Controls.StackPanel();
                        string className = "";

                        if (this.idToClassName.ContainsKey(explanationData[i].Item1))
                        {
                            className = this.idToClassName[explanationData[i].Item1];
                        }
                        else
                        {
                            className = explanationData[i].Item1 + "";
                        }

                        System.Windows.Controls.Label info = new System.Windows.Controls.Label
                        {
                            Content = "Class: " + className + " Score: " + explanationData[i].Item2.ToString("0.###")
                        };

                        System.Windows.Controls.Image img = new System.Windows.Controls.Image
                        {
                            Source = explanationData[i].Item3,
                        };

                        int ratio = getRatio(explanationData.Count);

                        img.Height = (this.containerExplainedImages.ActualHeight - explanationData.Count * 2 * 5) / ratio;
                        img.Width = (this.containerExplainedImages.ActualWidth - explanationData.Count * 2 * 5) / ratio;


                        wrapper.Margin = new Thickness(5);
                        wrapper.Children.Add(info);
                        wrapper.Children.Add(img);

                        this.containerExplainedImages.Children.Add(wrapper);
                    }

                    this.containerImageToBeExplained.Visibility = Visibility.Hidden;
                    this.explanationButton.IsEnabled = true;
                });

            }
            catch
            {

            }
        }

        private void getExplanation(object sender, RoutedEventArgs e)
        {

            _busyIndicator.BusyContent = "Generating explanation...";
            _busyIndicator.IsBusy = true;
            string sessionsstr = "[\"" + DatabaseHandler.SessionName + "\"]";

            double sr = MediaBoxStatic.Selected.Media.GetSampleRate();

            topLablesV = Int32.Parse(topLabels.Text);
            numSamplesV = Int32.Parse(numSamples.Text);
            numFeaturesV = Int32.Parse(numFeatures.Text);

            hideRestV = hideRest.IsChecked.Value.ToString();
            hideColorV = hideColor.IsChecked.Value.ToString();
            positiveOnlyV = positiveOnly.IsChecked.Value.ToString();

            JObject anno = new JObject
                {
                    {"id", "explanation_anno"},
                    {"type", "input" },
                    {"scheme", AnnoTier.Selected.AnnoList.Scheme.Name},
                    {"annotator", AnnoTier.Selected.AnnoList.Meta.Annotator},
                    {"src", "db:annotation" },
                    {"role", role},
                    {"active", "True" }
                };

            JObject ob = new JObject
            {
              {"id", "explanation_stream"},
              {"type", "input" },
              {"src", "db:stream" },
              {"name", videoname},
              {"role", role},
              {"active", "True" }
            };

            JObject output = new JObject
                {
                    {"id", "output"},
                    {"type", "output"},
                    {"src", "request:text"}
                };

            JArray data = new JArray();
            data.Add(ob);
            data.Add(anno);
            data.Add(output);
            string json = data.ToString(Newtonsoft.Json.Formatting.None);

            MultipartFormDataContent content = new MultipartFormDataContent
            {
                { new StringContent(modelPath), "modelFilePath" },
                { new StringContent(trainerPath), "trainer_file_path" },
                { new StringContent(Properties.Settings.Default.DatabaseAddress.Split(':')[0]), "dbHost" },
                { new StringContent(Properties.Settings.Default.DatabaseAddress.Split(':')[1]), "dbPort" },
                { new StringContent(Properties.Settings.Default.MongoDBUser), "dbUser" },
                { new StringContent(MainHandler.Decode(Properties.Settings.Default.MongoDBPass)), "dbPassword" },
                { new StringContent(DatabaseHandler.DatabaseName), "dataset" },
                { new StringContent(sessionsstr), "sessions" },
                { new StringContent(getIdHash()), "jobID" },
                { new StringContent(json), "data"  },
                { new StringContent("LIME_IMAGE"), "explainer"},
                { new StringContent(JsonConvert.SerializeObject(this.frame)), "frame_id" },
                { new StringContent(numFeaturesV.ToString()), "numFeatures" },
                { new StringContent(topLablesV.ToString()), "topLabels" },
                { new StringContent(numSamplesV.ToString()), "numSamples" },
                { new StringContent(hideRestV.ToString()), "hideRest" },
                { new StringContent(hideColorV.ToString()), "hideColor" },
                { new StringContent(positiveOnlyV.ToString()), "positiveOnly" },
                { new StringContent((1/sr).ToString()), "frame_size" }
            };

            Thread thread = new Thread(() => explanationStatus(content));
            thread.Start();

        }

        private void modelsBox_selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            trainerPath = availableTrainers[cmb.SelectedIndex].Path;
            modelPath = Path.Combine(Path.GetDirectoryName(trainerPath), availableTrainers[cmb.SelectedIndex].ModelWeightsPath);
            modelLoaded.Text = availableTrainers[cmb.SelectedIndex].ModelWeightsPath;
            Properties.Settings.Default.explainModelPath = modelPath;
            Properties.Settings.Default.Save();
            mlBackend = availableTrainers[cmb.SelectedIndex].Backend;
        }



        private void Window_Closing(object sender, CancelEventArgs e)
        {

        }

        public void deactiveExplainationButton()
        {
            this.explanationButton.Visibility = Visibility.Hidden;
        }

        private int getRatio(int n)
        {

            int m = 1;

            while (true)
            {
                if (Math.Pow((m - 1), 2) < n && n <= Math.Pow(m, 2))
                {
                    return m;
                }
                m++;
                if (m > 1000)
                {
                    return -1;
                }
            }

        }

        private string getIdHash()
        {

            int result = NovaServerUtility.GetDeterministicHashCode("database" + DatabaseHandler.DatabaseName + "session" + DatabaseHandler.SessionInfo + "username" + Properties.Settings.Default.MongoDBUser + "explainer" + "lime tabular" + "model" + modelPath);

            var jobIDhash = (Math.Abs(result)).ToString();
            int MaxLength = 8;
            if (jobIDhash.Length > MaxLength)
                jobIDhash = jobIDhash.ToString().Substring(0, MaxLength);

            return jobIDhash;
        }

    }

}
