using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using WiiTUIO.Properties;
using WiiTUIO.Provider;
using WindowsInput;
using WindowsInput.Native;

using System.IO;

namespace WiiTUIO.Output.Handlers
{
    public class MouseHandler : IButtonHandler, IStickHandler, ICursorHandler
    {
        private InputSimulator inputSimulator;

        private bool mouseLeftDown = false;
        private bool mouseRightDown = false;

        private CursorPositionHelper cursorPositionHelper;

        // TODO: BEGIN; add a user friendly control gear panel in the UI, setting -> control cobfiguration like the gear in Tilt-x, Tilt-y box, parameters min, max values and steps are given below

        private double mouseMainSensX = Settings.Default.flyfps_main_sensitivity; //range 0-10, step 0.01, default = 0.25, Sensitivity X and Y of mouse vector. When main_sensitivity_y=0 then main_sensitivity_y calculated from main_sensitivity by actual screen aspect ratio.  You can draw the mosue vector with pointer. This drawed vector move the mouse. This work independent from acceleration.
        private double mouseMainSensY = Settings.Default.flyfps_main_sensitivity_y; //range 0-10, step 0.01, default = 0 = auto from screen width/height aspect ratio, Sensitivity Y of mouse vector. When main_sensitivity_y=0 then main_sensitivity_y calculated from main_sensitivity.  You can draw the mosue vector with pointer. This drawed vector move the mouse.This work independent from acceleration.
        private double mouseMainAccX = Settings.Default.flyfps_main_acceleration; //range 0+, step 0.1, default = 40, multiply the mouse speed depending from the current mouse speed. Acceleration X and Y of mouse vector. When vector_acceleration_y=0 then vector_acceleration_y calculated from vector_acceleration by actual screen aspect ratio. You can draw the mosue vector with pointer. This drawed vector move the mouse. This work independent from sensitivity.
        private double mouseMainAccY = Settings.Default.flyfps_main_acceleration_y; //range 0+, step 0.1, default = 0 = auto from screen width/height aspect ratio, multiply the mouse speed depending from the current mouse speed. Acceleration Y of mouse vector. When vector_acceleration_y=0 then vector_acceleration_y calculated from vector_acceleration by actual screen aspect ratio.  You can draw the mosue vector with pointer. This drawed vector move the mouse. This work independent from sensitivity.
        private double mouseMainAccThreshold = Settings.Default.flyfps_main_acceleration_threshold; //range 0-100, step 0.01, default = 40, When your hand speed above flyfps_main_acceleration_threshold the mouse acceleration = flyfps_main_acceleration. When your hand speed slower then flyfps_main_acceleration_threshold the mouse acceleration ranged from 0 to flyfps_main_acceleration. When you lower  this value, mouse speed accelerated faster to the flyfps_main_acceleration.
        private double mouseMainAccFiner = Settings.Default.flyfps_main_acceleration_finer; //range 0-100, step 0.1, default = 10, This value determine how fast react the mouse speed to your hand. Bigger value mean react slower. Lower value mean react faster. The remote is very sensitive to acceleretaion so must extend it.
        private double mouseMainDeccStart = Settings.Default.flyfps_main_decceleration_low; //range 0-100, step 0.1, default = 5. When your hand speed is slow, this ranged to flyfps_main_decceleration_end when you move faster. This value decrease the pointer drawed mouse mover vector in each frame. When you not slow down the mouse vector, it turns for continously. Bigger value give more decceleration. This work independently from acceleration.
        private double mouseMainDeccEnd = Settings.Default.flyfps_main_decceleration_high; //range 0-100, step 0.1, default = 10.When your hand speed is fast, this ranged to flyfps_main_decceleration_start when you move slower. This value decrease the pointer drawed mouse mover vector in each frame. When you not slow down the mouse vector, it turns for continously. Bigger value give more decceleration. when flyfps_main_decceleration_end < flyfps_main_decceleration_start than your faster hand speed not break the mouse vector. This work independently from acceleration.
        private double mouseMainDeccThreshold = Settings.Default.flyfps_main_decceleration_threshold; //range 0-100, step 0.01, default = 40, When your hand speed above flyfps_main_decceleration_threshold the mouse deceleration = flyfps_main_decceleration_end. When your hand speed slower then flyfps_main_decceleration_threshold the mouse deceleration ranged from flyfps_main_decceleration_start to flyfps_main_decceleration_end. When you lower  this value, mouse speed reach faster flyfps_main_decceleration_end.
        private double returnBoundAccelerator = Settings.Default.flyfps_return_bound_acceleration; //range 0-100, step 0,1, default = 5, When the pointer return from out of bounds, the speed of mouse start from 0 and increased with this acceleration. When lower the value, you have more time for recenter your pointer to the sensor bar after you leave the sensed zone.
        private double outOfBoundsDecceleration = Settings.Default.flyfps_out_bound_decceleration; //range 0-100, step 0.1, default = 10, when leave the sensorbar bounds, the mouse speed deccelerated with this value. When you set it 0 then the pointer leave the sensorbar the mouse turn continously for the desired direction.
        private bool mouseFineLeftEnable = Settings.Default.flyfps_left_button_fine; //
        private double mouseFineSensX = Settings.Default.flyfps_fine_sensitivity; //range 0-10, step 0.1, default = 0.25, Fine Sensitivity X and Y of mouse, when you press the right or left mouse button you switch to fine aiming sensitivity mode. When set it 0 it turns off. The fine mouse flyfps_fine_sensitivity_y calculatod from flyfps_fine_sensitivity by actual screen aspect ratio.
        private double mouseFineSensY = Settings.Default.flyfps_fine_sensitivity_y; //range 0-10, step: 0.1, default: 0 = auto from screen width/height aspect ratio, Fine Sensitivity Y of mouse, when you press the right or left mouse button you switch to fine aiming sensitivity mode. When set it 0 then the fine mouse flyfps_fine_sensitivity_y calculatod from flyfps_fine_sensitivity by actual screen aspect ratio.
        private double mouseFineAccX = Settings.Default.flyfps_fine_acceleration; //range 0-100, step 0.1, default = 10, Fine acceleration X and Y of mouse, when you press the right or left mouse button you switch to fine aiming sensitivity mode. When set it 0 it turns off. The fine mouse flyfps_fine_acceleration_y calculatod from flyfps_fine_acceleration.
        private double mouseFineAccY = Settings.Default.flyfps_fine_acceleration_y; //range 0-100, step: 0.1, default: 0 = auto from screen width/height aspect ratio, Fine acceleration Y of mouse, when you press the right mouse button you switch to fine aiming sensitivity mode the pointer. When set it 0 then the fine mouse flyfps_fine_acceleration_y calculatod from flyfps_fine_acceleration.
        private double mouseFineDecc = Settings.Default.flyfps_fine_decceleration; //range 0-10, step 0.1, default = 10, Fine decceleration X and Y of mouse, when you press the right or left mouse button you switch to fine aiming sensitivity mode. When set it 0 it turns off the mouse accelerating from the current position.
        private double mouseForwardControlX = Settings.Default.flyfps_mouse_forward; //range 0-1000, step 1, default = 0 add fixed X direction jump to the mouse, when you shake your hand to the desired direction with defined acceleration. When your acceleration X reach the specified acceleretaion the mouse jump with fixed value.
        private double mouseForwardControlY = Settings.Default.flyfps_mouse_forward_y; //range 0-1000, step 1, default = 0 add fixed Y direction jump to the mouse. when you shake your hand to the desired direction with defined acceleration. When set to 0 its equal to flyfps_mouse_forward. When your acceleration Y reach the specified acceleretaion the mouse jump with fixed value.
        private double mouseForwardTurnOnThreshold = Settings.Default.flyfps_mouse_forward_turn_on_threshold; //range 0-100, default = 25, step 1, below this mousespeed limit mouse forward not take effect. When your hand acceleration above this value it add fix mouse move to the desired direction.
        private double mouseFoewardDecceleration = Settings.Default.flyfps_mouse_forward_decceleration; //range 0-100, default = 5, when mouse jump with fixed mouse forward its not stop instantly, slowed down with this decceleration.
        private double extraTurnSensX = Settings.Default.flyfps_extra_turn_sensitivity; //range 0-100, default = 0 = off, step 0.1, set extra turn sensitivity X and Y. During travelled distance with pointer the speed increased to this sensitivity. When flyfps_extra_turn_sensitivity_y = 0 then flyfps_extra_turn_sensitivity_y calculated from this value by actual screen aspect ratio. 
        private double extraTurnSensY = Settings.Default.flyfps_extra_turn_sensitivity_y; //range 0+, default = 0 = auto from screen width/height aspect ratio, step 0.1, set extra turn sensitivity Y. During travelled distance with pointer the speed increased to this sensitivity. When flyfps_extra_turn_sensitivity_y = 0 then flyfps_extra_turn_sensitivity_y calculated from this value.  
        private Vector extraTurnDeadZone = new Vector(Settings.Default.flyfps_extra_turn_deadzone, Settings.Default.flyfps_extra_turn_deadzone); // range 0 - 100, default = 20. In this zone the extra turn speed not take affect. When you move the opposite direction the pointer its start from beginning the travelling deadzone. For proper work increase flyfps_mouse_smooth_buffer for your hand.
        private Vector extraTurnEaseIn = new Vector(Settings.Default.flyfps_extra_turn_easein, Settings.Default.flyfps_extra_turn_easein); // range 0 - 100, default = 40. After travelling deadzone, the speed begin increasing to the extra sensitivity during this easein distance.
        private double mouseFinerLow = Settings.Default.flyfps_mouse_finer_low; // range 0 - 100, default = 85, step 0.1, this fining your mouse moves on low hand speed. Decreasing the distance of two mouse steps. The result is very fine and smooth mouse move. When increaseing pointer_FPS the move is faster you can add more fining. I prefer pointer_FPS=200,in the game lock fps to 60FPS. When you sync your framerate in touchmote(ie:60-120-180-200FPS) and in the game(ie:30-60FPS+) that give the best result. Just try it :)
        private double mouseFinerHigh = Settings.Default.flyfps_mouse_finer_high; // range 0 - 100, default = 80, step 0.1, this fining your mouse moves on high hand speed.  Decreasing the distance of two mouse steps. The result is very fine and smooth mouse move. When increaseing pointer_FPS the move is faster you can add more fining. I prefer pointer_FPS=200,in the game lock fps to 60FPS. When you sync your framerate in touchmote(ie:60-120-180-200FPS) and in the game(ie:30-60FPS+) that give the best result. Just try it :)
        private double mouseFinerThreshold = Settings.Default.flyfps_mouse_finer_threshold; // range 0 - 100, default = 5, step 1, When mouse speed(pixel) reach this value the mousfiner reach flyfps_mouse_finer_high. Below this value flyfps_mouse_finer_low ranged to flyfps_mouse_finer_high.
        private double borderTurnControlX = Settings.Default.flyfps_border_turn_speed_x; // range 0+, default = 0, step 0.1, when pointer leave flyfps_horizontal_border the sensitivity is increased depending the distance of pointer from border
        private double borderTurnControlY = Settings.Default.flyfps_border_turn_speed_y; // range 0+, default = 0, step 0.1, when pointer leave flyfps_vertical_border the sensitivity is increased depending the distance of pointer from border
        private double autoTurnControlX = Settings.Default.flyfps_auto_turn_speed_x; // range 0+, default = 0, step 0.1, when pointer leave flyfps_horizontal_border the mouse speed X continuosly increased depending the distance of pointer from border
        private double autoTurnControlY = Settings.Default.flyfps_auto_turn_speed_y; // range 0+, default = 0, step 0.1, when pointer leave flyfps_vertical_border the mouse speed Y continuosly increased depending the distance of pointer from border
        // TODO: END; add a user friendly control gear panel in the UI, like on the right Tilt X- ...

        private double screenWidth;
        private double screenHeight;
        private Vector cursorTargetPos;
        private Vector cursorVirtualPos;

        private Vector[] cursorVectorBuffer = new Vector[2];
        private Vector cursorVectorDelta;
        private SmoothingBuffer smoothingVectorBuffer;
        private Vector mouseMainSpeed;
        private Vector mouseMainStore;
        private Vector mouseMainAccStore;

        private SmoothingBuffer smoothingRelativeBuffer;
        private Vector smoothedCursorRelative;
        private Vector[] smoothedCursorRelativeBuffer = new Vector[2];
        private Vector smoothedCursorRelativeDelta;

        private bool mustFillSmoothedRelativeBuffer;
        private bool outTurnLeftRight;
        private bool outTurnUpDown;
        private double deltaOutAccelerator;
        private Vector outTurn;

        private Vector mouseForward;
        private Vector turnAcceleration;
        private Vector sumRelativeDelta;
        private Vector cursorDelta;

        private Vector mouseTurnSpeed;
        private Vector currentMouseSpeed;
        private double startingTurningSpeedX;
        private double startingTurningSpeedY;

        private double leftBorderRelative; private double rightBorderRelative; private double topBorderRelative; private double bottomBorderRelative;

        private string mydebug;
        private uint liveGraphAxisX;
        private bool debugEnabled = Settings.Default.flyfps_debug;

        public MouseHandler()
        {
            this.inputSimulator = new InputSimulator();
            cursorPositionHelper = new CursorPositionHelper();

            // common
            System.Drawing.Rectangle screenBounds = DeviceUtils.DeviceUtil.GetScreen(Settings.Default.primaryMonitor).Bounds;
            screenWidth = screenBounds.Width;
            screenHeight = screenBounds.Height;

            leftBorderRelative = (100 - Settings.Default.flyfps_horizontal_border) / 2 / 100;
            rightBorderRelative = 1 - leftBorderRelative;
            topBorderRelative = (100 - Settings.Default.flyfps_vertical_border) / 2 / 100;
            bottomBorderRelative = 1 - topBorderRelative;

            smoothingRelativeBuffer = new SmoothingBuffer(Settings.Default.flyfps_mouse_smooth_buffer);
            mustFillSmoothedRelativeBuffer = true;
            returnBoundAccelerator = mapRange(returnBoundAccelerator, 0, 100, 0.000000000000001, 0.1);
            outOfBoundsDecceleration = 1 - mapRange(outOfBoundsDecceleration, 0, 100, 0, 1);
            mouseFinerLow = 1 - mapRange(mouseFinerLow, 0, 100, 0, 0.99);
            mouseFinerHigh = 1 - mapRange(mouseFinerHigh, 0, 100, 0, 0.99);
            mouseFinerThreshold = mapRange(mouseFinerThreshold, 0, 100, 0, 100);

            // mouse speed vector
            smoothingVectorBuffer = new SmoothingBuffer(Settings.Default.flyfps_main_buffer);
            if (mouseMainSensY == 0)
                mouseMainSensY = Settings.Default.flyfps_main_sensitivity * screenHeight / screenWidth;

            if (mouseMainAccY == 0)
                mouseMainAccY = Settings.Default.flyfps_main_acceleration * screenHeight / screenWidth;
            mouseMainAccThreshold = mapRange(mouseMainAccThreshold, 0, 100, 0, 0.1);
            mouseMainAccFiner = 1 - mapRange(mouseMainAccFiner, 0, 100, 0, 0.99);

            mouseMainDeccStart = 0.99 - mapRange(mouseMainDeccStart, 0, 100, 0, 0.98);
            mouseMainDeccEnd = 0.99 - mapRange(mouseMainDeccEnd, 0, 100, 0, 0.98);
            mouseMainDeccThreshold = mapRange(mouseMainDeccThreshold, 0, 100, 0, 0.1);

            // mouse fine
            if (mouseFineSensY == 0)
                mouseFineSensY = Settings.Default.flyfps_fine_sensitivity * screenHeight / screenWidth;

            if (mouseFineAccY == 0)
                mouseFineAccY = Settings.Default.flyfps_fine_acceleration * screenHeight / screenWidth;

            mouseFineDecc = 1 - mapRange(mouseFineDecc, 0, 100, 0, 0.999);

            // mouse forward
            if (mouseForwardControlY == 0)
                mouseForwardControlY = Settings.Default.flyfps_mouse_forward;
            mouseForwardTurnOnThreshold = mapRange(mouseForwardTurnOnThreshold, 0, 100, 0, 0.1);
            mouseFoewardDecceleration = mapRange(mouseFoewardDecceleration, 0, 100, 0, 0.99);

            // extra turn distance
            extraTurnDeadZone.X = mapRange(extraTurnDeadZone.X, 0, 100, 0, 0.5);
            extraTurnDeadZone.Y = extraTurnDeadZone.X;

            extraTurnEaseIn.X = mapRange(extraTurnEaseIn.X, 0, 100, 0, 0.5);
            extraTurnEaseIn.Y = extraTurnEaseIn.X;

            if (extraTurnSensY == 0)
                extraTurnSensY = extraTurnSensX * screenHeight / screenWidth;

            liveGraphAxisX = 0;
            mydebug = "##|##\n";
            mydebug += "liveGraphAxisX|IRDeltaX|IRDeltaY|mouseMainSpeedX|mouseMainSpeedY|mouseMainStoreX|mouseMainStoreY|mouseOutX|mouseOutY\n";
            File.Delete(@"debug.txt");
        }

        public bool reset()
        {
            if (mouseLeftDown)
            {
                setButtonUp("mouseleft");
            }
            if (mouseRightDown)
            {
                setButtonUp("mouseright");
            }
            return true;
        }

        public bool setButtonDown(string key)
        {
            if (Enum.IsDefined(typeof(MouseCode), key.ToUpper()))
            {
                MouseCode mouseCode = (MouseCode)Enum.Parse(typeof(MouseCode), key, true);
                switch (mouseCode)
                {
                    case MouseCode.MOUSELEFT:
                        this.inputSimulator.Mouse.LeftButtonDown();
                        mouseLeftDown = true;
                        break;
                    case MouseCode.MOUSERIGHT:
                        this.inputSimulator.Mouse.RightButtonDown();
                        mouseRightDown = true;
                        break;
                    default:
                        return false;
                }
                return true;
            }
            return false;
        }

        public bool setButtonUp(string key)
        {
            if (Enum.IsDefined(typeof(MouseCode), key.ToUpper()))
            {
                MouseCode mouseCode = (MouseCode)Enum.Parse(typeof(MouseCode), key, true);
                switch (mouseCode)
                {
                    case MouseCode.MOUSELEFT:
                        this.inputSimulator.Mouse.LeftButtonUp();
                        mouseLeftDown = false;
                        break;
                    case MouseCode.MOUSERIGHT:
                        this.inputSimulator.Mouse.RightButtonUp();
                        mouseRightDown = false;
                        break;
                    default:
                        return false;
                }
                return true;
            }
            return false;
        }

        public bool setPosition(string key, CursorPos cursorPos)
        {
            key = key.ToLower();
            if (key.Equals("mouse"))
            {
                if (!cursorPos.OutOfReach)
                {
                    Point smoothedPos = cursorPositionHelper.getRelativePosition(new Point(cursorPos.X, cursorPos.Y));
                    this.inputSimulator.Mouse.MoveMouseToPositionOnVirtualDesktop((65535 * smoothedPos.X), (65535 * smoothedPos.Y));
                    return true;
                }
            }

            if (key.Equals("fpsmouse"))
            {
                Point smoothedPos = cursorPositionHelper.getSmoothedPosition(new Point(cursorPos.RelativeX, cursorPos.RelativeY));

                /*
                    * TODO: Consider sensor bar position?
                if (Settings.Default.pointer_sensorBarPos == "top")
                {
                    smoothedPos.Y = smoothedPos.Y - Settings.Default.pointer_sensorBarPosCompensation;
                }
                else if (Settings.Default.pointer_sensorBarPos == "bottom")
                {
                    smoothedPos.Y = smoothedPos.Y + Settings.Default.pointer_sensorBarPosCompensation;
                }
                */
                double deadzone = Settings.Default.fpsmouse_deadzone; // TODO: Move to settings
                double shiftX = Math.Abs(smoothedPos.X - 0.5) > deadzone ? smoothedPos.X - 0.5 : 0;
                double shiftY = Math.Abs(smoothedPos.Y - 0.5) > deadzone ? smoothedPos.Y - 0.5 : 0;

                this.inputSimulator.Mouse.MoveMouseBy((int)(Settings.Default.fpsmouse_speed * shiftX), (int)(Settings.Default.fpsmouse_speed * shiftY));

                return true;
            }

            if (key.Equals("flyfpsmouse"))
            {
                String tmpDebug = "";

                if (cursorPos.OutOfReach)
                {
                    double tmpOutTurnX = mouseMainSpeed.X + mouseForward.X + turnAcceleration.X + mouseTurnSpeed.X + currentMouseSpeed.X;
                    double tmpOutTurnY = mouseMainSpeed.Y + mouseForward.Y + turnAcceleration.Y + mouseTurnSpeed.Y + currentMouseSpeed.Y;
                    if (outTurnLeftRight & !outTurnUpDown) tmpOutTurnY = 0.000000000000001;
                    if (!outTurnLeftRight & outTurnUpDown) tmpOutTurnX = 0.000000000000001;

                    if (tmpOutTurnX == 0)
                    {
                        outTurn.X *= outOfBoundsDecceleration;
                    }
                    else
                    {
                        outTurn.X = tmpOutTurnX;
                        mouseMainSpeed.X = mouseForward.X = turnAcceleration.X = mouseTurnSpeed.X = currentMouseSpeed.X = 0;
                    }

                    if (tmpOutTurnY == 0)
                    {
                        outTurn.Y *= outOfBoundsDecceleration;
                    }
                    else
                    {
                        outTurn.Y = tmpOutTurnY;
                        mouseMainSpeed.Y = mouseForward.Y = turnAcceleration.Y = mouseTurnSpeed.Y = currentMouseSpeed.Y = 0;
                    }

                    mouseMainStore.X = mouseMainStore.Y = 0;
                    sumRelativeDelta.X = sumRelativeDelta.Y = 0;
                    mustFillSmoothedRelativeBuffer = true;
                    deltaOutAccelerator = 0.000000000000001;
                }
                else
                {
                    if (mustFillSmoothedRelativeBuffer)
                    {
                        smoothingRelativeBuffer.fill(cursorPos.RelativeX, cursorPos.RelativeY);
                        smoothingVectorBuffer.fill(cursorPos.RelativeX, cursorPos.RelativeY);

                        cursorVectorBuffer[1].X = smoothedCursorRelativeBuffer[1].X = cursorPos.RelativeX;
                        cursorVectorBuffer[1].Y = smoothedCursorRelativeBuffer[1].Y = cursorPos.RelativeY;
                        smoothedCursorRelativeDelta.X = smoothedCursorRelativeDelta.Y = 0;// mouseForvard
                        mustFillSmoothedRelativeBuffer = false;
                    }
                    else
                    {
                        smoothingRelativeBuffer.addValue(cursorPos.RelativeX, cursorPos.RelativeY);
                        smoothingVectorBuffer.addValue(cursorPos.RelativeX, cursorPos.RelativeY);
                    }

                    // mouseFineSpeed smoothed relative
                    smoothedCursorRelative = smoothingRelativeBuffer.getSmoothedValue();
                    smoothedCursorRelativeBuffer[0] = smoothedCursorRelativeBuffer[1];
                    smoothedCursorRelativeBuffer[1] = smoothedCursorRelative;
                    Vector prevSmoothedCursorRelativeDelta = smoothedCursorRelativeDelta;
                    smoothedCursorRelativeDelta = smoothedCursorRelativeBuffer[1] - smoothedCursorRelativeBuffer[0];

                    // vector smoothed relative
                    cursorVectorBuffer[0] = cursorVectorBuffer[1];
                    cursorVectorBuffer[1] = smoothingVectorBuffer.getSmoothedValue();
                    Vector prevCursorVectorDelta = cursorVectorDelta;
                    cursorVectorDelta = cursorVectorBuffer[1] - cursorVectorBuffer[0];

                    // outturn
                    if (deltaOutAccelerator != 0)
                    {
                        if (Math.Abs(outTurn.X) > 1 | Math.Abs(outTurn.Y) > 1)
                        {
                            outTurn.X *= 0.5;
                            outTurn.Y *= 0.5;
                            smoothedCursorRelativeDelta.X = smoothedCursorRelativeDelta.Y = cursorVectorDelta.X = cursorVectorDelta.Y = 0;
                        }
                        else
                        {
                            smoothedCursorRelativeDelta.X *= deltaOutAccelerator;
                            smoothedCursorRelativeDelta.Y *= deltaOutAccelerator;
                            cursorVectorDelta.X *= deltaOutAccelerator;
                            cursorVectorDelta.Y *= deltaOutAccelerator;
                            deltaOutAccelerator += returnBoundAccelerator;
                            if (deltaOutAccelerator >= 1)
                            {
                                deltaOutAccelerator = 0;
                            }
                            outTurn.X = outTurn.Y = 0;
                        }
                    }

                    // mouse aim speed mod
                    double mouseMainSensTmpX = mouseMainSensX;
                    double mouseMainAccTmpX = mouseMainAccX;
                    double mouseMainDeccTmp = mouseMainDeccStart;
                    double mouseMainDeccTmpMax = mouseMainDeccEnd;

                    double mouseMainSensTmpY = mouseMainSensY;
                    double mouseMainAccTmpY = mouseMainAccY;

                    if ((mouseRightDown || (mouseLeftDown && mouseFineLeftEnable)) && mouseFineSensX != 0)
                    {
                        mouseMainSensTmpX = mouseFineSensX;
                        mouseMainAccTmpX = mouseFineAccX;

                        mouseMainDeccTmp = mouseFineDecc;
                        mouseMainDeccTmpMax = mouseFineDecc;

                        mouseMainSensTmpY = mouseFineSensY;
                        mouseMainAccTmpY = mouseFineAccY;
                    }

                    // mouseMainSpeed Vector
                    mouseMainDeccTmp = mapRangeLength(mouseMainStore.Length, 0, mouseMainDeccThreshold, mouseMainDeccTmp, mouseMainDeccTmpMax);

                    mouseMainStore.X *= mouseMainDeccTmp;
                    mouseMainStore.X += cursorVectorDelta.X;

                    double readyAccelerationX = mapRangeLength(cursorVectorDelta.Length, 0, mouseMainAccThreshold, 0, mouseMainAccTmpX);
                    mouseMainAccStore.X += readyAccelerationX;
                    readyAccelerationX = mouseMainAccStore.X * mouseMainAccFiner;
                    mouseMainAccStore.X -= readyAccelerationX;

                    mouseMainSpeed.X = mouseMainStore.X * (mouseMainSensTmpX + readyAccelerationX) * screenWidth;


                    mouseMainStore.Y *= mouseMainDeccTmp;
                    mouseMainStore.Y += cursorVectorDelta.Y;

                    double readyAccelerationY = mapRangeLength(cursorVectorDelta.Length, 0, mouseMainAccThreshold, 0, mouseMainAccTmpY);
                    mouseMainAccStore.Y += readyAccelerationY;
                    readyAccelerationY = mouseMainAccStore.Y * mouseMainAccFiner;
                    mouseMainAccStore.Y -= readyAccelerationY;

                    mouseMainSpeed.Y = mouseMainStore.Y * (mouseMainSensTmpY + readyAccelerationY) * screenWidth; // DO NOT CHANGE :P


                    // mouseForward
                    double tmpMouseForwardX = Math.Abs(smoothedCursorRelativeDelta.X) - Math.Abs(prevSmoothedCursorRelativeDelta.X);
                    tmpMouseForwardX = Math.Max(tmpMouseForwardX, 0);
                    if (Math.Abs(smoothedCursorRelativeDelta.X) > mouseForwardTurnOnThreshold & Math.Abs(mouseForward.X) < 1)
                    {
                        mouseForward.X = Math.Sign(smoothedCursorRelativeDelta.X) * mouseForwardControlX;
                    }
                    else mouseForward.X = Math.Sign(mouseForward.X) * valueFollow(Math.Abs(mouseForward.X), 0, mouseFoewardDecceleration, mouseFoewardDecceleration);

                    double tmpMouseForwardY = Math.Abs(smoothedCursorRelativeDelta.Y) - Math.Abs(prevSmoothedCursorRelativeDelta.Y);
                    tmpMouseForwardY = Math.Max(tmpMouseForwardY, 0);
                    if (Math.Abs(smoothedCursorRelativeDelta.Y) > mouseForwardTurnOnThreshold & Math.Abs(mouseForward.Y) < 1)
                    {
                        mouseForward.Y = Math.Sign(smoothedCursorRelativeDelta.Y) * mouseForwardControlY;
                    }
                    else mouseForward.Y = Math.Sign(mouseForward.Y) * valueFollow(Math.Abs(mouseForward.Y), 0, mouseFoewardDecceleration, mouseFoewardDecceleration);

                    // turn acceleration from distance
                    Vector prevSumRelativeDelta = sumRelativeDelta;
                    sumRelativeDelta += smoothedCursorRelativeDelta;
                    Vector sumDeadZoneEaseIn = extraTurnDeadZone + extraTurnEaseIn;

                    double sumRelativeDeltaAbsX = Math.Abs(sumRelativeDelta.X);
                    double directionX = Math.Sign(sumRelativeDelta.X);
                    if (sumRelativeDeltaAbsX > sumDeadZoneEaseIn.X) sumRelativeDelta.X = directionX * sumDeadZoneEaseIn.X;
                    if (sumRelativeDeltaAbsX < Math.Abs(prevSumRelativeDelta.X)) sumRelativeDelta.X = 0;
                    turnAcceleration.X = directionX * mapRange(sumRelativeDeltaAbsX, extraTurnDeadZone.X, sumDeadZoneEaseIn.X, 0, extraTurnSensX) * screenWidth * Math.Abs(smoothedCursorRelativeDelta.X);

                    double sumRelativeDeltaAbsY = Math.Abs(sumRelativeDelta.Y);
                    double directionY = Math.Sign(sumRelativeDelta.Y);
                    if (sumRelativeDeltaAbsY > sumDeadZoneEaseIn.Y) sumRelativeDelta.Y = directionY * sumDeadZoneEaseIn.Y;
                    if (sumRelativeDeltaAbsY < Math.Abs(prevSumRelativeDelta.Y)) sumRelativeDelta.Y = 0;
                    turnAcceleration.Y = directionY * mapRange(sumRelativeDeltaAbsY, extraTurnDeadZone.Y, sumDeadZoneEaseIn.Y, 0, extraTurnSensY) * screenWidth * Math.Abs(smoothedCursorRelativeDelta.Y);

                    // turn acceleration from border
                    if (borderTurnControlX != 0)
                    {
                        double multiplyer;
                        double distanceFromBorder;
                        if (smoothedCursorRelative.X < leftBorderRelative)
                        {
                            distanceFromBorder = leftBorderRelative - smoothedCursorRelative.X;
                            multiplyer = mapRange(distanceFromBorder, 0, leftBorderRelative, 0.5, 1);

                            if (smoothedCursorRelativeDelta.X > 0) multiplyer *= multiplyer;
                            mouseTurnSpeed.X += smoothedCursorRelativeDelta.X * screenWidth * borderTurnControlX;
                        }
                        else if (smoothedCursorRelative.X > rightBorderRelative)
                        {
                            distanceFromBorder = smoothedCursorRelative.X - rightBorderRelative;
                            multiplyer = mapRange(distanceFromBorder, 0, leftBorderRelative, 0.5, 1);

                            if (smoothedCursorRelativeDelta.X < 0) multiplyer *= multiplyer;
                            mouseTurnSpeed.X += smoothedCursorRelativeDelta.X * screenWidth * borderTurnControlX;
                        }
                        else
                        {
                            distanceFromBorder = multiplyer = 0;
                        }
                        mouseTurnSpeed.X *= multiplyer;
                    }

                    if (borderTurnControlY != 0)
                    {
                        double multiplyer;
                        double distanceFromBorder;
                        if (smoothedCursorRelative.Y < topBorderRelative)
                        {
                            distanceFromBorder = topBorderRelative - smoothedCursorRelative.Y;
                            multiplyer = mapRange(distanceFromBorder, 0, topBorderRelative, 0.5, 1);

                            if (smoothedCursorRelativeDelta.Y > 0) multiplyer *= multiplyer;
                            mouseTurnSpeed.Y += smoothedCursorRelativeDelta.Y * screenHeight * borderTurnControlY;
                        }
                        else if (smoothedCursorRelative.Y > bottomBorderRelative)
                        {
                            distanceFromBorder = smoothedCursorRelative.Y - bottomBorderRelative;
                            multiplyer = mapRange(distanceFromBorder, 0, topBorderRelative, 0.5, 1);

                            if (smoothedCursorRelativeDelta.Y < 0) multiplyer *= multiplyer;
                            mouseTurnSpeed.Y += smoothedCursorRelativeDelta.Y * screenHeight * borderTurnControlY;
                        }
                        else
                        {
                            distanceFromBorder = multiplyer = 0;
                        }
                        mouseTurnSpeed.Y *= multiplyer;
                    }

                    // autoTurn
                    if (autoTurnControlX != 0)
                    {
                        if (startingTurningSpeedX == 999) startingTurningSpeedX = mouseMainSpeed.X + mouseTurnSpeed.X;
                        if (smoothedCursorRelative.X < leftBorderRelative)
                        {
                            currentMouseSpeed.X = -mapRange((leftBorderRelative - smoothedCursorRelative.X), 0, leftBorderRelative, 1, autoTurnControlX) + startingTurningSpeedX;
                            startingTurningSpeedX *= 0.85;
                            mouseMainSpeed.X = 0;
                        }
                        else if (smoothedCursorRelative.X > rightBorderRelative)
                        {
                            currentMouseSpeed.X = mapRange((smoothedCursorRelative.X - rightBorderRelative), 0, leftBorderRelative, 1, autoTurnControlX) + startingTurningSpeedX;
                            startingTurningSpeedX *= 0.85;
                            mouseMainSpeed.X = 0;
                        }
                        else
                        {
                            currentMouseSpeed.X = 0;
                            startingTurningSpeedX = 999;
                        }
                    }
                    if (autoTurnControlY != 0)
                    {
                        if (startingTurningSpeedY == 999) startingTurningSpeedY = mouseMainSpeed.Y + mouseTurnSpeed.Y;
                        if (smoothedCursorRelative.Y < topBorderRelative)
                        {
                            currentMouseSpeed.Y = -mapRange((topBorderRelative - smoothedCursorRelative.Y), 0, topBorderRelative, 1, autoTurnControlY) + startingTurningSpeedY;
                            startingTurningSpeedY *= 0.85;
                            mouseMainSpeed.Y = 0;
                        }
                        else if (smoothedCursorRelative.Y > bottomBorderRelative)
                        {
                            currentMouseSpeed.Y = mapRange((smoothedCursorRelative.Y - bottomBorderRelative), 0, topBorderRelative, 1, autoTurnControlY) + startingTurningSpeedY;
                            startingTurningSpeedY *= 0.85;
                            mouseMainSpeed.Y = 0;
                        }
                        else
                        {
                            currentMouseSpeed.Y = 0;
                            startingTurningSpeedY = 999;
                        }
                    }
                    // out bound turning direction
                    if (smoothedCursorRelative.X < leftBorderRelative | smoothedCursorRelative.X > rightBorderRelative) outTurnLeftRight = true;
                    else outTurnLeftRight = false;
                    if (smoothedCursorRelative.Y < topBorderRelative | smoothedCursorRelative.Y > bottomBorderRelative) outTurnUpDown = true;
                    else outTurnUpDown = false;
                }

                cursorDelta = new Vector(
                 turnAcceleration.X + mouseForward.X + mouseTurnSpeed.X + currentMouseSpeed.X + outTurn.X + mouseMainSpeed.X,
                 turnAcceleration.Y + mouseForward.Y + mouseTurnSpeed.Y + currentMouseSpeed.Y + outTurn.Y + mouseMainSpeed.Y
                );

                // mouse fining
                cursorTargetPos += cursorDelta;
                cursorDelta = cursorTargetPos - cursorVirtualPos;

                double mouseFinerTMP = mapRangeLength(cursorDelta.Length, 0, mouseFinerThreshold, mouseFinerLow, mouseFinerHigh);

                cursorDelta = cursorDelta * mouseFinerTMP;

                if (Math.Abs(cursorDelta.X) >= 1) cursorVirtualPos.X += cursorDelta.X;
                if (Math.Abs(cursorDelta.Y) >= 1) cursorVirtualPos.Y += cursorDelta.Y;
                // move the mouse
                this.inputSimulator.Mouse.MoveMouseBy(
                    (int)(cursorDelta.X),
                    (int)(cursorDelta.Y));

                if (debugEnabled)
                {
                    tmpDebug += liveGraphAxisX + "|";
                    tmpDebug += (cursorVectorDelta.X * 10000) + "|";
                    tmpDebug += (cursorVectorDelta.Y * 10000) + "|";
                    tmpDebug += Math.Floor(mouseMainSpeed.X) + "|";
                    tmpDebug += Math.Floor(mouseMainSpeed.Y) + "|";
                    tmpDebug += (mouseMainStore.X * 1000) + "|";
                    tmpDebug += (mouseMainStore.Y * 1000) + "|";
                    tmpDebug += Math.Floor(cursorDelta.X) + "|";
                    tmpDebug += Math.Floor(cursorDelta.Y) + "\n";
                    mydebug += tmpDebug.Replace(",", ".");
                    liveGraphAxisX++;
                    MainWindow.Current.ShowMessage("DebugENABLED", MainWindow.MessageType.Info);

                    using (StreamWriter outputFile = new StreamWriter("debug.txt", true))
                    {
                        //if (mydebug.Length > 102400)
                        if (liveGraphAxisX % 1900 == 0)
                        {
                            outputFile.WriteLine(mydebug);
                            mydebug = "";
                        }
                    }
                }

            }

            return false;
        }
        private double mapRange(double toRange, double inputStart, double inputEnd, double outputStart, double outputEnd)
        {
            if (toRange < inputStart) return outputStart;
            if (toRange > inputEnd) return outputEnd;
            if (outputEnd < outputStart) return outputStart;
            return outputStart + (toRange - inputStart) * (outputEnd - outputStart) / (inputEnd - inputStart);
        }
        private double mapRangeLength(double toRange, double inputStart, double inputEnd, double outputStart, double outputEnd)
        {
            if (toRange > inputEnd) return outputEnd;
            return outputStart + (toRange - inputStart) * (outputEnd - outputStart) / (inputEnd - inputStart);
        }
        private double valueFollow(double myvalue, double target, double acceleration, double deccelaration)
        {
            if (myvalue < target)
            {
                myvalue += Math.Abs(target - myvalue) * acceleration;
            }
            else if (myvalue > target)
            {
                myvalue -= Math.Abs(target - myvalue) * deccelaration;
            }
            return myvalue;
        }
        public bool setValue(string key, double value)
        {
            key = key.ToLower();
            switch (key)
            {
                case "mousey+":
                    this.inputSimulator.Mouse.MoveMouseBy(0, (int)(-30 * value + 0.5));
                    break;
                case "mousey-":
                    this.inputSimulator.Mouse.MoveMouseBy(0, (int)(30 * value + 0.5));
                    break;
                case "mousex+":
                    this.inputSimulator.Mouse.MoveMouseBy((int)(30 * value + 0.5), 0);
                    break;
                case "mousex-":
                    this.inputSimulator.Mouse.MoveMouseBy((int)(-30 * value + 0.5), 0);
                    break;
                default:
                    return false;
            }
            return true;
        }

        public bool connect()
        {
            return true;
        }

        public bool disconnect()
        {
            return true;
        }

        public bool startUpdate()
        {
            return true;
        }

        public bool endUpdate()
        {
            return true;
        }
    }

    public enum MouseCode
    {
        MOUSELEFT,
        MOUSERIGHT
    }
}
