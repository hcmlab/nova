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

            //var pythonPath = @"C:\\Program Files\\Python36";
            //var path = $"{pythonPath};{Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine)}";
            //Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);
            //PythonEngine.PythonHome += pythonPath;

            ////TODO change path variable to relative 
            ////TODO add scripts path in front of everything else
            //PythonEngine.PythonPath += ";PythonScripts";
            //PythonEngine.Initialize();
            //lk = PythonEngine.BeginAllowThreads();

            InitializeComponent();
            explanationButton.Click += getExplanation;
            modelPath = Properties.Settings.Default.explainModelPath;
            if(modelPath != null)
            {
                modelLoaded.Text = Path.GetFileName(modelPath);
            }
            explainingLabel.Visibility = Visibility.Hidden;
            //worker = new BackgroundWorker
            //{
            //    WorkerReportsProgress = true,
            //    WorkerSupportsCancellation = true
            //};
            //worker.DoWork += worker_GetExplanation;
            //worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            //worker.ProgressChanged += worker_OnProgressChanged;
            img = Screenshot.GetScreenShot(MediaBoxStatic.Selected.Media.GetView(), 1.0, 80);
            topLabels.Text = "2";
            numFeatures.Text = "1000";
            numSamples.Text = "1000";
            //InvokePython.initPython();
            //lk = InvokePython.allowThreads();
            getNewExplanation = false;
            //worker.RunWorkerAsync();
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
            //TODO fix for scaled explanation img
            hideRestV = hideRest.IsChecked.Value;
            hideColorV = hideColor.IsChecked.Value;
            positiveOnlyV = positiveOnly.IsChecked.Value;
            getNewExplanation = true;
            explanationButton.IsEnabled = false;
            //lk = InvokePython.allowThreads();

            //explainedImg = InvokePython.imageExplainerLime(modelPath, img, topLablesV, numSamplesV, numFeaturesV, hideRestV, hideColorV, positiveOnlyV);
            //explanationImage.Source = explainedImg;
            //worker.RunWorkerAsync();

        }

        private void worker_GetExplanation(object sender, DoWorkEventArgs e)
        {
            using (Py.GIL())
            {
                while (!worker.CancellationPending)
                {
                    if(modelPath != null)
                    {
                        dynamic limeExplainer = Py.Import("ImageExplainerLime");
                        dynamic model = limeExplainer.loadModel(modelPath);

                        if (img != null && getNewExplanation)
                        {
                            BackgroundWorker progress = (BackgroundWorker)sender;
                            var expImg = limeExplainer.explain(model, img, topLablesV, numSamplesV, numFeaturesV, hideRestV, hideColorV, positiveOnlyV);
                            BitmapImage final_img = new BitmapImage();
                            final_img.BeginInit();
                            final_img.StreamSource = new System.IO.MemoryStream((byte[])expImg);
                            final_img.EndInit();
                            final_img.Freeze();
                            explainedImg = final_img;
                            getNewExplanation = false;
                            progress.ReportProgress(0, final_img);
                        }
                    }
                }
            }
        }

        private void worker_OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            explainedImg = (BitmapImage)e.UserState;
            //explanationImage.Source.Dispatcher.Invoke(()=>explanationImage.Source = explainedImg);
            explanationImage.Source = explainedImg;
            explainingLabel.Visibility = Visibility.Hidden;
            BlurEffect blur = new BlurEffect();
            blur.Radius = 0;
            explanationImage.Effect = blur;
            this.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }

            private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

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
            //PythonEngine.EndAllowThreads(lk);
            //PythonEngine.Shutdown();
        }

    }
}
