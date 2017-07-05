/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;
using System;
using System.Collections.Generic;

namespace i3vr
{
    class AndroidServiceProvider : IControllerProvider
    {
        enum buttonType
        {
            TriggerButton = 1 << 0,
            AppButton = 1 << 1,
            HomeButton = 1 << 2,
            SwitchButton = 1 << 3,
        }
        enum GestureDirection
        {
            None,
            Up,
            Down,
            Left,
            Right,
        }

        private const string className = "com.unity3d.player.UnityPlayer";
        private const string fieldName = "currentActivity";
        private const string apiClassName = "cn.i3vr.vr.sdk.controller.I3vrController";

        private static AndroidJavaClass javaUnityPlayer;
        private static AndroidJavaObject currentActivity;
        private static AndroidJavaObject androidPlugin;

        private float[] rawOri;
        private float[] rawAccel;
        private float[] rawGyro;
        private float[] touchPos;

        private Quaternion rawOriQua = Quaternion.identity;
        private Vector3 rawAccelV3 = Vector3.zero;
        private Vector3 rawGyrolV3 = Vector3.zero;
        private Vector2 touchPosV2 = Vector2.zero;

        private MutablePose3D pose3d = new MutablePose3D();
        private bool initialRecenterDone = false;
        private Quaternion lastRawOrientation = Quaternion.identity;
        private Vector3 YawRotation = Vector3.zero;

        private const int I3VR_CONTROLLER_DISCONNECTED = 0;
        private const int I3VR_CONTROLLER_SCANNING = 1;
        private const int I3VR_CONTROLLER_CONNECTING = 2;
        private const int I3VR_CONTROLLER_CONNECTED = 3;

        private const int I3VR_CONTROLLER_API_OK = 0;
        private const int I3VR_CONTROLLER_API_UNSUPPORTED = 1;
        private const int I3VR_CONTROLLER_API_NOT_AUTHORIZED = 2;
        private const int I3VR_CONTROLLER_API_UNAVAILABLE = 3;
        private const int I3VR_CONTROLLER_API_SERVICE_OBSOLETE = 4;
        private const int I3VR_CONTROLLER_API_CLIENT_OBSOLETE = 5;
        private const int I3VR_CONTROLLER_API_MALFUNCTION = 6;

        internal AndroidServiceProvider(string deviceName = "i3vr")
        {
            javaUnityPlayer = new AndroidJavaClass(className);
            currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>(fieldName);
            androidPlugin = new AndroidJavaObject(apiClassName, currentActivity);
            OnCreate();
            SetDeviceName(deviceName);
            OnStart();
        }

        ~AndroidServiceProvider()
        {
            OnStop();
        }

        public void OnPause() { }

        public void OnResume() { }

        public void ReadState(ControllerState outState)
        {
            lock (outState)
            {
                outState.connectionState = I3vrControllerConnectionState();
                if (outState.connectionState == I3vrConnectionState.Connected)
                {
                    outState.apiStatus = I3vrControllerGetApiStatus();

                    rawOri = I3vrControllerOrientation();
                    rawOriQua.Set(-rawOri[1], rawOri[2], -rawOri[0], rawOri[3]);
                    pose3d.Set(Vector3.zero, rawOriQua * Quaternion.Euler(Vector3.forward * -180));
                    pose3d.SetRightHanded(pose3d.Matrix);
                    HandleOrientationEvent(outState);

                    rawAccel = I3vrControllerAccel();
                    rawAccelV3.Set(-rawAccel[1], rawAccel[2], -rawAccel[0]);
                    outState.accel = rawAccelV3;

                    rawGyro = I3vrControllerGyro();
                    rawGyrolV3.Set(-rawGyro[1], rawGyro[2], -rawGyro[0]);
                    outState.gyro = rawGyrolV3;

                    touchPos = I3vrControllerTouchPos();
                    touchPosV2.Set(touchPos[0], touchPos[1]);
                    outState.touchPos = touchPosV2;

                    outState.appButtonState = I3vrControllerButtonState(buttonType.AppButton);
                    outState.appButtonDown = I3vrControllerButtonDown(buttonType.AppButton);
                    outState.appButtonUp = I3vrControllerButtonUp(buttonType.AppButton);

                    outState.triggerButtonState = I3vrControllerButtonState(buttonType.TriggerButton);
                    outState.triggerButtonDown = I3vrControllerButtonDown(buttonType.TriggerButton);
                    outState.triggerButtonUp = I3vrControllerButtonUp(buttonType.TriggerButton);

                    outState.homeButtonState = I3vrControllerButtonState(buttonType.HomeButton);
                    outState.homeButtonDown = I3vrControllerButtonDown(buttonType.HomeButton);
                    outState.homeButtonUp = I3vrControllerButtonUp(buttonType.HomeButton);

                    outState.switchButtonState = I3vrControllerButtonState(buttonType.SwitchButton);
                    outState.switchButtonDown = I3vrControllerButtonDown(buttonType.SwitchButton);
                    outState.switchButtonUp = I3vrControllerButtonUp(buttonType.SwitchButton);

                    outState.isTouching = I3vrControllerIsTouching();
                    outState.touchDown = I3vrControllerTouchDown();
                    outState.touchUp = I3vrControllerTouchUp();

                    I3vrControllerGestureDirection(outState);

                    outState.recentered = I3vrControllerRecentered();

                    outState.headsetRecenterRequested = outState.recentered;

                    outState.errorDetails = "";
                }
            }
        }

        private void HandleOrientationEvent(ControllerState outState)
        {
            lastRawOrientation = pose3d.Orientation;
            if ((!initialRecenterDone || outState.recentered) && outState.connectionState == I3vrConnectionState.Connected)
            {
                initialRecenterDone = true;
                outState.headsetRecenterRequested = true;
                YawRotation = Camera.main.transform.rotation.eulerAngles - pose3d.Orientation.eulerAngles;
                YawRotation = Vector3.up * YawRotation.y;
            }
            outState.orientation = Quaternion.Euler(YawRotation) * lastRawOrientation;
        }

        private void OnCreate()
        {
            androidPlugin.Call("onCreate");
        }

        private void OnStart()
        {
            androidPlugin.Call("onStart");
        }

        private void SetDeviceName(String deviceName)
        {
            androidPlugin.Call("setDeviceName", deviceName);
        }

        private static void OnStop()
        {
            androidPlugin.Call("onStop");
        }

        public static void BleRelease() {
            OnStop();
        }

        private I3vrConnectionState I3vrControllerConnectionState()
        {
            switch (androidPlugin.Call<int>("getConnectionState"))
            {
                case I3VR_CONTROLLER_DISCONNECTED:
                    return I3vrConnectionState.Disconnected;
                case I3VR_CONTROLLER_SCANNING:
                    return I3vrConnectionState.Scanning;
                case I3VR_CONTROLLER_CONNECTING:
                    return I3vrConnectionState.Connecting;
                case I3VR_CONTROLLER_CONNECTED:
                    return I3vrConnectionState.Connected;
                default: return I3vrConnectionState.Error;
            }
        }

        private I3vrControllerApiStatus I3vrControllerGetApiStatus()
        {
            switch (androidPlugin.Call<int>("getApiStatus"))
            {
                case I3VR_CONTROLLER_API_OK:
                    return I3vrControllerApiStatus.Ok;
                case I3VR_CONTROLLER_API_UNSUPPORTED:
                    return I3vrControllerApiStatus.Unsupported;
                case I3VR_CONTROLLER_API_NOT_AUTHORIZED:
                    return I3vrControllerApiStatus.NotAuthorized;
                case I3VR_CONTROLLER_API_UNAVAILABLE:
                    return I3vrControllerApiStatus.Unavailable;
                case I3VR_CONTROLLER_API_SERVICE_OBSOLETE:
                    return I3vrControllerApiStatus.ApiServiceObsolete;
                case I3VR_CONTROLLER_API_CLIENT_OBSOLETE:
                    return I3vrControllerApiStatus.ApiClientObsolete;
                case I3VR_CONTROLLER_API_MALFUNCTION:
                    return I3vrControllerApiStatus.ApiMalfunction;
                default: return I3vrControllerApiStatus.Error;
            }
        }

        private float[] I3vrControllerOrientation()
        {
            return androidPlugin.Call<float[]>("getQuaternion");
        }

        private float[] I3vrControllerAccel()
        {
            return androidPlugin.Call<float[]>("getAccelerometer");
        }

        private float[] I3vrControllerGyro()
        {
            return androidPlugin.Call<float[]>("getGyro");
        }

        private float[] I3vrControllerTouchPos()
        {
            return androidPlugin.Call<float[]>("getTouchPos");
        }

        private bool I3vrControllerIsTouching()
        {
            return androidPlugin.Call<bool>("isTouching");
        }

        private bool I3vrControllerTouchUp()
        {
            return androidPlugin.Call<bool>("getTouchUp");
        }

        private bool I3vrControllerTouchDown()
        {
            return androidPlugin.Call<bool>("getTouchDown");
        }

        private bool I3vrControllerButtonState(buttonType cb)
        {
            return androidPlugin.Call<bool>("getButtonState", (int)cb);
        }

        private bool I3vrControllerButtonDown(buttonType cb)
        {
            return androidPlugin.Call<bool>("getButtonDown", (int)cb);
        }

        private bool I3vrControllerButtonUp(buttonType cb)
        {
            return androidPlugin.Call<bool>("getButtonUp", (int)cb);
        }

        private bool I3vrControllerRecentered()
        {
            return androidPlugin.Call<bool>("getRecentered");
        }

        private void I3vrControllerGestureDirection(ControllerState outState)
        {
            outState.touchGestureUp = false;
            outState.touchGestureDown = false;
            outState.touchGestureLeft = false;
            outState.touchGestureRight = false;
            int gestureDirection = androidPlugin.Call<int>("getGestureDirection");
            switch (gestureDirection)
            {
                case (int)GestureDirection.Up:
                    outState.touchGestureUp = true;
                    break;
                case (int)GestureDirection.Down:
                    outState.touchGestureDown = true;
                    break;
                case (int)GestureDirection.Left:
                    outState.touchGestureLeft = true;
                    break;
                case (int)GestureDirection.Right:
                    outState.touchGestureRight = true;
                    break;
            }
        }
    }
}
