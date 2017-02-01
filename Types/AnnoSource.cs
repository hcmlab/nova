using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using MongoDB.Bson;
using MongoDB.Driver;

namespace ssi
{
    public class AnnoSource
    {
        public class FileSource
        {
            public enum TYPE
            {
                ASCII,
                BINARY,
            }
            public TYPE Type { get; set; }

            public string Path { get; set; }
            public string Directory
            {
                get
                {
                    return Path != null ? System.IO.Path.GetDirectoryName(Path) : "";
                }
            }
            public string Name
            {
                get
                {
                    return Path != null ? System.IO.Path.GetFileNameWithoutExtension(Path) : "";
                }
            }
            public override string ToString()
            {
                return Path;
            }

            public FileSource()
            {
                Path = "";
                Type = TYPE.ASCII;
            }
        }

        public class DatabaseSource
        {
            public string Server { get; set; }
            public string Database { get; set; }
            public string Collection { get; set; }
            public ObjectId OID { get; set; }
            public bool FromString(string str)
            {
                string[] parts = str.Split('|');
                if (parts.Length == 4 )
                {
                    Server = parts[0];
                    Database = parts[1];
                    Collection = parts[2];
                    OID = new ObjectId(parts[3]);
                    return true;
                }
                return false;
            }
            public string Path
            {
                get { return Server + "|" + Database + "|" + Collection + "|" + OID.ToString(); }
            }

            public DatabaseSource ()
            {
                Server = "";
                Database = "";
                Collection = "Annotations";
                OID = new ObjectId();
            }
        }

        public FileSource File { get; set; }
        public DatabaseSource Database { get; set; }

        public AnnoSource()
        {
            File = new FileSource();
            Database = new DatabaseSource();
        }
       
        public bool HasFile()
        {
            return File.Path != "";
        }
 
        public bool HasDatabase()
        {
            return Database.Server != "";
        }
    }
}
