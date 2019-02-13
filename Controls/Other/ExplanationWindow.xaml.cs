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
        private List<string> models;
        private List<string> trainers;
        public Dictionary<int, string> idToClassName;

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
            numFeatures.Text = "100";
            numSamples.Text = "100";

            getNewExplanation = false;


            string schemeType = AnnoTier.Selected.AnnoList.Scheme.Type.ToString().ToLower();
            string scheme = AnnoTier.Selected.AnnoList.Scheme.Name;

            string basePath = Properties.Settings.Default.CMLDirectory + "\\models\\trainer\\"+ schemeType + "\\" + scheme + "\\" + "video" + "{video}";

            DirectoryInfo di = new DirectoryInfo(basePath);

            models = new List<string>();
            trainers = new List<string>();

            foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                if(fi.Extension == ".h5")
                {
                    models.Add(fi.FullName);
                    modelsBox.Items.Add(fi.Name);
                }

                if(fi.Extension == ".trainer")
                {
                    trainers.Add(fi.FullName);
                }
            }

            idToClassName = new Dictionary<int, string>();

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
            }
        }

        private void modelsBox_selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cmb = sender as ComboBox;
            modelPath = models[cmb.SelectedIndex];
            modelLoaded.Text = Path.GetFileName(modelPath);
            Properties.Settings.Default.explainModelPath = modelPath;
            Properties.Settings.Default.Save();

            parseTrainerFile(trainers[cmb.SelectedIndex]);
        }

        private void parseTrainerFile(string path)
        {
            XmlDocument trainer = new XmlDocument();
            trainer.Load(path);
            XmlNodeList classes = trainer.GetElementsByTagName("classes")[0].ChildNodes;

            for(int i = 0; i < classes.Count; i++)
            {
                idToClassName.Add(i, classes.Item(i).Attributes["name"].Value);
            }

        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {

        }

    }
}
