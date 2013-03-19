using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WiiTUIO.Input;

namespace WiiTUIO.Properties
{
    class Settings
    {

        public event PropertyChangedEventHandler PropertyChanged;

        private string _input = "multipointer";
        public string input
        {
            get { return _input; }
            set
            {
                _input = value;
                OnPropertyChanged("input");
            }
        }

        private string _output = "touch";
        public string output
        {
            get { return _output; }
            set
            {
                _output = value;
                OnPropertyChanged("output");
            }
        }

        private bool _connectOnStart = false;
        public bool connectOnStart
        {
            get { return _connectOnStart; }
            set
            {
                _connectOnStart = value;
                OnPropertyChanged("connectOnStart");
            }
        }

        private bool _pairedOnce = false;
        public bool pairedOnce
        {
            get { return _pairedOnce; }
            set
            {
                _pairedOnce = value;
                OnPropertyChanged("pairedOnce");
            }
        }

        private bool _completelyDisconnect = true;
        public bool completelyDisconnect
        {
            get { return _completelyDisconnect; }
            set
            {
                _completelyDisconnect = value;
                OnPropertyChanged("completelyDisconnect");
            }
        }

        private int _autoDisconnectTimeout = 300000;
        public int autoDisconnectTimeout
        {
            get { return _autoDisconnectTimeout; }
            set
            {
                _autoDisconnectTimeout = value;
                OnPropertyChanged("autoDisconnectTimeout");
            }
        }

        private string _tuio_IP = "127.0.0.1";
        public string tuio_IP
        {
            get { return _tuio_IP; }
            set
            {
                _tuio_IP = value;
                OnPropertyChanged("tuio_IP");
            }
        }

        private int _tuio_port = 3333;
        public int tuio_port
        {
            get { return _tuio_port; }
            set
            {
                _tuio_port = value;
                OnPropertyChanged("tuio_port");
            }
        }

        private bool _pointer_moveCursor = true;
        public bool pointer_moveCursor
        {
            get { return _pointer_moveCursor; }
            set
            {
                _pointer_moveCursor = value;
                OnPropertyChanged("pointer_moveCursor");
            }
        }

        private string _pointer_sensorBarPos = "center";
        public string pointer_sensorBarPos
        {
            get { return _pointer_sensorBarPos; }
            set
            {
                _pointer_sensorBarPos = value;
                OnPropertyChanged("pointer_sensorBarPos");
            }
        }

        private int _pointer_smoothingSize = 3;
        public int pointer_smoothingSize
        {
            get { return _pointer_smoothingSize; }
            set
            {
                _pointer_smoothingSize = value;
                OnPropertyChanged("pointer_smoothingSize");
            }
        }

        private int _touch_touchTapThreshold = 30;
        public int touch_touchTapThreshold
        {
            get { return _touch_touchTapThreshold; }
            set
            {
                _touch_touchTapThreshold = value;
                OnPropertyChanged("touch_touchTapThreshold");
            }
        }

        private int _touch_edgeGestureHelperMargins = 30;
        public int touch_edgeGestureHelperMargins
        {
            get { return _touch_edgeGestureHelperMargins; }
            set
            {
                _touch_edgeGestureHelperMargins = value;
                OnPropertyChanged("touch_edgeGestureHelperMargins");
            }
        }

        private int _touch_edgeGestureHelperRelease = 60;
        public int touch_edgeGestureHelperRelease
        {
            get { return _touch_edgeGestureHelperRelease; }
            set
            {
                _touch_edgeGestureHelperRelease = value;
                OnPropertyChanged("touch_edgeGestureHelperRelease");
            }
        }
      
        private static string SETTINGS_FILENAME = "settings.json";

        private static Settings defaultInstance;

        public static Settings Default
        {
            get
            {
                if (defaultInstance == null)
                {
                    defaultInstance = Load();
                }
                return defaultInstance;
            }
        }

        private static Settings Load()
        {
            Settings result;
            try
            {
                result = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(SETTINGS_FILENAME));
            }
            catch //If anything goes wrong, just load default values
            {
                result = new Settings();
            }

            return result;
        }

        public void Save()
        {
            File.WriteAllText(SETTINGS_FILENAME, JsonConvert.SerializeObject(Default,Formatting.Indented));
        }

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
    }

}
