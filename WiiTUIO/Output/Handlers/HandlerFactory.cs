using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WiiTUIO.Output.Handlers
{
    public class HandlerFactory
    {

        private List<IButtonHandler> allButtonHandlers;

        public HandlerFactory()
        {
            allButtonHandlers = this.createAllButtonHandlers();
        }

        private List<IButtonHandler> createAllButtonHandlers()
        {
            List<IButtonHandler> all = new List<IButtonHandler>();
            all.Add(new KeyboardHandler());
            return all;
        }

        public List<IButtonHandler> getAllButtonHandlers()
        {
            return allButtonHandlers;
        }

    }
}
