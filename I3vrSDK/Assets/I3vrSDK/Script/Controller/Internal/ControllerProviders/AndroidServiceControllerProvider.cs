/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/07/19 14:25
 */

using UnityEngine;

namespace i3vr
{
    class AndroidServiceControllerProvider : IControllerProvider
    {
        private const string UNITY_PLAY_CLASS_NAME = "com.unity3d.player.UnityPlayer";
        private const string CURRENT_ACTIVITY_FIELD_NAME = "currentActivity";
        private const string I3VR_CONTROLLER_CLASS_NAME = "cn.i3vr.vr.sdk.controller.ControllerServiceFactory";
        private const string I3VR_UnityAPI_CLASS_NAME = "cn.i3vr.cw.explorer.UnityAPI";

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

        private const int I3VR_BUTTON_TRIGGER = 1 << 0;
        private const int I3VR_BUTTON_APP = 1 << 1;
        private const int I3VR_BUTTON_RETURN = 1 << 2;
        private const int I3VR_BUTTON_HOME = 1 << 3;

        private const int I3VR_GESTURE_DIRECTION_NONE = 0;
        private const int I3VR_GESTURE_DIRECTION_UP = 1;
        private const int I3VR_GESTURE_DIRECTION_DOWN = 2;
        private const int I3VR_GESTURE_DIRECTION_LEFT = 3;
        private const int I3VR_GESTURE_DIRECTION_RIGHT = 4;

        private const int I3VR_CONTROLLER_INDEX_RIGHT = 0;
        private const int I3VR_CONTROLLER_INDEX_LEFT = 1;

        private const int I3VR_CONTROLLER_HANDEDNESS_RIGHT = 0;
        private const int I3VR_CONTROLLER_HANDEDNESS_LEFT = 1;

        private static AndroidJavaClass javaUnityPlayer;
        private static AndroidJavaObject currentActivity;
        private static AndroidJavaObject androidPlugin;
        private static AndroidJavaObject androidService;
        private static AndroidJavaObject androidAPI;

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
        private Quaternion rightlastRawOrientation = Quaternion.identity;
        private MutablePose3D leftpose3d = new MutablePose3D();
        private Quaternion leftlastRawOrientation = Quaternion.identity;
        private bool isBind = false;
        private bool isRightReadyRecentered = true;
        private bool isLeftReadyRecentered = true;
        private bool initialRightRecenterDone = false;
        private bool initialLeftRecenterDone = false;

        private static Vector3 leftYawRotation = Vector3.zero;
        private static Vector3 rightYawRotation = Vector3.zero;

        private static bool adsa=true;

        internal AndroidServiceControllerProvider(string deviceName = "i3vr")
        {
            javaUnityPlayer = new AndroidJavaClass(UNITY_PLAY_CLASS_NAME);
            currentActivity = javaUnityPlayer.GetStatic<AndroidJavaObject>(CURRENT_ACTIVITY_FIELD_NAME);
            androidPlugin = new AndroidJavaObject(I3VR_CONTROLLER_CLASS_NAME);
            androidService = androidPlugin.CallStatic<AndroidJavaObject>("createControllerService");
            androidAPI = new AndroidJavaObject(I3VR_UnityAPI_CLASS_NAME);
            //InitDeviceInfos(false, null, "31:13:11:09:00:ED", "31:13:11:09:00:XX");//launcher
            //InitDeviceInfos(false, null, "33:33:33:33:33:33", "31:13:11:09:00:XX");//launcher
            InitDeviceInfos(false, deviceName, null, null);//launcher
            //OnBindService("com.I3vr.I3vrSDK");//app
            OnStart();//launcher
        }

        ~AndroidServiceControllerProvider()
        {
            OnStop();
        }

        public void OnPause() { }

        public void OnResume() { }

        public void ReadState(ControllerState outState, bool IsRight = true)
        {
            if (!isBind)
            {
                if (androidService.Call<bool>("isBind"))
                {
                    isBind = true;
                }
            }
            else
            {
                if (IsRight)
                {
                    RightReadState(outState);
                }
                else
                {
                    LeftReadState(outState);
                }
            }
        }

        public void RightReadState(ControllerState outState)
        {
            lock (outState)
            {
                outState.connectionState = I3vrControllerConnectionState(I3VR_CONTROLLER_INDEX_RIGHT);
                if (outState.connectionState == I3vrConnectionState.Connected)
                {
                    rightRawOri = I3vrControllerOrientation(I3VR_CONTROLLER_INDEX_RIGHT);
                    rightrawOriQua.Set(rightRawOri[0], rightRawOri[1], rightRawOri[2], rightRawOri[3]);
                    
                    rightpose3d.Set(Vector3.zero, rightrawOriQua);
                    rightpose3d.SetRightHanded(rightpose3d.Matrix);
                    rightlastRawOrientation = rightpose3d.Orientation;
                    if ((!initialRightRecenterDone || outState.recentered)&& !rightlastRawOrientation.Equals(Quaternion.identity))
                    {
                        initialRightRecenterDone =true;
                        isRightReadyRecentered = false;
                        outState.headsetRecenterRequested = true;
                        rightYawRotation = I3vrControllerManager.MainCamera.transform.rotation.eulerAngles - rightpose3d.Orientation.eulerAngles;
                        rightYawRotation = Vector3.up * rightYawRotation.y;
                        Vector3 RootRotation = Vector3.zero;
                        if (I3vrControllerManager.MainCamera.transform.root) {
                            if (I3vrControllerManager.MainCamera.transform.root.rotation != Quaternion.identity)
                            {
								RootRotation = I3vrControllerManager.MainCamera.transform.root.rotation.eulerAngles.y*Vector3.up;
                            }
                            rightYawRotation = Vector3.up * rightYawRotation.y - RootRotation;
                        }
                    }
                    outState.orientation = Quaternion.Euler(rightYawRotation) * rightlastRawOrientation;
                    
                    rightRawAccel = I3vrControllerAccel(I3VR_CONTROLLER_INDEX_RIGHT);
                    rightrawAccelV3.Set(rightRawAccel[0], rightRawAccel[1], -rightRawAccel[2]);
                    outState.accel = rightrawAccelV3;

                    rightRawGyro = I3vrControllerGyro(I3VR_CONTROLLER_INDEX_RIGHT);
                    rightrawGyrolV3.Set(-rightRawGyro[0], -rightRawGyro[1], rightRawGyro[2]);
                    outState.gyro = rightrawGyrolV3;

                    rightTouchPos = I3vrControllerTouchPos(I3VR_CONTROLLER_INDEX_RIGHT);
                    righttouchPosV2.Set(rightTouchPos[0], rightTouchPos[1]);
                    outState.touchPos = righttouchPosV2;

                    if (outState.homeButtonDown)
                    {
                        isRightReadyRecentered = true;
                    }

                    I3vrControllerUpdateState(outState, I3VR_CONTROLLER_INDEX_RIGHT);
                }
            }
        }

        public void LeftReadState(ControllerState outState)
        {
            lock (outState)
            {
                outState.connectionState = I3vrControllerConnectionState(I3VR_CONTROLLER_INDEX_LEFT);
                if (outState.connectionState == I3vrConnectionState.Connected)
                {
                    leftRawOri = I3vrControllerOrientation(I3VR_CONTROLLER_INDEX_LEFT);
                    leftrawOriQua.Set(leftRawOri[0], leftRawOri[1], leftRawOri[2], leftRawOri[3]);
                    leftpose3d.Set(Vector3.zero, leftrawOriQua);
                    leftpose3d.SetRightHanded(leftpose3d.Matrix);
                    leftlastRawOrientation = leftpose3d.Orientation;
                    if ((!initialLeftRecenterDone||outState.recentered) && !leftlastRawOrientation.Equals(Quaternion.identity))
                    {
                        initialLeftRecenterDone = true;
                        isLeftReadyRecentered = false;
                        outState.headsetRecenterRequested = true;
                        leftYawRotation = I3vrControllerManager.MainCamera.transform.rotation.eulerAngles - leftpose3d.Orientation.eulerAngles;
                        leftYawRotation = Vector3.up * leftYawRotation.y;
                        Vector3 RootRotation = Vector3.zero;
                        if (I3vrControllerManager.MainCamera.transform.root)
                        {
                            if (I3vrControllerManager.MainCamera.transform.root.rotation != Quaternion.identity)
                            {
                                RootRotation = I3vrControllerManager.MainCamera.transform.root.rotation.eulerAngles.y * Vector3.up;
                            }
                            leftYawRotation = Vector3.up * rightYawRotation.y - RootRotation;
                        }
                    }
                    outState.orientation = Quaternion.Euler(leftYawRotation) * leftlastRawOrientation;

                    leftRawAccel = I3vrControllerAccel(I3VR_CONTROLLER_INDEX_LEFT);
                    leftrawAccelV3.Set(leftRawAccel[0], leftRawAccel[1], -leftRawAccel[2]);
                    outState.accel = leftrawAccelV3;

                    leftRawGyro = I3vrControllerGyro(I3VR_CONTROLLER_INDEX_LEFT);
                    leftrawGyrolV3.Set(-leftRawGyro[0], -leftRawGyro[1], leftRawGyro[2]);
                    outState.gyro = leftrawGyrolV3;

                    leftTouchPos = I3vrControllerTouchPos(I3VR_CONTROLLER_INDEX_LEFT);
                    lefttouchPosV2.Set(leftTouchPos[0], leftTouchPos[1]);
                    outState.touchPos = lefttouchPosV2;

                    if (outState.homeButtonDown)
                    {
                        isLeftReadyRecentered = true;
                    }

                    I3vrControllerUpdateState(outState, I3VR_CONTROLLER_INDEX_LEFT);
                }
            }
        }

        private void I3vrControllerUpdateState(ControllerState outState, int index)
        {
            outState.apiStatus = I3vrControllerGetApiStatus(index);

            outState.appButtonState = I3vrControllerButtonState(index, I3VR_BUTTON_APP);
            outState.appButtonDown = I3vrControllerButtonDown(index, I3VR_BUTTON_APP);
            outState.appButtonUp = I3vrControllerButtonUp(index, I3VR_BUTTON_APP);

            outState.triggerButtonState = I3vrControllerButtonState(index, I3VR_BUTTON_TRIGGER);
            outState.triggerButtonDown = I3vrControllerButtonDown(index, I3VR_BUTTON_TRIGGER);
            outState.triggerButtonUp = I3vrControllerButtonUp(index, I3VR_BUTTON_TRIGGER);

            outState.returnButtonState = I3vrControllerButtonState(index, I3VR_BUTTON_RETURN);
            outState.returnButtonDown = I3vrControllerButtonDown(index, I3VR_BUTTON_RETURN);
            outState.returnButtonUp = I3vrControllerButtonUp(index, I3VR_BUTTON_RETURN);

            outState.homeButtonState = I3vrControllerButtonState(index, I3VR_BUTTON_HOME);
            outState.homeButtonDown = I3vrControllerButtonDown(index, I3VR_BUTTON_HOME);
            outState.homeButtonUp = I3vrControllerButtonUp(index, I3VR_BUTTON_HOME);

            outState.isTouching = I3vrControllerIsTouching(index);
            outState.touchDown = I3vrControllerTouchDown(index);
            outState.touchUp = I3vrControllerTouchUp(index);

            I3vrControllerGestureDirection(index, outState);

            outState.recentered = I3vrControllerRecentered(index);

            outState.headsetRecenterRequested = outState.recentered;

            outState.errorDetails = "";
        }

        public static void StartApp(string appPackageName)
        {
            androidAPI.Call("startApp", appPackageName, currentActivity);
        }

        public static void OnStart()
        {
            androidService.Call("start", currentActivity);
        }

        public static void OnBindService(string packageName)
        {
            androidService.Call("bindService", currentActivity);
        }

        public static void OnStop()
        {
            androidService.Call("stop", currentActivity);//launcher
            //androidService.Call("unBindService", currentActivity);//app
        }

        public static void InitDeviceInfos(bool isSingleLever, string deviceName, string mac0, string mac1)
        {
            androidService.Call("initDeviceInfos", currentActivity, isSingleLever, deviceName, mac0, mac1);
        }

        public static void ResetRightYawRotation()
        {
            Vector3 RootRotation = Vector3.zero;    
            if (I3vrControllerManager.MainCamera.transform.root)
            {
                RootRotation = I3vrControllerManager.MainCamera.transform.root.rotation.eulerAngles;
            }
            float yawRotation = rightYawRotation.y - I3vrControllerManager.MainCamera.transform.rotation.eulerAngles.y;
            yawRotation -= (((int)yawRotation) / 360) * 360.0f;
            SetRightYawRotation(yawRotation - (360 - RootRotation.y));
        }

        public static void ResetLeftYawRotation()
        {
            Vector3 RootRotation = Vector3.zero;    
            if (I3vrControllerManager.MainCamera.transform.root)
            {
                RootRotation = I3vrControllerManager.MainCamera.transform.root.rotation.eulerAngles;
            }
            float yawRotation = leftYawRotation.y - I3vrControllerManager.MainCamera.transform.rotation.eulerAngles.y;
            yawRotation -= (((int)yawRotation) / 360) * 360.0f;
            SetLeftYawRotation(yawRotation - (360 - RootRotation.y));
        }

        public static void ResetSingleLever(bool isSingleLever)
        {
            androidService.Call("resetSingleLever", isSingleLever);
        }

        public static void ResetDeviceName(string deviceName)
        {
            androidService.Call("resetDeviceName", deviceName);
        }

        public static void ResetMacAddress(int index, string macAddress)
        {
            androidService.Call("resetMacAddress", index, macAddress);
        }

        public static long GetFrameNumber(int index)
        {
            return androidService.Call<long>("getFrameNumber", index);
        }

        public static string GetDeviceName()
        {
            return androidService.Call<string>("getDeviceName");
        }

        public static string GetMacAddress(int index)
        {
            return androidService.Call<string>("getMacAddress", index);
        }

        public static string GetManufacturerName(int index)
        {
            return androidService.Call<string>("getManufacturerName", index);
        }

        public static string GetModelNumber(int index)
        {
            return androidService.Call<string>("getModelNumber", index);
        }

        public static string GetSerialNumber(int index)
        {
            return androidService.Call<string>("getSerialNumber", index);
        }

        public static string GetHardwareRevision(int index)
        {
            return androidService.Call<string>("getHardwareRevision", index);
        }

        public static string GetFirmwareRevision(int index)
        {
            return androidService.Call<string>("getFirmwareRevision", index);
        }

        public static string GetSoftwareRevision(int index)
        {
            return androidService.Call<string>("getSoftwareRevision", index);
        }

        public static void SetHandedness(int handedness)
        {
            androidService.Call("setHandedness", currentActivity, handedness);
        }

        public static void SetRightYawRotation(float yawRotation)
        {
            androidService.Call("setRightYawRotation", yawRotation);
        }

        public static void SetLeftYawRotation(float yawRotation)
        {
            androidService.Call("setLeftYawRotation", yawRotation);
        }

        public static void GetRightYawRotation()
        {
            rightYawRotation.y = androidService.Call<float>("getRightYawRotation");
        }

        public static void GetLeftYawRotation()
        {
            leftYawRotation.y = androidService.Call<float>("getLeftYawRotation");
        }

        public static long GetInterval(int index)
        {
            return androidService.Call<long>("getInterval", index);
        }

        public static I3vrControllerHandedness GetHandedness()
        {
            switch (androidService.Call<int>("getHandedness", currentActivity))
            {
                case I3VR_CONTROLLER_HANDEDNESS_RIGHT:
                    return I3vrControllerHandedness.Right;
                case I3VR_CONTROLLER_HANDEDNESS_LEFT:
                    return I3vrControllerHandedness.Left;
                default:
                    return I3vrControllerHandedness.Error;
            }
        }

        private I3vrConnectionState I3vrControllerConnectionState(int index)
        {
            switch (androidService.Call<int>("getConnectionState", index))
            {
                case I3VR_CONTROLLER_DISCONNECTED:
                    return I3vrConnectionState.Disconnected;
                case I3VR_CONTROLLER_SCANNING:
                    return I3vrConnectionState.Scanning;
                case I3VR_CONTROLLER_CONNECTING:
                    return I3vrConnectionState.Connecting;
                case I3VR_CONTROLLER_CONNECTED:
                    return I3vrConnectionState.Connected;
                default:
                    return I3vrConnectionState.Error;
            }
        }

        private I3vrControllerApiStatus I3vrControllerGetApiStatus(int index)
        {
            switch (androidService.Call<int>("getApiStatus", index))
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
                default:
                    return I3vrControllerApiStatus.Error;
            }
        }

        private float[] I3vrControllerOrientation(int index)
        {
            return androidService.Call<float[]>("getQuaternion", index);
        }

        private float[] I3vrControllerAccel(int index)
        {
            return androidService.Call<float[]>("getAccelerometer", index);
        }

        private float[] I3vrControllerGyro(int index)
        {
            return androidService.Call<float[]>("getGyro", index);
        }

        private float[] I3vrControllerTouchPos(int index)
        {
            return androidService.Call<float[]>("getTouchPos", index);
        }

        private bool I3vrControllerIsTouching(int index)
        {
            return androidService.Call<bool>("isTouching", index);
        }

        private bool I3vrControllerTouchUp(int index)
        {
            return androidService.Call<bool>("getTouchUp", index);
        }

        private bool I3vrControllerTouchDown(int index)
        {
            return androidService.Call<bool>("getTouchDown", index);
        }

        private bool I3vrControllerButtonState(int index, int cb)
        {
            return androidService.Call<bool>("getButtonState", index, cb);
        }

        private bool I3vrControllerButtonDown(int index, int cb)
        {
            return androidService.Call<bool>("getButtonDown", index, cb);
        }

        private bool I3vrControllerButtonUp(int index, int cb)
        {
            return androidService.Call<bool>("getButtonUp", index, cb);
        }

        private bool I3vrControllerRecentered(int index)
        {
            bool ReadyRecentered = isRightReadyRecentered;
            if (index.Equals(I3VR_CONTROLLER_INDEX_LEFT))
            {
                ReadyRecentered = isLeftReadyRecentered;
            }
            return androidService.Call<bool>("getRecentered", index) && ReadyRecentered;
        }

        private void I3vrControllerGestureDirection(int index, ControllerState outState)
        {
            outState.touchGestureUp = false;
            outState.touchGestureDown = false;
            outState.touchGestureLeft = false;
            outState.touchGestureRight = false;
            int gestureDirection = androidService.Call<int>("getGestureDirection", index);
            switch (gestureDirection)
            {
                case I3VR_GESTURE_DIRECTION_UP:
                    outState.touchGestureUp = true;
                    break;
                case I3VR_GESTURE_DIRECTION_DOWN:
                    outState.touchGestureDown = true;
                    break;
                case I3VR_GESTURE_DIRECTION_LEFT:
                    outState.touchGestureLeft = true;
                    break;
                case I3VR_GESTURE_DIRECTION_RIGHT:
                    outState.touchGestureRight = true;
                    break;
            }
        }
    }
}
