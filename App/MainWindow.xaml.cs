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
            CultureInfo ci = new CultureInfo("en-GB");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Title = "(NOn)Verbal Annotator | v" + MainHandler.BuildVersion + " | HCM-Lab, Augsburg University | http://openssi.net";
           
        }

        private void viewHandlerLoaded(MainHandler handler)
        {
            viewh = handler;
            viewh.LoadButton.Click += loadButton_Click;
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

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            viewh.loadFiles();
        }

        private void HandleClArgs(IList<string> args)
        {
            if (args.Count <= 1) return;
            for (var i = 1; i < args.Count; i++)
            {
                try
                {

                    string filerooted;
                    if (!Path.IsPathRooted(args[i]))
                    {
                        filerooted = Directory.GetCurrentDirectory() + "\\" + args[i];
                    }
                    else
                    {
                        filerooted = args[i];
                    }
                    viewh.loadMultipleFilesOrDirectory(new[] { filerooted });
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            bool anytrackchanged = false;
            foreach (AnnoTier track in viewh.AnnoTiers)
            {
                if (track.AnnoList.HasChanged) anytrackchanged = true;
            }

            QuestionWindow.Input input;
            if (anytrackchanged)
            {
                input = new QuestionWindow.Input() { Question = "Save changes before closing the application?", YesButton = "Save", NoButton = "Discard", CancelButton = "Cancel" };
                QuestionWindow dialog = new QuestionWindow(input);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowDialog();
                if (dialog.DialogResult == true)
                {
                    if (dialog.Result == 0)
                    {
                        viewh.clearSession(true, false);
                    }
                    else if (dialog.Result == 1)
                    {
                        viewh.clearSession(true, true);
                    }
                    else if (dialog.Result == 2)
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    e.Cancel = true;
                }
            }
            else
            {
                input = new QuestionWindow.Input() { Question = "Close the application?", YesButton = "Yes", NoButton = "", CancelButton = "Cancel" };
                QuestionWindow dialog = new QuestionWindow(input);
                dialog.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dialog.ShowDialog();
                if (dialog.DialogResult == true)
                {
                    if (dialog.Result == 1)
                    {
                        viewh.clearSession(true, false);
                    }
                    else if (dialog.Result == 2)
                    {
                        e.Cancel = true;
                    }
                }
                else
                {
                    e.Cancel = true;
                }
            }

            
            
        }
    }
}