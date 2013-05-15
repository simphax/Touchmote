using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace VJoylib
{
    class VJoy
    {
        public enum POVType
        {
            Up = 0,
            Right = 1,
            Down = 2,
            Left = 3,
            Nil = 4
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
        public struct JoystickState
        {
            public byte ReportId;
            public short XAxis;
            public short YAxis;
            public short ZAxis;
            public short XRotation;
            public short YRotation;
            public short ZRotation;
            public short Slider;
            public short Dial;
            public ushort POV;
            public uint Buttons;
        };

        [DllImport("VJoy.dll", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VJoy_Initialize(StringBuilder name, StringBuilder serial);

        [DllImport("VJoy.dll", CallingConvention = CallingConvention.StdCall)]
        private static extern void VJoy_Shutdown();

        [DllImport("VJoy.dll", CallingConvention = CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool VJoy_UpdateJoyState(int id, ref JoystickState joyState);

        private JoystickState[] m_joyState;

        public VJoy()
        {
        }

        public bool Initialize()
        {
            m_joyState = new JoystickState[2];

            m_joyState[0] = new JoystickState();
            m_joyState[1] = new JoystickState();

            StringBuilder Name = new StringBuilder("");
            StringBuilder Serial = new StringBuilder("");

            return VJoy_Initialize(Name, Serial);
        }

        public void Shutdown()
        {
            VJoy_Shutdown();
        }

        public bool Update(int id)
        {
            return VJoy_UpdateJoyState(id, ref m_joyState[id]);
        }

        public void Reset()
        {
            m_joyState[0].ReportId = 0;
            m_joyState[0].XAxis = 0;
            m_joyState[0].YAxis = 0;
            m_joyState[0].ZAxis = 0;
            m_joyState[0].XRotation = 0;
            m_joyState[0].YRotation = 0;
            m_joyState[0].ZRotation = 0;
            m_joyState[0].Slider = 0;
            m_joyState[0].Dial = 0;
            m_joyState[0].POV = ((int)POVType.Nil << 12) | ((int)POVType.Nil << 8) | ((int)POVType.Nil << 4) | (int)POVType.Nil;
            m_joyState[0].Buttons = 0;

            m_joyState[1].ReportId = 0;
            m_joyState[1].XAxis = 0;
            m_joyState[1].YAxis = 0;
            m_joyState[1].ZAxis = 0;
            m_joyState[1].XRotation = 0;
            m_joyState[1].YRotation = 0;
            m_joyState[1].ZRotation = 0;
            m_joyState[1].Slider = 0;
            m_joyState[1].Dial = 0;
            m_joyState[1].POV = ((int)POVType.Nil << 12) | ((int)POVType.Nil << 8) | ((int)POVType.Nil << 4) | (int)POVType.Nil;
            m_joyState[1].Buttons = 0;
        }

        public short GetXAxis(int index)
        {
            return m_joyState[index].XAxis;
        }

        public void SetXAxis(int index, short value)
        {
            m_joyState[index].XAxis = value;
        }

        public short GetYAxis(int index)
        {
            return m_joyState[index].YAxis;
        }

        public void SetYAxis(int index, short value)
        {
            m_joyState[index].YAxis = value;
        }

        public short GetZAxis(int index)
        {
            return m_joyState[index].ZAxis;
        }

        public void SetZAxis(int index, short value)
        {
            m_joyState[index].ZAxis = value;
        }

        public short GetXRotation(int index)
        {
            return m_joyState[index].XRotation;
        }

        public void SetXRotation(int index, short value)
        {
            m_joyState[index].XRotation = value;
        }

        public short GetYRotation(int index)
        {
            return m_joyState[index].YRotation;
        }

        public void SetYRotation(int index, short value)
        {
            m_joyState[index].YRotation = value;
        }

        public short GetZRotation(int index)
        {
            return m_joyState[index].ZRotation;
        }

        public void SetZRotation(int index, short value)
        {
            m_joyState[index].ZRotation = value;
        }

        public short GetSlider(int index)
        {
            return m_joyState[index].Slider;
        }

        public void SetSlider(int index, short value)
        {
            m_joyState[index].Slider = value;
        }

        public short GetDial(int index)
        {
            return m_joyState[index].Dial;
        }

        public void SetDial(int index, short value)
        {
            m_joyState[index].Dial = value;
        }

        public void SetPOV(int index, int pov, POVType value)
        {
            m_joyState[index].POV &= (ushort)~((int)0xf << ((3 - pov) * 4));
            m_joyState[index].POV |= (ushort)((int)value << ((3 - pov) * 4));
        }

        public POVType GetPOV(int index, int pov)
        {
            return (POVType)((m_joyState[index].POV >> ((3 - pov) * 4)) & 0xf);
        }

        public void SetButton(int index, int button, bool value)
        {
            if (value)
                m_joyState[index].Buttons |= (uint)(1 << button);
            else
                m_joyState[index].Buttons &= (uint)~(1 << button);
        }

        public bool GetButton(int index, int button)
        {
            return ((m_joyState[index].Buttons & (1 << button)) == 1);
        }
    }
}
