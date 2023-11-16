using NovA;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Net.Http;
using System.Web.Script.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using static ssi.DatabaseNovaServerWindow;
using System.Threading;

namespace ssi.Controls.Other
{
    /// <summary>
    /// Interaction logic for ExplanationWindowInnvestigate.xaml
    /// </summary>
    public partial class ExplanationWindowTfExplain : Window
    {

        public string modelPath;
        private string mlBackend;
        private string trainerPath;
        public bool getNewExplanation;
        public List<Processor> availableTrainers;
        public int frame;
        private string videoname;
        private string role;
        public byte[] img;
        public string postprocess;

        private List<Dictionary<string, string>> classDictionaryList;
        private static readonly HttpClient client = new HttpClient();


        public ExplanationWindowTfExplain()
        {
            InitializeComponent();
            explanationButton.Click += getExplanation;
            explainer.SelectedIndex = 0;
            lrpalpha.Text = "1.0";
            lrpbeta.Text = "0.0";

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
                Processor processor = NovaServerUtility.NovaServerUtility.parseTrainerFile(t);
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

                    Dictionary<string, string> statusresponse = await NovaServerUtility.NovaServerUtility.getInfoFromServer(getIdHash(), url, client);
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

                var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(result);

                this.Dispatcher.Invoke(() =>
                {
                    if (explanationDic == null)
                    {
                        this.explanationButton.IsEnabled = true;
                        return;
                    }

                    byte[] explanation = System.Convert.FromBase64String(explanationDic["explanation"]);

                    BitmapImage explanationBitmap = new BitmapImage();
                    explanationBitmap.BeginInit();
                    explanationBitmap.StreamSource = new System.IO.MemoryStream((byte[])explanation);
                    explanationBitmap.EndInit();
                    explanationBitmap.Freeze();

                    System.Windows.Controls.StackPanel wrapper = new System.Windows.Controls.StackPanel();
                    string className = "";

                    if (classDictionaryList.Count > 0)
                    {
                        className = classDictionaryList[Int32.Parse(explanationDic["prediction"])]["name"];
                    }
                    else
                    {
                        Int32.Parse(explanationDic["prediction"]);
                    }

                    //if (this.idToClassName.ContainsKey(Int32.Parse(explanationDic["prediction"])))
                    //{
                    //    className = this.idToClassName[Int32.Parse(explanationDic["prediction"])];
                    //}
                    //else
                    //{
                    //    className = explanationDic["prediction"];
                    //}

                    System.Windows.Controls.Label info = new System.Windows.Controls.Label
                    {
                        Content = "Class: " + className + " Score: " + explanationDic["prediction_score"]
                    };

                    System.Windows.Controls.Image img = new System.Windows.Controls.Image
                    {
                        Source = explanationBitmap,
                    };

                    wrapper.Margin = new Thickness(5);
                    wrapper.Children.Add(info);
                    wrapper.Children.Add(img);

                    this.containerExplainedImages.Children.Add(wrapper);

                    this.containerImageToBeExplained.Visibility = Visibility.Hidden;
                    this.explanationButton.IsEnabled = true;
                    _busyIndicator.IsBusy = false;
                });

            }
            catch
            {
                this.Dispatcher.Invoke(() =>
                {
                    _busyIndicator.IsBusy = false;
                });
                return;
            }
        }

        private void getExplanation(object sender, RoutedEventArgs e)
        {

            _busyIndicator.BusyContent = "Generating explanation...";
            _busyIndicator.IsBusy = true;
            string sessionsstr = "[\"" + DatabaseHandler.SessionName + "\"]";
            double sr = MediaBoxStatic.Selected.Media.GetSampleRate();

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
                { new StringContent(modelPath), "modelPath" },
                { new StringContent(Properties.Settings.Default.DatabaseAddress.Split(':')[0]), "dbHost" },
                { new StringContent(Properties.Settings.Default.DatabaseAddress.Split(':')[1]), "dbPort" },
                { new StringContent(Properties.Settings.Default.MongoDBUser), "dbUser" },
                { new StringContent(MainHandler.Decode(Properties.Settings.Default.MongoDBPass)), "dbPassword" },
                { new StringContent(DatabaseHandler.DatabaseName), "dataset" },
                { new StringContent(sessionsstr), "sessions" },
                { new StringContent(getIdHash()), "jobID" },
                { new StringContent(json), "data"  },
                { new StringContent("TF_EXPLAIN"), "explainer"},
                { new StringContent( explainer.Text.ToUpper()), "tfExplainer"},
                { new StringContent(JsonConvert.SerializeObject(this.frame)), "frame_id" },
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

        public void deactiveExplainationButton()
        {
            this.explanationButton.Visibility = Visibility.Hidden;
        }

        private string getIdHash()
        {
            int result = NovaServerUtility.NovaServerUtility.GetDeterministicHashCode("database" + DatabaseHandler.DatabaseName + "session" + DatabaseHandler.SessionInfo + "username" + Properties.Settings.Default.MongoDBUser + "explainer" + "lime tabular" + "model" + modelPath);

            var jobIDhash = (Math.Abs(result)).ToString();
            int MaxLength = 8;
            if (jobIDhash.Length > MaxLength)
                jobIDhash = jobIDhash.ToString().Substring(0, MaxLength);

            return jobIDhash;
        }

    }
}
