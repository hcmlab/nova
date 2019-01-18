using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using NovA;
using Python.Runtime;
using ssi.Controls.Other;

namespace ssi
{
    public partial class MainHandler
    {

        private void explanationWindow_Click(object sender, RoutedEventArgs e)
        {
            ExplanationWindow window = new ExplanationWindow();

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

            //getLimeExplanation();
        }

        //private void getLimeExplanation()
        //{
            
        //    byte[] img = Screenshot.GetScreenShot(MediaBoxStatic.Selected.Media.GetView(), 1.0, 80);

        //    ////TODO change path variable to relative 
        //    //var pythonPath = @"C:\\Program Files\\Python36";
        //    //var path = $"{pythonPath};{Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine)}";
        //    //Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);
        //    //PythonEngine.PythonHome += pythonPath;

        //    ////TODO add scripts path in front of everything else
        //    //PythonEngine.PythonPath += ";../../PythonScripts";
        //    //PythonEngine.Initialize();

        //    InvokePython.initPython();

        //    InvokePython.imageExplainer("", img);
        //}

    }
}

