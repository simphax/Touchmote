using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Output.Handlers
{
    interface IStickHandler : IOutputHandler
    {

        bool setValue(string key, double value);

    }
}
