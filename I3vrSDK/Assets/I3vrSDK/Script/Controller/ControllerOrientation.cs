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
        public bool isRightSource;
        public bool useLateUpdate;
        public bool useLocalOrientation = false;

        private I3vrController controller;

        private void Start()
        {
            controller = I3vrControllerManager.RightController;
            if (!isRightSource) {
                controller = I3vrControllerManager.LeftController;
            }
        }

        void Update()
        {
            if (!useLateUpdate)
            {
                UpdateOrient();
            }
        }

        void LateUpdate()
        {
            if (useLateUpdate)
            {
                UpdateOrient();
            }
        }

        void UpdateOrient()
        {
            if (useLocalOrientation)
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
