using System;
using System.IO;
using System.Xml;

namespace ssi
{
    public class Signal
    {
        private struct WavHeader
        {
            public byte[] riffID;
            public uint size;
            public byte[] wavID;
            public byte[] fmtID;
            public uint fmtSize;
            public ushort format;
            public ushort channels;
            public uint sampleRate;
            public uint bytePerSec;
            public ushort blockSize;
            public ushort bit;
            public byte[] dataID;
            public uint dataSize;
        }

        public bool loaded = false;
        private String name = null;
        private String filename = null;
        private String filepath = null;
        private bool isAudio = false;
        private uint showDim = 0;

        public uint ShowDim
        {
            get { return showDim; }
            set
            {
                if (value < dim)
                {
                    showDim = value;
                }
                else
                {
                    ViewTools.ShowErrorMessage("Selected dimension '" + value + "' exceeds dimension of signal '" + dim + "'");
                }
            }
        }

        public bool IsAudio
        {
            get { return isAudio; }
        }

        public String Name
        {
            get { return name; }
            set { name = value; }
        }

        public String Filename
        {
            get { return filename; }
            set { filename = value; }
        }

        public String Filepath
        {
            get { return filepath; }
            set { filepath = value; }
        }

        public String Folderpath
        {
            get { return filepath.Substring(0, filepath.LastIndexOf("\\") + 1); }
        }

        public double rate;
        public double rate_real;
        public UInt32 dim;
        public UInt32 bytes;
        public ViewTools.SSI_TYPE type;
        public double time;
        public UInt32 number;
        public float[] data;
        public float[] min;
        public float minmin;
        public float[] max;
        public float maxmax;
        public string meta_name;
        public int meta_num;
        public string meta_type;

        protected Signal()
        {
            rate = 0;
            dim = 0;
            bytes = 0;
            time = 0;
            number = 0;
            data = null;
            min = null;
            max = null;
            loaded = false;
            isAudio = false;
            type = ViewTools.SSI_TYPE.UNDEF;
            meta_name = "";
            meta_num = 0;
            meta_type = "";
        }

        protected Signal(string filepath, double rate, uint dim, uint bytes, uint number, ViewTools.SSI_TYPE type, string meta_name = "", int meta_num = 0, string meta_type = "")
            : base()
        {
            this.filepath = filepath;
            string[] tmp = filepath.Split('\\');
            this.filename = tmp[tmp.Length - 1];
            this.name = this.filename.Split('.')[0];
            this.rate_real = this.rate = rate;
            this.dim = dim;
            this.bytes = bytes;
            this.number = number;
            this.type = type;

            this.meta_name = meta_name;
            this.meta_num = meta_num;
            this.meta_type = meta_type;

            if (number > 0)
            {
                data = new float[number * dim];
            }
        }

        //shrink konsturktor
        public Signal(Signal s, UInt32 width, double fromInSec, double toInSec)
        {
            this.dim = s.dim;
            this.bytes = s.bytes;
            this.time = s.time;
            this.filename = s.filename;
            this.filepath = s.filepath;
            this.name = s.name;
            this.type = s.type;
            this.isAudio = s.isAudio;

            this.meta_name = "";
            this.meta_num = 0;
            this.meta_type = "";

            uint fromInSamples = (uint)Math.Round(fromInSec * s.rate);
            uint toInSamples = (uint)Math.Round(toInSec * s.rate);
            uint lenInSamples = toInSamples - fromInSamples;

            uint fromAvailableInSamples = Math.Min(s.number, fromInSamples);
            uint toAvailableInSamples = Math.Min(s.number, toInSamples);
            uint lenAvailableInSamples = toAvailableInSamples - fromAvailableInSamples;

            if (lenAvailableInSamples == 0)
            {
                this.rate = 0;
                this.number = 0;
                this.data = null;
            }

            double step = (double)lenInSamples / (double)width;

            if (step > 1.0)
            {
                //Resample(s.number, from, to, this.number, this.dim, s.data, this.data);

                this.number = width;
                this.data = new float[this.number * this.dim];
                this.rate = s.rate / step;
                this.rate_real = s.rate_real;

                double step_sum = 0;
                uint pos_to = 0;
                uint pos_from = 0;
                for (uint i = 0; i < this.number; i++)
                {
                    pos_from = fromInSamples + ((uint)step_sum);
                    if (pos_from >= s.number)
                    {
                        for (; i < this.number; i++)
                        {
                            for (uint j = 0; j < this.dim; j++)
                            {
                                this.data[pos_to++] = float.NaN;
                            }
                        }
                        break;
                    }

                    pos_from *= this.dim;
                    for (uint j = 0; j < this.dim; j++)
                    {
                        this.data[pos_to++] = s.data[pos_from + j];
                    }
                    step_sum += step;
                }
            }
            else
            {
                this.rate = s.rate;
                this.rate_real = s.rate_real;
                this.number = lenInSamples;

                data = new float[lenInSamples * dim];
                int index = 0;
                uint offset = fromInSamples * dim;
                for (uint n = 0; n < lenInSamples; n++)
                {
                    if (offset + index >= s.number * dim)
                    {
                        for (; n < lenInSamples; n++)
                        {
                            for (uint j = 0; j < this.dim; j++)
                            {
                                this.data[index++] = float.NaN;
                            }
                        }
                        break;
                    }
                    for (uint d = 0; d < dim; d++)
                    {
                        data[index] = s.data[offset + index];
                        index++;
                    }
                }
            }

            minmax();
            loaded = true;
        }

        public float Value(double atSecond)
        {
            uint index = Math.Min(number - 1, (uint)(atSecond * rate));
            return data[index * dim + showDim];
        }

        public static bool SelectDataType(string filename, ref ViewTools.SSI_TYPE type, ref double rate)
        {
            SelectionBox box = new SelectionBox("Please select data type and sample rate (Hz) for:\r\n" + filename + "'", ViewTools.SSI_TYPE_NAME, "0");
            box.ShowDialog();
            box.Close();

            if (box.DialogResult == true)
            {
                type = (ViewTools.SSI_TYPE)box.ComboBoxResult();
                try
                {
                    rate = double.Parse(box.TextFieldResult());
                    return true;
                }
                catch (Exception e)
                {
                    ViewTools.ShowErrorMessage(e.ToString());
                }
            }

            return false;
        }

        public static Signal LoadARFFFile(string filepath)
        {
            Signal signal = null;

            try
            {
                ViewTools.SSI_TYPE type = ViewTools.SSI_TYPE.FLOAT;
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
                uint bytes = ViewTools.SSI_BYTES[(int)type];

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
                ViewTools.ShowErrorMessage(e.ToString());
                return null;
            }

            return signal;
        }

        public static Signal LoadCSVFile(string filepath)
        {
            Signal signal = null;

            try
            {
                ViewTools.SSI_TYPE type = ViewTools.SSI_TYPE.UNDEF;
                uint dim = 0;
                double rate = 0;

                if (Signal.SelectDataType(filepath, ref type, ref rate))
                {
                    string[] lines = File.ReadAllLines(filepath);

                    uint number = (uint)lines.Length;
                    uint bytes = ViewTools.SSI_BYTES[(int)type];

                    if (type != ViewTools.SSI_TYPE.UNDEF
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
                ViewTools.ShowErrorMessage(e.ToString());
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
                signal.filepath = filepath;
                string[] tmp = filepath.Split('\\');
                signal.filename = tmp[tmp.Length - 1];
                signal.name = signal.filename.Split('.')[0];

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
                    for (uint i = 0; i < ViewTools.SSI_TYPE_NAME.Length; i++)
                    {
                        if (type_s == ViewTools.SSI_TYPE_NAME[i])
                        {
                            signal.type = (ViewTools.SSI_TYPE)i;
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
                    ViewTools.ShowErrorMessage("could not read stream file '" + filepath + "'");
                    return null;
                }

                // close file
                fs.Close();

                signal.minmax();
                signal.loaded = true;
            }
            catch (Exception e)
            {
                ViewTools.ShowErrorMessage(e.ToString());
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
                case ViewTools.SSI_TYPE.SHORT:
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

                case ViewTools.SSI_TYPE.USHORT:
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

                case ViewTools.SSI_TYPE.INT:
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

                case ViewTools.SSI_TYPE.UINT:
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

                case ViewTools.SSI_TYPE.FLOAT:
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

                case ViewTools.SSI_TYPE.DOUBLE:
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
                case ViewTools.SSI_TYPE.SHORT:
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
                            ViewTools.ShowErrorMessage("Invalid number of bytes (" + signal.bytes + ") for data type 'short'");
                            return false;
                        }
                    }
                    break;

                case ViewTools.SSI_TYPE.USHORT:
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
                            ViewTools.ShowErrorMessage("Invalid number of bytes (" + signal.bytes + ") for data type 'short'");
                            return false;
                        }
                    }
                    break;

                case ViewTools.SSI_TYPE.INT:
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
                            ViewTools.ShowErrorMessage("Invalid number of bytes (" + signal.bytes + ") for data type 'short'");
                            return false;
                        }
                    }
                    break;

                case ViewTools.SSI_TYPE.UINT:
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
                            ViewTools.ShowErrorMessage("Invalid number of bytes (" + signal.bytes + ") for data type 'short'");
                            return false;
                        }
                    }
                    break;

                case ViewTools.SSI_TYPE.FLOAT:
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
                            ViewTools.ShowErrorMessage("Invalid number of bytes (" + signal.bytes + ") for data type 'float'");
                            return false;
                        }
                    }
                    break;

                case ViewTools.SSI_TYPE.DOUBLE:
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
                            ViewTools.ShowErrorMessage("Invalid number of bytes (" + signal.bytes + ") for data type 'float'");
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

                    signal = new Signal(filepath, rate, dimension, 2, samples, ViewTools.SSI_TYPE.FLOAT);

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
                    ViewTools.ShowErrorMessage(e.ToString());
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

        public void minmax()
        {
            min = new float[dim];
            max = new float[dim];

            if (data.Length == 0)
            {
                return;
            }

            minmin = data[0];
            maxmax = data[0];

            for (int i = 0; i < dim; i++)
            {
                min[i] = data[i];
                max[i] = data[i];
                minmin = Math.Min(minmin, min[i]);
                maxmax = Math.Max(maxmax, min[i]);
            }

            uint index = dim;
            for (int i = 1; i < number; i++)
            {
                for (int j = 0; j < dim; j++)
                {
                    float value = data[index++];
                    if (value < min[j])
                    {
                        min[j] = value;
                        minmin = Math.Min(minmin, value);
                    }
                    else if (value > max[j])
                    {
                        max[j] = value;
                        maxmax = Math.Max(maxmax, value);
                    }
                }
            }
        }
    }
}