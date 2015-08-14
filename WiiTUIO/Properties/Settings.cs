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

        private bool _pairOnStart = false;
        public bool pairOnStart
        {
            get { return _pairOnStart; }
            set
            {
                _pairOnStart = value;
                OnPropertyChanged("pairOnStart");
            }
        }

        private bool _connectOnStart = true;
        public bool connectOnStart
        {
            get { return _connectOnStart; }
            set
            {
                _connectOnStart = value;
                OnPropertyChanged("connectOnStart");
            }
        }

        private bool _minimizeOnStart = false;
        public bool minimizeOnStart
        {
            get { return _minimizeOnStart; }
            set
            {
                _minimizeOnStart = value;
                OnPropertyChanged("minimizeOnStart");
            }
        }

        private bool _minimizeToTray = false;
        public bool minimizeToTray
        {
            get { return _minimizeToTray; }
            set
            {
                _minimizeToTray = value;
                OnPropertyChanged("minimizeToTray");
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

        private string _primaryMonitor = "";
        public string primaryMonitor
        {
            get { return _primaryMonitor; }
            set
            {
                _primaryMonitor = value;
                OnPropertyChanged("primaryMonitor");
            }
        }

        private bool _completelyDisconnect = false;
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

        private double _defaultContinousScale = 1.0;
        public double defaultContinousScale
        {
            get { return _defaultContinousScale; }
            set
            {
                _defaultContinousScale = value;
                OnPropertyChanged("defaultContinousScale");
            }
        }

        private double _defaultContinousPressThreshold = 0.4;
        public double defaultContinousPressThreshold
        {
            get { return _defaultContinousPressThreshold; }
            set
            {
                _defaultContinousPressThreshold = value;
                OnPropertyChanged("defaultContinousPressThreshold");
            }
        }

        private double _defaultContinousDeadzone = 0.01;
        public double defaultContinousDeadzone
        {
            get { return _defaultContinousDeadzone; }
            set
            {
                _defaultContinousDeadzone = value;
                OnPropertyChanged("defaultContinousDeadzone");
            }
        }

        private bool _alternativeStickToCursorMapping = false;
        public bool alternativeStickToCursorMapping
        {
            get { return _alternativeStickToCursorMapping; }
            set
            {
                _alternativeStickToCursorMapping = value;
                OnPropertyChanged("alternativeStickToCursorMapping");
            }
        }

        private bool _disconnectWiimotesOnDolphin = false;
        public bool disconnectWiimotesOnDolphin
        {
            get { return _disconnectWiimotesOnDolphin; }
            set
            {
                _disconnectWiimotesOnDolphin = value;
                OnPropertyChanged("disconnectWiimotesOnDolphin");
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

        private string _keymaps_path = @"Keymaps\";
        public string keymaps_path
        {
            get { return _keymaps_path; }
            set
            {
                _keymaps_path = value;
                OnPropertyChanged("keymaps_path");
            }
        }

        private bool _noTopmost = false;
        public bool noTopmost
        {
            get { return _noTopmost; }
            set
            {
                _noTopmost = value;
                OnPropertyChanged("noTopmost");
            }
        }

        private string _keymaps_config = @"Keymaps.json";
        public string keymaps_config
        {
            get { return _keymaps_config; }
            set
            {
                _keymaps_config = value;
                OnPropertyChanged("keymaps_config");
            }
        }

        private double _pointer_sensorBarPosCompensation = 0.30;
        public double pointer_sensorBarPosCompensation
        {
            get { return _pointer_sensorBarPosCompensation; }
            set
            {
                _pointer_sensorBarPosCompensation = value;
                OnPropertyChanged("pointer_sensorBarPosCompensation");
            }
        }

        private double _pointer_cursorSize = 0.03;
        public double pointer_cursorSize
        {
            get { return _pointer_cursorSize; }
            set
            {
                _pointer_cursorSize = value;
                OnPropertyChanged("pointer_cursorSize");
            }
        }

        private double _pointer_marginsTopBottom = 0.8;
        public double pointer_marginsTopBottom
        {
            get { return _pointer_marginsTopBottom; }
            set
            {
                _pointer_marginsTopBottom = value;
                OnPropertyChanged("pointer_marginsTopBottom");
            }
        }

        private double _pointer_marginsLeftRight = 0.7;
        public double pointer_marginsLeftRight
        {
            get { return _pointer_marginsLeftRight; }
            set
            {
                _pointer_marginsLeftRight = value;
                OnPropertyChanged("pointer_marginsLeftRight");
            }
        }

        private bool _pointer_considerRotation = true;
        public bool pointer_considerRotation
        {
            get { return _pointer_considerRotation; }
            set
            {
                _pointer_considerRotation = value;
                OnPropertyChanged("pointer_considerRotation");
            }
        }

        private bool _pointer_customCursor = true;
        public bool pointer_customCursor
        {
            get { return _pointer_customCursor; }
            set
            {
                _pointer_customCursor = value;
                OnPropertyChanged("pointer_customCursor");
            }
        }

        private int _pointer_cursorStillHideTimeout = 3000;
        public int pointer_cursorStillHideTimeout
        {
            get { return _pointer_cursorStillHideTimeout; }
            set
            {
                _pointer_cursorStillHideTimeout = value;
                OnPropertyChanged("pointer_cursorStillHideTimeout");
            }
        }

        //Delta pixels before the cursor is considered still.
        private int _pointer_cursorStillThreshold = 10;
        public int pointer_cursorStillThreshold
        {
            get { return _pointer_cursorStillThreshold; }
            set
            {
                _pointer_cursorStillThreshold = value;
                OnPropertyChanged("pointer_cursorStillThreshold");
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

        private int _pointer_FPS = 100;
        public int pointer_FPS
        {
            get { return _pointer_FPS; }
            set
            {
                _pointer_FPS = value;
                OnPropertyChanged("pointer_FPS");
            }
        }

        private int _pointer_positionSmoothing = 3;
        public int pointer_positionSmoothing
        {
            get { return _pointer_positionSmoothing; }
            set
            {
                _pointer_positionSmoothing = value;
                OnPropertyChanged("pointer_positionSmoothing");
            }
        }

        private double _fpsmouse_deadzone = 0.03;
        public double fpsmouse_deadzone
        {
            get { return _fpsmouse_deadzone; }
            set
            {
                _fpsmouse_deadzone = value;
                OnPropertyChanged("fpsmouse_deadzone");
            }
        }

        private int _fpsmouse_speed = 30;
        public int fpsmouse_speed
        {
            get { return _fpsmouse_speed; }
            set
            {
                _fpsmouse_speed = value;
                OnPropertyChanged("fpsmouse_speed");
            }
        }

        private int _touch_touchTapThreshold = 40;
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
        
        private int _xinput_rumbleThreshold_big = 200;
        public int xinput_rumbleThreshold_big
        {
            get { return _xinput_rumbleThreshold_big; }
            set
            {
                _xinput_rumbleThreshold_big = value;
                OnPropertyChanged("xinput_rumbleThreshold_big");
            }
        }

        private int _xinput_rumbleThreshold_small = 200;
        public int xinput_rumbleThreshold_small
        {
            get { return _xinput_rumbleThreshold_small; }
            set
            {
                _xinput_rumbleThreshold_small = value;
                OnPropertyChanged("xinput_rumbleThreshold_small");
            }
        }
      
        private static string SETTINGS_FILENAME = System.AppDomain.CurrentDomain.BaseDirectory+"settings.json";

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
