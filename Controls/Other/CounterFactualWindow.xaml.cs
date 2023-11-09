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
        private List<ModelTrainer> modelsTrainers;
        public Dictionary<int, string> idToClassName;
        private string basePath;
        public int frame;
        public float[] featurestream;
        private IntPtr lk;
        private static Action EmptyDelegate = delegate () { };
        public uint dim;
        private MainHandler handler;
        //private string featurepath;

        public SeriesCollection SeriesCollection { get; set; }
        public SeriesCollection SeriesCollection2 { get; set; }

        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }
        public Func<double, string> Formatter2 { get; set; }


        private static readonly HttpClient client = new HttpClient();


        public CounterFactualWindow(string featurePath, MainHandler handler)
        {

            InitializeComponent();
            explanationButton.Click += getExplanation;
            modelPath = Properties.Settings.Default.explainModelPath;
            if (modelPath != null)
            {
                modelLoaded.Text = Path.GetFileName(modelPath);
            }
            explainingLabel.Visibility = Visibility.Hidden;

            numCounterfactuals.Text = "1";
            classCounterfactual.Text = "0";

            getNewExplanation = false;

            this.handler = handler;

            string schemeType = AnnoTier.Selected.AnnoList.Scheme.Type.ToString().ToLower();
            string scheme = AnnoTier.Selected.AnnoList.Scheme.Name;

            string[] featurenames = Path.GetFileNameWithoutExtension(featurePath).Split('.');
            string featurename = "";

            for (int i = 2; i < featurenames.Length; i++) {
                if (featurename == "")
                {
                    featurename = featurenames[i];
                }
                else
                {
                    featurename = featurename + "." + featurenames[i];
                }
            }

            basePath = Properties.Settings.Default.CMLDirectory + "\\models\\trainer\\" + schemeType + "\\" + scheme + "\\" + "feature" + "{" + featurename + "}";

            //TODO add cml info request
            // search for applicable trainer file

            DirectoryInfo di = new DirectoryInfo(basePath);

            modelsTrainers = new List<ModelTrainer>();

            idToClassName = new Dictionary<int, string>();
            loadModelAndTrainer(basePath);
            parseTrainerFile(getTrainerFile(basePath, modelPath));

            SeriesCollection = new SeriesCollection();
            SeriesCollection2 = new SeriesCollection();

        }

        private void loadModelAndTrainer(string path)
        {

            DirectoryInfo di = new DirectoryInfo(path);
            if (di.Exists)
            {

                foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    if (fi.Extension == ".model")
                    {

                        //TODO LOL
                        string trainerPath = fi.FullName.Split('.')[0] + ".trainer";

                        modelsTrainers.Add(new ModelTrainer(fi.FullName, trainerPath, "SKLEARN"));
                        modelsBox.Items.Add(fi.Name + " " + fi.Directory.Name);
                    }
                    if (fi.Extension == ".h5")
                    {
                        modelsTrainers.Add(new ModelTrainer(fi.FullName, null, "TENSORFLOW"));
                        modelsBox.Items.Add(fi.Name + " " + fi.Directory.Name);
                    }
                }

                foreach (var t in modelsTrainers)
                {
                    foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
                    {
                        var subDirModel = Path.GetDirectoryName(t.model).Split(Path.DirectorySeparatorChar).Last();
                        var subDirTrainer = Path.GetDirectoryName(fi.FullName).Split(Path.DirectorySeparatorChar).Last();
                        if (fi.Extension == ".trainer" && string.Join(".", Path.GetFileName(t.model).Split('.').Take(2)) == fi.Name && subDirModel == subDirTrainer)
                        {
                            t.trainer = fi.FullName;
                        }
                    }
                }
            }

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
                var localImportance = JsonConvert.DeserializeObject<Dictionary<int, float>>(JsonConvert.SerializeObject(explanationDic["local_importance"]));
                var globalImportance = JsonConvert.DeserializeObject<Dictionary<int, float>>(JsonConvert.SerializeObject(explanationDic["global_importance"]));
                Dictionary<int, float>[] explanationData = new Dictionary<int, float>[3];
                explanationData[0] = counterFactuals;
                explanationData[1] = localImportance;
                explanationData[2] = globalImportance;

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
                    Dictionary<int, float> glocalImportanceData = explanationData[2];

                    //Labels = new string[explanationData.Count];
                    ChartValues<float> counterFactualScores = new ChartValues<float>();
                    ChartValues<float> localImportanceScores = new ChartValues<float>();
                    ChartValues<float> globalImportanceScores = new ChartValues<float>();

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

                    SeriesCollection.Add(new ColumnSeries
                    {
                        Title = "",
                        Values = counterFactualScores
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

                    foreach (var entry in glocalImportanceData)
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
                });

            }
            catch (Exception e)
            {
                return;
            }


        }

        private void getExplanation(object sender, RoutedEventArgs e)
        {


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


        private void modelPanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                modelPath = files[0];
                modelLoaded.Text = Path.GetFileName(modelPath);
                Properties.Settings.Default.explainModelPath = modelPath;
                Properties.Settings.Default.Save();

                //TODO add logic for retrieving ml backend

                idToClassName.Clear();
                parseTrainerFile(getTrainerFile(basePath, modelPath));
            }
        }

        private void modelsBox_selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            modelPath = modelsTrainers[cmb.SelectedIndex].model;
            modelLoaded.Text = Path.GetFileName(modelPath);
            Properties.Settings.Default.explainModelPath = modelPath;
            Properties.Settings.Default.Save();

            mlBackend = modelsTrainers[cmb.SelectedIndex].backend;
            trainerPath = modelsTrainers[cmb.SelectedIndex].trainer;

            Console.WriteLine("Model: " + modelsTrainers[cmb.SelectedIndex].model);
            Console.WriteLine("Trainer: " + modelsTrainers[cmb.SelectedIndex].trainer);
            Console.WriteLine("-------");

            idToClassName.Clear();
            parseTrainerFile(modelsTrainers[cmb.SelectedIndex].trainer);
        }

        private string getTrainerFile(string path, string modelPath)
        {
            DirectoryInfo di = new DirectoryInfo(path);

            string trainerPath = null;

            if(di.Exists)
            {
                foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    var subDirModel = Path.GetDirectoryName(modelPath).Split(Path.DirectorySeparatorChar).Last();
                    var subDirTrainer = Path.GetDirectoryName(fi.FullName).Split(Path.DirectorySeparatorChar).Last();
                    if (fi.Extension == ".trainer" && string.Join(".", Path.GetFileName(modelPath).Split('.').Take(2)) == fi.Name && subDirModel == subDirTrainer)
                    {
                        trainerPath = fi.FullName;
                    }
                }
            }
           

            return trainerPath;
        }

        private void parseTrainerFile(string path)
        {
            if(path != null)
            {
                XmlDocument trainer = new XmlDocument();
                trainer.Load(path);
                XmlNodeList classes = trainer.GetElementsByTagName("classes")[0].ChildNodes;

                for(int i = 0; i < classes.Count; i++)
                {
                    idToClassName.Add(i, classes.Item(i).Attributes["name"].Value);
                }
            }

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {

        }

        public void deactiveExplainationButton()
        {
            this.explanationButton.Visibility = Visibility.Hidden;
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


        private class ModelTrainer
        {
            public string model{ get; set; }
            public string trainer { get; set; }
            public string backend { get; set; }

            public ModelTrainer(string model, string trainer, string backend)
            {
                this.model = model;
                this.trainer = trainer;
                this.backend = backend;
            }
        }

    }

}
