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
        private string  annotator = null;

        private static bool _isDiscrete = true;

        public bool Loaded
        {
            get { return loaded; }
            set { loaded = value; }
        }

        public string Role
        {
            get { return role; }
            set { role = value; }
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

        public bool isDiscrete
        {
            get { return _isDiscrete; }
            set { _isDiscrete = value; }
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
                    label = (event_attr == null ? "" : event_attr.Value)
                            + "@"
                            + (sender_attr == null ? "" : sender_attr.Value);
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
                        case Type.TUPLE:
                            meta = e.Value == null ? "" : e.Value;
                            break;
                    }

                    list.Add(new AnnoListItem(start, duration, label, meta));
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
            try
            {
                StreamReader sr = new StreamReader(filepath, System.Text.Encoding.Default);
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (type == "semicolon")
                    {
                        _isDiscrete = true;
                        string[] data = line.Split(';');
                        double start = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                        double duration = Convert.ToDouble(data[1]) - Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                        string label = "";
                        string tier = "";
                        string meta = "";
                        if (data.Length > 2)
                        {
                            label = data[2];
                        }
                        if (data.Length > 3)
                        {
                            tier = data[3];
                            tier = tier.Remove(0, 1);
                        }
                        if (data.Length > 4)
                        {
                            for (int i = 4; i < data.Length; i++)
                            {
                                meta += data[i] + ";";
                            }
                        }

                        AnnoListItem e = new AnnoListItem(start, duration, label, meta, tier);

                        if (filter == null || tier == filter)
                            list.Add(e);
                    }
                    else if (type == "continuous")
                    {
                        _isDiscrete = false;
                        string[] data = line.Split(';');
                        double start = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                        string label = "";
                        string tier = "";

                        if (data.Length > 1)
                        {
                            label = data[1];
                        }
                        if (data.Length > 2)
                        {
                            tier = data[2];
                            tier = tier.Remove(0, 1);
                        }
                        if (data.Length > 3)
                        {
                            list.Lowborder = double.Parse(data[3]);

                            if (data.Length > 4)
                            {
                                list.Highborder = double.Parse(data[4]);
                            }
                            AnnoListItem e = new AnnoListItem(start, samplerate, label, "Range: " + list.Lowborder + "-" + list.Highborder, tier);
                            list.Add(e);
                        }
                        else
                        {
                            AnnoListItem e = new AnnoListItem(start, samplerate, label, "", tier);
                            if (filter == null || tier == filter)
                                list.Add(e);
                        }
                    }
                    else if (type == "legacy")
                    {
                        _isDiscrete = true;
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
                            list.Add(e);
                    }
                }
                sr.Close(); ;
                list.loaded = true;
                list.HasChanged = false;
            }
            catch (Exception e)
            {
                MessageBox.Show("Can't read annotation file. Is it used by another program?");
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
                        list[i].Add(new AnnoListItem(start, duration, label, "", tierid));
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
                        list[index].Add(new AnnoListItem(start, duration, label, meta, tierid));
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
            if (filepath == null || filepath.Split('.')[1] == "eaf")
            {
                filepath = ViewTools.SaveFileDialog("", ".csv");
                if (filepath != null)
                {
                    filename = filepath.Split('.')[0];
                }
            }

            return saveToFile(filepath);
        }

        public AnnoList saveToFile(String _filename, String _delimiter = ";")
        {
            try
            {
                StreamWriter sw = new StreamWriter(_filename, false, System.Text.Encoding.Default);

                foreach (AnnoListItem e in this)
                {
                    sw.WriteLine(e.Start.ToString() + _delimiter + e.Stop.ToString() + _delimiter + e.Label + _delimiter + "#" + this.Name);
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
            if (filepath == null)
            {
                filepath = ViewTools.SaveFileDialog(this.name, ".csv");
                if (filepath != null)
                {
                    filename = filepath.Split('.')[0];
                }
            }

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
                        sw.WriteLine(e.Start.ToString() + ";" + e.Label + ";#" + this.Name + ";" + this.lowborder + ";" + this.highborder);
                        metawritten = true;
                    }
                    else
                    {
                        sw.WriteLine(e.Start.ToString() + ";" + e.Label + ";#" + this.Name);
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