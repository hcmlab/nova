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
using Python.Runtime;
using System.Windows.Controls;
using System.Xml;

namespace ssi.Controls.Other
{
    /// <summary>
    /// Interaction logic for ExplanationWindow.xaml
    /// </summary>
    public partial class ExplanationWindow : Window
    {

        public string modelPath;
        private readonly BackgroundWorker worker;
        public byte[] img;
        public bool getNewExplanation;
        public BitmapImage explainedImg;
        public int topLablesV;
        public int numSamplesV;
        public int numFeaturesV;
        public bool hideRestV;
        public bool hideColorV;
        public bool positiveOnlyV;
        private List<ModelTrainer> modelsTrainers;
        public Dictionary<int, string> idToClassName;
        private string basePath;

        private IntPtr lk;
        private static Action EmptyDelegate = delegate () { };

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

            img = Screenshot.GetScreenShot(MediaBoxStatic.Selected.Media.GetView(), 1.0, 80);
            topLabels.Text = "2";
            numFeatures.Text = "15";
            numSamples.Text = "800";

            getNewExplanation = false;


            string schemeType = AnnoTier.Selected.AnnoList.Scheme.Type.ToString().ToLower();
            string scheme = AnnoTier.Selected.AnnoList.Scheme.Name;

            string videoname = Path.GetFileNameWithoutExtension(MediaBoxStatic.Selected.Media.GetFilepath()).Split('.')[1];
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

        private void getExplanation(object sender, RoutedEventArgs e)
        {
            containerImageToBeExplained.Visibility = Visibility.Visible;
            explainingLabel.Visibility = Visibility.Visible;
            BlurEffect blur = new BlurEffect();
            blur.Radius = 20;
            explanationImage.Effect = blur;
            topLablesV = Int32.Parse(topLabels.Text);
            numSamplesV = Int32.Parse(numSamples.Text);
            numFeaturesV = Int32.Parse(numFeatures.Text);

            hideRestV = hideRest.IsChecked.Value;
            hideColorV = hideColor.IsChecked.Value;
            positiveOnlyV = positiveOnly.IsChecked.Value;
            getNewExplanation = true;
            explanationButton.IsEnabled = false;

            containerExplainedImages.Children.Clear();

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
