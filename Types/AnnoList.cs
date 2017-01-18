using System.IO;

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

        private string filePath = null;
        private string fileName = null;


        public bool LoadedFromDB { get; set; }

        public string Role { get; set; }

        public string FileType { get; set; }

        public string Subject { get; set; }

        public string Annotator { get; set; }

        public string AnnotatorFullName { get; set; }

        public string FilePath
        {
            get { return filePath; }
            set
            {
                filePath = value;
                string[] tmp = filePath.Split('\\');
                fileName = tmp[tmp.Length - 1];
                //name = fileName.Split('.')[0];
            }
        }

        public string FileName
        {
            get { return fileName; }
        }

        public string Directory
        {
            get { return Path.GetDirectoryName(filePath); }
        }

        public string Name { get; set; }


        public AnnoScheme Scheme { get; set; }
        public AnnoList()
            : base()
        {
        }

    }
}