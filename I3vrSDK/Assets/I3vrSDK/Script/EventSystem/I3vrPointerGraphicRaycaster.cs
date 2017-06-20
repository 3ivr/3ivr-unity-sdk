/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// This script provides a raycaster for use with the I3vrPointerInputModule.
/// It behaves similarly to the standards Graphic raycaster, except that it utilize raycast
/// modes specifically for I3vr.
///
/// View I3vrBasePointerRaycaster.cs and I3vrPointerInputModule.cs for more details.
[RequireComponent(typeof(Canvas))]
public class I3vrPointerGraphicRaycaster : I3vrBasePointerRaycaster
{
    public enum BlockingObjects
    {
        None = 0,
        TwoD = 1,
        ThreeD = 2,
        All = 3,
    }

    private const int NO_EVENT_MASK_SET = -1;

    public bool ignoreReversedGraphics = true;
    public BlockingObjects blockingObjects = BlockingObjects.None;
    public LayerMask blockingMask = NO_EVENT_MASK_SET;

    private Canvas targetCanvas;
    private List<Graphic> raycastResults = new List<Graphic>();
    private Camera cachedPointerEventCamera;

    private static readonly List<Graphic> sortedGraphics = new List<Graphic>();

    public override Camera eventCamera
    {
        get
        {
            switch (raycastMode)
            {
                case RaycastMode.Direct:
                    if (cachedPointerEventCamera == null)
                    {
                        if (!IsPointerAvailable())
                        {
                            Debug.LogError("Can't find eventCamera, pointer is not available.");
                            return Camera.main;
                        }

                        Transform pointerTransform = I3vrPointerManager.Pointer.GetPointerTransform();
                        cachedPointerEventCamera = pointerTransform.GetComponent<Camera>();

                        if (cachedPointerEventCamera == null)
                        {
                            Debug.LogError("I3vrPointerGraphicRaycaster requires I3vrPointer to have a Camera when in Direct mode.");
                        }
                    }

                    return cachedPointerEventCamera != null ? cachedPointerEventCamera : Camera.main;
                case RaycastMode.Camera:
                default:
                    return Camera.main;
            }
        }
    }

    private Canvas canvas
    {
        get
        {
            if (targetCanvas != null)
                return targetCanvas;

            targetCanvas = GetComponent<Canvas>();
            return targetCanvas;
        }
    }

    protected I3vrPointerGraphicRaycaster()
    {
    }

    public override void Raycast(PointerEventData eventData, List<RaycastResult> resultAppendList)
    {
        if (canvas == null)
        {
            return;
        }

        if (eventCamera == null)
        {
            return;
        }

        if (!IsPointerAvailable())
        {
            return;
        }

        if (canvas.renderMode != RenderMode.WorldSpace)
        {
            Debug.LogError("I3vrPointerGraphicRaycaster requires that the canvase renderMode is set to WorldSpace.");
            return;
        }

        Ray ray = GetRay();
        float hitDistance = float.MaxValue;

        if (blockingObjects != BlockingObjects.None)
        {
            float dist = eventCamera.farClipPlane - eventCamera.nearClipPlane;

            if (blockingObjects == BlockingObjects.ThreeD || blockingObjects == BlockingObjects.All)
            {
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, dist, blockingMask))
                {
                    hitDistance = hit.distance;
                }
            }

            if (blockingObjects == BlockingObjects.TwoD || blockingObjects == BlockingObjects.All)
            {
                RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, dist, blockingMask);

                if (hit.collider != null)
                {
                    hitDistance = hit.fraction * dist;
                }
            }
        }

        raycastResults.Clear();
        Ray finalRay;
        Raycast(canvas, ray, eventCamera, MaxPointerDistance, raycastResults, out finalRay);

        for (int index = 0; index < raycastResults.Count; index++)
        {
            GameObject go = raycastResults[index].gameObject;
            bool appendGraphic = true;

            if (ignoreReversedGraphics)
            {
                // If we have a camera compare the direction against the cameras forward.
                Vector3 cameraFoward = eventCamera.transform.rotation * Vector3.forward;
                Vector3 dir = go.transform.rotation * Vector3.forward;
                appendGraphic = Vector3.Dot(cameraFoward, dir) > 0;
            }

            if (appendGraphic)
            {
                float distance = 0;

                Transform trans = go.transform;
                Vector3 transForward = trans.forward;
                // http://geomalgorithms.com/a06-_intersect-2.html
                distance = (Vector3.Dot(transForward, trans.position - finalRay.origin) / Vector3.Dot(transForward, finalRay.direction));

                // Check to see if the go is behind the camera.
                if (distance < 0)
                {
                    continue;
                }

                if (distance >= hitDistance)
                {
                    continue;
                }

                RaycastResult castResult = new RaycastResult
                {
                    gameObject = go,
                    module = this,
                    distance = distance,
                    worldPosition = finalRay.origin + (finalRay.direction * distance),
                    screenPosition = eventData.position,
                    index = resultAppendList.Count,
                    depth = raycastResults[index].depth,
                    sortingLayer = canvas.sortingLayerID,
                    sortingOrder = canvas.sortingOrder
                };
                resultAppendList.Add(castResult);
            }
        }
    }

    /// Perform a raycast into the screen and collect all graphics underneath it.
    private static void Raycast(Canvas canvas, Ray ray, Camera cam, float maxPointerDistance,
                                List<Graphic> results, out Ray finalRay)
    {
        Vector3 screenPoint = cam.WorldToScreenPoint(ray.GetPoint(maxPointerDistance));
        finalRay = cam.ScreenPointToRay(screenPoint);

        // Necessary for the event system
        IList<Graphic> foundGraphics = GraphicRegistry.GetGraphicsForCanvas(canvas);
        for (int i = 0; i < foundGraphics.Count; ++i)
        {
            Graphic graphic = foundGraphics[i];

            // -1 means it hasn't been processed by the canvas, which means it isn't actually drawn
            if (graphic.depth == -1 || !graphic.raycastTarget)
            {
                continue;
            }

            if (!RectTransformUtility.RectangleContainsScreenPoint(graphic.rectTransform, screenPoint, cam))
            {
                continue;
            }

            if (graphic.Raycast(screenPoint, cam))
            {
                sortedGraphics.Add(graphic);
            }
        }

        sortedGraphics.Sort((g1, g2) => g2.depth.CompareTo(g1.depth));

        for (int i = 0; i < sortedGraphics.Count; ++i)
        {
            results.Add(sortedGraphics[i]);
        }

        sortedGraphics.Clear();
    }
}
