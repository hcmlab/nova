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

namespace ssi
{
    /// <summary>
    /// Interaction logic for ExplanationWindow.xaml
    /// </summary>
    public partial class FeatureExplanationWindow : Window
    {

        public string modelPath;
        public byte[] img;
        public bool getNewExplanation;
        public BitmapImage explainedImg;
        public int topLablesV;
        public int numSamplesV;
        public int numFeaturesV;
        public string hideRestV;
        public string hideColorV;
        public string positiveOnlyV;
        private List<ModelTrainer> modelsTrainers;
        public Dictionary<int, string> idToClassName;
        private string basePath;
        public List<float> sample;
        public float[] featurestream;
        private IntPtr lk;
        private static Action EmptyDelegate = delegate () { };
        public uint dim;
        //private string featurepath;

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> Formatter { get; set; }

        private static readonly HttpClient client = new HttpClient();
        

        public FeatureExplanationWindow(string featurePath)
        {

            InitializeComponent();
            explanationButton.Click += getExplanation;
            modelPath = Properties.Settings.Default.explainModelPath;
            if(modelPath != null)
            {
                modelLoaded.Text = Path.GetFileName(modelPath);
            }
            explainingLabel.Visibility = Visibility.Hidden;

            numFeatures.Text = "2";
            getNewExplanation = false;


            string schemeType = AnnoTier.Selected.AnnoList.Scheme.Type.ToString().ToLower();
            string scheme = AnnoTier.Selected.AnnoList.Scheme.Name;

            string[] featurenames = Path.GetFileNameWithoutExtension(featurePath).Split('.');
            string featurename = "";

            for(int i = 2; i < featurenames.Length; i++ ){
                if(featurename == "")
                {
                    featurename = featurenames[i];
                }
                else
                {
                    featurename = featurename + "." + featurenames[i];
                }
            }

            basePath = Properties.Settings.Default.CMLDirectory + "\\models\\trainer\\" + schemeType + "\\" + scheme + "\\" + "feature" + "{" + featurename + "}";

            DirectoryInfo di = new DirectoryInfo(basePath);

            modelsTrainers = new List<ModelTrainer>();

            idToClassName = new Dictionary<int, string>();
            loadModelAndTrainer(basePath);
            parseTrainerFile(getTrainerFile(basePath, modelPath));

            SeriesCollection = new SeriesCollection();

        }

        private void loadModelAndTrainer(string path)
        {
            
            DirectoryInfo di = new DirectoryInfo(path);
            if(di.Exists)
            {

                foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    if (fi.Extension == ".model")
                    {
                        modelsTrainers.Add(new ModelTrainer(fi.FullName, null));
                        modelsBox.Items.Add(fi.Name + " " + fi.Directory.Name);
                    }
                }

                foreach( var t in modelsTrainers)
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

        private async Task<Dictionary<string, float>> getExplanationFromBackend()
        {
            try
            {

               
                var content = new MultipartFormDataContent
                {
                    { new StringContent(modelPath), "model_path" },
                    { new StringContent(JsonConvert.SerializeObject(this.sample).ToString()), "sample" },
                    { new StringContent(JsonConvert.SerializeObject(this.featurestream).ToString()), "data" },
                    { new StringContent(this.dim.ToString()), "dim" }
                };

                numFeaturesV = Int32.Parse(numFeatures.Text);

                string url = "http://localhost:5000/tabular?&numfeatures=" + numFeaturesV;

                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();


                var explanationDic = (JObject)JsonConvert.DeserializeObject(responseString);
                
                if (explanationDic["success"].ToString() == "failed")
                {
                    return null;
                }


                var explanations = JsonConvert.DeserializeObject<Dictionary<string, float>>(JsonConvert.SerializeObject(explanationDic["explanation"]));

                return explanations;

            }
            catch (Exception e)
            {
                return null;
            }
        }

        private async void getExplanation(object sender, RoutedEventArgs e)
        {

            SeriesCollection.Clear();
            //explanationAnalysis.Visibility = Visibility.Visible;
            explanationButton.IsEnabled = false;

            //containerExplainedImages.Children.Clear();

            Dictionary<string, float> explanationData = await getExplanationFromBackend();

            if(explanationData == null)
            {
                MainHandler.restartExplanationBackend();
                this.explanationButton.IsEnabled = true;

                return;
            }


            //Labels = new string[explanationData.Count];
            ChartValues<float> importanceScores = new ChartValues<float>();
            explanationChart.AxisX[0].Labels = new string[explanationData.Count];

            int i = 0;

            foreach(var entry in explanationData)
            {
                importanceScores.Add(entry.Value);
                explanationChart.AxisX[0].Labels[i] = entry.Key;
                i++;
            }

            SeriesCollection.Add(new ColumnSeries
            {
                Title = "",
                Values = importanceScores
            });

            Formatter = value => value.ToString("N");

            DataContext = this;

            this.explanationButton.IsEnabled = true;
            
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

        private class ModelTrainer
        {
            public string model{ get; set; }
            public string trainer { get; set; }

            public ModelTrainer(string model, string trainer)
            {
                this.model = model;
                this.trainer = trainer;
            }
        }

    }

}
