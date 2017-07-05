/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/07/04 17:05
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VR;
using System.Collections;

/// Helper functions to perform common math operations for I3vr.
public static class I3vrMathHelpers
{
    private static Vector2 sphericalCoordinatesResult;

    public static Vector3 GetIntersectionPosition(Camera cam, RaycastResult raycastResult)
    {
        // Check for camera
        if (cam == null)
        {
            return Vector3.zero;
        }

        float intersectionDistance = raycastResult.distance + cam.nearClipPlane;
        Vector3 intersectionPosition = cam.transform.position + cam.transform.forward * intersectionDistance;
        return intersectionPosition;
    }

    public static Vector2 GetViewportCenter()
    {
        int viewportWidth = Screen.width;
        int viewportHeight = Screen.height;
        // I3VR native integration is supported.
        if (VRSettings.enabled)
        {
            viewportWidth = VRSettings.eyeTextureWidth;
            viewportHeight = VRSettings.eyeTextureHeight;
        }
        return new Vector2(0.5f * viewportWidth, 0.5f * viewportHeight);
    }

    public static Vector2 NormalizedCartesianToSpherical(Vector3 cartCoords)
    {
        cartCoords.Normalize();

        if (cartCoords.x == 0)
        {
            cartCoords.x = Mathf.Epsilon;
        }

        float outPolar = Mathf.Atan(cartCoords.z / cartCoords.x);

        if (cartCoords.x < 0)
        {
            outPolar += Mathf.PI;
        }

        float outElevation = Mathf.Asin(cartCoords.y);

        sphericalCoordinatesResult.x = outPolar;
        sphericalCoordinatesResult.y = outElevation;
        return sphericalCoordinatesResult;
    }
}
