BIO:
==============
Hy Dear Gamers! I am an electrotechnicer and programmer.  I think i write some special code algorythm for touchmote to play ALL TYPE OF GAMES FOR FREE from couch. The Wiimote is very special and senstivy stuff.
When i firstly meet with WiiRemote i think i have Parkinson :D, but i not have. Just placed it to a paper box and aim to sensor bar and leave it alone and the cursor not shaked anymore.
That mean the wiimote is very sensitive and just need an effective interpolation system. That was i made. NO handshake, NO deadzone, NO buffer just precise fast mouse moves ;)
I played out Doom twice on ultra violence while i programming, so i know it is work fine!

FLYFPS Mode Description
==============
This mode for Touchmote is designed to play all types of games. FPS, flysym, strategy... FlyFPS mode use NOT AIM TO SCREEN but use AIM TO SENSORBAR method.
This mode have 6 innovative mouse moving for turn.

You can several SETTINGS (see below SETTINGS:) defining for mouse move. (1:flyfps_main, 2:flyfps_fine, 3:flyfps_mouse_forward, 4:flyfps_extra_turn, 5:flyfps_border_turn, 6:flyfps_auto_turn)

I mean important thing to set pointer_FPS syncron with your game frame rate or increase the pointer_FPS in the settings.json file while you not have the desired moving/turning.
Otherwise the mouse move still laggy or stutter. ie: i locked the doom to 60 frame rate and set the pointer_FPS to 200 and i can shoot very precisly and fast.

The Wiimote is VERY sensitive. Just chock your wrist on your leg or on the bed. When you leave with your pointer the sensorbar area and camera stopped, just aim to sensorbar fast as you can.

TIPP: when your camera not move ie: Doom glory kill or entering a vechile. Just recenter your pointer.
Try setup in settings.json in touchmote directory. Just increasing values step by step and test it on your game while it is reach the desired smooth fast motion.


IMPORTANT:
==============
flyfps_main_mouse: this is the main mouse mover. You can draw a vector with your IRpointer that move your mouse.

flyfps_main_acceleration mouse: this is the acceleration of vector mouse. This work independent from mouse vector sensitivity. Can use alone.

flyfps_main_acceleration_finer: This value define how fast react your mouse acceleration for IRpointer acceleration. When you increase this value the mouse react slower. This is very sensitive. This value define how fast can you react for things in game.

flyfps_fine_sensitivity: this is the fine mouse aim. When you hold the right and left mouse button then activate it. ie: assign your right mouse button to the Remote B or A button. I use for fire mouse left = A button, fine aim right mouse = B button.

flyfps_mouse_forward: this activated when your hand arriving a specified acceleration (flyfps_mouse_forward_turn_on_threshold) and move the mouse with specified distance.

flyfps_extra_turn: this is relative for your pointer position. When you move your IRponter in the desired direction this accelerated in specified distance to the specified sensitivity.

flyfps_border_turn_speed: when your pointer leave the border this accelerated your mouse depending the pointer distance from border.

flyfps_auto_turn_speed: when your pointer leave the border this continuosly move your mouse, the speed depending from the pointer distance from border

All these thing work individually, independent from each other.

SETTINGS:
==============
flyfps_horizontal_screen_border: 50.0, // range 0 - 100, this is the width of rectangle on screen in percent, when pointer leave this border than start work flyfps_border_turn_speed or flyfps_auto_turn_speed

flyfps_vertical_screen_border: 80.0, // range 0 - 100, this is the height of rectangle in percent, when pointer leave this border than start work flyfps_border_turn_speed or flyfps_auto_turn_speed

flyfps_mouse_finer_low: 85, // range 0 - 100, default = 85, step 0.1, this fining your mouse moves on low hand speed. Decreasing the distance of two mouse steps. The result is very fine and smooth mouse move. When increaseing pointer_FPS the move is faster you can add more fining. I prefer pointer_FPS=200,in the game lock fps to 60FPS. When you set your framerate in touchmote(ie:60-120-180-200FPS) and in the game(ie:30-60FPS+) that give the best result. Just try it :) I use for Doom 60FPS locked and pointer_FPS=200

flyfps_mouse_finer_high: 75, // range 0 - 100, default = 75, step 0.1, this fining your mouse moves on high hand speed.  Decreasing the distance of two mouse steps. The result is very fine and smooth mouse move. When increaseing pointer_FPS the move is faster you can add more fining. I prefer pointer_FPS=200,in the game lock fps to 60FPS. When you set your framerate in touchmote(ie:60-120-180-200FPS) and in the game(ie:30-60FPS+) that give the best result. Just try it what is the smoothest setting :) Bigger steps on higher hand speed give faster mouse move

flyfps_mouse_finer_threshold: 5, // range 0 - 100, default = 5, step 1, When mouse speed(pixel) reach this value the mousefiner reach flyfps_mouse_finer_high. Below this value flyfps_mouse_finer_low ranged to flyfps_mouse_finer_high. With mouse finer you can calibrate how fast follow the camera the IRmouse.

flyfps_out_bound_decceleration: 10.0, //range 0-100, step 0.1, default = 10, when leave the sensorbar bounds, the mouse speed deccelerated with this value. When you set it 0 then the pointer leave the sensorbar the mouse turn continously for the desired direction.

flyfps_return_bound_acceleration: 5.0, //range 0-100, step 0,1, default = 5, When the pointer return from out of bounds, the speed of mouse start from 0 and increased with this acceleration. When lower the value, you have more time for recenter your pointer to the sensor bar after you leave the sensed zone.

flyfps_main_buffer: 1, // range 1+; 1 = BUFFER OFF :) This buffer smoothing your shaking hand when you need, JUST IN MAIN AND FINE MOUSE, but keep in mind, this slowing down the reaction time. Set it to lowes value where your cursor not shaked. Default 1 for me, main_sens = 0.25; 1 = no buffering

flyfps_main_sensitivity: 0.25,// range 0-10, step 0.01, default = 0.25, Sensitivity X and Y of mouse vector. When main_sensitivity_y=0 then main_sensitivity_y calculated from main_sensitivity by actual screen aspect ratio.  You can draw the mosue vector with pointer. This drawed vector move the mouse. This work independent from acceleration.

flyfps_main_decceleration_low: 4.5, //range 0-100, step 0.1, default = 4.5. When your hand speed is slow, this ranged to flyfps_main_decceleration_high when you move faster. This value decrease the pointer drawed mouse mover vector in each frame. When you not slow down the mouse vector, it turns for continously. Bigger value give more decceleration. This work independently from acceleration.

flyfps_main_decceleration_high: 10.0, //range 0-100, step 0.1, default = 10. When your hand speed is fast, this ranged to flyfps_main_decceleration_low when you move slower. This value decrease the pointer drawed mouse mover vector in each frame. When you not slow down the mouse vector, it turns for continously. Bigger value give more decceleration. When flyfps_main_decceleration_high < flyfps_main_decceleration_low than your faster hand speed not break the mouse vector. But when flyfps_main_decceleration_end > flyfps_main_decceleration_start, you can break the mouse, so not slide when you wanna make a fast turn. This work independently from acceleration.

flyfps_main_decceleration_threshold: 50 //range 0-100, step 0.01, default = 50, When your hand speed above flyfps_main_decceleration_threshold the mouse deceleration = flyfps_main_decceleration_high. When your hand speed slower then flyfps_main_decceleration_threshold the mouse deceleration ranged from flyfps_main_decceleration_low to flyfps_main_decceleration_high. When you lower  this value, mouse speed reach faster flyfps_main_decceleration_high.

flyfps_main_acceleration: 30.0, //range 0+, step 0.1, default = 30, multiply the mouse speed depending from the current hand speed. Acceleration X and Y of mouse vector. When vector_acceleration_y=0 then vector_acceleration_y calculated from vector_acceleration by actual screen aspect ratio. You can draw the mosue vector with pointer. This drawed vector move the mouse. This work independent from sensitivity.

flyfps_main_acceleration_threshold: 50.0, //range 0-100, step 0.1, default = 50, When your hand speed above flyfps_main_acceleration_threshold the mouse acceleration = flyfps_main_acceleration. When your hand speed slower then flyfps_main_acceleration_threshold the mouse acceleration ranged from 0 to flyfps_main_acceleration. When you lower  this value, mouse speed accelerated faster to the flyfps_main_acceleration. You can use lower value than 1. ie: 0.1 or 0.01 when you need ;)

flyfps_main_acceleration_finer: 85 //range 0-100, step 0.1, default 85, This value define how fast react your mouse acceleration for IRpointer acceleration. When you increase this value the mouse react slower. This is very important and individually for evry one. This value define how fast can you react for things in game.

flyfps_left_button_fine: true // true or false. When set to true, while holding down left mouse button the mouse switch to fine aiming mode like on right mouse hold down.

flyfps_fine_sensitivity: 0.25, //range 0-10, step 0.1, default = 0.25, Fine Sensitivity X and Y of mouse, when you hold the right or left mouse button you switch to fine aiming sensitivity mode. When set it 0 = turns off. The fine mouse flyfps_fine_sensitivity_y calculatod from flyfps_fine_sensitivity by actual screen aspect ratio.

flyfps_fine_acceleration: 5.0, //range 0-100, step 0.1, default = 5, Fine acceleration X and Y of mouse, when you hold the right or left mouse button you switch to fine aiming sensitivity mode. When set it 0 it turns off. The fine mouse flyfps_fine_acceleration_y calculatod from flyfps_fine_acceleration.

flyfps_fine_decceleration: 5.0, //range 0-10, step 0.1, default = 5, Fine decceleration X and Y of mouse, when you hold the right or left mouse button you switch to fine aiming sensitivity mode. When set it 0 it turns off the mouse accelerating from the current position to the desired direction.

flyfps_mouse_smooth_buffer: 10, // range 1+ this buffer smoothing your shaking hand in (flyfps_mouse_forward, flyfps_extra_turn, flyfps_border_turn, flyfps_auto_turn), but keep in mind, this slowing down the reaction time. Set it to lowes value where your cursor not shaked.

flyfps_mouse_forward: 0.0, //range 0-1000, step 1, default = 0 add fixed X direction jump to the mouse, when you shake your hand to the desired direction with defined acceleration. When your acceleration X reach the specified acceleretaion the mouse jump with fixed value.

flyfps_mouse_forward_turn_on_threshold: 25.0, //range 0-100, default = 25, step 1, below this IR acceleration limit mouse forward not take effect. When your hand acceleration above this value it add fix mouse move to the desired direction.

flyfps_mouse_forward_decceleration: 5.0, //range 0-100, default = 5, when mouse jump with fixed mouse forward its not stop instantly, slowed down with this decceleration.

flyfps_extra_turn_sensitivity: 0.0, //range 0-100, default = 0 = off, step 0.1, set extra turn sensitivity X and Y. During travelled distance with pointer, the speed increased to this sensitivity. When flyfps_extra_turn_sensitivity_y = 0 then flyfps_extra_turn_sensitivity_y calculated from this value by actual screen aspect ratio. 

flyfps_extra_turn_deadzone: 20.0,// range 0 - 100, default = 20. In this zone the extra turn speed not take affect. When you move the opposite direction the pointer start from 0 the travelled deadzone. For proper work increase flyfps_mouse_smooth_buffer for your hand.

flyfps_extra_turn_easein: 40.0, // range 0 - 100, default = 40. After travelling deadzone, the speed begin increasing to the extra sensitivity during this easein distance.

flyfps_border_turn_speed_x: 0.0, // range 0+, default = 0, step 0.1, when pointer leave flyfps_horizontal_border the sensitivity is increased depending the distance of pointer from border

flyfps_border_turn_speed_y: 0.0, // range 0+, default = 0, step 0.1, when pointer leave flyfps_vertical_border the sensitivity is increased depending the distance of pointer from border

flyfps_auto_turn_speed_x: 0.0, // range 0+, default = 0, step 0.1, when pointer leave flyfps_horizontal_border the mouse speed X continuosly increased depending the distance of pointer from border

flyfps_auto_turn_speed_y: 0.0, // range 0+, default = 0, step 0.1, when pointer leave flyfps_vertical_border the mouse speed Y continuosly increased depending the distance of pointer from border

flyfps_main_sensitivity_y: 0.0, //range 0-10, step 0.01, default = 0 = auto from screen width/height aspect ratio, Sensitivity Y of mouse vector. When main_sensitivity_y=0 then main_sensitivity_y calculated from main_sensitivity.  You can draw the mosue vector with pointer. This drawed vector move the mouse.This work independent from acceleration.

flyfps_main_acceleration_y: 0.0, //range 0+, step 0.1, default = 0 = auto from screen width/height aspect ratio, multiply the mouse speed depending from the current hand speed. Acceleration Y of mouse vector. When vector_acceleration_y=0 then vector_acceleration_y calculated from vector_acceleration by actual screen aspect ratio.  You can draw the mosue vector with pointer. This drawed vector move the mouse. This work independent from sensitivity.

flyfps_fine_sensitivity_y: 0.0, //range 0-10, step: 0.1, default: 0 = auto from screen width/height aspect ratio, Fine Sensitivity Y of mouse, when you press the right or left mouse button you switch to fine aiming sensitivity mode. When set it 0 then the fine mouse flyfps_fine_sensitivity_y calculatod from flyfps_fine_sensitivity by actual screen aspect ratio.

flyfps_fine_acceleration_y: 0.0, //range 0-100, step: 0.1, default: 0 = auto from screen width/height aspect ratio, Fine acceleration Y of mouse, when you press the right or left mouse button you switch to fine aiming sensitivity mode the pointer. When set it 0 then the fine mouse flyfps_fine_acceleration_y calculatod from flyfps_fine_acceleration.

flyfps_mouse_forward_y: 0.0, //range 0-1000, step 1, default = 0 add fixed Y direction jump to the mouse. when you shake your hand to the desired direction with defined acceleration. When set to 0 its equal to flyfps_mouse_forward. When your acceleration Y reach the specified acceleretaion the mouse jump with fixed value.

flyfps_extra_turn_sensitivity_y: 0.0, //range 0+, default = 0 = auto from screen width/height aspect ratio, step 0.1, set extra turn sensitivity Y. During travelled distance with pointer the speed increased to this sensitivity. When flyfps_extra_turn_sensitivity_y = 0 then flyfps_extra_turn_sensitivity_y calculated from this value.  

flyfps_debug: false, // when set to true, its create a debug.txt file where the touchmote.exe. First start touchmote and switch to flyfps mode. Then debug.txt can open with LiveGraph free software and analyze your graph.

DEBUG description: 
IRDeltaX = input pointer relative delta, the main buffer is used here;
mouseMainSpeedX = mouse main vector output;
mouseMainStoreX = mouse main vector output, sum of IRDelta with decceleration / frame;
mouseOutX = Sum of all mouse moves. You can analyze. flyfps_mouse_finer take effect on all mouse moves (flyfps_main, flyfps_fine, flyfps_mouse_forward, flyfps_extra_turn, flyfps_border_turn, flyfps_auto_turn)
