using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssi.Interfaces
{
    interface ICommand
    {
        PolygonLabel Do();
        PolygonLabel Undo();
    }
}
