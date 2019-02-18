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

namespace ssi.Controls.Other
{
    /// <summary>
    /// Interaction logic for ExplanationWindowInnvestigate.xaml
    /// </summary>
    public partial class ExplanationWindowInnvestigate : Window
    {

        public bool getNewExplanation;
        public string modelPath;
        public byte[] img;
        private List<ModelTrainer> modelsTrainers;
        public Dictionary<int, string> idToClassName;
        public string postprocess;
        public string explainAlgorithm;


        public ExplanationWindowInnvestigate()
        {
            InitializeComponent();
            explanationButton.Click += getExplanation;
            modelPath = Properties.Settings.Default.explainModelPath;

            if (modelPath != null)
            {
                modelLoaded.Text = Path.GetFileName(modelPath);
            }
            explainingLabel.Visibility = Visibility.Hidden;

            img = Screenshot.GetScreenShot(MediaBoxStatic.Selected.Media.GetView(), 1.0, 80);

            string schemeType = AnnoTier.Selected.AnnoList.Scheme.Type.ToString().ToLower();
            string scheme = AnnoTier.Selected.AnnoList.Scheme.Name;

            string basePath = Properties.Settings.Default.CMLDirectory + "\\models\\trainer\\" + schemeType + "\\" + scheme + "\\" + "video" + "{video}";

            DirectoryInfo di = new DirectoryInfo(basePath);

            modelsTrainers = new List<ModelTrainer>();

            idToClassName = new Dictionary<int, string>();
            loadModelAndTrainer(basePath);

        }

        private void loadModelAndTrainer(string path)
        {
            DirectoryInfo di = new DirectoryInfo(path);

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

        private void getExplanation(object sender, RoutedEventArgs e)
        {
            containerImageToBeExplained.Visibility = Visibility.Visible;
            explainingLabel.Visibility = Visibility.Visible;
            BlurEffect blur = new BlurEffect();
            blur.Radius = 20;
            explanationImage.Effect = blur;

            postprocess = postprocessing.Text.ToUpper();
            explainAlgorithm = explainer.Text.ToUpper();

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

        private void parseTrainerFile(string path)
        {
            XmlDocument trainer = new XmlDocument();
            trainer.Load(path);
            XmlNodeList classes = trainer.GetElementsByTagName("classes")[0].ChildNodes;

            for (int i = 0; i < classes.Count; i++)
            {
                idToClassName.Add(i, classes.Item(i).Attributes["name"].Value);
            }

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
