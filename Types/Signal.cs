using System;
using System.Collections.Generic;
using System.IO;

namespace ssi
{
    public partial class Signal
    {
        public enum Type : byte
        {
            UNDEF = 0,
            CHAR = 1,
            UCHAR = 2,
            SHORT = 3,
            USHORT = 4,
            INT = 5,
            UINT = 6,
            LONG = 7,
            ULONG = 8,
            FLOAT = 9,
            DOUBLE = 10,
            LDOUBLE = 11,
            STRUCT = 12,
            IMAGE = 13,
            BOOL = 14
        }

        public static readonly uint[] TypeSize =
        {
            0,
            sizeof(char),
            sizeof(char),
            sizeof(short),
            sizeof(ushort),
            sizeof (int),
            sizeof(uint),
            sizeof(long),
            sizeof(ulong),
            sizeof(float),
            sizeof(double),
            sizeof(double),
            0,
            0,
            sizeof(bool)
        };

        public static readonly string[] TypeName =
        {
            "UNDEF",
            "CHAR",
            "UCHAR",
            "SHORT",
            "USHORT",
            "INT",
            "UINT",
            "LONG",
            "ULONG",
            "FLOAT",
            "DOUBLE",
            "LDOUBLE",
            "STRUCT",
            "IMAGE",
            "BOOL"
        };

        //private struct WavHeader
        //{
        //    public byte[] riffID;
        //    public uint size;
        //    public byte[] wavID;
        //    public byte[] fmtID;
        //    public uint fmtSize;
        //    public ushort format;
        //    public ushort channels;
        //    public uint sampleRate;
        //    public uint bytePerSec;
        //    public ushort blockSize;
        //    public ushort bit;
        //    public byte[] dataID;
        //    public uint dataSize;
        //}

        public bool loaded = false;
        private string name = null;
        private string fileName = null;
        private string filePath = null;
        private bool isAudio = false;
        private int showDim = 0;

        public int ShowDim
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
                    MessageTools.Error("Selected dimension '" + value + "' exceeds dimension of signal '" + dim + "'");
                }
            }
        }

        public IMedia Media { get; set; }

        public bool IsAudio
        {
            get { return isAudio; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        public string FilePath
        {
            get { return filePath; }
            set { filePath = value; }
        }

        public string Directory
        {
            get { return Path.GetDirectoryName(filePath); }
        }

        public double rate;
        public double rate_real;
        public uint dim;
        public uint bytes;
        public Type type;
        public double time;
        public uint number;
        public float[] data;
        public float[] min;
        public float minmin;
        public float[] max;
        public float maxmax;

        public Dictionary<string, string> Meta { get; set; }

        public Signal()
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
            type = Type.UNDEF;
            Meta = new Dictionary<string, string>();
        }

        protected Signal(string filepath, double rate, uint dim, uint bytes, uint number, Type type)
            : base()
        {
            this.filePath = filepath;
            string[] tmp = filepath.Split('\\');
            this.fileName = tmp[tmp.Length - 1];
            this.name = this.fileName.Split('.')[0];
            this.rate_real = this.rate = rate;
            this.dim = dim;
            this.bytes = bytes;
            this.number = number;
            this.type = type;

            if (number > 0)
            {
                data = new float[number * dim];
            }
        }

        //shrink konsturktor
        public Signal(Signal s, uint width, double fromInSec, double toInSec)
        {
            this.dim = s.dim;
            this.bytes = s.bytes;
            this.time = s.time;
            this.fileName = s.fileName;
            this.filePath = s.filePath;
            this.name = s.name;
            this.type = s.type;
            this.isAudio = s.isAudio;

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

        public static bool SelectDataType(string filename, ref Signal.Type type, ref double rate)
        {
            SelectionBox box = new SelectionBox("Please select data type and sample rate (Hz) for:\r\n" + filename + "'", Signal.TypeName, "1", 9);
            box.ShowDialog();
            box.Close();

            if (box.DialogResult == true)
            {
                type = (Signal.Type)box.ComboBoxResult();
                try
                {
                    rate = double.Parse(box.TextFieldResult());
                    return true;
                }
                catch (Exception e)
                {
                    MessageTools.Error(e.ToString());
                }
            }

            return false;
        }

        private void minmax()
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