/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;

namespace i3vr
{
    /// This script positions and rotates the transform that it is attached to
    /// according to a joint in the arm model. See i3vrArmModel.cs for details.
    public class I3vrArmModelOffsets : MonoBehaviour
    {
        public enum Joint
        {
            Pointer,
            Wrist,
            Shoulder,
            Elbow
        }
        /// Determines which joint to set the position and rotation to.
        public Joint joint;
        public bool isRightSource;

        private I3vrController controller;

        private void Start()
        {
            controller = I3vrControllerManager.RightController;
            if (!isRightSource)
            {
                controller = I3vrControllerManager.LeftController;
            }
        }

        void LateUpdate()
        {
            Vector3 jointPosition;
            Quaternion jointRotation;
            switch (joint)
            {
                case Joint.Pointer:
                    jointPosition = controller.ArmModel.Instance.pointerPosition;
                    jointRotation = controller.ArmModel.Instance.pointerRotation;
                    break;
                case Joint.Wrist:
                    jointPosition = controller.ArmModel.Instance.wristPosition;
                    jointRotation = controller.ArmModel.Instance.wristRotation;
                    break;
                case Joint.Elbow:
                    jointPosition = controller.ArmModel.Instance.elbowPosition;
                    jointRotation = controller.ArmModel.Instance.elbowRotation;
                    break;
                case Joint.Shoulder:
                    jointPosition = controller.ArmModel.Instance.shoulderPosition;
                    jointRotation = controller.ArmModel.Instance.shoulderRotation;
                    break;
                default:
                    throw new System.Exception("Invalid FromJoint.");
            }
            transform.localPosition = jointPosition;
            transform.localRotation = jointRotation;
        }
    }
}