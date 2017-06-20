/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// Base implementation of II3vrPointer
///
/// Automatically registers pointer with I3vrPointerManager.
/// Uses transform that this script is attached to as the pointer transform.
public abstract class I3vrBasePointer : MonoBehaviour, II3vrPointer
{

    protected virtual void Start()
    {
        I3vrPointerManager.OnPointerCreated(this);
    }

    public bool ShouldUseExitRadiusForRaycast
    {
        get;
        set;
    }

    /// Declare methods from II3vrPointer
    public abstract void OnInputModuleEnabled();

    public abstract void OnInputModuleDisabled();

    public abstract void OnPointerEnter(GameObject targetObject, Vector3 intersectionPosition,
        Ray intersectionRay, bool isInteractive);

    public abstract void OnPointerHover(GameObject targetObject, Vector3 intersectionPosition,
        Ray intersectionRay, bool isInteractive);

    public abstract void OnPointerExit(GameObject targetObject);

    public abstract void OnPointerClickDown();

    public abstract void OnPointerClickUp();

    public abstract float GetMaxPointerDistance();

    public abstract void GetPointerRadius(out float enterRadius, out float exitRadius);

    public virtual Transform GetPointerTransform()
    {
        return transform;
    }
}
