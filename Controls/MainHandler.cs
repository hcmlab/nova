using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace ssi
{
    public partial class MainHandler
    {
        public static string BuildVersion = "0.9.9.6.1";

        private static Timeline timeline = null;

        public enum SSI_FILE_TYPE
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

        public static readonly string[] SSIFileTypeNames = { "ssi", "audio", "video", "anno", "stream", "events", "eaf", "anvil", "vui", "arff", "annotation" };

        private MainControl control;

        public static Timeline Time
        {
            get { return timeline; }
        }
        
        public Cursor signalCursor = null;
        public Cursor annoCursor = null;

        private List<SignalTrack> signalTracks = new List<SignalTrack>();
        private List<Signal> signals = new List<Signal>();
        private List<AnnoTier> annoTiers = new List<AnnoTier>();
        private List<AnnoList> annoLists = new List<AnnoList>();
        private MediaList mediaList = new MediaList();
        private List<MediaBox> mediaBoxes = new List<MediaBox>();

        private bool infastforward = false;
        private bool infastbackward = false;
        private bool inNoMediaPlayMode = false;
        private DispatcherTimer _timerff = new DispatcherTimer();
        private DispatcherTimer _timerfb = new DispatcherTimer();
        private DispatcherTimer _timerp = new DispatcherTimer();
        private bool isMouseButtonDown = false;
        private bool isKeyDown = false;
        private bool movemedialock = false;
        private double skelfps = 25;
        private double lasttimepos = 0;
        private string lastDownloadFileName = null;
        private bool visualizeskel = false;
        private bool visualizepoints = false;
        public bool databaseloaded = false;

        public List<DatabaseMediaInfo> loadedDBmedia = null;
       
        private CancellationTokenSource tokenSource = new CancellationTokenSource();
        public AnnoTierSegment temp_segment;

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

        public List<AnnoTier> AnnoTiers
        {
            get { return annoTiers; }
        }

        public AnnoTier getAnnoTierFromName(string name)
        {
            foreach (AnnoTier anno in annoTiers)
            {
                if (anno.AnnoList.Scheme.Name == name)
                {
                    return anno;
                }
            }

            return null;
        }

        public MenuItem LoadButton
        {
            get { return control.loadMenu; }
        }

        public MenuItem clearButton
        {
            get { return control.clearSessionMenu; }
        }

        public MainHandler(MainControl view)
        {
            control = view;
            control.Drop += controlDrop;

            // Shadow box

            control.shadowBoxCancelButton.Click += shadowBoxCancel_Click;
            
            // Media

            control.mediaStatusBar.Background = Defaults.Brushes.Highlight;
            mediaList.OnMediaPlay += onMediaPlay;
            MediaBoxStatic.OnBoxChange += onMediaBoxChange;
            control.mediaVolumeControl.volumeSlider.ValueChanged += mediaVolumeControl_ValueChanged;
            control.mediaCloseButton.Click += mediaBoxCloseButton_Click;

            // Signal

            control.signalStatusBar.Background = Defaults.Brushes.Highlight;
            control.signalSettingsButton.Click += signalSettingsButton_Click;
            control.signalStatusDimComboBox.SelectionChanged += signalDimComboBox_SelectionChanged;
            control.signalVolumeControl.volumeSlider.ValueChanged += signalVolumeControl_ValueChanged;
            control.signalCloseButton.Click += signalTrackCloseButton_Click;
            SignalTrackStatic.OnTrackChange += onSignalTrackChange;
            control.signalAndAnnoGrid.MouseDown += signalTrackGrid_MouseDown;

            // Anno

            control.annoStatusBar.Background = Defaults.Brushes.Highlight;
            control.annoCloseButton.Click += annoTierCloseButton_Click;
            control.annoSettingsButton.Click += annoSettingsButton_Click;
            control.annoListControl.annoDataGrid.SelectionChanged += annoList_SelectionChanged;
            control.annoListControl.editButton.Click += annoListEdit_Click;
            control.annoListControl.editTextBox.KeyDown += annoListEdit_KeyDown;
            control.annoListControl.editTextBox.GotMouseCapture += annoListEdit_Focused;
            AnnoTierStatic.OnTierChange += onAnnoTierChange;
            AnnoTierStatic.OnTierSegmentChange += changeAnnoTierSegmentHandler;
            control.annoTierControl.MouseDown += annoTierControl_MouseDown;
            control.annoTierControl.MouseMove += annoTierControl_MouseMove;
            control.annoTierControl.MouseRightButtonUp += annoTierControl_MouseRightButtonUp;

            // Geometric

            control.geometricListControl.editButton.Click += geometricListEdit_Click;
            control.geometricListControl.editTextBox.GotMouseCapture += geometricListEdit_Focused;
            //control.geometricListControl.xTextBox.GotMouseCapture += geometricListEdit_Focused;
            //control.geometricListControl.yTextBox.GotMouseCapture += geometricListEdit_Focused;
            control.geometricListControl.copyButton.Click += geometricListCopy_Click;
            control.geometricListControl.selectAllButton.Click += geometricListSelectAll_Click;
            control.geometricListControl.geometricDataGrid.SelectionChanged += geometricList_Selection;
            control.geometricListControl.MenuItemDeleteClick.Click += geometricListDelete;

            // Menu

            control.clearSessionMenu.Click += navigatorClearSession_Click;
            control.saveSessionMenu.Click += saveSession_Click;
            control.saveProjectMenu.Click += saveProject_Click;
            control.showSettingsMenu.Click += showSettings_Click;

            control.updateApplicationMenu.Click += updateApplication_Click;
            control.tierMenu.MouseEnter += tierMenu_Click;
            control.helpMenu.Click += helpMenu_Click;

            control.saveAnnoMenu.Click += saveAnno_Click;
            control.saveAnnoMenuAs.Click += saveAnnoAs_Click;
            control.exportSamplesMenu.Click += exportSamples_Click;
            control.exportTierToXPSMenu.Click += exportTierToXPS_Click;
            control.exportTierToPNGMenu.Click += exportTierToPNG_Click;
            control.exportSignalToXPSMenu.Click += exportSignalToXPS_Click;
            control.exportSignalToPNGMenu.Click += exportSignalToPNG_Click;
            control.exportAnnoToCSVMenu.Click += exportAnnoToCSV_Click;
            control.convertAnnoContinuousToDiscreteMenu.Click += exportAnnoContinuousToDiscrete_Click;
            control.convertAnnoToSignalMenu.Click += exportAnnoToSignal_Click;
            control.exportAnnoDiscreteToContinuouMenu.Click += exportSignalToContinuous_Click;
            control.exportAnnoToFrameWiseMenu.Click += exportAnnoToFrameWiseMenu_Click;                           
                  
            control.databaseSaveSessionMenu.Click += databaseSaveSession_Click;
            control.databaseSaveSessionAndMarkAsFinishedMenu.Click += databaseSaveSessionAndMarkAsFinished_Click;
            control.databaseLoadSessionMenu.Click += databaseLoadSession_Click;
            control.databaseShowDownloadDirectoryMenu.Click += databaseShowDownloadDirectory_Click;
            control.databaseChangeDownloadDirectoryMenu.Click += databaseChangeDownloadDirectory_Click;
            control.databaseCMLCompleteStepMenu.Click += databaseCMLCompleteStep_Click;
            control.databaseCMLTransferStepMenu.Click += databaseCMLTransferStep_Click;
            control.databaseCMLExtractFeaturesMenu.Click += databaseCMLExtractFeatures_Click;
            control.databaseManageMenu.Click += databaseManage_Click;

            // Navigator

            control.navigator.newAnnoButton.Click += navigatorNewAnno_Click;
            control.navigator.clearButton.Click += navigatorClearSession_Click;
            control.navigator.jumpFrontButton.Click += navigatorJumpFront_Click;
            control.navigator.playButton.Click += navigatorPlay_Click;
            control.navigator.fastForwardButton.Click += navigatorFastForward_Click;
            control.navigator.fastBackwardButton.Click += navigatorFastBackward_Click;
            control.navigator.jumpEndButton.Click += navigatorJumpEnd_Click;
            control.navigator.followAnnoCheckBox.Unchecked += navigatorFollowAnno_Unchecked;
            control.navigator.correctionModeCheckBox.Click += navigatorCorrectionMode_Click;                  

            // Timeline

            timeline = new Timeline();
            timeline.SelectionInPixel = control.signalAndAnnoAdorner.ActualWidth;
            control.signalAndAnnoAdorner.SizeChanged += signalAndAnnoControlSizeChanged;
            control.timeLineControl.rangeSlider.OnTimeRangeChanged += control.timeLineControl.timeTrack.TimeRangeChanged;
            control.timeLineControl.rangeSlider.OnTimeRangeChanged += control.timeLineControl.timeTrackSelection.TimeRangeChanged;
            control.timeLineControl.rangeSlider.OnTimeRangeChanged += Time.TimelineChanged;
            control.timeLineControl.rangeSlider.Update();            

            // Mouse

            control.MouseWheel += (sender, args) =>
            {
                if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    if (args.Delta > 0)
                    {
                        control.timeLineControl.rangeSlider.MoveAndUpdate(true, 0.2f);
                    }
                    else if (args.Delta < 0)
                    {
                        control.timeLineControl.rangeSlider.MoveAndUpdate(false, 0.2f);
                    }
                }
            };

            // Cursor

            initCursor();

            // Update

            if (Properties.Settings.Default.CheckUpdateOnStart && Properties.Settings.Default.LastUpdateCheckDate.Date != DateTime.Today.Date)
            {
                Properties.Settings.Default.LastUpdateCheckDate = DateTime.Today.Date;
                Properties.Settings.Default.Save();
                checkForUpdates(true);
            }

            // Clear
            
            clearSignalTrack();
            clearAnnoInfo();
            clearMediaBox();
        }

        private void signalAndAnnoControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            timeline.SelectionInPixel = control.signalAndAnnoAdorner.ActualWidth;
            if (!movemedialock) control.timeLineControl.rangeSlider.Update();
        }
        
        public void clearSession(bool exiting = false, bool saveRequested = false)
        {
            tokenSource.Cancel();
            Stop();

            bool anytrackchanged = saveRequested;
            if (!saveRequested && !exiting)
            {
                foreach (AnnoTier track in annoTiers)
                {
                    if (track.AnnoList.HasChanged) anytrackchanged = true;
                }
            }

            if (annoTiers.Count > 0 && anytrackchanged)
            {
                if (!saveRequested)
                {
                    MessageBoxResult mbx = MessageBox.Show("Save annotations?", "Question", MessageBoxButton.YesNo);
                    if (mbx == MessageBoxResult.Yes)
                    {
                        saveAnnos();
                    }
                }
                else
                {
                    saveAnnos();
                }
            }
            
            DatabaseLoaded = false;
            if (Time.TotalDuration > 0) fixTimeRange(Properties.Settings.Default.DefaultZoomInSeconds);

            clearSignalTrack();
            clearAnnoInfo();
            
            control.navigator.playButton.IsEnabled = false;
            control.navigator.Statusbar.Content = "";

            signalCursor.X = 0;
            setAnnoList(null);

            control.annoTierControl.Clear();
            control.signalTrackControl.Clear();
            control.mediaBoxControl.Clear();
            //control.pointcontrol.Clear();

            inNoMediaPlayMode = false;

            signalTracks.Clear();
            signals.Clear();
            annoTiers.Clear();
            annoLists.Clear();
            mediaList.Clear();
            mediaBoxes.Clear();

            visualizepoints = false;
            visualizeskel = false;

            Time.TotalDuration = 0;

            control.timeLineControl.rangeSlider.Update();
        }

        private void updatePositionLabels(double time)
        {
            if (SignalTrackStatic.Selected != null && SignalTrackStatic.Selected.Signal != null)
            {
                Signal signal = SignalTrackStatic.Selected.Signal;
                control.signalPositionLabel.Text = FileTools.FormatSeconds(time);
                control.signalStatusValueLabel.Text = signal.Value(time).ToString();
                control.signalStatusValueMinLabel.Text = "min " + signal.min[signal.ShowDim].ToString();
                control.signalStatusValueMaxLabel.Text = "max " + signal.max[signal.ShowDim].ToString();
            }
            if (MediaBoxStatic.Selected != null && MediaBoxStatic.Selected.Media != null)
            {
                control.mediaPositionLabel.Text = "#" + FileTools.FormatFrames(time, MediaBoxStatic.Selected.Media.GetSampleRate());
            }
        }

        private void updateTimeRange(double duration)
        {
            if (duration > Time.TotalDuration)
            {
                Time.TotalDuration = duration;
                if (!movemedialock) control.timeLineControl.rangeSlider.Update();
            }
        }

        private void fixTimeRange(double duration)
        {
            if (!movemedialock) control.timeLineControl.rangeSlider.UpdateFixedRange(duration);
        }

        private void controlDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];
                if (filenames != null)
                {
                    loadMultipleFiles(filenames);
                }
            }
        }

        private void shadowBoxCancel_Click(object sender, RoutedEventArgs e)
        {
            tokenSource.Cancel();
        }

        public void loadFiles()
        {
            string[] filenames = FileTools.OpenFileDialog("Viewable files (*.stream,*.annotation;*.wav,*.avi,*.wmv)|*.stream;*.annotation;*.wav;*.avi;*.wmv;*mp4;*mpg;*mkv;*vui|Signal files (*.stream)|*.stream|Annotation files (*.annotation)|*annotation;*.anno|Wave files (*.wav)|*.wav|Video files(*.avi,*.wmv,*.mp4;*.mov)|*.avi;*.wmv;*.mp4;*.mov|All files (*.*)|*.*", true);
            if (filenames != null)
            {
                control.Cursor = Cursors.Wait;
                loadMultipleFiles(filenames);
                control.Cursor = Cursors.Arrow;
            }
        }

        private void saveSession_Click(object sender, RoutedEventArgs e)
        {
            saveAnnos();
        }

        private void showSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings s = new Settings();
            s.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            s.ShowDialog();

            if (s.DialogResult == true)
            {
                Properties.Settings.Default.UncertaintyLevel = s.Uncertainty();
                Properties.Settings.Default.Annotator = s.AnnotatorName();
                Properties.Settings.Default.DatabaseAddress = s.MongoServer();
                Properties.Settings.Default.MongoDBUser = s.MongoUser();
                Properties.Settings.Default.MongoDBPass = s.MongoPass();
                Properties.Settings.Default.DefaultZoomInSeconds = double.Parse(s.ZoomInseconds());
                Properties.Settings.Default.DefaultMinSegmentSize = double.Parse(s.SegmentMinDur());
                Properties.Settings.Default.DefaultDiscreteSampleRate = double.Parse(s.SampleRate());
                Properties.Settings.Default.CheckUpdateOnStart = s.CheckforUpdatesonStartup();
                Properties.Settings.Default.DatabaseAskBeforeOverwrite = s.DBAskforOverwrite();
                Properties.Settings.Default.Save();
            }
        }

    }
}