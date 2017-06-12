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

        private void loadFiles_Click(object sender, RoutedEventArgs e)
        {
            loadFiles();
        }

        public void loadMultipleFilesOrDirectory(string[] filenames)
        {
            Array.Sort(filenames, StringComparer.InvariantCulture);

            foreach (string filename in filenames)
            {
                if (File.Exists(filename) || Directory.Exists(filename))
                {
                    FileAttributes attr = File.GetAttributes(filename);
                    if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        string[] subfilenames = Directory.GetFiles(filename);
                        loadMultipleFilesOrDirectory(subfilenames);
                    }
                    else
                    {
                        loadFile(filename);
                    }
                }
            }
        }

        private bool loadFile(string filepath)
        {
            return loadFile(filepath, Defaults.Colors.Foreground, Defaults.Colors.Background);
        }

        private bool loadFile(string filepath,
            Color foreground,
            Color background)
        {
            if (filepath == null || filepath.EndsWith("~"))
            {
                return false;
            }

            control.Cursor = Cursors.Wait;
            Action EmptyDelegate = delegate () { };
            control.ShadowBoxText.Text = "Loading '" + filepath + "'";
            control.ShadowBox.Visibility = Visibility.Visible;
            control.UpdateLayout();
            control.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);

            bool loaded = false;

            SSI_FILE_TYPE ftype = SSI_FILE_TYPE.UNKOWN;

            int index = filepath.LastIndexOf('.');
            if (index > 0)
            {
                string type = filepath.Substring(index + 1);
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
                    loadMediaFile(filepath, MediaType.VIDEO);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.CSV:
                    loadCSVFile(filepath, foreground, background);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.AUDIO:
                    Signal signal = loadWAVSignalFile(filepath, foreground, background);
                    IMedia media = loadMediaFile(filepath, MediaType.AUDIO);                    
                    if (signal != null)
                    {
                        signal.Media = media;
                    }
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.ANNOTATION:
                    loadAnnoFile(filepath);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.STREAM:
                    loadSignalFile(filepath, foreground, background);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.EVENTS:
                    ImportAnnoFromSSIEvents(filepath);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.EAF:
                    ImportAnnoFromElan(filepath);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.ARFF:
                    loadARFFAnnoFile(filepath);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.ANVIL:
                    ImportAnnoFromAnvil(filepath);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.PROJECT:
                    loadProjectFile(filepath);
                    loaded = true;
                    // workaround
                    foreach (AnnoList annoList in annoLists)
                    {
                        annoList.HasChanged = false;
                    }
                    break;

                default:
                    break;
            }

            control.ShadowBox.Visibility = Visibility.Collapsed;
            control.Cursor = Cursors.Arrow;

            return loaded;
        }

        private void addMedia(IMedia media)
        {
            double time = Time.TimeFromPixel(signalCursor.X);
            media.Move(time);
            mediaList.Add(media);

            if (media.GetMediaType() != MediaType.AUDIO)
            {
                addMediaBox(media);
            }

            control.navigator.playButton.IsEnabled = true;
        }

        private IMedia loadMediaFile(string filename, MediaType type)
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Media file not found '" + filename + "'");
                return null;
            }

            MediaKit media = new MediaKit(filename, type);
            // Media media = new Media(filename, type);   
            media.OnMediaMouseDown += OnMediaMouseDown;
            media.OnMediaMouseUp += OnMediaMouseUp;
            media.OnMediaMouseMove += OnMediaMouseMove;
            addMedia(media);

            return media;
        }

        private void loadAnnoFile(string filename)
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Annotation file not found '" + filename + "'");
                return;
            }

            AnnoList annoList = AnnoList.LoadfromFile(filename);            
            addAnnoTierFromList(annoList);

            foreach (AnnoTrigger trigger in annoList.Meta.Trigger)
            {                                
                mediaList.Add(trigger);
            }
        }

        private void loadCSVAnnoFile(string filename, double samplerate = 1, string type = "semicolon", string filter = null)
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Annotation file not found '" + filename + "'");
                return;
            }

            AnnoList annoList = AnnoList.LoadFromCSVFile(filename, samplerate, type, filter);
            addAnnoTierFromList(annoList);
        }

        private void loadARFFAnnoFile(string filename)
        {
            loadARFFAnnoFile(filename, Defaults.Colors.Foreground, Defaults.Colors.Background);
        }

        private void loadARFFAnnoFile(string filename, Color signalColor, Color backgroundColor)
        {
            Signal signal = Signal.LoadARFFFile(filename);
            if (signal != null && signal.loaded)
            {
                addSignalTrack(signal, signalColor, backgroundColor);
            }
        }

        private void loadSignalFile(string filename)
        {
            loadSignalFile(filename, Defaults.Colors.Foreground, Defaults.Colors.Background);
        }

        private void loadSignalFile(string filename, Color signalColor, Color backgroundColor)
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Stream file not found '" + filename + "'");
                return;
            }

            Signal signal = Signal.LoadStreamFile(filename);
            if (signal != null && signal.loaded)
            {                
                if (signal.Meta.ContainsKey("name") && signal.Meta["name"] == "face")
                {
                    IMedia media = new Face(filename, signal);
                    addMedia(media);
                }
                else if (signal.Meta.ContainsKey("name") && signal.Meta["name"] == "skeleton")
                {
                    IMedia media = new Skeleton(filename, signal);
                    addMedia(media);
                }
                else
                {
                    signalCursor.signalLoaded = true;
                    annoCursor.signalLoaded = true;
                    addSignalTrack(signal, signalColor, backgroundColor);                  
                }
            }
        }

        private Signal loadWAVSignalFile(string filename, Color signalColor, Color backgroundColor)
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Wav file not found '" + filename + "'");
                return null;
            }

            Signal signal = Signal.LoadWaveFile(filename);
            if (signal != null && signal.loaded)
            {
                addSignalTrack(signal, signalColor, backgroundColor);
            }

            return signal;
        }

        private void loadCSVFile(string filename, Color foreground, Color background)
        {
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
            string[] split = line.Split(';');
            double samplerate = 1.0;

            if (line != null)
            {
                bool iscontinouswithtier = regcontnew.IsMatch(line);

               

                if ((regcont.IsMatch(line) || iscontinouswithtier))
                {
                    string[] data = line.Split(';');

                    if(data.Length == 3)
                    {
                        try
                        {
                            double start = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                            line = sr.ReadLine();
                            data = line.Split(';');
                            double start2 = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                            samplerate = 1 / (start2 - start);
                            type = "continuous";
                        }
                        catch
                        {
                            MessageBox.Show("Error reading continuous file");
                        }
                    }

                    else if (reg.IsMatch(line) && !iscontinouswithtier) type = "semicolon";

                    else if (reglegacy.IsMatch(line) && !iscontinouswithtier) type = "legacy";

                }

               
              

                sr.Close();
            }
            else type = "semicolon";

            if (type == "continuous" || type == "semicolon" || type == "legacy")
            {
                loadCSVAnnoFile(filename, samplerate, type);
            }
            else
            {
                loadCSVSignalFile(filename, foreground, background);
            }
        }

        private void loadCSVSignalFile(string filename, Color signalColor, Color backgroundColor)
        {
            Signal signal = Signal.LoadCSVFile(filename);
            if (signal != null && signal.loaded)
            {
                addSignalTrack(signal, signalColor, backgroundColor);
            }
        }
   
        public void loadProjectFile(string filepath)
        {
            clearWorkspace();

            string workdir = Path.GetDirectoryName(filepath);

            XmlDocument doc = new XmlDocument();
            try
            {
                doc.Load(filepath);
               
                foreach (XmlNode node in doc.SelectNodes("//media"))
                {                    
                    string path = FileTools.GetAbsolutePath(node.InnerText, workdir);
                    loadFile(path);
                }

                foreach (XmlNode node in doc.SelectNodes("//signal"))
                {
                    Color background = Defaults.Colors.Background;
                    Color foreground = Defaults.Colors.Foreground;
                    if (node.Attributes["bg"] != null)
                    {
                        background = (Color)ColorConverter.ConvertFromString(node.Attributes["bg"].LastChild.Value);
                    }
                    if (node.Attributes["fg"] != null)
                    {
                        foreground = (Color)ColorConverter.ConvertFromString(node.Attributes["fg"].LastChild.Value);
                    }
                    string path = FileTools.GetAbsolutePath(node.InnerText, workdir);
                    loadFile(path, foreground, background);
                }

                if (DatabaseHandler.IsConnected)
                {
                    XmlNode node = doc.SelectSingleNode("//tiers");
                    if(node!=null && node.Attributes["database"] != null)
                    {
                        DatabaseHandler.ChangeDatabase(node.Attributes["database"].LastChild.Value);
                    }
                }

                foreach (XmlNode node in (doc.SelectNodes("//tier")))
                {
                    string path = node.InnerText;
                    if (!Path.HasExtension(path))
                    {
                        AnnoList annoList = DatabaseHandler.LoadAnnoList(path);
                        if (annoList != null)
                        {
                            addAnnoTier(annoList);
                        }
                        else
                        {
                            MessageTools.Warning("Could not load annotation from database with id '" + node.InnerText + "'");
                        }
                    }
                    else
                    {                        
                        if (path == "")
                        {
                            path = node.Attributes["filepath"].LastChild.Value;
                        }
                        path = FileTools.GetAbsolutePath(path, workdir);
                        loadFile(path);
                    }                    
                }
            }
            catch (Exception e)
            {
                MessageTools.Error(e.ToString());
            }
        }

        private void loadProject()
        {
            string[] filePath = FileTools.OpenFileDialog("NOVA Project (*.nova)|*.nova", false);
            if (filePath != null && filePath.Length > 0)
            {
                loadProjectFile(filePath[0]);
            }
        }

        #endregion LOAD

        #region SAVE

        private void saveSelectedAnno()
        {
            if (AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.AnnoList != null)
            {
                AnnoTierStatic.Selected.AnnoList.Save(databaseSessionStreams);
                updateAnnoInfo(AnnoTierStatic.Selected);
            }
        }

        private void saveSelectedAnnoAs()
        {
            if (AnnoTierStatic.Selected.AnnoList != null)
            {
                string directory = AnnoTierStatic.Selected.AnnoList.Source.File.Directory;
                string path = FileTools.SaveFileDialog(AnnoTierStatic.Selected.AnnoList.Source.File.Name, ".annotation", "Annotation(*.annotation)|*.annotation", AnnoTierStatic.Selected.AnnoList.Source.File.Directory);
                if (path != null)
                {
                    AnnoTierStatic.Selected.AnnoList.Source.File.Path = path;
                    AnnoTierStatic.Selected.AnnoList.HasChanged = true;
                    AnnoTierStatic.Selected.AnnoList.Save();
                    updateAnnoInfo(AnnoTierStatic.Selected);
                }
            }
        }

        private void saveAllAnnos()
        {
            foreach (AnnoTier tier in annoTiers)
            {
                tier.AnnoList.Save();
            }
        }

        private void saveProject()
        {
            saveAllAnnos();

            string firstmediadir = "";
            if (mediaList.Count > 0) firstmediadir = mediaList[0].GetDirectory();
            else if (signals.Count > 0) firstmediadir = signals[0].Directory;

            string filePath = FileTools.SaveFileDialog("project", ".nova", "NOVA Project (*.nova)|*.nova", firstmediadir);
            if (filePath != null)
            {
                saveProjectFile(annoTiers, mediaBoxes, signalTracks, filePath);
            }
        }

        private void saveProjectFile(List<AnnoTier> annoTiers, List<MediaBox> mediaBoxes, List<SignalTrack> signalTracks, string filepath)
        {
            string workdir = Path.GetDirectoryName(filepath);

            StreamWriter sw = new StreamWriter(filepath, false, System.Text.Encoding.Default);
            sw.WriteLine("<nova version=\"1\">");

            sw.WriteLine("\t<medias>");
            if (mediaList != null)
            {
                foreach (MediaBox box in mediaBoxes)
                {
                    if (box.Media.GetFilepath() != null)
                    {
                        sw.WriteLine("\t\t<media>" + FileTools.GetRelativePath(box.Media.GetFilepath(), workdir) + "</media>");
                    }
                }
            }
            sw.WriteLine("\t</medias>");

            sw.WriteLine("\t<signals>");
            if (signalTracks != null)
            {
                foreach (SignalTrack st in signalTracks)
                {
                    if (st.Signal.FilePath != null)
                    {
                        sw.WriteLine("\t\t<signal bg=\"" + st.Background + "\" fg=\"" + st.SignalColor + "\">" + FileTools.GetRelativePath(st.Signal.FilePath, workdir) + "</signal>");
                    }
                }
            }
            sw.WriteLine("\t</signals>");
           
            if (DatabaseHandler.IsConnected && DatabaseHandler.IsDatabase && DatabaseHandler.IsSession)
            {
                sw.WriteLine("\t<tiers database=\"" + DatabaseHandler.DatabaseName + "\">");
            }
            else
            {
                sw.WriteLine("\t<tiers>");
            }

            foreach (AnnoTier t in annoTiers)
            {
                if (t.AnnoList.Source.HasFile )
                {
                    sw.WriteLine("\t\t<tier>" + FileTools.GetRelativePath(t.AnnoList.Path, workdir) + "</tier>");
                }
                else if (t.AnnoList.Source.HasDatabase)
                {
                    sw.WriteLine("\t\t<tier>" + t.AnnoList.Path + "</tier>");
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
            if (annoLists.Count == 1 && maxdur > Properties.Settings.Default.DefaultZoomInSeconds && Properties.Settings.Default.DefaultZoomInSeconds != 0)
            {
                fixTimeRange(Properties.Settings.Default.DefaultZoomInSeconds);
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
            if (annoLists.Count == 1 && maxdur > Properties.Settings.Default.DefaultZoomInSeconds && Properties.Settings.Default.DefaultZoomInSeconds != 0)
            {
                fixTimeRange(Properties.Settings.Default.DefaultZoomInSeconds);
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
            if (annoLists.Count == 1 && maxdur > Properties.Settings.Default.DefaultZoomInSeconds && Properties.Settings.Default.DefaultZoomInSeconds != 0)
            {
                fixTimeRange(Properties.Settings.Default.DefaultZoomInSeconds);
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
                headline += s.AnnoList.Meta.Role + "_" +  s.AnnoList.Scheme.Name + seperator;
            }

            string firstmediadir = "";
            if (mediaList.Count > 0) firstmediadir = mediaList[0].GetDirectory();
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
                            }
                            else
                            {
                                found = false;
                            }
                            if (!found)
                                headline += restclass + seperator;
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
          

            if (AnnoTierStatic.Selected != null && !AnnoTierStatic.Selected.IsDiscreteOrFree)
            {
                Dictionary<string, UserInputWindow.Input> input = new Dictionary<string, UserInputWindow.Input>();
                input["labels"] = new UserInputWindow.Input() { Label = "Class labels (separated by ;)", DefaultValue = Properties.Settings.Default.ConvertToDiscreteClasses };
                input["thresholds"] = new UserInputWindow.Input() { Label = "Upper thresholds (separated by ;)", DefaultValue = Properties.Settings.Default.ConvertToDiscreteThreshs };
                input["offset"] = new UserInputWindow.Input() { Label = "Optional offset (s)", DefaultValue = Properties.Settings.Default.ConvertToDiscreteDelays };
                UserInputWindow dialog = new UserInputWindow("Convert to discrete annotation", input);
                dialog.ShowDialog();

                List<string> classes = new List<string>();
                List<double> upperThresholds = new List<double>();
                double offset = 0.0;

                if (dialog.DialogResult == true)
                {
                    Properties.Settings.Default.ConvertToDiscreteClasses = dialog.Result("labels");
                    Properties.Settings.Default.ConvertToDiscreteThreshs = dialog.Result("thresholds");
                    Properties.Settings.Default.ConvertToDiscreteDelays = dialog.Result("offset");
                    Properties.Settings.Default.Save();


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
                discretevalues.Meta.Role = AnnoTier.Selected.AnnoList.Meta.Role;
                discretevalues.Meta.Annotator = AnnoTier.Selected.AnnoList.Meta.Annotator;
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

                AnnoTier.Unselect();
                addAnnoTierFromList(discretevalues);

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
                    annoList.Source.File.Path = newFilePath;
                    if (annoList.Save())
                    {
                        MessageBoxResult mb = MessageBoxResult.None;
                        mb = MessageBox.Show("Load converted annotation?", "Success", MessageBoxButton.YesNo);
                        if (mb == MessageBoxResult.Yes)
                        {
                            loadAnnoFile(newFilePath);
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
                double sr = 1 / annoTier.AnnoList[0].Duration;
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
                    m = MessageBox.Show("You need to save continous annotations on tier " + annoTier.AnnoList.Scheme.Name + " first", "Confirm", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    if (m == MessageBoxResult.OK)
                    {
                        saveSelectedAnnoAs();
                    }
                }

                string filename = Path.GetDirectoryName(annoTier.AnnoList.Source.File.Path) + "\\" + annoTier.AnnoList.Scheme.Name + ".stream";

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
                    loadSignalFile(filename);
                }
            }
        }

        #endregion EXPORT

        #region EVENTHANDLER

        private void annoSave_Click(object sender, RoutedEventArgs e)
        {
            saveSelectedAnno();
        }

        private void annoSaveAs_Click(object sender, RoutedEventArgs e)
        {
            saveSelectedAnnoAs();
        }

        private void fileSaveProject_Click(object sender, RoutedEventArgs e)
        {        
            saveProject();         
        }

        private void fileLoadProject_Click(object sender, RoutedEventArgs e)
        {
            loadProject();
        }

        private void exportSamples_Click(object sender, RoutedEventArgs e)
        {
            ExportSamplesWindow window = new ExportSamplesWindow();
            foreach (AnnoTier tier in this.annoTiers)
            {
                if (tier.AnnoList.Source.HasFile && 
                    (tier.AnnoList.Scheme.Type == AnnoScheme.TYPE.DISCRETE ||
                    tier.AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE))
                {
                    window.control.annoComboBox.Items.Add(tier.AnnoList.Source.File.Path);
                }
            }
            foreach (Signal signal in signals)
            {
                window.control.signalAvailableListBox.Items.Add(signal.FilePath);
            }
            window.ShowDialog();
        }

        private void exportToGenie_Click(object sender, RoutedEventArgs e)
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

        private void convertAnnoContinuousToDiscrete_Click(object sender, RoutedEventArgs e)
        {
            ExportAnnoContinuousToDiscrete();
        }

        private void convertSignalToAnnoContinuous_Click(object sender, RoutedEventArgs e)
        {
            ExportSignalToContinuous();
        }

        private void convertAnnoToSignal_Click(object sender, RoutedEventArgs e)
        {
            ExportAnnoToSignal();
        }

        private void exportTierToXPS_Click(object sender, RoutedEventArgs e)
        {
            if (AnnoTierStatic.Selected != null)
            {
                string filepath = FileTools.SaveFileDialog(AnnoTierStatic.Selected.AnnoList.Scheme.Name, "xps", "XPS (*.xps)|*.xps", AnnoTierStatic.Selected.AnnoList.Source.File.Directory);
                if (filepath != null)
                {
                    var uri = new Uri(filepath);                    
                    AnnoTierStatic.Selected.ExportToXPS(uri, AnnoTierStatic.Selected);
                    AnnoTierStatic.Selected.TimeRangeChanged(Time);
                }
            }
        }

        private void exportTierToPNG_Click(object sender, RoutedEventArgs e)
        {
            if (AnnoTierStatic.Selected != null)
            {
                string filepath = FileTools.SaveFileDialog(AnnoTierStatic.Selected.AnnoList.Scheme.Name, "xps", "PNG (*.png)|*.png", AnnoTierStatic.Selected.AnnoList.Source.File.Directory);
                if (filepath != null)
                {
                    var uri = new Uri(filepath);                    
                    AnnoTierStatic.Selected.ExportToPng(uri, AnnoTierStatic.Selected);
                    AnnoTierStatic.Selected.TimeRangeChanged(Time);
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