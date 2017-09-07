/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/07/04 17:05
 */

using UnityEngine;

namespace i3vr
{
    /// I3vrPointerManager is a standard interface for
    /// controlling which I3vrBasePointer is being used
    /// for user input affordance.
    ///
    public class I3vrPointerManager : MonoBehaviour
    {
        private static I3vrPointerManager instance;

        /// Change the I3vrBasePointer that is currently being used.
        public static I3vrBasePointer Pointer
        {
            get
            {
                return instance == null ? null : instance.pointer;
            }
            set
            {
                if (instance == null || instance.pointer == value)
                {
                    return;
                }

                instance.pointer = value;
            }
        }

        /// I3vrBasePointer calls this when it is created.
        /// If a pointer hasn't already been assigned, it
        /// will assign the newly created one by default.
        ///
        /// This simplifies the common case of having only one
        /// I3vrBasePointer so is can be automatically hooked up
        /// to the manager.  If multiple I3vrGazePointers are in
        /// the scene, the app has to take responsibility for
        /// setting which one is active.
        public static void OnPointerCreated(I3vrBasePointer createdPointer)
        {
            if (instance != null && I3vrPointerManager.Pointer == null)
            {
                I3vrPointerManager.Pointer = createdPointer;
            }
        }

        private I3vrBasePointer pointer;

        void Awake()
        {
            if (instance != null)
            {
                Debug.LogError("More than one I3vrPointerManager instance was found in your scene. "
                  + "Ensure that there is only one I3vrPointerManager.");
                this.enabled = false;
                return;
            }

            instance = this;
        }

        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
