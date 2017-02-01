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

            if(Source.HasDatabase())
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

            return saved;
        }

    }
}