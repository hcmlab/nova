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

namespace ssi.Controls.Other
{
    /// <summary>
    /// Interaction logic for ExplanationWindow.xaml
    /// </summary>
    public partial class ExplanationWindow : Window
    {

        private string modelPath;
        private readonly BackgroundWorker worker;
        private byte[] img;
        private BitmapImage explainedImg;

        public ExplanationWindow()
        {
            InitializeComponent();
            explanationButton.Click += getExplanation;
            this.modelPath = null;
            explainingLabel.Visibility = Visibility.Hidden;
            worker = new BackgroundWorker();
            worker.DoWork += worker_GetExplanation;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            img = Screenshot.GetScreenShot(MediaBoxStatic.Selected.Media.GetView(), 1.0, 80);
        }

        private void getExplanation(object sender, RoutedEventArgs e)
        {
            explainingLabel.Visibility = Visibility.Visible;
            BlurEffect blur = new BlurEffect();
            blur.Radius = 20;
            explanationImage.Effect = blur;

            modelLoaded.Text = "wait what";
            worker.RunWorkerAsync();
        }

        private void worker_GetExplanation(object sender, DoWorkEventArgs e)
        {
            
            if (modelPath != null && img != null)
            {
                InvokePython.initPython();
                explainedImg = InvokePython.imageExplainer(modelPath, img);
            }
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            explanationImage.Source.Dispatcher.Invoke(()=>explanationImage.Source = explainedImg);
            explainingLabel.Visibility = Visibility.Hidden;
            BlurEffect blur = new BlurEffect();
            blur.Radius = 0;
            explanationImage.Effect = blur;
        }

        private void modelPanel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                modelPath = files[0];
                modelLoaded.Text = Path.GetFileName(modelPath);
            }
        }
    }
}
