using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace ssi
{
    public enum AnnoType
    {
        DISCRETE,
        FREE,
        CONTINUOUS,
    }

    public class AnnoList : MyEventList
    {
        private enum Type
        {
            EMPTY,
            MAP,
            TUPLE,
            STRING,
        }

        private bool loaded = false;
        private bool fromDB = false;
        private String name = null;
        private String filename = null;
        private String filepath = null;
        private String sampleannopath = null;
        private double lowborder = 0.0;
        private double highborder = 1.0;
        private double confidence = 1.0;
        private string role = null;
        private string subject = null;
        private double sr = 1.0;
        private AnnotationScheme scheme;
        public bool usesAnnoScheme = false;
        private string annotator = null;
        private string annotatorfn = null;
        private string ftype = "ASCII";
        private string defaultcolor = "#FF000000";

        private AnnoType _Type = AnnoType.DISCRETE;

        public bool Loaded
        {
            get { return loaded; }
            set { loaded = value; }
        }

        public bool FromDB
        {
            get { return fromDB; }
            set { fromDB = value; }
        }

        public string Role
        {
            get { return role; }
            set { role = value; }
        }

        public string Ftype
        {
            get { return ftype; }
            set { ftype = value; }
        }

        public string Subject
        {
            get { return subject; }
            set { subject = value; }
        }

        public double SR
        {
            get { return sr; }
            set { sr = value; }
        }

        public AnnoType AnnotationType
        {
            get { return _Type; }
            set { _Type = value; }
        }

        public double Lowborder
        {
            get { return lowborder; }
            set { lowborder = value; }
        }

        public double Highborder
        {
            get { return highborder; }
            set { highborder = value; }
        }

        public string Filename
        {
            get { return filename; }
        }

        public string Annotator
        {
            get { return annotator; }
            set { annotator = value; }
        }

        public string AnnotatorFullName
        {
            get { return annotatorfn; }
            set { annotatorfn = value; }
        }

        public string Filepath
        {
            get { return filepath; }
            set { filepath = value; }
        }

        public double Confidence
        {
            get { return confidence; }
            set { confidence = value; }
        }

        public string SampleAnnoPath
        {
            get { return sampleannopath; }
            set { sampleannopath = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public AnnotationScheme AnnotationScheme
        {
            get { return scheme; }
            set { scheme = value; }
        }

        public AnnoList()
            : base()
        {
        }

        public AnnoList(String filepath)
        {
            this.filepath = filepath;
            this.SampleAnnoPath = sampleannopath;
            string[] tmp = filepath.Split('\\');
            this.filename = tmp[tmp.Length - 1];
            this.name = this.filename.Split('.')[0];
        }

        public static AnnoList LoadFromEventsFile(String filepath)
        {
            AnnoList list = new AnnoList();
            try
            {
                XDocument doc = XDocument.Load(filepath);
                var events = doc.Descendants("event");

                foreach (var e in events)
                {
                    string label = null;
                    string meta = "";
                    double start = -1;
                    double duration = -1;
                    Type type = Type.EMPTY;

                    var sender_attr = e.Attribute("sender");
                    var event_attr = e.Attribute("event");
                    label = (event_attr == null ? "" : event_attr.Value);
                            //+ "@"
                            //+ (sender_attr == null ? "" : sender_attr.Value);
                    var from_attr = e.Attribute("from");
                    if (from_attr != null)
                    {
                        start = double.Parse(from_attr.Value) / 1000.0;
                    }
                    var dur_attr = e.Attribute("dur");
                    if (dur_attr != null)
                    {
                        duration = double.Parse(dur_attr.Value) / 1000.0;
                    }
                    var type_attr = e.Attribute("type");
                    if (type_attr != null)
                    {
                        switch (type_attr.Value)
                        {
                            case "MAP":
                                type = Type.MAP;
                                break;

                            case "TUPLE":
                                type = Type.TUPLE;
                                break;

                            case "STRING":
                                type = Type.STRING;
                                break;

                            //depricated, matched to tuple
                            case "FLOATS":
                                type = Type.TUPLE;
                                break;

                            //depricated, matched to map
                            case "NTUPLE":
                                type = Type.MAP;
                                break;
                        }
                    }

                    switch (type)
                    {
                        case Type.MAP:
                            var tuples = e.Descendants("tuple");
                            foreach (var tuple in tuples)
                            {
                                var string_attr = tuple.Attribute("string");
                                var value_attr = tuple.Attribute("value");
                                meta = meta + ((string_attr == null ? "" : string_attr.Value)
                                    + "="
                                    + (value_attr == null ? "" : value_attr.Value)) + ";";
                            }
                            break;


                        case Type.STRING:
                            if(e.Value != "") label = e.Value;

                            break;

                        case Type.TUPLE:
                            meta = e.Value == null ? "" : e.Value;
                            break;
                    }

                    var state_attr = e.Attribute("state");
                    if (state_attr.Value.ToString().ToUpper() == "COMPLETED")
                    {
                        list.AddSorted(new AnnoListItem(start, duration, label, meta));
                    }
                }
            }
            catch (Exception ex)
            {
                ViewTools.ShowErrorMessage(ex.ToString());
            }

            return list;
        }

        public static AnnoList LoadfromFile(String filepath, double samplerate = 1, string type = "legacy", string filter = null)
        {
            AnnoList list = new AnnoList(filepath);
            list.Lowborder = 0.0;
            list.Highborder = 1.0;
            list.sr = samplerate;
            list.Filepath = Path.GetDirectoryName(filepath);
            list.AnnotationScheme = new AnnotationScheme();
            try
            {
                StreamReader sr = new StreamReader(filepath, System.Text.Encoding.Default);
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (type == "semicolon")
                    {
                        list.AnnotationType = AnnoType.FREE;
                        string[] data = line.Split(';');
                        double start = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                        double duration = Convert.ToDouble(data[1]) - Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                        string label = "";
                        string tier = "";
                        string meta = "";
                        double confidence = 1.0;
                        if (data.Length > 2)
                        {
                            label = data[2];
                        }
                        if (data.Length > 3)
                        {
                            if (data[3].Contains("#"))
                            {
                                tier = data[3];
                                tier = tier.Remove(0, 1);
                            }
                            else
                            {
                                bool isconfidence = double.TryParse(data[3], out confidence);
                            }
                        }
                        if (data.Length > 4)
                        {
                            for (int i = 4; i < data.Length; i++)
                            {
                                meta += data[i] + ";";
                            }
                        }

                        AnnoListItem e = new AnnoListItem(start, duration, label, meta, "#000000", confidence);

                        if (filter == null || tier == filter)
                            list.AddSorted(e);
                    }
                    else if (type == "continuous")
                    {
                        list.AnnotationType = AnnoType.CONTINUOUS;
                        list.SR = (1000.0 / (samplerate * 1000.0));
                        string[] data = line.Split(';');
                        double start = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                        string label = "";
                        string tier = "";
                        double confidence = 1.0;

                        if (data.Length > 1)
                        {
                            label = data[1];
                        }
                        if (data.Length > 2)
                        {
                            if (data[2].Contains("#"))
                            {
                                tier = data[2];
                                tier = tier.Remove(0, 1);
                            }
                            else
                            {
                                bool isconfidence = double.TryParse(data[3], out confidence);
                            }
                        }
                        if (data.Length > 3)
                        {
                            list.Lowborder = double.Parse(data[3]);

                            if (data.Length > 4)
                            {
                                list.Highborder = double.Parse(data[4]);
                            }
                            AnnoListItem e = new AnnoListItem(start, samplerate, label, "Range: " + list.Lowborder + "-" + list.Highborder, tier);
                            list.AddSorted(e);
                        }
                        else
                        {
                            AnnoListItem e = new AnnoListItem(start, samplerate, label, "", tier);
                            if (filter == null || tier == filter)
                                list.AddSorted(e);
                        }
                    }
                    else if (type == "legacy")
                    {
                        list.AnnotationType = AnnoType.FREE;
                        string[] data;
                        data = line.Split(' ');
                        if (data.Length < 2) data = line.Split('\t');
                        if (data.Length < 2) data = line.Split(';');

                        double start = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                        double duration = Convert.ToDouble(data[1], CultureInfo.InvariantCulture) - Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                        string label = "";
                        string tier = "";
                        if (data.Length > 2)
                        {
                            label = data[2];
                        }

                        if (data.Length > 3)
                        {
                            tier = data[3];
                        }

                        for (int i = 4; i < data.Length; i++)
                        {
                            label += " " + data[i];
                        }
                        AnnoListItem e = new AnnoListItem(start, duration, label, "", tier);
                        if (filter == null || tier == filter)
                            list.AddSorted(e);
                    }
                }
                sr.Close(); ;
                list.loaded = true;
                list.HasChanged = false;
            }
            catch (Exception e)
            {
                MessageBox.Show("An exception occured while reading annotation from '" + filepath + "'");
            }

            return list;
        }

        public static AnnoList LoadfromFileNew(String filepath)
        {
            AnnoList list = new AnnoList(filepath);
            list.Lowborder = 0.0;
            list.Highborder = 1.0;
            list.sr = 1;
            list.Filepath = filepath;

            list.AnnotationScheme = new AnnotationScheme();
            list.AnnotationScheme.LabelsAndColors = new List<LabelColorPair>();
            // list.AnnotationScheme.mincolor = "#FFFFFF";

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filepath);

                XmlNode annotation = doc.SelectSingleNode("annotation");

                XmlNode info = annotation.SelectSingleNode("info");
                list.Ftype = info.Attributes["ftype"].Value;
                int size = Int32.Parse(info.Attributes["size"].Value);

                XmlNode meta = annotation.SelectSingleNode("meta");
                if (meta != null)
                {
                    if (meta.Attributes["role"] != null) list.Role = meta.Attributes["role"].Value;
                    if (meta.Attributes["annotator"] != null) list.Annotator = meta.Attributes["annotator"].Value;
                }

                XmlNode scheme = annotation.SelectSingleNode("scheme");
                if (scheme.Attributes["name"] != null) list.Name = scheme.Attributes["name"].Value;
                string type = "FREE";
                if (scheme.Attributes["type"] != null) type = scheme.Attributes["type"].Value;
                if (scheme.Attributes["color"] != null) list.AnnotationScheme.mincolor = scheme.Attributes["color"].Value;
                else if (scheme.Attributes["mincolor"] != null) list.AnnotationScheme.mincolor = scheme.Attributes["mincolor"].Value;
                if (scheme.Attributes["maxcolor"] != null) list.AnnotationScheme.maxcolor = scheme.Attributes["maxcolor"].Value;

                string role = "";
                if (list.Role != null) role = "#" + list.Role + " "; ;
                string annotator = "";
                if (list.Annotator != null) annotator = " #" + list.Annotator;

                list.Name = role + "#" + scheme.Attributes["name"].Value + annotator;


                if (type == "DISCRETE") list.AnnotationType = AnnoType.DISCRETE;
                else if (type == "CONTINUOUS") list.AnnotationType = AnnoType.CONTINUOUS;
                else list.AnnotationType = AnnoType.FREE;

                Dictionary<string, string> LabelIds = new Dictionary<string, string>();

                if (list.AnnotationType == AnnoType.DISCRETE)
                {
                    list.usesAnnoScheme = true;
                    foreach (XmlNode item in scheme)
                    {
                        LabelIds.Add(item.Attributes["id"].Value, item.Attributes["name"].Value);

                        string color = "#000000";
                        if (item.Attributes["color"] != null) color = item.Attributes["color"].Value;
                        LabelColorPair lcp = new LabelColorPair(item.Attributes["name"].Value, color);
                        list.AnnotationScheme.LabelsAndColors.Add(lcp);
                    }

                    LabelColorPair garbage = new LabelColorPair("GARBAGE", "#FF000000");
                    list.AnnotationScheme.LabelsAndColors.Add(garbage);
                }
                else if (list.AnnotationType == AnnoType.FREE)
                {
                    list.usesAnnoScheme = false;
                }
                else if (list.AnnotationType == AnnoType.CONTINUOUS)
                {
                    list.usesAnnoScheme = true;
                    list.sr = Double.Parse(scheme.Attributes["sr"].Value);
                    list.Lowborder = Double.Parse(scheme.Attributes["min"].Value);
                    list.Highborder = Double.Parse(scheme.Attributes["max"].Value);
                }

                if (File.Exists(filepath + "~"))

                {
                    if (list.Ftype == "ASCII")
                    {
                        StreamReader sr = new StreamReader(filepath + "~", System.Text.Encoding.Default);
                        string line = null;
                        double start = 0.0;

                        while ((line = sr.ReadLine()) != null)
                        {
                            string[] data = line.Split(';');
                            if (list.AnnotationType == AnnoType.CONTINUOUS)
                            {
                                string value = data[0];
                                double confidence = Convert.ToDouble(data[1], CultureInfo.InvariantCulture);
                                AnnoListItem e = new AnnoListItem(start, (1000.0 / list.SR) / 1000.0, value, "", "#000000", confidence);
                                list.Add(e);
                                start = start + (1000.0 / list.SR) / 1000.0;
                            }
                            else if (list.AnnotationType == AnnoType.DISCRETE)
                            {
                                start = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                                double stop = Convert.ToDouble(data[1], CultureInfo.InvariantCulture);
                                double dur = stop - start;
                                string label = "";
                                if (int.Parse(data[2]) < 0) label = "GARBAGE";
                                else LabelIds.TryGetValue(data[2], out label);
                                string color = "#000000";

                                if (list.AnnotationScheme.LabelsAndColors.Find(x => x.Label == label) != null)
                                {
                                    color = list.AnnotationScheme.LabelsAndColors.Find(x => x.Label == label).Color;
                                }

                                double confidence = Convert.ToDouble(data[3], CultureInfo.InvariantCulture);
                                AnnoListItem e = new AnnoListItem(start, dur, label, "", color, confidence);
                                list.AddSorted(e);
                            }
                            else if (list.AnnotationType == AnnoType.FREE)
                            {
                                start = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                                double stop = Convert.ToDouble(data[1], CultureInfo.InvariantCulture);
                                double dur = stop - start;
                                string label = data[2];
                                double confidence = Convert.ToDouble(data[3], CultureInfo.InvariantCulture);
                                string color = "#FF000000";
                                if (data.Length > 4)
                                {
                                 string[] metapairs = data[4].Split('=');
                                    for(int i=0; i< metapairs.Length;i++)
                                    {
                                        if(metapairs[i].Contains("color"))
                                        {
                                            color = metapairs[i + 1];
                                            break;
                                        }
                                    }  
                                }
                                AnnoListItem e = new AnnoListItem(start, dur, label, "", color, confidence);
                                list.AddSorted(e);
                            }
                        }
                        sr.Close();
                    }
                    else if (list.Ftype == "BINARY")
                    {
                        BinaryReader binaryReader = new BinaryReader(File.Open(filepath + "~", FileMode.Open));
                        long length = (binaryReader.BaseStream.Length);

                        double start = 0.0;
                        while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                        {
                            if (list.AnnotationType == AnnoType.CONTINUOUS)
                            {
                                string value = binaryReader.ReadSingle().ToString();
                                double confidence = (double)binaryReader.ReadSingle();
                                AnnoListItem e = new AnnoListItem(start, (1000.0 / list.SR) / 1000.0, value, "", "#000000", confidence);
                                list.Add(e);
                                start = start + (1000.0 / list.SR) / 1000.0;
                            }
                            else if (list.AnnotationType == AnnoType.DISCRETE)
                            {
                                start = binaryReader.ReadDouble();
                                double stop = binaryReader.ReadDouble();
                                double dur = stop - start;
                                string label = "";
                                int index = binaryReader.ReadInt32();
                                if (index < 0) label = "GARBAGE";
                                else LabelIds.TryGetValue(index.ToString(), out label);
                                string color = "#000000";

                                if (list.AnnotationScheme.LabelsAndColors.Find(x => x.Label == label) != null)
                                {
                                    color = list.AnnotationScheme.LabelsAndColors.Find(x => x.Label == label).Color;
                                }
                                double confidence = Math.Round(binaryReader.ReadSingle(), 3, MidpointRounding.AwayFromZero);
                                AnnoListItem e = new AnnoListItem(start, dur, label, "", color, confidence);
                                list.AddSorted(e);
                            }
                            else if (list.AnnotationType == AnnoType.FREE)
                            {
                                start = binaryReader.ReadDouble();
                                double stop = binaryReader.ReadDouble();
                                double dur = stop - start;
                                int stringlength = (int)binaryReader.ReadUInt32();
                                byte[] labelasbytes = (binaryReader.ReadBytes(stringlength));
                                string label = System.Text.Encoding.Default.GetString(labelasbytes);
                                string color = "#000000";

                                /*
                                int stringlength2 = (int)binaryReader.ReadUInt32();
                                byte[] colorasbytes = (binaryReader.ReadBytes(stringlength2));
                                string color = System.Text.Encoding.Default.GetString(colorasbytes);
                                */


                                double confidence = Math.Round(binaryReader.ReadSingle(), 3, MidpointRounding.AwayFromZero);
                                AnnoListItem e = new AnnoListItem(start, dur, label, "", color, confidence);
                                list.AddSorted(e);
                            }
                        }

                        binaryReader.Close();
                    }
                }
                else
                {
                    MessageBox.Show("Annotation data was not found, load scheme only from '" + filepath + "'");
                }

                list.loaded = true;
                list.HasChanged = false;
            }
            catch (Exception e)
            {
                MessageBox.Show("An exception occured while reading annotation from '" + filepath + "'");
            }

            return list;
        }

        public static AnnoList[] LoadfromElanFile(String filepath)
        {
            AnnoList[] list = new AnnoList[1];

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filepath);
                XmlNode time_order = doc.DocumentElement.ChildNodes[1];
                List<KeyValuePair<string, string>> time_order_list = new List<KeyValuePair<string, string>>();
                foreach (XmlNode node in time_order.ChildNodes)
                {
                    time_order_list.Add(new KeyValuePair<string, string>(node.Attributes[0].Value.ToString(), node.Attributes[1].Value.ToString()));
                }

                int numberoftracks = 0;
                foreach (XmlNode tier in doc.SelectNodes("//TIER"))
                {
                    numberoftracks++;
                }

                list = new AnnoList[numberoftracks];

                int i = 0;
                foreach (XmlNode tier in doc.SelectNodes("//TIER"))
                {
                    string tierid;
                    if (tier.Attributes.Count == 2) tierid = tier.Attributes[1].Value.ToString();
                    else tierid = tier.Attributes[2].Value.ToString();
                    list[i] = new AnnoList(filepath);

                    foreach (XmlNode annotation in tier.ChildNodes)
                    {
                        string label = null;
                        string starttmp = "";
                        string endtmp = "";
                        double start = -1;
                        double end = -1;
                        double duration = -1;
                        XmlNode alignable_annotation = annotation.FirstChild;

                        starttmp = (from kvp in time_order_list where kvp.Key == alignable_annotation.Attributes.GetNamedItem("TIME_SLOT_REF1").Value.ToString() select kvp.Value).ToList()[0];
                        start = double.Parse(starttmp, CultureInfo.InvariantCulture) / 1000;
                        endtmp = (from kvp in time_order_list where kvp.Key == alignable_annotation.Attributes.GetNamedItem("TIME_SLOT_REF2").Value.ToString() select kvp.Value).ToList()[0];
                        end = double.Parse(endtmp, CultureInfo.InvariantCulture) / 1000;
                        label = alignable_annotation.FirstChild.InnerText;
                        duration = end - start;
                        list[i].AddSorted(new AnnoListItem(start, duration, label, "", tierid));
                        list[i].Name = tierid;
                        //The tier is used as metainformation as well. Might be changed if thats relevant in the future
                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                ViewTools.ShowErrorMessage(ex.ToString());
            }

            return list;
        }

        public static AnnoList[] LoadfromAnvilFile(String filepath)
        {
            XmlDocument doc = new XmlDocument();
            doc.Load(filepath);
            XmlNode annotracks = doc.DocumentElement.ChildNodes[1];
            int numberoftracks = annotracks.ChildNodes.Count;

            int index = 0;
            AnnoList[] list = new AnnoList[numberoftracks];
            foreach (XmlNode annotrack in annotracks)
            {
                list[index] = new AnnoList(filepath);
                foreach (XmlNode annotation in annotrack.ChildNodes)
                {
                    try
                    {
                        string tierid = annotrack.Attributes[0].Value.ToString() + ":" + annotrack.Attributes[1].Value.ToString();
                        string starttmp = annotation.Attributes[1].Value.ToString();
                        string stoptmp = annotation.Attributes[2].Value.ToString();
                        string label = annotation.ChildNodes[0].InnerText;
                        string meta = annotation.ChildNodes[0].Attributes[0].Value.ToString();
                        double start = double.Parse(starttmp, CultureInfo.InvariantCulture);
                        double end = double.Parse(stoptmp, CultureInfo.InvariantCulture);
                        double duration = end - start;

                        for (int i = 1; i < annotation.ChildNodes.Count; i++)
                        {
                            label = label + ";" + annotation.ChildNodes[i].InnerText;
                            meta = meta + ";" + annotation.ChildNodes[i].Attributes[0].Value.ToString();
                        }
                        //  string tierid = annotrack.InnerText;
                        list[index].AddSorted(new AnnoListItem(start, duration, label, meta, tierid));
                        list[index].Name = tierid;
                    }
                    catch
                    {
                        Console.WriteLine("Error parsing line in Anvil file");
                    }
                }
                index++;
            }

            return list;
        }

        public AnnoList saveToFile()
        {
            if (filepath == null || filepath.Split('.')[1] != "csv")
            {
                filepath = ViewTools.SaveFileDialog(this.Name, ".csv", Path.GetDirectoryName(this.Filepath), 2);
            }
            this.Filepath = filepath;
            return saveToFile(filepath);
        }

        public AnnoList saveToFileNew()
        {
            if (filepath == null || filepath.Split('.')[1] == "eaf" || filepath.Split('.')[1] == "anvil" || filepath.Split('.')[1] == "anno" || filepath.Split('.')[1] == "csv")
            {
                if(this.AnnotatorFullName != null )
                {
                    if (this.Annotator == null) this.Annotator = this.AnnotatorFullName;
                    if(this.annotator != null)
                    filepath = ViewTools.SaveFileDialog(this.AnnotationScheme.name + "." + this.Role + "." + this.Annotator, ".annotation", Path.GetDirectoryName(this.Filepath));
                }
                else filepath = ViewTools.SaveFileDialog(this.Name, ".annotation", Path.GetDirectoryName(this.Filepath));

                if (filepath != null)
                {
                    filename = filepath.Split('.')[0];
                }
            }

            return saveToFileNew(filepath);
        }

        public AnnoList saveToFileNew(String _filename, String _delimiter = ";")
        {
            Dictionary<string, string> LabelIds = new Dictionary<string, string>();

            try
            {
                StreamWriter sw = new StreamWriter(_filename, false, System.Text.Encoding.Default);
                sw.WriteLine("<?xml version=\"1.0\" ?>");
                sw.WriteLine("<annotation ssi-v=\"3\">");

                sw.WriteLine("    <info ftype=\"" + this.Ftype + "\" size=\"" + this.Count + "\" />");
                sw.WriteLine("    <meta annotator=\"" + Properties.Settings.Default.Annotator + "\"/>");
                if (this.AnnotationType == AnnoType.CONTINUOUS)
                {
                    sw.WriteLine("    <scheme name=\"" + this.Name + "\" type=\"CONTINUOUS\" sr=\"" + this.SR + "\" min=\"" + this.Lowborder + "\" max=\"" + this.Highborder + "\" mincolor=\"" + this.AnnotationScheme.mincolor + "\" maxcolor=\"" + this.AnnotationScheme.maxcolor + "\" />");
                }
                else if (this.AnnotationType == AnnoType.FREE)
                {
                    sw.WriteLine("    <scheme name=\"" + this.Name + "\" type=\"FREE\" color=\"" + this.AnnotationScheme.mincolor + "\"/>");
                }
                else if (this.AnnotationType == AnnoType.DISCRETE)
                {
                    sw.WriteLine("    <scheme name=\"" + this.Name + "\" type=\"DISCRETE\"  color=\"" + this.AnnotationScheme.mincolor + "\">");
                    int index = 0;

                    foreach (LabelColorPair lp in this.AnnotationScheme.LabelsAndColors)
                    {
                        if (lp.Label != "GARBAGE")
                        {
                            sw.WriteLine("        <item name=\"" + lp.Label + "\" id=\"" + index + "\" color=\"" + lp.Color + "\" />");
                            LabelIds.Add(lp.Label, index.ToString());
                            index++;
                        }
                    }
                    sw.WriteLine("    </scheme>");
                }

                sw.WriteLine("</annotation>");
                sw.Close();
            }
            catch
            {
                return null;
            }

            try
            {
                if (this.Ftype == "ASCII")
                {
                    StreamWriter sw = new StreamWriter(_filename + "~", false, System.Text.Encoding.Default);
                    if (this.AnnotationType == AnnoType.CONTINUOUS)
                    {
                        foreach (AnnoListItem e in this)
                        {
                            sw.WriteLine(e.Label + _delimiter + e.Confidence.ToString("n2"));
                        }
                    }
                    else if (this.AnnotationType == AnnoType.FREE)
                    {
                        foreach (AnnoListItem e in this)
                        {
                            if(e.Bg == defaultcolor)
                            sw.WriteLine(e.Start.ToString("n2") + _delimiter + e.Stop.ToString("n2") + _delimiter + e.Label + _delimiter + e.Confidence.ToString("n2"));
                            else
                            sw.WriteLine(e.Start.ToString("n2") + _delimiter + e.Stop.ToString("n2") + _delimiter + e.Label + _delimiter + e.Confidence.ToString("n2") + _delimiter + "color=" + e.Bg.ToString());
                        }
                    }
                    else if (this.AnnotationType == AnnoType.DISCRETE)
                    {
                        string index = "";

                        foreach (AnnoListItem e in this)
                        {
                            if (e.Label != "GARBAGE")
                            {
                                LabelIds.TryGetValue(e.Label, out index);
                                sw.WriteLine(e.Start.ToString("n2") + _delimiter + e.Stop.ToString("n2") + _delimiter + index + _delimiter + e.Confidence.ToString("n2"));
                            }
                            else
                            {
                                sw.WriteLine(e.Start.ToString("n2") + _delimiter + e.Stop.ToString("n2") + _delimiter + -1 + _delimiter + e.Confidence.ToString("n2"));
                            }
                        }
                    }

                    sw.Close();
                }
                else
                {
                    BinaryWriter bw = new BinaryWriter(new FileStream(_filename + "~", FileMode.Create));
                    if (this.AnnotationType == AnnoType.CONTINUOUS)
                    {
                        foreach (AnnoListItem e in this)
                        {
                            bw.Write(float.Parse(e.Label));
                            bw.Write((float)e.Confidence);
                        }
                    }
                    else if (this.AnnotationType == AnnoType.FREE)
                    {
                        foreach (AnnoListItem e in this)
                        {
                            bw.Write(e.Start);
                            bw.Write(e.Stop);
                            bw.Write((uint)e.Label.Length);
                            byte[] label = System.Text.Encoding.UTF8.GetBytes(e.Label);
                            bw.Write(label);
                            bw.Write((float)e.Confidence);
                            //bw.Write((uint)e.Bg.Length);
                            //byte[] color = System.Text.Encoding.UTF8.GetBytes(e.Bg);
                            //bw.Write(color);
                        }
                    }
                    else if (this.AnnotationType == AnnoType.DISCRETE)
                    {
                        string index = "";
                        foreach (AnnoListItem e in this)
                        {
                            uint ind = unchecked((uint)-1);
                            if (e.Label != "GARBAGE")
                            {
                                LabelIds.TryGetValue(e.Label, out index);
                                ind = uint.Parse(index);
                            }
                            bw.Write(e.Start);
                            bw.Write(e.Stop);
                            bw.Write(ind);
                            bw.Write((float)e.Confidence);
                        }
                    }
                    bw.Close();
                }

                HasChanged = false;
                AnnoList newAnno = new AnnoList(_filename);
                newAnno.Filepath = _filename;
                return newAnno;
            }
            catch
            {
                return null;
            }
        }

        public AnnoList saveToFile(String _filename, String _delimiter = ";")
        {
            try
            {
                StreamWriter sw = new StreamWriter(_filename, false, System.Text.Encoding.Default);

                foreach (AnnoListItem e in this)
                {
                    sw.WriteLine(e.Start.ToString() + _delimiter + e.Stop.ToString() + _delimiter + e.Label + _delimiter + e.Confidence);
                }
                sw.Close();

                HasChanged = false;
                AnnoList newAnno = new AnnoList(_filename);
                return newAnno;
            }
            catch
            {
                return null;
            }
        }

        public AnnoList saveContinousToFile()
        {
            if (filepath == null || filepath.Split('.')[1] != "csv")
            {
                filepath = ViewTools.SaveFileDialog(this.Name, ".csv", Path.GetDirectoryName(this.Filepath), 2);
            }
            this.Filepath = filepath;

            return saveContinousToFile(filepath);
        }

        public AnnoList saveContinousToFile(String _filename)
        {
            bool metawritten = false;
            try
            {
                StreamWriter sw = new StreamWriter(_filename, false, System.Text.Encoding.Default);

                foreach (AnnoListItem e in this)
                {
                    if (!metawritten)
                    {
                        sw.WriteLine(e.Start.ToString() + ";" + e.Label + ";" + e.Confidence + ";" + this.lowborder + ";" + this.highborder);
                        metawritten = true;
                    }
                    else
                    {
                        sw.WriteLine(e.Start.ToString() + ";" + e.Label + ";" + e.Confidence);
                    }
                }
                sw.Close();

                HasChanged = false;

                AnnoList newAnno = new AnnoList(_filename);
                newAnno.filename = _filename;
                return newAnno;
            }
            catch
            {
                return null;
            }
        }
    }

    public class AnnotationScheme
    {
        public string name { get; set; }

        public List<LabelColorPair> LabelsAndColors { get; set; }

        public double minborder { get; set; }

        public double maxborder { get; set; }

        public double sr { get; set; }

        public string mincolor { get; set; }

        public string maxcolor { get; set; }

        public string type { get; set; }
    }
}