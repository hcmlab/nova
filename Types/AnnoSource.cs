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
            public static ObjectId ZERO = new ObjectId();

            public ObjectId OID { get; set; }
            public override string ToString()
            {
                return OID.ToString();
            }

            public DatabaseSource ()
            {
                OID = ZERO;
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
            return Database.OID.CompareTo(DatabaseSource.ZERO) != 0;
        }



    }
}
