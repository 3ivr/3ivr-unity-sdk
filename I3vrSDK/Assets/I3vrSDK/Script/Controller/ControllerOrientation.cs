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
        public bool UseLateUpdate;
        public bool UseLocalOrientation = false;

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
                transform.localRotation = I3vrController.Orientation;
            }
            else
            {
                transform.rotation = I3vrController.Orientation;
            }
        }
    }
}
