using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Output.Handlers
{
    public interface IButtonHandler : IOutputHandler
    {
        //Return true if it has been handled
        bool setButtonDown(string key);
        bool setButtonUp(string key);

    }
}
