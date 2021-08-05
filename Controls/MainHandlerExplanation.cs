using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using NovA;
using ssi.Controls.Other;
       


namespace ssi
{
    public partial class MainHandler
    {

        [DllImport("user32.dll")]
        static extern int ShowWindow(int hwnd, int nCmdShow);


        const int SW_HIDE = 0;
        const int SW_SHOW = 5;

        public static BackgroundWorker explanationWorker;
        ExplanationWindow window;
        ExplanationWindowInnvestigate windowInnvestigate;
        ExplanationWindowTfExplain windowfexplain;
        FeatureExplanationWindow windowFeatureExplanations;
        private static Action EmptyDelegate = delegate () { };

        private void explanationWindow_Click(object sender, RoutedEventArgs e)
        {
            if (window != null)
            {
                window.deactiveExplainationButton();
            }

            if (AnnoTier.Selected == null)
            {
                MessageBox.Show("Select annotation track first");
                return;
            }


            window = new ExplanationWindow();


            try
            {

                byte[] img = Screenshot.GetScreenShot(MediaBoxStatic.Selected.Media.GetView(), 1.0, 80);

                BitmapImage imageBitmap = new BitmapImage();
                imageBitmap.BeginInit();
                imageBitmap.StreamSource = new System.IO.MemoryStream(img);
                imageBitmap.EndInit();
                window.explanationImage.Source = imageBitmap;




                //int frame = FileTools.FormatFramesInteger(Time.CurrentPlayPosition, SignalTrackStatic.Selected.Signal.rate);
                //List<float> sample = new List<float>();
                //for(int d= 0; d< SignalTrackStatic.Selected.Signal.dim; d++ )
                //{
                //    sample.Add(SignalTrackStatic.Selected.Signal.data[frame * SignalTrackStatic.Selected.Signal.dim + d]);
                //}

                //window.featurestream = SignalTrackStatic.Selected.Signal.data;
                //window.sample = sample;



                window.Show();
            }
            catch
            {

            }



        }

        private void featureExplanationWindow_Click(object sender, RoutedEventArgs e)
        {
            if (windowFeatureExplanations != null)
            {
                windowFeatureExplanations.deactiveExplainationButton();
            }

            if (AnnoTier.Selected == null)
            {
                MessageBox.Show("Select annotation track first");
                return;
            }

            string test = SignalTrackStatic.Selected.Signal.FilePath;

            windowFeatureExplanations = new FeatureExplanationWindow(SignalTrackStatic.Selected.Signal.FilePath);


            try
            {

                int frame = FileTools.FormatFramesInteger(Time.CurrentPlayPosition, SignalTrackStatic.Selected.Signal.rate);
                List<float> sample = new List<float>();
                for (int d = 0; d < SignalTrackStatic.Selected.Signal.dim; d++)
                {
                    sample.Add(SignalTrackStatic.Selected.Signal.data[frame * SignalTrackStatic.Selected.Signal.dim + d]);
                }

                windowFeatureExplanations.featurestream = SignalTrackStatic.Selected.Signal.data;
                windowFeatureExplanations.sample = sample;
                windowFeatureExplanations.dim = SignalTrackStatic.Selected.Signal.dim;



                windowFeatureExplanations.Show();
            }
            catch
            {

            }


        }


        private void explanationWindowInnvestigate_Click(object sender, RoutedEventArgs e)
        {
            if(windowInnvestigate != null)
            {
                windowInnvestigate.deactiveExplainationButton();
            }

            if(AnnoTier.Selected == null)
            {
                MessageBox.Show("Select annotation track first");
                return;
            }

            windowInnvestigate = new ExplanationWindowInnvestigate();

            try
            {
                byte[] img = Screenshot.GetScreenShot(MediaBoxStatic.Selected.Media.GetView(), 1.0, 80);

                BitmapImage imageBitmap = new BitmapImage();
                imageBitmap.BeginInit();
                imageBitmap.StreamSource = new System.IO.MemoryStream(img);
                imageBitmap.EndInit();
                windowInnvestigate.explanationImage.Source = imageBitmap;

                windowInnvestigate.Show();
            }
            catch
            {

            }
        }


        private void explanationWindowtfexplain_Click(object sender, RoutedEventArgs e)
        {
            if (windowfexplain != null)
            {
                windowfexplain.deactiveExplainationButton();
            }

            if (AnnoTier.Selected == null)
            {
                MessageBox.Show("Select annotation track first");
                return;
            }

            windowfexplain = new ExplanationWindowTfExplain();

            try
            {
                byte[] img = Screenshot.GetScreenShot(MediaBoxStatic.Selected.Media.GetView(), 1.0, 80);

                BitmapImage imageBitmap = new BitmapImage();
                imageBitmap.BeginInit();
                imageBitmap.StreamSource = new System.IO.MemoryStream(img);
                imageBitmap.EndInit();
                windowfexplain.explanationImage.Source = imageBitmap;

                windowfexplain.Show();
            }
            catch
            {

            }
        }




        public void checkPythonInstallation()
        {


            if (Properties.Settings.Default.EnablePython)
            {

                if (Properties.Settings.Default.forcepythonupdate)
                {
                    MessageBoxResult res = MessageBox.Show("NOVA's new XAI Features require an embedded Python Version, do you want to download the dependencies now? This will take some minutes..", "Attention", MessageBoxButton.YesNo, MessageBoxImage.Information);
                    if (res == MessageBoxResult.Yes)
                    {
                        GetPython();

                        //recursive magic.
                    }
                    else
                    {
                         Properties.Settings.Default.forcepythonupdate = false;
                         Properties.Settings.Default.Save();
                    }
                }



            }
        }

        public static string showExplanationBackend(bool show)
        {
            int nCMD = SW_HIDE;
            if (show) nCMD = SW_SHOW;

            Process[] processes = Process.GetProcessesByName("Python");
            foreach (Process p in processes)
            {

                p.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                int p1 = p.MainWindowHandle.ToInt32();
                ShowWindow(p1, nCMD);
                return "success";
            }
            return "failed";
        }
      
    

        public static int startPythonBackend(bool forcedebug = false)
        {

            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            try
            {
         

                startInfo.WindowStyle = (Properties.Settings.Default.EnablePythonDebug == true) ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden;
                //startInfo.WindowStyle = (forcedebug == true ) ? ProcessWindowStyle.Minimized : startInfo.WindowStyle;
                //startInfo.RedirectStandardOutput = true;
                //startInfo.RedirectStandardError = true;
                //if(!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "ssi\\nova_python.exe")){
                //    File.Copy(AppDomain.CurrentDomain.BaseDirectory + "ssi\\python.exe", AppDomain.CurrentDomain.BaseDirectory + "ssi\\nova_python.exe");
                //}


                  
                startInfo.FileName = "\"" + AppDomain.CurrentDomain.BaseDirectory + "ssi\\python.exe" + "\"";
                startInfo.Arguments = "\"" + AppDomain.CurrentDomain.BaseDirectory + "PythonScripts\\explanation_backend.py" + "\"";
               
                process.StartInfo = startInfo;

                process.Start();
                
        
                //string output = process.StandardOutput.ReadToEnd();
                //Console.WriteLine(output);
              

                //string output = process.StandardOutput.ReadToEnd();
                //MessageBox.Show(output);
                //Console.WriteLine(output);
                //string err = process.StandardError.ReadToEnd();
                //Console.WriteLine(err);
                //process.WaitForExit();

                //ShowWindow(process.MainWindowHandle, 5);

                return process.Id;
            }

            catch
            {
                return -1;
            }
           
       
        }

        public static void killPythonBackend(bool forcedebug = false )
        {
            try
            {
                Process[] process = Process.GetProcesses();
                foreach (Process prs in process)
                {
                   
                    if (prs.Id == MainHandler.pythonProcessID)
                    {
                        KillProcessAndChildren(prs.Id);
                        break;
                    }
                }

             

            }
            catch
            {

            }
        }

        public static void restartPythonnBackend()
        {
            killPythonBackend();
            MainHandler.pythonProcessID = startPythonBackend();
        }



        public static void KillProcessAndChildren(int pid)
        {
            // Cannot close 'system idle process'.
            if (pid == 0)
            {
                return;
            }
            ManagementObjectSearcher searcher = new ManagementObjectSearcher
                    ("Select * From Win32_Process Where ParentProcessID=" + pid);
            ManagementObjectCollection moc = searcher.Get();
            foreach (ManagementObject mo in moc)
            {
                KillProcessAndChildren(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }

    }
}

