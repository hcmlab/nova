using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ssi.DatabaseNovaServerWindow;

namespace ssi.Database
{
    public class NovaServerHandler
    {

        public class ServerInputOutput
        {
            public string ID { get; set; }
            public string IO { get; set; }
            public string Type { get; set; }
            public string SubType { get; set; }
            public string SubSubType { get; set; }

            public string DefaultName { get; set; }
        }

        public class Explainer
        {

            public string Path { get; set; }
            public string Name { get; set; }
            public List<string> mlFrameworks{ get; set; }
            public string Description { get; set; }
            public string Category { get; set; }
            public bool isTrained { get; set; }
            public List<ServerInputOutput> Inputs { get; set; }
            public string ModelCreate { get; set; }
            public string ModelScriptPath { get; set; }
            public string Option { get; set; }
        }

    }
}
