using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Xml;

namespace ssi
{
    public partial class MainHandler
    {
        #region LOAD

        public void loadMultipleFiles(string[] filenames, string[] url = null)
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
                        loadMultipleFiles(subfilenames);
                    }
                    else
                    {
                        if (url != null)
                        {
                            loadFileHandler(filename, url[i]);
                            i++;
                        }
                        else
                        {
                            loadFileHandler(filename);
                        }
                    }
                }
            }
        }

        private bool loadFileHandler(string filename, string url = null)
        {
            if (filename == null || filename.EndsWith("~"))
            {
                return false;
            }

            control.Cursor = Cursors.Wait;
            Action EmptyDelegate = delegate () { };
            control.ShadowBoxText.Text = "Loading '" + filename + "'";
            control.ShadowBox.Visibility = Visibility.Visible;
            control.UpdateLayout();
            control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            bool loaded = false;

            SSI_FILE_TYPE ftype = SSI_FILE_TYPE.UNKOWN;

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

                        ftype = SSI_FILE_TYPE.VIDEO;
                        break;

                    case "csv":
                    case "anno":
                        ftype = SSI_FILE_TYPE.CSV;
                        break;

                    case "wav":
                        ftype = SSI_FILE_TYPE.AUDIO;
                        break;

                    case "annotation":
                        ftype = SSI_FILE_TYPE.ANNOTATION;
                        break;

                    case "stream":
                        ftype = SSI_FILE_TYPE.STREAM;
                        break;

                    case "events":
                        ftype = SSI_FILE_TYPE.EVENTS;
                        break;

                    case "eaf":
                        ftype = SSI_FILE_TYPE.EAF;
                        break;

                    case "arff":
                        ftype = SSI_FILE_TYPE.ARFF;
                        break;

                    case "anvil":
                        ftype = SSI_FILE_TYPE.ANVIL;
                        break;

                    case "nova":
                        ftype = SSI_FILE_TYPE.PROJECT;
                        break;
                }
            }

            switch (ftype)
            {
                case SSI_FILE_TYPE.VIDEO:
                    loadMedia(filename, true, url);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.CSV:

                    //Read first line, check if format is an annotation, else interpret it as external csv
                    //Read second line to check for sample rate (only relevant for continous files)
                    string csvanno = "^([0-9]+.[0-9]+|[0-9]+);([0-9]+.[0-9]+|[0-9]+);.*";
                    string legacyanno = "^([0-9]+.[0-9]+|[0-9]+) ([0-9]+.[0-9]+|[0-9]+) .*";
                    string csvcont = "^([0-9]+.[0-9]+|[0-9]+;)(.)[^;]*";
                    string csvcontnew = "^((-?)[0-9]+.[0-9]+|[0-9]+;)+([0-9]+|[0-9]+;)(.)[^;];\\#.*";

                    string type = "";

                    Regex reg = new Regex(csvanno);
                    Regex reglegacy = new Regex(legacyanno);
                    Regex regcont = new Regex(csvcont);
                    Regex regcontnew = new Regex(csvcontnew);
                    StreamReader sr = new StreamReader(filename, System.Text.Encoding.Default);
                    string line = sr.ReadLine();
                    double samplerate = 1.0;

                    if (line != null)
                    {
                        bool iscontinouswithtier = regcontnew.IsMatch(line);
                        if (reg.IsMatch(line) && !iscontinouswithtier) type = "semicolon";
                        else if (reglegacy.IsMatch(line) && !iscontinouswithtier) type = "legacy";
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

                    if (type == "continuous" || type == "semicolon" || type == "legacy")
                    {
                        loadCSVAnnotation(filename, samplerate, type);
                    }
                    else
                    {
                        loadCSV(filename);
                    }

                    loaded = true;
                    break;

                case SSI_FILE_TYPE.AUDIO:
                    loadWav(filename);
                    loadMedia(filename, false);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.ANNOTATION:
                    loadAnnotation(filename);
                    loaded = true;

                    break;

                case SSI_FILE_TYPE.STREAM:
                    loadStream(filename);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.EVENTS:
                    ImportAnnoFromSSIEvents(filename);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.EAF:
                    ImportAnnoFromElan(filename);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.ARFF:
                    loadARFF(filename);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.ANVIL:
                    ImportAnnoFromAnvil(filename);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.PROJECT:
                    loadProject(filename);
                    loaded = true;
                    break;

                default:
                    break;
            }

            control.ShadowBox.Visibility = Visibility.Collapsed;
            control.Cursor = Cursors.Arrow;

            return loaded;
        }

        private void loadAnnotation(string filename)
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Annotation file not found '" + filename + "'");
                return;
            }

            AnnoList annoList = AnnoList.LoadfromFile(filename);
            handleAnnotation(annoList);
        }

        private void loadCSVAnnotation(string filename, double samplerate = 1, string type = "semicolon", string filter = null)
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Annotation file not found '" + filename + "'");
                return;
            }

            AnnoList annoList = AnnoList.LoadFromCSVFile(filename, samplerate, type, filter);
            handleAnnotation(annoList);
        }

        private void loadStream(string filename, string color = "#FF000000", string background = "#FFF0F0F0")
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Stream file not found '" + filename + "'");
                return;
            }

            Signal signal = Signal.LoadStreamFile(filename);
            signalCursor.signalLoaded = true;
            annoCursor.signalLoaded = true;
            if (signal != null && signal.loaded)
            {
                addSignal(signal, color, background);

                if (signal.meta_name == "face" || signal.meta_name == "skeleton")
                {
                    if (control.videoskel.ColumnDefinitions.Count < 2 && mediaList.Medias.Count > 0)
                    {
                        ColumnDefinition split_column = new ColumnDefinition();
                        split_column.Width = new GridLength(1, GridUnitType.Auto);
                        control.videoskel.ColumnDefinitions.Add(split_column);
                        GridSplitter splitter = new GridSplitter();
                        splitter.ResizeDirection = GridResizeDirection.Columns;
                        splitter.Width = 3;
                        splitter.HorizontalAlignment = HorizontalAlignment.Stretch;
                        splitter.VerticalAlignment = VerticalAlignment.Stretch;
                        Grid.SetRowSpan(splitter, 1);
                        //Grid.SetColumn(splitter, 0);
                        Grid.SetColumn(splitter, control.videoskel.ColumnDefinitions.Count - 1);

                        control.videoskel.Children.Add(splitter);

                        ColumnDefinition column = new ColumnDefinition();
                        column.Width = new GridLength(1, GridUnitType.Star);
                        control.videoskel.ColumnDefinitions.Add(column);
                    }
                    else if (control.videoskel.ColumnDefinitions.Count < 2)
                    {
                        ColumnDefinition columvideo = control.videoskel.ColumnDefinitions[0];
                        columvideo.Width = new GridLength(0, GridUnitType.Pixel);

                        ColumnDefinition column = new ColumnDefinition();
                        column.Width = new GridLength(1, GridUnitType.Star);
                        control.videoskel.ColumnDefinitions.Add(column);
                    }
                    control.pointcontrol.AddSignal(signal);
                    visualizepoints = true;
                    control.navigator.playButton.IsEnabled = true;
                }
            }
        }

        private void loadWav(string filename, string color = "#FF000000", string background = "#FFF0F0F0")
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Wav file not found '" + filename + "'");
                return;
            }

            Signal signal = Signal.LoadWaveFile(filename);
            if (signal != null && signal.loaded)
            {
                addSignal(signal, color, background);
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
                    string path = node.InnerText;
                    if (Path.GetExtension(path) == ".wav")
                    {
                        isvideo = false;
                    }
                    loadMedia(FileTools.GetAbsolutePath(path, workdir), isvideo);
                }

                foreach (XmlNode node in doc.SelectNodes("//signal"))
                {
                    string background = node.Attributes["bg"].LastChild.Value;
                    string foreground = node.Attributes["fg"].LastChild.Value;
                    string path = node.InnerText;
                    if (Path.GetExtension(path) == ".wav")
                    {
                        loadWav(FileTools.GetAbsolutePath(path, workdir), foreground, background);
                    }
                    else
                    {
                        loadStream(FileTools.GetAbsolutePath(path, workdir), foreground, background);
                    }
                }

                foreach (XmlNode node in (doc.SelectNodes("//tier")))
                {
                    string path = node.InnerText;
                    if (path == "")
                    {
                        path = node.Attributes["filepath"].LastChild.Value;
                    }
                    loadFileHandler(FileTools.GetAbsolutePath(path, workdir));
                }
            }
            catch (Exception e)
            {
                MessageTools.Error(e.ToString());
            }
        }

        #endregion LOAD

        #region SAVE

        private void saveAnno()
        {
            if (AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.AnnoList != null)
            {
                saveAnno(AnnoTierStatic.Selected.AnnoList, AnnoTierStatic.Selected.AnnoList.FilePath);
            }
        }

        private void saveAnno(string filepath)
        {
            saveAnno(AnnoTierStatic.Selected.AnnoList, filepath);
        }

        private void saveAnno(AnnoList anno, string filepath)
        {
            if (anno != null)
            {
                if (filepath == null
                    || filepath.Split('.')[1] == "eaf"
                    || filepath.Split('.')[1] == "anvil"
                    || filepath.Split('.')[1] == "anno"
                    || filepath.Split('.')[1] == "csv")
                {
                    filepath = FileTools.SaveFileDialog(anno.Scheme.Name, ".annotation", "Annotation (*.annotation)|*.annotation", "");
                }

                if (filepath != null)
                {
                    anno.SaveToFile(filepath);
                }
            }
        }

        private void saveAnnoAs()
        {
            if (AnnoTierStatic.Selected.AnnoList != null)
            {
                string filename = FileTools.SaveFileDialog(AnnoTierStatic.Selected.AnnoList.FileName, ".annotation", "Annotation(*.annotation)|*.annotation", AnnoTierStatic.Selected.AnnoList.Directory);
                saveAnno(filename);
            }
        }

        private void saveProject()
        {
            saveSession();

            string firstmediadir = "";
            if (mediaList.Medias.Count > 0) firstmediadir = mediaList.Medias[0].GetFolderepath();
            else if (signals.Count > 0) firstmediadir = signals[0].Directory;

            string filePath = FileTools.SaveFileDialog("project", ".nova", "NOVA Project (*.nova)|*.nova", firstmediadir);
            if (filePath != null)
            {
                saveProjectTracks(annoTiers, mediaList, signalTracks, filePath);
            }
        }

        private void saveProjectTracks(List<AnnoTier> tracks, MediaList ml, List<ISignalTrack> signal_tracks, string filepath)
        {
            string workdir = Path.GetDirectoryName(filepath);

            StreamWriter sw = new StreamWriter(filepath, false, System.Text.Encoding.Default);
            sw.WriteLine("<nova version=\"1\">");

            sw.WriteLine("\t<medias>");
            if (ml != null)
            {
                foreach (IMedia t in ml.Medias)
                {
                    if (t.GetFilepath() != null)
                    {
                        sw.WriteLine("\t\t<media>" + FileTools.GetRelativePath(t.GetFilepath(), workdir) + "</media>");
                    }
                }
            }
            sw.WriteLine("\t</medias>");

            sw.WriteLine("\t<signals>");
            if (signal_tracks != null)
            {
                foreach (SignalTrack st in signal_tracks)
                {
                    if (st.Signal.FilePath != null)
                    {
                        sw.WriteLine("\t\t<signal bg=\"" + st.Background + "\" fg=\"" + st.SignalColor + "\">" + FileTools.GetRelativePath(st.Signal.FilePath, workdir) + "</signal>");
                    }
                }
            }
            sw.WriteLine("\t</signals>");

            sw.WriteLine("\t<tiers>");
            foreach (AnnoTier t in tracks)
            {
                if (t.AnnoList.FilePath != null)
                {
                    sw.WriteLine("\t\t<tier name=\"" + t.AnnoList.Scheme.Name + "\">" + FileTools.GetRelativePath(t.AnnoList.FilePath, workdir) + "</tier>");
                }
            }
            sw.WriteLine("\t</tiers>");

            sw.WriteLine("</nova>");
            sw.Close();
        }

        #endregion SAVE

        #region IMPORT

        private void ImportAnnoFromElan(string filename)
        {
            AnnoList[] lists = AnnoList.LoadfromElanFile(filename);
            double maxdur = 0;

            if (lists != null)
            {
                foreach (AnnoList list in lists)
                {
                    foreach (AnnoListItem it in list)
                    {
                        if (it.Stop > maxdur)
                        {
                            maxdur = it.Stop;
                        }
                    }

                    annoLists.Add(list);
                    addAnnoTier(list);
                }
            }
            updateTimeRange(maxdur);
            if (annoLists.Count == 1 && maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0)
            {
                fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
            }
        }

        private void ImportAnnoFromAnvil(string filename)
        {
            AnnoList[] lists = AnnoList.LoadfromAnvilFile(filename);
            double maxdur = 0;

            if (lists != null)
            {
                foreach (AnnoList list in lists)
                {
                    foreach (AnnoListItem it in list)
                    {
                        if (it.Stop > maxdur)
                        {
                            maxdur = it.Stop;
                        }
                    }

                    annoLists.Add(list);
                    addAnnoTier(list);
                }
            }
            updateTimeRange(maxdur);
            if (annoLists.Count == 1 && maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0)
            {
                fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
            }
        }

        private void ImportAnnoFromSSIEvents(string filename)
        {
            AnnoList anno = AnnoList.LoadFromEventFile(filename);
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

                annoLists.Add(anno);
                addAnnoTier(anno);
            }

            updateTimeRange(maxdur);
            if (annoLists.Count == 1 && maxdur > Properties.Settings.Default.DefaultZoominSeconds && Properties.Settings.Default.DefaultZoominSeconds != 0)
            {
                fixTimeRange(Properties.Settings.Default.DefaultZoominSeconds);
            }
        }

        #endregion IMPORT

        #region EXPORT

        private void ExportFrameWiseAnnotations(int sr, string seperator, string restclass)
        {
            bool found = false;
            int chunksize = sr;

            int currenttime = 0;
            string headline = "";

            foreach (AnnoTier s in annoTiers)
            {
                headline += s.AnnoList.Scheme.Name + seperator;
            }

            string firstmediadir = "";
            if (mediaList.Medias.Count > 0) firstmediadir = mediaList.Medias[0].GetFolderepath();
            else if (signals.Count > 0) firstmediadir = signals[0].Directory;

            string filepath = FileTools.SaveFileDialog("SampledAnnotations_Export", "txt", "Plain Text(*.txt)|*.txt", firstmediadir);

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
                        foreach (AnnoTier tier in annoTiers)
                        {
                            if (tier.AnnoList.Count > 0)
                            {
                                foreach (AnnoListItem ali in tier.AnnoList)
                                {
                                    if ((ali.Start * 1000) - (ali.Duration * 1000) < currenttime && ali.Stop * 1000 > currenttime)
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

        private void ExportAnnoContinuousToDiscrete()
        {
          

            if (AnnoTierStatic.Selected != null && !AnnoTierStatic.Selected.isDiscreteOrFree)
            {
                Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
                input["labels"] = new UserInputWindow.Input() { Label = "Class labels (separated by ;)", DefaultValue = "LOW;MEDIUM;HIGH" };
                input["thresholds"] = new UserInputWindow.Input() { Label = "Upper thresholds (separated by ;)", DefaultValue = "0.33;0.66;1.0" };
                input["offset"] = new UserInputWindow.Input() { Label = "Optional offset (s)", DefaultValue = "0.0" };
                UserInputWindow dialog = new UserInputWindow("Convert to discrete annotation", input);
                dialog.ShowDialog();

                List<string> classes = new List<string>();
                List<double> upperThresholds = new List<double>();
                double offset = 0.0;

                if (dialog.DialogResult == true)
                {
                    string[] labels = dialog.Result("labels").Split(';');
                    for (int i = 0; i < labels.Length; i++)
                    {
                        classes.Add(labels[i]);
                    }

                    string[] thresholds = dialog.Result("thresholds").Split(';');
                    for (int i = 0; i < thresholds.Length; i++)
                    {
                        double thresh = -1;
                        double.TryParse(thresholds[i], out thresh);
                        if (thresh > -1)
                        {
                            upperThresholds.Add(thresh);
                        }
                        else
                        {
                            MessageTools.Warning("Could not parse input");
                        }
                    }

                    if (thresholds.Length == labels.Length - 1) upperThresholds.Add(1.0);
                    else if (thresholds.Length == labels.Length + 1) classes.Add("REST");
                    else if (thresholds.Length != labels.Length)
                    {
                        MessageBox.Show("Number of labels does not match number of threshholds");
                    }

                    double.TryParse(dialog.Result("offset"), out offset);
                }
                Mouse.SetCursor(Cursors.No);

                AnnoList discretevalues = new AnnoList();
                discretevalues.Scheme = new AnnoScheme();
                discretevalues.Scheme.Type = AnnoScheme.TYPE.DISCRETE;
                discretevalues.Role = AnnoTier.Selected.AnnoList.Role;
                discretevalues.Annotator = AnnoTier.Selected.AnnoList.Annotator;
                discretevalues.Scheme.Name = AnnoTier.Selected.AnnoList.Scheme.Name;

                foreach (string label in classes)
                {
                    AnnoScheme.Label item = new AnnoScheme.Label(label, System.Windows.Media.Colors.Black);
                    discretevalues.Scheme.Labels.Add(item);
                }

                AnnoScheme.Label garbage = new AnnoScheme.Label("GARBAGE", Colors.Black);
                discretevalues.Scheme.Labels.Add(garbage);

                double lowThres = -Double.MaxValue;
                double highThres = 1.0;

                foreach (AnnoListItem ali in AnnoTierStatic.Selected.AnnoList)
                {
                    double val = 0;
                    double.TryParse(ali.Label, out val);

                    for (int i = 0; i < classes.Count; i++)
                    {
                        highThres = upperThresholds[i];
                        if (i > 0) lowThres = upperThresholds[i - 1];
                        else lowThres = -Double.MaxValue;

                        if (val > lowThres && val <= highThres)
                        {
                            if (!(discretevalues.Count > 0 && discretevalues[discretevalues.Count - 1].Label == classes[i]))
                            {


                                AnnoListItem newItem = new AnnoListItem(ali.Start + offset, ali.Duration, classes[i],"", discretevalues.Scheme.GetColorForLabel(classes[i])); 
                                if (newItem.Start < 0.0)
                                {
                                    newItem.Duration = ali.Duration + offset + newItem.Start;
                                    newItem.Start = 0.0;
                                    newItem.Stop = newItem.Duration;
                                }
                                if (newItem.Duration > 0.0) discretevalues.Add(newItem);
                            }
                            else
                            {
                                discretevalues[discretevalues.Count - 1].Stop = discretevalues[discretevalues.Count - 1].Stop + ali.Duration;
                            }
                            break;
                        }
                    }
                }

                AnnoTier.UnselectTier();
                handleAnnotation(discretevalues);

                Mouse.SetCursor(System.Windows.Input.Cursors.Arrow);
            }
            else
            {
                MessageBox.Show("Tier is already discrete");
            }
        }

        private void ExportSignalToContinuous()
        {
            if (SignalTrackStatic.Selected != null && SignalTrackStatic.Selected.Signal != null)
            {
                Signal signal = SignalTrackStatic.Selected.Signal;

                if (signal != null && !SignalTrackStatic.Selected.Signal.IsAudio)
                {
                    AnnoList annoList = signal.ExportToAnno();
                    string newFilePath = FileTools.SaveFileDialog(signal.FileName, ".annotation", "Annotation (*.annotation)|*.annotation", Path.GetDirectoryName(signal.FilePath));
                    if (annoList.SaveToFile(newFilePath))
                    {
                        MessageBoxResult mb = MessageBoxResult.None;
                        mb = MessageBox.Show("Load converted annotation?", "Success", MessageBoxButton.YesNo);
                        if (mb == MessageBoxResult.Yes)
                        {
                            loadAnnotation(newFilePath);
                        }
                    }
                }
            }
        }

        private void ExportAnnoToSignal()
        {
            AnnoTier annoTier = AnnoTierStatic.Selected;
            if (annoTier.AnnoList.Scheme.Type != AnnoScheme.TYPE.CONTINUOUS) MessageBox.Show("Selected a continious track to convert to ssi stream");
            else
            {
                double sr = 1000.0 / (annoTier.AnnoList[0].Duration * 1000);
                double from = 0.0;
                double to = annoTier.AnnoList[annoTier.AnnoList.Count - 1].Stop;
                int num = annoTier.AnnoList.Count;
                string ftype = "ASCII";
                string type = "FLOAT";
                int by = sizeof(float);
                int dim = 1;
                int ms = Environment.TickCount;

                if (annoTier.AnnoList.HasChanged)
                {
                    MessageBoxResult m = MessageBoxResult.None;
                    m = MessageBox.Show("You need to save continous annotations on tier #" + annoTier.AnnoList.Scheme.Name + " first", "Confirm", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    if (m == MessageBoxResult.OK)
                    {
                        saveAnnoAs();
                        annoTier.AnnoList.HasChanged = false;
                    }
                }

                string filename = Path.GetDirectoryName(annoTier.AnnoList.FilePath) + "\\" + annoTier.AnnoList.Scheme.Name + ".stream";

                StreamWriter swheader = new StreamWriter(filename, false, System.Text.Encoding.Default);
                swheader.WriteLine("<?xml version=\"1.0\" ?>");
                swheader.WriteLine("<stream ssi-v=\"2\">");
                swheader.WriteLine("\t<info ftype=\"" + ftype + "\" sr=\"" + sr.ToString("0.000000", CultureInfo.InvariantCulture) + "\" dim=\"" + dim.ToString() + "\" byte=\"" + by.ToString() + "\" type=\"" + type + "\" />");
                swheader.WriteLine("\t<time ms=\"" + ms + "\" local=\"" + DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss") + "\" system=\"" + DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss") + "\"/>");
                swheader.WriteLine("\t<chunk from=\"" + from.ToString("0.000000", CultureInfo.InvariantCulture) + "\" to=\"" + to.ToString("0.000000", CultureInfo.InvariantCulture) + "\" byte=\"" + "0" + "\" num=\"" + num + "\"/>");

                swheader.WriteLine("</stream>");

                swheader.Close();

                StreamWriter swdata = new StreamWriter(filename + "~", false, System.Text.Encoding.Default);
                foreach (AnnoListItem i in annoTier.AnnoList)
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

        #endregion EXPORT

        #region EVENTHANDLER

        private void saveAnno_Click(object sender, RoutedEventArgs e)
        {
            saveAnno();
        }

        private void saveProject_Click(object sender, RoutedEventArgs e)
        {
            if (DatabaseLoaded)
            {
                MessageBox.Show("Storing a project file is meant for working with annotation files only , not the database.");
            }
            else if (annoTiers.Count == 0)
            {
                MessageBox.Show("No annotations have been loaded.");
            }
            else saveProject();
        }

        private void exportSamples_Click(object sender, RoutedEventArgs e)
        {
            ExportSamplesWindow window = new ExportSamplesWindow();
            foreach (AnnoTier tier in this.annoTiers)
            {
                if (tier.AnnoList.FilePath != null && !tier.AnnoList.LoadedFromDB && (tier.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE ||
                    tier.AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE))
                {

                    window.control.annoComboBox.Items.Add(tier.AnnoList.FilePath);
                }
            }
            foreach (Signal signal in signals)
            {
                window.control.signalAvailableListBox.Items.Add(signal.FilePath);
            }
            window.ShowDialog();
        }

        private void exportAnnoToFrameWiseMenu_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
            input["separator"] = new UserInputWindow.Input() { Label = "File seperator", DefaultValue = ";" };
            input["sr"] = new UserInputWindow.Input() { Label = "Sample rate", DefaultValue = "100" };
            input["label"] = new UserInputWindow.Input() { Label = "Label of rest class", DefaultValue = "REST" };
            UserInputWindow dialog = new UserInputWindow("Export frame-wise annotation", input);
            dialog.ShowDialog();

            if (dialog.DialogResult == true)
            {
                int sr;
                if (Int32.TryParse(dialog.Result("sr"), out sr))
                {
                    ExportFrameWiseAnnotations(sr, dialog.Result("separator"), dialog.Result("label"));
                }
            }
        }

        private void exportAnnoContinuousToDiscrete_Click(object sender, RoutedEventArgs e)
        {
            ExportAnnoContinuousToDiscrete();
        }

        private void exportSignalToContinuous_Click(object sender, RoutedEventArgs e)
        {
            ExportSignalToContinuous();
        }

        private void exportAnnoToSignal_Click(object sender, RoutedEventArgs e)
        {
            ExportAnnoToSignal();
        }

        private void exportTierToXPS_Click(object sender, RoutedEventArgs e)
        {
            if (AnnoTierStatic.Selected != null)
            {
                string filepath = FileTools.SaveFileDialog(AnnoTierStatic.Selected.AnnoList.Scheme.Name, "xps", "XPS (*.xps)|*.xps", AnnoTierStatic.Selected.AnnoList.Directory);
                if (filepath != null)
                {
                    var uri = new Uri(filepath);
                    if (AnnoTierStatic.Selected.isDiscreteOrFree) AnnoTierStatic.Selected.Background = AnnoTierStatic.Selected.BackgroundBrush;
                    AnnoTierStatic.Selected.ExportToXPS(uri, AnnoTierStatic.Selected);
                    AnnoTierStatic.Selected.select(true);
                    AnnoTierStatic.Selected.TimeRangeChanged(MainHandler.Time);
                }
            }
        }

        private void exportTierToPNG_Click(object sender, RoutedEventArgs e)
        {
            if (AnnoTierStatic.Selected != null)
            {
                string filepath = FileTools.SaveFileDialog(AnnoTierStatic.Selected.AnnoList.Scheme.Name, "xps", "PNG (*.png)|*.png", AnnoTierStatic.Selected.AnnoList.Directory);
                if (filepath != null)
                {
                    var uri = new Uri(filepath);
                    if (AnnoTierStatic.Selected.isDiscreteOrFree) AnnoTierStatic.Selected.Background = AnnoTierStatic.Selected.BackgroundBrush;
                    AnnoTierStatic.Selected.ExportToPng(uri, AnnoTierStatic.Selected);
                    AnnoTierStatic.Selected.select(true);
                    AnnoTierStatic.Selected.TimeRangeChanged(MainHandler.Time);
                }
            }
        }

        private void exportSignalToXPS_Click(object sender, RoutedEventArgs e)
        {
            if (SignalTrackStatic.Selected != null)
            {
                string filepath = FileTools.SaveFileDialog(SignalTrackStatic.Selected.Name, "xps", "XPS (*.xps)|*.xps", SignalTrackStatic.Selected.Signal.Directory);
                if (filepath != null)
                {
                    var uri = new Uri(filepath);
                    SignalTrackStatic.Selected.ExportToXPS(uri, SignalTrackStatic.Selected);
                }
            }
        }

        private void exportSignalToPNG_Click(object sender, RoutedEventArgs e)
        {
            if (SignalTrackStatic.Selected != null)
            {
                string filepath = FileTools.SaveFileDialog(SignalTrackStatic.Selected.Name, "png", "PNG (*.png)|*.png", SignalTrackStatic.Selected.Signal.Directory);
                if (filepath != null)
                {
                    var uri = new Uri(filepath);

                    if (SignalTrackStatic.Selected != null)
                    {
                        SignalTrackStatic.Selected.ExportToPng(uri, SignalTrackStatic.Selected);
                    }
                }
            }
        }

        private void exportAnnoToCSV_Click(object sender, RoutedEventArgs e)
        {
            if (AnnoTierStatic.Selected != null)
            {
                AnnoTierStatic.Selected.AnnoList.SaveToCSVFile();
            }
        }

        #endregion EVENTHANDLER
    }
}