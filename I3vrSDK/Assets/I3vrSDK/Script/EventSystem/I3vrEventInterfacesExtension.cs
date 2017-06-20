/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/06/19 08:08
 */

using UnityEngine.EventSystems;

/// Interface to implement if you wish to receive OnI3vrPointerHover callbacks.
/// Executed by GazeInputModule.cs.
public interface II3vrPointerHoverHandler : IEventSystemHandler
{
    /// Called when pointer is hovering over GameObject.
    void OnI3vrPointerHover(PointerEventData eventData);
}
