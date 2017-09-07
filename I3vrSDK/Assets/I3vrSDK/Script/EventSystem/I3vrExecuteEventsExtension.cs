﻿/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/07/04 17:05
 */

using UnityEngine.EventSystems;

/// This script extends the standard Unity EventSystem events with I3vr specific events.
public static class I3vrExecuteEventsExtension
{
    private static readonly ExecuteEvents.EventFunction<II3vrPointerHoverHandler> s_HoverHandler = Execute;

    private static void Execute(II3vrPointerHoverHandler handler, BaseEventData eventData)
    {
        handler.OnI3vrPointerHover(ExecuteEvents.ValidateEventData<PointerEventData>(eventData));
    }

    public static ExecuteEvents.EventFunction<II3vrPointerHoverHandler> pointerHoverHandler
    {
        get { return s_HoverHandler; }
    }
}
