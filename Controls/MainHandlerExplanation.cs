using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using NovA;
using Python.Runtime;


namespace ssi
{
    public partial class MainHandler
    {

        private void getExplanation_Click(object sender, RoutedEventArgs e)
        {
            getLimeExplanation();
        }

        private void getLimeExplanation()
        {
            
            byte[] img = Screenshot.GetScreenShot(MediaBoxStatic.Selected.Media.GetView(), 1.0, 80);

            //TODO change path variable to relative 
            var pythonPath = @"C:\\Program Files\\Python36";
            var path = $"{pythonPath};{Environment.GetEnvironmentVariable("Path", EnvironmentVariableTarget.Machine)}";
            Environment.SetEnvironmentVariable("Path", path, EnvironmentVariableTarget.Process);
            PythonEngine.PythonHome += pythonPath;

            //TODO add scripts path in front of everything else
            PythonEngine.PythonPath += ";Scripts";
            PythonEngine.Initialize();

            InvokePython.imageExplainer("", img);
        }

    }
}

