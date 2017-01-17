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

        private bool loadedFromDB = false;
        private string name = null;
        private string filePath = null;
        private string fileName = null;
        private string role = null;
        private string subject = null;
        private AnnoScheme scheme;
        private string annotator = null;
        private string annotatorFullName = null;
        private string fileType = "ASCII";

        public bool FromDB
        {
            get { return loadedFromDB; }
            set { loadedFromDB = value; }
        }

        public string Role
        {
            get { return role; }
            set { role = value; }
        }

        public string FileType
        {
            get { return fileType; }
            set { fileType = value; }
        }

        public string Subject
        {
            get { return subject; }
            set { subject = value; }
        }

        public string FileName
        {
            get { return fileName; }
        }

        public string Annotator
        {
            get { return annotator; }
            set { annotator = value; }
        }

        public string AnnotatorFullName
        {
            get { return annotatorFullName; }
            set { annotatorFullName = value; }
        }

        public string FilePath
        {
            get { return filePath; }
            set
            {
                filePath = value;
                string[] tmp = filePath.Split('\\');
                fileName = tmp[tmp.Length - 1];
                name = fileName.Split('.')[0];
            }
        }

        public string Directory
        {
            get { return Path.GetDirectoryName(filePath); }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        public AnnoScheme Scheme
        {
            get { return scheme; }
            set { scheme = value; }
        }

        public AnnoList()
            : base()
        {
        }
    }
}