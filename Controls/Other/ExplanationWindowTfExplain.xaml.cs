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

namespace ssi.Controls.Other
{
    /// <summary>
    /// Interaction logic for ExplanationWindowInnvestigate.xaml
    /// </summary>
    public partial class ExplanationWindowTfExplain : Window
    {

        public bool getNewExplanation;
        public string modelPath;
        public int frame;
        private string videoname;
        private string role;
        public byte[] img;
        private List<ModelTrainer> modelsTrainers;
        public Dictionary<int, string> idToClassName;
        public string postprocess;
        public string explainAlgorithm;
        public string args;
        private string basePath;

        private static readonly HttpClient client = new HttpClient();


        public ExplanationWindowTfExplain()
        {
            InitializeComponent();
            explanationButton.Click += getExplanation;
            modelPath = Properties.Settings.Default.explainModelPath;

            if (modelPath != null)
            {
                modelLoaded.Text = Path.GetFileName(modelPath);
            }
            explainingLabel.Visibility = Visibility.Hidden;

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
            explainer.SelectedIndex = 0;
            lrpalpha.Text = "1.0";
            lrpbeta.Text = "0.0";
            args = "";

        }

        private void loadModelAndTrainer(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);

            if (di.Exists)
            {
                foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    if (fi.Extension == ".h5")
                    {
                        modelsTrainers.Add(new ModelTrainer(fi.FullName, null));
                        modelsBox.Items.Add(fi.Name);
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

        private async Task<Dictionary<string, string>> sendExplanationRequestForm()
        {

            string sessionsstr = "[\"" + DatabaseHandler.SessionName + "\"]";

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
                { new StringContent("TF_EXPLAIN"), "explainer"},
                { new StringContent( explainer.Text.ToUpper()), "tfExplainer"},
                { new StringContent(JsonConvert.SerializeObject(this.frame)), "frame" }
            };

            try
            {
                string[] tokens = Properties.Settings.Default.NovaServerAddress.Split(':');
                string url = "http://" + tokens[0] + ":" + tokens[1] + "/explain";
                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();

                var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                if (explanationDic["success"] == "failed")
                {
                    return null;
                }

                return explanationDic;
            }
            catch (Exception e)
            {
                return null;
            }


        }

        private async Task<Dictionary<string, string>> getExplanationFromBackend()
        {

            try
            {

                var base64 = System.Convert.ToBase64String(this.img);

                var content = new MultipartFormDataContent
                {
                    { new StringContent(modelPath), "model_path" },
                    { new StringContent(base64), "image" }
                };

                //postprocess = postprocessing.Text.ToUpper();
                postprocess = "NONE";
                explainAlgorithm = explainer.Text.ToUpper();
                string url = "http://localhost:5000/tfexplain?postprocess=" + postprocess + "&explainer=" + explainAlgorithm;

                var response = await client.PostAsync(url, content);

                var responseString = await response.Content.ReadAsStringAsync();
                var explanationDic = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);

                if (explanationDic["success"] == "failed")
                {
                    return null;
                }

                return explanationDic;

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

            var explanationDic = await sendExplanationRequestForm();

            if (explanationDic == null)
            {
                this.explainingLabel.Visibility = Visibility.Hidden;
                blur = new BlurEffect();
                blur.Radius = 0;
                this.explanationImage.Effect = blur;
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

            if (this.idToClassName.ContainsKey(Int32.Parse(explanationDic["prediction"])))
            {
                className = this.idToClassName[Int32.Parse(explanationDic["prediction"])];
            }
            else
            {
                className = explanationDic["prediction"];
            }

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
            if (di.Exists)
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
            if (path != null)
            {
                XmlDocument trainer = new XmlDocument();
                trainer.Load(path);
                XmlNodeList classes = trainer.GetElementsByTagName("classes")[0].ChildNodes;

                for (int i = 0; i < classes.Count; i++)
                {
                    idToClassName.Add(i, classes.Item(i).Attributes["name"].Value);
                }
            }

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


        private class ModelTrainer
        {
            public string model { get; set; }
            public string trainer { get; set; }

            public ModelTrainer(string model, string trainer)
            {
                this.model = model;
                this.trainer = trainer;
            }
        }

    }
}
