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
using LiveCharts;
using LiveCharts.Wpf;
using System.Threading;
using ssi.Controls.Other.NovaServerUtility;
using static ssi.DatabaseNovaServerWindow;

namespace ssi
{
    /// <summary>
    /// Interaction logic for ExplanationWindow.xaml
    /// </summary>
    public partial class CounterFactualWindow : Window
    {

        public string modelPath;
        private string mlBackend;
        private string trainerPath;
        public bool getNewExplanation;
        public List<Processor> availableTrainers;
        public int frame;
        private static Action EmptyDelegate = delegate () { };
        private MainHandler handler;

        public SeriesCollection SeriesCollection { get; set; }
        public SeriesCollection SeriesCollection2 { get; set; }

        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }
        public Func<double, string> Formatter2 { get; set; }


        private static readonly HttpClient client = new HttpClient();

        public CounterFactualWindow(MainHandler handler)
        {
            InitializeComponent();
            explanationButton.Click += getExplanation;

            explainingLabel.Visibility = Visibility.Hidden;

            numCounterfactuals.Text = "1";
            classCounterfactual.Text = "0";

            getNewExplanation = false;

            this.handler = handler;

            string schemeType = AnnoTier.Selected.AnnoList.Scheme.Type.ToString().ToLower();
            string scheme = AnnoTier.Selected.AnnoList.Scheme.Name;

            string role = SignalTrack.Selected.Signal.Name.Split('.')[0];
            string streamName = "stream:SSIStream:" + SignalTrack.Selected.Signal.Name.Split('.')[2];
            string schemeName = "annotation:" + schemeType + ":" + scheme;
            availableTrainers = new List<Processor>();
            Thread thread = new Thread(() => getAvailableTrainers(streamName, schemeName));
            thread.Start();

            SeriesCollection = new SeriesCollection();
            SeriesCollection2 = new SeriesCollection();
        }

        private void loadTrainers()
        {
            if(availableTrainers != null)
            {
                foreach(Processor trainer in availableTrainers)
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
                if(processor != null)
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

                    Dictionary<string, string> statusresponse = await NovaServerUtility.getInfoFromServer(getIdHash(), url, client);
                    var status = statusresponse["status"];

                    if (status == "2")
                    {
                        killme = false;
                    } else if (status == "3")
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

                var counterFactuals = JsonConvert.DeserializeObject<Dictionary<int, float>>(JsonConvert.SerializeObject(explanationDic["explanation"]));
                var originalStream = JsonConvert.DeserializeObject<Dictionary<int, float>>(JsonConvert.SerializeObject(explanationDic["original_data"]));
                var localImportance = JsonConvert.DeserializeObject<Dictionary<int, float>>(JsonConvert.SerializeObject(explanationDic["local_importance"]));
                var globalImportance = JsonConvert.DeserializeObject<Dictionary<int, float>>(JsonConvert.SerializeObject(explanationDic["global_importance"]));
                Dictionary<int, float>[] explanationData = new Dictionary<int, float>[4];
                explanationData[0] = counterFactuals;
                explanationData[1] = localImportance;
                explanationData[2] = globalImportance;
                explanationData[3] = originalStream;

                this.Dispatcher.Invoke(() =>
                {
                    SeriesCollection.Clear();
                    SeriesCollection2.Clear();

                    explanationButton.IsEnabled = false;

                    Dictionary<int, string> dimNameDictionary = SignalTrack.Selected.Signal.DimLabels;

                    if (explanationData == null)
                    {
                        this.explanationButton.IsEnabled = true;

                        return;
                    }

                    Dictionary<int, float> counterFactualData = explanationData[0];
                    Dictionary<int, float> localImportanceData = explanationData[1];
                    Dictionary<int, float> globalImportanceData = explanationData[2];
                    Dictionary<int, float> originalData = explanationData[3];

                    //Labels = new string[explanationData.Count];
                    ChartValues<float> counterFactualScores = new ChartValues<float>();
                    ChartValues<float> localImportanceScores = new ChartValues<float>();
                    ChartValues<float> globalImportanceScores = new ChartValues<float>();
                    ChartValues<float> originalDataScores = new ChartValues<float>();


                    explanationChart.AxisX[0].Labels = new string[counterFactualData.Count];
                    explanationChart.AxisX[0].FontSize = 9;
                    int labelLength = 16;
                    int i = 0;

                    foreach (var entry in counterFactualData)
                    {
                        counterFactualScores.Add(entry.Value);

                        if (dimNameDictionary.Count == 0)
                        {
                            explanationChart.AxisX[0].Labels[i] = entry.Key.ToString();
                        }
                        else
                        {
                            string featureName = dimNameDictionary[entry.Key];
                            if (featureName.Length > labelLength)
                            {
                                featureName = featureName.Substring(0, labelLength) + "...";
                            }
                            explanationChart.AxisX[0].Labels[i] = featureName;
                        }

                        i++;
                    }

                    foreach (var entry in originalData)
                    {
                        originalDataScores.Add(entry.Value);
                    }

                    SeriesCollection.Add(new ColumnSeries
                    {
                        Title = "Counterfactual",
                        Values = counterFactualScores
                    });

                    SeriesCollection.Add(new ColumnSeries
                    {
                        Title = "Original",
                        Values = originalDataScores
                    });

                    Formatter = value => value.ToString("N");


                    localImportanceChart.AxisX[0].Labels = new string[localImportanceData.Count];
                    localImportanceChart.AxisX[0].FontSize = 9;

                    i = 0;

                    foreach (var entry in localImportanceData)
                    {
                        localImportanceScores.Add(entry.Value);
                        if (dimNameDictionary.Count == 0)
                        {
                            localImportanceChart.AxisX[0].Labels[i] = entry.Key.ToString();
                        }
                        else
                        {
                            string featureName = dimNameDictionary[entry.Key];
                            if (featureName.Length > labelLength)
                            {
                                featureName = featureName.Substring(0, labelLength) + "...";
                            }
                            localImportanceChart.AxisX[0].Labels[i] = featureName;
                        }

                        i++;
                    }

                    foreach (var entry in globalImportanceData)
                    {
                        globalImportanceScores.Add(entry.Value);
                    }

                    SeriesCollection2.Add(new ColumnSeries
                    {
                        Title = "local",
                        Values = localImportanceScores
                    });

                    SeriesCollection2.Add(new ColumnSeries
                    {
                        Title = "global",
                        Values = globalImportanceScores
                    });

                    Formatter2 = value => value.ToString("N");

                    DataContext = this;

                    this.explanationButton.IsEnabled = true;
                    _busyIndicator.IsBusy = false;
                });

            }
            catch (Exception e)
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
            string role = SignalTrack.Selected.Signal.Name.Split('.')[0];
            string fileName = SignalTrack.Selected.Signal.Name.Replace(role + ".", "");
            double sr = SignalTrack.Selected.Signal.rate;

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
                    {"name", fileName},
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
                        { new StringContent("DICE"), "explainer"},
                        { new StringContent(JsonConvert.SerializeObject(this.frame)), "frame_id" },
                        { new StringContent(numCounterfactuals.Text), "num_counterfactuals" },
                        { new StringContent(classCounterfactual.Text), "class_counterfactual" },
                        { new StringContent(getIdHash()), "jobID" },
                        { new StringContent(json), "data" },
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

        private void Window_Closing(object sender, CancelEventArgs e)
        {
        }

        private string getIdHash()
        {

            int result = NovaServerUtility.GetDeterministicHashCode("database" + DatabaseHandler.DatabaseName + "session" + DatabaseHandler.SessionInfo + "username" + Properties.Settings.Default.MongoDBUser + "explainer" + "dice" + "model" + modelPath);

            var jobIDhash = (Math.Abs(result)).ToString();
            int MaxLength = 8;
            if (jobIDhash.Length > MaxLength)
                jobIDhash = jobIDhash.ToString().Substring(0, MaxLength);

            return jobIDhash;
        }

    }

}
