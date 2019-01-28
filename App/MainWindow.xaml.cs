using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace ssi
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainHandler viewh = null;

        public MainWindow()
        {
            InitializeComponent();

            view.OnHandlerLoaded += viewHandlerLoaded;
            CultureInfo ci = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Title = "NOVA | v" + MainHandler.BuildVersion + " | HCM-Lab, Augsburg University | http://openssi.net";
        }

        private void viewHandlerLoaded(MainHandler handler)
        {
            viewh = handler;

            KeyDown += OnKeyDown;
            PreviewKeyDown += handler.OnPreviewKeyDown;
            KeyDown += handler.OnKeyDown;
            KeyUp += handler.OnKeyUp;

            HandleClArgs(Environment.GetCommandLineArgs());

            ((App)Application.Current).ReceiveExternalClArgs += (app, clArgs) =>
            {
                HandleClArgs(clArgs.Args);
            };
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.IsKeyDown(Key.LeftAlt) && e.KeyboardDevice.IsKeyDown(Key.Enter))
            {
                if (WindowStyle != WindowStyle.None)
                {
                    WindowStyle = WindowStyle.None;
                    WindowState = WindowState.Normal;
                    WindowState = WindowState.Maximized;
                }
            }
            else if (e.KeyboardDevice.IsKeyDown(Key.Escape))
            {
                if (WindowStyle != WindowStyle.SingleBorderWindow)
                {
                    WindowStyle = WindowStyle.SingleBorderWindow;
                    WindowState = WindowState.Normal;
                }
            }
        }

        private void HandleClArgs(IList<string> args)
        {
            if (args.Count <= 1) return;
            //for (var i = 1; i < args.Count; i++)
            //{
            //    try
            //    {

            //        string filerooted;
            //        if (!Path.IsPathRooted(args[i]))
            //        {
            //            filerooted = Directory.GetCurrentDirectory() + "\\" + args[i];
            //        }
            //        else
            //        {
            //            filerooted = args[i];
            //        }
            //        viewh.loadMultipleFilesOrDirectory(new[] { filerooted });
            //    }
            //    catch (Exception)
            //    {
            //        // ignored
            //    }
            //}

            string path = args[1];
            for (var i = 2; i < args.Count; i++)
            {
                path += " " + args[i];
            }
            if (!Path.IsPathRooted(path))
            {
                path = Directory.GetCurrentDirectory() + "\\" + path;
            }
            viewh.loadFile(path);

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {            
            if (!viewh.clearWorkspace())
            {
                e.Cancel = true;
            }
            
        }
    }
}