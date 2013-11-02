using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiTUIO.Output.Handlers.Xinput;

namespace WiiTUIO.Output.Handlers
{
    public class HandlerFactory
    {
        private Dictionary<long, List<IButtonHandler>> buttonHandlers;

        public HandlerFactory()
        {
            buttonHandlers = new Dictionary<long, List<IButtonHandler>>();
        }

        private List<IButtonHandler> createButtonHandlers(long id)
        {
            List<IButtonHandler> all = new List<IButtonHandler>();
            all.Add(new KeyboardHandler());
            all.Add(new XinputHandler(id));
            return all;
        }

        public List<IButtonHandler> getButtonHandlers(long id)
        {
            List<IButtonHandler> handlerList;
            if (buttonHandlers.TryGetValue(id, out handlerList))
            {
                return handlerList;
            }
            else
            {
                handlerList = this.createButtonHandlers(id);
                return handlerList;
            }
        }

    }
}
