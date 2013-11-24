using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiTUIO.Provider;

namespace WiiTUIO.Output.Handlers
{
    interface ICursorHandler : IOutputHandler
    {

        bool setPosition(string key, CursorPos cursorPos);

    }
}
