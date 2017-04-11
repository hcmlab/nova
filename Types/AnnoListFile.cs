﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

namespace ssi
{
    public partial class AnnoList
    {

        #region SAVE TO FILE

        private bool saveToFile(string filePath, string delimiter = ";")
        {
            Dictionary<string, string> LabelIds = new Dictionary<string, string>();

            try
            {
                StreamWriter sw = new StreamWriter(filePath, false, System.Text.Encoding.Default);
                sw.WriteLine("<?xml version=\"1.0\" ?>");
                sw.WriteLine("<annotation ssi-v=\"3\">");

                sw.WriteLine("    <info ftype=\"" + Source.File.Type.ToString() + "\" size=\"" + this.Count + "\" />");
                sw.WriteLine("    <meta annotator=\"" + Properties.Settings.Default.Annotator + "\"/>");
                if (Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    sw.WriteLine("    <scheme name=\"" + this.Scheme.Name + "\" type=\"CONTINUOUS\" sr=\"" + this.Scheme.SampleRate + "\" min=\"" + this.Scheme.MinScore + "\" max=\"" + this.Scheme.MaxScore + "\" mincolor=\"" + this.Scheme.MinOrBackColor + "\" maxcolor=\"" + this.Scheme.MaxOrForeColor + "\" />");
                }
                else if (Scheme.Type == AnnoScheme.TYPE.FREE)
                {
                    sw.WriteLine("    <scheme name=\"" + this.Scheme.Name + "\" type=\"FREE\" color=\"" + this.Scheme.MinOrBackColor + "\"/>");
                }
                else if (Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                {
                    sw.WriteLine("    <scheme name=\"" + this.Scheme.Name + "\" type=\"DISCRETE\"  color=\"" + this.Scheme.MinOrBackColor + "\">");
                    int index = 0;

                    foreach (AnnoScheme.Label lp in this.Scheme.Labels)
                    {
                        if (lp.Name != "GARBAGE")
                        {
                            sw.WriteLine("        <item name=\"" + lp.Name + "\" id=\"" + index + "\" color=\"" + lp.Color + "\" />");
                            LabelIds.Add(lp.Name, index.ToString());
                            index++;
                        }
                    }
                    sw.WriteLine("    </scheme>");
                }

                else if (Scheme.Type == AnnoScheme.TYPE.POINT)
                {
                    sw.WriteLine("    <scheme name=\"" + this.Scheme.Name + "\" type=\"POINT\" sr=\"" + this.Scheme.SampleRate + "\" num=\"" + this.Scheme.NumberOfPoints + "\" color=\"" + this.Scheme.MinOrBackColor + "\" />");
                }
                else if (Scheme.Type == AnnoScheme.TYPE.POLYGON)
                {
                    sw.WriteLine("    <scheme name=\"" + this.Scheme.Name + "\" type=\"CONTINUOUS\" sr=\"" + this.Scheme.SampleRate + "\" num=\"" + this.Scheme.NumberOfPoints + "\" color=\"" + this.Scheme.MinOrBackColor + "\" />");
                }
                else if (Scheme.Type == AnnoScheme.TYPE.GRAPH)
                {
                    sw.WriteLine("    <scheme name=\"" + this.Scheme.Name + "\" type=\"CONTINUOUS\" sr=\"" + this.Scheme.SampleRate + "\" num=\"" + this.Scheme.NumberOfPoints + "\" color=\"" + this.Scheme.MinOrBackColor + "\"  />");
                }
                else if (Scheme.Type == AnnoScheme.TYPE.SEGMENTATION)
                {
                    sw.WriteLine("    <scheme name=\"" + this.Scheme.Name + "\" type=\"CONTINUOUS\" sr=\"" + this.Scheme.SampleRate + "\" num=\"" + this.Scheme.NumberOfPoints +  "\" color=\"" + this.Scheme.MinOrBackColor + "\" />");
                }

                sw.WriteLine("</annotation>");
                sw.Close();
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
                return false;
            }

            try
            {
                if (Source.File.Type == AnnoSource.FileSource.TYPE.ASCII)
                {
                    StreamWriter sw = new StreamWriter(filePath + "~", false, System.Text.Encoding.Default);
                    if (Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                    {
                        foreach (AnnoListItem e in this)
                        {
                            sw.WriteLine(e.Label + delimiter + e.Confidence.ToString("n2"));
                        }
                    }
                    else if (Scheme.Type == AnnoScheme.TYPE.FREE)
                    {
                        foreach (AnnoListItem e in this)
                        {
                            if (e.Color != Scheme.MaxOrForeColor)
                            {
                                sw.WriteLine(e.Start.ToString("n2") + delimiter + e.Stop.ToString("n2") + delimiter + e.Label + delimiter + e.Confidence.ToString("n2"));
                            }
                            else
                            {
                                sw.WriteLine(e.Start.ToString("n2") + delimiter + e.Stop.ToString("n2") + delimiter + e.Label + delimiter + e.Confidence.ToString("n2") + delimiter + "color=" + e.Color.ToString());
                            }
                        }
                    }
                    else if (Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                    {
                        string index = "";

                        foreach (AnnoListItem e in this)
                        {
                            if (e.Label != "GARBAGE")
                            {
                                LabelIds.TryGetValue(e.Label, out index);
                                sw.WriteLine(e.Start.ToString("n2") + delimiter + e.Stop.ToString("n2") + delimiter + index + delimiter + e.Confidence.ToString("n2"));
                            }
                            else
                            {
                                sw.WriteLine(e.Start.ToString("n2") + delimiter + e.Stop.ToString("n2") + delimiter + -1 + delimiter + e.Confidence.ToString("n2"));
                            }
                        }
                    }

                    else if (Scheme.Type == AnnoScheme.TYPE.POINT)
                    {
                        foreach (AnnoListItem e in this)
                        {
                            string output = "";
                            output += e.Label + delimiter;
                            for (int i = 0; i < e.Points.Count; ++i)
                            {
                                output += '(' + e.Points[i].Label + ':' + e.Points[i].XCoord + ':' + e.Points[i].YCoord + ":" + e.Points[i].Confidence + ')' + delimiter;
                            }
                            sw.WriteLine(output + e.Confidence.ToString("n2"));
                        }
                    }
                    else if (Scheme.Type == AnnoScheme.TYPE.POLYGON)
                    {
                        foreach (AnnoListItem e in this)
                        {
                            sw.WriteLine(e.Label + delimiter + e.Confidence.ToString("n2"));
                        }
                    }
                    else if (Scheme.Type == AnnoScheme.TYPE.GRAPH)
                    {
                        foreach (AnnoListItem e in this)
                        {
                            sw.WriteLine(e.Label + delimiter + e.Confidence.ToString("n2"));
                        }
                    }
                    else if (Scheme.Type == AnnoScheme.TYPE.SEGMENTATION)
                    {
                        foreach (AnnoListItem e in this)
                        {
                            sw.WriteLine(e.Label + delimiter + e.Confidence.ToString("n2"));
                        }
                    }

                    sw.Close();
                }
                else
                {
                    BinaryWriter bw = new BinaryWriter(new FileStream(filePath + "~", FileMode.Create));
                    if (Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                    {
                        foreach (AnnoListItem e in this)
                        {
                            bw.Write(float.Parse(e.Label));
                            bw.Write((float)e.Confidence);
                        }
                    }
                    else if (Scheme.Type == AnnoScheme.TYPE.FREE)
                    {
                        foreach (AnnoListItem e in this)
                        {
                            bw.Write(e.Start);
                            bw.Write(e.Stop);
                            bw.Write((uint)e.Label.Length);
                            byte[] label = System.Text.Encoding.UTF8.GetBytes(e.Label);
                            bw.Write(label);
                            bw.Write((float)e.Confidence);
                        }
                    }
                    else if (Scheme.Type == AnnoScheme.TYPE.DISCRETE)
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
                AnnoList newAnno = new AnnoList();
                newAnno.Source.File.Path = filePath;
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
                return false;
            }

            return true;
        }

        public bool SaveToCSVFile(string delimiter = ";")
        {
            string filePath = FileTools.SaveFileDialog(Source.File.Path != "" ? Source.File.Name : DefaultName(), ".csv", "Annotation(*.csv)|*.csv", "");

            if (filePath == null) return false;

            if (Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
            {
                try
                {
                    StreamWriter sw = new StreamWriter(filePath, false, System.Text.Encoding.Default);

                    foreach (AnnoListItem e in this)
                    {
                        sw.WriteLine(e.Start.ToString() + ";" + e.Label + ";" + e.Confidence);
                    }
                    sw.Close();

                    return true;
                }
                catch (Exception ex)
                {
                    MessageTools.Error(ex.ToString());
                }
            }
            else
            {
                try
                {
                    StreamWriter sw = new StreamWriter(filePath, false, System.Text.Encoding.Default);

                    foreach (AnnoListItem e in this)
                    {
                        sw.WriteLine(e.Start.ToString() + delimiter + e.Stop.ToString() + delimiter + e.Label + delimiter + e.Confidence);
                    }
                    sw.Close();

                    return true;
                }
                catch (Exception ex)
                {
                    MessageTools.Error(ex.ToString());
                }
            }

            return false;
        }


        #endregion

        #region LOAD FROM FILE

        public static AnnoList LoadfromFile(string filepath)
        {
            AnnoList list = new AnnoList();

            list.Source.File.Path = filepath;
            list.Scheme = new AnnoScheme();
            list.Scheme.Labels = new List<AnnoScheme.Label>();

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filepath);

                XmlNode annotation = doc.SelectSingleNode("annotation");

                XmlNode info = annotation.SelectSingleNode("info");
                list.Source.File.Type = info.Attributes["ftype"].Value == AnnoSource.FileSource.TYPE.ASCII.ToString() ? AnnoSource.FileSource.TYPE.ASCII : AnnoSource.FileSource.TYPE.BINARY;
                int size = int.Parse(info.Attributes["size"].Value);

                XmlNode meta = annotation.SelectSingleNode("meta");
                if (meta != null)
                {
                    if (meta.Attributes["role"] != null) list.Meta.Role = meta.Attributes["role"].Value;
                    if (meta.Attributes["annotator"] != null) list.Meta.Annotator = meta.Attributes["annotator"].Value;
                    if (meta.Attributes["trigger"] != null)
                    {
                        string[] triggers = meta.Attributes["trigger"].Value.Split(';');
                        foreach (string trigger in triggers)
                        {
                            Match match = Regex.Match(trigger, @"([^{]+)\{([^}]*)\}");
                            if (match.Success && match.Groups.Count == 3)
                            {
                                string dllName = match.Groups[1].Value;
                                string arguments = match.Groups[2].Value;
                                Dictionary<string, object> args = arguments.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                                    .Select(part => part.Split('='))
                                    .ToDictionary(split => split[0], split => (object)split[1]);                                
                                PluginCaller pluginCaller = new PluginCaller(dllName + ".dll", dllName);
                                AnnoTrigger annoTrigger = new AnnoTrigger(list, pluginCaller, args);
                                list.Meta.Trigger.Add(annoTrigger);
                            }
                            else
                            {
                                MessageTools.Warning("could not parse trigger '" + trigger + "'");
                            }



                        }
                        
                    }
                }

                XmlNode scheme = annotation.SelectSingleNode("scheme");
                if (scheme.Attributes["name"] != null)
                {
                    list.Scheme.Name = scheme.Attributes["name"].Value;
                }
                string type = "FREE";
                if (scheme.Attributes["type"] != null)
                {
                    type = scheme.Attributes["type"].Value;
                }

                if (type == AnnoScheme.TYPE.DISCRETE.ToString())
                {
                    list.Scheme.Type = AnnoScheme.TYPE.DISCRETE;
                }
                else if (type == AnnoScheme.TYPE.CONTINUOUS.ToString())
                {
                    list.Scheme.Type = AnnoScheme.TYPE.CONTINUOUS;
                }
                else if (type == AnnoScheme.TYPE.FREE.ToString())
                {
                    list.Scheme.Type = AnnoScheme.TYPE.FREE;
                }

                else if (type == AnnoScheme.TYPE.POINT.ToString())
                {
                    list.Scheme.Type = AnnoScheme.TYPE.POINT;
                }
                else if (type == AnnoScheme.TYPE.POLYGON.ToString())
                {
                    list.Scheme.Type = AnnoScheme.TYPE.POLYGON;
                }
                else if (type == AnnoScheme.TYPE.GRAPH.ToString())
                {
                    list.Scheme.Type = AnnoScheme.TYPE.GRAPH;
                }
                else if (type == AnnoScheme.TYPE.SEGMENTATION.ToString())
                {
                    list.Scheme.Type = AnnoScheme.TYPE.SEGMENTATION;
                }

                if (scheme.Attributes["color"] != null)
                {
                    list.Scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(scheme.Attributes["color"].Value);
                }
                else if (scheme.Attributes["mincolor"] != null)
                {
                    list.Scheme.MinOrBackColor = (Color)ColorConverter.ConvertFromString(scheme.Attributes["mincolor"].Value);
                }
                else if (list.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    list.Scheme.MinOrBackColor = Defaults.Colors.GradientMin;
                }

                if (scheme.Attributes["maxcolor"] != null)
                {
                    list.Scheme.MaxOrForeColor = (Color)ColorConverter.ConvertFromString(scheme.Attributes["maxcolor"].Value);
                }
                else if (list.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    list.Scheme.MaxOrForeColor = Defaults.Colors.GradientMax;
                }

                Dictionary<string, string> LabelIds = new Dictionary<string, string>();

                if (list.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                {
                    foreach (XmlNode item in scheme)
                    {
                        LabelIds.Add(item.Attributes["id"].Value, item.Attributes["name"].Value);

                        Color color = Defaults.Colors.Foreground;
                        if (item.Attributes["color"] != null) color = (Color)ColorConverter.ConvertFromString(item.Attributes["color"].Value);
                        AnnoScheme.Label lcp = new AnnoScheme.Label(item.Attributes["name"].Value, color);
                        list.Scheme.Labels.Add(lcp);
                    }

                    AnnoScheme.Label garbage = new AnnoScheme.Label("GARBAGE", Defaults.Colors.Foreground);
                    list.Scheme.Labels.Add(garbage);
                }
                else if (list.Scheme.Type == AnnoScheme.TYPE.FREE)
                {
                }
                else if (list.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                {
                    list.Scheme.MinScore = double.Parse(scheme.Attributes["min"].Value);
                    list.Scheme.MaxScore = double.Parse(scheme.Attributes["max"].Value);
                    list.Scheme.SampleRate = double.Parse(scheme.Attributes["sr"].Value);
                }
                else if (list.Scheme.Type == AnnoScheme.TYPE.POINT ||
                         list.Scheme.Type == AnnoScheme.TYPE.POLYGON || list.Scheme.Type == AnnoScheme.TYPE.GRAPH ||
                         list.Scheme.Type == AnnoScheme.TYPE.SEGMENTATION)
                {
                    list.Scheme.SampleRate = double.Parse(scheme.Attributes["sr"].Value);
                    list.Scheme.NumberOfPoints = int.Parse(scheme.Attributes["num"].Value);                    
                }

                if (File.Exists(filepath + "~"))

                {
                    if (list.Source.File.Type == AnnoSource.FileSource.TYPE.ASCII)
                    {
                        StreamReader sr = new StreamReader(filepath + "~", System.Text.Encoding.Default);
                        string line = null;
                        double start = 0.0;

                        while ((line = sr.ReadLine()) != null)
                        {
                            string[] data = line.Split(';');
                            if (list.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                            {
                                string value = data[0];
                                double confidence = Convert.ToDouble(data[1], CultureInfo.InvariantCulture);
                                AnnoListItem e = new AnnoListItem(start, 1 / list.Scheme.SampleRate, value, "", Defaults.Colors.Foreground, confidence);
                                list.Add(e);
                                start = start + 1 / list.Scheme.SampleRate;
                            }
                            else if (list.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                            {
                                start = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                                double stop = Convert.ToDouble(data[1], CultureInfo.InvariantCulture);
                                double dur = stop - start;
                                string label = "";
                                if (int.Parse(data[2]) < 0) label = "GARBAGE";
                                else LabelIds.TryGetValue(data[2], out label);
                                Color color = Colors.Black;

                                if (list.Scheme.Labels.Find(x => x.Name == label) != null)
                                {
                                    color = list.Scheme.Labels.Find(x => x.Name == label).Color;
                                }

                                double confidence = Convert.ToDouble(data[3], CultureInfo.InvariantCulture);
                                AnnoListItem e = new AnnoListItem(start, dur, label, "", color, confidence);
                                list.AddSorted(e);
                            }
                            else if (list.Scheme.Type == AnnoScheme.TYPE.FREE)
                            {
                                start = Convert.ToDouble(data[0], CultureInfo.InvariantCulture);
                                double stop = Convert.ToDouble(data[1], CultureInfo.InvariantCulture);
                                double dur = stop - start;
                                string label = data[2];
                                double confidence = Convert.ToDouble(data[3], CultureInfo.InvariantCulture);
                                Color color = Colors.Black;
                                if (data.Length > 4)
                                {
                                    string[] metapairs = data[4].Split('=');
                                    for (int i = 0; i < metapairs.Length; i++)
                                    {
                                        if (metapairs[i].Contains("color"))
                                        {
                                            color = (Color)ColorConverter.ConvertFromString(metapairs[i + 1]);
                                            break;
                                        }
                                    }
                                }
                                AnnoListItem e = new AnnoListItem(start, dur, label, "", color, confidence);
                                list.AddSorted(e);
                            }

                            else if (list.Scheme.Type == AnnoScheme.TYPE.POINT)
                            {
                                string frameLabel = data[0];
                                double frameConfidence = Convert.ToDouble(data[data.Count() - 1], CultureInfo.InvariantCulture);
                                PointList points = new PointList();
                                for (int i = 1; i < data.Count() - 1; ++i)
                                {
                                    string pd = data[i].Replace("(", "");
                                    pd = pd.Replace(")", "");
                                    string[] pointData = pd.Split(':');
                                    points.Add(new PointListItem(double.Parse(pointData[1]), double.Parse(pointData[2]), pointData[0], double.Parse(pointData[3])));
                                }
                                AnnoListItem ali = new AnnoListItem(start, 1 / list.Scheme.SampleRate, frameLabel, "", list.Scheme.MinOrBackColor, frameConfidence, true, points);
                                list.Add(ali);
                                start = start + 1 / list.Scheme.SampleRate;
                            }
                        }
                        sr.Close();
                    }
                    else if (list.Source.File.Type == AnnoSource.FileSource.TYPE.BINARY)
                    {
                        BinaryReader binaryReader = new BinaryReader(File.Open(filepath + "~", FileMode.Open));
                        long length = (binaryReader.BaseStream.Length);

                        double start = 0.0;
                        while (binaryReader.BaseStream.Position < binaryReader.BaseStream.Length)
                        {
                            if (list.Scheme.Type == AnnoScheme.TYPE.CONTINUOUS)
                            {
                                string value = binaryReader.ReadSingle().ToString();
                                double confidence = (double)binaryReader.ReadSingle();
                                AnnoListItem e = new AnnoListItem(start, 1 / list.Scheme.SampleRate, value, "", Defaults.Colors.Foreground, confidence);
                                list.Add(e);
                                start = start + 1 / list.Scheme.SampleRate;
                            }
                            else if (list.Scheme.Type == AnnoScheme.TYPE.DISCRETE)
                            {
                                start = binaryReader.ReadDouble();
                                double stop = binaryReader.ReadDouble();
                                double dur = stop - start;
                                string label = "";
                                int index = binaryReader.ReadInt32();
                                if (index < 0) label = "GARBAGE";
                                else LabelIds.TryGetValue(index.ToString(), out label);
                                Color color = Colors.Black;

                                if (list.Scheme.Labels.Find(x => x.Name == label) != null)
                                {
                                    color = list.Scheme.Labels.Find(x => x.Name == label).Color;
                                }
                                double confidence = Math.Round(binaryReader.ReadSingle(), 3, MidpointRounding.AwayFromZero);
                                AnnoListItem e = new AnnoListItem(start, dur, label, "", color, confidence);
                                list.AddSorted(e);
                            }
                            else if (list.Scheme.Type == AnnoScheme.TYPE.FREE)
                            {
                                start = binaryReader.ReadDouble();
                                double stop = binaryReader.ReadDouble();
                                double dur = stop - start;
                                int stringlength = (int)binaryReader.ReadUInt32();
                                byte[] labelasbytes = (binaryReader.ReadBytes(stringlength));
                                string label = System.Text.Encoding.Default.GetString(labelasbytes);
                                Color color = Colors.Black;
                                double confidence = Math.Round(binaryReader.ReadSingle(), 3, MidpointRounding.AwayFromZero);
                                AnnoListItem e = new AnnoListItem(start, dur, label, "", color, confidence);
                                list.AddSorted(e);
                            }
                        }

                        binaryReader.Close();
                    }
                }
                //else
                //{
                //    MessageBox.Show("Annotation data was not found, load scheme only from '" + filepath + "'");
                //}
            }
            catch (Exception e)
            {
                MessageBox.Show("An exception occured while reading annotation from '" + filepath + "'");
            }

            return list;
        }


        public static AnnoList LoadFromEventFile(String filepath)
        {
            AnnoList list = new AnnoList();
            try
            {
                XDocument doc = XDocument.Load(filepath);
                var events = doc.Descendants("event");

                //list.Scheme.Type = AnnoScheme.TYPE.DISCRETE;
                //list.Scheme.Labels = new List<AnnoScheme.Label>();

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
                            if (e.Value != "") label = e.Value;

                            break;

                        case Type.TUPLE:
                            meta = e.Value == null ? "" : e.Value;
                            break;
                    }

                    AnnoScheme.Label asl = new AnnoScheme.Label(label, Colors.Black);
                    if(!list.Scheme.Labels.Any(item => item.Name == asl.Name)) list.Scheme.Labels.Add(asl); 

                    var state_attr = e.Attribute("state");
                    if (state_attr.Value.ToString().ToUpper() == "COMPLETED")
                    {
                        Color color = Colors.Black;

                        //if (list.Scheme.Labels.Find(x => x.Name == label) != null)
                        //{
                        //    color = list.Scheme.Labels.Find(x => x.Name == label).Color;
                        //}

                        list.AddSorted(new AnnoListItem(start, duration, label, meta, color));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
            }

            //AnnoScheme.Label garbage = new AnnoScheme.Label("GARBAGE", Colors.Black);
            //list.Scheme.Labels.Add(garbage);


            return list;
        }

        public static AnnoList LoadFromCSVFile(String filepath, double samplerate = 1, string type = "legacy", string filter = null)
        {
            AnnoList list = new AnnoList();
            list.Source.File.Path = filepath;
            list.Scheme = new AnnoScheme();

            try
            {
                StreamReader sr = new StreamReader(filepath, System.Text.Encoding.Default);
                string line = null;
                while ((line = sr.ReadLine()) != null)
                {
                    if (type == "semicolon")
                    {
                        list.Scheme.Type = AnnoScheme.TYPE.FREE;
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

                        AnnoListItem e = new AnnoListItem(start, duration, label, meta, Colors.Black, confidence);

                        if (filter == null || tier == filter)
                            list.AddSorted(e);
                    }
                    else if (type == "continuous")
                    {
                        list.Scheme.Type = AnnoScheme.TYPE.CONTINUOUS;
                        list.Scheme.SampleRate = samplerate;
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
                            list.Scheme.MinScore = double.Parse(data[3]);

                            if (data.Length > 4)
                            {
                                list.Scheme.MaxScore = double.Parse(data[4]);
                            }
                            AnnoListItem e = new AnnoListItem(start, samplerate == 0 ? 0 : 1 /samplerate, label, "Range: " + list.Scheme.MinScore + "-" + list.Scheme.MaxScore, Colors.Black);
                            list.AddSorted(e);
                        }
                        else
                        {
                            AnnoListItem e = new AnnoListItem(start, samplerate == 0 ? 0 : 1 / samplerate, label, "", Colors.Black, 1);
                            if (filter == null || tier == filter)
                                list.AddSorted(e);
                        }
                    }
                    else if (type == "legacy")
                    {
                        list.Scheme.Type = AnnoScheme.TYPE.FREE;
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
                        AnnoListItem e = new AnnoListItem(start, duration, label, "", Colors.Black, 1);
                        if (filter == null || tier == filter)
                            list.AddSorted(e);
                    }
                }
                sr.Close(); ;
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

                    list[i] = new AnnoList();
                    list[i].Source.File.Path = filepath;

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
                        list[i].AddSorted(new AnnoListItem(start, duration, label, "", Colors.Black));
                        list[i].Scheme.Name = tierid;
                        //The tier is used as metainformation as well. Might be changed if thats relevant in the future
                    }
                    i++;
                }
            }
            catch (Exception ex)
            {
                MessageTools.Error(ex.ToString());
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
                list[index] = new AnnoList();
                list[index].Source.File.Path = filepath;

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
                        list[index].AddSorted(new AnnoListItem(start, duration, label, meta, Colors.Black));
                        list[index].Scheme.Name = tierid;
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
    }

    #endregion

}

