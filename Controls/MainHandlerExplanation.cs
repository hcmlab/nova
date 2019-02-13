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

            if (Properties.Settings.Default.forcepythonupdate)
            {
                try
                {
                    Directory.Delete(AppDomain.CurrentDomain.BaseDirectory + "\\python\\", true);
                }
                catch { }
               
            }

            var pythonPath = AppDomain.CurrentDomain.BaseDirectory + "python";
            var pythonScriptsPath = AppDomain.CurrentDomain.BaseDirectory + "PythonScripts";
          
            if (Directory.Exists(pythonPath) && Properties.Settings.Default.EnablePython == true)
            {
                var pp = Environment.GetEnvironmentVariable("PYTHONPATH", EnvironmentVariableTarget.User);
                var path = $"{pythonPath};{Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine)}";
                Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);


                PythonEngine.PythonPath += ";" + pythonScriptsPath;
                PythonEngine.PythonHome += pythonPath;
                PythonEngine.PythonHome += AppDomain.CurrentDomain.BaseDirectory;



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

                if(!Properties.Settings.Default.forcepythonupdate && Properties.Settings.Default.EnablePython == true)
                {
                    MessageBoxResult res = MessageBox.Show("NOVA's new XAI Features require an embedded Python Version, do you want to download the dependencies now? This will take some minutes..", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (res == MessageBoxResult.Yes)
                    {
                        GetPython();
                        startExplainableThread();
                        //recursive magic.
                    }
                }
                else if(Properties.Settings.Default.forcepythonupdate && Properties.Settings.Default.EnablePython == true )
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
                try
                {
                    dynamic limeExplainer = Py.Import("ImageExplainerLime");
                    while (!explanationWorker.CancellationPending)
                    {
                        if (window != null && window.modelPath != null && window.getNewExplanation)
                        {
                            dynamic model = limeExplainer.loadModel(window.modelPath);

                            BackgroundWorker progress = (BackgroundWorker)sender;
                            var data = limeExplainer.explain_multiple(model, window.img, window.topLablesV, window.numSamplesV, window.numFeaturesV, window.hideRestV, window.hideColorV, window.positiveOnlyV);
                            int length = data[1];

                            List<Tuple<int, double, BitmapImage>> explanationData = new List<Tuple<int, double, BitmapImage>>();

                            for(int i = 0; i < length; i++)
                            {
                                int classID = data[0][i][0];
                                double acc = data[0][i][1];

                                BitmapImage temp = new BitmapImage();
                                temp.BeginInit();
                                temp.StreamSource = new System.IO.MemoryStream((byte[])data[0][i][2]);
                                temp.EndInit();
                                temp.Freeze();

                                Tuple<int, double, BitmapImage> tuple = new Tuple<int, double, BitmapImage> (classID, acc, temp);

                                explanationData.Add(tuple);
                            }

                            //BitmapImage final_img = new BitmapImage();
                            //final_img.BeginInit();
                            //final_img.StreamSource = new System.IO.MemoryStream((byte[])expImg);
                            //final_img.EndInit();
                            //final_img.Freeze();
                            //window.explainedImg = final_img;
                            window.getNewExplanation = false;
                            progress.ReportProgress(0, explanationData);

                        }
                    }
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Python installation not found or not complete\nError: " + ex);
                }
               
            }
        }

        private void worker_OnProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            List<Tuple<int, double, BitmapImage>> data = (List<Tuple<int, double, BitmapImage>>) e.UserState;

            for(int i = 0; i < data.Count; i++)
            {

                System.Windows.Controls.StackPanel wrapper = new System.Windows.Controls.StackPanel();

                System.Windows.Controls.Label info = new System.Windows.Controls.Label
                {
                    Content = "Class: " + data[i].Item1 + " Score: " + data[i].Item2.ToString("0.###")
                };

                System.Windows.Controls.Image img = new System.Windows.Controls.Image
                {
                    Source = data[i].Item3,
                    //Stretch = System.Windows.Media.Stretch.Fill
                };
                //img.Margin = new Thickness(0,5,0,0);

                //img.Height = (window.containerExplainedImages.ActualHeight - data.Count * 2 * 5) / data.Count;
                //img.Width = (window.containerExplainedImages.ActualWidth - data.Count * 2 * 5) / data.Count;

                int ratio = getRatio(data.Count);

                img.Height = (window.containerExplainedImages.ActualHeight - data.Count * 2 * 5) / ratio;
                img.Width = (window.containerExplainedImages.ActualWidth - data.Count * 2 * 5) / ratio;


                wrapper.Margin = new Thickness(5);
                wrapper.Children.Add(info);
                wrapper.Children.Add(img);

                window.containerExplainedImages.Children.Add(wrapper);
                //window.containerExplainedImages.VerticalAlignment = VerticalAlignment.Center;
            }

            //window.explanationImage.Source = data[0].Item3;
            window.containerImageToBeExplained.Visibility = Visibility.Hidden;
            window.explainingLabel.Visibility = Visibility.Hidden;
            BlurEffect blur = new BlurEffect();
            blur.Radius = 0;
            window.containerImageToBeExplained.Effect = blur;
            window.explanationButton.IsEnabled = true;
            window.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
        }

        private int getRatio(int n)
        {

            int m = 1;

            while(true)
            {
                if(Math.Pow((m-1), 2) < n && n <= Math.Pow(m, 2))
                {
                    return m;
                }
                m++;
                if(m > 1000)
                {
                    return -1;
                }
            }

        }


    }
}

