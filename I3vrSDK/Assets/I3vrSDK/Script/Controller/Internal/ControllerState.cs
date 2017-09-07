/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;

namespace i3vr
{
    class ControllerState
    {
        internal I3vrConnectionState connectionState = I3vrConnectionState.Disconnected;
        internal I3vrControllerApiStatus apiStatus = I3vrControllerApiStatus.Unavailable;
        internal Quaternion orientation = Quaternion.identity;
        internal Vector3 gyro = Vector3.zero;
        internal Vector3 accel = Vector3.zero;
        internal bool isTouching = false;
        internal Vector2 touchPos = Vector2.zero;
        internal bool touchDown = false;
        internal bool touchUp = false;
        internal bool recentered = false;
        internal bool touchGestureLeft = false;
        internal bool touchGestureRight = false;
        internal bool touchGestureUp = false;
        internal bool touchGestureDown = false;
        internal bool triggerButtonState = false;
        internal bool triggerButtonDown = false;
        internal bool triggerButtonUp = false;
        internal bool appButtonState = false;
        internal bool appButtonDown = false;
        internal bool appButtonUp = false;
        internal bool returnButtonState = false;
        internal bool returnButtonDown = false;
        internal bool returnButtonUp = false;
        internal bool homeButtonState = false;
        internal bool homeButtonDown = false;
        internal bool homeButtonUp = false;
        internal string errorDetails = "";
        // Indicates whether or not a headset recenter was requested.
        // This is up to the ControllerProvider implementation to decide.
        internal bool headsetRecenterRequested = false;
    }
}

