/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/07/19 14:25
 */

using UnityEngine;

namespace i3vr
{
    public class I3vrControllerManager : MonoBehaviour
    {
        private static Camera _mainCamera;
        private static I3vrController _rightController;
        private static I3vrController _leftController;

        public Camera SetMainCamera;
        public I3vrController _SetRightController;
        public I3vrController _SetLeftController;

        public static float angle;
        public static I3vrController RightController
        {
            get
            {
                return _rightController;
            }
        }
        public static I3vrController LeftController
        {
            get
            {
                return _leftController;
            }
        }

        public static Camera MainCamera
        {
            get
            {
                return _mainCamera;
            }
        }

        void Awake()
        {
            if (_SetRightController || _SetLeftController)
            {
                _rightController = _SetRightController;
                _leftController = _SetLeftController;
            }
            else Debug.LogError("Not Set Controller");

            if (SetMainCamera)
            {
                _mainCamera = SetMainCamera;
            }
            else Debug.LogError("Not Set MainCamera");
        }

        private void OnApplicationQuit()
        {
            AndroidServiceControllerProvider.OnStop();
        }

        private void OnApplicationFocus(bool focus)
        {
            if (focus)
            {
                AndroidServiceControllerProvider.GetRightYawRotation();
                AndroidServiceControllerProvider.GetLeftYawRotation();
            }
            if (!focus)
            {
                AndroidServiceControllerProvider.ResetRightYawRotation();
                AndroidServiceControllerProvider.ResetLeftYawRotation();
            }
        }
    }
}