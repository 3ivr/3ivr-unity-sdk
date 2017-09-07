/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;

namespace i3vr
{
    /// @cond
    /// Encapsulates a rotation and a translation.  This is a convenience class that allows
    /// construction and value access either by Matrix4x4 or Quaternion + Vector3 types.
    public class Pose3D
    {
        /// Right-handed to left-handed matrix converter (and vice versa).
        protected static readonly Matrix4x4 flipZ = Matrix4x4.Scale(new Vector3(1, 1, -1));

        /// The translation component of the pose.
        public Vector3 Position { get; protected set; }

        /// The rotation component of the pose.
        public Quaternion Orientation { get; protected set; }

        /// The pose as a matrix in Unity gameobject convention (left-handed).
        public Matrix4x4 Matrix { get; protected set; }

        /// The pose as a matrix in right-handed coordinates.
        public Matrix4x4 RightHandedMatrix
        {
            get
            {
                return flipZ * Matrix * flipZ;
            }
        }

        /// Default constructor.
        /// Initializes position to the origin and orientation to the identity rotation.
        public Pose3D()
        {
            Position = Vector3.zero;
            Orientation = Quaternion.identity;
            Matrix = Matrix4x4.identity;
        }

        /// Constructor that takes a Vector3 and a Quaternion.
        public Pose3D(Vector3 position, Quaternion orientation)
        {
            Set(position, orientation);
        }

        /// Constructor that takes a Matrix4x4.
        public Pose3D(Matrix4x4 matrix)
        {
            Set(matrix);
        }

        protected void Set(Vector3 position, Quaternion orientation)
        {
            Position = position;
            Orientation = orientation;
            Matrix = Matrix4x4.TRS(position, orientation, Vector3.one);
        }

        protected void Set(Matrix4x4 matrix)
        {
            Matrix = matrix;
            Position = matrix.GetColumn(3);
            Orientation = Quaternion.LookRotation(matrix.GetColumn(2), matrix.GetColumn(1));
        }
    }
    /// @endcond

    /// @cond
    /// Mutable version of Pose3D.
    public class MutablePose3D : Pose3D
    {
        /// Sets the position and orientation from a Vector3 + Quaternion.
        public new void Set(Vector3 position, Quaternion orientation)
        {
            base.Set(position, orientation);
        }

        /// Sets the position and orientation from a Matrix4x4.
        public new void Set(Matrix4x4 matrix)
        {
            base.Set(matrix);
        }

        /// Sets the position and orientation from a right-handed Matrix4x4.
        public void SetRightHanded(Matrix4x4 matrix)
        {
            Set(flipZ * matrix * flipZ);
        }
    }
}
/// @endcond
