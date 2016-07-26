using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace ssi
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public ViewHandler viewh = null;

        public Window1()
        {
            InitializeComponent();

            this.view.OnHandlerLoaded += viewHandlerLoaded;
            CultureInfo ci = new CultureInfo("en-GB");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
        }

        private void viewHandlerLoaded(ViewHandler handler)
        {
            this.viewh = handler;
            this.viewh.LoadButton.Click += loadButton_Click;
            this.KeyDown += OnKeyDown;
            this.PreviewKeyDown += handler.OnPreviewKeyDown;
            this.KeyDown += handler.OnKeyDown;
            this.KeyUp += handler.OnKeyUp;

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
                if (this.WindowStyle != WindowStyle.None)
                {
                    this.WindowStyle = WindowStyle.None;
                    this.WindowState = WindowState.Normal;
                    this.WindowState = WindowState.Maximized;
                }
            }
            else if (e.KeyboardDevice.IsKeyDown(Key.Escape))
            {
                if (this.WindowStyle != WindowStyle.SingleBorderWindow)
                {
                    this.WindowStyle = WindowStyle.SingleBorderWindow;
                    this.WindowState = WindowState.Normal;
                }
            }
        }

        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            this.viewh.addFiles();
        }

        private void HandleClArgs(IList<string> args)
        {
            if (args.Count <= 1) return;
            for (var i = 1; i < args.Count; i++)
            {
                try
                {
                    this.viewh.LoadFiles(new[] { args[i] });
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }

        private void view_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}