using Octokit;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;
using Tamir.SharpSsh;

namespace ssi
{
    public class ViewHandler
    {
        public static string BuildVersion = "0.9.9.4.1";

        private static ViewTime time = null;

        private enum ssi_file_type
        {
            UNKOWN = 0,
            CSV,
            AUDIO,
            VIDEO,
            ANNO,
            ANNOTATION,
            STREAM,
            EVENTS,
            EAF,
            ANVIL,
            ARFF,
            PROJECT,
            IGNORE
        }

        private static readonly string[] SSI_FILE_TYPE_NAME = { "ssi", "audio", "video", "anno", "stream", "events", "eaf", "anvil", "vui", "arff", "annotation" };

    


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
        private bool movemedialock = false;
        private double skelfps = 25;
        private double lasttimepos = 0;
        private string lastdlfile = null;
        private bool visualizeskel = false;
        private bool visualizepoints = false;
        public bool databaseloaded = false;
        private ViewControl view;
        private String annofilepath = "";
        public List<DatabaseMediaInfo> loadedDBmedia = null;
        private int numberofparalleldownloads = 0;
        private List<long> downloadsreceived = new List<long>();
        private List<long> downloadstotal = new List<long>();
        private List<string> filestoload = new List<string>();
        private List<DownloadStatus> downloads = new List<DownloadStatus>();
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        public AnnoTrackSegment temp_segment;

        public bool DatabaseLoaded
        {
            get { return databaseloaded; }
            set { databaseloaded = value; }
        }

        public class DownloadStatus
        {
            public string File;
            public string percent;
            public bool active;
        }

        public List<AnnoTrack> AnnoTracks
        {
            get { return anno_tracks; }
        }

        public AnnoTrack getAnnoTrackFromName(string name)
        {
            foreach(AnnoTrack anno in anno_tracks)
            {
                if (anno.AnnoList.Name == name)
                {
                    return anno;
                }
            }

            return null;
        }

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
            this.view.ShadowBoxCancel.Click += cancel_click;
            this.view.annoListControl.annoDataGrid.SelectionChanged += annoDataGrid_SelectionChanged;
            this.view.annoListControl.editButton.Click += editAnnoButton_Click;
            this.view.annoListControl.editTextBox.KeyDown += editTextBox_KeyDown;
            this.view.annoListControl.editTextBox.GotMouseCapture += editTextBox_focused;
            this.view.trackControl.CloseAnnotrackButton.Click += closeTier_Click;
            this.view.trackControl.annoNameLabel.Click += annoNameLabel_Click;
            this.view.navigator.newAnnoButton.Click += newAnnoButton_Click;

            this.view.clearMenu.Click += clearButton_Click;
            this.view.saveMenu.Click += saveButton_Click;
            this.view.saveProject.Click += saveProject_Click;
            this.view.settingsMenu.Click += settingsMenu_Click;
            this.view.exportSamples.Click += exportSamplesButton_Click;
            this.view.convertodiscretemenu.Click += converttodiscrete_Click;
            this.view.convertosignalemenu.Click += convertosignal_Click;
            this.view.exporttracktoxps.Click += exporttracktoxps_Click;
            this.view.exporttracktopng.Click += exporttracktopng_Click;
            this.view.exportsignaltoxps.Click += exportsignaltoxps_Click;
            this.view.exportsignaltopng.Click += exportsignaltopng_Click;
            this.view.exporttracktocsv.Click += exporttracktocsv_Click;
            // this.view.calculatepraat.Click += calculatepraat_Click;
            this.view.savetiermenu.Click += saveAnnoAsButton_Click;
            this.view.convertocontannoemenu.Click += convertToContinuousAnnotation_Click;
            this.view.mongodbmenu.Click += mongodb_Store;
            this.view.mongodbmenufinished.Click += mongodb_Store_Finished;
            this.view.mongodbmenu2.Click += mongodb_Load;
            this.view.mongodbmenushow.Click += mongodb_Show;
            this.view.mongodbCMLCompleteLearning.Click += mongodbCMLCompleteLearning_Click;
            this.view.mongodbCMLTrainandLabel.Click += mongodbCMLTrainandLabel_Click;
            this.view.mongodbCMLextract.Click += mongodbCMLExtract_Click;
            this.view.addmongodb.Click += mongodb_Add;
            this.view.mongodbfunctions.Click += mongodb_Functions;
            this.view.mongodbchangefolder.Click += mongodb_ChangeFolder;
            this.view.update.Click += update_Click;

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
            this.view.annoListControl.editComboBox.SelectionChanged += changed_annoschemeselectionbox;
            this.view.navigator.hideHighConf.Click += check_lowconfonly;
            this.view.Dispatcher.ShutdownStarted += ControlUnloaded;

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
            this.view.trackControl.timeTrackControl.rangeSlider.OnTimeRangeChanged += Time.timeRangeChanged;
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

            if(Properties.Settings.Default.CheckUpdateonStartup)
            {
                checkforUpdates(true);
            }

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
                    handlePlay();

                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.D0) || e.KeyboardDevice.IsKeyDown(Key.NumPad0))
                {
                    if (AnnoTrack.GetSelectedTrack().AnnoList.AnnotationType == AnnoType.DISCRETE || AnnoTrack.GetSelectedTrack().AnnoList.AnnotationType == AnnoType.FREE)
                    {
                        string label = "GARBAGE";
                        if (AnnoTrack.GetSelectedSegment() != null)
                        {
                            AnnoTrack.GetSelectedSegment().Item.Label = label;
                            AnnoTrack.GetSelectedSegment().Item.Bg = "#FF000000";
                            AnnoTrack.GetSelectedSegment().Item.Confidence = 1.0;
                        }
                    }
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.D1) || e.KeyboardDevice.IsKeyDown(Key.D2) || e.KeyboardDevice.IsKeyDown(Key.D3) || e.KeyboardDevice.IsKeyDown(Key.D4) ||
                    e.KeyboardDevice.IsKeyDown(Key.D5) || e.KeyboardDevice.IsKeyDown(Key.D6) || e.KeyboardDevice.IsKeyDown(Key.D7) || e.KeyboardDevice.IsKeyDown(Key.D8) || e.KeyboardDevice.IsKeyDown(Key.D9) ||
                    e.KeyboardDevice.IsKeyDown(Key.NumPad1) || e.KeyboardDevice.IsKeyDown(Key.NumPad2) || e.KeyboardDevice.IsKeyDown(Key.NumPad3) || e.KeyboardDevice.IsKeyDown(Key.NumPad4) || e.KeyboardDevice.IsKeyDown(Key.NumPad5) ||
                      e.KeyboardDevice.IsKeyDown(Key.NumPad6) || e.KeyboardDevice.IsKeyDown(Key.NumPad7) || e.KeyboardDevice.IsKeyDown(Key.NumPad8) || e.KeyboardDevice.IsKeyDown(Key.NumPad9))
                {
                    int index = 0;
                    if (e.Key - Key.D1 < 10 && AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors != null && AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors.Count > 0) index = Math.Min(AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors.Count - 1, e.Key - Key.D1);
                    else if (AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors != null) index = Math.Min(AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors.Count - 1, e.Key - Key.NumPad1);

                    if (index >= 0 && AnnoTrack.GetSelectedTrack().AnnoList.AnnotationType == AnnoType.DISCRETE)
                    {
                        string label = AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors.ElementAt(index).Label;
                        if (AnnoTrack.GetSelectedSegment() != null)
                        {
                            AnnoTrack.GetSelectedSegment().Item.Label = label;
                            AnnoTrack.GetSelectedTrack().Defaultlabel = label;

                            foreach (LabelColorPair lp in AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors)
                            {
                                if (label == lp.Label)
                                {
                                    AnnoTrack.GetSelectedSegment().Item.Bg = lp.Color;
                                    AnnoTrack.GetSelectedSegment().Item.Confidence = 1.0;
                                    AnnoTrack.GetSelectedTrack().DefaultColor = lp.Color;

                                    break;
                                }
                            }
                        }
                    }
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

                if (e.KeyboardDevice.IsKeyDown(Key.T) && e.KeyboardDevice.IsKeyDown(Key.Down))
                {
                    for (int i = 0; i < anno_tracks.Count; i++)
                    {
                        if (anno_tracks[i] == AnnoTrack.GetSelectedTrack() && i + 1 < anno_tracks.Count)
                        {
                            AnnoTrack.SelectTrack(anno_tracks[i + 1]);
                            AnnoTrack.SelectSegment(null);
                            break;
                        }
                    }
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.T) && e.KeyboardDevice.IsKeyDown(Key.Up))
                {
                    for (int i = 0; i < anno_tracks.Count; i++)
                    {
                        if (anno_tracks[i] == AnnoTrack.GetSelectedTrack() && i > 0)
                        {
                            AnnoTrack.SelectTrack(anno_tracks[i - 1]);
                            AnnoTrack.SelectSegment(null);
                            break;
                        }
                    }
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.C) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && AnnoTrack.GetSelectedTrack() != null && AnnoTrack.GetSelectedTrack().isDiscrete)
                {
                    if (AnnoTrack.GetSelectedSegment() != null)
                    {
                        temp_segment = AnnoTrack.GetSelectedSegment();
                        AnnoTrack.GetSelectedSegment().select(false);
                    }

                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.X) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && AnnoTrack.GetSelectedTrack() != null && AnnoTrack.GetSelectedTrack().isDiscrete)
                {
                    if (AnnoTrack.GetSelectedSegment() != null)
                    {
                        temp_segment = AnnoTrack.GetSelectedSegment();
                        AnnoTrack.OnKeyDownHandler(sender, e);
                    }

                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.V) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && AnnoTrack.GetSelectedTrack() != null  && AnnoTrack.GetSelectedTrack().isDiscrete)
                {
                    if (AnnoTrack.GetSelectedTrack() != null)
                    {
                        double start = Time.TimeFromPixel(annoCursor.X);
                        if (temp_segment != null) AnnoTrack.GetSelectedTrack().newAnnocopy(start, start + temp_segment.Item.Duration, temp_segment.Item.Label, temp_segment.Item.Bg, temp_segment.Item.Confidence);
                    }

                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyDown(Key.Z))
                {
                    if (AnnoTrack.GetSelectedTrack() != null)
                    {
                        AnnoTrack.GetSelectedTrack().UnDoObject.Undo(1);
                    }
                }

                if (e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyDown(Key.Y))
                {
                    if (AnnoTrack.GetSelectedTrack() != null)
                    {
                        AnnoTrack.GetSelectedTrack().UnDoObject.Redo(1);
                    }
                }

                if (e.KeyboardDevice.IsKeyDown(Key.LeftAlt) && e.KeyboardDevice.IsKeyDown(Key.Down))
                {
                    if (AnnoTrack.GetSelectedSegment() != null)
                    {
                        AnnoListItem temp = AnnoTrack.GetSelectedSegment().Item;

                        for (int i = 0; i < anno_tracks.Count; i++)
                        {
                            if (anno_tracks[i] == AnnoTrack.GetSelectedTrack() && i + 1 < anno_tracks.Count)
                            {
                                AnnoTrack.SelectTrack(anno_tracks[i + 1]);
                                AnnoTrack.SelectSegment(null);
                                if (!AnnoTrack.GetSelectedTrack().AnnoList.Contains(temp)) AnnoTrack.GetSelectedTrack().newAnnocopy(temp.Start, temp.Stop, temp.Label, temp.Bg);

                                break;
                            }
                        }
                    }
                    e.Handled = true;
                }

                if (e.KeyboardDevice.IsKeyDown(Key.LeftAlt) && e.KeyboardDevice.IsKeyDown(Key.Up))
                {
                    if (AnnoTrack.GetSelectedSegment() != null)
                    {
                        AnnoListItem temp = AnnoTrack.GetSelectedSegment().Item;

                        for (int i = 0; i < anno_tracks.Count; i++)
                        {
                            if (anno_tracks[i] == AnnoTrack.GetSelectedTrack() && i > 0)
                            {
                                AnnoTrack.SelectTrack(anno_tracks[i - 1]);
                                AnnoTrack.SelectSegment(null);
                                if (!AnnoTrack.GetSelectedTrack().AnnoList.Contains(temp)) AnnoTrack.GetSelectedTrack().newAnnocopy(temp.Start, temp.Stop, temp.Label, temp.Bg);
                                break;
                            }
                        }
                    }
                    e.Handled = true;
                }
            }
        }

        public void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!this.view.annoListControl.editTextBox.IsFocused)
            {
                if (e.KeyboardDevice.IsKeyDown(Key.S) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) && e.KeyboardDevice.IsKeyDown(Key.LeftShift))
                {
                    if (DatabaseLoaded)
                    {
                        mongodbStore(true);
                    }
                    else saveAnnoAs();
                }
                else if (e.KeyboardDevice.IsKeyDown(Key.S) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                {
                    if (DatabaseLoaded)
                    {
                        mongodbStore();
                    }
                    else saveAnno();
                }
                else if (e.KeyboardDevice.IsKeyDown(Key.Delete) || e.KeyboardDevice.IsKeyDown(Key.Back))
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

                if (e.KeyboardDevice.IsKeyDown(Key.R) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl))
                {
                    if (Properties.Settings.Default.DefaultDiscreteSampleRate != 0 && AnnoTrack.GetSelectedTrack().AnnoList.SR != Properties.Settings.Default.DefaultDiscreteSampleRate)
                    {
                        foreach (AnnoListItem ali in AnnoTrack.GetSelectedTrack().AnnoList)
                        {
                            if (ali.Start % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                            {
                                int round = (int)(ali.Start / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                                ali.Start = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                            }

                            if (ali.Stop % (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) != 0)
                            {
                                int round = (int)(ali.Stop / (1 / Properties.Settings.Default.DefaultDiscreteSampleRate) + 0.5);
                                ali.Stop = round * (1 / Properties.Settings.Default.DefaultDiscreteSampleRate);
                            }

                            ali.Duration = ali.Stop - ali.Start;
                        }
                        AnnoTrack.GetSelectedTrack().AnnoList.SR = Properties.Settings.Default.DefaultDiscreteSampleRate;
                    }
                }

                if (e.KeyboardDevice.IsKeyDown(Key.R) && e.KeyboardDevice.IsKeyDown(Key.LeftCtrl) || e.KeyboardDevice.IsKeyDown(Key.F5))
                {
                    AnnoTrack track = AnnoTrack.GetSelectedTrack();
                    if (track != null)
                    {
                        if (DatabaseLoaded)
                            reloadAnnoDB(track);
                        else
                            reloadAnno(track.AnnoList.Filepath);
                    }
                    e.Handled = true;
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

                if ((e.KeyboardDevice.IsKeyDown(Key.W) || e.KeyboardDevice.IsKeyDown(Key.Return) && !keyDown && AnnoTrack.GetSelectedTrack() != null) && AnnoTrack.GetSelectedTrack().isDiscrete)
                {
                    if (AnnoTrack.GetSelectedSegment() == null)
                    {
                        AnnoTrack.GetSelectedTrack().newAnnokey();
                    }
                    else
                    {
                        ShowLabelBox();
                    }
                    if (AnnoTrack.GetSelectedSegment() != null) AnnoTrack.GetSelectedSegment().select(true);
                    keyDown = true;
                    // e.Handled = true;
                }
                else if ((e.KeyboardDevice.IsKeyDown(Key.W) || e.KeyboardDevice.IsKeyDown(Key.Return) && !keyDown && AnnoTrack.GetSelectedTrack() != null) && !AnnoTrack.GetSelectedTrack().isDiscrete)
                {
                    if (AnnoTrack.GetSelectedSegment() != null)
                    {
                        ShowLabelBoxCont();
                    }
                    if (AnnoTrack.GetSelectedSegment() != null) AnnoTrack.GetSelectedSegment().select(true);
                    keyDown = true;
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
            if (!movemedialock) this.view.trackControl.timeTrackControl.rangeSlider.Update();
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

            if (configfilepath == "")
            {
                //If no (new format) anno file was loaded, get directory of first media element, else from first signal, else default last folder is picked.
                string firstmediadir = "";
                if (media_list.Medias.Count > 0) firstmediadir = media_list.Medias[0].GetFolderepath();
                else if (signals.Count > 0) firstmediadir = signals[0].Folderpath;

                configfilepath = ViewTools.SaveFileDialog("project", ".nova", firstmediadir, 5);
                if (configfilepath != null)
                {
                    saveProjectTracks(anno_tracks, media_list, signal_tracks, configfilepath);
                }
            }
            else
            {
                saveProjectTracks(anno_tracks, media_list, signal_tracks, configfilepath);
            }
        }

        public void saveAll()
        {
            bool anytrackchanged = false;
            foreach (AnnoTrack track in anno_tracks)
            {
                if (track.AnnoList.HasChanged) anytrackchanged = true;
            }

            if (anno_tracks.Count > 0 && anytrackchanged)
            {
                //if (DatabaseLoaded)
                //{
                ////    mongodbStore();
                //}

                //else
                // {
                foreach (AnnoTrack track in anno_tracks)
                {
                    if (track.AnnoList.HasChanged /*&& !track.isDiscrete*/)
                    {
                        if (track.AnnoList.FromDB)
                        {

                        }
                        else
                        {
                            saveAnno(track.AnnoList, track.AnnoList.Filepath);
                            track.AnnoList.HasChanged = false;
                        }
                    }
                }
                // }
            }
        }

        public void clear()
        {
            Stop();

            bool anytrackchanged = false;
            foreach (AnnoTrack track in anno_tracks)
            {
                if (track.AnnoList.HasChanged) anytrackchanged = true;
            }

            if (anno_tracks.Count > 0 && anytrackchanged)
            {
                MessageBoxResult mbx = MessageBox.Show("Save annotations?", "Question", MessageBoxButton.YesNo);
                if (mbx == MessageBoxResult.Yes)
                {
                    saveAll();
                }
            }

            DatabaseLoaded = false;
            if (Time.TotalDuration > 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
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
            this.view.navigator.Statusbar.Content = "";

            this.signalCursor.X = 0;
            setAnnoList(null);

            this.view.trackControl.annoTrackControl.clear();
            this.view.trackControl.signalTrackControl.clear();
            this.view.videoControl.clear();
            this.view.pointcontrol.clear();
           
            innomediaplaymode = false;

            this.signal_tracks.Clear();
            this.signals.Clear();
            this.anno_tracks.Clear();
            this.annos.Clear();
            this.media_list.clear();
            this.view.videoskel.Children.OfType<GridSplitter>().ToList().ForEach(b => this.view.videoskel.Children.Remove(b));
            // this.view.videoskel.Children.Remove);
            annofilepath = "";
            visualizepoints = false;
            visualizeskel = false;

            while (this.view.videoskel.ColumnDefinitions.Count > 1)
            {
                this.view.videoskel.ColumnDefinitions.RemoveAt(this.view.videoskel.ColumnDefinitions.Count-1);
            }


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
                fps = s.rate;
                skelfps = fps;
            }
            else
            {
                fps = skelfps;
            }
            EventHandler ev = new EventHandler(delegate (object sender, EventArgs a)
            {
                if (!movemedialock)
                {
                    double time = (ViewHandler.Time.CurrentPlayPositionPrecise + (1000.0 / fps) / 1000.0);
                    ViewHandler.Time.CurrentPlayPositionPrecise = time;
                    // if (media_list.Medias.Count == 0)
                    if (visualizeskel || visualizepoints)
                    {
                        signalCursor.X = ViewHandler.Time.PixelFromTime(ViewHandler.Time.CurrentPlayPositionPrecise);

                        if (Time.CurrentPlayPositionPrecise >= Time.SelectionStop && this.view.navigator.followplaybox.IsChecked == true)
                        {
                            double factor = (((Time.CurrentPlayPositionPrecise - Time.SelectionStart) / (Time.SelectionStop - Time.SelectionStart)));

                            this.view.trackControl.timeTrackControl.rangeSlider.followmedia = true;
                            this.view.trackControl.timeTrackControl.rangeSlider.MoveAndUpdate(true, factor);
                        }
                        else if (this.view.navigator.followplaybox.IsChecked == false) this.view.trackControl.timeTrackControl.rangeSlider.followmedia = false;

                        //hm additional syncstep..
                        if (lasttimepos < ViewHandler.Time.CurrentPlayPosition)
                        {
                            lasttimepos = ViewHandler.Time.CurrentPlayPosition;
                            ViewHandler.Time.CurrentPlayPositionPrecise = lasttimepos;
                        }
                        if (AnnoTrack.GetSelectedSegment() != null) AnnoTrack.GetSelectedSegment().select(true);
                    }
                }

                if (!innomediaplaymode)
                {
                    if (_timerp != null)
                    {
                        _timerp.Stop();
                        _timerp = null;
                    }

                }
            });


            if (innomediaplaymode)
            {
               // lasttimepos = ViewHandler.Time.CurrentPlayPosition;
                this.view.navigator.playButton.Content = "II";
                // Play();
                _timerp = new DispatcherTimer();
                _timerp.Interval = TimeSpan.FromMilliseconds(1000.0/fps);
                _timerp.Tick += ev;
                _timerp.Start();
            }

            else
            {
                if(_timerp != null)
                {
                    _timerp.Stop();
                    _timerp.Tick -= ev;
                }
              
            }

       
        }

        private void mediaPlayHandler(MediaList videos, MediaPlayEventArgs e)
        {
            if (movemedialock == false)
            {
                double pos = ViewHandler.Time.PixelFromTime(e.pos);

                if (Time.SelectionStop - Time.SelectionStart < 1) Time.SelectionStart = Time.SelectionStop - 1;

                Time.CurrentPlayPosition = e.pos;

                if(!visualizeskel && !visualizepoints)       signalCursor.X = pos;
                //   Console.WriteLine("5 " + signalCursor.X);
                //if (ViewHandler.Time.TimeFromPixel(signalCursor.X) > Time.SelectionStop || signalCursor.X <= 1 ) signalCursor.X = ViewHandler.Time.PixelFromTime(Time.SelectionStart);
                // Console.WriteLine(signalCursor.X + "_____" + Time.SelectionStart);

                double time = Time.TimeFromPixel(pos);
                this.view.trackControl.signalPositionLabel.Text = ViewTools.FormatSeconds(e.pos);
                this.view.trackControl.annoTrackControl.currenttime = Time.TimeFromPixel(pos);

                if (e.pos > ViewHandler.time.TotalDuration - 0.5)
                {
                    Stop();
                }
            }

            if (Time.CurrentPlayPosition >= Time.SelectionStop && this.view.navigator.followplaybox.IsChecked == true && !movemedialock)
            {
                double factor = (((Time.CurrentPlayPosition - Time.SelectionStart) / (Time.SelectionStop - Time.SelectionStart)));

                this.view.trackControl.timeTrackControl.rangeSlider.followmedia = true;
                this.view.trackControl.timeTrackControl.rangeSlider.MoveAndUpdate(true, factor);
            }
            else if (this.view.navigator.followplaybox.IsChecked == false) this.view.trackControl.timeTrackControl.rangeSlider.followmedia = false;
            if (AnnoTrack.GetSelectedSegment() != null) AnnoTrack.GetSelectedSegment().select(true);
        }

        private void annoDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListView grid = (ListView)sender;

            if (grid.SelectedIndex >= 0 && grid.SelectedIndex < grid.Items.Count)
            {
                AnnoListItem item = (AnnoListItem)grid.SelectedItem;
                view.annoListControl.editComboBox.SelectedItem = item.Label;

                movemedialock = true;
                Time.CurrentPlayPosition = item.Start;
                Time.CurrentPlayPositionPrecise = item.Start;

                media_list.move(item.Start);
                moveCursorTo(item.Start);

                if (item.Start >= time.SelectionStop)
                {
                    float factor = (float)(((item.Start - Time.SelectionStart) / (Time.SelectionStop - Time.SelectionStart)));
                    this.view.trackControl.timeTrackControl.rangeSlider.MoveAndUpdate(true, factor);
                }
                else if (item.Stop <= time.SelectionStart)
                {
                    float factor = (float)(((Time.SelectionStart - item.Start)) / (Time.SelectionStop - Time.SelectionStart));
                    this.view.trackControl.timeTrackControl.rangeSlider.MoveAndUpdate(false, factor);
                }

                foreach (AnnoListItem a in AnnoTrack.GetSelectedTrack().AnnoList)
                {
                    if (a.Start == item.Start && a.Stop == item.Stop && item.Label == a.Label)
                    {
                        AnnoTrack.SelectSegment(AnnoTrack.GetSelectedTrack().getSegment(a));
                        view.annoListControl.editComboBox.SelectedItem = item.Label;
                        view.annoListControl.editTextBox.Text = item.Label;

                        break;
                    }
                }

                movemedialock = false;
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

        public void newAnno(AnnoType isDiscrete, double samplerate = 1.0, double borderlow = 0, double borderhigh = 1.0, Brush background = null)
        {
            AnnoList anno = new AnnoList();
            if (DatabaseLoaded)
            {
                string l = Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@";
                DatabaseHandler db = new DatabaseHandler("mongodb://" + l + Properties.Settings.Default.MongoDBIP);

                anno.Role = db.LoadRoles(Properties.Settings.Default.Database, null);
                if (anno.Role == null)
                {
                    return;
                }

                string annoscheme = db.LoadAnnotationSchemes(Properties.Settings.Default.Database, null, isDiscrete);
                if (annoscheme == null)
                {
                    return;
                }

                anno.AnnotationScheme = db.GetAnnotationScheme(annoscheme, isDiscrete);
                anno.AnnotationScheme.LabelsAndColors.Add(new LabelColorPair("GARBAGE", "#FF000000"));
                if (anno.AnnotationScheme.type == "FREE") anno.AnnotationType = AnnoType.FREE;
                else if (anno.AnnotationScheme.type == "DISCRETE") anno.AnnotationType = AnnoType.DISCRETE;
                else if (anno.AnnotationScheme.type == "CONTINUOUS") anno.AnnotationType = AnnoType.CONTINUOUS;

                if (anno.AnnotationScheme.type != "FREE") anno.usesAnnoScheme = true;
                else anno.usesAnnoScheme = false;
                anno.Name = "#" + anno.Role + " #" + anno.AnnotationScheme.name + " #" + anno.Annotator;

                if (anno.AnnotationScheme != null && anno.AnnotationScheme.mincolor != null && anno.AnnotationScheme.maxcolor != null)
                {
                    background = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(anno.AnnotationScheme.maxcolor), (Color)ColorConverter.ConvertFromString(anno.AnnotationScheme.mincolor), 90.0);
                    background.Opacity = 0.75;
                }
                else if (anno.AnnotationScheme.mincolor != null) { background = new SolidColorBrush((Color)(ColorConverter.ConvertFromString(anno.AnnotationScheme.mincolor))); }

                addAnno(anno, anno.AnnotationType, (1000.0 / anno.AnnotationScheme.sr) / 1000.0, null, anno.AnnotationScheme.minborder, anno.AnnotationScheme.maxborder, background);
                view.annoListControl.editComboBox.SelectedIndex = 0;
            }
            else
            {
                if (isDiscrete == AnnoType.CONTINUOUS)
                {
                    if (anno.AnnotationScheme != null && anno.AnnotationScheme.mincolor != null && anno.AnnotationScheme.maxcolor != null)
                    {
                        background = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(anno.AnnotationScheme.maxcolor), (Color)ColorConverter.ConvertFromString(anno.AnnotationScheme.mincolor), 90.0);
                        background.Opacity = 0.75;
                    }
                }
                addAnno(anno, isDiscrete, samplerate, null, borderlow, borderhigh, background);
            }
        }

        public void addAnno(AnnoList anno, AnnoType isdiscrete, double samplerate = 1, string filepath = null, double borderlow = 0.0, double borderhigh = 1.0, Brush background = null, string annotator = null)
        {
            string TierId;
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
            track.AnnoList.AnnotationType = isdiscrete;
            if (annotator == null) track.AnnoList.Annotator = anno.Annotator;
            else track.AnnoList.Annotator = annotator;
            track.AnnoList.usesAnnoScheme = anno.usesAnnoScheme;
            track.AnnoList.AnnotationScheme = anno.AnnotationScheme;
            track.AnnoList.HasChanged = false;

            this.view.trackControl.timeTrackControl.rangeSlider.OnTimeRangeChanged += track.timeRangeChanged;

            this.anno_tracks.Add(track);
            this.annos.Add(anno);

            if (track != null && background == null && track.AnnoList != null && track.AnnoList.AnnotationScheme != null && anno.AnnotationScheme.mincolor != null && track.AnnoList.AnnotationType != AnnoType.CONTINUOUS)
            {
                background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(track.AnnoList.AnnotationScheme.mincolor));
            }
            if (track.AnnoList.AnnotationType != AnnoType.CONTINUOUS && track.AnnoList.AnnotationScheme != null && track.AnnoList.AnnotationScheme.mincolor != null)
            {
                track.Background = background;
                track.BackgroundColor = background;
            }

            AnnoTrack.SelectTrack(track);
            if (track.AnnoList.AnnotationType == AnnoType.CONTINUOUS)
            {
                if (anno.AnnotationScheme != null && track.AnnoList.AnnotationScheme.mincolor != null && track.AnnoList.AnnotationScheme.maxcolor != null)
                {
                    background = new LinearGradientBrush((Color)ColorConverter.ConvertFromString(track.AnnoList.AnnotationScheme.mincolor), (Color)ColorConverter.ConvertFromString(track.AnnoList.AnnotationScheme.maxcolor), 90.0);
                    background.Opacity = 0.75;
                }
                else background = resultbrush("RedBlue");
                track.ContiniousBrush = background;
                track.Background = background;
            }
            if (track.AnnoList.AnnotationType == AnnoType.CONTINUOUS)
            {
                if (anno.AnnotationScheme == null) anno.AnnotationScheme = new AnnotationScheme();
                track.AnnoList.AnnotationScheme.mincolor = new SolidColorBrush(((LinearGradientBrush)track.ContiniousBrush).GradientStops[0].Color).ToString();
                track.AnnoList.AnnotationScheme.maxcolor = new SolidColorBrush(((LinearGradientBrush)track.ContiniousBrush).GradientStops[1].Color).ToString();
            }

            track.timeRangeChanged(ViewHandler.Time);
        }

        private void reloadAnno(string filename)

        {
            if (!File.Exists(filename))
            {
                ViewTools.ShowErrorMessage("Annotation file not found '" + filename + "'");
                return;
            }

            AnnoList anno = AnnoList.LoadfromFileNew(filename);
            anno.Filepath = filename;
            anno.SampleAnnoPath = filename;
            double maxdur = 0;

            maxdur = anno[anno.Count - 1].Stop;

            if (anno != null && AnnoTrack.GetSelectedTrack() != null)
            {
                setAnnoList(anno);
                AnnoTrack.GetSelectedTrack().Children.Clear();
                AnnoTrack.GetSelectedTrack().AnnoList.Clear();
                AnnoTrack.GetSelectedTrack().segments.Clear();

                AnnoTrack.GetSelectedTrack().AnnoList = anno;

                foreach (AnnoListItem item in anno)
                {
                    AnnoTrack.GetSelectedTrack().addSegment(item);
                }

                AnnoTrack.GetSelectedTrack().timeRangeChanged(ViewHandler.Time);
            }

            updateTimeRange(maxdur);
            if (maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0 && annos.Count != 0 && media_list.Medias.Count == 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
        }

        public void reloadAnnoDB(AnnoTrack tier)

        {

            Action EmptyDelegate = delegate () { };
            this.view.ShadowBoxText.Text = "Reloading Annotation";
            this.view.ShadowBox.Visibility = Visibility.Visible;
            view.UpdateLayout();
            view.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            DatabaseAnno s = new DatabaseAnno();
            s.Role = AnnoTrack.GetSelectedTrack().AnnoList.Role;
            s.AnnoType = AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.name;
            s.AnnotatorFullname = AnnoTrack.GetSelectedTrack().AnnoList.AnnotatorFullName;
            s.Annotator = AnnoTrack.GetSelectedTrack().AnnoList.Annotator;

            List<DatabaseAnno> list = new List<DatabaseAnno>();
            List<AnnoTrack> tracks = new List<AnnoTrack>();
            list.Add(s);
            tracks.Add(AnnoTrack.GetSelectedTrack());

            string l = Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@";
            DatabaseHandler db = new DatabaseHandler("mongodb://" + l + Properties.Settings.Default.MongoDBIP);

           // db.StoreToDatabase(Properties.Settings.Default.Database, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser, tracks, loadedDBmedia, false);


            List<AnnoList> annos = db.LoadFromDatabase(list, Properties.Settings.Default.Database, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser);
            double maxdur = 0;

            if(annos[0].Count > 0)   maxdur = annos[0][annos[0].Count - 1].Stop;

            if (annos[0] != null && AnnoTrack.GetSelectedTrack() != null)
            {
                setAnnoList(annos[0]);
                tier.Children.Clear();
                tier.AnnoList.Clear();
                tier.segments.Clear();
                tier.AnnoList = annos[0];

                foreach (AnnoListItem item in annos[0])
                {
                    tier.addSegment(item);
                }

                tier.timeRangeChanged(ViewHandler.Time);
            }

            updateTimeRange(maxdur);
            if (maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0 && annos.Count != 0 && media_list.Medias.Count == 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
            this.view.ShadowBox.Visibility = Visibility.Collapsed;
        }

        //new File Format...
        private void loadAnnotation(string filename)
        {
            if (!File.Exists(filename))
            {
                ViewTools.ShowErrorMessage("Annotation file not found '" + filename + "'");
                return;
            }

            AnnoList anno = AnnoList.LoadfromFileNew(filename);
            anno.Filepath = filename;
            anno.SampleAnnoPath = filename;
            double maxdur = 0;

            maxdur = anno[anno.Count - 1].Stop;

            if (anno != null)
            {
                setAnnoList(anno);
                addAnno(anno, anno.AnnotationType, anno.SR, filename, anno.Lowborder, anno.Highborder);
            }

            updateTimeRange(maxdur);
            if (maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0 && annos.Count != 0 && media_list.Medias.Count == 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
        }

        private void handleAnnotation(AnnoList anno, string filename)
        {
            if ((anno.AnnotationType == AnnoType.FREE || anno.AnnotationType == AnnoType.DISCRETE) && annofilepath == "") annofilepath = filename;
            double maxdur = 0;
            //Get all tier ids that haven't been added before
            //(This is where in the future tiers will be read from the config)
            List<String> TierIds = new List<String>();

            if (anno.Count > 0)
            {
                if (anno.AnnotationType != AnnoType.CONTINUOUS)
                {
                    //legacy code for multitier files, probably remove this in the future

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
                }
                else
                {
                    string tier = "";
                    if (anno[0].Tier != null) tier = anno[0].Tier;
                    else tier = anno.Name;
                    TierIds.Add(tier);
                    maxdur = anno[anno.Count - 1].Stop;
                }
            }
            else
            {
                TierIds.Add(anno.Name);
            }

            //Create a new AnnoList per tierId and add it
            if (TierIds.Count > 0)
            {
            }
            foreach (String tierid in TierIds)
            {
                AnnoList annolist = new AnnoList();

                if (anno.Count > 0)
                {
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
                }

                if (annolist.Name == null) annolist.Name = anno.Name;
                annolist.Annotator = anno.Annotator;
                annolist.AnnotatorFullName = anno.AnnotatorFullName;
                annolist.Highborder = anno.Highborder;
                annolist.Lowborder = anno.Lowborder;
                annolist.AnnotationScheme = anno.AnnotationScheme;
                annolist.AnnotationScheme.name = anno.AnnotationScheme.name;
                annolist.Role = anno.Role;
                annolist.SR = anno.SR;
                annolist.HasChanged = false;
                annolist.AnnotationType = anno.AnnotationType;
                annolist.Filepath = anno.Filepath;
                annolist.SampleAnnoPath = anno.SampleAnnoPath;
                annolist.usesAnnoScheme = anno.usesAnnoScheme;
                annolist.AnnotationScheme.minborder = anno.AnnotationScheme.minborder;
                annolist.AnnotationScheme.maxborder = anno.AnnotationScheme.maxborder;
                annolist.FromDB = anno.FromDB;

                Brush background = null;
                if (anno.AnnotationScheme != null && anno.AnnotationScheme.mincolor != null && anno.AnnotationScheme.maxcolor != null && anno.AnnotationType == AnnoType.CONTINUOUS)
                {
                    annolist.AnnotationScheme.mincolor = anno.AnnotationScheme.mincolor;
                    annolist.AnnotationScheme.maxcolor = anno.AnnotationScheme.maxcolor;
                }

                if (anno.AnnotationScheme == null && anno.AnnotationType == AnnoType.CONTINUOUS)
                {
                    anno.AnnotationScheme = new AnnotationScheme();
                    background = resultbrush("RedBlue");
                    background.Opacity = 0.75;
                }
                else if (anno.AnnotationScheme != null && anno.AnnotationScheme.mincolor != null && anno.AnnotationType != AnnoType.CONTINUOUS)
                {
                    annolist.AnnotationScheme.mincolor = anno.AnnotationScheme.mincolor;
                }

                if (annolist != null)
                {
                    setAnnoList(annolist);
                    annolist.SampleAnnoPath = filename;
                    addAnno(annolist, annolist.AnnotationType, (1000.0 / annolist.SR) / 1000.0, filename, annolist.Lowborder, annolist.Highborder, background);
                    annolist.HasChanged = false;
                    tiercount++;
                }
            }

            //Adjust the view
            updateTimeRange(maxdur);
            if (maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
        }

        private void loadCSVAnnotation(string filename, double samplerate = 1, string type = "semicolon", string filter = null)
        {
            if (!File.Exists(filename))
            {
                ViewTools.ShowErrorMessage("Annotation file not found '" + filename + "'");
                return;
            }

            //Temp list that contains all annotations from file
            AnnoList anno = AnnoList.LoadfromFile(filename, samplerate, type, filter);
            handleAnnotation(anno, filename);
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
                    addAnno(a, AnnoType.FREE, 1);
                }
            }
            updateTimeRange(maxdur);
            if (maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
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
                    addAnno(a, AnnoType.FREE, 1);
                }
            }
            updateTimeRange(maxdur);
            if (maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
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
                addAnno(anno, AnnoType.FREE);
            }

            updateTimeRange(maxdur);
            if (maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
        }

        private void loadStream(string filename, string color = "#FF000000", string background = "#FFF0F0F0")
        {
            if (!File.Exists(filename))
            {
                ViewTools.ShowErrorMessage("Stream file not found '" + filename + "'");
                return;
            }

            Signal signal = Signal.LoadStreamFile(filename);
            signalCursor.signalLoaded = true;
            annoCursor.signalLoaded = true;
            if (signal != null && signal.loaded)
            {
                addSignal(signal, color, background);

                //if signal is SSI Skeleton...
                //if (signal.meta_name == "skeleton")
                //{
                //    if (this.view.videoskel.ColumnDefinitions.Count < 2 && media_list.Medias.Count > 0 && !visualizeskel)
                //    {
                //        ColumnDefinition column = new ColumnDefinition();
                //        column.Width = new GridLength(1, GridUnitType.Star);
                //        this.view.videoskel.ColumnDefinitions.Add(column);
                //    }
                //    else if (this.view.videoskel.ColumnDefinitions.Count < 2 )
                //    {
                //        ColumnDefinition columvideo = this.view.videoskel.ColumnDefinitions[0];
                //        columvideo.Width = new GridLength(0, GridUnitType.Pixel);

                //        ColumnDefinition column = new ColumnDefinition();
                //        column.Width = new GridLength(1, GridUnitType.Star);
                //        this.view.videoskel.ColumnDefinitions.Add(column);
                //    }
                //    this.view.pointcontrol.addPoints(signal);
                //    visualizeskel = true;
                //    this.view.navigator.playButton.IsEnabled = true;

                //}

                if (signal.meta_name == "face" || signal.meta_name == "skeleton")
                {
                    if (this.view.videoskel.ColumnDefinitions.Count < 2 && media_list.Medias.Count > 0)
                    {


                        ColumnDefinition split_column = new ColumnDefinition();
                        split_column.Width = new GridLength(1, GridUnitType.Auto);
                        this.view.videoskel.ColumnDefinitions.Add(split_column);
                        GridSplitter splitter = new GridSplitter();
                        splitter.ResizeDirection = GridResizeDirection.Columns;
                        splitter.Width = 3;
                        splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                        splitter.VerticalAlignment = VerticalAlignment.Stretch;
                        Grid.SetRowSpan(splitter, 1);
                        //Grid.SetColumn(splitter, 0);
                        Grid.SetColumn(splitter, this.view.videoskel.ColumnDefinitions.Count - 1);


                        this.view.videoskel.Children.Add(splitter);



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
                    this.view.pointcontrol.addPoints(signal);
                    visualizepoints = true;
                    this.view.navigator.playButton.IsEnabled = true;
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

        private void loadCSV(string filename, string color = "#FF000000", string background = "#FFF0F0F0")
        {
            Signal signal = Signal.LoadCSVFile(filename);
            if (signal != null && signal.loaded)
            {
                addSignal(signal, color, background);
            }
        }

        private void loadARFF(string filename, string color = "#FF000000", string background = "#FFF0F0F0")
        {
            Signal signal = Signal.LoadARFFFile(filename);
            if (signal != null && signal.loaded)
            {
                addSignal(signal, color, background);
            }
        }

        private void loadWav(string filename, string color = "#FF000000", string background = "#FFF0F0F0")
        {
            if (!File.Exists(filename))
            {
                ViewTools.ShowErrorMessage("Wav file not found '" + filename + "'");
                return;
            }

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
            if (duration > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
            //updateTimeRange(duration);
            //track.timeRangeChanged(ViewHandler.Time);
        }

        private void updateTimeRange(double duration)
        {
            if (duration > ViewHandler.Time.TotalDuration)
            {
                ViewHandler.Time.TotalDuration = duration;
                if (!movemedialock) this.view.trackControl.timeTrackControl.rangeSlider.Update();
            }
        }

        private void fixTimeRange(double duration)
        {
            if (!movemedialock) this.view.trackControl.timeTrackControl.rangeSlider.UpdateFixedRange(duration);
        }

        private void loadMedia(string filename, bool is_video, string url = null)
        {
            if (!File.Exists(filename))
            {
                ViewTools.ShowErrorMessage("Media file not found '" + filename + "'");
                return;
            }

            double pos = ViewHandler.Time.TimeFromPixel(signalCursor.X);
            IMedia media = media_list.addMedia(filename, pos, url);
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
                    if (media.GetLength() > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
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
            handlePlay();
        }


        private void handlePlay()
        {

            if ((string)this.view.navigator.playButton.Content == "II")
            {
              
             //   nomediaPlayHandler(null);
                innomediaplaymode = false;
                this.view.navigator.playButton.Content = ">";
            }
            else
            {
                innomediaplaymode = true;
                nomediaPlayHandler(null);
                this.view.navigator.playButton.Content = "II";
            }

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
            this.view.trackControl.annoNameLabel.Content = track.AnnoList.Name;

            //  this.view.annoNameLabel.ToolTip = track.AnnoList.Filepath;
            setAnnoList(track.AnnoList);
            current_anno = track.AnnoList;

            view.annoListControl.editComboBox.Items.Clear();

            if (AnnoTrack.GetSelectedTrack() != null)
            {
                if (AnnoTrack.GetSelectedTrack().AnnoList.AnnotationType == AnnoType.CONTINUOUS)
                {
                    view.annoListControl.editButton.Visibility = Visibility.Collapsed;
                    view.annoListControl.editComboBox.Visibility = Visibility.Collapsed;
                    view.annoListControl.editTextBox.Visibility = Visibility.Collapsed;
                    view.annoListControl.editComboBox.IsEnabled = false;
                    view.annoListControl.editTextBox.IsEnabled = false;
                }

                view.annoListControl.editComboBox.Items.Clear();
                if (AnnoTrack.GetSelectedTrack().AnnoList.AnnotationType == AnnoType.DISCRETE)
                {
                    view.annoListControl.editComboBox.IsEnabled = true;
                    view.annoListControl.editComboBox.Visibility = Visibility.Visible;
                    view.annoListControl.editTextBox.Visibility = Visibility.Collapsed;
                    view.annoListControl.editTextBox.IsEnabled = false;
                    view.annoListControl.editButton.Visibility = Visibility.Visible;

                    if (AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme != null && AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors != null && AnnoTrack.GetSelectedTrack().AnnoList.AnnotationType == AnnoType.DISCRETE)
                    {
                        if (AnnoTrack.GetSelectedTrack().Defaultlabel == "") AnnoTrack.GetSelectedTrack().DefaultColor = AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors[0].Color;
                        if (AnnoTrack.GetSelectedTrack().Defaultlabel == "") AnnoTrack.GetSelectedTrack().Defaultlabel = AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors[0].Label;

                        foreach (LabelColorPair lcp in AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors)
                        {
                            view.annoListControl.editComboBox.Items.Add(lcp.Label);
                        }

                        view.annoListControl.editComboBox.SelectedIndex = 0;
                    }
                }
                else if (AnnoTrack.GetSelectedTrack().AnnoList.AnnotationType == AnnoType.FREE)
                {
                    view.annoListControl.editTextBox.Visibility = Visibility.Visible;
                    view.annoListControl.editComboBox.Visibility = Visibility.Collapsed;
                    view.annoListControl.editButton.Visibility = Visibility.Visible;
                    view.annoListControl.editTextBox.IsEnabled = true;
                }
            }

            //this.view.trackControl.annoNameLabel.Text = track.AnnoList.Filename;
        }

        private void changeAnnoTrackSegmentHandler(AnnoTrackSegment segment, EventArgs e)
        {
            // this.view.annoNameLabel.Text = track.AnnoList.Filename;
            // this.view.annoNameLabel.ToolTip = track.AnnoList.Filepath;
            // setAnnoList(track.AnnoList);

            if (IsPlaying())
            {
                Stop();
                Play();
            }

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

        private void changed_annoschemeselectionbox(Object sender, EventArgs e)
        {
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
            saveAnno(this.current_anno, this.current_anno.Filepath);
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
                if (!(filepath == null || filepath.Split('.')[1] == "eaf" || filepath.Split('.')[1] == "anvil" || filepath.Split('.')[1] == "anno" || filepath.Split('.')[1] == "csv"))
                {
                    //anno.saveToFile(filepath);
                    anno.saveToFileNew(filepath);
                }
                else
                {
                    anno.saveToFileNew();
                    //   anno.saveToFile();
                }
            }
        }

        public void exportAnnotoCSV(AnnoList anno)
        {
            if (anno != null)
            {
                if (anno.AnnotationType == AnnoType.CONTINUOUS)
                {
                    anno.saveContinousToFile();
                }
                else
                {
                    anno.saveToFile();
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

        public void saveProjectTracks(List<AnnoTrack> tracks, MediaList ml, List<ISignalTrack> signal_tracks, string filepath)
        {
            string workdir = Path.GetDirectoryName(filepath);

            StreamWriter sw = new StreamWriter(filepath, false, System.Text.Encoding.Default);
            sw.WriteLine("<novaproject version=\"1\">");

            sw.WriteLine("\t<medias>");
            if (ml != null)
            {
                foreach (IMedia t in ml.Medias)
                {
                    if (t.GetFilepath() != null)
                    {
                        sw.WriteLine("\t\t<media>" + ViewTools.GetRelativePath(t.GetFilepath(), workdir) + "</media>");
                    }
                }
            }
            sw.WriteLine("\t</medias>");
            sw.WriteLine("\t<signals>");
            if (signal_tracks != null)
            {
                foreach (SignalTrack st in signal_tracks)
                {
                    if (st.getSignal().Filepath != null)
                    {
                        sw.WriteLine("\t\t<signal bg=\"" + st.Background + "\" fg=\"" + st.SignalColor + "\">" + ViewTools.GetRelativePath(st.getSignal().Filepath, workdir) + "</signal>");
                    }
                }
            }

            sw.WriteLine("\t</signals>");
            sw.WriteLine("\t<tiers>");

            foreach (AnnoTrack t in tracks)
            {
                if (t.AnnoList.Filepath != null)
                {
                    sw.WriteLine("\t\t<tier filepath=\"" + ViewTools.GetRelativePath(t.AnnoList.Filepath, workdir) + "\" name=\"" + t.AnnoList.Name + "\">" + "</tier>");
                }
            }

            sw.WriteLine("\t</tiers>");

            sw.WriteLine("</novaproject>");
            sw.Close();
        }

        public void loadProject(string filepath)
        {
            string workdir = Path.GetDirectoryName(filepath);

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(filepath);
                foreach (XmlNode node in doc.SelectNodes("//media"))
                {
                    bool isvideo = true;
                    if (node.InnerText.Contains("wav"))
                    {
                        isvideo = false;
                        // loadWav(node.InnerText);
                    }
                    loadMedia(ViewTools.GetAbsolutePath(node.InnerText, workdir), isvideo);
                }

                foreach (XmlNode node in doc.SelectNodes("//signal"))
                {
                    string background = node.Attributes[0].LastChild.Value;
                    string foreground = node.Attributes[1].LastChild.Value;
                    if (node.InnerText.Contains("wav"))
                    {
                        loadWav(ViewTools.GetAbsolutePath(node.InnerText, workdir));
                    }
                    else
                    {
                        loadStream(ViewTools.GetAbsolutePath(node.InnerText, workdir), foreground, background);
                    }
                }

                foreach (XmlNode child in (doc.SelectNodes("//tier")))
                {
                    if (child.Attributes.Count > 0)
                    {
                        if (child.Attributes[0].InnerText.Contains("csv"))
                        {
                            loadCSVAnnotation(ViewTools.GetAbsolutePath(child.Attributes[1].InnerText, workdir), 1, "semicolon", child.Attributes[2].InnerText);
                        }
                        else if (child.Attributes[0].InnerText.Contains("annotation"))
                        {
                            loadAnnotation(ViewTools.GetAbsolutePath(child.Attributes[0].InnerText, workdir));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                ViewTools.ShowErrorMessage(e.ToString());
            }
        }

        private void annoTrackGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (this.view.navigator.askforlabels.IsChecked == true) AnnoTrack.askforlabel = true;
            else AnnoTrack.askforlabel = false;

            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (AnnoTrack.GetSelectedSegment() != null)
                {
                    AnnoTrack.GetSelectedSegment().is_moveable = true;
                    AnnoTrack.GetSelectedTrack().leftMouseButtonDown(e);
                }
            }

            if (e.RightButton == MouseButtonState.Pressed)
            {
                if (AnnoTrack.GetSelectedSegment() != null) AnnoTrack.GetSelectedSegment().select(false);

                if (AnnoTrack.GetSelectedTrack() != null && (AnnoTrack.GetSelectedTrack().AnnoList.AnnotationType != AnnoType.CONTINUOUS || mouseDown == false))
                {
                    foreach (AnnoTrack a in anno_tracks)
                    {
                        if (a.IsMouseOver)
                        {
                            AnnoTrack.SelectSegment(null);
                            AnnoTrack.SelectTrack(a);
                            break;
                        }
                    }
                }

                if (AnnoTrack.GetSelectedTrack() != null) AnnoTrack.GetSelectedTrack().rightMouseButtonDown(e);
                mouseDown = true;
            }

            if (AnnoTrack.GetSelectedTrack() != null)
            {
                if (AnnoTrack.GetSelectedTrack().isDiscrete || (!AnnoTrack.GetSelectedTrack().isDiscrete && Keyboard.IsKeyDown(Key.LeftShift)))
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
                    AnnoTrack.GetSelectedTrack().track_used_labels.Clear();

                    if (AnnoTrack.GetSelectedTrack().AnnoList.usesAnnoScheme)

                    {
                        if (AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors != null)
                        {
                            foreach (LabelColorPair l in AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors)
                            {
                                AnnoTrack.GetSelectedTrack().track_used_labels.Add(l);
                            }
                        }
                    }
                    else
                    {
                        foreach (AnnoListItem item in AnnoTrack.GetSelectedTrack().AnnoList)
                        {
                            if (item.Label != "")
                            {
                                LabelColorPair l = new LabelColorPair(item.Label, item.Bg);
                                bool detected = false;
                                foreach (LabelColorPair p in AnnoTrack.GetSelectedTrack().track_used_labels)
                                {
                                    if (p.Label == l.Label)
                                    {
                                        detected = true;
                                    }
                                }

                                if (detected == false) AnnoTrack.GetSelectedTrack().track_used_labels.Add(l);
                            }
                        }
                    }

                    LabelInputBox inputBox = new LabelInputBox("Input", "Enter a label for your annotation", AnnoTrack.GetSelectedSegment().Item.Label, AnnoTrack.GetSelectedTrack().track_used_labels, 1, "", "", true, AnnoTrack.GetSelectedTrack().AnnoList.usesAnnoScheme);
                    if (view.navigator.hideHighConf.IsChecked == true)
                    {
                        inputBox.showSlider(true, 1.0);
                    }
                    else inputBox.showSlider(true, AnnoTrack.GetSelectedSegment().Item.Confidence);
                    inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    inputBox.ShowDialog();
                    inputBox.Close();
                    if (inputBox.DialogResult == true)
                    {
                        rename(inputBox.Result());
                        AnnoTrack.GetSelectedSegment().Item.Bg = inputBox.Color();
                        AnnoTrack.GetSelectedSegment().Item.Confidence = inputBox.ResultSlider();

                        AnnoTrack.GetSelectedTrack().track_used_labels.Clear();
                        foreach (AnnoListItem a in AnnoTrack.GetSelectedTrack().AnnoList)
                        {
                            if (a.Label == AnnoTrack.GetSelectedSegment().Item.Label) a.Bg = AnnoTrack.GetSelectedSegment().Item.Bg;

                            if (a.Label != "")
                            {
                                LabelColorPair l = new LabelColorPair(a.Label, a.Bg);
                                bool detected = false;
                                foreach (LabelColorPair p in AnnoTrack.GetSelectedTrack().track_used_labels)
                                {
                                    if (p.Label == l.Label)
                                    {
                                        detected = true;
                                    }
                                }

                                if (detected == false) AnnoTrack.GetSelectedTrack().track_used_labels.Add(l);
                            }
                        }

                        AnnoTrack.GetSelectedTrack().track_used_labels = AnnoTrack.GetSelectedTrack().track_used_labels;
                        AnnoTrack.GetSelectedTrack().Defaultlabel = inputBox.Result();
                        AnnoTrack.GetSelectedTrack().DefaultColor = inputBox.Color();
                    }
                }
            }
        }

        private void ShowLabelBoxCont()

        {
            if (AnnoTrack.GetSelectedTrack() != null)
            {
                LabelInputBox inputBox = new LabelInputBox("Input", "Enter Confidence for selected Area", "", null, 1, "", "", false, false);
                inputBox.showSlider(true, AnnoTrack.GetSelectedSegment().Item.Confidence);
                inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                inputBox.ShowDialog();
                inputBox.Close();
                if (inputBox.DialogResult == true)
                {
                    foreach (AnnoListItem ali in AnnoTrack.GetSelectedTrack().AnnoList)
                    {
                        if (ali.Start >= AnnoTrack.GetSelectedSegment().Item.Start && ali.Stop <= AnnoTrack.GetSelectedSegment().Item.Stop)
                        {
                            if (inputBox.Result() != "")
                            {
                                double valueasdouble;
                                if (double.TryParse(inputBox.Result(), out valueasdouble))
                                {
                                    if (valueasdouble >= AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.minborder && valueasdouble <= AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.maxborder)
                                        ali.Label = inputBox.Result();
                                    else
                                    {
                                        MessageBox.Show("Value must be within range of " + AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.minborder + " and " + AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.maxborder);
                                        break;
                                    }
                                }
                                else
                                {
                                    MessageBox.Show("Value must be a number within the range of " + AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.minborder + " and " + AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.maxborder);
                                    break;
                                }
                            }

                            ali.Confidence = inputBox.ResultSlider();
                        }
                    }

                    AnnoTrack.GetSelectedTrack().timeRangeChanged(time);
                    AnnoTrack.GetSelectedTrack().timeRangeChanged(time);
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
                            AnnoTrack.GetSelectedTrack().track_used_labels.Clear();
                            foreach (AnnoListItem item in AnnoTrack.GetSelectedTrack().AnnoList)
                            {
                                if (item.Label != "")
                                {
                                    LabelColorPair l = new LabelColorPair(item.Label, item.Bg);
                                    bool detected = false;
                                    foreach (LabelColorPair p in AnnoTrack.GetSelectedTrack().track_used_labels)
                                    {
                                        if (p.Label == l.Label)
                                        {
                                            detected = true;
                                        }
                                    }

                                    if (detected == false) AnnoTrack.GetSelectedTrack().track_used_labels.Add(l);
                                }
                            }

                            LabelInputBox inputBox = new LabelInputBox("Input", "Enter a label for your annotation", "Label", AnnoTrack.GetSelectedTrack().track_used_labels, 1, "", "", true);
                            inputBox.showSlider(true, AnnoTrack.GetSelectedSegment().Item.Confidence);
                            inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            inputBox.ShowDialog();
                            inputBox.Close();
                            if (inputBox.DialogResult == true && AnnoTrack.GetSelectedSegment() != null)
                            {
                                AnnoTrack.GetSelectedSegment().Item.Confidence = inputBox.ResultSlider();
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
            signalCursor.MouseDown += annoTrackGrid_MouseDown;
            annoCursor.MouseDown += annoTrackGrid_MouseDown;
        }

        private void viewControl_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                if (filenames != null)
                {
                    LoadFiles(filenames);
                }
            }
        }

        public void LoadFiles(string[] filenames, string[] url = null)
        {
            int i = 0;
            foreach (string filename in filenames)
            {
                if (File.Exists(filename))
                {
                    FileAttributes attr = File.GetAttributes(filename);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        string[] subfilenames = Directory.GetFiles(filename);
                        LoadFiles(subfilenames);
                    }
                    else
                    {
                        if (url != null)
                        {
                            loadFromFile(filename, url[i]);
                            i++;
                        }
                        else
                        {
                            loadFromFile(filename);
                        }
                    }
                }
            }
        }

        public bool loadFromFile(string filename, string url = null)
        {
            if (filename == null || filename.EndsWith("~"))
            {
                return false;
            }

            this.view.Cursor = Cursors.Wait;
            Action EmptyDelegate = delegate () { };
            this.view.ShadowBoxText.Text = "Loading '" + filename + "'";
            this.view.ShadowBox.Visibility = Visibility.Visible;
            view.UpdateLayout();
            view.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

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
                    case "divx":
                    case "mpg":
                    case "JPG":
                    case "JPEG":
                    case "PNG":
                    case "jpg":
                    case "png":
                    case "jpeg":
                    case "gif":
                    case "webm":

                        ftype = ssi_file_type.VIDEO;
                        break;

                    case "csv":
                        ftype = ssi_file_type.CSV;
                        break;

                    case "wav":
                        ftype = ssi_file_type.AUDIO;
                        break;

                    case "annotation":
                        ftype = ssi_file_type.ANNOTATION;
                        break;

                    case "stream":
                        ftype = ssi_file_type.STREAM;
                        break;

                    case "anno":
                        ftype = ssi_file_type.ANNO;
                        break;

                    case "events":
                        ftype = ssi_file_type.EVENTS;
                        break;

                    case "eaf":
                        ftype = ssi_file_type.EAF;
                        break;

                    case "arff":
                        ftype = ssi_file_type.ARFF;
                        break;

                    case "anvil":
                        ftype = ssi_file_type.ANVIL;
                        break;

                    case "nova":
                        ftype = ssi_file_type.PROJECT;
                        break;
                }
            }

            switch (ftype)
            {
                case ssi_file_type.VIDEO:
                    loadMedia(filename, true, url);
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

                    if (line != null)
                    {
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
                    }
                    else type = "semicolon";

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

                case ssi_file_type.ANNOTATION:
                    loadAnnotation(filename);
                    loaded = true;

                    break;

                case ssi_file_type.ANNO:

                    loadCSVAnnotation(filename, 1, "legacy");
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

                case ssi_file_type.ARFF:
                    loadARFF(filename);
                    loaded = true;
                    break;

                case ssi_file_type.ANVIL:
                    loadAnvil(filename);
                    loaded = true;
                    break;

                case ssi_file_type.PROJECT:
                    loadProject(filename);
                    loaded = true;
                    break;

                default:
                    break;
            }

            this.view.ShadowBox.Visibility = Visibility.Collapsed;
            this.view.Cursor = Cursors.Arrow;

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
                    this.view.trackControl.annoNameLabel.Content = inputBox.Result();
                    AnnoTrack.GetSelectedTrack().TierId = inputBox.Result();
                    AnnoTrack.GetSelectedTrack().AnnoList.Name = AnnoTrack.GetSelectedTrack().TierId;

                    foreach (AnnoListItem ali in AnnoTrack.GetSelectedTrack().AnnoList)

                    {
                        ali.Tier = AnnoTrack.GetSelectedTrack().TierId;
                    }

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

        private void editTextBox_focused(object sender, RoutedEventArgs e)
        {
            this.view.annoListControl.editTextBox.SelectAll();
        }

        private void editTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Return)
            {
                foreach (AnnoListItem item in view.annoListControl.annoDataGrid.SelectedItems)
                {
                    if (view.annoListControl.editComboBox.Visibility == Visibility.Visible && view.annoListControl.editComboBox.Items.Count > 0)
                    {
                        item.Label = view.annoListControl.editComboBox.SelectedItem.ToString();
                    }
                    else item.Label = view.annoListControl.editTextBox.Text;
                }
            }
        }

        private void cancel_click(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
        }

        private void editAnnoButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (AnnoListItem item in view.annoListControl.annoDataGrid.SelectedItems)
            {
                if (view.annoListControl.editComboBox.Visibility == Visibility.Visible && view.annoListControl.editComboBox.Items.Count > 0)
                {
                    item.Label = view.annoListControl.editComboBox.SelectedItem.ToString();
                    foreach (LabelColorPair lcp in AnnoTrack.GetSelectedTrack().AnnoList.AnnotationScheme.LabelsAndColors)
                    {
                        if (lcp.Label == item.Label)
                        {
                            item.Bg = lcp.Color;
                            break;
                        }
                    }
                }
                else item.Label = view.annoListControl.editTextBox.Text;
            }
        }

        public void saveAnnoAs()
        {
            if (this.current_anno != null)
            {
                if (this.current_anno.AnnotatorFullName == null) this.current_anno.AnnotatorFullName = this.current_anno.Annotator;
                string filename = ViewTools.SaveFileDialog(this.current_anno.AnnotationScheme.name + "." + this.current_anno.Role + "." + this.current_anno.Annotator, ".annotation");
                saveAnno(filename);
            }
        }

        public void addFiles()
        {
            string[] filenames = ViewTools.OpenFileDialog("Viewable files (*.stream,*.annotation;*.wav,*.avi,*.wmv)|*.stream;*.annotation;*.eaf;*.csv;*.wav;*.avi;*.wmv;*mp4;*mpg;*mkv;*vui|Signal files (*.stream)|*.stream|Annotation files (*.annotation;*.csv;*.anno;*.txt)|*annotation;*.anno;*csv;*txt|Wave files (*.wav)|*.wav|Video files(*.avi,*.wmv,*.mp4;*.mov)|*.avi;*.wmv;*.mp4;*.mov|All files (*.*)|*.*", true);
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

        private void settingsMenu_Click(object sender, RoutedEventArgs e)
        {
            Settings s = new Settings();
            s.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            s.ShowDialog();

            if (s.DialogResult == true)
            {
                Properties.Settings.Default.UncertaintyLevel = s.Uncertainty();
                Properties.Settings.Default.Annotator = s.AnnotatorName();
                Properties.Settings.Default.MongoDBIP = s.MongoServer();
                Properties.Settings.Default.MongoDBUser = s.MongoUser();
                Properties.Settings.Default.MongoDBPass = s.MongoPass();
                Properties.Settings.Default.DefaultZoominSeconds = double.Parse(s.ZoomInseconds());
                Properties.Settings.Default.DefaultMinSegmentSize = double.Parse(s.SegmentMinDur());
                Properties.Settings.Default.DefaultDiscreteSampleRate = double.Parse(s.SampleRate());
                Properties.Settings.Default.CheckUpdateonStartup = s.CheckforUpdatesonStartup();
                Properties.Settings.Default.Save();
            }
        }

        private void saveProject_Click(object sender, RoutedEventArgs e)
        {
            if (DatabaseLoaded)
            {
                MessageBox.Show("Storing a project file is meant for working with annotation files, not the database.");
            }
            else if (anno_tracks.Count == 0)
            {
                MessageBox.Show("No annotations have been loaded.");
            }
            else saveProject();
        }

        private void newAnnoButton_Click(object sender, RoutedEventArgs e)
        {
            if (Time.TotalDuration > 0)
            {
                NewTierWindow ntw = new NewTierWindow();
                ntw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                ntw.ShowDialog();

                if (ntw.DialogResult == true)
                {
                    AnnoType at = ntw.Result();

                    if (at == AnnoType.FREE) newAnno(AnnoType.FREE);
                    else if (at == AnnoType.DISCRETE)
                    {
                        if (DatabaseLoaded)
                        {
                            newAnno(AnnoType.DISCRETE);
                        }
                        else
                        {
                            AnnoSchemeEditor ase = new AnnoSchemeEditor();
                            ase.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            ase.ShowDialog();

                            if (ase.DialogResult == true)
                            {
                                AnnoList al = ase.GetAnnoList();
                                al.Filepath = al.Name;
                                addAnno(al, AnnoType.DISCRETE);
                            }
                        }
                    }
                    else if (at == AnnoType.CONTINUOUS)
                    {
                        List<String> list = ContinuousColorTheme.List;
                        HashSet<LabelColorPair> h = new HashSet<LabelColorPair>();

                        foreach (String i in list)
                        {
                            LabelColorPair l = new LabelColorPair(i, "");
                            h.Add(l);
                        }

                        //TODO

                        string defaultsr = "25";

                        //check if a video is loaded, and if so use it's sample rate as default
                        foreach (IMedia m in media_list.Medias)
                        {
                            if (m.IsVideo())
                            {
                                defaultsr = (m.GetSampleRate()).ToString();
                                break;
                            }
                        }

                        if (DatabaseLoaded)
                        {
                            newAnno(AnnoType.CONTINUOUS, 0, 0, 1, resultbrush("RedBlue"));
                        }
                        else
                        {
                            LabelInputBox inputBox = new LabelInputBox("New Continous Tier", "Enter Samplerate in fps, Min and Max Value", "0", h, 3, "1", defaultsr);
                            h.Clear();
                            inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            inputBox.ShowDialog();
                            inputBox.Close();
                            if (inputBox.DialogResult == true)
                            {
                                double samplerate;
                                if (double.TryParse(inputBox.Result3(), out samplerate))
                                {
                                    newAnno(AnnoType.CONTINUOUS, (1000.0 / samplerate) / 1000, double.Parse(inputBox.Result()), double.Parse(inputBox.Result2()), resultbrush(inputBox.SelectedItem()));
                                }
                            }
                        }
                    }
                }
            }
            else MessageBox.Show("There is nothing to annotate! Please load at least one media file first.", "No media loaded!", MessageBoxButton.OK, MessageBoxImage.Information);
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

                string defaultsr = "25";

                //check if a video is loaded, and if so use it's sample rate as default
                foreach (IMedia m in media_list.Medias)
                {
                    if (m.IsVideo())
                    {
                        defaultsr = (m.GetSampleRate()).ToString();
                        break;
                    }
                }

                if (DatabaseLoaded)
                {
                    newAnno(AnnoType.CONTINUOUS, 0, 0, 1, resultbrush("RedBlue"));
                }
                else
                {
                    LabelInputBox inputBox = new LabelInputBox("New Continous Tier", "Enter Samplerate in fps, Min and Max Value", "0", h, 3, "1", defaultsr);
                    h.Clear();
                    inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    inputBox.ShowDialog();
                    inputBox.Close();
                    if (inputBox.DialogResult == true)
                    {
                        double samplerate;
                        if (double.TryParse(inputBox.Result3(), out samplerate))
                        {
                            newAnno(AnnoType.CONTINUOUS, (1000.0 / samplerate) / 1000, double.Parse(inputBox.Result()), double.Parse(inputBox.Result2()), resultbrush(inputBox.SelectedItem()));
                        }
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
                //if (AnnoTrack.GetSelectedTrack().isDiscrete)
                //saveAnnoAs();
                saveAnno();
                //   else saveContinousAnnoAs();
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
                        if (DatabaseLoaded)
                        {
                            mongodbStore();
                        }
                        else saveAnno();

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

        private async Task DownloadFileSFTP(string ftphost, string folder, string db, string sessionid, string fileName, string login, string password)
        {
            bool iscanceled = false;
            string localfilepath = Properties.Settings.Default.DataPath + "\\" + db + "\\" + sessionid + "\\";
            lastdlfile = fileName;

            string ftpfilepath = "/" + folder + fileName;
            string localpath = localfilepath + fileName;

            if (!localpath.EndsWith(".stream~")) filestoload.Add(localpath);
            numberofparalleldownloads++;

            if (!File.Exists(localfilepath + fileName))
            {
                DownloadStatus dl = new DownloadStatus();
                dl.File = localpath;
                dl.percent = "0.00";
                dl.active = true;
                downloads.Add(dl);

                Sftp sftp = new Sftp(ftphost, login, password);
                try
                {
                    sftp.OnTransferProgress += new FileTransferEvent(sshCp_OnTransferProgress);
                    await view.Dispatcher.BeginInvoke(new Action<string>(SFTPConnect), DispatcherPriority.Normal, "");
                    Console.WriteLine("Connecting...");
                    sftp.Connect();
                    Console.WriteLine("OK");

                    Directory.CreateDirectory(Properties.Settings.Default.DataPath + "\\" + db + "\\" + sessionid);
                    CancellationToken token = tokenSource.Token;

                    await Task.Run(() =>
                    {
                        if (sftp.Connected)
                        {
                            token.Register(() => { sftp.Cancel(); iscanceled = true; while (sftp.Connected) Thread.Sleep(100); CanceledDownload(localpath); return; });
                            if (!iscanceled)
                            {
                                try
                                {
                                    Console.WriteLine("Downloading...");
                                    sftp.Get(ftpfilepath, localfilepath);
                                    Console.WriteLine("Disconnecting...");
                                    sftp.Close();
                                }
                                catch
                                {
                                    Console.WriteLine("Disconnecting on cancel..");
                                    sftp.Cancel();
                                    sftp.Close();
                                }
                            }
                        }
                    }, token);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    if (null != sftp && sftp.Connected)
                    {
                        sftp.Cancel();
                    }
                    MessageBox.Show("Can't login to Data Server. Not authorized. Continuing without media.");
                }
            }
            else { Console.WriteLine("File " + fileName + " already exists, skipping download"); }

            if (!iscanceled) await view.Dispatcher.BeginInvoke(new Action<string>(SFTPUpdateUIonTransferEnd), DispatcherPriority.Normal, "");
        }

        private void CanceledDownload(string localpath)
        {
            foreach (DownloadStatus d in downloads)
            {
                if (d.active == true && d.File == localpath)
                {
                    try
                    {
                        if (localpath.EndsWith("~"))
                        {
                            File.Delete(localpath.Trim('~'));
                        }
                    }
                    catch { }
                    File.Delete(localpath);
                    if (!localpath.EndsWith(".stream~"))
                    {
                        filestoload.Remove(localpath);
                    }
                    else
                    {
                        filestoload.Remove(localpath.Trim('~'));
                    }

                    numberofparalleldownloads--;
                    break;
                }
            }

            this.view.ShadowBox.Visibility = Visibility.Collapsed;
            this.view.ShadowBoxCancel.Visibility = Visibility.Collapsed;

            if (numberofparalleldownloads <= 0)
            {
                string[] files2 = new string[filestoload.Count];
                for (int i = 0; i < filestoload.Count; i++)
                {
                    files2[i] = filestoload[i];
                }

                try
                {
                    if (files2.Length > 0) LoadFiles(files2);
                }
                catch { }

                filestoload.Clear();
                downloads.Clear();
                tokenSource.Dispose();
                tokenSource = new CancellationTokenSource();
            }
        }

        private void SFTPConnect(string text)
        {
            this.view.ShadowBoxText.Text = "Connecting to Server...";
            this.view.ShadowBox.Visibility = Visibility.Visible;
            this.view.ShadowBoxCancel.Visibility = Visibility.Visible;
        }

        private void SFTPUpdateUIonTransferEnd(string text)
        {
            numberofparalleldownloads--;
            if (numberofparalleldownloads <= 0)
            {
                this.view.ShadowBox.Visibility = Visibility.Collapsed;
                this.view.ShadowBoxCancel.Visibility = Visibility.Collapsed;
                numberofparalleldownloads = 0;
                string[] files2 = new string[filestoload.Count];
                for (int i = 0; i < filestoload.Count; i++)
                {
                    files2[i] = filestoload[i];
                }

                if (files2.Length > 0) LoadFiles(files2);
                filestoload.Clear();
                downloads.Clear();
            }
        }

        private void sshCp_OnTransferProgress(string src, string dst, int transferredBytes, int totalBytes, string message)
        {
            double percent = ((double)transferredBytes / (double)totalBytes) * 100.0;
            string param = dst + "#" + percent.ToString("F2");
            view.Dispatcher.BeginInvoke(new Action<string>(UpdateUIonTransfer), DispatcherPriority.Normal, param);
        }

        private void UpdateUIonTransfer(string text)
        {
            string[] split = text.Split('#');
            this.view.ShadowBoxText.Text = "";
            foreach (DownloadStatus d in downloads)
            {
                if (d.File == split[0])
                {
                    d.percent = split[1];
                }

                int pos = d.File.LastIndexOf("\\") + 1;
                if (double.Parse(d.percent) < 99.0) this.view.ShadowBoxText.Text = this.view.ShadowBoxText.Text + "Downloading " + d.File.Substring(pos, d.File.Length - pos) + "  (" + d.percent + "%)\n";
                else d.active = false;
            }
        }

        private async Task httpPost(string URL, string filename, string db, string login, string password, string sessionid = "Default")
        {
            string fileName = filename;
            if (fileName.EndsWith(".stream%7E"))
            {
                fileName = fileName.Remove(fileName.Length - 3);
                fileName = fileName + "~";
            }

            string localpath = Properties.Settings.Default.DataPath + "\\" + db + "\\" + sessionid + "\\" + filename;
            numberofparalleldownloads++;

            if (!localpath.EndsWith(".stream~")) filestoload.Add(localpath);

            if (!File.Exists(localpath))
            {
                DownloadStatus dl = new DownloadStatus();
                dl.File = localpath;
                dl.percent = "0.00";
                dl.active = true;
                downloads.Add(dl);

                try
                {
                    Action EmptyDelegate = delegate () { };
                    this.view.ShadowBoxText.Text = "Downloading '" + filename + "'";
                    this.view.ShadowBox.Visibility = Visibility.Visible;
                    this.view.ShadowBoxCancel.Visibility = Visibility.Visible;
                    view.UpdateLayout();
                    view.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                    // Create a new WebClient instance.

                    WebClient client = new WebClient();

                    client.UploadProgressChanged += (s, e) =>
                    {
                        double percent = ((double)e.BytesReceived / (double)e.TotalBytesToReceive) * 100.0;
                        string param = localpath + "#" + percent.ToString("F2");
                        view.Dispatcher.BeginInvoke(new Action<string>(UpdateUIonTransfer), DispatcherPriority.Normal, param);
                    };

                    client.UploadValuesCompleted += (s, e) =>
                    {
                        try
                        {
                            byte[] response = e.Result;
                            File.WriteAllBytes(localpath, response);
                            view.Dispatcher.BeginInvoke(new Action<string>(httpPostfinished), DispatcherPriority.Normal, "");
                        }
                        catch (Exception ex)
                        {
                            //Could happen when we cancel the download.
                        }
                    };

                    Console.WriteLine("Downloading File \"{0}\" from \"{1}\" .......\n\n", filename, URL);
                    Directory.CreateDirectory(Properties.Settings.Default.DataPath + "\\" + db + "\\" + sessionid);

                    CancellationToken token = tokenSource.Token;

                    await Task.Run(() =>
                    {
                        token.Register(() => { client.CancelAsync(); CanceledDownload(localpath); return; });
                        string resultString = Regex.Match(sessionid, @"\d+").Value;
                        //Here we assume that the session is stored as simple ID. (as it is done in the Noxi Database). If the SessionID really is a string, this step is not needed.
                        int sid = Int32.Parse(resultString);
                        var values = new NameValueCollection();
                        values.Add("username", login);
                        values.Add("password", password);
                        values.Add("session_id", sid.ToString());
                        values.Add("filename", filename);

                        Uri url = new Uri(URL);
                        client.UploadValuesAsync(url, values);
                    }, token);
                }
                catch (Exception ex)

                {
                    MessageBox.Show("Credentials or URL are wrong.");
                }
            }
            else await view.Dispatcher.BeginInvoke(new Action<string>(httpPostfinished), DispatcherPriority.Normal, "");
        }

        private void httpPostfinished(string param)
        {
            numberofparalleldownloads--;
            if (numberofparalleldownloads <= 0)
            {
                Action EmptyDelegate = delegate () { };
                this.view.ShadowBoxText.UpdateLayout();
                this.view.ShadowBoxText.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                this.view.ShadowBoxText.Text = "Loading Data";
                this.view.ShadowBox.Visibility = Visibility.Collapsed;
                this.view.ShadowBoxCancel.Visibility = Visibility.Collapsed;
                this.view.ShadowBox.UpdateLayout();

                string[] files = new string[filestoload.Count];
                for (int i = 0; i < filestoload.Count; i++)
                {
                    files[i] = filestoload[i];
                }

                filestoload.Clear();
                downloads.Clear();
                LoadFiles(files);
            }
        }

        private void httpGet(string URL, string db, string sessionid = "Default", string filename = "")
        {
            string fileName = filename;
            if (fileName.EndsWith(".stream%7E"))
            {
                fileName = fileName.Remove(fileName.Length - 3);
                fileName = fileName + "~";
            }

            string localpath = Properties.Settings.Default.DataPath + "\\" + db + "\\" + sessionid + "\\" + fileName;

            if (!File.Exists(localpath))
            {
                try
                {
                    WebClient client = new WebClient();

                    client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(client_DownloadProgressChanged);
                    client.DownloadFileCompleted += client_DownloadFileCompleted;
                    client.QueryString.Add("file", localpath);
                    client.QueryString.Add("id", numberofparalleldownloads.ToString());
                    downloadstotal.Add(0);
                    downloadsreceived.Add(0);

                    if (!localpath.EndsWith(".stream~")) filestoload.Add(localpath);
                    numberofparalleldownloads++;
                    Directory.CreateDirectory(Properties.Settings.Default.DataPath + "\\" + db + "\\" + sessionid);
                    client.DownloadFileAsync(new Uri(URL), Properties.Settings.Default.DataPath + "\\" + db + " \\" + sessionid + "\\" + fileName);
                }
                catch { MessageBox.Show("Url not found"); }
            }
            else if (!localpath.EndsWith(".stream~") && !localpath.EndsWith(".stream%7E")) loadFromFile(localpath);
        }

        private void client_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            string id_ = ((System.Net.WebClient)(sender)).QueryString["id"];
            int id = Int32.Parse(id_);

            string filename = ((System.Net.WebClient)(sender)).QueryString["file"];

            numberofparalleldownloads--;
            if (numberofparalleldownloads == 0)
            {
                Action EmptyDelegate = delegate () { };
                this.view.navigator.Statusbar.Content = "© 2016 HCM-Lab, Augsburg University";
                this.view.navigator.Statusbar.UpdateLayout();
                this.view.navigator.Statusbar.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                this.view.ShadowBoxText.UpdateLayout();
                this.view.ShadowBoxText.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                this.view.ShadowBoxText.Text = "Loading Data";
                this.view.ShadowBox.Visibility = Visibility.Collapsed;
                this.view.ShadowBox.UpdateLayout();
                downloadsreceived.Clear();
                downloadstotal.Clear();
                string[] files = new string[filestoload.Count];
                for (int i = 0; i < filestoload.Count; i++)
                {
                    files[i] = filestoload[i];
                }
                LoadFiles(files);
                filestoload.Clear();
            }
        }

        private void client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                Action EmptyDelegate = delegate () { };

                string filename = ((System.Net.WebClient)(sender)).QueryString["file"];
                string id_ = ((System.Net.WebClient)(sender)).QueryString["id"];
                int id = Int32.Parse(id_);

                downloadsreceived[id] = e.BytesReceived;
                downloadstotal[id] = e.TotalBytesToReceive;

                double bytesreceived = 0;
                double bytestotal = 0;
                for (int i = 0; i < downloadstotal.Count; i++)

                {
                    bytesreceived = bytesreceived + downloadsreceived[i];
                    bytestotal = bytestotal + downloadstotal[i];
                }

                double percent = ((double)bytesreceived / (double)bytestotal) * 100.0;

                this.view.navigator.Statusbar.Content = "Downloading " + lastdlfile + "  (" + percent.ToString("F3") + "%)";
                this.view.navigator.Statusbar.UpdateLayout();
                this.view.navigator.Statusbar.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
                this.view.ShadowBox.Visibility = Visibility.Visible;
                this.view.ShadowBoxText.Text = "Downloading Files... Total progress: " + "  (" + percent.ToString("F2") + "%)";
            }
            catch (Exception e1)
            {
            }
        }

        private void mongodb_Store_Finished(object sender, RoutedEventArgs e)
        {
            mongodbStore(true);
        }

        private void mongodb_Store(object sender, RoutedEventArgs e)
        {
            mongodbStore();
        }

        private void mongodb_Load(object sender, RoutedEventArgs e)
        {
            mongodbLoad();
        }

        private void mongodb_Add(object sender, RoutedEventArgs e)
        {
            mongodbAdd();
        }

        private void mongodb_Functions(object sender, RoutedEventArgs e)
        {
            DatabaseFunctions dbf = new DatabaseFunctions();
            dbf.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            dbf.ShowDialog();

            if (dbf.DialogResult == true)
            {
                if (dbf.Median() != null)
                {
                    addAnno(dbf.Median(), AnnoType.CONTINUOUS, dbf.Median().SR, null, dbf.Median().Lowborder, dbf.Median().Highborder, null, "Median");

                    updateTimeRange(dbf.Median().Last().Stop);
                    if (dbf.Median().Last().Stop > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
                }

                if (dbf.RMS() != null)
                {
                    addAnno(dbf.RMS(), AnnoType.CONTINUOUS, dbf.RMS().SR, null, dbf.RMS().Lowborder, dbf.RMS().Highborder, null, "RMS");

                    updateTimeRange(dbf.RMS().Last().Stop);
                    if (dbf.RMS().Last().Stop > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0) fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
                }
            }
            this.view.mongodbmenu.IsEnabled = true;
        }

        private void mongodb_ChangeFolder(object sender, RoutedEventArgs e)
        {
            //LabelInputBox inputBox = new LabelInputBox("Database Folder", "Choose path for local files", Properties.Settings.Default.DataPath, null);
            //inputBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //inputBox.ShowDialog();
            //inputBox.Close();

            //if (inputBox.DialogResult == true)
            //{
            //    Properties.Settings.Default.DataPath = inputBox.Result();
            //}

            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.SelectedPath = Properties.Settings.Default.DataPath;
            dialog.ShowNewFolderButton = true;
            dialog.Description = "Select the folder where you want to store the media of your databases in.";
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                Properties.Settings.Default.DataPath = dialog.SelectedPath;
            }
        }

        private void mongodbCMLCompleteLearning_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLCompleteTierWindow window = new DatabaseCMLCompleteTierWindow(this);
            window.Show();
        }

        private void mongodbCMLTrainandLabel_Click(object sender, RoutedEventArgs e)
        {
            DatabaseCMLTrainWindow window = new DatabaseCMLTrainWindow(this);
            window.Show();
        }



        private void mongodbCMLExtract_Click(object sender, RoutedEventArgs e)
        {
            //TODO More logic here in the future

            string arguments = " -overwrite -log cml_extract.log " +  "\"" + Properties.Settings.Default.DataPath + "\\" + Properties.Settings.Default.Database + "\" " + " expert;novice close";


            Process process = new Process();
            ProcessStartInfo startInfo = new ProcessStartInfo();
            //   startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            startInfo.FileName = "cmltrain.exe";
            startInfo.Arguments = "--extract" + arguments;
            process.StartInfo = startInfo;
            process.Start();
            process.WaitForExit();




        }



        private void mongodbAdd()

        {
            try
            {
                DatabaseAdminWindow daw = new DatabaseAdminWindow();
                daw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                daw.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Not authorized to add a new Session");
            }
        }

        private void mongodbStore(bool isfinished = false)
        {          
            if (DatabaseLoaded)
            {
                string message = "";

                string login = Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@";

                foreach (AnnoTrack track in anno_tracks)
                {
                    if (track.AnnoList.FromDB && (track.AnnoList.HasChanged || isfinished))
                    {
                        try
                        {
                            DatabaseHandler db = new DatabaseHandler("mongodb://" + login + Properties.Settings.Default.MongoDBIP);

                            string annotator = db.StoreToDatabase(Properties.Settings.Default.Database, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser, track, loadedDBmedia, isfinished);
                            if (annotator != null)
                            {
                                track.AnnoList.HasChanged = false;
                                if(annotator != null) message += "\r\n" + track.AnnoList.Name;
                            }
                        }
                        catch (Exception ex)
                        {
                            if (ex.Message.Contains("not auth"))
                            {
                                MessageBox.Show("Sorry, you don't have write access to the database", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                            else
                            {
                                MessageBox.Show("Could not store tier '" + track.AnnoList.Name + "' to database", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(message))
                {
                    MessageBox.Show("The following tiers have been stored to the database: " + message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Load a database session first", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }            
        }

        private void mongodbLoad()
        {
            clear();

            if (loadedDBmedia != null) loadedDBmedia.Clear();
            if (filestoload != null) filestoload.Clear();

            System.Collections.IList annotations = null;
            List<DatabaseMediaInfo> ci = null;

            DatabaseHandlerWindow dbhw = new DatabaseHandlerWindow();
            try
            {
                dbhw.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                dbhw.ShowDialog();

                if (dbhw.DialogResult == true)
                {
                    annotations = dbhw.Annotations();
                    loadedDBmedia = dbhw.Media();
                    ci = dbhw.MediaConnectionInfo();
                    this.view.mongodbmenu.IsEnabled = true;
                    this.view.mongodbmenufinished.IsEnabled = true;
                    this.view.mongodbCMLCompleteLearning.IsEnabled = true;
                    this.view.mongodbCMLTrainandLabel.IsEnabled = true;
                    this.view.mongodbCMLextract.IsEnabled = true;
                    

                    //This is just a UI thing. If a user does not have according rights in the mongodb he will not have acess anyway. We just dont want to show the ui here.
                    if (dbhw.Authlevel() > 2)
                    {
                        this.view.addmongodb.Visibility = Visibility.Visible;
                        this.view.mongodbfunctions.Visibility = Visibility.Visible;
                        this.view.mongodbCMLTrainandLabel.Visibility = Visibility.Visible;
                       
                    }
                    else
                    {
                        this.view.addmongodb.Visibility = Visibility.Collapsed;
                        this.view.mongodbfunctions.Visibility = Visibility.Collapsed;
                        this.view.mongodbCMLTrainandLabel.Visibility = Visibility.Collapsed;
                       
                    }
                }

                string l = Properties.Settings.Default.MongoDBUser + ":" + Properties.Settings.Default.MongoDBPass + "@";
                DatabaseHandler db = new DatabaseHandler("mongodb://" + l + Properties.Settings.Default.MongoDBIP);

                this.view.mongodbmenu.IsEnabled = true;
                this.view.mongodbfunctions.IsEnabled = true;

                if (annotations != null)
                {
                    Action EmptyDelegate = delegate () { };
                    this.view.ShadowBox.Visibility = Visibility.Visible;
                    view.UpdateLayout();
                    view.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

                    List<AnnoList> annos = db.LoadFromDatabase(annotations, Properties.Settings.Default.Database, Properties.Settings.Default.LastSessionId, Properties.Settings.Default.MongoDBUser);
                    this.view.navigator.Statusbar.Content = "Database Session: " + (Properties.Settings.Default.LastSessionId).Replace('_', '-');
                    try
                    {
                        if (annos != null)
                        {
                            foreach (AnnoList anno in annos)

                            {
                                anno.Filepath = anno.Role + "." + anno.AnnotationScheme.name + "." + anno.AnnotatorFullName;
                                anno.SampleAnnoPath = anno.Role + "." + anno.AnnotationScheme.name + "." + anno.AnnotatorFullName;

                                if (anno.AnnotationType == AnnoType.CONTINUOUS) anno.usesAnnoScheme = true;
                                else if (anno.AnnotationType == AnnoType.DISCRETE) anno.usesAnnoScheme = true;
                                else if (anno.AnnotationType == AnnoType.FREE) anno.usesAnnoScheme = false;
                                handleAnnotation(anno, null);
                            }

                            view.ShadowBox.Visibility = Visibility.Collapsed;

                            //handle media

                            if (loadedDBmedia.Count > 0)
                            {
                                for (int i = 0; i < loadedDBmedia.Count; i++)
                                {
                                    foreach (DatabaseMediaInfo c in ci)

                                    {
                                        Properties.Settings.Default.DataServerConnectionType = c.connection;

                                        if (c.filename == loadedDBmedia[i].filename.ToString())

                                        {
                                            if (c.connection == "sftp")
                                            {
                                                Properties.Settings.Default.DataServerConnectionType = "sftp";
                                                DownloadFileSFTP(c.ip, c.folder, Properties.Settings.Default.Database, Properties.Settings.Default.LastSessionId, c.filename, Properties.Settings.Default.DataServerLogin, Properties.Settings.Default.DataServerPass);
                                            }
                                            else if (ci[i].connection == "http" || ci[i].connection == "https" && ci[i].requiresauth == "false")
                                            {
                                                Properties.Settings.Default.DataServerConnectionType = "http";
                                                httpGet(c.filepath, Properties.Settings.Default.Database, Properties.Settings.Default.LastSessionId, c.filename);
                                            }
                                            else if (ci[i].connection == "http" || ci[i].connection == "https" && ci[i].requiresauth == "true")
                                            {
                                                Properties.Settings.Default.DataServerConnectionType = "http";
                                                //This has not been tested and probably needs rework.
                                                httpPost(c.filepath, c.filename, Properties.Settings.Default.Database, Properties.Settings.Default.DataServerLogin, Properties.Settings.Default.DataServerPass, Properties.Settings.Default.LastSessionId);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        DatabaseLoaded = true;
                    }
                    catch (TimeoutException e1)
                    {
                        MessageBox.Show("Make sure ip, login and password are correct", "Connection to database not possible");
                    }
                }
            }
            catch (Exception ex)
            {
                dbhw.Close();
                MessageBox.Show("Something went wrong");
            }
        }

        private void mongodb_Show(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory(Properties.Settings.Default.DataPath);
            Process.Start(Properties.Settings.Default.DataPath);
        }


        private async void checkforUpdates(bool silent=false)
        {
            try
            {
                var client = new GitHubClient(new ProductHeaderValue("nova"));
                var releases = await client.Repository.Release.GetAll("hcmlab", "nova");
                var latest = releases[0];
                string LatestGitVersion = latest.TagName;
                var result = compareVersion(LatestGitVersion, BuildVersion);

                if (result == 0 && !silent)
                {
                    MessageBox.Show("You already have the latest version of NOVA. (Build: " + LatestGitVersion + ")");
                }

                else if (result > 0)
                {
                    MessageBoxResult mb = MessageBox.Show("Your build version is " + BuildVersion + ". The latest version is  " + LatestGitVersion + ". Do you want to update nova to the latest version? \n\n Release Notes:\n\n " + latest.Body, "Update available!", MessageBoxButton.YesNo);
                    if (mb == MessageBoxResult.Yes)
                    {
                        //  string url = "https://github.com/hcmlab/nova/releases/download/" + latest + "//nova.exe";
                       // System.Diagnostics.Process.Start("https://github.com/hcmlab/nova/releases/latest");




                        string url = "https://github.com/hcmlab/nova/blob/master/bin/updater.exe?raw=true";

                        WebClient Client = new WebClient();
                        Client.DownloadFile(url, AppDomain.CurrentDomain.BaseDirectory + "\\updater.exe");

                        System.Diagnostics.Process updateProcess = new System.Diagnostics.Process();
                        updateProcess.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "updater.exe";
                        updateProcess.StartInfo.Arguments = LatestGitVersion;
                        updateProcess.Start();
                        System.Environment.Exit(0);






                    }
                }

                else if (result < 0 && !silent)
                {
                    MessageBox.Show("The version you are running (" + BuildVersion + ") is more recent than the latest offical release (" + LatestGitVersion + ")");
                }
            }

            catch
            {

            }

        }


        private  void update_Click(object sender, RoutedEventArgs e)
        {

            checkforUpdates();

     




            //WebClient Client = new WebClient();



            //Client.DownloadFile(url, AppDomain.CurrentDomain.BaseDirectory + "\\nova_temp.exe");

            //System.Diagnostics.Process updateProcess = new System.Diagnostics.Process();
            //updateProcess.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "nova.exe";
            //updateProcess.StartInfo.Arguments = AppDomain.CurrentDomain.BaseDirectory;
            //updateProcess.Start();
            //Process.GetCurrentProcess().Kill();

            //


        }

        public int compareVersion(string Version1, string Version2)
        {
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"([\d]+)");
            System.Text.RegularExpressions.MatchCollection m1 = regex.Matches(Version1);
            System.Text.RegularExpressions.MatchCollection m2 = regex.Matches(Version2);
            int min = Math.Min(m1.Count, m2.Count);
            for (int i = 0; i < min; i++)
            {
                if (Convert.ToInt32(m1[i].Value) > Convert.ToInt32(m2[i].Value))
                {
                    return 1;
                }
                if (Convert.ToInt32(m1[i].Value) < Convert.ToInt32(m2[i].Value))
                {
                    return -1;
                }
            }
            return 0;
        }

        private void convertToContinuousAnnotation_Click(object sender, RoutedEventArgs e)
        {
            if (SignalTrack.SelectedSignal != null && !SignalTrack.SelectedSignal.IsAudio)
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
                    al.AnnotationScheme = new AnnotationScheme();
                    al.AnnotationScheme.minborder = al.Lowborder;
                    al.AnnotationScheme.maxborder = al.Highborder;
                    al.AnnotationScheme.mincolor = "#FFFF0000";
                    al.AnnotationScheme.maxcolor = "#FF0000FF";
                    al.AnnotationType = AnnoType.CONTINUOUS;
                    al.SR = s.rate;

                    AnnoList result = al.saveToFileNew();

                    if (result != null)
                    {
                        MessageBoxResult mb = MessageBoxResult.None;
                        mb = MessageBox.Show("Successfully converted stream to anno. Load the anno?", "Success", MessageBoxButton.YesNo);
                        if (mb == MessageBoxResult.Yes)
                        {
                            loadAnnotation(result.Filepath);
                        }
                    }
                }
            }
        }

        private void exporttracktoxps_Click(object sender, RoutedEventArgs e)
        {
            string filepath = ViewTools.SaveFileDialog("export_track", "xps", "", 5);
            if (filepath != null)
            {
                var uri = new System.Uri(filepath);
                if (AnnoTrack.GetSelectedTrack() != null)
                {
                    if (AnnoTrack.GetSelectedTrack().isDiscrete) AnnoTrack.GetSelectedTrack().Background = AnnoTrack.GetSelectedTrack().BackgroundColor;
                    AnnoTrack.GetSelectedTrack().ExportToXPS(uri, AnnoTrack.GetSelectedTrack());
                    AnnoTrack.GetSelectedTrack().select(true);
                    AnnoTrack.GetSelectedTrack().timeRangeChanged(ViewHandler.Time);
                }
            }
        }

        private void exporttracktopng_Click(object sender, RoutedEventArgs e)
        {
            string filepath = ViewTools.SaveFileDialog("export_track", "png", "", 6);
            if (filepath != null)
            {
                var uri = new System.Uri(filepath);
                if (AnnoTrack.GetSelectedTrack().isDiscrete) AnnoTrack.GetSelectedTrack().Background = AnnoTrack.GetSelectedTrack().BackgroundColor;
                AnnoTrack.GetSelectedTrack().ExportToPng(uri, AnnoTrack.GetSelectedTrack());
                AnnoTrack.GetSelectedTrack().select(true);
                AnnoTrack.GetSelectedTrack().timeRangeChanged(ViewHandler.Time);
            }
        }

        private void exportsignaltoxps_Click(object sender, RoutedEventArgs e)
        {
            string filepath = ViewTools.SaveFileDialog("export_signal", "xps", "", 5);
            if (filepath != null)
            {
                var uri = new System.Uri(filepath);

                if (SignalTrack.selected_track != null)
                {
                    SignalTrack.selected_track.ExportToXPS(uri, SignalTrack.selected_track);
                }
            }
        }

        private void exportsignaltopng_Click(object sender, RoutedEventArgs e)
        {
            string filepath = ViewTools.SaveFileDialog("export_signal", "png", "", 6);
            if (filepath != null)
            {
                var uri = new System.Uri(filepath);

                if (SignalTrack.selected_track != null)
                {
                    SignalTrack.selected_track.ExportToPng(uri, SignalTrack.selected_track);
                }
            }
        }

        private void exporttracktocsv_Click(object sender, RoutedEventArgs e)
        {
            if (AnnoTrack.GetSelectedTrack() != null)
            {
                exportAnnotoCSV(AnnoTrack.GetSelectedTrack().AnnoList);
            }
        }

        private void convertosignal_Click(object sender, RoutedEventArgs e)
        {
            AnnoTrack at = AnnoTrack.GetSelectedTrack();
            if (at.AnnoList.AnnotationType != AnnoType.CONTINUOUS) MessageBox.Show("Selected a continious track to convert to ssi stream");
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

                if (at.AnnoList.HasChanged)
                {
                    MessageBoxResult m = MessageBoxResult.None;
                    m = MessageBox.Show("You need to save continous annotations on tier #" + at.TierId + " first", "Confirm", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    if (m == MessageBoxResult.OK)
                    {
                        saveAnnoAs();
                        at.AnnoList.HasChanged = false;
                    }
                }

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
                            saveAnnoAs();
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

        private void check_lowconfonly(object sender, RoutedEventArgs e)
        {
            if (view.navigator.hideHighConf.IsChecked == true) AnnoTrack.CorrectMode = true;
            else AnnoTrack.CorrectMode = false;

            foreach (AnnoTrack a in anno_tracks)
            {
                a.timeRangeChanged(time);
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

            foreach (AnnoTrack anno in anno_tracks)
            {
                if (anno.AnnoList.Count > 0)
                {
                    foreach (AnnoListItem ali in anno.AnnoList)
                    {
                        if (!columns.Contains(ali.Tier)) columns.Add(ali.Tier);
                    }
                }
                else columns.Add(anno.AnnoList.Name);
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
                            foreach (AnnoTrack anno in anno_tracks)
                            {
                                if (anno.AnnoList.Count > 0)
                                {
                                    foreach (AnnoListItem ali in anno.AnnoList)
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
                                else
                                {
                                    found = false;
                                }
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

        private void ControlUnloaded(object sender, EventArgs e)
        {
            tokenSource.Cancel();
        }
    }
}