using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssi.Types.Polygon
{
    public class LabelInformations
    {
        public enum TYPE
        {
            EDIT,
            CREATION,
            EXTRA_POINT,
            REMOVE,
            COPY
        }
        public TYPE Type { get; set; }

        public LabelInformations(TYPE type)
        {
            this.Type = type;
        }
    }
}
