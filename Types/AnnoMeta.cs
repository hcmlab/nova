using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssi
{
    public class AnnoMeta
    {
        public string Role { get; set; }

        public string Subject { get; set; }

        public string Annotator { get; set; }

        public string AnnotatorFullName { get; set; }

        public Collection<AnnoTrigger> Trigger { get; set; }
        public Collection<Pipeline> Pipeline { get; set; }

        public AnnoMeta()
        {
            Role = "";
            Subject = "";
            Annotator = "";
            AnnotatorFullName = "";
            Trigger = new Collection<AnnoTrigger>();
            Pipeline = new Collection<Pipeline>();
        }
    }
}
