using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Output.Handlers
{
    interface IStickHandler
    {

        bool updateStateX(long id, double x);
        bool updateStateY(long id, double y);

    }
}
