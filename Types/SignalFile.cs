using System;
using System.IO;
using System.Xml;

namespace ssi
{
    public partial class Signal
    {
        #region LOAD FILE

        public static Signal LoadARFFFile(string filepath)
        {
            Signal signal = null;

            try
            {
                Signal.Type type = Signal.Type.FLOAT;
                uint dim = 0;
                double rate = 0;

                string[] lines = File.ReadAllLines(filepath);
                char[] delims = { ' ', '\t', ';', ',' };
                string[] tokens = lines[0].Split(delims);
                dim = (uint)tokens.Length;

                string[] row = null;

                row = lines[0].Split(delims);
                double time1 = double.Parse(row[1]);
                row = lines[1].Split(delims);
                double time2 = double.Parse(row[1]);

                double step = time2 - time1;
                rate = 1000.0 / (1000.0 * step);

                uint number = (uint)lines.Length;
                uint bytes = Signal.TypeSize[(int)type];

                if (dim > 0)
                {
                    signal = new Signal(filepath, rate, 1, bytes, number, type);

                    StreamReader fs_data = new StreamReader(filepath);
                    LoadDataArff(signal, fs_data, dim - 1);
                    fs_data.Close();

                    signal.ShowDim = 0;
                    signal.loaded = true;
                }
            }
            catch (Exception e)
            {
                MessageTools.Error(e.ToString());
                return null;
            }

            return signal;
        }

        public static Signal LoadCSVFile(string filepath)
        {
            Signal signal = null;

            try
            {
                Signal.Type type = Signal.Type.UNDEF;
                uint dim = 0;
                double rate = 0;

                if (SelectDataType(filepath, ref type, ref rate))
                {
                    string[] lines = File.ReadAllLines(filepath);

                    uint number = (uint)lines.Length;
                    uint bytes = Signal.TypeSize[(int)type];

                    if (type != Signal.Type.UNDEF
                        && rate > 0
                        && bytes > 0
                        && number > 0)
                    {
                        char[] delims = { ' ', '\t', ';', ',' };
                        string[] tokens = lines[0].Split(delims);
                        dim = (uint)tokens.Length;

                        if (dim > 0)
                        {
                            signal = new Signal(filepath, rate, dim, bytes, number, type);

                            StreamReader fs_data = new StreamReader(filepath);
                            LoadDataV2a(signal, fs_data);
                            fs_data.Close();

                            signal.loaded = true;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                MessageTools.Error(e.ToString());
                return null;
            }

            return signal;
        }

        public static Signal LoadStreamFile(string filepath)
        {
            Signal signal = new Signal();
            if (filepath.EndsWith("~")) filepath = filepath.Remove(filepath.Length - 1);
            try
            {
                // parse filename
                signal.filePath = filepath;
                string[] tmp = filepath.Split('\\');
                signal.fileName = tmp[tmp.Length - 1];
                signal.name = signal.fileName.Split('.')[0];

                // open file
                FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[12];

                // determine version
                fs.Read(buffer, 0, 4);
                if (buffer[0] == '<' && buffer[1] == '?' && buffer[2] == 'x')
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    do
                    {
                    } while (Convert.ToChar(fs.ReadByte()) != '\n');
                    XmlReader xml = XmlReader.Create(fs);
                    do
                    {
                        xml.Read();
                    } while (xml.Name != "stream");

                    xml.MoveToAttribute("ssi-v");
                    string version_s = xml.Value;
                    int version = int.Parse(version_s);

                    do
                    {
                        xml.Read();
                    } while (xml.Name != "info");

                    xml.MoveToAttribute("ftype");
                    string ftype_s = xml.Value;

                    xml.MoveToAttribute("sr");
                    string sr_s = xml.Value;
                    signal.rate = double.Parse(sr_s);

                    xml.MoveToAttribute("dim");
                    string dim_s = xml.Value;
                    signal.dim = uint.Parse(dim_s);

                    xml.MoveToAttribute("byte");
                    string bytes_s = xml.Value;
                    signal.bytes = uint.Parse(bytes_s);

                    xml.MoveToAttribute("type");
                    string type_s = xml.Value;
                    for (uint i = 0; i < Signal.TypeName.Length; i++)
                    {
                        if (type_s == Signal.TypeName[i])
                        {
                            signal.type = (Signal.Type)i;
                        }
                    }

                    uint num = 0;
                    while (xml.Read())
                    {
                        if (xml.IsStartElement() && xml.Name == "meta")
                        {
                            xml.MoveToAttribute("name");
                            string meta_name = xml.Value;
                            signal.meta_name = meta_name;

                            xml.MoveToAttribute("num");
                            string meta_num = xml.Value;
                            signal.meta_num = int.Parse(meta_num);

                            xml.MoveToAttribute("type");
                            string meta_type = xml.Value;
                            signal.meta_type = meta_type;
                        }

                        if (xml.IsStartElement() && xml.Name == "chunk")
                        {
                            xml.MoveToAttribute("num");
                            string num_s = xml.Value;
                            num += uint.Parse(num_s);
                        }
                    }

                    signal.number = num;
                    signal.time = 0;
                    signal.data = new float[signal.dim * signal.number];

                    if (ftype_s == "ASCII")
                    {
                        StreamReader fs_data = new StreamReader(filepath + "~");
                        LoadDataV2a(signal, fs_data);
                        fs_data.Close();
                    }
                    else
                    {
                        FileStream fs_data = new FileStream(filepath + "~", FileMode.Open, FileAccess.Read);
                        LoadDataV2b(signal, fs_data);
                        fs_data.Close();
                    }
                }
                else
                {
                    MessageTools.Error("could not read stream file '" + filepath + "'");
                    return null;
                }

                // close file
                fs.Close();

                signal.minmax();
                signal.loaded = true;
            }
            catch (Exception e)
            {
                MessageTools.Error(filepath + ": " + e.ToString());
                return null;
            }

            return signal;
        }

        public static bool LoadDataArff(Signal signal, StreamReader fs, uint dim)
        {
            string line = null;
            string[] row = null;

            char[] split = { ' ', '\t', ';', ',' };
            for (UInt32 i = 0; i < signal.number; i++)
            {
                line = fs.ReadLine();
                row = line.Split(split);
                signal.data[i] = float.Parse(row[dim]);
            }
            signal.minmax();

            return true;
        }

        public static bool LoadDataV2a(Signal signal, StreamReader fs)
        {
            string line = null;
            string[] row = null;

            char[] split = { ' ', '\t', ';', ',' };

            switch (signal.type)
            {
                case Signal.Type.SHORT:
                    {
                        for (UInt32 i = 0; i < signal.number; i++)
                        {
                            line = fs.ReadLine();
                            row = line.Split(split);
                            for (UInt32 j = 0; j < signal.dim; j++)
                            {
                                signal.data[i * signal.dim + j] = (float)short.Parse(row[j]);
                            }
                        }
                    }
                    break;

                case Signal.Type.USHORT:
                    {
                        for (UInt32 i = 0; i < signal.number; i++)
                        {
                            line = fs.ReadLine();
                            row = line.Split(split);
                            for (UInt32 j = 0; j < signal.dim; j++)
                            {
                                signal.data[i * signal.dim + j] = (float)ushort.Parse(row[j]);
                            }
                        }
                    }
                    break;

                case Signal.Type.INT:
                    {
                        for (UInt32 i = 0; i < signal.number; i++)
                        {
                            line = fs.ReadLine();
                            row = line.Split(split);
                            for (UInt32 j = 0; j < signal.dim; j++)
                            {
                                signal.data[i * signal.dim + j] = (float)int.Parse(row[j]);
                            }
                        }
                    }
                    break;

                case Signal.Type.UINT:
                    {
                        for (UInt32 i = 0; i < signal.number; i++)
                        {
                            line = fs.ReadLine();
                            row = line.Split(split);
                            for (UInt32 j = 0; j < signal.dim; j++)
                            {
                                signal.data[i * signal.dim + j] = (float)uint.Parse(row[j]);
                            }
                        }
                    }
                    break;

                case Signal.Type.FLOAT:
                    {
                        for (UInt32 i = 0; i < signal.number; i++)
                        {
                            line = fs.ReadLine();
                            row = line.Split(split);
                            for (UInt32 j = 0; j < signal.dim; j++)
                            {
                                signal.data[i * signal.dim + j] = float.Parse(row[j]);
                            }
                        }
                    }
                    break;

                case Signal.Type.DOUBLE:
                    {
                        for (UInt32 i = 0; i < signal.number; i++)
                        {
                            line = fs.ReadLine();
                            row = line.Split(split);
                            for (UInt32 j = 0; j < signal.dim; j++)
                            {
                                signal.data[i * signal.dim + j] = (float)double.Parse(row[j]);
                            }
                        }
                    }
                    break;
            }

            return true;
        }

        public static bool LoadDataV2b(Signal signal, FileStream fs)
        {
            uint total = signal.number * signal.dim * signal.bytes;
            byte[] data_buffer = new byte[total];

            fs.Read(data_buffer, 0, (int)total);

            int index = 0;
            int step = (int)signal.bytes;

            switch (signal.type)
            {
                case Signal.Type.SHORT:
                    {
                        if (signal.bytes == sizeof(short))
                        {
                            for (UInt32 i = 0; i < signal.number; i++)
                            {
                                for (UInt32 j = 0; j < signal.dim; j++)
                                {
                                    signal.data[i * signal.dim + j] = (float)BitConverter.ToInt16(data_buffer, index);
                                    index += step;
                                }
                            }
                        }
                        else
                        {
                            MessageTools.Error("Invalid number of bytes (" + signal.bytes + ") for data type 'short'");
                            return false;
                        }
                    }
                    break;

                case Signal.Type.USHORT:
                    {
                        if (signal.bytes == sizeof(ushort))
                        {
                            for (UInt32 i = 0; i < signal.number; i++)
                            {
                                for (UInt32 j = 0; j < signal.dim; j++)
                                {
                                    signal.data[i * signal.dim + j] = (float)BitConverter.ToUInt16(data_buffer, index);
                                    index += step;
                                }
                            }
                        }
                        else
                        {
                            MessageTools.Error("Invalid number of bytes (" + signal.bytes + ") for data type 'short'");
                            return false;
                        }
                    }
                    break;

                case Signal.Type.INT:
                    {
                        if (signal.bytes == sizeof(int))
                        {
                            for (UInt32 i = 0; i < signal.number; i++)
                            {
                                for (UInt32 j = 0; j < signal.dim; j++)
                                {
                                    signal.data[i * signal.dim + j] = (float)BitConverter.ToInt32(data_buffer, index);
                                    index += step;
                                }
                            }
                        }
                        else
                        {
                            MessageTools.Error("Invalid number of bytes (" + signal.bytes + ") for data type 'short'");
                            return false;
                        }
                    }
                    break;

                case Signal.Type.UINT:
                    {
                        if (signal.bytes == sizeof(uint))
                        {
                            for (UInt32 i = 0; i < signal.number; i++)
                            {
                                for (UInt32 j = 0; j < signal.dim; j++)
                                {
                                    signal.data[i * signal.dim + j] = (float)BitConverter.ToUInt32(data_buffer, index);
                                    index += step;
                                }
                            }
                        }
                        else
                        {
                            MessageTools.Error("Invalid number of bytes (" + signal.bytes + ") for data type 'short'");
                            return false;
                        }
                    }
                    break;

                case Signal.Type.FLOAT:
                    {
                        if (signal.bytes == sizeof(float))
                        {
                            for (UInt32 i = 0; i < signal.number; i++)
                            {
                                for (UInt32 j = 0; j < signal.dim; j++)
                                {
                                    signal.data[i * signal.dim + j] = BitConverter.ToSingle(data_buffer, index);
                                    index += step;
                                }
                            }
                        }
                        else
                        {
                            MessageTools.Error("Invalid number of bytes (" + signal.bytes + ") for data type 'float'");
                            return false;
                        }
                    }
                    break;

                case Signal.Type.DOUBLE:
                    {
                        if (signal.bytes == sizeof(double))
                        {
                            for (UInt32 i = 0; i < signal.number; i++)
                            {
                                for (UInt32 j = 0; j < signal.dim; j++)
                                {
                                    signal.data[i * signal.dim + j] = (float)BitConverter.ToSingle(data_buffer, index);
                                    index += step;
                                }
                            }
                        }
                        else
                        {
                            MessageTools.Error("Invalid number of bytes (" + signal.bytes + ") for data type 'float'");
                            return false;
                        }
                    }
                    break;
            }

            return true;
        }

        public static Signal LoadWaveFile(string filepath)
        {
            WavHeader Header = new WavHeader();
            Signal signal = null;

            using (FileStream fs = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            using (BinaryReader br = new BinaryReader(fs))
            {
                try
                {
                    Header.riffID = br.ReadBytes(4);
                    Header.size = br.ReadUInt32();
                    Header.wavID = br.ReadBytes(4);
                    Header.fmtID = br.ReadBytes(4);
                    Header.fmtSize = br.ReadUInt32();
                    Header.format = br.ReadUInt16();
                    Header.channels = br.ReadUInt16();
                    Header.sampleRate = br.ReadUInt32();
                    Header.bytePerSec = br.ReadUInt32();
                    Header.blockSize = br.ReadUInt16();
                    Header.bit = br.ReadUInt16();
                    Header.dataID = br.ReadBytes(4);
                    Header.dataSize = br.ReadUInt32();

                    double rate = Header.sampleRate;
                    uint dimension = Header.channels;
                    uint samples = Header.dataSize / Header.blockSize;

                    signal = new Signal(filepath, rate, dimension, 2, samples, Signal.Type.FLOAT);

                    if (Header.bit == 16)
                    {
                        for (int i = 0; i < samples * dimension; i++)
                        {
                            signal.data[i] = ((float)br.ReadInt16()) / 32768.0f;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < samples * dimension; i++)
                        {
                            signal.data[i] = br.ReadSingle();
                        }
                    }

                    signal.minmax();
                    signal.isAudio = true;
                    signal.loaded = true;
                }
                catch (Exception e)
                {
                    MessageTools.Error(e.ToString());
                }
                if (br != null)
                {
                    br.Close();
                }
                if (fs != null)
                {
                    fs.Close();
                }
            }

            return signal;
        }

        #endregion

        #region EXPORT

        public AnnoList ExportToAnno()
        {
            AnnoList annoList = new AnnoList();
            annoList.Scheme = new AnnoScheme();
            annoList.Scheme.Type = AnnoScheme.TYPE.CONTINUOUS;
            annoList.Scheme.MinScore = min[ShowDim];
            annoList.Scheme.MaxScore = max[ShowDim];
            annoList.Scheme.SampleRate = rate;

            AnnoListItem annoListItem;
            double dur = 1 / rate;
            for (int i = 0; i < number; i++)
            {
                annoListItem = new AnnoListItem(i * dur, dur, data[i * dim + ShowDim].ToString(), "", annoList.Scheme.MaxOrForeColor);
                annoList.Add(annoListItem);
                annoList.Scheme.Name = Name;
            }

            return annoList;
        }

        #endregion
    }
}
