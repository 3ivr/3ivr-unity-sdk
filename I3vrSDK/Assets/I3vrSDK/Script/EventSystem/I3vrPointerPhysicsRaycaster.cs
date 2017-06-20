/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// This script provides a raycaster for use with the I3vrPointerInputModule.
/// It behaves similarly to the standards Physics raycaster, except that it utilize raycast
/// modes specifically for I3vr.
///
/// View I3vrBasePointerRaycaster.cs and I3vrPointerInputModule.cs for more details.
public class I3vrPointerPhysicsRaycaster : I3vrBasePointerRaycaster
{
    /// Const to use for clarity when no event mask is set
    protected const int NO_EVENT_MASK_SET = -1;

    /// Layer mask used to filter events. Always combined with the camera's culling mask if a camera is used.
    [SerializeField]
    protected LayerMask raycasterEventMask = NO_EVENT_MASK_SET;

    /// Stored reference to the event camera.
    private Camera cachedEventCamera;

    /// eventCamera is used for masking layers and determining the distance of the raycast.
    /// It will use the camera on the same object as this script.
    /// If there is none, it will use the main camera.
    public override Camera eventCamera
    {
        get
        {
            if (cachedEventCamera == null)
            {
                cachedEventCamera = GetComponent<Camera>();
            }
            return cachedEventCamera != null ? cachedEventCamera : Camera.main;
        }
    }

    /// Event mask used to determine which objects will receive events.
    public int finalEventMask
    {
        get
        {
            return (eventCamera != null) ? eventCamera.cullingMask & eventMask : NO_EVENT_MASK_SET;
        }
    }

    /// Layer mask used to filter events. Always combined with the camera's culling mask if a camera is used.
    public LayerMask eventMask
    {
        get
        {
            return raycasterEventMask;
        }
        set
        {
            raycasterEventMask = value;
        }
    }

    protected I3vrPointerPhysicsRaycaster()
    {
    }

    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        if (eventCamera == null)
        {
            return;
        }

        if (!IsPointerAvailable())
        {
            return;
        }

        Ray ray = GetRay();
        float dist = eventCamera.farClipPlane - eventCamera.nearClipPlane;
        float radius = PointerRadius;
        RaycastHit[] hits;

        if (radius > 0.0f)
        {
            hits = Physics.SphereCastAll(ray, radius, dist, finalEventMask);
        }
        else
        {
            hits = Physics.RaycastAll(ray, dist, finalEventMask);
        }

        if (hits.Length == 0)
        {
            return;
        }

        System.Array.Sort(hits, (r1, r2) => r1.distance.CompareTo(r2.distance));

        for (int b = 0, bmax = hits.Length; b < bmax; ++b)
        {
            Vector3 projection = Vector3.Project(hits[b].point - ray.origin, ray.direction);
            Vector3 hitPosition = projection + ray.origin;

            RaycastResult result = new RaycastResult
            {
                gameObject = hits[b].collider.gameObject,
                module = this,
                distance = hits[b].distance,
                worldPosition = hitPosition,
                worldNormal = hits[b].normal,
                screenPosition = eventData.position,
                index = resultAppendList.Count,
                sortingLayer = 0,
                sortingOrder = 0
            };

            resultAppendList.Add(result);
        }
    }
}
