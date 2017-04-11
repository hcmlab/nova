﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace ssi
{
    public partial class AnnoList : IAnnoList
    {
        private enum Type
        {
            EMPTY,
            MAP,
            TUPLE,
            STRING,
            ID,
        }

        public int ID { get; }

        public string Name
        {
            get
            {
                if (Source.HasFile())
                {
                    return Source.File.Name;
                }
                else if (Source.HasDatabase())
                {
                    return Scheme.Name;
                }
                else
                {
                    return "*";
                }
            }
        }

        private static int seed = Environment.TickCount;
        private static Random rnd = new Random(seed);
        public int GetRandomInt(int start, int finish)
        {
            return rnd.Next(start, finish);
        }

        public AnnoSource Source { get; set; }

        public AnnoScheme Scheme { get; set; }

        public AnnoMeta Meta { get; set; }

        public bool Show { get; set; }

        public AnnoList()
            : base()
        {
            Source = new AnnoSource();
            Scheme = new AnnoScheme();
            Meta = new AnnoMeta();
            ID = GetRandomInt(1, Int32.MaxValue);
            Show = true;
        }

        public bool Save(List<DatabaseMediaInfo> loadedMedia = null)
        {
            bool saved = false;

            if (Source.HasFile())
            {
                if (saveToFile(Source.File.Path))
                {
                    saved = true;
                }
            }

            if (Source.HasDatabase())
            {
                if (DatabaseHandler.StoreToDatabase(this, loadedMedia) != null)
                {
                    saved = true;
                }
            }

            if (!saved)
            {
                string path = FileTools.SaveFileDialog(Scheme.Name, ".annotation", "Annotation(*.annotation)|*.annotation", "");
                if (path != null)
                {
                    Source.File.Path = path;
                    Save();
                }
            }

            if (saved)
            {
                HasChanged = false;
            }

            return saved;
        }


        public string DefaultName(string seperator = ".", bool startWithSeperator = false, bool showFullName = false)
        {
            string name = "";

            if (startWithSeperator)
            {
                name = seperator + Scheme.Name;
            }
            else
            {
                name = Scheme.Name;
            }

            if (Meta.Role != "")
            {
                name += seperator + Meta.Role;
            }

            if (Meta.AnnotatorFullName != "" && showFullName)
            {
                name += seperator + Meta.AnnotatorFullName;
            }
            else if (Meta.Annotator != "")
            {
                name += seperator + Meta.Annotator;
            }

            return name;
        }

    }
}