using System;
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
                if (Source.HasFile)
                {
                    return Source.File.Name;
                }
                else if (Source.HasDatabase)
                {
                    return Source.Database.OID.ToString();
                }
                else
                {
                    return "*";
                }                
            }
        }

        public string Path
        {
            get
            {
                if (Source.HasFile)
                {
                    return Source.File.Path;
                }
                else if (Source.HasDatabase)
                {
                    return Source.Database.OID.ToString();
                }
                else
                {
                    return null;
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

        public AnnoList()
            : base()
        {
            Source = new AnnoSource();
            Scheme = new AnnoScheme();
            Meta = new AnnoMeta();
            ID = GetRandomInt(1, Int32.MaxValue);
        }

        public bool Save(List<DatabaseStream> loadedStreams = null, bool force = false)
        {
            bool saved = false;

            if (!force && !HasChanged)
            {
                return true;
            }

            if (Source.HasFile || Source.StoreToFile)
            {
               
                if (!Source.HasFile)
                {
                  Source.File.Path = FileTools.SaveFileDialog(Scheme.Name, ".annotation", "Annotation(*.annotation)|*.annotation", ""); 
                }
              
                if (Source.HasFile)
                {
                    if (saveToFile(Source.File.Path))
                    {
                        saved = true;
                    }
                }
            }
            else if (Source.HasDatabase || Source.StoreToDatabase)
            {
                if (DatabaseHandler.SaveAnnoList(this, loadedStreams, force))
                {
                    saved = true;
                }
                if (!Source.HasDatabase)
                {
                    Source.Database.OID = DatabaseHandler.GetAnnotationId(Meta.Role, Scheme.Name, Meta.Annotator, Source.Database.Session);
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