/*
 * Copyright (C) 2017 3ivr. All rights reserved.
 *
 * Author: Lucas(Wu Pengcheng)
 * Date  : 2017/07/04 17:05
 */

using UnityEngine;
using UnityEngine.EventSystems;

/// Provides an interface for executing events for _IEventSystemHandler_.
public interface II3vrEventExecutor
{
    bool Execute<T>(GameObject target,
      BaseEventData eventData,
      ExecuteEvents.EventFunction<T> functor)
      where T : IEventSystemHandler;

    GameObject ExecuteHierarchy<T>(GameObject root,
      BaseEventData eventData,
      ExecuteEvents.EventFunction<T> callbackFunction)
      where T : IEventSystemHandler;

    GameObject GetEventHandler<T>(GameObject root)
      where T : IEventSystemHandler;
}
