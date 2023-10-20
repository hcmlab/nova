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

namespace ssi
{
    /// <summary>
    /// Interaction logic for ExplanationWindow.xaml
    /// </summary>
    public partial class ExplanationWindow : Window
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
        public int frame;
        private string filePath;
        private string videoname;
        private string role;
        private List<ModelTrainer> modelsTrainers;
        public Dictionary<int, string> idToClassName;
        private string basePath;
        private IntPtr lk;
        private static Action EmptyDelegate = delegate () { };

        private static readonly HttpClient client = new HttpClient();
        

        public ExplanationWindow()
        {

            InitializeComponent();
            explanationButton.Click += getExplanation;
            modelPath = Properties.Settings.Default.explainModelPath;
            if(modelPath != null)
            {
                modelLoaded.Text = Path.GetFileName(modelPath);
            }
            explainingLabel.Visibility = Visibility.Hidden;


            topLabels.Text = "2";
            numFeatures.Text = "15";
            numSamples.Text = "800";

            getNewExplanation = false;

            string schemeType = AnnoTier.Selected.AnnoList.Scheme.Type.ToString().ToLower();
            string scheme = AnnoTier.Selected.AnnoList.Scheme.Name;

            videoname = Path.GetFileNameWithoutExtension(MediaBoxStatic.Selected.Media.GetFilepath()).Split('.')[1];
            role = Path.GetFileNameWithoutExtension(MediaBoxStatic.Selected.Media.GetFilepath()).Split('.')[0];
            basePath = Properties.Settings.Default.CMLDirectory + "\\models\\trainer\\" + schemeType + "\\" + scheme + "\\" + "video" + "{" + videoname + "}";

            DirectoryInfo di = new DirectoryInfo(basePath);

            modelsTrainers = new List<ModelTrainer>();

            idToClassName = new Dictionary<int, string>();
            loadModelAndTrainer(basePath);
            parseTrainerFile(getTrainerFile(basePath, modelPath));
                            
        }

        private void loadModelAndTrainer(string path)
        {
            
            DirectoryInfo di = new DirectoryInfo(path);
            if(di.Exists)
            {

                foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    if (fi.Extension == ".h5")
                    {
                        modelsTrainers.Add(new ModelTrainer(fi.FullName, null));
                        modelsBox.Items.Add(fi.Name);
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

        private async Task<List<Tuple<int, double, BitmapImage>>> sendExplanationRequestForm()
        {

            string sessionsstr = "[\"" + DatabaseHandler.SessionName + "\"]";

            topLablesV = Int32.Parse(topLabels.Text);
            numSamplesV = Int32.Parse(numSamples.Text);
            numFeaturesV = Int32.Parse(numFeatures.Text);

            hideRestV = hideRest.IsChecked.Value.ToString();
            hideColorV = hideColor.IsChecked.Value.ToString();
            positiveOnlyV = positiveOnly.IsChecked.Value.ToString();

            JObject ob = new JObject
            {
              {"id", "explanation_stream"},
              {"type", "input" },
              {"src", "db:stream" },
              {"name", videoname},
              {"role", role},
              {"active", "True" }
            };
            JArray data = new JArray();
            data.Add(ob);
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
                { new StringContent("LIME_IMAGE"), "explainer"},
                { new StringContent(JsonConvert.SerializeObject(this.frame)), "frame" },
                { new StringContent(numFeaturesV.ToString()), "numFeatures" },
                { new StringContent(topLablesV.ToString()), "topLabels" },
                { new StringContent(numSamplesV.ToString()), "numSamples" },
                { new StringContent(hideRestV.ToString()), "hideRest" },
                { new StringContent(hideColorV.ToString()), "hideColor" },
                { new StringContent(positiveOnlyV.ToString()), "positiveOnly" }
            };

            try
            {
                string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
                string url = "http://" + tokens[0] + ":" + tokens[1] + "/explain";
                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();

                var explanationDic = (JObject)JsonConvert.DeserializeObject(responseString);

                //var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, float>>(responseString);

                if (explanationDic["success"].ToString() == "failed")
                {
                    return null;
                }

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

                return explanationData;
            }
            catch (Exception e)
            {
                return null;
            }


        }

        private async void getExplanation(object sender, RoutedEventArgs e)
        {
            containerImageToBeExplained.Visibility = Visibility.Visible;
            explainingLabel.Visibility = Visibility.Visible;
            BlurEffect blur = new BlurEffect();
            blur.Radius = 20;
            explanationImage.Effect = blur;
            explanationButton.IsEnabled = false;

            containerExplainedImages.Children.Clear();

            List<Tuple<int, double, BitmapImage>> explanationData = await sendExplanationRequestForm();

            if(explanationData == null)
            {

                this.explainingLabel.Visibility = Visibility.Hidden;
                blur = new BlurEffect();
                blur.Radius = 0;
                this.explanationImage.Effect = blur;
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
            this.explainingLabel.Visibility = Visibility.Hidden;
            blur = new BlurEffect();
            blur.Radius = 0;
            this.explanationImage.Effect = blur;
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

        static int GetDeterministicHashCode(string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                        break;
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        private string getIdHash()
        {

            int result = GetDeterministicHashCode("database" + DatabaseHandler.DatabaseName + "session" + DatabaseHandler.SessionInfo + "username" + Properties.Settings.Default.MongoDBUser + "explainer" + "lime tabular" + "model" + modelPath);

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

            public ModelTrainer(string model, string trainer)
            {
                this.model = model;
                this.trainer = trainer;
            }
        }

    }

}
