using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiTUIO.Output.Handlers.Touch;
using WiiTUIO.Output.Handlers.Xinput;

namespace WiiTUIO.Output.Handlers
{
    public class HandlerFactory
    {
        private Dictionary<long, List<IOutputHandler>> outputHandlers;

        public HandlerFactory()
        {
            outputHandlers = new Dictionary<long, List<IOutputHandler>>();
        }

        private List<IOutputHandler> createOutputHandlers(long id)
        {
            List<IOutputHandler> all = new List<IOutputHandler>();
            IOutputHandler keyboardHandler = VmultiDevice.Current.isAvailable() ? (IOutputHandler)(VmultiKeyboardHandler.Default) : (IOutputHandler)(new KeyboardHandler());
            all.Add(keyboardHandler);
            all.Add(new MouseHandler());
            all.Add(new XinputHandler(id));
            all.Add(new TouchHandler(TouchOutputFactory.getCurrentProviderHandler(),id));
            return all;
        }

        public List<IOutputHandler> getOutputHandlers(long id)
        {
            List<IOutputHandler> handlerList;
            if (outputHandlers.TryGetValue(id, out handlerList))
            {
                return handlerList;
            }
            else
            {
                handlerList = this.createOutputHandlers(id);
                return handlerList;
            }
        }

    }
}
