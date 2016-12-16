using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace updater
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {

        void App_Startup(object sender, StartupEventArgs e)
        {
            // Application is running
            // Process command line args
            string version = "";
            if(e.Args.Length > 0)
            version = e.Args[0];
    

            // Create main application window, starting minimized if specified
            MainWindow mainWindow = new MainWindow(version);
            mainWindow.Show();
        }

    }
}
