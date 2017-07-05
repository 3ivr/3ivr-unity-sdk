/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/07/04 17:05
 */

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// Interface for manipulating an InputModule used by _I3vrPointerInputModuleImpl_
public interface II3vrInputModuleController
{
    EventSystem eventSystem { get; }
    List<RaycastResult> RaycastResultCache { get; }

    bool ShouldActivate();
    void Deactivate();
    GameObject FindCommonRoot(GameObject g1, GameObject g2);
    BaseEventData GetBaseEventData();
    RaycastResult FindFirstRaycast(List<RaycastResult> candidates);
}
