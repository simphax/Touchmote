using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Output.Handlers
{
    interface IStickHandler : IOutputHandler
    {
        //Value is normally 0.0-1.0 , but can be altered with the "scaling" setting
        bool setValue(string key, double value);

    }
}
