using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Output.Handlers
{
    public interface IOutputHandler
    {
        bool connect();
        bool disconnect();

        //Called when the keymap changed. So we should put all buttons to UP position
        bool reset();

        bool startUpdate();
        bool endUpdate();
    }
}
