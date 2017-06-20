/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;

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

    void LateUpdate()
    {
        Vector3 jointPosition;
        Quaternion jointRotation;
        switch (joint)
        {
            case Joint.Pointer:
                jointPosition = I3vrArmModel.Instance.pointerPosition;
                jointRotation = I3vrArmModel.Instance.pointerRotation;
                break;
            case Joint.Wrist:
                jointPosition = I3vrArmModel.Instance.wristPosition;
                jointRotation = I3vrArmModel.Instance.wristRotation;
                break;
            case Joint.Elbow:
                jointPosition = I3vrArmModel.Instance.elbowPosition;
                jointRotation = I3vrArmModel.Instance.elbowRotation;
                break;
            case Joint.Shoulder:
                jointPosition = I3vrArmModel.Instance.shoulderPosition;
                jointRotation = I3vrArmModel.Instance.shoulderRotation;
                break;
            default:
                throw new System.Exception("Invalid FromJoint.");
        }
        transform.localPosition = jointPosition;
        transform.localRotation = jointRotation;
    }
}
