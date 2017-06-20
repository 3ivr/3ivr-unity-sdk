/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;

namespace i3vr
{
    public static class I3vrSettings
    {
        public enum UserPrefsHandedness
        {
            Error = -1,
            Right,
            Left
        }
        private static UserPrefsHandedness handedness = UserPrefsHandedness.Right;

        public static UserPrefsHandedness Handedness
        {
            get
            {
                return handedness;
            }
            set
            {
                handedness = value;
            }
        }

        private static Transform vrHeadTransform;
        public static Transform VrHeadTransform
        {
            get
            {
                return vrHeadTransform;
            }
            set
            {
                vrHeadTransform = value;
            }
        }
    }
}
