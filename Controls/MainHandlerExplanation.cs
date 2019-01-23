using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NovA;
using Python.Runtime;
using ssi.Controls.Other;

namespace ssi
{
    public partial class MainHandler
    {

        private BackgroundWorker explanationWorker;
        ExplanationWindow window;
        private static Action EmptyDelegate = delegate () { };

        private void explanationWindow_Click(object sender, RoutedEventArgs e)
        {
            window = new ExplanationWindow();

            try
            {
                byte[] img = Screenshot.GetScreenShot(MediaBoxStatic.Selected.Media.GetView(), 1.0, 80);

                BitmapImage imageBitmap = new BitmapImage();
                imageBitmap.BeginInit();
                imageBitmap.StreamSource = new System.IO.MemoryStream(img);
                imageBitmap.EndInit();
                window.explanationImage.Source = imageBitmap;

                window.ShowDialog();
            }
            catch
            {

            }

        }

        public void startExplainableThread()
        {
            var pythonPath = @"C:\\Program Files\\Python36";
            var path = $"{pythonPath};{Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine)}";
            Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);
            PythonEngine.PythonHome += pythonPath;

            //TODO change path variable to relative 
            //TODO add scripts path in front of everything else
            PythonEngine.PythonPath += ";PythonScripts";
            PythonEngine.Initialize();
            PythonEngine.BeginAllowThreads();
            explanationWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true,
                WorkerSupportsCancellation = true
            };
            explanationWorker.DoWork += worker_GetExplanation;
            explanationWorker.ProgressChanged += worker_OnProgressChanged;
            explanationWorker.RunWorkerAsync();
        }

        private void worker_GetExplanation(object sender, DoWorkEventArgs e)
        {
            using (Py.GIL())
            {
                while (!explanationWorker.CancellationPending)
                {
                    if (window != null && window.modelPath != null)
                    {
                        dynamic limeExplainer = Py.Import("ImageExplainerLime");
                        dynamic model = limeExplainer.loadModel(window.modelPath);

                        if (window.img != null && window.getNewExplanation)
                        {
                            BackgroundWorker progress = (BackgroundWorker)sender;
                            var expImg = limeExplainer.explain_deprecated(model, window.img, window.topLablesV, window.numSamplesV, window.numFeaturesV, window.hideRestV, window.hideColorV, window.positiveOnlyV);
                            BitmapImage final_img = new BitmapImage();
                            final_img.BeginInit();
                            final_img.StreamSource = new System.IO.MemoryStream((byte[])expImg);
                            final_img.EndInit();
                            final_img.Freeze();
                            window.explainedImg = final_img;
                            window.getNewExplanation = false;
                            progress.ReportProgress(0, final_img);
                        }
                    }
                }
            }
        }

        private void worker_OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //explanationImage.Source.Dispatcher.Invoke(()=>explanationImage.Source = explainedImg);
            window.explanationImage.Source = (BitmapImage)e.UserState;
            window.explainingLabel.Visibility = Visibility.Hidden;
            BlurEffect blur = new BlurEffect();
            blur.Radius = 0;
            window.explanationImage.Effect = blur;
            window.explanationButton.IsEnabled = true;
            window.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }


    }
}

