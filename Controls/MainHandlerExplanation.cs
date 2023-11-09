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
        CounterFactualWindow windowCounterFactual;
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

                int frame = FileTools.FormatFramesInteger(Time.CurrentPlayPosition, MediaBoxStatic.Selected.Media.GetSampleRate());
                window.frame = frame;

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

            windowFeatureExplanations = new FeatureExplanationWindow(SignalTrackStatic.Selected.Signal.FilePath);


            try
            {

                int frame = FileTools.FormatFramesInteger(Time.CurrentPlayPosition, SignalTrackStatic.Selected.Signal.rate);

                windowFeatureExplanations.featurestream = SignalTrackStatic.Selected.Signal.data;
                windowFeatureExplanations.dim = SignalTrackStatic.Selected.Signal.dim;
                windowFeatureExplanations.frame = frame;

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
                int frame = FileTools.FormatFramesInteger(Time.CurrentPlayPosition, MediaBoxStatic.Selected.Media.GetSampleRate());
                windowfexplain.frame = frame;

                windowfexplain.Show();
            }
            catch
            {

            }
        }

        private void counterFactualWindow_Click(object sender, RoutedEventArgs e)
        {
            if (windowCounterFactual != null)
            {
                windowCounterFactual.deactiveExplainationButton();
            }

            if (AnnoTier.Selected == null)
            {
                MessageBox.Show("Select annotation track first");
                return;
            }

            windowCounterFactual = new CounterFactualWindow(SignalTrackStatic.Selected.Signal.FilePath, this);


            try
            {

                int frame = FileTools.FormatFramesInteger(Time.CurrentPlayPosition, SignalTrackStatic.Selected.Signal.rate);

                windowCounterFactual.featurestream = SignalTrackStatic.Selected.Signal.data;
                windowCounterFactual.dim = SignalTrackStatic.Selected.Signal.dim;
                windowCounterFactual.frame = frame;

                windowCounterFactual.Show();
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

