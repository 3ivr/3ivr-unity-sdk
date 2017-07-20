/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;

namespace i3vr
{
    /// <summary>
    /// Rotates a transform to match the controller orientation
    /// </summary>
    public class ControllerOrientation : MonoBehaviour
    {
        private I3vrController controller;

        public bool UseLateUpdate;
        public bool UseLocalOrientation = false;

        public DataSource ControllerDataSource = DataSource.Right;

        private void Start()
        {
            controller = I3vrControllerManager.I3vrRightController;
            if (ControllerDataSource == DataSource.Left)
            {
                controller = I3vrControllerManager.I3vrLeftController;
            }
        }

        void Update()
        {
            if (!UseLateUpdate)
            {
                UpdateOrient();
            }
        }

        void LateUpdate()
        {
            if (UseLateUpdate)
            {
                UpdateOrient();
            }
        }

        void UpdateOrient()
        {
            if (UseLocalOrientation)
            {
                transform.localRotation = controller.Orientation;
            }
            else
            {
                transform.rotation = controller.Orientation;
            }
        }
    }
}
