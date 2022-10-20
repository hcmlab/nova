using Microsoft.Shell;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace ssi
{
    public class ExternalClArgs : EventArgs
    {
        public IList<string> Args { get; set; }
    }

    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, ISingleInstanceApp
    {
        public delegate void ClArgsEventHandler(App app, ExternalClArgs e);

        public event ClArgsEventHandler ReceiveExternalClArgs;

        private const string Unique = "A53C6E56-FF5B-47F4-8541-08BB10DDA85E";

        public bool SignalExternalCommandLineArgs(IList<string> args)
        {
            OnReceiveExternalClArgs(args);
            if (MainWindow.WindowState == WindowState.Minimized)
            {
                MainWindow.WindowState = WindowState.Normal;
            }
            MainWindow.Activate();
            return true;
        }

        [STAThread]
        public static void Main()
        {
            var openAnotherInstance = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            if (openAnotherInstance || SingleInstance<App>.InitializeAsFirstInstance(Unique))
            {
                //Decide here the number of parallel downloads by any Webclient
                System.Net.ServicePointManager.DefaultConnectionLimit = 5;
                var application = new App();
                application.OnReceiveExternalClArgs(Environment.GetCommandLineArgs());
                application.InitializeComponent();
                application.Run();
                // Allow single instance code to perform cleanup operations
                SingleInstance<App>.Cleanup();

               
            }
        }

        protected void OnReceiveExternalClArgs(IList<string> clargs)
        {
            var handler = ReceiveExternalClArgs;
            if (handler != null) handler(this, new ExternalClArgs() { Args = clargs });
        }
    }
}