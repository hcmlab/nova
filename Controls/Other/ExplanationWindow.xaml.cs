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
            numFeatures.Text = "1000";
            numSamples.Text = "1000";

            getNewExplanation = false;

        }

        private void getExplanation(object sender, RoutedEventArgs e)
        {
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
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {

        }

    }
}
