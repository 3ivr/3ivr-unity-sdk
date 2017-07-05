/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/07/04 17:05
 */

using UnityEngine;
using UnityEngine.EventSystems;

/// This script provides shared functionality used by all I3vr raycasters.
public abstract class I3vrBasePointerRaycaster : BaseRaycaster
{
    public enum RaycastMode
    {
        /// Default method for casting ray.
        /// Casts a ray from the camera through the target of the pointer.
        /// This is ideal for reticles that are always rendered on top.
        /// The object that is selected will always be the object that appears
        /// underneath the reticle from the perspective of the camera.
        /// This also prevents the reticle from appearing to "jump" when it starts/stops hitting an object.
        ///
        /// Note: This will prevent the user from pointing around an object to hit something that is out of sight.
        /// This isn't a problem in a typical use case.
        Camera,
        /// Cast a ray directly from the pointer origin.
        /// This is ideal for full-length laser pointers.
        Direct
    }

    /// Determines which raycast mode to use for this raycaster.
    public RaycastMode raycastMode = RaycastMode.Camera;

    private Ray lastRay;

    /// Returns the pointer's maximum distance from the pointer's origin.
    public float MaxPointerDistance
    {
        get
        {
            if (I3vrPointerManager.Pointer == null)
            {
                return 0.0f;
            }

            return I3vrPointerManager.Pointer.MaxPointerDistance;
        }
    }

    /// Returns the pointer's radius to use for the raycast.
    public float PointerRadius
    {
        get
        {
            if (I3vrPointerManager.Pointer == null)
            {
                return 0.0f;
            }

            float enterRadius, exitRadius;
            I3vrPointerManager.Pointer.GetPointerRadius(out enterRadius, out exitRadius);
            if (I3vrPointerManager.Pointer.ShouldUseExitRadiusForRaycast)
            {
                return exitRadius;
            }
            else
            {
                return enterRadius;
            }
        }
    }

    protected I3vrBasePointerRaycaster()
    {
    }

    /// Returns true if the pointer and the pointer's transform are both
    /// available through the I3vrPointerManager.
    public bool IsPointerAvailable()
    {
        if (I3vrPointerManager.Pointer == null)
        {
            return false;
        }

        if (I3vrPointerManager.Pointer.PointerTransform == null)
        {
            return false;
        }

        return true;
    }

    public Ray GetLastRay()
    {
        return lastRay;
    }

    /// Calculates the ray to use for raycasting based on
    /// the selected raycast mode.
    protected Ray GetRay()
    {
        if (!IsPointerAvailable())
        {
            Debug.LogError("Calling GetRay when the pointer isn't available.");
            lastRay = new Ray();
            return lastRay;
        }

        Transform pointerTransform = I3vrPointerManager.Pointer.PointerTransform;

        switch (raycastMode)
        {
            case RaycastMode.Camera:
                Vector3 rayPointerStart = pointerTransform.position;
                Vector3 rayPointerEnd = rayPointerStart + (pointerTransform.forward * MaxPointerDistance);

                Vector3 cameraLocation = Camera.main.transform.position;
                Vector3 finalRayDirection = rayPointerEnd - cameraLocation;
                finalRayDirection.Normalize();

                Vector3 finalRayStart = cameraLocation + (finalRayDirection * Camera.main.nearClipPlane);

                lastRay = new Ray(finalRayStart, finalRayDirection);
                break;
            case RaycastMode.Direct:
                lastRay = new Ray(pointerTransform.position, pointerTransform.forward);
                break;
            default:
                lastRay = new Ray();
                break;
        }

        return lastRay;
    }
}
