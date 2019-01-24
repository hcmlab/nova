using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
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

        public static BackgroundWorker explanationWorker;
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

            if(Properties.Settings.Default.forcepythonupdate)
            {
                try
                {
                    Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "\\python\\", true);
                }
                catch { }
               
            }

            var pythonPath = AppDomain.CurrentDomain.BaseDirectory + "python";
            var pythonScriptsPath = AppDomain.CurrentDomain.BaseDirectory + "PythonScripts";
          
            if (Directory.Exists(pythonPath))
            {

                var path = $"{pythonPath};{Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine)}";
                Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.User);
                PythonEngine.PythonHome += pythonPath;
             
                PythonEngine.PythonPath += ";" + pythonScriptsPath;
    
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

            else
            {

                if(!Properties.Settings.Default.forcepythonupdate)
                {
                    MessageBoxResult res = MessageBox.Show("NOVA's new XAI Features require an embedded Python Version, do you want to download the dependencies now?", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (res == MessageBoxResult.Yes)
                    {
                        GetPython();
                        startExplainableThread();
                        //recursive magic.
                    }
                }
                else
                {
                    Properties.Settings.Default.forcepythonupdate = false;
                    Properties.Settings.Default.Save();
                    GetPython();
                    startExplainableThread();
                }
               
                
            }
        }

        private void worker_GetExplanation(object sender, DoWorkEventArgs e)
        {
            using (Py.GIL())
            {
                dynamic limeExplainer = Py.Import("ImageExplainerLime");
                while (!explanationWorker.CancellationPending)
                {
                    if (window != null && window.modelPath != null && window.getNewExplanation)
                    {                      
                        dynamic model = limeExplainer.loadModel(window.modelPath);

                        BackgroundWorker progress = (BackgroundWorker)sender;
                        var expImg = limeExplainer.explain_raw(model, window.img, window.topLablesV, window.numSamplesV, window.numFeaturesV, window.hideRestV, window.hideColorV, window.positiveOnlyV);
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

        private void worker_OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {
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

