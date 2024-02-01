
using SharpCompress.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Linq;
using Image = System.Windows.Controls.Image;
using System.Drawing.Imaging;

using Dicom;
using Dicom.Imaging;
using PropertyTools.Wpf;

using System.Threading.Tasks;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

using CSJ2K.j2k.image;
using FFMediaToolkit.Encoding;

using VideoCodec = FFMediaToolkit.Encoding.VideoCodec;
using Point = System.Drawing.Point;
using FFMediaToolkit.Graphics;
using System.Windows.Forms.VisualStyles;
using NAudio.CoreAudioApi;
using Tamir.SharpSsh.jsch;
using WPFMediaKit.DirectShow.Controls;
using Tamir.SharpSsh.java.lang;
using NDtw;
using System.Windows.Media.Media3D;

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
            int dcoms = 0;
            foreach (string filename in filenames)
            {
                if (filename.ToLower().EndsWith("dcm")){ //maybe use this for all images

                    dcoms++;

                }
                else { 

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

            if(dcoms == filenames.Count()){
                showShadowBox("Loading '" + filenames[0].ToString() + "'");
                LoadDicomFile(filenames);
                hideShadowBox();
                polygonUtilities.updateImageSize();
            }

          

        }

        public bool loadFile(string filepath, Dictionary<int, string> dimnames = null)
        {
            return loadFile(filepath, Defaults.Colors.Foreground, Defaults.Colors.Background, dimnames);
        }

        private bool loadFile(string filepath,
            Color foreground,
            Color background,
            Dictionary<int,string> dimnames)
        {
            if (filepath == null || filepath.EndsWith("~") || !File.Exists(filepath))
            {
                return false;
            }

            showShadowBox("Loading '" + filepath + "'");

            bool loaded = false;

            SSI_FILE_TYPE ftype = SSI_FILE_TYPE.UNKOWN;

            int index = filepath.LastIndexOf('.');
            if (index > 0)
            {
                string type = filepath.Substring(index + 1).ToLower();
                switch (type)
                {
                    case "mp4":
                    case "avi":
                    case "wmv":
                    case "mov":                                        
                    case "mkv":
                    case "divx":
                    case "mpg":                    
                    case "PNG":
                    case "jpg":
                    case "png":
                    case "jpeg":
                    case "gif":
                    case "webm":

                        ftype = SSI_FILE_TYPE.VIDEO;
                        Time.TotalDuration = 1; // This allows to annotate images
                        break;

                    case "csv":
                    case "anno":
                        ftype = SSI_FILE_TYPE.CSV;
                        break;
                    
                    case "wma":
                    case "wav":
                    case "mp3":
                    case "m4a":
                    case "flac":
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

                    case "srt":
                        ftype = SSI_FILE_TYPE.SRT;
                        break;

                    case "arff":
                        ftype = SSI_FILE_TYPE.ARFF;
                        break;

                    case "anvil":
                        ftype = SSI_FILE_TYPE.ANVIL;
                        break;

                    case "odf":
                        ftype = SSI_FILE_TYPE.NOLDUS;
                        break;
                    case "dcm":
                    case "dicom":
                        ftype = SSI_FILE_TYPE.DICOM;
                        break;

                    case "nova":
                        ftype = SSI_FILE_TYPE.PROJECT;
                        break;
                }
            }

            switch (ftype)
            {
                case SSI_FILE_TYPE.VIDEO:

                    int videoCount = 0;
                    foreach(IMedia media in mediaList)
                    {
                        if (media.GetMediaType() == MediaType.VIDEO)
                            videoCount++;
                    }

                    if (videoCount > 0)
                    {
                        foreach(AnnoList anno in annoLists)
                        {
                            if (anno.Scheme.Type == AnnoScheme.TYPE.POLYGON || anno.Scheme.Type == AnnoScheme.TYPE.DISCRETE_POLYGON)
                            {
                                MessageBox.Show("Only one video can be loaded with the polygon annotation!", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                                hideShadowBox();
                                return false;
                            }
                        }
                    }

                    loadMediaFile(filepath, MediaType.VIDEO);

                    if (Properties.Settings.Default.DrawVideoWavform)
                    {
                        Signal signal = loadAudioSignalFile(filepath, foreground, background);

                        if (signal != null)
                        {
                            signal.Media = loadMediaFile(filepath, MediaType.AUDIO);
                        }
                    }

                    else loadMediaFile(filepath, MediaType.AUDIO);

                    loaded = true;
                    break;

                case SSI_FILE_TYPE.AUDIO:

                    Signal signala = loadAudioSignalFile(filepath, foreground, background);
                    if (signala == null)
                    {
                        MessageBox.Show("Can't open Audio file " + filepath);
                    }
                    else
                    {
                        signala.Media = loadMediaFile(filepath, MediaType.AUDIO);
                    }
                  


                    loaded = true;

                    break;

                case SSI_FILE_TYPE.DICOM:
                    string[] array = new string[1];
                    array[0] = filepath;
                    LoadDicomFile(array);
                    loaded = true;
                    break;
                case SSI_FILE_TYPE.ANNOTATION:
                    loadAnnoFile(filepath);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.STREAM:
                    loadSignalFile(filepath, foreground, background, dimnames);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.EVENTS:
                    ImportAnnoFromSSIEvents(filepath);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.CSV:
                    loadCSVFile(filepath, foreground, background);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.EAF:
                    ImportAnnoFromElan(filepath);
                    loaded = true;
                    break;

                case SSI_FILE_TYPE.SRT:
                    bool wordlevel = Properties.Settings.Default.SRTwordlevel;
                    ImportAnnoFromSubtitles(filepath, wordlevel);
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

                case SSI_FILE_TYPE.NOLDUS:
                    ImportAnnoFromNoldus(filepath);
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

            hideShadowBox();            

            if(loaded)
                polygonUtilities.updateImageSize();

            return loaded;
        }

        private void addMedia(IMedia media)
        {
            media.Play();
            media.Pause();
            media.Move(0);
            mediaList.Add(media);

            if (media.GetMediaType() != MediaType.AUDIO)
            {
                addMediaBox(media);
            }

            control.navigator.playButton.IsEnabled = true;
        }



        public byte[] GetData(DicomFile m_File)
        {
            var image = new DicomImage(m_File.Dataset);
            using (var image2 = image.RenderImage())
            {
                using (var bitmap = image2.As<System.Drawing.Bitmap>())
                {
                    var stream = new MemoryStream();
                    bitmap.Save(stream, ImageFormat.Jpeg);
                    return stream.ToArray();
                }
            }
        }


    

        private IMedia LoadDicomFile(string[] fileNames)
        {
            string name = Path.GetFileNameWithoutExtension(fileNames[0]);
            var userDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\nova";
            int index = 0;

            foreach (string fileName in fileNames)
            {
            DicomFile m_File = DicomFile.Open(fileName);
            var dicomImage = new DicomImage(m_File.Dataset);
            var frames =
                Enumerable.Range(0, dicomImage.NumberOfFrames)
                    .Select(selector: frame => dicomImage.RenderImage(frame).As<System.Drawing.Bitmap>());

           

           
            Directory.CreateDirectory(userDataFolder + "\\dicom\\" + name + "\\");
            int size = frames.Count<System.Drawing.Bitmap>();

            foreach (var frame in frames)
            {
            
                    frame.Save(userDataFolder + "\\dicom\\" + name + "\\" + index.ToString("0000") + ".bmp");
                    index++; 
            }
            }

            if (!File.Exists("runtimes\\win-x64\\native\\ffmpeg.exe"))
            {

                GetFFMPEG();
            }


            string inputFolder = userDataFolder + "\\dicom\\" + name + "\\";
            var files = Directory.GetFiles(inputFolder,"*.bmp");
            System.Drawing.Image img = System.Drawing.Image.FromFile(files[0]);
            var settings = new VideoEncoderSettings(width: img.Width, height: img.Height, framerate: 25, codec: VideoCodec.H264)
            {
                EncoderPreset = EncoderPreset.Medium,
                CRF = 18
            };

            var file = MediaBuilder.CreateContainer(userDataFolder + "\\dicom\\" + name + "\\" + name + ".mp4").WithVideo(settings).Create();
            foreach (var inputFile in files)
            {
                var binInputFile = File.ReadAllBytes(inputFile);
                var memInput = new MemoryStream(binInputFile);
                var bitmap = System.Drawing.Bitmap.FromStream(memInput) as System.Drawing.Bitmap;
                var rect = new System.Drawing.Rectangle(Point.Empty, bitmap.Size);
                var bitLock = bitmap.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                ImageData bitmapData = ImageData.FromPointer(bitLock.Scan0, ImagePixelFormat.Bgr24, bitmap.Size);
                file.Video.AddFrame(bitmapData);
                bitmap.UnlockBits(bitLock);
            }

            file.Dispose();


          
            if (MediaBackend == MEDIABACKEND.MEDIAKIT)
            {
                MediaKit media = new MediaKit(userDataFolder + "\\dicom\\" + name + "\\" + name + ".mp4", MediaType.VIDEO);
                addEvents(null, media);
                addMedia(media);
              
                return media;
            }
            else if (MediaBackend == MEDIABACKEND.MEDIA)
            {
                MessageBox.Show("Dicom viewer needs Hardware acceleration, please change Settings to Hardware media backend");
            }

            return null;
        }
   
        private IMedia loadMediaFile(string filename, MediaType type)
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Media file not found '" + filename + "'");
                return null;
            }
            try
            {
                if (MediaBackend == MEDIABACKEND.MEDIAKIT)
                {
                    MediaKit media = new MediaKit(filename, type);
                    addEvents(null, media);
                    addMedia(media);
                    media.Play();
                    media.Pause();

                    return media;
                }
                else if (MediaBackend == MEDIABACKEND.MEDIA)
                {
                    Media media = new Media(filename, type);
                    addEvents(media);
                    addMedia(media);
                    media.Play();
                    media.Pause();
                    media.Position = TimeSpan.Zero;
      
                    return media;


                }
            }
            catch
            {
                return null;
                //ignore the file
            }

            return null;
        }

        private void addEvents(Media media = null, MediaKit mediaKit = null)
        {
            if(media != null)
            {
                media.OnMediaMouseDown += OnMediaMouseDown;
                media.ContextMenu = new ContextMenu();
                media.OnMediaMouseUp += OnMediaMouseUp;
                media.OnMediaMouseMove += OnMediaMouseMove;
                media.OnMediaMouseDown += OnPolygonMedia_MouseDown;
                media.OnMediaRightMouseDown += OnPolygonMedia_RightMouseDown;
                media.OnMediaMouseUp += OnPolygonMedia_MouseUp;
                media.OnMediaMouseMove += OnPolygonMedia_MouseMove;
            }
            else
            {
                mediaKit.OnMediaMouseDown += OnMediaMouseDown;
                mediaKit.OnMediaMouseUp += OnMediaMouseUp;
                mediaKit.OnMediaMouseMove += OnMediaMouseMove;
                mediaKit.OnMediaMouseDown += OnPolygonMedia_MouseDown;
                mediaKit.OnMediaRightMouseDown += OnPolygonMedia_RightMouseDown;
                mediaKit.OnMediaMouseUp += OnPolygonMedia_MouseUp;
                mediaKit.OnMediaMouseMove += OnPolygonMedia_MouseMove;
            }
        }

        private void loadAnnoFile(string filename)
        {
            if (!File.Exists(filename))
            {
                MessageTools.Error("Annotation file not found '" + filename + "'");
                return;
            }

            AnnoScheme.TYPE newAnnoType = AnnoList.getTypeFromFile(filename);
            int videoCount = 0;
            foreach (IMedia media in mediaList)
            {
                if (media.GetMediaType() == MediaType.VIDEO)
                    videoCount++;
            }

            if (videoCount > 1 && (newAnnoType == AnnoScheme.TYPE.POLYGON || newAnnoType == AnnoScheme.TYPE.DISCRETE_POLYGON))
            {
                MessageBox.Show("You can't use a polygon annotation while more than one videos are open!", "Confirm", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            AnnoList annoList = AnnoList.LoadfromFile(filename);            
            addAnnoTierFromList(annoList);

            foreach (AnnoTrigger trigger in annoList.Meta.Trigger)
            {                                
                mediaList.Add(trigger);
            }
            foreach (Pipeline pipeline in annoList.Meta.Pipeline)
            {
                mediaList.Add(pipeline);
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
            loadSignalFile(filename, Defaults.Colors.Foreground, Defaults.Colors.Background, null);
        }

        private void loadSignalFile(string filename, Color signalColor, Color backgroundColor, Dictionary<int, string> dimnames)
        {
            if (!File.Exists(filename) || !File.Exists(filename + "~"))
            {
                MessageTools.Error("Stream file not found '" + filename + "'");
                return;
            }
            Signal signal = null;
        
             signal = Signal.LoadStreamFile(filename, dimnames);
         
            if (signal != null && signal.loaded )
            {                
 
                if(signal.Meta.Count > 0)
                {

                    int width = 500;
                    int height = 500;
                    int mwidth = width;
                    int mheight = height;

                    if (signal.Meta.ContainsKey("ratio"))
                    {
                        string[] split = signal.Meta["ratio"].Split(':');
                        int w = Integer.parseInt(split[0]);
                        int h = Integer.parseInt(split[1]);
                        float scale = 1;
                        if (w > h)
                        {
                            scale = (float)((float)h / (float)w);
                            mwidth = width;
                            mheight = (int)(height * scale);
                        }

                        else if (w < h)
                        {
                            scale = (float)((float)w /(float)h);
                            mwidth = (int)(width * scale);
                            mheight = height;
                        }

                        else
                        {
                            mwidth = width;
                            mheight = height;
                        }

                    }


                    if ((signal.Meta.ContainsKey("name") && signal.Meta["name"] == "face" && signal.Meta.ContainsKey("type") && signal.Meta["type"] == "openface") || signal.Meta["type"] == "openface")
                    {
                        IMedia media = new Face(filename, signal, Face.FaceType.OPENFACE);
                        addMedia(media);
                    }
                    else if ((signal.Meta.ContainsKey("name") && signal.Meta["name"] == "face" && signal.Meta.ContainsKey("type") && signal.Meta["type"] == "openface2") || signal.Meta["type"] == "openface2")
                    {
                        IMedia media = new Face(filename, signal, Face.FaceType.OPENFACE2);
                        addMedia(media);
                    }
                    else if (signal.Meta.ContainsKey("name") && signal.Meta["name"] == "face" && signal.Meta.ContainsKey("type") && (signal.Meta["type"] == "kinect1" || signal.Meta["type"] == "kinect"))
                    {
                        IMedia media = new Face(filename, signal, Face.FaceType.KINECT1);
                        addMedia(media);
                    }
                    else if (signal.Meta.ContainsKey("name") && signal.Meta["name"] == "face" && signal.Meta.ContainsKey("type") && signal.Meta["type"] == "kinect2" || signal.Meta["type"] == "kinect2")
                    {
                        IMedia media = new Face(filename, signal, Face.FaceType.KINECT2);
                        addMedia(media);
                    }
                    else if ((signal.Meta.ContainsKey("name") && signal.Meta["name"] == "face" && signal.Meta.ContainsKey("type") && signal.Meta["type"] == "blazeface") || (signal.Meta.ContainsKey("type") && signal.Meta["type"] == "blazeface"))
                    {   
                        //LEGACY
                        IMedia media = new Face(filename, signal, Face.FaceType.BLAZEFACE, mwidth, mheight);
                        addMedia(media);
                    }

                    else if ((signal.Meta.ContainsKey("name") && signal.Meta["name"] == "face" && signal.Meta.ContainsKey("type") && signal.Meta["type"] == "feature;face") || (signal.Meta.ContainsKey("type") && signal.Meta["type"].Contains("feature;face")))
                    {

                        IMedia media = new Face(filename, signal, Face.FaceType.GENERIC, mwidth, mheight);
                        addMedia(media);
                    }

                    else if (signal.Meta.ContainsKey("name") && signal.Meta["name"] == "skeleton")
                    {
                        IMedia media = new Skeleton(filename, signal, mwidth, mheight);
                        addMedia(media);
                    }
                }
               

                //else
                //{

                    this.control.signalbar.Height = new GridLength(control.signalAndAnnoGrid.ActualHeight /2 - 30);
                    this.control.signalstatusbar.Visibility = Visibility.Visible;
                    signalCursor.signalLoaded = true;
                    annoCursor.signalLoaded = true;
                    addSignalTrack(signal, signalColor, backgroundColor);                  
                //}
            }
        }


        private Signal loadAudioSignalFile(string filename, Color signalColor, Color backgroundColor)
        {

            if (!File.Exists(filename))
            {
                MessageTools.Error("Audio file not found '" + filename + "'");
                return null;
            }


            Signal signal = null;

            try
            {
                signal = Signal.LoadAudioFile(filename);
            }
            catch (Exception e)
            {
                return null;
            }

            if (signal != null && signal.loaded)
            {
                this.control.signalbar.Height = new GridLength(control.signalAndAnnoGrid.ActualHeight / 2 - 30);
                this.control.signalstatusbar.Visibility = Visibility.Visible;
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

                    if (data.Length == 3)
                    {
                        try
                        {
                            double value;
                            if (!double.TryParse(data[2], out value))
                            {
                                type = "semicolon";
                            }
                            else
                            {
                                double start = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                                line = sr.ReadLine();
                                data = line.Split(';');
                                double start2 = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                                samplerate = 1 / (start2 - start);

                                type = "continuous";
                            }
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

                MessageBoxResult mb = MessageBox.Show("CSV matches Legacy annotation format. Load as Annotation?", "No to load as signal", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if(mb == MessageBoxResult.Cancel)
                {
                    return;
                }
                else if(mb == MessageBoxResult.Yes)
                {
                    loadCSVAnnoFile(filename, samplerate, type);
                }
                else
                {
                    loadCSVSignalFile(filename, foreground, background);
                }

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
                this.control.signalbar.Height = new GridLength(control.signalAndAnnoGrid.ActualHeight / 2 - 30);
                this.control.signalstatusbar.Visibility = Visibility.Visible;
                signalCursor.signalLoaded = true;
                annoCursor.signalLoaded = true;
                addSignalTrack(signal, signalColor, backgroundColor);
            }
        }


        public void loadFilesForBounty(DatabaseBounty bounty, string user)
        {
            clearWorkspace();
            DatabaseHandler.ChangeDatabase(bounty.Database);
            DatabaseHandler.ChangeSession(bounty.Session);

            
            foreach (StreamItem path in bounty.streams)
            {

   
                // string directory = Properties.Settings.Default.DatabaseDirectory + "\\" + DatabaseHandler.DatabaseName + "\\" + session + "\\";
                string existingPath = Defaults.FileExistsinPath(path.Name, bounty.Database, bounty.Session);
                loadFile(existingPath + "\\" + bounty.Database + "\\" + bounty.Session + "\\" + path.Name);       
            }

         
 
        }

        public void loadAnnoForBounty(DatabaseBounty bounty, string user)
        {

            var id = DatabaseHandler.GetAnnotationId(bounty.Role, bounty.Scheme, user, bounty.Session);
            AnnoList annoList = DatabaseHandler.LoadAnnoList(id);
            if (annoList != null)
            {
               annoList.Source.Database.BountyID = bounty.OID;
               annoList.Source.Database.HasBounty = true;


                addAnnoTierFromList(annoList);
    }
            else
            {
                addNewAnnotationDatabase(bounty);
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
                    loadFile(path, foreground, background, null);
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
            MessageBoxResult result = MessageBox.Show("The workspace will be cleared. Do you want to continue?", "Question", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            string[] filePath = FileTools.OpenFileDialog("NOVA Project (*.nova)|*.nova", false);
            if (filePath != null && filePath.Length > 0)
            {
                loadProjectFile(filePath[0]);
            }
        }

        #endregion LOAD

        #region SAVE

        private void saveSelectedAnno(bool force = false, bool markAsFinished = false)
        {
            if (AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.AnnoList != null)
            {
                AnnoTierStatic.Selected.AnnoList.Save(databaseSessionStreams, force, false, markAsFinished);
                updateAnnoInfo(AnnoTierStatic.Selected);
            }
        }

        private void reloadBackupSelectedAnno()
        {
            if (AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.AnnoList != null)
            {
                if (AnnoTierStatic.Selected.AnnoList.Source.HasDatabase)
                {
                    ReloadAnnoTierFromDatabase(AnnoTierStatic.Selected, true);
                }
            }
        }

        private void reloadSelectedAnno()
        {
            if (AnnoTierStatic.Selected != null && AnnoTierStatic.Selected.AnnoList != null)
            {
                if (AnnoTierStatic.Selected.AnnoList.Source.HasFile)
                {
                    reloadAnnoTierFromFile(AnnoTierStatic.Selected);
                }
                else if (AnnoTierStatic.Selected.AnnoList.Source.HasDatabase)
                {
                    ReloadAnnoTierFromDatabase(AnnoTierStatic.Selected, false);
                }
            }
        }

        private void exportSelectedAnnoAs()
        {
            if (AnnoTierStatic.Selected.AnnoList != null)
            {
                string directory = AnnoTierStatic.Selected.AnnoList.Source.File.Directory;
                string path = FileTools.SaveFileDialog(AnnoTierStatic.Selected.AnnoList.Source.File.Name, ".annotation", "Annotation(*.annotation)|*.annotation", AnnoTierStatic.Selected.AnnoList.Source.File.Directory);
                if (path != null)
                {
                    AnnoSource source = AnnoTierStatic.Selected.AnnoList.Source;
                    AnnoTierStatic.Selected.AnnoList.Source.File.Path = path;                    
                    AnnoTierStatic.Selected.AnnoList.Save(null, true);
                    AnnoTierStatic.Selected.AnnoList.Source = source;
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

        
        private void BatchConvertElanAnnotations(string parentDiretory)
        {
            DirectoryInfo directory = new DirectoryInfo(parentDiretory);
            DirectoryInfo[] directories = directory.GetDirectories();

            foreach (DirectoryInfo folder in directories)
            {
                var ext = new List<string> { "eaf" };
                var eafFiles = Directory.EnumerateFiles(folder.FullName, "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(Path.GetExtension(s).TrimStart('.').ToLowerInvariant()));
                foreach(var eafFile in eafFiles)
                {
                    List<AnnoList> lists = AnnoList.LoadfromElanFile(eafFile);
                    foreach (AnnoList list in lists)
                    {
                        list.Scheme.Name = convert_uml(list.Scheme.Name);

                        string saveto = folder.FullName + "\\" + list.Meta.Role + "." + list.Scheme.Name + ".annotation";
                        list.Source.StoreToFile = true;
                        list.Source.File.Path = saveto;
                        list.Save();
                    }
                }

            }


        }


        public static string convert_uml(string old)
        {
            old = old.Replace("ä", "ae");
            old = old.Replace("ö", "oe");
            old = old.Replace("ü", "ue");
            old = old.Replace("Ä", "Ae");
            old = old.Replace("Ö", "Oe");
            old = old.Replace("Ü", "Ue");
            old = old.Replace("ß", "ss");
            return (old);
        }


        private void ImportAnnoFromSubtitles(string filename, bool wordlevel)
        {
            int annoListnum = annoLists.Count;
            bool addemptytiers = false;
            List<AnnoList> lists = AnnoList.LoadfromSRTFile(filename, wordlevel);

            if (lists.Exists(n => n.Count == 0))
            {
                MessageBoxResult mb = MessageBox.Show("At least one tier is empty, should empty tiers be excluded?", "Attention", MessageBoxButton.YesNo);
                if (mb == MessageBoxResult.No)
                {
                    addemptytiers = true;
                }
            }

            double maxdur = 0;

            if (lists != null)
            {
                foreach (AnnoList list in lists)
                {
                    if (list.Count > 0) maxdur = list[list.Count - 1].Stop;

                    if (list.Count > 0 || addemptytiers)
                    {
                        annoLists.Add(list);
                        addAnnoTier(list);
                    }

                }
            }
            updateTimeRange(maxdur);
            if (annoListnum == 0 && maxdur > Properties.Settings.Default.DefaultZoomInSeconds && Properties.Settings.Default.DefaultZoomInSeconds != 0)
            {
                fixTimeRange(Properties.Settings.Default.DefaultZoomInSeconds);
            }
        }

        

        private void ImportAnnoFromElan(string filename)
        {
            int annoListnum = annoLists.Count; 
            bool addemptytiers = false;
            List<AnnoList> lists = AnnoList.LoadfromElanFile(filename);

            if (lists.Exists(n => n.Count == 0))
            {
                MessageBoxResult mb = MessageBox.Show("At least one tier is empty, should empty tiers be excluded?", "Attention", MessageBoxButton.YesNo);
                if (mb == MessageBoxResult.No)
                {
                    addemptytiers = true;
                }
            }

            double maxdur = 0;

            if (lists != null)
            {
                foreach (AnnoList list in lists)
                {
                    if (list.Count > 0) maxdur = list[list.Count - 1].Stop;

                    if (list.Count > 0 || addemptytiers)
                    {
                        annoLists.Add(list);
                        addAnnoTier(list);
                    }
                  
                }
            }
            updateTimeRange(maxdur);
            if (annoListnum == 0 && maxdur > Properties.Settings.Default.DefaultZoomInSeconds && Properties.Settings.Default.DefaultZoomInSeconds != 0)
            {
                fixTimeRange(Properties.Settings.Default.DefaultZoomInSeconds);
            }
        }

        private void ImportAnnoFromAnvil(string filename)
        {
            int annoListnum = annoLists.Count;
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
            if (annoListnum == 0 && maxdur > Properties.Settings.Default.DefaultZoomInSeconds && Properties.Settings.Default.DefaultZoomInSeconds != 0)
            {
                fixTimeRange(Properties.Settings.Default.DefaultZoomInSeconds);
            }
        }


        private void batchConvertNoldus (string directory)
        {
            string[] dirs = Directory.GetDirectories(directory, "*", SearchOption.AllDirectories);
                foreach(string dir in dirs)
                {
                    string[] files = Directory.GetFiles(dir, "*.odf", SearchOption.TopDirectoryOnly);
                    foreach(string filename in files)
                    {
                    AnnoList[] lists = AnnoList.LoadfromNoldusFile(filename);
                    if (lists != null)
                    {
                        foreach (AnnoList list in lists)
                        {
                            string path = Path.GetFullPath(Path.GetDirectoryName(list.Source.File.Path) + "\\" + list.Scheme.Name) + ".annotation";
                            list.SaveToFile(path);
                            //list.Save();
                        }
                    }
                }
                }

           
        }

        private void ImportAnnoFromNoldus(string filename)
        {
            int annoListnum = annoLists.Count;
            AnnoList[] lists = AnnoList.LoadfromNoldusFile(filename);
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
            if (annoListnum == 0 && maxdur > Properties.Settings.Default.DefaultZoomInSeconds && Properties.Settings.Default.DefaultZoomInSeconds != 0)
            {
                fixTimeRange(Properties.Settings.Default.DefaultZoomInSeconds);
            }
        }


        

        private void ImportAnnoFromSSIEvents(string filename)
        {
            int annoListnum = annoLists.Count;
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
            if (annoListnum == 0 && maxdur > Properties.Settings.Default.DefaultZoomInSeconds && Properties.Settings.Default.DefaultZoomInSeconds != 0)
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
                    double val = ali.Score;

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

                if (annoTier.AnnoList.HasChanged || annoTier.AnnoList.Source.File.Path == "")
                {
                    MessageBoxResult m = MessageBoxResult.None;
                    m = MessageBox.Show("You need to save continous annotations on tier " + annoTier.AnnoList.Scheme.Name + " first", "Confirm", MessageBoxButton.OK, MessageBoxImage.Exclamation);

                    if (m == MessageBoxResult.OK)
                    {
                        exportSelectedAnnoAs();
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
                    swdata.WriteLine(i.Score);
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

        private void annoSaveAsFinished_Click(object sender, RoutedEventArgs e)
        {
            saveSelectedAnno(false, true);
        }

        private void annoReload_Click(object sender, RoutedEventArgs e)
        {
            reloadSelectedAnno();
        }

        private void annoReloadBackup_Click(object sender, RoutedEventArgs e)
        {
            reloadBackupSelectedAnno();
        }

        private void annoExport_Click(object sender, RoutedEventArgs e)
        {
            exportSelectedAnnoAs();
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
            foreach (AnnoTier tier in annoTiers)
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
            if(AnnoTierStatic.Selected != null)
            {
                if(AnnoTierStatic.Selected.AnnoList.Scheme.Type  == AnnoScheme.TYPE.CONTINUOUS)
                {
                    ExportAnnoContinuousToDiscrete();
                }

                else if (AnnoTierStatic.Selected.AnnoList.Scheme.Type == AnnoScheme.TYPE.FREE)
                {

                   AnnoList newlist = AnnoList.ConvertFreetoDiscreteAnnotation(AnnoTierStatic.Selected.AnnoList);
                   AnnoTier.Unselect();
                   addAnnoTierFromList(newlist);
                }
               
               

            }
          
        }

        private void convertSignalToAnnoContinuous_Click(object sender, RoutedEventArgs e)
        {
            ExportSignalToContinuous();
        }

        private void convertAnnoToSignal_Click(object sender, RoutedEventArgs e)
        {
            ExportAnnoToSignal();
        }

        private void exportAnnoToCSV_Click(object sender, RoutedEventArgs e)
        {
            if (AnnoTierStatic.Selected != null)
            {
                showShadowBox("Export annotation to CSV'");
                AnnoTierStatic.Selected.AnnoList.ExportToCSV();
                hideShadowBox();
            }
        }

        private void exportAnnoToXPS_Click(object sender, RoutedEventArgs e)
        {
            if (AnnoTierStatic.Selected != null)
            {
                string filepath = FileTools.SaveFileDialog(AnnoTierStatic.Selected.AnnoList.Scheme.Name, "xps", "XPS (*.xps)|*.xps", AnnoTierStatic.Selected.AnnoList.Source.File.Directory);
                if (filepath != null)
                {
                    var uri = new Uri(filepath);

                    if (AnnoTierStatic.Selected != null)
                    {
                        AnnoTierStatic.Selected.ExportToXPS(uri, AnnoTierStatic.Selected);
                        AnnoTierStatic.Selected.TimeRangeChanged(Time);
                    }
                }
            }
        }

        private void exportAnnoToPNG_Click(object sender, RoutedEventArgs e)
        {
            if (AnnoTierStatic.Selected != null)
            {
                string filepath = FileTools.SaveFileDialog(AnnoTierStatic.Selected.AnnoList.Scheme.Name, "xps", "PNG (*.png)|*.png", AnnoTierStatic.Selected.AnnoList.Source.File.Directory);
                if (filepath != null)
                {
                    var uri = new Uri(filepath);

                    if (AnnoTierStatic.Selected != null)
                    {
                        AnnoTierStatic.Selected.ExportToPNG(uri, AnnoTierStatic.Selected);
                        AnnoTierStatic.Selected.TimeRangeChanged(Time);
                    }
                }
            }
        }

        private void exportSignalToCSV_Click(object sender, RoutedEventArgs e)
        {
            if (SignalTrackStatic.Selected != null)
            {
                string filepath = FileTools.SaveFileDialog(SignalTrackStatic.Selected.Name, "csv", "CSV (*.csv)|*.csv", SignalTrackStatic.Selected.Signal.Directory);
                if (filepath != null)
                {
                    var uri = new Uri(filepath);

                    if (SignalTrackStatic.Selected != null)
                    {
                        showShadowBox("Export signal to CSV '" + uri.LocalPath + "'");
                        SignalTrackStatic.Selected.ExportToCSV(uri, SignalTrackStatic.Selected.Signal);
                        hideShadowBox();
                    }
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

                    if (SignalTrackStatic.Selected != null)
                    {
                        SignalTrackStatic.Selected.ExportToXPS(uri, SignalTrackStatic.Selected);
                    }
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
                        SignalTrackStatic.Selected.ExportToPNG(uri, SignalTrackStatic.Selected);
                    }
                }
            }
        }



        #endregion EVENTHANDLER
    }
}