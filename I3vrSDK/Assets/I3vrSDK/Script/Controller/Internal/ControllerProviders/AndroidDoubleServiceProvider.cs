/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/07/19 14:25
 */

using UnityEngine;
using System;
using System.Collections.Generic;

namespace i3vr
{
    class AndroidDoubleServiceProvider : IControllerProvider
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

        private const string UNITY_PLAY_CLASS_NAME = "com.unity3d.player.UnityPlayer";
        private const string CURRENT_ACTIVITY_FIELD_NAME = "currentActivity";
        private const string I3VR_CONTROLLER_CLASS_NAME = "cn.i3vr.vr.sdk.controller.I3vrControllers";

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

        private AndroidJavaClass javaUnityPlayer;
        private AndroidJavaObject currentActivity;
        private static AndroidJavaObject androidPlugin;
        private AndroidJavaObject rightAndroidPlugin;
        private AndroidJavaObject leftAndroidPlugin;

        private float[] rightRawOri;
        private float[] rightRawAccel;
        private float[] rightRawGyro;
        private float[] rightTouchPos;
        private float[] leftRawOri;
        private float[] leftRawAccel;
        private float[] leftRawGyro;
        private float[] leftTouchPos;

        private Quaternion rightrawOriQua = Quaternion.identity;
        private Vector3 rightrawAccelV3 = Vector3.zero;
        private Vector3 rightrawGyrolV3 = Vector3.zero;
        private Vector2 righttouchPosV2 = Vector2.zero;
        private Quaternion leftrawOriQua = Quaternion.identity;
        private Vector3 leftrawAccelV3 = Vector3.zero;
        private Vector3 leftrawGyrolV3 = Vector3.zero;
        private Vector2 lefttouchPosV2 = Vector2.zero;

        private MutablePose3D rightpose3d = new MutablePose3D();
        private bool rightinitialRecenterDone = false;
        private Quaternion rightlastRawOrientation = Quaternion.identity;
        private Vector3 rightYawRotation = Vector3.zero;
        private MutablePose3D leftpose3d = new MutablePose3D();
        private bool leftinitialRecenterDone = false;
        private Quaternion leftlastRawOrientation = Quaternion.identity;
        private Vector3 leftYawRotation = Vector3.zero;

        public static long rightFrame;
        public static long leftFrame;



        internal AndroidDoubleServiceProvider(string deviceName_1 = "i3vr", string deviceName_2 = "i3vr")
        {
            javaUnityPlayer = new AndroidJavaClass(UNITY_PLAY_CLASS_NAME);
            currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>(CURRENT_ACTIVITY_FIELD_NAME);
            androidPlugin = new AndroidJavaObject(I3VR_CONTROLLER_CLASS_NAME, currentActivity);
            OnCreate();
            rightAndroidPlugin = androidPlugin.Call<AndroidJavaObject>("getController1stInfo");
            leftAndroidPlugin = androidPlugin.Call<AndroidJavaObject>("getController2ndInfo");
            SetDeviceName(0, deviceName_1);
            SetDeviceName(1, deviceName_2);
            OnResumeBle();
        }

        ~AndroidDoubleServiceProvider()
        {
            OnDestroy();
        }

        public void OnPause() { }

        public void OnResume() { }

        public void RightReadState(ControllerState outState)
        {
            lock (outState)
            {
                outState.connectionState = I3vrControllerConnectionState(rightAndroidPlugin);
                if (outState.connectionState == I3vrConnectionState.Connected)
                {
                    rightFrame = OnGetFrameNumber(rightAndroidPlugin);
                    outState.apiStatus = I3vrControllerGetApiStatus(rightAndroidPlugin);
                    rightRawOri = I3vrControllerOrientation(rightAndroidPlugin);
                    rightrawOriQua.Set(-rightRawOri[1], rightRawOri[2], -rightRawOri[0], rightRawOri[3]);
                    rightpose3d.Set(Vector3.zero, rightrawOriQua * Quaternion.Euler(Vector3.forward * -180));
                    rightpose3d.SetRightHanded(rightpose3d.Matrix);
                    rightlastRawOrientation = rightpose3d.Orientation;
                    if ((!rightinitialRecenterDone || outState.recentered) && outState.connectionState == I3vrConnectionState.Connected)
                    {
                        rightinitialRecenterDone = true;
                        outState.headsetRecenterRequested = true;
                        rightYawRotation = Camera.main.transform.rotation.eulerAngles - rightpose3d.Orientation.eulerAngles;
                        rightYawRotation = Vector3.up * rightYawRotation.y;
                    }
                    outState.orientation = Quaternion.Euler(rightYawRotation) * rightlastRawOrientation;

                    rightRawAccel = I3vrControllerAccel(rightAndroidPlugin);
                    rightrawAccelV3.Set(-rightRawAccel[1], rightRawAccel[2], -rightRawAccel[0]);
                    outState.accel = rightrawAccelV3;

                    rightRawGyro = I3vrControllerGyro(rightAndroidPlugin);
                    rightrawGyrolV3.Set(-rightRawGyro[1], rightRawGyro[2], -rightRawGyro[0]);
                    outState.gyro = rightrawGyrolV3;

                    rightTouchPos = I3vrControllerTouchPos(rightAndroidPlugin);
                    righttouchPosV2.Set(rightTouchPos[0], rightTouchPos[1]);
                    outState.touchPos = righttouchPosV2;

                    outState.appButtonState = I3vrControllerButtonState(rightAndroidPlugin, buttonType.AppButton);
                    outState.appButtonDown = I3vrControllerButtonDown(rightAndroidPlugin, buttonType.AppButton);
                    outState.appButtonUp = I3vrControllerButtonUp(rightAndroidPlugin, buttonType.AppButton);

                    outState.triggerButtonState = I3vrControllerButtonState(rightAndroidPlugin, buttonType.TriggerButton);
                    outState.triggerButtonDown = I3vrControllerButtonDown(rightAndroidPlugin, buttonType.TriggerButton);
                    outState.triggerButtonUp = I3vrControllerButtonUp(rightAndroidPlugin, buttonType.TriggerButton);

                    outState.homeButtonState = I3vrControllerButtonState(rightAndroidPlugin, buttonType.HomeButton);
                    outState.homeButtonDown = I3vrControllerButtonDown(rightAndroidPlugin, buttonType.HomeButton);
                    outState.homeButtonUp = I3vrControllerButtonUp(rightAndroidPlugin, buttonType.HomeButton);

                    outState.switchButtonState = I3vrControllerButtonState(rightAndroidPlugin, buttonType.SwitchButton);
                    outState.switchButtonDown = I3vrControllerButtonDown(rightAndroidPlugin, buttonType.SwitchButton);
                    outState.switchButtonUp = I3vrControllerButtonUp(rightAndroidPlugin, buttonType.SwitchButton);

                    outState.isTouching = I3vrControllerIsTouching(rightAndroidPlugin);
                    outState.touchDown = I3vrControllerTouchDown(rightAndroidPlugin);
                    outState.touchUp = I3vrControllerTouchUp(rightAndroidPlugin);

                    I3vrControllerGestureDirection(rightAndroidPlugin, outState);

                    outState.recentered = I3vrControllerRecentered(rightAndroidPlugin);

                    outState.headsetRecenterRequested = outState.recentered;

                    outState.errorDetails = "";
                }
            }
        }

        public void LeftReadState(ControllerState outState)
        {
            lock (outState)
            {
                outState.connectionState = I3vrControllerConnectionState(leftAndroidPlugin);
                if (outState.connectionState == I3vrConnectionState.Connected)
                {
                    leftFrame = OnGetFrameNumber(leftAndroidPlugin);
                    OnGetFrameNumber(leftAndroidPlugin);
                    outState.apiStatus = I3vrControllerGetApiStatus(leftAndroidPlugin);
                    leftRawOri = I3vrControllerOrientation(leftAndroidPlugin);
                    leftrawOriQua.Set(-leftRawOri[1], leftRawOri[2], -leftRawOri[0], leftRawOri[3]);
                    leftpose3d.Set(Vector3.zero, leftrawOriQua * Quaternion.Euler(Vector3.forward * -180));
                    leftpose3d.SetRightHanded(leftpose3d.Matrix);
                    leftlastRawOrientation = leftpose3d.Orientation;
                    if ((!leftinitialRecenterDone || outState.recentered) && outState.connectionState == I3vrConnectionState.Connected)
                    {
                        leftinitialRecenterDone = true;
                        outState.headsetRecenterRequested = true;
                        leftYawRotation = Camera.main.transform.rotation.eulerAngles - leftpose3d.Orientation.eulerAngles;
                        leftYawRotation = Vector3.up * leftYawRotation.y;
                    }
                    outState.orientation = Quaternion.Euler(leftYawRotation) * leftlastRawOrientation;

                    leftRawAccel = I3vrControllerAccel(leftAndroidPlugin);
                    leftrawAccelV3.Set(-leftRawAccel[1], leftRawAccel[2], -leftRawAccel[0]);
                    outState.accel = leftrawAccelV3;

                    leftRawGyro = I3vrControllerGyro(leftAndroidPlugin);
                    leftrawGyrolV3.Set(-leftRawGyro[1], leftRawGyro[2], -leftRawGyro[0]);
                    outState.gyro = leftrawGyrolV3;

                    leftTouchPos = I3vrControllerTouchPos(leftAndroidPlugin);
                    lefttouchPosV2.Set(leftTouchPos[0], leftTouchPos[1]);
                    outState.touchPos = lefttouchPosV2;

                    outState.appButtonState = I3vrControllerButtonState(leftAndroidPlugin, buttonType.AppButton);
                    outState.appButtonDown = I3vrControllerButtonDown(leftAndroidPlugin, buttonType.AppButton);
                    outState.appButtonUp = I3vrControllerButtonUp(leftAndroidPlugin, buttonType.AppButton);

                    outState.triggerButtonState = I3vrControllerButtonState(leftAndroidPlugin, buttonType.TriggerButton);
                    outState.triggerButtonDown = I3vrControllerButtonDown(leftAndroidPlugin, buttonType.TriggerButton);
                    outState.triggerButtonUp = I3vrControllerButtonUp(leftAndroidPlugin, buttonType.TriggerButton);

                    outState.homeButtonState = I3vrControllerButtonState(leftAndroidPlugin, buttonType.HomeButton);
                    outState.homeButtonDown = I3vrControllerButtonDown(leftAndroidPlugin, buttonType.HomeButton);
                    outState.homeButtonUp = I3vrControllerButtonUp(leftAndroidPlugin, buttonType.HomeButton);

                    outState.switchButtonState = I3vrControllerButtonState(leftAndroidPlugin, buttonType.SwitchButton);
                    outState.switchButtonDown = I3vrControllerButtonDown(leftAndroidPlugin, buttonType.SwitchButton);
                    outState.switchButtonUp = I3vrControllerButtonUp(leftAndroidPlugin, buttonType.SwitchButton);

                    outState.isTouching = I3vrControllerIsTouching(leftAndroidPlugin);
                    outState.touchDown = I3vrControllerTouchDown(leftAndroidPlugin);
                    outState.touchUp = I3vrControllerTouchUp(leftAndroidPlugin);

                    I3vrControllerGestureDirection(leftAndroidPlugin, outState);

                    outState.recentered = I3vrControllerRecentered(leftAndroidPlugin);

                    outState.headsetRecenterRequested = outState.recentered;

                    outState.errorDetails = "";
                }
            }
        }

        private void OnCreate()
        {
            androidPlugin.Call("onCreate");
        }

        private void OnStart()
        {
            androidPlugin.Call("onStart");
        }

        private void SetDeviceName(int index, String deviceName)
        {
            androidPlugin.Call("setDeviceName", index, deviceName);
        }

        private static void OnStop()
        {
            androidPlugin.Call("onStop");
        }

        private long OnGetFrameNumber(AndroidJavaObject javaObj)
        {
            return javaObj.Call<long>("getFrameNumber");
        }

        private void OnResumeBle()
        {
            androidPlugin.Call("onResume");
        }

        private static void OnDestroy()
        {
            androidPlugin.Call("onDestroy");
        }

        public static void BleDestroy()
        {
            OnDestroy();
        }

        private I3vrConnectionState I3vrControllerConnectionState(AndroidJavaObject controller)
        {
            switch (controller.Call<int>("getConnectionState"))
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

        private I3vrControllerApiStatus I3vrControllerGetApiStatus(AndroidJavaObject controller)
        {
            switch (controller.Call<int>("getApiStatus"))
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

        private float[] I3vrControllerOrientation(AndroidJavaObject controller)
        {
            return controller.Call<float[]>("getQuaternion");
        }

        private float[] I3vrControllerAccel(AndroidJavaObject controller)
        {
            return controller.Call<float[]>("getAccelerometer");
        }

        private float[] I3vrControllerGyro(AndroidJavaObject controller)
        {
            return controller.Call<float[]>("getGyro");
        }

        private float[] I3vrControllerTouchPos(AndroidJavaObject controller)
        {
            return controller.Call<float[]>("getTouchPos");
        }

        private bool I3vrControllerIsTouching(AndroidJavaObject controller)
        {
            return controller.Call<bool>("isTouching");
        }

        private bool I3vrControllerTouchUp(AndroidJavaObject controller)
        {
            return controller.Call<bool>("getTouchUp");
        }

        private bool I3vrControllerTouchDown(AndroidJavaObject controller)
        {
            return controller.Call<bool>("getTouchDown");
        }

        private bool I3vrControllerButtonState(AndroidJavaObject controller, buttonType cb)
        {
            return controller.Call<bool>("getButtonState", (int)cb);
        }

        private bool I3vrControllerButtonDown(AndroidJavaObject controller, buttonType cb)
        {
            return controller.Call<bool>("getButtonDown", (int)cb);
        }

        private bool I3vrControllerButtonUp(AndroidJavaObject controller, buttonType cb)
        {
            return controller.Call<bool>("getButtonUp", (int)cb);
        }

        private bool I3vrControllerRecentered(AndroidJavaObject controller)
        {
            return controller.Call<bool>("getRecentered");
        }

        private void I3vrControllerGestureDirection(AndroidJavaObject controller, ControllerState outState)
        {
            outState.touchGestureUp = false;
            outState.touchGestureDown = false;
            outState.touchGestureLeft = false;
            outState.touchGestureRight = false;
            int gestureDirection = controller.Call<int>("getGestureDirection");
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

        public void ReadState(ControllerState outState)
        {

        }
    }
}
