/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using System;

namespace i3vr
{
    interface IControllerProvider
    {
        /// Notifies the controller provider that the application has paused.
        void OnPause();

        /// Notifies the controller provider that the application has resumed.
        void OnResume();

        /// Reads the controller's current state and stores it in outState.
        void ReadState(ControllerState outState,bool isRightController);
    }
}
