using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Output.Handlers
{
    public interface IButtonHandler
    {

        bool handleButtonDown(long id, string key);
        bool handleButtonUp(long id, string key);

    }
}
