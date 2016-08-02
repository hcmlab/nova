﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace ssi
{
    public class ViewHandler
    {
        private static ViewTime time = null;

        private enum ssi_file_type
        {
            UNKOWN = 0,
            CSV,
            AUDIO,
            VIDEO,
            ANNO,
            STREAM,
            EVENTS,
            EAF,
            ANVIL,
            VUI
        }

        private static readonly string[] SSI_FILE_TYPE_NAME = { "ssi", "audio", "video", "anno", "stream", "events", "eaf", "anvil", "vui" };

        public static ViewTime Time
        {
            get { return ViewHandler.time; }
        }

        private MediaList media_list = new MediaList();
        public Cursor signalCursor = null;
        public Cursor annoCursor = null;

        private List<ISignalTrack> signal_tracks = new List<ISignalTrack>();
        private List<Signal> signals = new List<Signal>();

        private List<AnnoTrack> anno_tracks = new List<AnnoTrack>();
        private List<AnnoList> annos = new List<AnnoList>();
        private AnnoList current_anno = null;
        private int tiercount = 0;
        private bool infastforward = false;
        private bool infastbackward = false;
        private bool innomediaplaymode = false;
        private DispatcherTimer _timerff = new DispatcherTimer();
        private DispatcherTimer _timerfb = new DispatcherTimer();
        private DispatcherTimer _timerp = new DispatcherTimer();
        private bool mouseDown = false;
        private bool keyDown = false;
        private double skelfps;
        private double lasttimepos = 0;
        private string mongodbip = "127.0.0.1:27017";

        private ViewControl view;
        private String annofilepath = "";

        public MenuItem LoadButton
        {
            get { return this.view.loadMenu; }
        }

        public MenuItem clearButton
        {
            get { return this.view.clearMenu; }
        }

        public ViewHandler(ViewControl view)
        {
            this.view = view;
            this.view.Dispatcher.ShutdownStarted += Dispatcher_ShutdownStarted;

            this.view.videoControl.RemoveMedia += new EventHandler<MediaRemoveEventArgs>(removeMedia);
            this.view.trackControl.signalTrackControl.RemoveSignal += new EventHandler<SignalRemoveEventArgs>(removeSignal);

            this.view.annoListControl.annoDataGrid.SelectionChanged += annoDataGrid_SelectionChanged;
            this.view.annoListControl.editButton.Click += editAnnoButton_Click;
            this.view.trackControl.CloseAnnotrackButton.Click += closeTier_Click;
            this.view.trackControl.annoNameLabel.Click += annoNameLabel_Click;

            this.view.navigator.newAnnoButton.Click += newAnnoButton_Click;
            this.view.navigator.newAnnoContButton.Click += newAnnoContButton_Click;

            this.view.clearMenu.Click += clearButton_Click;
            this.view.saveMenu.Click += saveButton_Click;
            this.view.saveProject.Click += saveProject_Click;
            this.view.exportSamples.Click += exportSamplesButton_Click;
            this.view.convertodiscretemenu.Click += converttodiscrete_Click;
            this.view.convertosignalemenu.Click += convertosignal_Click;
            // this.view.calculatepraat.Click += calculatepraat_Click;
            this.view.savetiermenu.Click += saveAnnoAsButton_Click;
            this.view.convertocontannoemenu.Click += convertocontanno_Click;
            this.view.mongodbmenu.Click += mongodb_Store;
            this.view.mongodbmenu2.Click += mongodb_Load;
            this.view.tiermenu.MouseEnter += tierMenu_Click;
            this.view.help.Click += helpMenu_Click;

            //  this.view.navigator.saveAnnoAsButton.Click += saveAnnoAsButton_Click;
            this.view.navigator.clearButton.Click += clearButton_Click;
            this.view.navigator.jumpFrontButton.Click += jumpFrontButton_Click;
            this.view.navigator.playButton.Click += playButton_Click;
            this.view.navigator.fastforward.Click += fastforward_Click;
            this.view.navigator.fastbackward.Click += fastbackward_Click;
            this.view.navigator.jumpEndButton.Click += jumpEndButton_Click;
            this.view.navigator.framewisebox.Unchecked += frameWiseBox_Unchecked;
            this.view.exportSampledAnnotations.Click += exportSampledAnnotationsButton_Click;

            //AnnoTrack.OnTrackPlay += playTrackHandler;
            AnnoTrack.OnTrackChange += changeAnnoTrackHandler;
            AnnoTrack.OnTrackSegmentChange += changeAnnoTrackSegmentHandler;
            SignalTrack.OnChange += changeSignalTrackHandler;
            SignalTrackEx.OnChange += changeSignalTrackHandler;

            ViewHandler.time = new ViewTime();
            this.view.trackControl.SizeChanged += new SizeChangedEventHandler(OnTrackControlSizeChanged);
            time.SelectionInPixel = this.view.trackControl.ActualWidth;
            this.view.trackControl.timeTrackControl.rangeSlider.ViewTime = time;

            this.view.trackControl.timeTrackControl.rangeSlider.OnTimeRangeChanged += this.view.trackControl.timeTrackControl.timeTrack.timeRangeChanged;
            this.view.trackControl.timeTrackControl.rangeSlider.OnTimeRangeChanged += this.view.trackControl.timeTrackControl.timeTrackSelection.timeRangeChanged;
            this.view.trackControl.timeTrackControl.rangeSlider.Update();

            this.media_list.OnMediaPlay += mediaPlayHandler;
            this.view.Drop += viewControl_Drop;

            this.view.trackControl.MouseWheel += (sender, args) =>
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    if (args.Delta > 0)
                    {
                        this.view.trackControl.timeTrackControl.rangeSlider.MoveAndUpdate(true, 0.2f);
                    }
                    else if (args.Delta < 0)
                    {
                        this.view.trackControl.timeTrackControl.rangeSlider.MoveAndUpdate(false, 0.2f);
                    }
                }
            };

            initCursor();
        }

        protected void removeMedia(object sender, MediaRemoveEventArgs e)
        {
            media_list.Medias.Remove(e.media);
        }

        protected void removeSignal(object sender, SignalRemoveEventArgs e)
        {
            signal_tracks.Remove(e.signal);
        }

        private void Dispatcher_ShutdownStarted(object sender, EventArgs e)
        {
            clear();
        }

        public void OnKeyUp(object sender, KeyEventArgs e)
        {
            keyDown = false;
        }

        public void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!this.view.annoListControl.editTextBox.IsFocused)
            {
                if (e.KeyboardDevice.IsKeyDown(Key.Space))
                {
                    bool is_playing = IsPlaying();
                    if (!is_playing)
                    {
                        Play();
                    }
                    else { Stop(); }

                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.Right) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                {
                    if (AnnoTrack.GetSelectedSegment() != null && AnnoTrack.GetSelectedTrack().isDiscrete && keyDown == false /*&& AnnoTrack.GetSelectedSegment() == null*/)
                    {
                        UIElement container = VisualTreeHelper.GetParent(AnnoTrack.GetSelectedSegment()) as UIElement;
                        Point relativeLocation = AnnoTrack.GetSelectedSegment().TranslatePoint(new Point(0, 0), container);

                        media_list.move(ViewHandler.Time.TimeFromPixel(relativeLocation.X + AnnoTrack.GetSelectedSegment().Width));

                        if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                        {
                            annoCursor.X = relativeLocation.X + AnnoTrack.GetSelectedSegment().Width;
                        }
                        else signalCursor.X = relativeLocation.X + AnnoTrack.GetSelectedSegment().Width;

                        time.CurrentSelectPosition = annoCursor.X;
                        time.CurrentPlayPosition = ViewHandler.Time.TimeFromPixel(signalCursor.X);
                        time.CurrentPlayPositionPrecise = ViewHandler.Time.TimeFromPixel(signalCursor.X);
                        AnnoTrack.GetSelectedSegment().select(true);
                        keyDown = true;
                    }
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.Left) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                {
                    if (AnnoTrack.GetSelectedSegment() != null && AnnoTrack.GetSelectedTrack().isDiscrete && keyDown == false /*&& AnnoTrack.GetSelectedSegment() == null*/)
                    {
                        UIElement container = VisualTreeHelper.GetParent(AnnoTrack.GetSelectedSegment()) as UIElement;
                        Point relativeLocation = AnnoTrack.GetSelectedSegment().TranslatePoint(new Point(0, 0), container);

                        media_list.move(ViewHandler.Time.TimeFromPixel(relativeLocation.X));

                        if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                        {
                            annoCursor.X = relativeLocation.X;
                        }
                        else signalCursor.X = relativeLocation.X;

                        time.CurrentSelectPosition = annoCursor.X;
                        time.CurrentPlayPosition = ViewHandler.Time.TimeFromPixel(signalCursor.X);
                        time.CurrentPlayPositionPrecise = ViewHandler.Time.TimeFromPixel(signalCursor.X);
                        AnnoTrack.GetSelectedSegment().select(true);
                        keyDown = true;
                    }
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && !keyDown)
                {
                    if (AnnoTrack.GetSelectedTrack() != null && !AnnoTrack.GetSelectedTrack().isDiscrete)
                    {
                        AnnoTrack.GetSelectedTrack().ContAnnoMode();
                    }
                    keyDown = true;
                    e.Handled = true;
                }
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!this.view.annoListControl.editTextBox.IsFocused)
            {
                if (e.KeyboardDevice.IsKeyDown(Key.S) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                {
                    if (AnnoTrack.GetSelectedTrack().isDiscrete) saveAnno();
                    else saveAnnoContinous();
                }
                else if (e.KeyboardDevice.IsKeyDown(Key.Delete))
                {
                    if (AnnoTrack.GetSelectedSegment() == null && Mouse.DirectlyOver == AnnoTrack.GetSelectedTrack())
                    {
                        removeTier();
                    }
                    else
                    {
                        AnnoTrack.OnKeyDownHandler(sender, e);
                    }
                }

                if (e.KeyboardDevice.IsKeyDown(Key.E) && !keyDown)
                {
                    if (AnnoTrack.GetSelectedSegment() != null && AnnoTrack.GetSelectedTrack().isDiscrete && keyDown == false)
                    {
                        UIElement container = VisualTreeHelper.GetParent(AnnoTrack.GetSelectedSegment()) as UIElement;
                        Point relativeLocation = AnnoTrack.GetSelectedSegment().TranslatePoint(new Point(0, 0), container);

                        media_list.move(ViewHandler.Time.TimeFromPixel(relativeLocation.X + AnnoTrack.GetSelectedSegment().Width));

                        annoCursor.X = relativeLocation.X;
                        signalCursor.X = relativeLocation.X + AnnoTrack.GetSelectedSegment().Width;

                        time.CurrentSelectPosition = annoCursor.X;
                        time.CurrentPlayPosition = ViewHandler.Time.TimeFromPixel(signalCursor.X);
                        AnnoTrack.GetSelectedSegment().select(true);
                        keyDown = true;
                    }
                }

                if (e.KeyboardDevice.IsKeyDown(Key.Q) && !keyDown)
                {
                    if (AnnoTrack.GetSelectedSegment() != null && AnnoTrack.GetSelectedTrack().isDiscrete && keyDown == false)
                    {
                        UIElement container = VisualTreeHelper.GetParent(AnnoTrack.GetSelectedSegment()) as UIElement;
                        Point relativeLocation = AnnoTrack.GetSelectedSegment().TranslatePoint(new Point(0, 0), container);

                        media_list.move(ViewHandler.Time.TimeFromPixel(relativeLocation.X));

                        annoCursor.X = relativeLocation.X + AnnoTrack.GetSelectedSegment().Width;
                        signalCursor.X = relativeLocation.X;

                        time.CurrentSelectPosition = annoCursor.X;
                        time.CurrentPlayPosition = ViewHandler.Time.TimeFromPixel(signalCursor.X);
                        AnnoTrack.GetSelectedSegment().select(true);
                        keyDown = true;
                    }
                }

                if ((e.KeyboardDevice.IsKeyDown(Key.W) || e.KeyboardDevice.IsKeyDown(Key.A) || e.KeyboardDevice.IsKeyDown(Key.Return) && !keyDown && AnnoTrack.GetSelectedTrack() != null) && AnnoTrack.GetSelectedTrack().isDiscrete)
                {
                    if (AnnoTrack.GetSelectedSegment() == null)
                    {
                        AnnoTrack.GetSelectedTrack().newAnnokey();
                    }
                    else
                    {
                        ShowLabelBox();
                    }
                    AnnoTrack.GetSelectedSegment().select(true);
                    keyDown = true;
                    // e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.Right) && e.KeyboardDevice.IsKeyDown(Key.LeftAlt) /*&& !keyDown*/)
                {
                    int i = 0;
                    double fps = 1.0 / 30.0;
                    foreach (IMedia im in media_list.Medias)
                    {
                        if (im.IsVideo())
                        {
                            break;
                        }
                        i++;
                    }

                    if (i < media_list.Medias.Count)
                    {
                        fps = 1.0 / media_list.Medias[i].GetSampleRate();
                    }

                    //In case no media is loaded it takes the sr of the first loaded signal
                    else
                    {
                        if (signals.Count > 0)
                        {
                            fps = 1.0 / signals[0].rate;
                        }
                    }

                    media_list.move(ViewHandler.Time.TimeFromPixel(signalCursor.X) + fps);

                    if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                    {
                        annoCursor.X = annoCursor.X + ViewHandler.Time.PixelFromTime(fps);
                    }
                    else signalCursor.X = signalCursor.X + ViewHandler.Time.PixelFromTime(fps);

                    time.CurrentSelectPosition = annoCursor.X;
                    time.CurrentPlayPosition = ViewHandler.Time.TimeFromPixel(signalCursor.X);
                    time.CurrentPlayPositionPrecise = ViewHandler.Time.TimeFromPixel(signalCursor.X);

                    if (AnnoTrack.GetSelectedSegment() != null)
                    {
                        double start = annoCursor.X;
                        double end = signalCursor.X;

                        if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                        {
                            start = signalCursor.X;
                            end = annoCursor.X;
                        }
                        if (end > start)
                        {
                            AnnoTrack.GetSelectedSegment().resize_right(ViewHandler.Time.PixelFromTime(fps));
                        }
                        else
                        {
                            AnnoTrack.GetSelectedSegment().resize_left(ViewHandler.Time.PixelFromTime(fps));
                        }
                        AnnoTrack.GetSelectedSegment().select(true);
                    }

                    keyDown = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.Left) && e.KeyboardDevice.IsKeyDown(Key.LeftAlt)/* && !keyDown*/)
                {
                    int i = 0;
                    double fps = 1.0 / 30.0;
                    foreach (IMedia im in media_list.Medias)
                    {
                        if (im.IsVideo())
                        {
                            break;
                        }
                        i++;
                    }

                    if (i < media_list.Medias.Count)
                    {
                        fps = 1.0 / media_list.Medias[i].GetSampleRate();
                    }

                    media_list.move(ViewHandler.Time.TimeFromPixel(signalCursor.X) - fps);
                    if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                    {
                        annoCursor.X = annoCursor.X - ViewHandler.Time.PixelFromTime(fps);
                    }
                    else
                    {
                        signalCursor.X = signalCursor.X - ViewHandler.Time.PixelFromTime(fps);
                    }

                    time.CurrentSelectPosition = annoCursor.X;
                    time.CurrentPlayPosition = ViewHandler.Time.TimeFromPixel(signalCursor.X);
                    time.CurrentPlayPositionPrecise = ViewHandler.Time.TimeFromPixel(signalCursor.X);

                    double start = annoCursor.X;
                    double end = signalCursor.X;

                    if (e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                    {
                        start = signalCursor.X;
                        end = annoCursor.X;
                    }

                    if (AnnoTrack.GetSelectedSegment() != null)
                    {
                        if (end > start)
                        {
                            AnnoTrack.GetSelectedSegment().resize_right(-ViewHandler.Time.PixelFromTime(fps));
                        }
                        else
                        {
                            AnnoTrack.GetSelectedSegment().resize_left(-ViewHandler.Time.PixelFromTime(fps));
                        }
                        AnnoTrack.GetSelectedSegment().select(true);
                    }

                    keyDown = true;
                }
                else
                {
                    AnnoTrack.OnKeyDownHandler(sender, e);
                }
            }
        }

        private void OnTrackControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewHandler.time.SelectionInPixel = this.view.trackControl.ActualWidth;
            this.view.trackControl.timeTrackControl.rangeSlider.Update();
        }

        public AnnoList getCurrentAnno()
        {
            return current_anno;
        }

        public Signal getCurrentSignal()
        {
            return SignalTrack.SelectedSignal;
        }

        public void saveProject()
        {
            saveAll();
            string configfilepath = "";

            MessageBoxResult mbx = MessageBox.Show("Save project to file " + configfilepath.ToString() + "?", "Question", MessageBoxButton.YesNo);
            if (mbx == MessageBoxResult.Yes)
            {
                if (configfilepath == "")
                {
                    //If no (new format) anno file was loaded, get directory of first media element, else from first signal, else default last folder is picked.
                    string firstmediadir = "";
                    if (media_list.Medias.Count > 0) firstmediadir = media_list.Medias[0].GetFolderepath();
                    else if (signals.Count > 0) firstmediadir = signals[0].Folderpath;

                    configfilepath = ViewTools.SaveFileDialog("project", ".vui", firstmediadir, 4);
                    if (configfilepath != null)
                    {
                        saveConfig(anno_tracks, media_list, signal_tracks, configfilepath);
                    }
                }
                else
                {
                    saveConfig(anno_tracks, media_list, signal_tracks, configfilepath);
                }
            }
        }

        public void saveAll()
        {
            bool anytrackchanged = false;
            foreach (AnnoTrack track in anno_tracks)
            {
                if (track.AnnoList.HasChanged && track.isDiscrete) anytrackchanged = true;
            }

            if (anno_tracks.Count > 0 && anytrackchanged)
            {
                if (anno_tracks.Count == 1)
                {
                    if (annofilepath == "" || annofilepath == null) annofilepath = anno_tracks[0].AnnoList.Filepath;
                }

                string csvfilepath = annofilepath;
                if (csvfilepath == null) csvfilepath = "";

                MessageBoxResult mbx = MessageBox.Show("Save annotations to file " + csvfilepath.ToString() + "?", "Question", MessageBoxButton.YesNo);
                if (mbx == MessageBoxResult.Yes)
                {
                    if (csvfilepath == "")
                    {
                        //If no (new format) anno file was loaded, get directory of first media element, else from first signal, else default last folder is picked.
                        string firstmediadir = "";
                        if (media_list.Medias.Count > 0) firstmediadir = media_list.Medias[0].GetFolderepath();
                        else if (signals.Count > 0) firstmediadir = signals[0].Folderpath;

                        csvfilepath = ViewTools.SaveFileDialog("anno", ".csv", firstmediadir);
                        if (csvfilepath != null)
                        {
                            //config will be the project config later, for now its the anno file containing all tiers.
                            saveCSVAnno(anno_tracks, csvfilepath);
                            foreach (AnnoTrack track in anno_tracks)
                            {
                                if (track.isDiscrete) track.AnnoList.HasChanged = false;
                            }
                        }
                    }
                    else
                    {
                        saveCSVAnno(anno_tracks, csvfilepath);
                        foreach (AnnoTrack track in anno_tracks)
                        {
                            if (track.isDiscrete) track.AnnoList.HasChanged = false;
                        }
                    }
                }
            }

            //handle continous tracks se
            foreach (AnnoTrack track in anno_tracks)
            {
                if (track.AnnoList.HasChanged && !track.isDiscrete)
                {
                    MessageBoxResult mbr = MessageBox.Show("Save changes on continous tier " + track.TierId + "?", "Question", MessageBoxButton.YesNo);
                    if (mbr == MessageBoxResult.Yes)
                    {
                        saveAnnoContinous(track.AnnoList, track.AnnoList.Filepath);
                        if (!track.isDiscrete) track.AnnoList.HasChanged = false; ;
                    }
                }
            }
        }

        public void clear()
        {
            Stop();
            saveAll();

            this.view.trackControl.signalNameLabel.Text = "";
            this.view.trackControl.signalNameLabel.ToolTip = "";
            this.view.trackControl.signalBytesLabel.Text = "";
            this.view.trackControl.signalDimLabel.Text = "";
            this.view.trackControl.signalSrLabel.Text = "";
            this.view.trackControl.signalTypeLabel.Text = "";
            this.view.trackControl.signalValueLabel.Text = "";
            this.view.trackControl.signalValueMinLabel.Text = "";
            this.view.trackControl.signalValueMaxLabel.Text = "";
            this.view.trackControl.signalPositionLabel.Text = "00:00:00";
            this.view.trackControl.annoNameLabel.Content = "#NoTier";
            this.view.trackControl.annoPositionLabel.Text = "00:00:00";
            this.view.navigator.playButton.IsEnabled = false;

            this.signalCursor.X = 0;
            setAnnoList(null);

            this.view.trackControl.annoTrackControl.clear();
            this.view.trackControl.signalTrackControl.clear();
            this.view.videoControl.clear();
            this.view.skeletonControl.clear();

            this.signal_tracks.Clear();
            this.signals.Clear();
            this.anno_tracks.Clear();
            this.annos.Clear();
            this.media_list.clear();
            annofilepath = "";

            if (this.view.videoskel.ColumnDefinitions.Count > 1)
            {
                ColumnDefinition column = this.view.videoskel.ColumnDefinitions[1];
                this.view.videoskel.ColumnDefinitions.Remove(column);
            }

            Time.TotalDuration = 0;

            this.view.trackControl.timeTrackControl.rangeSlider.Update();
        }

        private void nomediaPlayHandler(Signal s = null)
        {
            this.view.navigator.playButton.IsEnabled = true;
            double fps;
            if (s != null)
            {
                fps = 1000.0 / s.rate;
                skelfps = fps;
            }
            else
            {
                fps = skelfps;
            }

            lasttimepos = ViewHandler.Time.CurrentPlayPosition;
            if (innomediaplaymode)
            {
                this.view.navigator.playButton.Content = "II";
                // Play();
                _timerp.Interval = TimeSpan.FromMilliseconds(fps);
                _timerp.Tick += new EventHandler(delegate (object sender, EventArgs a)
                {
                    ViewHandler.Time.CurrentPlayPositionPrecise = (ViewHandler.Time.CurrentPlayPositionPrecise + (fps / 1000.0));
                    if (media_list.Medias.Count == 0) signalCursor.X = ViewHandler.Time.PixelFromTime(ViewHandler.Time.CurrentPlayPositionPrecise);

                    //hm additional syncstep..
                    if (lasttimepos != ViewHandler.Time.CurrentPlayPosition)
                    {
                        lasttimepos = ViewHandler.Time.CurrentPlayPosition;
                        ViewHandler.Time.CurrentPlayPositionPrecise = lasttimepos;
                    }

                    if (!innomediaplaymode) _timerp.Stop();
                });
                _timerp.Start();
            }
            else
            {
                _timerp.Stop();
            }
        }

        private void mediaPlayHandler(MediaList videos, MediaPlayEventArgs e)
        {
            double pos = ViewHandler.Time.PixelFromTime(e.pos);

            signalCursor.X = pos;

            if (ViewHandler.Time.TimeFromPixel(signalCursor.X) > ViewHandler.time.TotalDuration - 0.5)
            {
                Stop();
            }
            Time.CurrentPlayPosition = ViewHandler.Time.TimeFromPixel(signalCursor.X);
            // Time.CurrentPlayPosition = e.pos;

            if (Time.CurrentPlayPosition > Time.SelectionStop && this.view.navigator.followplaybox.IsChecked == true)
                this.view.trackControl.timeTrackControl.rangeSlider.MoveAndUpdate(true, 1.0f);

            double time = Time.TimeFromPixel(pos);
            this.view.trackControl.signalPositionLabel.Text = ViewTools.FormatSeconds(time);
            this.view.trackControl.annoTrackControl.currenttime = Time.TimeFromPixel(pos);
        }

        private void annoDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView grid = (ListView)sender;

            if (grid.SelectedIndex >= 0 && grid.SelectedIndex < grid.Items.Count)
            {
                AnnoListItem item = current_anno[grid.SelectedIndex];
                //moveCursorTo (item.Start);
            }
        }

        private void onCursorChange(double pos)
        {
            Signal signal = getCurrentSignal();
            if (signal != null)
            {
                double time = Time.TimeFromPixel(pos);
                this.view.trackControl.signalValueLabel.Text = signal.Value(time).ToString();
                this.view.trackControl.signalValueMinLabel.Text = "min " + signal.min[signal.ShowDim].ToString();
                this.view.trackControl.signalValueMaxLabel.Text = "max " + signal.max[signal.ShowDim].ToString();
            }
        }

        private void moveCursorTo(double seconds)
        {
            double pos = ViewHandler.Time.PixelFromTime(seconds);
            signalCursor.X = pos;
            //this.view.trackControl.scrollViewer.ScrollToHorizontalOffset(Math.Max(0, pos - this.view.trackControl.scrollViewer.ActualWidth / 2));
            double time = Time.TimeFromPixel(pos);
            this.view.trackControl.signalPositionLabel.Text = ViewTools.FormatSeconds(time);
        }

        public void newAnno(bool isDiscrete, double samplerate = 1.0, double borderlow = 0, double borderhigh = 1.0, LinearGradientBrush background = null)
        {
            AnnoList anno = new AnnoList();
            addAnno(anno, isDiscrete, samplerate, null, borderlow, borderhigh, background);
        }

        public void addAnno(AnnoList anno, bool isdiscrete, double samplerate = 1, string filepath = null, double borderlow = 0.0, double borderhigh = 1.0, LinearGradientBrush background = null)
        {
            string TierId;
            if (anno.Count > 0) samplerate = anno[0].Duration;
            if (anno.Name != null) TierId = anno.Name;
            else if (this.anno_tracks.Count == 0) TierId = "Default";
            else
            {
                tiercount++;
                TierId = "Default" + (tiercount).ToString();
            }
            anno.Name = TierId;

            AnnoTrack track = this.view.trackControl.annoTrackControl.addAnnoTrack(anno, isdiscrete, samplerate, TierId, borderlow, borderhigh);
            track.TierId = anno.Name;
            track.AnnoList.Filepath = filepath;
            track.AnnoList.Lowborder = borderlow;
            track.AnnoList.Highborder = borderhigh;

            this.view.trackControl.timeTrackControl.rangeSlider.OnTimeRangeChanged += track.timeRangeChanged;

            this.anno_tracks.Add(track);
            this.annos.Add(anno);

            AnnoTrack.SelectTrack(track);
            if (background != null)
            {
                AnnoTrack.GetSelectedTrack().ContiniousBrush = background;
                AnnoTrack.GetSelectedTrack().Background = background;
            }
            track.timeRangeChanged(ViewHandler.Time);
        }

        private void loadAnno(string filename)

        {
            AnnoList anno = AnnoList.LoadfromFile(filename);
            anno.Filepath = filename;
            anno.SampleAnnoPath = filename;
            double maxdur = 0;

            foreach (AnnoListItem ali in anno)
            {
                if (ali.Stop > maxdur)
                {
                    maxdur = ali.Stop;
                }
            }
            if (anno != null)
            {
                setAnnoList(anno);
                addAnno(anno, anno.isDiscrete, 1, filename);
            }

            updateTimeRange(maxdur);
        }

        private void handleAnnotation(AnnoList anno, string filename, double samplerate = 1)
        {
            if (anno.isDiscrete && annofilepath == "") annofilepath = filename;
            double maxdur = 0;
            //Get all tier ids that haven't been added before
            //(This is where in the future tiers will be read from the config)
            List<String> TierIds = new List<String>();
            foreach (AnnoListItem ali in anno)
            {
                if (!TierIds.Contains(ali.Tier))
                {
                    TierIds.Add(ali.Tier);
                }

                //While doing this anyway, we find the latest time to adjust the view according to the latest annotation
                if (ali.Stop > maxdur)
                {
                    maxdur = ali.Stop;
                }
            }

            //Create a new AnnoList per tierId and add it
            foreach (String tierid in TierIds)
            {
                AnnoList annolist = new AnnoList();
                foreach (AnnoListItem ali in anno)
                {
                    if (ali.Tier == tierid)
                    {
                        annolist.Name = tierid;
                        //check if trackid is already used
                        foreach (AnnoTrack a in anno_tracks)
                        {
                            if (a.AnnoList.Name == tierid)
                            {
                                annolist.Name = tierid + tiercount.ToString();
                                break;
                            }
                        }
                        ali.Tier = annolist.Name;
                        annolist.Add(ali);
                    }
                }
                annolist.Highborder = anno.Highborder;
                annolist.Lowborder = anno.Lowborder;
                if (annolist != null)
                {
                    setAnnoList(annolist);
                    annolist.SampleAnnoPath = filename;
                    addAnno(annolist, annolist.isDiscrete, samplerate, filename, annolist.Lowborder, annolist.Highborder);
                    annolist.HasChanged = false;
                    tiercount++;
                }
            }

            //Adjust the view
            updateTimeRange(maxdur);
        }

        private void loadCSVAnnotation(string filename, double samplerate = 1, string type = "semicolon")
        {
            //Temp list that contains all annotations from file
            AnnoList anno = AnnoList.LoadfromFile(filename, samplerate, type);
            handleAnnotation(anno, filename, samplerate = 1);
        }

        private void loadElan(string filename)
        {
            AnnoList[] anno = AnnoList.LoadfromElanFile(filename);
            double maxdur = 0;

            if (anno != null)
            {
                foreach (AnnoList a in anno)
                {
                    foreach (AnnoListItem it in a)
                    {
                        if (it.Stop > maxdur)
                        {
                            maxdur = it.Stop;
                        }
                    }

                    annos.Add(a);
                    setAnnoList(a);
                    addAnno(a, true, 1);
                }
            }
            updateTimeRange(maxdur);
        }

        private void loadAnvil(string filename)
        {
            AnnoList[] anno = AnnoList.LoadfromAnvilFile(filename);
            double maxdur = 0;

            if (anno != null)
            {
                foreach (AnnoList a in anno)
                {
                    foreach (AnnoListItem it in a)
                    {
                        if (it.Stop > maxdur)
                        {
                            maxdur = it.Stop;
                        }
                    }

                    annos.Add(a);
                    setAnnoList(a);
                    addAnno(a, true, 1);
                }
            }
            updateTimeRange(maxdur);
        }

        private void loadEvents(string filename)
        {
            AnnoList anno = AnnoList.LoadFromEventsFile(filename);
            double maxdur = 0;

            if (anno != null)
            {
                foreach (AnnoListItem it in anno)
                {
                    if (it.Stop > maxdur)
                    {
                        maxdur = it.Stop;
                    }
                }

                annos.Add(anno);
                setAnnoList(anno);
                addAnno(anno, true);
            }

            updateTimeRange(maxdur);
        }

        private void loadStream(string filename, string color = "#FF000000", string background = "#FFF0F0F0")
        {
            Signal signal = Signal.LoadStreamFile(filename);
            if (signal != null && signal.loaded)
            {
                addSignal(signal, color, background);

                //if signal is SSI Skeleton...
                if (signal.meta_name == "skeleton")
                {
                    if (this.view.videoskel.ColumnDefinitions.Count < 2 && media_list.Medias.Count > 0)
                    {
                        ColumnDefinition column = new ColumnDefinition();
                        column.Width = new GridLength(1, GridUnitType.Star);
                        this.view.videoskel.ColumnDefinitions.Add(column);
                    }
                    else if (this.view.videoskel.ColumnDefinitions.Count < 2)
                    {
                        ColumnDefinition columvideo = this.view.videoskel.ColumnDefinitions[0];
                        columvideo.Width = new GridLength(0, GridUnitType.Pixel);

                        ColumnDefinition column = new ColumnDefinition();
                        column.Width = new GridLength(1, GridUnitType.Star);
                        this.view.videoskel.ColumnDefinitions.Add(column);
                    }
                    this.view.skeletonControl.addSkeleton(signal);
                    nomediaPlayHandler(signal);
                }
            }
        }

        private void loadCSV(string filename, string color = "#FF000000", string background = "#FFF0F0F0")
        {
            Signal signal = Signal.LoadCSVFile(filename);
            if (signal != null && signal.loaded)
            {
                addSignal(signal, color, background);
            }
        }

        private void loadWav(string filename, string color = "#FF000000", string background = "#FFF0F0F0")
        {
            Signal signal = Signal.LoadWaveFile(filename);
            if (signal != null && signal.loaded)
            {
                addSignal(signal, color, background);
            }
        }

        private void addSignal(Signal signal, string color, string background)
        {
            ISignalTrack track = this.view.trackControl.signalTrackControl.addSignalTrack(signal, color, background);
            this.view.trackControl.timeTrackControl.rangeSlider.OnTimeRangeChanged += track.timeRangeChanged;
            this.signals.Add(signal);
            this.signal_tracks.Add(track);

            double duration = signal.number / signal.rate;
            if (duration > ViewHandler.Time.TotalDuration)
            {
                ViewHandler.Time.TotalDuration = duration;
                this.view.trackControl.timeTrackControl.rangeSlider.Update();
            }
            else
            {
                track.timeRangeChanged(ViewHandler.Time);
            }
            //updateTimeRange(duration);
            //track.timeRangeChanged(ViewHandler.Time);
        }

        private void updateTimeRange(double duration)
        {
            if (duration > ViewHandler.Time.TotalDuration)
            {
                ViewHandler.Time.TotalDuration = duration;
                this.view.trackControl.timeTrackControl.rangeSlider.Update();
            }
        }

        private void loadMedia(string filename, bool is_video)
        {
            double pos = ViewHandler.Time.TimeFromPixel(signalCursor.X);
            IMedia media = media_list.addMedia(filename, pos);
            this.view.videoControl.addMedia(media, is_video);
            this.view.navigator.playButton.IsEnabled = true;
            innomediaplaymode = false;
            nomediaPlayHandler(null);

            ColumnDefinition columvideo = this.view.videoskel.ColumnDefinitions[0];
            columvideo.Width = new GridLength(1, GridUnitType.Star);

            DispatcherTimer _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromMilliseconds(50);
            _timer.Tick += new EventHandler(delegate (object s, EventArgs a)
            {
                if (media.GetLength() > 0)
                {
                    updateTimeRange(media.GetLength());
                    _timer.Stop();
                }
            });
            _timer.Start();
        }

        private void setAnnoList(AnnoList anno)
        {
            current_anno = anno;
            view.annoListControl.annoDataGrid.ItemsSource = anno;
        }

        private void frameWiseBox_Unchecked(object sender, RoutedEventArgs e)
        {
            AnnoTrack.UnselectSegment();

            bool is_playing = IsPlaying();
            Stop();
            if (is_playing)
            {
                Play();
            }
        }

        private void jumpFrontButton_Click(object sender, RoutedEventArgs e)
        {
            bool is_playing = IsPlaying();
            Stop();
            moveCursorTo(0);
            if (is_playing)
            {
                Play();
            }
        }

        private void jumpEndButton_Click(object sender, RoutedEventArgs e)
        {
            Stop();
            moveCursorTo(ViewHandler.Time.TotalDuration);
        }

        private void playButton_Click(object sender, RoutedEventArgs e)
        {
            //if(media_list.Medias.Count == 0)
            //{
            if ((string)this.view.navigator.playButton.Content == "II")
            {
                innomediaplaymode = false;
                nomediaPlayHandler(null);
                this.view.navigator.playButton.Content = ">";
            }
            else
            {
                innomediaplaymode = true;
                nomediaPlayHandler(null);
                this.view.navigator.playButton.Content = "II";
            }

            //   }

            infastbackward = false;
            infastforward = false;

            this.view.navigator.fastforward.Content = ">>";
            this.view.navigator.fastbackward.Content = "<<";

            if (media_list.Medias.Count > 0)
            {
                if (IsPlaying())
                {
                    Stop();
                }
                else
                {
                    Play();
                }
            }
        }

        private void fastforward_Click(object sender, RoutedEventArgs e)
        {
            int updateinms = 300;
            double updatestep = 1;

            infastbackward = false;
            if (infastforward) infastforward = false;
            else infastforward = true;

            if (infastforward)
            {
                this.view.navigator.fastforward.Content = ">";
                _timerff.Interval = TimeSpan.FromMilliseconds(updateinms);
                _timerff.Tick += new EventHandler(delegate (object s, EventArgs a)
                {
                    if (media_list.Medias.Count > 0)
                    {
                        media_list.move(ViewHandler.Time.TimeFromPixel(signalCursor.X) + updatestep);
                        //  media_list.move(Time.CurrentPlayPosition+1);
                        if (!infastforward) _timerff.Stop();
                    }
                });
                _timerff.Start();
            }
            else
            {
                _timerff.Stop();
                this.view.navigator.fastforward.Content = ">>";
            }
        }

        private void fastbackward_Click(object sender, RoutedEventArgs e)
        {
            int updateinms = 300;
            double updatestep = 1;

            infastforward = false;

            if (infastbackward) infastbackward = false;
            else infastbackward = true;

            if (infastbackward)
            {
                this.view.navigator.fastbackward.Content = ">";
                _timerfb.Interval = TimeSpan.FromMilliseconds(updateinms);
                _timerfb.Tick += new EventHandler(delegate (object s, EventArgs a)
                {
                    if (media_list.Medias.Count > 0)
                    {
                        media_list.move(ViewHandler.Time.TimeFromPixel(signalCursor.X) - updatestep);
                        if (!infastbackward) _timerfb.Stop();
                    }
                });
                _timerfb.Start();
            }
            else
            {
                _timerfb.Stop();
                this.view.navigator.fastbackward.Content = "<<";
            }
        }

        public void Stop()
        {
            if (IsPlaying())
            {
                media_list.stop();
                this.view.navigator.playButton.Content = ">";
            }
        }

        public bool IsPlaying()
        {
            return media_list.IsPlaying;
        }

        public void Play()
        {
            Stop();

            double pos = 0;
            AnnoListItem item = null;
            bool loop = false;

            AnnoTrackSegment selected = AnnoTrack.GetSelectedSegment();
            if (selected != null)
            {
                item = selected.Item;
                signalCursor.X = Time.PixelFromTime(item.Start);
                loop = true;
            }
            else
            {
                pos = signalCursor.X;
                double from = ViewHandler.Time.TimeFromPixel(pos);
                double to = ViewHandler.time.TotalDuration;
                item = new AnnoListItem(from, to, "");
                signalCursor.X = pos;
            }

            try
            {
                media_list.play(item, loop);
                this.view.navigator.playButton.Content = "II";
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.ToString());
            }

            //
        }

        private void changeAnnoTrackHandler(AnnoTrack track, EventArgs e)
        {
            this.view.trackControl.annoNameLabel.Content = "#" + track.TierId;
            //  this.view.annoNameLabel.ToolTip = track.AnnoList.Filepath;
            setAnnoList(track.AnnoList);

            //this.view.trackControl.annoNameLabel.Text = track.AnnoList.Filename;
        }

        private void changeAnnoTrackSegmentHandler(AnnoTrackSegment segment, EventArgs e)
        {
            // this.view.annoNameLabel.Text = track.AnnoList.Filename;
            // this.view.annoNameLabel.ToolTip = track.AnnoList.Filepath;
            // setAnnoList(track.AnnoList);

            foreach (AnnoListItem item in view.annoListControl.annoDataGrid.Items)
            {
                if (segment.Item.Start == item.Start)
                {
                    view.annoListControl.annoDataGrid.SelectedItem = item;
                    view.annoListControl.annoDataGrid.ScrollIntoView(view.annoListControl.annoDataGrid.SelectedItem);
                    break;
                }
            }
        }

        private void changeSignalTrackHandler(ISignalTrack track, EventArgs e)
        {
            Signal signal = track.getSignal();

            if (signal != null)
            {
                this.view.trackControl.signalNameLabel.Text = signal.Filename;
                this.view.trackControl.signalNameLabel.ToolTip = signal.Filepath;
                this.view.trackControl.signalBytesLabel.Text = signal.bytes + " bytes";
                this.view.trackControl.signalDimLabel.Text = signal.dim.ToString();
                this.view.trackControl.signalSrLabel.Text = signal.rate + " Hz";
                this.view.trackControl.signalTypeLabel.Text = ViewTools.SSI_TYPE_NAME[(int)signal.type];
                this.view.trackControl.signalValueLabel.Text = signal.Value(time.SelectionStart).ToString();
                this.view.trackControl.signalValueMinLabel.Text = "min " + signal.min[signal.ShowDim].ToString();
                this.view.trackControl.signalValueMaxLabel.Text = "max " + signal.max[signal.ShowDim].ToString();
            }
        }

        public void saveAnno()
        {
            saveAnno(this.current_anno, this.current_anno.SampleAnnoPath);
        }

        public void saveAnno(string filepath)
        {
            saveAnno(this.current_anno, filepath);
        }

        public void saveAnno(AnnoList anno, string filepath)
        {
            if (anno != null)
            {
                anno.SampleAnnoPath = filepath;
                if (filepath != null)
                {
                    anno.saveToFile(filepath);
                }
                else
                {
                    anno.saveToFile();
                }
            }
        }

        public void saveAnnoContinous()
        {
            saveAnnoContinous(this.current_anno, this.current_anno.Filepath);
        }

        public void saveAnnoContinous(string filepath)
        {
            saveAnnoContinous(this.current_anno, filepath);
        }

        public void saveAnnoContinous(AnnoList anno, string filepath)
        {
            if (anno != null)
            {
                if (filepath != null)
                {
                    anno.saveContinousToFile(filepath);
                    anno.Filepath = filepath;
                }
                else
                {
                    anno.saveContinousToFile();
                }
            }
        }

        public void saveCSVAnno(List<AnnoTrack> tracks, string filepath)
        {
            if (tracks != null)
            {
                try
                {
                    StreamWriter sw = new StreamWriter(filepath, false, System.Text.Encoding.Default);

                    foreach (AnnoTrack t in tracks)
                    {
                        if (t.isDiscrete)
                        {
                            t.AnnoList.Filepath = filepath;
                            foreach (AnnoListItem e in t.AnnoList)
                            {
                                string meta = "";
                                if (e.Meta != "") meta = ";" + e.Meta;
                                sw.WriteLine(e.Start.ToString() + ";" + e.Stop.ToString() + ";" + e.Label + ";" + "#" + t.TierId + meta);
                            }
                        }
                    }
                    sw.Close();
                }
                catch
                {
                    Console.Write("Error writing file!");
                }
            }
        }

        public void saveConfig(List<AnnoTrack> tracks, MediaList ml, List<ISignalTrack> signal_tracks, string filepath)
        {
            StreamWriter sw = new StreamWriter(filepath, false, System.Text.Encoding.Default);
            sw.WriteLine("<ViewUIproject version=\"1\">");

            sw.WriteLine("\t<medias>");
            if (ml != null)
            {
                foreach (IMedia t in ml.Medias)
                {
                    sw.WriteLine("\t\t<media>" + t.GetFilepath() + "</media>");
                }
            }
            sw.WriteLine("\t</medias>");
            sw.WriteLine("\t<signals>");
            if (signal_tracks != null)
            {
                foreach (SignalTrack st in signal_tracks)
                {
                    sw.WriteLine("\t\t<signal bg=\"" + st.Background + "\" fg=\"" + st.SignalColor + "\">" + st.getSignal().Filepath + "</signal>");
                }
            }

            sw.WriteLine("\t</signals>");
            sw.WriteLine("\t<tiers>");

            string path = "";
            bool nodisretefound = true;
            foreach (AnnoTrack t in tracks)
            {
                if (t.isDiscrete)
                {
                    if (path == "")
                    {
                        path = t.AnnoList.Filepath;
                        sw.WriteLine("\t\t<discrete filepath=\"" + t.AnnoList.Filepath + "\">");
                        nodisretefound = false;
                    }

                    sw.WriteLine("\t\t\t<tier bg=\"" + t.BackgroundColor + "\" name=\"" + t.AnnoList.Name + "\">");

                    HashSet<LabelColorPair> h = new HashSet<LabelColorPair>();
                    foreach (AnnoListItem item in t.AnnoList)
                    {
                        //  if (item.Label != "")
                        //  {
                        LabelColorPair l = new LabelColorPair(item.Label, item.Bg);
                        bool detected = false;
                        foreach (LabelColorPair p in h)
                        {
                            if (p.label == l.label)
                            {
                                detected = true;
                            }
                        }

                        if (detected == false)
                        {
                            sw.WriteLine("\t\t\t\t<annotation color=\"" + l.Color + "\">" + l.Label + "</annotation>");
                            h.Add(l);
                        }
                        // }
                    }

                    sw.WriteLine("\t\t\t</tier>");
                }
            }
            if (nodisretefound) sw.WriteLine("\t\t<discrete>");

            sw.WriteLine("\t\t</discrete>");

            sw.WriteLine("\t\t<continuous>");
            foreach (AnnoTrack t in tracks)
            {
                if (!t.isDiscrete)
                {
                    sw.WriteLine("\t\t\t<tier bg=\"" + t.BackgroundColor + "\" filepath=\"" + t.AnnoList.Filepath + "\">" + t.AnnoList.Name + "</tier>");
                }
            }
            sw.WriteLine("\t\t</continuous>");

            sw.WriteLine("\t</tiers>");

            sw.WriteLine("</ViewUIproject>");
            sw.Close();
        }

        public void loadConfig(string filepath)
        {
            //try
            //{
            XmlDocument doc = new XmlDocument();
            doc.Load(filepath);
            foreach (XmlNode node in doc.SelectNodes("//media"))
            {
                bool isvideo = true;
                if (node.InnerText.Contains("wav"))
                {
                    isvideo = false;
                    // loadWav(node.InnerText);
                }
                loadMedia(node.InnerText, isvideo);
            }

            foreach (XmlNode node in doc.SelectNodes("//signal"))
            {
                string background = node.Attributes[0].LastChild.Value;
                string foreground = node.Attributes[1].LastChild.Value;
                if (node.InnerText.Contains("wav"))
                {
                    loadWav(node.InnerText);
                }
                else
                {
                    loadStream(node.InnerText, foreground, background);
                }
            }

            //

            foreach (XmlNode child in (doc.SelectNodes("//discrete")))
            {
                if (child.Attributes.Count > 0)
                {
                    if (child.Attributes[0].InnerText.Contains("csv"))
                    {
                        loadCSVAnnotation(child.Attributes[0].InnerText);
                    }
                    else if (child.Attributes[0].InnerText.Contains("anno"))
                    {
                        loadAnno(child.Attributes[0].InnerText);
                    }
                }
            }

            foreach (XmlNode child in (doc.SelectNodes("//tier")))
            {
                HashSet<LabelColorPair> h = new HashSet<LabelColorPair>();
                foreach (XmlNode grandchild in child.ChildNodes)
                {
                    if (child.Attributes.Count > 0 && grandchild.Attributes != null)
                    {
                        LabelColorPair l = new LabelColorPair(grandchild.InnerText, grandchild.Attributes[0].InnerText);
                        h.Add(l);
                    }

                    foreach (AnnoTrack at in this.anno_tracks)
                    {
                        if (child.Attributes[1].InnerText == at.TierId && at.isDiscrete)
                        {
                            at.BackgroundColor = (SolidColorBrush)(new BrushConverter().ConvertFrom(child.Attributes[0].InnerText));
                            at.Background = at.BackgroundColor;

                            foreach (AnnoListItem ali in at.AnnoList)
                            {
                                foreach (LabelColorPair l in h)
                                {
                                    if (ali.Label == l.Label)
                                    {
                                        ali.Bg = l.Color;
                                    }
                                }
                            }
                            h.Clear();
                            h = new HashSet<LabelColorPair>();
                        }
                    }
                }
            }

            XmlNode cont = doc.SelectSingleNode("//continuous");

            foreach (XmlNode child in cont)
            {
                double samplerate = 1; ;
                StreamReader sr = new StreamReader(child.Attributes[1].InnerText, System.Text.Encoding.Default);
                string line = sr.ReadLine();

                string[] data = line.Split(';');
                try
                {
                    double start = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                    line = sr.ReadLine();
                    data = line.Split(';');
                    double start2 = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                    samplerate = start2 - start;
                }
                catch
                {
                    MessageBox.Show("Error reading continuous file");
                }
                sr.Close();
                loadCSVAnnotation(child.Attributes[1].InnerText, samplerate, "continuous");
            }
            //}
            //catch (Exception ex)
            //{
            //    ViewTools.ShowErrorMessage(ex.ToString());
            //}
        }

        private void annoTrackGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.view.navigator.askforlabels.IsChecked == true) AnnoTrack.askforlabel = true;
            else AnnoTrack.askforlabel = false;

            if (e.RightButton == MouseButtonState.Pressed) mouseDown = true;

            if (AnnoTrack.GetSelectedTrack() != null)
            {
                if (AnnoTrack.GetSelectedTrack().isDiscrete)
                {
                    double pos = e.GetPosition(this.view.trackControl.trackGrid).X;
                    annoCursor.X = pos;
                    Time.CurrentSelectPosition = pos;

                    annoCursor.Visibility = Visibility.Visible;
                    double time = Time.TimeFromPixel(pos);
                    this.view.trackControl.annoPositionLabel.Text = ViewTools.FormatSeconds(time);
                }
                else
                {
                    annoCursor.X = 0;
                    double time = Time.TimeFromPixel(0);
                    annoCursor.Visibility = Visibility.Hidden;
                    this.view.trackControl.annoPositionLabel.Text = ViewTools.FormatSeconds(time);
                }
            }
        }

        private void signalTrackGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (AnnoTrack.GetSelectedSegment() != null && Mouse.DirectlyOver.GetType() != AnnoTrack.GetSelectedSegment().GetType() || AnnoTrack.GetSelectedSegment() == null)
                {
                    AnnoTrack.UnselectSegment();
                    bool is_playing = IsPlaying();
                    if (is_playing)
                    {
                        Stop();
                    }

                    double pos = e.GetPosition(this.view.trackControl.trackGrid).X;
                    signalCursor.X = pos;
                    Time.CurrentPlayPosition = ViewHandler.Time.TimeFromPixel(signalCursor.X);
                    Time.CurrentPlayPositionPrecise = ViewHandler.Time.TimeFromPixel(signalCursor.X);
                    media_list.move(ViewHandler.Time.TimeFromPixel(pos));
                    double time = Time.TimeFromPixel(pos);
                    this.view.trackControl.signalPositionLabel.Text = ViewTools.FormatSeconds(time);

                    if (is_playing)
                    {
                        Play();
                    }
                }
            }
        }

        private void annoTrackGrid_MouseUp(object sender, MouseEventArgs e)
        {
            mouseDown = false;
            if (AnnoTrack.askforlabel == true)
                ShowLabelBox();
        }

        private void ShowLabelBox()

        {
            if (AnnoTrack.GetSelectedTrack() != null)
            {
                if (AnnoTrack.GetSelectedTrack().isDiscrete)
                {
                    AnnoTrackStatic.used_labels.Clear();

                    foreach (AnnoListItem item in AnnoTrack.GetSelectedTrack().AnnoList)
                    {
                        if (item.Label != "")
                        {
                            LabelColorPair l = new LabelColorPair(item.Label, item.Bg);
                            bool detected = false;
                            foreach (LabelColorPair p in AnnoTrackStatic.used_labels)
                            {
                                if (p.label == l.label)
                                {
                                    detected = true;
                                }
                            }

                            if (detected == false) AnnoTrackStatic.used_labels.Add(l);
                        }
                    }

                    LabelInputBox inputBox = new LabelInputBox("Input", "Enter a new label name", AnnoTrack.GetSelectedSegment().Item.Label, AnnoTrackStatic.used_labels, 1, "", "", true);
                    inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    inputBox.ShowDialog();
                    inputBox.Close();
                    if (inputBox.DialogResult == true)
                    {
                        rename(inputBox.Result());
                        AnnoTrack.GetSelectedSegment().Item.Bg = inputBox.Color();

                        AnnoTrackStatic.used_labels.Clear();
                        foreach (AnnoListItem a in AnnoTrack.GetSelectedTrack().AnnoList)
                        {
                            if (a.Label == AnnoTrack.GetSelectedSegment().Item.Label) a.Bg = AnnoTrack.GetSelectedSegment().Item.Bg;

                            if (a.Label != "")
                            {
                                LabelColorPair l = new LabelColorPair(a.Label, a.Bg);
                                bool detected = false;
                                foreach (LabelColorPair p in AnnoTrackStatic.used_labels)
                                {
                                    if (p.label == l.label)
                                    {
                                        detected = true;
                                    }
                                }

                                if (detected == false) AnnoTrackStatic.used_labels.Add(l);
                            }
                        }

                        AnnoTrack.GetSelectedTrack().track_used_labels = AnnoTrackStatic.used_labels;
                    }
                }
            }
        }

        public void rename(string label)
        {
            AnnoTrack.GetSelectedSegment().Item.Label = label;
            AnnoTrack.GetSelectedSegment().Text = label;
            AnnoTrack.GetSelectedSegment().update();
        }

        private void annoTrackGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                double pos = e.GetPosition(this.view.trackControl.trackGrid).X;
                annoCursor.X = pos;
                Time.CurrentSelectPosition = pos;
                double time = Time.TimeFromPixel(pos);
                this.view.trackControl.annoPositionLabel.Text = ViewTools.FormatSeconds(time);
            }
            if ((e.RightButton == MouseButtonState.Pressed || e.LeftButton == MouseButtonState.Pressed) && this.view.navigator.framewisebox.IsChecked == true)
            {
                if (media_list.Medias.Count > 0)
                {
                    media_list.move(Time.TimeFromPixel(e.GetPosition(this.view.trackControl.trackGrid).X));
                    moveCursorTo(Time.TimeFromPixel(e.GetPosition(this.view.trackControl.trackGrid).X));
                    Stop();
                }
            }

            if (e.RightButton == MouseButtonState.Released && mouseDown == true)
            {
                mouseDown = false;

                if (this.view.navigator.framewisebox.IsChecked == true)
                {
                    bool is_playing = IsPlaying();

                    if (!is_playing)
                    {
                        Play();
                    }
                }
                if (this.view.navigator.askforlabels.IsChecked == true)
                {
                    if (AnnoTrack.GetSelectedTrack() != null)
                    {
                        if (AnnoTrack.GetSelectedTrack().isDiscrete)
                        {
                            AnnoTrackStatic.used_labels.Clear();
                            foreach (AnnoListItem item in AnnoTrack.GetSelectedTrack().AnnoList)
                            {
                                if (item.Label != "")
                                {
                                    LabelColorPair l = new LabelColorPair(item.Label, item.Bg);
                                    bool detected = false;
                                    foreach (LabelColorPair p in AnnoTrackStatic.used_labels)
                                    {
                                        if (p.label == l.label)
                                        {
                                            detected = true;
                                        }
                                    }

                                    if (detected == false) AnnoTrackStatic.used_labels.Add(l);
                                }
                            }

                            LabelInputBox inputBox = new LabelInputBox("Input", "Enter a new label name", "Label", AnnoTrackStatic.used_labels, 1, "", "", true);
                            inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            inputBox.ShowDialog();
                            inputBox.Close();
                            if (inputBox.DialogResult == true && AnnoTrack.GetSelectedSegment() != null)
                            {
                                AnnoTrack.GetSelectedSegment().Item.Label = inputBox.Result();
                                AnnoTrack.GetSelectedSegment().Item.Bg = inputBox.Color();

                                foreach (AnnoListItem a in AnnoTrack.GetSelectedTrack().AnnoList)
                                {
                                    if (a.Label == AnnoTrack.GetSelectedSegment().Item.Label) a.Bg = AnnoTrack.GetSelectedSegment().Item.Bg;
                                }
                            }
                        }
                    }
                }
            }
        }

        private void initCursor()
        {
            //  this.view.trackControl.signalTrackControl.MouseDown += signalTrackGrid_MouseDown;
            this.view.trackControl.trackGrid.MouseDown += signalTrackGrid_MouseDown;
            this.view.trackControl.annoTrackControl.MouseDown += annoTrackGrid_MouseDown;
            this.view.trackControl.annoTrackControl.MouseMove += annoTrackGrid_MouseMove;
            this.view.trackControl.annoTrackControl.MouseRightButtonUp += annoTrackGrid_MouseUp;

            AdornerLayer cursorLayer = view.trackControl.adornerLayer.AdornerLayer;
            signalCursor = new Cursor(this.view.trackControl.trackGrid, Brushes.Red, 1.5);
            cursorLayer.Add(signalCursor);
            annoCursor = new Cursor(this.view.trackControl.trackGrid, Brushes.Green, 1.5);
            cursorLayer.Add(annoCursor);

            signalCursor.OnCursorChange += onCursorChange;
        }

        private void viewControl_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                if (filenames != null)
                {
                    this.view.Cursor = Cursors.Wait;
                    LoadFiles(filenames);
                    this.view.Cursor = Cursors.Arrow;
                }
            }
        }

        public void LoadFiles(string[] filenames)
        {
            foreach (string filename in filenames)
            {
                FileAttributes attr = File.GetAttributes(filename);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    string[] subfilenames = Directory.GetFiles(filename);
                    LoadFiles(subfilenames);
                }
                else
                {
                    if (!loadFromFile(filename))
                    {
                        //ViewTools.ShowErrorMessage("could not load " + filenames[i]);
                    }
                }
            }
        }

        public bool loadFromFile(string filename)
        {
            bool loaded = false;

            ssi_file_type ftype = ssi_file_type.UNKOWN;

            int index = filename.LastIndexOf('.');
            if (index > 0)
            {
                string type = filename.Substring(index + 1);
                switch (type)
                {
                    case "avi":
                    case "wmv":
                    case "mp4":
                    case "mov":
                    case "MOV":
                    case "m4a":
                    case "mkv":
                    case "mpg":
                    case "JPG":
                    case "JPEG":
                    case "PNG":
                    case "jpg":
                    case "png":
                    case "jpeg":
                    case "gif":

                        ftype = ssi_file_type.VIDEO;
                        break;

                    case "csv":
                        ftype = ssi_file_type.CSV;
                        break;

                    case "wav":
                        ftype = ssi_file_type.AUDIO;
                        break;

                    case "anno":
                        ftype = ssi_file_type.ANNO;
                        break;

                    case "stream":
                        ftype = ssi_file_type.STREAM;
                        break;

                    case "events":
                        ftype = ssi_file_type.EVENTS;
                        break;

                    case "eaf":
                        ftype = ssi_file_type.EAF;
                        break;

                    case "anvil":
                        ftype = ssi_file_type.ANVIL;
                        break;

                    case "nova":
                    case "vui":
                        ftype = ssi_file_type.VUI;
                        break;
                }
            }

            switch (ftype)
            {
                case ssi_file_type.VIDEO:
                    loadMedia(filename, true);
                    loaded = true;
                    break;

                case ssi_file_type.CSV:

                    //Read first line, check if format is an annotation, else interpret it as external csv
                    //Read second line to check for sample rate (only relevant for continous files)
                    string csvanno = "^([0-9]+.[0-9]+|[0-9]+);([0-9]+.[0-9]+|[0-9]+);.*";
                    string csvcont = "^([0-9]+.[0-9]+|[0-9]+;)(.)[^;]*";
                    string csvcontnew = "^((-?)[0-9]+.[0-9]+|[0-9]+;)+([0-9]+|[0-9]+;)(.)[^;];\\#.*";

                    string type = "";

                    Regex reg = new Regex(csvanno);
                    Regex regcont = new Regex(csvcont);
                    Regex regcontnew = new Regex(csvcontnew);
                    StreamReader sr = new StreamReader(filename, System.Text.Encoding.Default);
                    string line = sr.ReadLine();
                    double samplerate = 1.0;
                    bool iscontinouswithtier = regcontnew.IsMatch(line);

                    if (reg.IsMatch(line) && !iscontinouswithtier) type = "semicolon";
                    else if ((regcont.IsMatch(line) || iscontinouswithtier))
                    {
                        string[] data = line.Split(';');
                        try
                        {
                            double start = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                            line = sr.ReadLine();
                            data = line.Split(';');
                            double start2 = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                            samplerate = start2 - start;
                            type = "continuous";
                        }
                        catch
                        {
                            MessageBox.Show("Error reading continuous file");
                        }
                    }

                    sr.Close();

                    if (type == "continuous" || type == "semicolon")
                    {
                        loadCSVAnnotation(filename, samplerate, type);
                    }
                    else
                    {
                        loadCSV(filename);
                    }
                    loaded = true;

                    break;

                case ssi_file_type.AUDIO:
                    loadWav(filename);
                    loadMedia(filename, false);
                    loaded = true;
                    break;

                case ssi_file_type.ANNO:
                    loadAnno(filename);
                    loaded = true;
                    break;

                case ssi_file_type.STREAM:
                    loadStream(filename);
                    loaded = true;
                    break;

                case ssi_file_type.EVENTS:
                    loadEvents(filename);
                    loaded = true;
                    break;

                case ssi_file_type.EAF:
                    loadElan(filename);
                    loaded = true;
                    break;

                case ssi_file_type.ANVIL:
                    loadAnvil(filename);
                    loaded = true;
                    break;

                case ssi_file_type.VUI:
                    loadConfig(filename);
                    loaded = true;
                    break;

                default:
                    MessageBox.Show("File Format not supported");

                    break;
            }

            return loaded;
        }

        private void annoNameLabel_Click(object sender, RoutedEventArgs e)
        {
            if (AnnoTrack.GetSelectedTrack() != null)
            {
                LabelInputBox inputBox;
                if (AnnoTrack.GetSelectedTrack().isDiscrete)
                {
                    inputBox = new LabelInputBox("Enter Tier Name", "Enter Tier Name", AnnoTrack.GetSelectedTrack().TierId, null, 1, "", "", true);
                }
                else
                {
                    List<String> list = ContinuousColorTheme.List;
                    HashSet<LabelColorPair> h = new HashSet<LabelColorPair>();

                    foreach (String i in list)
                    {
                        LabelColorPair l = new LabelColorPair(i, "");
                        h.Add(l);
                    }

                    inputBox = new LabelInputBox("Enter Tier Name", "Enter Tier Name", AnnoTrack.GetSelectedTrack().TierId, h, 1, "", "", false);
                }

                inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                inputBox.ShowDialog();
                inputBox.Close();
                if (inputBox.DialogResult == true)
                {
                    this.view.trackControl.annoNameLabel.Content = "#" + inputBox.Result();
                    AnnoTrack.GetSelectedTrack().TierId = inputBox.Result();
                    AnnoTrack.GetSelectedTrack().AnnoList.Name = AnnoTrack.GetSelectedTrack().TierId;
                    if (AnnoTrack.GetSelectedTrack().isDiscrete)
                    {
                        AnnoTrack.GetSelectedTrack().BackgroundColor = (SolidColorBrush)(new BrushConverter().ConvertFrom(inputBox.Color()));
                        AnnoTrack.GetSelectedTrack().Background = AnnoTrack.GetSelectedTrack().BackgroundColor;
                    }
                    else
                    {
                        AnnoTrack.GetSelectedTrack().Background = resultbrush(inputBox.SelectedItem());
                        AnnoTrack.GetSelectedTrack().ContiniousBrush = resultbrush(inputBox.SelectedItem());
                    }

                    //  this.current_anno.Name = inputBox.Result();
                }
            }
        }

        private LinearGradientBrush resultbrush(string compare)
        {
            LinearGradientBrush br = new LinearGradientBrush();
            if (compare == "RedBlue")
            {
                br = ContinuousColorTheme.RedBlue;
            }
            else if (compare == "BlueRed")
            {
                br = ContinuousColorTheme.BlueRed;
            }
            else if (compare == "Heatmap")
            {
                br = ContinuousColorTheme.Heatmap;
            }
            br.Opacity = .75;
            return br;
        }

        private void editAnnoButton_Click(object sender, RoutedEventArgs e)
        {
            string label = view.annoListControl.editTextBox.Text;
            foreach (AnnoListItem item in view.annoListControl.annoDataGrid.SelectedItems)
            {
                item.Label = label;
            }
            //view.annoListControl.annoDataGrid.editLabel(label);
        }

        public void saveAnnoAs()
        {
            if (this.current_anno != null)
            {
                string filename = ViewTools.SaveFileDialog(this.current_anno.SampleAnnoPath, ".csv");
                saveAnno(filename);
            }
        }

        public void saveContinousAnnoAs()
        {
            if (this.current_anno != null)
            {
                string filename = ViewTools.SaveFileDialog(this.current_anno.SampleAnnoPath, ".csv");
                saveAnnoContinous(filename);
            }
        }

        public void addFiles()
        {
            string[] filenames = ViewTools.OpenFileDialog("Viewable files (*.stream,*.anno,*.wav,*.avi,*.wmv)|*.stream;*.anno;*.eaf;*.csv;*.wav;*.avi;*.wmv;*mp4;*mpg;*mkv;*vui|Signal files (*.stream)|*.stream|Annotation files (*.csv;*.anno;*.txt)|*.anno;*csv;*txt|Wave files (*.wav)|*.wav|Video files(*.avi,*.wmv,*.mp4;*.mov)|*.avi;*.wmv;*.mp4;*.mov|All files (*.*)|*.*", true);
            if (filenames != null)
            {
                this.view.Cursor = Cursors.Wait;
                LoadFiles(filenames);
                this.view.Cursor = Cursors.Arrow;
            }
        }

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            clear();
        }

        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            saveAll();
        }

        private void saveProject_Click(object sender, RoutedEventArgs e)
        {
            saveProject();
        }

        private void newAnnoButton_Click(object sender, RoutedEventArgs e)
        {
            newAnno(true);
        }

        private void newAnnoContButton_Click(object sender, RoutedEventArgs e)
        {
            if (Time.TotalDuration > 0)
            {
                List<String> list = ContinuousColorTheme.List;
                HashSet<LabelColorPair> h = new HashSet<LabelColorPair>();

                foreach (String i in list)
                {
                    LabelColorPair l = new LabelColorPair(i, "");
                    h.Add(l);
                }

                //TODO

                string defaultsr = "40";

                //check if a video is loaded, and if so use it's sample rate as default
                foreach (IMedia m in media_list.Medias)
                {
                    if (m.IsVideo())
                    {
                        defaultsr = (1000.0 / m.GetSampleRate()).ToString();
                        break;
                    }
                }

                LabelInputBox inputBox = new LabelInputBox("New Continous Tier", "Enter Samplerate in ms, Min and Max Value", "0", h, 3, "1", defaultsr);
                h.Clear();
                inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                inputBox.ShowDialog();
                inputBox.Close();
                if (inputBox.DialogResult == true)
                {
                    double samplerate;
                    if (double.TryParse(inputBox.Result3(), out samplerate))
                    {
                        newAnno(false, samplerate / 1000, double.Parse(inputBox.Result()), double.Parse(inputBox.Result2()), resultbrush(inputBox.SelectedItem()));
                    }
                }
            }
            else MessageBox.Show("There is nothing to annotate!", "Please load data first", MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void saveAnnoButton_Click(object sender, RoutedEventArgs e)
        {
            saveAnno(null);
        }

        private void saveAnnoAsButton_Click(object sender, RoutedEventArgs e)
        {
            if (AnnoTrack.GetSelectedTrack() != null)
            {
                if (AnnoTrack.GetSelectedTrack().isDiscrete) saveAnnoAs();
                else saveContinousAnnoAs();
            }
        }

        private void removeTier()
        {
            AnnoTrack at = AnnoTrack.GetSelectedTrack();

            if (at != null)
            {
                MessageBoxResult mb = MessageBoxResult.No;
                if (at.AnnoList.HasChanged)
                {
                    mb = MessageBox.Show("Save annotations on tier #" + at.TierId + " first?", "Confirm", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                    if (mb == MessageBoxResult.Yes)
                    {
                        if (at.isDiscrete) saveAnno();
                        else saveAnnoContinous();

                        at.AnnoList.HasChanged = false;
                    }
                }

                if (mb != MessageBoxResult.Cancel)
                {
                    this.view.trackControl.annoTrackControl.annoTrackGrid.RowDefinitions[Grid.GetRow(at)].Height = new GridLength(0);
                    if (this.view.trackControl.annoTrackControl.annoTrackGrid.Children.IndexOf(at) > 0)
                    {
                        this.view.trackControl.annoTrackControl.annoTrackGrid.Children.RemoveAt(this.view.trackControl.annoTrackControl.annoTrackGrid.Children.IndexOf(at) - 1);
                        this.view.trackControl.annoTrackControl.annoTrackGrid.Children.RemoveAt(this.view.trackControl.annoTrackControl.annoTrackGrid.Children.IndexOf(at));
                    }

                    AnnoTrack.UnselectTrack();
                    at.Children.Clear();
                    at.AnnoList.Clear();

                    anno_tracks.Remove(at);
                    this.view.trackControl.annoNameLabel.Content = "#NoTier";
                }
            }
        }

        private void closeTier_Click(object sender, RoutedEventArgs e)
        {
            removeTier();
        }

        private void mongodb_Store(object sender, RoutedEventArgs e)
        {
            if (AnnoTrack.GetSelectedTrack() != null)
            {
              

                //Todo, use e.g. folder name for default suggestion

                string table = "SessionId";

                if (media_list.Medias.Count > 0)
                    table = new DirectoryInfo(media_list.Medias[0].GetFilepath()).Parent.Parent.Name + "_" + new DirectoryInfo(media_list.Medias[0].GetFilepath()).Parent.Name;
                else if (signals.Count > 0)
                    table = new DirectoryInfo(signals[0].Filepath).Parent.Name;
     




                LabelInputBox inputBox = new LabelInputBox("MongoDB Connection", "Enter Ip and Port of Server, and Database", mongodbip, null, 2, table);
                inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                inputBox.ShowDialog();
                inputBox.Close();
                if (inputBox.DialogResult == true)
                {
                    mongodbip = inputBox.Result();
                    table = inputBox.Result2();
                

                try
                {
                    DatabaseHandler db = new DatabaseHandler("mongodb://" + mongodbip);
                    db.StoretoDatabase(table, AnnoTrack.GetSelectedTrack().AnnoList);
                    MessageBox.Show("Track " + AnnoTrack.GetSelectedTrack().TierId + " has been stored in the Database " + table);
                }
                catch
                {
                    MessageBox.Show("Could not connect to MongoDB Server");
                }
            }
            else
            {
                MessageBox.Show("Select  Annotation Track first");
            }
            }
        }

        private void mongodb_Load(object sender, RoutedEventArgs e)
        {
        
            LabelInputBox inputBox = new LabelInputBox("MongoDB Connection", "Enter Ip and Port of Server", mongodbip, null, 1);
            inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            inputBox.ShowDialog();
            inputBox.Close();
            if (inputBox.DialogResult == true)
            {
                mongodbip = inputBox.Result();

                DatabaseHandler db = new DatabaseHandler("mongodb://" + mongodbip);                                                                    
                AnnoList anno = db.LoadfromDatabase();
                if (anno != null)
                {
                    //Todo.. checking for continuous or discrete labels based on the first 2 samples (could be done nicer)
                    if (anno[0].Start + anno[0].Duration == anno[1].Start && anno[1].Start + anno[1].Duration == anno[2].Start)
                    {
                        anno.isDiscrete = false;
                        handleAnnotation(anno, "database", anno[0].Duration);
                    }
                    else
                    {
                        anno.isDiscrete = true;
                        anno.Filepath = "db";
                        anno.SampleAnnoPath = "db";
                        double maxdur = 0;

                        foreach (AnnoListItem ali in anno)
                        {
                            if (ali.Stop > maxdur)
                            {
                                maxdur = ali.Stop;
                            }
                        }
                        if (anno != null)
                        {
                            setAnnoList(anno);
                            addAnno(anno, anno.isDiscrete, 1);
                        }

                        updateTimeRange(maxdur);
                    }
                }
            }
        }

        private void convertocontanno_Click(object sender, RoutedEventArgs e)
        {
            if (SignalTrack.SelectedSignal != null)
            {
                Signal s = SignalTrack.SelectedSignal;
                AnnoList al = new AnnoList();
                AnnoListItem ali;

                double dur = (1000.0 / s.rate) / 1000.0;
                if (s != null)
                {
                    string tier = s.Name;

                    for (int i = 0; i < s.number; i++)
                    {
                        ali = new AnnoListItem(i * dur, dur, s.data[i * s.dim + s.ShowDim].ToString(), "", s.Name + "_dim" + s.ShowDim);
                        al.Add(ali);
                        al.Name = ali.Tier;
                    }

                    al.Lowborder = s.min[s.ShowDim];
                    al.Highborder = s.max[s.ShowDim];

                    AnnoList result = al.saveContinousToFile();

                    if (result != null)
                    {
                        MessageBoxResult mb = MessageBoxResult.None;
                        mb = MessageBox.Show("Successfully converted stream to anno. Load the anno?", "Success", MessageBoxButton.YesNo);
                        if (mb == MessageBoxResult.Yes)
                        {
                            loadCSVAnnotation(result.Filename, dur, "continuous");
                        }
                    }
                }
            }
        }

        private void convertosignal_Click(object sender, RoutedEventArgs e)
        {
            AnnoTrack at = AnnoTrack.GetSelectedTrack();
            if (at.isDiscrete) MessageBox.Show("Selected a continious track to convert to ssi stream");
            else
            {
                double sr = 1000.0 / (at.AnnoList[0].Duration * 1000);
                double from = 0.0;
                double to = at.AnnoList[at.AnnoList.Count - 1].Stop;
                int num = at.AnnoList.Count;
                string ftype = "ASCII";
                string type = "FLOAT";
                int by = sizeof(float);
                int dim = 1;
                int ms = Environment.TickCount;

                //if (at.AnnoList.Filepath == null)
                //{
                if (at.AnnoList.HasChanged)
                {
                    MessageBoxResult m = MessageBoxResult.None;
                    m = MessageBox.Show("You need to save continous annotations on tier #" + at.TierId + " first", "Confirm", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    if (m == MessageBoxResult.OK)
                    {
                        saveContinousAnnoAs();
                        at.AnnoList.HasChanged = false;
                    }
                }
                //string firstmediadir = "";
                //if (media_list.Medias.Count > 0) firstmediadir = media_list.Medias[0].GetFolderepath();
                //else if (signals.Count > 0) firstmediadir = signals[0].Folderpath;
                //filepath = firstmediadir;
                //}

                //else {
                //    filepath = at.AnnoList.Filepath;
                //}

                string filename = Path.GetDirectoryName(at.AnnoList.Filepath) + "\\" + at.TierId + ".stream";

                StreamWriter swheader = new StreamWriter(filename, false, System.Text.Encoding.Default);
                swheader.WriteLine("<?xml version=\"1.0\" ?>");
                swheader.WriteLine("<stream ssi-v=\"2\">");
                swheader.WriteLine("\t<info ftype=\"" + ftype + "\" sr=\"" + sr.ToString("0.000000", CultureInfo.InvariantCulture) + "\" dim=\"" + dim.ToString() + "\" byte=\"" + by.ToString() + "\" type=\"" + type + "\" />");
                swheader.WriteLine("\t<time ms=\"" + ms + "\" local=\"" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "\" system=\"" + DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss") + "\"/>");
                swheader.WriteLine("\t<chunk from=\"" + from.ToString("0.000000", CultureInfo.InvariantCulture) + "\" to=\"" + to.ToString("0.000000", CultureInfo.InvariantCulture) + "\" byte=\"" + "0" + "\" num=\"" + num + "\"/>");

                swheader.WriteLine("</stream>");

                swheader.Close();

                StreamWriter swdata = new StreamWriter(filename + "~", false, System.Text.Encoding.Default);
                foreach (AnnoListItem i in at.AnnoList)
                {
                    swdata.WriteLine(i.Label);
                }
                swdata.Close();

                MessageBoxResult mb = MessageBoxResult.None;
                mb = MessageBox.Show("Successfully converted anno to stream. Load the stream?", "Success", MessageBoxButton.YesNo);
                if (mb == MessageBoxResult.Yes)
                {
                    loadStream(filename);
                }
            }
        }

        private void converttodiscrete_Click(object sender, RoutedEventArgs e)
        {
            AnnoTrack at = AnnoTrack.GetSelectedTrack();
            if (at != null)
            {
                MessageBoxResult mb = MessageBoxResult.None;
                if (!at.isDiscrete)
                {
                    if (at.AnnoList.HasChanged)
                    {
                        mb = MessageBox.Show("Save continous annotations on tier #" + at.TierId + " first?", "Confirm", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

                        if (mb == MessageBoxResult.Yes)
                        {
                            saveContinousAnnoAs();
                            at.AnnoList.HasChanged = false;
                        }
                    }

                    if (mb != MessageBoxResult.Cancel)
                    {
                        List<string> classes = new List<string>();
                        List<double> upperthresholds = new List<double>();
                        double offset = 0.0;
                        LabelInputBox inputBox = new LabelInputBox("Enter Lables and UpperThresholds seperated by ;", "Enter offset in Seconds if needed", "Low; Medium; High", null, 3, "-0.0", "0.33;0.66;1.0");
                        inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        inputBox.ShowDialog();
                        inputBox.Close();
                        if (inputBox.DialogResult == true)
                        {
                            string[] data = inputBox.Result().Split(';');
                            for (int i = 0; i < data.Length; i++)
                            {
                                classes.Add(data[i]);
                            }

                            string[] data2 = inputBox.Result3().Split(';');
                            for (int i = 0; i < data2.Length; i++)
                            {
                                double thresh = -1;
                                double.TryParse(data2[i], out thresh);
                                if (thresh > -1)
                                {
                                    upperthresholds.Add(thresh);
                                }
                                else
                                {
                                    MessageBox.Show("Entries in wrong format");
                                }
                            }
                            //If sombody forgets the 1.0
                            if (data2.Length == data.Length - 1) upperthresholds.Add(1.0);
                            else if (data2.Length == data.Length + 1) classes.Add("Rest");
                            else if (data2.Length != data.Length)
                            {
                                MessageBox.Show("Number of labels does not match number of threshholds");
                            }

                            double off = 0.0;
                            double.TryParse(inputBox.Result2(), out off);
                            offset = off;
                        }
                        Mouse.SetCursor(System.Windows.Input.Cursors.No);

                        AnnoList discretevalues = new AnnoList();
                        at.isDiscrete = true;

                        double lowthres = -Double.MaxValue;
                        double highthres = 1.0;

                        foreach (AnnoListItem ali in at.AnnoList)
                        {
                            double val = double.Parse(ali.Label);

                            for (int i = 0; i < classes.Count; i++)
                            {
                                highthres = upperthresholds[i];
                                if (i > 0) lowthres = upperthresholds[i - 1];
                                else lowthres = -Double.MaxValue;

                                if (val > lowthres && val < highthres)
                                {
                                    if (discretevalues.Count > 0 && discretevalues[discretevalues.Count - 1].Label == classes[i])
                                    {
                                        discretevalues[discretevalues.Count - 1].Stop = discretevalues[discretevalues.Count - 1].Stop + ali.Duration;
                                    }
                                    else
                                    {
                                        AnnoListItem newItem = ali;
                                        newItem.Start = ali.Start + offset;
                                        if (newItem.Start < 0.0)
                                        {
                                            newItem.Duration = ali.Duration + offset + newItem.Start;
                                            newItem.Start = 0.0;

                                            newItem.Stop = newItem.Duration;
                                        }
                                        newItem.Stop = ali.Stop + offset;
                                        newItem.Label = classes[i];
                                        if (newItem.Duration > 0.0) discretevalues.Add(newItem);
                                    }
                                    break;
                                }
                            }
                        }

                        at.Children.Clear();
                        at.AnnoList.Clear();

                        foreach (AnnoListItem ali in discretevalues)
                        {
                            at.AnnoList.Add(ali);
                            at.addSegment(ali);
                        }

                        Mouse.SetCursor(System.Windows.Input.Cursors.Arrow);
                    }
                }
                else
                {
                    MessageBox.Show("Tier is already discrete");
                }
            }
        }

        private void helpMenu_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Shortcuts:\n\nalt + return to enter fullscreen, esc to close fullscreen\nleftctrl for continuous anno mode, again to close\nalt+click or W on discrete anno to change label/color\nDel on Anno to delete Anno, on tier to delete tier\nalt + right/left to move signalmarker framewise\nshift + alt + right/left to move annomarker framewise\nQ to move signalmarker to start and annomarker to end of selected Segment\nE move annomarker to start and signalmarker to end of selected Segment\na for new Anno between boths markers\nSpace Play/Pause media ", "Quick Reference");
        }

        private void tierMenu_Click(object sender, RoutedEventArgs e)
        {
            AnnoTrack a = AnnoTrack.GetSelectedTrack();
            if (a != null)
            {
                if (a.isDiscrete)
                {
                    this.view.convertodiscretemenu.IsEnabled = false;
                    this.view.convertosignalemenu.IsEnabled = false;
                }
                else if (!a.isDiscrete)
                {
                    this.view.convertodiscretemenu.IsEnabled = true;
                    this.view.convertosignalemenu.IsEnabled = true;
                }

                this.view.savetiermenu.IsEnabled = true;
            }
        }

        private void exportSamplesButton_Click(object sender, RoutedEventArgs e)
        {
            ExportSamplesWindow window = new ExportSamplesWindow();
            foreach (AnnoTrack a in this.anno_tracks)
            {
                if (a.AnnoList.SampleAnnoPath != null)
                {
                    //TODO find tiers

                    window.control.annoComboBox.Items.Add(a.AnnoList.SampleAnnoPath + "#" + a.TierId);
                }
            }
            foreach (Signal signal in signals)
            {
                window.control.signalAvailableListBox.Items.Add(signal.Filepath);
            }
            window.ShowDialog();
        }

        private void calculatepraat_Click(object sender, RoutedEventArgs e)
        {
            if (SignalTrack.SelectedSignal != null)
            {
                if (SignalTrack.SelectedSignal.IsAudio)
                {
                    try
                    {
                        Process process = new Process();
                        ProcessStartInfo startInfo = new ProcessStartInfo();
                        startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                        startInfo.FileName = "preannotation.exe";
                        startInfo.Arguments = SignalTrack.SelectedSignal.Filename + " " + SignalTrack.SelectedSignal.Filepath.Remove(SignalTrack.SelectedSignal.Filepath.Length - SignalTrack.SelectedSignal.Filename.Length - 1);
                        process.StartInfo = startInfo;
                        process.Start();
                    }
                    catch
                    {
                    }
                }
                else
                {
                    MessageBox.Show("Please select an audio signal to calculate Praat features");
                }
            }
        }

        private void exportSampledAnnotationsButton_Click(object sender, RoutedEventArgs e)
        {
            LabelInputBox inputBox = new LabelInputBox("Export Sampled Annotations", "Enter file seperator, samplerate in ms and default rest class", "100", null, 3, "notPresent", ";", false);
            inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            inputBox.ShowDialog();
            inputBox.Close();
            if (inputBox.DialogResult == true)
            {
                int samplerate;
                if (Int32.TryParse(inputBox.Result(), out samplerate))
                {
                    exportSampledAnnotations(samplerate, inputBox.Result3(), inputBox.Result2());
                }
            }
        }

        public void exportSampledAnnotations(int sr, string seperator, string restclass)
        {
            bool found = false;
            int chunksize = sr;

            List<string> columns = new List<string>();

            foreach (AnnoList anno in annos)
            {
                foreach (AnnoListItem ali in anno)
                {
                    if (!columns.Contains(ali.Tier)) columns.Add(ali.Tier);
                }
            }

            int currenttime = 0;
            string headline = "";

            foreach (string s in columns)
            {
                headline += s + seperator;
            }

            string firstmediadir = "";
            if (media_list.Medias.Count > 0) firstmediadir = media_list.Medias[0].GetFolderepath();
            else if (signals.Count > 0) firstmediadir = signals[0].Folderpath;

            string filepath = ViewTools.SaveFileDialog("SampledAnnotations_Export", "txt", firstmediadir, 3);

            try
            {
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(filepath, false))
                {
                    headline = headline.Remove(headline.Length - 1);
                    file.WriteLine(headline);
                    headline = "";
                    int maxdur = (int)(((Time.TotalDuration - 0.5) * 1000));
                    Mouse.SetCursor(System.Windows.Input.Cursors.Wait);
                    while (currenttime < maxdur)
                    {
                        foreach (string s in columns)
                        {
                            foreach (AnnoList anno in annos)
                            {
                                foreach (AnnoListItem ali in anno)
                                {
                                    if (ali.Tier == s && (ali.Start * 1000) - (ali.Duration * 1000) < currenttime && ali.Stop * 1000 > currenttime)
                                    {
                                        found = true;
                                        headline += ali.Label + seperator;
                                        break;
                                    }
                                    else found = false;
                                }
                                if (found) break;
                            }
                            if (!found) headline += restclass + seperator;
                        }

                        headline = headline.Remove(headline.Length - 1);
                        file.WriteLine(headline);
                        headline = "";
                        currenttime += chunksize;
                    }
                }
                Mouse.SetCursor(System.Windows.Input.Cursors.Arrow);
                MessageBox.Show("Sampled Annotations Data File successfully created!", "Sucess", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch
            {
                MessageBox.Show("Could not create Sampled Annotations Data File!", "Warning", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}